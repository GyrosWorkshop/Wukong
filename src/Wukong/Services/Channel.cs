using System;
using System.Collections.Generic;
using System.Linq;
using Wukong.Helpers;
using Wukong.Models;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace Wukong.Services
{
    public interface IChannelServiceFactory
    {
        Channel GetChannel(string channelId);
    }

    public class ChannelServiceFactory : IChannelServiceFactory
    {
        private readonly ILogger Logger;
        private readonly ISocketManager SocketManager;
        private readonly IProvider Provider;

        public ChannelServiceFactory(ILoggerFactory loggerFactory, ISocketManager socketManager, IProvider provider)
        {
            Logger = loggerFactory.CreateLogger("ChannelServiceFactory");
            SocketManager = socketManager;
            Provider = provider;
        }

        public Channel GetChannel(string channelId)
        {
            return Storage.Instance.GetChannel(channelId) ?? Storage.Instance.CreateChannel(channelId, SocketManager, Provider);
        }
    }

    public class Channel
    {
        private readonly string channelId;
        private readonly ISocketManager SocketManager;
        private readonly IProvider Provider;


        IDictionary<string, ClientSong> SongMap = new Dictionary<string, ClientSong>();
        ISet<string> FinishedUsers = new HashSet<string>();
        ISet<string> DownvoteUsers = new HashSet<string>();
        LinkedList<string> userList = new LinkedList<string>();

        LinkedListNode<string> nextUser = null;
        LinkedListNode<string> currentUser = null;
        ClientSong _NextSong = null;
        ClientSong CurrentSong = null;
        DateTime StartTime = DateTime.Now;
        private bool IsFinished = true;
        private Timer FinishTimeoutTimer = null;

        public ClientSong NextSong
        {
            private set
            {
                if (_NextSong != value)
                {
                    _NextSong = value;
                    BroadcastNextSongUpdated();
                    if (IsFinished)
                    {
                        // only happens when first song to play.
                        CurrentSong = value;
                        StartPlayingCurrent();
                    }
                }
            }
            get
            {
                return _NextSong;
            }
        }

        private LinkedListNode<string> CurrentUser
        {
            get
            {
                return currentUser ?? userList.First;
            }

            set
            {
                if (CurrentUser != value)
                {
                    currentUser = value;
                }
            }
        }

        public List<string> UserList
        {
            get
            {
                // WTF.
                return userList.Select(i => i).ToList();
            }
        }

        public string CurrentUserId
        {
            get
            {
                return CurrentUser?.Value;
            }
        }

        public double Elapsed
        {
            get
            {
                return (DateTime.Now - StartTime).TotalSeconds;
            }
        }

        public bool IsIdle
        {
            get
            {
                return FinishedUsers.IsSupersetOf(userList);
            }
        }

        public string Id
        {
            get
            {
                return channelId;
            }
        }

        public Channel(string id, ISocketManager socketManager, IProvider provider)
        {
            channelId = id;
            SocketManager = socketManager;
            Provider = provider;
        }

        public void Join(string userId)
        {
            if (!userList.Contains(userId))
            {
                userList.AddLast(userId);
                BroadcastUserListUpdated();
                UpdateNextSong();
            }
        }

        public void Leave(string userId)
        {
            // Fixme: remove channel when no people in.
            var user = userList.Find(userId);
            if (user == null) return;
            if (userList.Count == 1)
            {
                userList.Clear();
                nextUser = null;
                return;
            }
            SongMap.Remove(userId);
            if (user == CurrentUser)
            {
                CurrentUser = CurrentUser.NextOrFirst();
            }
            userList.Remove(user);
            BroadcastUserListUpdated();
            UpdateNextSong();
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
            if (userList.Contains(userId))
            {
                if (song == null)
                {
                    SongMap.Remove(userId);
                }
                else
                {
                    SongMap[userId] = song;
                }
                UpdateNextSong();
            }
        }

        public void ReportFinish(string userId, ClientSong song, bool force = false)
        {
            if (song == null || CurrentSong == null || song.SiteId != CurrentSong.SiteId || song.SongId != CurrentSong.SongId)
                return;
            if (force) DownvoteUsers.Add(userId);
            else FinishedUsers.Add(userId);
            CheckShouldForwardCurrentSong();
        }

        public bool HasUser(string userId)
        {
            return userList.Contains(userId);
        }

        private void StartPlayingCurrent()
        {
            StartTime = DateTime.Now;
            FinishedUsers.Clear();
            DownvoteUsers.Clear();
            BroadcastPlayCurrentSong();
            IsFinished = CurrentSong == null;
        }

        private void UpdateNextSong()
        {
            nextUser = CurrentUser.NextOrFirst();
            if (nextUser == null)
            {
                NextSong = null;
                return;
            }
            for (int i = 0; i < userList.Count; i++)
            {
                if (!SongMap.ContainsKey(nextUser.Value) || SongMap[nextUser.Value] == null)
                {
                    nextUser = nextUser.NextOrFirst();
                    continue;
                }
                NextSong = SongMap[nextUser.Value];
                return;
            }
            NextSong = null;
        }

        private void CheckShouldForwardCurrentSong()
        {
            var downVoteUserCount = DownvoteUsers.Intersect(userList).Count;
            var undeterminedCount = UserList.Except(DownvoteUsers).Except(FinishedUsers).Count();
            var connectedUserCount = UserList.Select(it => SocketManager.IsConnected(it)).Count();
            if (downVoteUserCount > QueryForceForwardCount(connectedUserCount))
            {
                ShouldForwardNow();
            }
            else if (undeterminedCount < connectedUserCount * 0.5)
            {
                if (FinishTimeoutTimer != null) return;
                FinishTimeoutTimer = new Timer(ShouldForwardNow, null, 10 * 1000, Timeout.Infinite);
            }
        }

        private int QueryForceForwardCount(int total)
        {
            switch (total)
            {
                case 0:
                case 1:
                case 2:
                    return 1;
                case 3:
                    return 2;
                default:
                    return Convert.ToInt32(Math.Ceiling(((double)total) / 2));
            }
        }

        private void ShouldForwardNow(object state = null)
        {
            FinishTimeoutTimer?.Dispose();
            FinishTimeoutTimer = null;
            IsFinished = true;
            CurrentUser = nextUser;
            CurrentSong = NextSong;
            StartPlayingCurrent();
            UpdateNextSong();
        }

        private void BroadcastUserListUpdated(string userId = null)
        {
            SocketManager.SendMessage(userId != null ? new List<string> { userId } : UserList,
                new UserListUpdated
                {
                    Users = UserList.Select(it => Storage.Instance.GetUser(it)).ToList()
                });
        }

        private async void BroadcastNextSongUpdated(string userId = null)
        {
            SocketManager.SendMessage(userId != null ? new List<string> { userId } : UserList,
                new NextSongUpdated
                {
                    Song = await Provider.GetSong(NextSong, true)
                });
        }

        private async void BroadcastPlayCurrentSong(string userId = null)
        {
            var song = await Provider.GetSong(CurrentSong, true);
            SocketManager.SendMessage(userId != null ? new List<string> { userId } : UserList
                , new Play
                {
                    Song = song,
                    Elapsed = Elapsed,
                    User = CurrentUserId
                });
        }

        private void EmitChannelInfo(string userId)
        {
            BroadcastPlayCurrentSong(userId);
            BroadcastUserListUpdated(userId);
        }
    }
}