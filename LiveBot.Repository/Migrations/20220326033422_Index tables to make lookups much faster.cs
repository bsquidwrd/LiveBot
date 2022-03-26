using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveBot.Repository.Migrations
{
    public partial class Indextablestomakelookupsmuchfaster : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_NotificationLookup",
                table: "StreamNotification",
                columns: new[] { "User_SourceID", "Stream_SourceID", "Stream_StartTime", "DiscordGuild_DiscordId", "DiscordChannel_DiscordId", "Game_SourceID" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_PreviousNotificationLookup",
                table: "StreamNotification",
                columns: new[] { "User_SourceID", "DiscordGuild_DiscordId", "DiscordChannel_DiscordId" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_MonitorAuth",
                table: "MonitorAuth",
                columns: new[] { "ServiceType", "ClientId", "Expired" }
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotificationLookup",
                table: "StreamNotification"
            );

            migrationBuilder.DropIndex(
                name: "IX_PreviousNotificationLookup",
                table: "StreamNotification"
            );

            migrationBuilder.DropIndex(
                name: "IX_MonitorAuth",
                table: "MonitorAuth"
            );
        }
    }
}