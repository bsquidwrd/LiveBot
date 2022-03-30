using Microsoft.EntityFrameworkCore.Migrations;

namespace LiveBot.Repository.Migrations
{
    public partial class AddDiscordGuildIsInBetaandStreamSubscriptionIsFromRole : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFromRole",
                table: "StreamSubscription",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsInBeta",
                table: "DiscordGuild",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFromRole",
                table: "StreamSubscription");

            migrationBuilder.DropColumn(
                name: "IsInBeta",
                table: "DiscordGuild");
        }
    }
}