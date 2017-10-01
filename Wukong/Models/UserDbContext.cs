using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Collections.Generic;

namespace Wukong.Models
{
    public class UserConfigurationData
    {
        public string FromSite { get; set; }
        public string UserId { get; set; }
        public string SyncPlaylists { get; set; }
        public string Cookies { get; set; }
    }

    public class UserDbContext : DbContext
    {
        public DbSet<UserConfigurationData> UserConfiguration { get; set; }

        public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserConfigurationData>().HasKey(m => new { m.FromSite, m.UserId });
            modelBuilder.Entity<UserConfigurationData>().Property(m => m.SyncPlaylists).HasMaxLength(10000);
            modelBuilder.Entity<UserConfigurationData>().Property(m => m.Cookies).HasMaxLength(10000);

            base.OnModelCreating(modelBuilder);
        }


    }
}