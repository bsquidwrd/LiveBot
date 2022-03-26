using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveBot.Repository.Migrations
{
    public partial class AddindextotimestampinStreamNotificationsoitwillbeeasiertoanalyze : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_StreamNotification_Timestamp",
                table: "StreamNotification",
                column: "TimeStamp"
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StreamNotification_Timestamp",
                table: "StreamNotification"
            );
        }
    }
}