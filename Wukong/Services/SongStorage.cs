using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wukong.Models;

namespace Wukong.Services
{
    public interface ISongStorage
    {
        /// <summary>
        /// Add song update listener for specific user.
        /// Will call listener once listener is setup.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="listener"></param>
        void AddListener(User user, SongStorage.UserSongChanged listener);
        void RemoveListener(User user);
        void SetSong(User user, ClientSong song);
        void MarkCurrent(User user);
        ClientSong GetCurrent(User user);
    }
    public class SongStorage: ISongStorage
    {
        public delegate void UserSongChanged(User user, ClientSong song);

        private readonly ConcurrentDictionary<string, ClientSong> _songs =
            new ConcurrentDictionary<string, ClientSong>();

        private readonly ConcurrentDictionary<string, UserSongChanged> _listeners =
            new ConcurrentDictionary<string, UserSongChanged>();

        public void AddListener(User user, UserSongChanged listener)
        {
            _listeners[user.Id] = listener;
            listener?.Invoke(user, _songs[user.Id]);
        }

        public void RemoveListener(User user)
        {
            _listeners.TryRemove(user.Id, out UserSongChanged _);
        }

        public void SetSong(User user, ClientSong song)
        {
            _songs[user.Id] = song;
            _listeners[user.Id]?.Invoke(user, song);
        }

        public void MarkCurrent(User user)
        {
            _songs.TryRemove(user.Id, out ClientSong _);
        }

        public ClientSong GetCurrent(User user)
        {
            return _songs[user.Id];
        }
    }
}
