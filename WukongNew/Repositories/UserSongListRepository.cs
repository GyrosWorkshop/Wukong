using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

using Wukong.Models;

namespace Wukong.Repositories
{
    public interface IUserSongListRepository
    {
        Task<ClientSongList> GetAsync(string userId, long songListId);
        Task<bool> UpdateAsync(string userId, long songListId, ClientSongList info);
        Task<long> AddAsync(string userId, ClientSongList info);
        Task RemoveAsync(string userId, long songListId);
        Task<IList<SongListData>> ListAsync(string userId);
    }

    public class UserSongListRepository : IUserSongListRepository
    {
        private readonly UserSongListContext context;

        public UserSongListRepository(UserSongListContext context)
        {
            this.context = context;
        }

        private async Task<UserSongListData> GetUserSongList(string userId)
        {
            return await context.UserSongList
                .Include(usl => usl.SongList)
                .FirstOrDefaultAsync(t => t.UserId == userId) ??
                context.UserSongList.Add(new UserSongListData
                {
                    UserId = userId
                }).Entity;
        }

        public async Task<IList<SongListData>> ListAsync(string userId)
        {
            var list = await context.UserSongList
                .Include(it => it.SongList)
                .FirstOrDefaultAsync(it => it.UserId == userId);
            return list?.SongList;
        }

        public async Task<ClientSongList> GetAsync(string userId, long songListId)
        {
            var songList = await context.SongList
                .Include(sl => sl.Song)
                .FirstOrDefaultAsync(t => t.Id == songListId && t.UserId == userId);

            // There should be a better way to write this.
            if (songList == null)
            {
                return null;
            }
            return new ClientSongList
            {
                Name = songList.Name,

                // We dont want to pass relationship back to client.
                Song = songList.Song.Select(song => new ClientSong
                {
                    SongId = song.SongId,
                    SiteId = song.SiteId,
                }).ToList(),
            };
        }

        public async Task<bool> UpdateAsync(string userId, long songListId, ClientSongList info)
        {
            var songList = await context.SongList
                .Include(sl => sl.Song)
                .FirstOrDefaultAsync(t => t.Id == songListId && t.UserId == userId);
            if (songList == null)
            {
                return false;
            }
            var songData = info.Song.Select(m => new ClientSongData(m)).ToList();
            Func<ClientSongData, List<ClientSongData>, bool> inList = (ClientSongData a, List<ClientSongData> b) =>
            {
                return b.Any(song => song.SongId == a.SongId && song.SiteId == song.SiteId);
            };
            var intersection = songList.Song.Where(song => inList(song, songData)).ToList();
            songList.Song.RemoveAll(song => !inList(song, intersection));
            songData.RemoveAll(song => inList(song, intersection));
            songList.Song.AddRange(songData);
            songList.Name = info.Name;
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<long> AddAsync(string userId, ClientSongList info)
        {
            var userSongList = await GetUserSongList(userId);

            var songList = new SongListData
            {
                Name = info.Name,
                Song = info.Song.Select(song => new ClientSongData(song)).ToList(),
            };
            userSongList.SongList.Add(songList);
            await context.SaveChangesAsync();
            return songList.Id;
        }

        public async Task RemoveAsync(string userId, long songListId)
        {
            var userSongList = await GetUserSongList(userId);
            userSongList.SongList.RemoveAll(t => t.Id == songListId);
            await context.SaveChangesAsync();
        }
    }
}