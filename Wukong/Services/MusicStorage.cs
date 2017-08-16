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
        ClientSong GetSongAndMarkUsed(User user);
        void SetNextSong(User user, ClientSong song);
    }
    public class SongStorage: ISongStorage
    {
        private readonly ConcurrentDictionary<string, ClientSong> _songs = new ConcurrentDictionary<string, ClientSong>();

        public ClientSong GetSongAndMarkUsed(User user)
        {
            _songs.TryRemove(user.Id, out ClientSong song);
            return song;
        }

        public void SetNextSong(User user, ClientSong song)
        {
            _songs.AddOrUpdate(user.Id, song, (s, _) => song);
        }
    }
}
