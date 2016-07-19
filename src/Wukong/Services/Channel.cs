using System;
using System.Collections.Generic;
using System.Linq;
using Wukong.Helpers;
using Wukong.Models;

namespace Wukong.Services
{
    public delegate void NextSongUpdated(string channelId);
    public delegate void ShouldForwardCurrentSong(string channelId);
    public delegate void UserListUpdated(string channelId);

    public class Channel
    {
        string channelId;
        IDictionary<string, ClientSong> songMap = new Dictionary<string, ClientSong>();
        ISet<string> downVoteUsers = new HashSet<string>();
        ISet<string> finishedUsers = new HashSet<string>();
        LinkedList<string> userList = new LinkedList<string>();
        LinkedListNode<string> nextUser = null;
        LinkedListNode<string> currentUser = null;
        ClientSong nextSong = null;
        ClientSong currentSong = null;
        DateTime startTime = DateTime.Now;

        public event NextSongUpdated NextSongUpdated;
        public event ShouldForwardCurrentSong ShouldForwardCurrentSong;
        public event UserListUpdated UserListUpdated;

        public ClientSong NextSong
        {
            private set
            {
                if (nextSong != value)
                {
                    nextSong = value;
                    BroadcastNextSongUpdated();
                }
            }
            get
            {
                return nextSong;
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
                currentUser = value;
            }
        }

        public ClientSong CurrentSong
        {
            private set
            {
                currentSong = value;
            }
            get
            {
                return currentSong;
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
                return (DateTime.Now - startTime).TotalSeconds;
            }
        }

        public DateTime StartTime
        {
            set
            {
                startTime = value;
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

        public Channel(string id)
        {
            channelId = id;
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
            if (user == nextUser)
            {
                nextUser = nextUser.NextOrFirst();
            }
            userList.Remove(user);
            BroadcastUserListUpdated();
            UpdateNextSong();
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

        public void StartPlayingNextSong()
        {
            CurrentSong = NextSong;
            CurrentUser = nextUser;
            CleanStorage();
            UpdateNextSong();
        }

        public void DownVote(string userId)
        {
            downVoteUsers.Add(userId);
            CheckShouldForwardCurrentSong();
        }

        public void AddFinishedUser(string userId)
        {
            finishedUsers.Add(userId);
        }

        public bool HasUser(string userId)
        {
            return userList.Contains(userId);
        }

        private void CleanStorage()
        {
            downVoteUsers.Clear();
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
            var downVoteUserCount = downVoteUsers.Intersect(userList).Count;
            var userCount = userList.Count;
            if (downVoteUserCount >= userCount * 0.5)
            {
                BroadcastShouldForwardCurrentSong();
            }
        }

        private void BroadcastNextSongUpdated()
        {
            NextSongUpdated(Id);
        }

        private void BroadcastShouldForwardCurrentSong()
        {
            ShouldForwardCurrentSong(Id);
        }

        private void BroadcastUserListUpdated()
        {
            UserListUpdated(Id);
        }
    }
}