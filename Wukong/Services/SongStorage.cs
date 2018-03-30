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
        private readonly object _lock = new object();

        public delegate void UserSongChanged(string userId, ClientSong song);

        private readonly Dictionary<string, ClientSong> _pendingSongs =
            new Dictionary<string, ClientSong>();

        private readonly Dictionary<string, ClientSong> _currentSongs =
            new Dictionary<string, ClientSong>();

        private readonly Dictionary<string, UserSongChanged> _listeners =
            new Dictionary<string, UserSongChanged>();

        public void AddListener(string userId, UserSongChanged listener)
        {
            lock (_lock)
            {
                _listeners[userId] = listener;
                if (_pendingSongs.TryGetValue(userId, out var song))
                {
                    listener?.Invoke(userId, song);
                    MarkCurrent(userId);
                }
            }
        }

        public void RemoveListener(string userId)
        {
            lock (_lock)
            {
                _listeners.Remove(userId);
                _pendingSongs.Remove(userId);
            }
        }

        public void SetSong(string userId, ClientSong song)
        {
            lock (_lock)
            {
                _pendingSongs[userId] = song;
                if (!_pendingSongs.ContainsKey(userId))
                {
                    // No song is playing, emit song now.
                    EmitSong(userId);
                }
            }
        }

        private void EmitSong(string userId)
        {
            ClientSong nextSong;
            _pendingSongs.TryGetValue(userId, out nextSong);
        }

        public void MarkCurrent(string userId)
        {
            _pendingSongs.Remove(userId);
            EmitSong(userId);
        }
    }
}
