using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Wukong.Models;

namespace Wukong.Migrations
{
    [DbContext(typeof(UserDbContext))]
    partial class UserDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.1");

            modelBuilder.Entity("Wukong.Models.UserConfigurationData", b =>
            {
                    b.Property<string>("FromSite")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("UserId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Cookies")
                        .HasMaxLength(10000);

                    b.Property<string>("SyncPlaylists")
                        .HasMaxLength(10000);

                    b.HasKey("FromSite", "UserId");

                    b.ToTable("UserConfiguration");
                });
        }
    }
}
