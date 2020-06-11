using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using System;

namespace LiveBot.Repository.Migrations
{
    public partial class StreamNotificationmodel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StreamNotification",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TimeStamp = table.Column<DateTime>(nullable: false),
                    Deleted = table.Column<bool>(nullable: false),
                    ServiceType = table.Column<int>(nullable: false),
                    Success = table.Column<bool>(nullable: false),
                    Message = table.Column<string>(nullable: true),
                    User_SourceID = table.Column<string>(nullable: true),
                    User_Username = table.Column<string>(nullable: true),
                    User_DisplayName = table.Column<string>(nullable: true),
                    User_AvatarURL = table.Column<string>(nullable: true),
                    User_ProfileURL = table.Column<string>(nullable: true),
                    Stream_SourceID = table.Column<string>(nullable: true),
                    Stream_Title = table.Column<string>(nullable: true),
                    Stream_StartTime = table.Column<DateTime>(nullable: false),
                    Stream_ThumbnailURL = table.Column<string>(nullable: true),
                    Stream_StreamURL = table.Column<string>(nullable: true),
                    Game_SourceID = table.Column<string>(nullable: true),
                    Game_Name = table.Column<string>(nullable: true),
                    Game_ThumbnailURL = table.Column<string>(nullable: true),
                    DiscordGuild_DiscordId = table.Column<decimal>(nullable: false),
                    DiscordGuild_Name = table.Column<string>(nullable: true),
                    DiscordChannel_DiscordId = table.Column<decimal>(nullable: false),
                    DiscordChannel_Name = table.Column<string>(nullable: true),
                    DiscordRole_DiscordId = table.Column<decimal>(nullable: false),
                    DiscordRole_Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreamNotification", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StreamNotification");
        }
    }
}