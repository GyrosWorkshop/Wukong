using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using Wukong.Helpers;
using Wukong.Models;
using static System.Threading.Timeout;

namespace Wukong.Services
{
    internal class UserSong
    {
        public delegate void ClientSongChangedHandler(UserSong userSong, ClientSong previousSong);
        public event ClientSongChangedHandler ClientSongChanged;

        public readonly string UserId;
        private ClientSong _song;

        public ClientSong Song
        {
            get { return _song; }
            set
            {
                var previous = _song;
                _song = value;
                OnSongChanged(previous);
            }
        }

        public UserSong(string userId)
        {
            UserId = userId;
        }

        private void OnSongChanged(ClientSong previous)
        {
            ClientSongChanged?.Invoke(this, previous);
        }

        public UserSong Clone()
        {
            return new UserSong(UserId)
            {
                Song = Song
            };
        }
    }

    internal class ChannelUserList
    {
        private static readonly object UserListLock = new object();


        public delegate void UserListChangedHandler(bool add, string userId);

        public event UserListChangedHandler UserChanged;

        public delegate void CurrentChangedHandler(UserSong userSong);

        public event CurrentChangedHandler CurrentChanged;

        public delegate void NextSongChangedHandler(ClientSong song);

        public event NextSongChangedHandler NextChanged;

        private readonly LinkedList<UserSong> List = new LinkedList<UserSong>();
        private LinkedListNode<UserSong> _current;
        private LinkedListNode<UserSong> Current
        {
            get { return _current; }
            set
            {
                _current = value;
                CurrentPlayingChanged();
            }
        }

        public UserSong CurrentPlaying { get; private set; }
        public bool IsPlaying => CurrentPlaying?.Song != null;
        public bool Empty => List.Count == 0;
        public List<string> UserList => List.Select(it => it.UserId).ToList();
        public ClientSong NextSong { get; private set; }

        private void RefreshNextSong()
        {
            var next = FindNext(Current);
            if (NextSong != next?.Value?.Song)
            {
                NextSong = next?.Value?.Song;
                NextChanged?.Invoke(NextSong);
            }
        }

        private void CurrentPlayingChanged()
        {
            CurrentPlaying = Current?.Value?.Clone();
            CurrentChanged?.Invoke(CurrentPlaying?.Clone());
            RefreshNextSong();
        }

        public void AddUser(string userId)
        {
            var userSong = UserSong(userId);
            if (userSong == null)
            {
                lock (UserListLock)
                {
                    List.AddLast(new UserSong(userId));
                }
                UserChanged?.Invoke(true, userId);
            }
            if (!IsPlaying) GoNext();
            else RefreshNextSong();
        }

        public void RemoveUser(string userId)
        {
            //TODO: May cause deadlock?
            //TODO: removing current playing user may cause nextUser loopback to first.
            var userSong = UserSong(userId);
            if (userSong != null)
            {
                lock (UserListLock)
                {
                    List.Remove(userSong);
                }
                UserChanged?.Invoke(false, userId);
            }
            RefreshNextSong();
        }

        public void SetSong(string userId, ClientSong song)
        {
            var userSong = UserSong(userId);
            if (userSong != null)
            {
                userSong.Song = song;
            }
            if (!IsPlaying) GoNext();
            else RefreshNextSong();
        }

        private LinkedListNode<UserSong> FindNext(LinkedListNode<UserSong> item)
        {
            var next = Current.NextOrFirst(List);
            for (var i = 0; i != List.Count; ++i)
            {
                var song = next?.Value?.Song;
                if (song != null)
                {
                    return next;
                }
                next = next?.NextOrFirst(List);
            }
            return null;
        }

        public void GoNext()
        {
            Current = FindNext(Current);
        }

        private UserSong UserSong(string userId)
        {
            return List.FirstOrDefault(it => it.UserId == userId);
        }
    }

    public class Channel
    {
        public string Id { get; }
        private readonly ISocketManager SocketManager;
        private readonly IProvider Provider;
        private readonly IStorage Storage;
        private readonly IUserManager UserManager;

        private readonly ISet<string> FinishedUsers = new HashSet<string>();
        private readonly ISet<string> DownvoteUsers = new HashSet<string>();
        private readonly ChannelUserList List = new ChannelUserList();

        private DateTime StartTime;
        private Timer FinishTimeoutTimer;

        private double Elapsed => (DateTime.Now - StartTime).TotalSeconds;
        public bool Empty => List.Empty;
        private Song NextServerSong;
        public List<string> UserList => List.UserList;

        public Channel(string id, ISocketManager socketManager, IProvider provider, IStorage storage, IUserManager userManager)
        {
            Id = id;
            SocketManager = socketManager;
            Provider = provider;
            Storage = storage;
            UserManager = userManager;

            List.CurrentChanged += song =>
            {
                StartTime = DateTime.Now;
                FinishedUsers.Clear();
                DownvoteUsers.Clear();
                BroadcastPlayCurrentSong(song);
            };
            List.UserChanged += (add, userId) =>
            {
                BroadcastUserListUpdated();
            };
            List.NextChanged += song =>
            {
                BroadcastNextSongUpdated(song);
            };
        }

        public void Join(string userId)
        {
            List.AddUser(userId);
            if (SocketManager.IsConnected(userId))
            {
                EmitChannelInfo(userId);
            }
        }

        public void Leave(string userId)
        {
            List.RemoveUser(userId);
        }

        public void Connect(string userId)
        {
            EmitChannelInfo(userId);
        }

        public void Disconnect(string userId)
        {

        }

        public void UpdateSong(string userId, ClientSong song)
        {
            List.SetSong(userId, song);
        }

        public bool ReportFinish(string userId, ClientSong song, bool force = false)
        {
            if (song != List.CurrentPlaying?.Song) {
                CheckShouldForwardCurrentSong();
                return false;
            }
            if (force) DownvoteUsers.Add(userId);
            else FinishedUsers.Add(userId);
            CheckShouldForwardCurrentSong();
            return true;
        }

        public bool HasUser(string userId)
        {
            return List.UserList.Contains(userId);
        }

        private void CheckShouldForwardCurrentSong()
        {
            var userList = List.UserList;
            var downVoteUserCount = DownvoteUsers.Intersect(userList).Count;
            var undeterminedCount = userList.Except(DownvoteUsers).Except(FinishedUsers).Count();
            var connectedUserCount = userList.Select(it => SocketManager.IsConnected(it)).Count();
            if (!List.IsPlaying || downVoteUserCount >= QueryForceForwardCount(connectedUserCount) || undeterminedCount == 0)
            {
                ShouldForwardNow();
            }
            else if (undeterminedCount <= connectedUserCount * 0.5)
            {
                if (FinishTimeoutTimer != null) return;
                FinishTimeoutTimer = new Timer(ShouldForwardNow, null, 5 * 1000, Infinite);
            }
        }

        private static int QueryForceForwardCount(int total)
        {
            return Convert.ToInt32(Math.Ceiling((double)total / 2));
        }

        private void ShouldForwardNow(object state = null)
        {
            FinishTimeoutTimer?.Change(Infinite, Infinite);
            FinishTimeoutTimer?.Dispose();
            FinishTimeoutTimer = null;
            List.GoNext();
        }

        private void BroadcastUserListUpdated(string userId = null)
        {
            var users = List.UserList;
            SocketManager.SendMessage(userId != null ? new[] { userId } : users.ToArray(),
                new UserListUpdated
                {
                    ChannelId = Id,
                    Users = users.Select(it => UserManager.GetUser(it)).ToList()
                });
        }

        private async void BroadcastNextSongUpdated(ClientSong next, string userId = null)
        {
            if (next == null) return;
            if (NextServerSong == null || NextServerSong.SongId != next.SongId || NextServerSong.SiteId != next.SiteId)
            {
                NextServerSong = await Provider.GetSong(next, true);
            }
            if (NextServerSong == null) return;
            SocketManager.SendMessage(userId != null ? new[] { userId } : UserList.ToArray(),
                new NextSongUpdated
                {
                    ChannelId = Id,
                    Song = NextServerSong
                });
        }

        private async void BroadcastPlayCurrentSong(UserSong current, string userId = null)
        {
            Song song = null;
            if (current?.Song != null)
            {
                if (NextServerSong != null &&
                NextServerSong.SiteId == current.Song.SiteId &&
                NextServerSong.SongId == current.Song.SongId)
                {
                    song = NextServerSong;
                }
                else
                {
                    song = await Provider.GetSong(current.Song, true);
                }

                SocketManager.SendMessage(userId != null ? new[] { userId } : List.UserList.ToArray()
                    , new Play
                    {
                        ChannelId = Id,
                        Downvote = DownvoteUsers.Contains(userId),
                        Song = song ?? new Song
                        {
                            SiteId = current.Song.SiteId,
                            SongId = current.Song.SongId
                        },    // Workaround for play song == null problem
                    Elapsed = Elapsed,
                        User = current?.UserId
                    });

                if (song == null)
                {
                    BroadcastNotification(string.Format("Server error: Failed to get song {0}:{1}", current.Song.SiteId, current.Song.SongId), userId);
                }
            }
        }

        private void BroadcastNotification(string message, string userId = null)
        {
            SocketManager.SendMessage(userId != null ? new[] { userId } : List.UserList.ToArray(),
                new NotificationEvent
                {
                    ChannelId = Id,
                    Notification = new Notification
                    {
                        Message = message,
                        Timeout = 10000
                    }
                });
        }

        private void EmitChannelInfo(string userId)
        {
            BroadcastUserListUpdated(userId);
            BroadcastPlayCurrentSong(List.CurrentPlaying, userId);
            BroadcastNextSongUpdated(List.NextSong, userId);
        }
    }
}
