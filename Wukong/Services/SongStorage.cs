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
        /// Will invoke listener once setup.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="listener"></param>
        void AddListener(string userId, SongStorage.UserSongChanged listener);
        void RemoveListener(string userId);
        void SetSong(string userId, ClientSong song);
        void MarkCurrent(string userId);
    }
    public class SongStorage: ISongStorage
    {
        public delegate void UserSongChanged(string userId, ClientSong song);

        private readonly ConcurrentDictionary<string, ClientSong> _songs =
            new ConcurrentDictionary<string, ClientSong>();

        private readonly ConcurrentDictionary<string, UserSongChanged> _listeners =
            new ConcurrentDictionary<string, UserSongChanged>();

        public void AddListener(string userId, UserSongChanged listener)
        {
            _listeners[userId] = listener;
            if (_songs.TryGetValue(userId, out var song))
            {
                listener?.Invoke(userId, song);
            }
        }

        public void RemoveListener(string userId)
        {
            _listeners.TryRemove(userId, out UserSongChanged _);
        }

        public void SetSong(string userId, ClientSong song)
        {
            _songs[userId] = song;
            _listeners[userId]?.Invoke(userId, song);
        }

        public void MarkCurrent(string userId)
        {
            _songs.TryRemove(userId, out ClientSong _);
            _listeners[userId]?.Invoke(userId, null);
        }
    }
}
