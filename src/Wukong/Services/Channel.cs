using System;
using System.Collections.Generic;
using System.Linq;
using Wukong.Helpers;
using Wukong.Models;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

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

        private bool _IsFinished = true;

        IDictionary<string, ClientSong> songMap = new Dictionary<string, ClientSong>();
        ISet<string> finishedUsers = new HashSet<string>();
        LinkedList<string> userList = new LinkedList<string>();

        LinkedListNode<string> nextUser = null;
        LinkedListNode<string> currentUser = null;
        ClientSong _NextSong = null;
        ClientSong _CurrentSong = null;
        DateTime StartTime = DateTime.Now;

        public ClientSong NextSong
        {
            private set
            {
                // Fixme: when next song is the same as current.
                if (_NextSong != value)
                {
                    _NextSong = value;
                    BroadcastNextSongUpdated();
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

        private ClientSong CurrentSong
        {
            set
            {
                if (value != null)
                {
                    IsFinished = false;
                }
                _CurrentSong = value;
                CleanStorage();
                BroadcastPlayCurrentSong();
            }
            get
            {
                return _CurrentSong;
            }
        }

        private bool IsFinished
        {
            set
            {
                if (_IsFinished == value) return;
                _IsFinished = value;
                if (value)
                {
                    // make sure currentuser is correctly set.
                    UpdateNextSong();
                    CurrentUser = nextUser;
                    CurrentSong = NextSong;
                    UpdateNextSong();
                }
                else
                {

                }
            }
            get
            {
                return _IsFinished;
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
                return finishedUsers.IsSupersetOf(userList);
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
            var user = userList.Find(userId);
            if (user == null) return;
            if (userList.Count == 1)
            {
                userList.Clear();
                nextUser = null;
                return;
            }
            songMap.Remove(userId);
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
                    songMap.Remove(userId);
                }
                else
                {
                    songMap[userId] = song;
                }
                UpdateNextSong();
            }
        }

        public void DownVote(string userId, ClientSong song)
        {
            if (song == null || CurrentSong == null || song.SiteId != CurrentSong.SiteId || song.SongId != CurrentSong.SongId)
                return;
            finishedUsers.Add(userId);
            CheckShouldForwardCurrentSong();
        }

        public bool HasUser(string userId)
        {
            return userList.Contains(userId);
        }

        private void CleanStorage()
        {
            StartTime = DateTime.Now;
            finishedUsers.Clear();
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
                if (!songMap.ContainsKey(nextUser.Value) || songMap[nextUser.Value] == null)
                {
                    nextUser = nextUser.NextOrFirst();
                    continue;
                }
                NextSong = songMap[nextUser.Value];
                return;
            }
            NextSong = null;
        }

        private void CheckShouldForwardCurrentSong()
        {
            var downVoteUserCount = finishedUsers.Intersect(userList).Count;
            var userCount = userList.Count;
            if (downVoteUserCount >= userCount * 0.5)
            {
                IsFinished = true;
            }
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