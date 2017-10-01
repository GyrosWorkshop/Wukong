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
                    FromSite = table.Column<string>(nullable: false),
                    UserId = table.Column<string>(nullable: false),
                    Cookies = table.Column<string>(maxLength: 10000, nullable: true),
                    SyncPlaylists = table.Column<string>(maxLength: 10000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConfiguration", x => new {x.FromSite, x.UserId});
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserConfiguration");
        }
    }
}
