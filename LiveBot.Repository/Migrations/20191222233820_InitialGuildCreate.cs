using Microsoft.EntityFrameworkCore.Migrations;

namespace LiveBot.Repository.Migrations
{
    public partial class InitialGuildCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Guild",
                columns: table => new
                {
                    GuildID = table.Column<decimal>(nullable: false),
                    GuildName = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guild", x => x.GuildID);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Guild");
        }
    }
}
