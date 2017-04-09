using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Wukong.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserConfiguration",
                columns: table => new
                {
                    UserId = table.Column<string>(nullable: false),
                    Cookies = table.Column<string>(maxLength: 10000, nullable: true),
                    SyncPlaylists = table.Column<string>(maxLength: 10000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConfiguration", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "UserSongListData",
                columns: table => new
                {
                    UserId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSongListData", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "SongList",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: true),
                    UserId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SongList", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SongList_UserSongListData_UserId",
                        column: x => x.UserId,
                        principalTable: "UserSongListData",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientSongData",
                columns: table => new
                {
                    SongId = table.Column<string>(nullable: false),
                    SiteId = table.Column<string>(nullable: false),
                    SongListId = table.Column<long>(nullable: false),
                    WithCookie = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientSongData", x => new { x.SongId, x.SiteId, x.SongListId });
                    table.ForeignKey(
                        name: "FK_ClientSongData_SongList_SongListId",
                        column: x => x.SongListId,
                        principalTable: "SongList",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientSongData_SongListId",
                table: "ClientSongData",
                column: "SongListId");

            migrationBuilder.CreateIndex(
                name: "IX_SongList_UserId",
                table: "SongList",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientSongData");

            migrationBuilder.DropTable(
                name: "UserConfiguration");

            migrationBuilder.DropTable(
                name: "SongList");

            migrationBuilder.DropTable(
                name: "UserSongListData");
        }
    }
}
