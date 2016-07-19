using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Collections.Generic;

namespace Wukong.Models
{
    public class SongListData
    {
        public string Name { get; set; }
        public List<ClientSongData> Song { get; set; } = new List<ClientSongData>();
        public long Id { get; set; }

        public string UserId { get; set; }
        public virtual UserSongListData UserSongList { get; set; }
    }
    public class UserSongListData
    {
        public string UserId { get; set; }
        public List<SongListData> SongList { get; set; } = new List<SongListData>();
    }

    public class UserSongListContext : DbContext
    {
        public DbSet<UserSongListData> UserSongList { get; set; }
        public DbSet<SongListData> SongList { get; set; }
        public DbSet<ClientSongData> Song;
        public UserSongListContext(DbContextOptions<UserSongListContext> options) : base(options) { }

        public DbSet<UserSongListData> DataEventRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<UserSongListData>().HasKey(m => m.UserId);
            builder.Entity<UserSongListData>()
                .HasMany(m => m.SongList)
                .WithOne(m => m.UserSongList)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ClientSongData>().HasKey(m => new { m.SongId, m.SiteId, m.SongListId });

            builder.Entity<SongListData>().HasKey(m => m.Id);
            builder.Entity<SongListData>()
                .HasMany(m => m.Song)
                .WithOne(m => m.SongList)
                .HasForeignKey(m => m.SongListId)
                .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(builder);
        }


    }
}