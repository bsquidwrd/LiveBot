using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using System;

namespace LiveBot.Repository.Migrations
{
    public partial class CreateDiscordRoleandStreamSubscription : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StreamSubscription",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TimeStamp = table.Column<DateTime>(nullable: false),
                    Deleted = table.Column<bool>(nullable: false),
                    ServiceType = table.Column<int>(nullable: false),
                    GuildId = table.Column<int>(nullable: true),
                    ChannelId = table.Column<int>(nullable: true),
                    RoleId = table.Column<int>(nullable: true),
                    SourceID = table.Column<string>(nullable: true),
                    Message = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreamSubscription", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StreamSubscription_DiscordChannel_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "DiscordChannel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StreamSubscription_DiscordGuild_GuildId",
                        column: x => x.GuildId,
                        principalTable: "DiscordGuild",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StreamSubscription_DiscordRole_RoleId",
                        column: x => x.RoleId,
                        principalTable: "DiscordRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StreamSubscription_ChannelId",
                table: "StreamSubscription",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_StreamSubscription_GuildId",
                table: "StreamSubscription",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_StreamSubscription_RoleId",
                table: "StreamSubscription",
                column: "RoleId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StreamSubscription");
        }
    }
}