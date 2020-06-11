using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace LiveBot.Repository.Migrations
{
    public partial class RemovenullallowforsomeStreamNotificationfields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Stream_StartTime",
                table: "StreamNotification",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscordGuild_DiscordId",
                table: "StreamNotification",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscordChannel_DiscordId",
                table: "StreamNotification",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Stream_StartTime",
                table: "StreamNotification",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime));

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscordGuild_DiscordId",
                table: "StreamNotification",
                type: "numeric(20,0)",
                nullable: true,
                oldClrType: typeof(decimal));

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscordChannel_DiscordId",
                table: "StreamNotification",
                type: "numeric(20,0)",
                nullable: true,
                oldClrType: typeof(decimal));
        }
    }
}