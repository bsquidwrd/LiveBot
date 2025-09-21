using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace LiveBot.Repository.Migrations
{
    public partial class AddDiscordGuildConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscordGuildConfig",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TimeStamp = table.Column<DateTime>(nullable: false),
                    Deleted = table.Column<bool>(nullable: false),
                    Message = table.Column<string>(nullable: true),
                    DiscordGuildId = table.Column<long>(nullable: false),
                    DiscordChannelId = table.Column<long>(nullable: true),
                    DiscordRoleId = table.Column<long>(nullable: true),
                    MonitorRoleId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordGuildConfig", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscordGuildConfig_DiscordChannel_DiscordChannelId",
                        column: x => x.DiscordChannelId,
                        principalTable: "DiscordChannel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiscordGuildConfig_DiscordGuild_DiscordGuildId",
                        column: x => x.DiscordGuildId,
                        principalTable: "DiscordGuild",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiscordGuildConfig_DiscordRole_DiscordRoleId",
                        column: x => x.DiscordRoleId,
                        principalTable: "DiscordRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiscordGuildConfig_DiscordRole_MonitorRoleId",
                        column: x => x.MonitorRoleId,
                        principalTable: "DiscordRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscordGuildConfig_DiscordChannelId",
                table: "DiscordGuildConfig",
                column: "DiscordChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordGuildConfig_DiscordGuildId",
                table: "DiscordGuildConfig",
                column: "DiscordGuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiscordGuildConfig_DiscordRoleId",
                table: "DiscordGuildConfig",
                column: "DiscordRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordGuildConfig_MonitorRoleId",
                table: "DiscordGuildConfig",
                column: "MonitorRoleId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscordGuildConfig");
        }
    }
}