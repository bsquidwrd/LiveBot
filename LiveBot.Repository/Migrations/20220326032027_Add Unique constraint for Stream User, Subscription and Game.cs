using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveBot.Repository.Migrations
{
    public partial class AddUniqueconstraintforStreamUserSubscriptionandGame : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "Unique_StreamUser_ServiceTypeSourcedId",
                table: "StreamUser",
                columns: new[] { "ServiceType", "SourceID" }
            );
            migrationBuilder.AddUniqueConstraint(
                name: "Unique_StreamGame_ServiceTypeSourcedId",
                table: "StreamGame",
                columns: new[] { "ServiceType", "SourceId" }
            );

            migrationBuilder.AddUniqueConstraint(
                name: "Unique_StreamSubscription_UserIdDiscordGuildId",
                table: "StreamSubscription",
                columns: new[] { "UserId", "DiscordGuildId" }
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "Unique_StreamUser_ServiceTypeSourcedId",
                table: "StreamUser"
            );
            migrationBuilder.DropUniqueConstraint(
                name: "Unique_StreamGame_ServiceTypeSourcedId",
                table: "StreamGame"
            );

            migrationBuilder.DropUniqueConstraint(
                name: "Unique_StreamSubscription_UserIdDiscordGuildId",
                table: "StreamSubscription"
            );
        }
    }
}