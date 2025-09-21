using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace LiveBot.Repository.Migrations
{
    public partial class AddStreamSubscriptionandStreamuser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StreamUser",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TimeStamp = table.Column<DateTime>(nullable: false),
                    Deleted = table.Column<bool>(nullable: false),
                    ServiceType = table.Column<int>(nullable: false),
                    SourceID = table.Column<string>(nullable: true),
                    Username = table.Column<string>(nullable: true),
                    DisplayName = table.Column<string>(nullable: true),
                    AvatarURL = table.Column<string>(nullable: true),
                    ProfileURL = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreamUser", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StreamSubscription",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TimeStamp = table.Column<DateTime>(nullable: false),
                    Deleted = table.Column<bool>(nullable: false),
                    UserId = table.Column<int>(nullable: true),
                    DiscordChannelId = table.Column<int>(nullable: true),
                    DiscordRoleId = table.Column<int>(nullable: true),
                    Message = table.Column<string>(nullable: true),
                    DiscordGuildId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreamSubscription", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StreamSubscription_DiscordChannel_DiscordChannelId",
                        column: x => x.DiscordChannelId,
                        principalTable: "DiscordChannel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StreamSubscription_DiscordGuild_DiscordGuildId",
                        column: x => x.DiscordGuildId,
                        principalTable: "DiscordGuild",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StreamSubscription_DiscordRole_DiscordRoleId",
                        column: x => x.DiscordRoleId,
                        principalTable: "DiscordRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StreamSubscription_StreamUser_UserId",
                        column: x => x.UserId,
                        principalTable: "StreamUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StreamSubscription_DiscordChannelId",
                table: "StreamSubscription",
                column: "DiscordChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_StreamSubscription_DiscordGuildId",
                table: "StreamSubscription",
                column: "DiscordGuildId");

            migrationBuilder.CreateIndex(
                name: "IX_StreamSubscription_DiscordRoleId",
                table: "StreamSubscription",
                column: "DiscordRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_StreamSubscription_UserId",
                table: "StreamSubscription",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StreamSubscription");

            migrationBuilder.DropTable(
                name: "StreamUser");
        }
    }
}