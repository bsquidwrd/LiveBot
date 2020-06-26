using Microsoft.EntityFrameworkCore.Migrations;

namespace LiveBot.Repository.Migrations
{
    public partial class CreateStreamNotificationLogMessage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LogMessage",
                table: "StreamNotification",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogMessage",
                table: "StreamNotification");
        }
    }
}
