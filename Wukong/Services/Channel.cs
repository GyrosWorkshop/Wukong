using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Wukong.Helpers;
using Wukong.Models;
using static System.Threading.Timeout;

namespace Wukong.Services
{
    public class UserSong
    {
        public delegate void ClientSongChangedHandler(UserSong userSong, ClientSong previousSong);
        public event ClientSongChangedHandler ClientSongChanged;

        public readonly string UserId;
        private ClientSong song;

        public ClientSong Song
        {
            get => song;
            set
            {
                var previous = song;
                song = value;
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

    public class Channel
    {
        public string Id { get; }
        private readonly ISocketManager socketManager;
        private readonly IProvider provider;
        private readonly IUserManager userManager;

        private readonly ISet<string> finishedUsers = new HashSet<string>();
        private readonly ISet<string> downvoteUsers = new HashSet<string>();
        private readonly ChannelUserList list = new ChannelUserList();

        private DateTime startTime;
        private Timer finishTimeoutTimer;

        private double Elapsed => (DateTime.Now - startTime).TotalSeconds;
        public bool Empty => list.Empty;
        private Song nextServerSong;
        public List<string> UserList => list.UserList;

        public Channel(string id, ISocketManager socketManager, IProvider provider, IUserManager userManager)
        {
            Id = id;
            this.socketManager = socketManager;
            this.provider = provider;
            this.userManager = userManager;

            list.CurrentChanged += song =>
            {
                startTime = DateTime.Now;
                finishedUsers.Clear();
                downvoteUsers.Clear();
                BroadcastPlayCurrentSong(song);
            };
            list.UserChanged += (add, userId) =>
            {
                BroadcastUserListUpdated();
            };
            list.NextChanged += song =>
            {
                BroadcastNextSongUpdated(song);
            };
        }

        public void Join(string userId)
        {
            list.AddUser(userId);
            if (socketManager.IsConnected(userId))
            {
                EmitChannelInfo(userId);
            }
        }

        public void Leave(string userId)
        {
            list.RemoveUser(userId);
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
            list.SetSong(userId, song);
        }

        public bool ReportFinish(string userId, ClientSong song, bool force = false)
        {
            if (song != list.CurrentPlaying?.Song) {
                CheckShouldForwardCurrentSong();
                // Workaround: told the user the current song
                EmitChannelInfo(userId);
                return false;
            }
            if (force) downvoteUsers.Add(userId);
            else finishedUsers.Add(userId);
            CheckShouldForwardCurrentSong();
            return true;
        }

        public bool HasUser(string userId)
        {
            return list.UserList.Contains(userId);
        }

        private void CheckShouldForwardCurrentSong()
        {
            var userList = list.UserList;
            var downVoteUserCount = downvoteUsers.Intersect(userList).Count;
            var undeterminedCount = userList.Except(downvoteUsers).Except(finishedUsers).Count();
            var connectedUserCount = userList.Select(it => socketManager.IsConnected(it)).Count();
            if (!list.IsPlaying || downVoteUserCount >= QueryForceForwardCount(connectedUserCount) || undeterminedCount == 0)
            {
                ShouldForwardNow();
            }
            else if (undeterminedCount <= connectedUserCount * 0.5)
            {
                if (finishTimeoutTimer != null) return;
                finishTimeoutTimer = new Timer(ShouldForwardNow, null, 5 * 1000, Infinite);
            }
        }

        private static int QueryForceForwardCount(int total)
        {
            return Convert.ToInt32(Math.Ceiling((double)total / 2));
        }

        private void ShouldForwardNow(object state = null)
        {
            finishTimeoutTimer?.Change(Infinite, Infinite);
            finishTimeoutTimer?.Dispose();
            finishTimeoutTimer = null;
            list.GoNext();
        }

        private void BroadcastUserListUpdated(string userId = null)
        {
            var users = list.UserList;
            socketManager.SendMessage(userId != null ? new[] { userId } : users.ToArray(),
                new UserListUpdated
                {
                    ChannelId = Id,
                    Users = users.Select(it => userManager.GetUser(it)).ToList()
                });
        }

        private async void BroadcastNextSongUpdated(ClientSong next, string userId = null)
        {
            if (next == null) return;
            if (nextServerSong == null || nextServerSong.SongId != next.SongId || nextServerSong.SiteId != next.SiteId)
            {
                nextServerSong = await provider.GetSong(next, true);
            }
            if (nextServerSong == null) return;
            socketManager.SendMessage(userId != null ? new[] { userId } : UserList.ToArray(),
                new NextSongUpdated
                {
                    ChannelId = Id,
                    Song = nextServerSong
                });
        }

        private async void BroadcastPlayCurrentSong(UserSong current, string userId = null)
        {
            if (current?.Song != null)
            {
                Song song;
                if (nextServerSong != null &&
                nextServerSong.SiteId == current.Song.SiteId &&
                nextServerSong.SongId == current.Song.SongId)
                {
                    song = nextServerSong;
                }
                else
                {
                    song = await provider.GetSong(current.Song, true);
                }

                socketManager.SendMessage(userId != null ? new[] { userId } : list.UserList.ToArray()
                    , new Play
                    {
                        ChannelId = Id,
                        Downvote = downvoteUsers.Contains(userId),
                        Song = song ?? new Song
                        {
                            SiteId = current.Song.SiteId,
                            SongId = current.Song.SongId,
                            Title = "server load error",
                            Artist = current.Song.SiteId,
                            Album = current.Song.SongId
                        },    // Workaround for play song == null problem
                    Elapsed = Elapsed,
                        User = current.UserId
                    });

                if (song == null)
                {
                    BroadcastNotification(string.Format("Server error: Failed to get song {0}:{1}", current.Song.SiteId, current.Song.SongId), userId);
                }
            }
        }

        private void BroadcastNotification(string message, string userId = null)
        {
            socketManager.SendMessage(userId != null ? new[] { userId } : list.UserList.ToArray(),
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
            BroadcastPlayCurrentSong(list.CurrentPlaying, userId);
            BroadcastNextSongUpdated(list.NextSong, userId);
        }
    }
}
