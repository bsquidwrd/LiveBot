using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace LiveBot.Repository.Migrations
{
    public partial class AllownullfieldsforStreamNotification : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Stream_StartTime",
                table: "StreamNotification",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscordRole_DiscordId",
                table: "StreamNotification",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscordMessage_DiscordId",
                table: "StreamNotification",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscordGuild_DiscordId",
                table: "StreamNotification",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscordChannel_DiscordId",
                table: "StreamNotification",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Stream_StartTime",
                table: "StreamNotification",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscordRole_DiscordId",
                table: "StreamNotification",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscordMessage_DiscordId",
                table: "StreamNotification",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscordGuild_DiscordId",
                table: "StreamNotification",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscordChannel_DiscordId",
                table: "StreamNotification",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldNullable: true);
        }
    }
}