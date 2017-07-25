using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Wukong.Helpers;
using Wukong.Models;

namespace Wukong.Services
{
    public class ChannelUserList
    {
        private static readonly Mutex UserListMutex = new Mutex();

        public delegate void UserListChangedHandler(bool add, string userId);

        public event UserListChangedHandler UserChanged;

        public delegate void CurrentChangedHandler(UserSong userSong);

        public event CurrentChangedHandler CurrentChanged;

        public delegate void NextSongChangedHandler(ClientSong song);

        public event NextSongChangedHandler NextChanged;


        private readonly LinkedList<UserSong> _list = new LinkedList<UserSong>();

        private LinkedListNode<UserSong> _current;
        private LinkedListNode<UserSong> Current
        {
            get => _current;
            set
            {
                _current = value;
                CurrentPlayingChanged();
            }
        }

        public UserSong CurrentPlaying { get; private set; }

        public bool IsPlaying => CurrentPlaying?.Song != null;

        public bool Empty => _list.Count == 0;

        public List<string> UserList
        {
            get
            {
                UserListMutex.WaitOne();
                var userList = _list.Select(it => it.UserId).ToList();
                UserListMutex.ReleaseMutex();
                return userList;
            }
        }

        public ClientSong NextSong { get; private set; }

        private void RefreshNextSong()
        {
            var next = FindNext();
            if (NextSong != next?.Value?.Song)
            {
                NextSong = next?.Value?.Song;
                NextChanged?.Invoke(NextSong);
            }
        }

        private void CurrentPlayingChanged()
        {
            var playing = Current?.Value?.Clone();
            if (CurrentPlaying != playing)
            {
                CurrentPlaying = playing;
                CurrentChanged?.Invoke(CurrentPlaying);
            }
            RefreshNextSong();
        }

        public void AddUser(string userId)
        {
            var changed = false;
            UserListMutex.WaitOne();
            if (_list.FirstOrDefault(it => it.UserId == userId) == null)
            {
                _list.AddLast(new UserSong(userId));
                changed = true;
            }
            UserListMutex.ReleaseMutex();
            if (changed) UserChanged?.Invoke(true, userId);
            if (!IsPlaying) GoNext();
            else RefreshNextSong();
        }

        public void RemoveUser(string userId)
        {
            //TODO: removing current playing user may cause nextUser loopback to first.
            var changed = false;
            UserListMutex.WaitOne();
            var userSong = _list.FirstOrDefault(it => it.UserId == userId);
            if (userSong != null)
            {
                _list.Remove(userSong);
                changed = true;
            }
            UserListMutex.ReleaseMutex();
            if (changed) UserChanged?.Invoke(false, userId);
            RefreshNextSong();
            if (Empty)
            {
                Current = null;
            }
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

        private LinkedListNode<UserSong> FindNext()
        {
            var next = Current.NextOrFirst(_list);
            for (var i = 0; i != _list.Count; ++i)
            {
                var song = next?.Value?.Song;
                if (song != null)
                {
                    return next;
                }
                next = next?.NextOrFirst(_list);
            }
            return null;
        }

        public void GoNext()
        {
            Current = FindNext();
        }

        private UserSong UserSong(string userId)
        {
            UserListMutex.WaitOne();
            var userSong = _list.FirstOrDefault(it => it.UserId == userId);
            UserListMutex.ReleaseMutex();
            return userSong;
        }
    }
}