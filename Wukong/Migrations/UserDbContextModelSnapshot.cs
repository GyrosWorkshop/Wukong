using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
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

            modelBuilder.Entity("Wukong.Models.ClientSongData", b =>
                {
                    b.Property<string>("SongId");

                    b.Property<string>("SiteId");

                    b.Property<long>("SongListId");

                    b.Property<string>("WithCookie");

                    b.HasKey("SongId", "SiteId", "SongListId");

                    b.HasIndex("SongListId");

                    b.ToTable("ClientSongData");
                });

            modelBuilder.Entity("Wukong.Models.SongListData", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name");

                    b.Property<string>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("SongList");
                });

            modelBuilder.Entity("Wukong.Models.UserConfigurationData", b =>
                {
                    b.Property<string>("UserId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Cookies")
                        .HasMaxLength(10000);

                    b.Property<string>("SyncPlaylists")
                        .HasMaxLength(10000);

                    b.HasKey("UserId");

                    b.ToTable("UserConfiguration");
                });

            modelBuilder.Entity("Wukong.Models.UserSongListData", b =>
                {
                    b.Property<string>("UserId")
                        .ValueGeneratedOnAdd();

                    b.HasKey("UserId");

                    b.ToTable("UserSongListData");
                });

            modelBuilder.Entity("Wukong.Models.ClientSongData", b =>
                {
                    b.HasOne("Wukong.Models.SongListData", "SongList")
                        .WithMany("Song")
                        .HasForeignKey("SongListId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Wukong.Models.SongListData", b =>
                {
                    b.HasOne("Wukong.Models.UserSongListData", "UserSongList")
                        .WithMany("SongList")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
        }
    }
}
