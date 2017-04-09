using Microsoft.EntityFrameworkCore;

namespace Wukong.Models
{
    public class UserConfigurationData
    {
        public string UserId { get; set; }
        public string SyncPlaylists { get; set; }
        public string Cookies { get; set; }
    }

    public class UserConfigurationContext : DbContext
    {
        public DbSet<UserConfigurationData> UserConfiguration { get; set; }
        public UserConfigurationContext(DbContextOptions<UserConfigurationContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserConfigurationData>().HasKey(m => m.UserId);
            modelBuilder.Entity<UserConfigurationData>().Property(m => m.SyncPlaylists).HasMaxLength(10000);
            modelBuilder.Entity<UserConfigurationData>().Property(m => m.Cookies).HasMaxLength(10000);
            base.OnModelCreating(modelBuilder);
        }
    }
}
