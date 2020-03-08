using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using System;

namespace LiveBot.Repository.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscordGuild",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TimeStamp = table.Column<DateTime>(nullable: false),
                    Deleted = table.Column<bool>(nullable: false),
                    DiscordId = table.Column<decimal>(nullable: false),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordGuild", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiscordChannel",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TimeStamp = table.Column<DateTime>(nullable: false),
                    Deleted = table.Column<bool>(nullable: false),
                    DiscordId = table.Column<decimal>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    DiscordGuildId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordChannel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscordChannel_DiscordGuild_DiscordGuildId",
                        column: x => x.DiscordGuildId,
                        principalTable: "DiscordGuild",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscordChannel_DiscordGuildId",
                table: "DiscordChannel",
                column: "DiscordGuildId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscordChannel");

            migrationBuilder.DropTable(
                name: "DiscordGuild");
        }
    }
}