using Microsoft.EntityFrameworkCore.Migrations;

namespace LiveBot.Repository.Migrations
{
    public partial class AddDiscordGuildtoStreamSubscription : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiscordGuildId",
                table: "StreamSubscription",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StreamSubscription_DiscordGuildId",
                table: "StreamSubscription",
                column: "DiscordGuildId");

            migrationBuilder.AddForeignKey(
                name: "FK_StreamSubscription_DiscordGuild_DiscordGuildId",
                table: "StreamSubscription",
                column: "DiscordGuildId",
                principalTable: "DiscordGuild",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StreamSubscription_DiscordGuild_DiscordGuildId",
                table: "StreamSubscription");

            migrationBuilder.DropIndex(
                name: "IX_StreamSubscription_DiscordGuildId",
                table: "StreamSubscription");

            migrationBuilder.DropColumn(
                name: "DiscordGuildId",
                table: "StreamSubscription");
        }
    }
}