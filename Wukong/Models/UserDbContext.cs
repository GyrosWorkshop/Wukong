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

    public class UserConfigurationData
    {
        public string UserId { get; set; }
        public string SyncPlaylists { get; set; }
        public string Cookies { get; set; }
    }

    public class UserDbContext : DbContext
    {
        public DbSet<UserSongListData> UserSongList { get; set; }
        public DbSet<SongListData> SongList { get; set; }
        public DbSet<ClientSongData> Song;
        public DbSet<UserConfigurationData> UserConfiguration { get; set; }

        public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }

        public DbSet<UserSongListData> DataEventRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserSongListData>().HasKey(m => m.UserId);
            modelBuilder.Entity<UserSongListData>()
                .HasMany(m => m.SongList)
                .WithOne(m => m.UserSongList)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClientSongData>().HasKey(m => new { m.SongId, m.SiteId, m.SongListId });

            modelBuilder.Entity<SongListData>().HasKey(m => m.Id);
            modelBuilder.Entity<SongListData>()
                .HasMany(m => m.Song)
                .WithOne(m => m.SongList)
                .HasForeignKey(m => m.SongListId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserConfigurationData>().HasKey(m => m.UserId);
            modelBuilder.Entity<UserConfigurationData>().Property(m => m.SyncPlaylists).HasMaxLength(10000);
            modelBuilder.Entity<UserConfigurationData>().Property(m => m.Cookies).HasMaxLength(10000);

            base.OnModelCreating(modelBuilder);
        }


    }
}