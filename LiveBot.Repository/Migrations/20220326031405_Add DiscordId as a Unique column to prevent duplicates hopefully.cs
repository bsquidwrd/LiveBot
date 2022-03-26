using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveBot.Repository.Migrations
{
    public partial class AddDiscordIdasaUniquecolumntopreventduplicateshopefully : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "Unique_DiscordGuild_DiscordId",
                table: "DiscordGuild",
                column: "DiscordId"
            );
            migrationBuilder.AddUniqueConstraint(
                name: "Unique_DiscordChannel_DiscordId",
                table: "DiscordChannel",
                column: "DiscordId"
            );
            migrationBuilder.AddUniqueConstraint(
                name: "Unique_DiscordRole_DiscordId",
                table: "DiscordRole",
                column: "DiscordId"
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "Unique_DiscordGuild_DiscordId",
                table: "DiscordGuild"
            );
            migrationBuilder.DropUniqueConstraint(
                name: "Unique_DiscordChannel_DiscordId",
                table: "DiscordChannel"
            );
            migrationBuilder.DropUniqueConstraint(
                name: "Unique_DiscordRole_DiscordId",
                table: "DiscordRole"
            );
        }
    }
}