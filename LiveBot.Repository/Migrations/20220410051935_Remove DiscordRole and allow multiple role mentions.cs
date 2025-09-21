using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LiveBot.Repository.Migrations
{
    public partial class RemoveDiscordRoleandallowmultiplerolementions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(sql: @"
CREATE TABLE ""DiscordGuildConfig_Translation"" AS
SELECT
    ""DiscordGuildConfig"".""Id""
    ,""Mention"".""DiscordId"" AS ""MentionRoleDiscordId""
    ,""Admin"".""DiscordId"" AS ""AdminRoleDiscordId""
    ,""Monitor"".""DiscordId"" AS ""MonitorRoleDiscordId""

FROM
    public.""DiscordGuildConfig""
    LEFT JOIN public.""DiscordRole"" AS ""Mention"" ON ""DiscordGuildConfig"".""DiscordRoleId"" = ""Mention"".""Id""
    LEFT JOIN public.""DiscordRole"" AS ""Admin"" ON ""DiscordGuildConfig"".""AdminRoleId"" = ""Admin"".""Id""
    LEFT JOIN public.""DiscordRole"" AS ""Monitor"" ON ""DiscordGuildConfig"".""MonitorRoleId"" = ""Monitor"".""Id""

WHERE
    ""DiscordGuildConfig"".""DiscordRoleId"" IS NOT NULL
    OR ""DiscordGuildConfig"".""MonitorRoleId"" IS NOT NULL
    OR ""DiscordGuildConfig"".""AdminRoleId"" IS NOT NULL;

CREATE TABLE ""StreamSubscription_Translation"" AS
SELECT
    ""StreamSubscription"".""Id""
    ,""DiscordRole"".""DiscordId"" AS ""DiscordRoleId""

FROM
    public.""StreamSubscription""
    INNER JOIN public.""DiscordRole"" ON ""StreamSubscription"".""DiscordRoleId"" = ""DiscordRole"".""Id""

WHERE
    1=1;

CREATE TABLE ""StreamSubscription_Backup_IsFromRole"" AS
SELECT*
FROM public.""StreamSubscription""
WHERE ""IsFromRole"" = TRUE;

DELETE FROM public.""StreamSubscription""
WHERE ""IsFromRole"" = TRUE;
");

            migrationBuilder.DropForeignKey(
                name: "FK_DiscordGuildConfig_DiscordRole_AdminRoleId",
                table: "DiscordGuildConfig");

            migrationBuilder.DropForeignKey(
                name: "FK_DiscordGuildConfig_DiscordRole_DiscordRoleId",
                table: "DiscordGuildConfig");

            migrationBuilder.DropForeignKey(
                name: "FK_DiscordGuildConfig_DiscordRole_MonitorRoleId",
                table: "DiscordGuildConfig");

            migrationBuilder.DropForeignKey(
                name: "FK_StreamSubscription_DiscordRole_DiscordRoleId",
                table: "StreamSubscription");

            migrationBuilder.DropTable(
                name: "DiscordRole");

            migrationBuilder.DropIndex(
                name: "IX_StreamSubscription_DiscordRoleId",
                table: "StreamSubscription");

            migrationBuilder.DropIndex(
                name: "IX_DiscordGuildConfig_AdminRoleId",
                table: "DiscordGuildConfig");

            migrationBuilder.DropIndex(
                name: "IX_DiscordGuildConfig_DiscordRoleId",
                table: "DiscordGuildConfig");

            migrationBuilder.DropIndex(
                name: "IX_DiscordGuildConfig_MonitorRoleId",
                table: "DiscordGuildConfig");

            migrationBuilder.DropColumn(
                name: "DiscordRoleId",
                table: "StreamSubscription");

            migrationBuilder.DropColumn(
                name: "IsFromRole",
                table: "StreamSubscription");

            migrationBuilder.DropColumn(
                name: "AdminRoleId",
                table: "DiscordGuildConfig");

            migrationBuilder.DropColumn(
                name: "DiscordRoleId",
                table: "DiscordGuildConfig");

            migrationBuilder.DropColumn(
                name: "MonitorRoleId",
                table: "DiscordGuildConfig");

            migrationBuilder.AddColumn<decimal>(
                name: "AdminRoleDiscordId",
                table: "DiscordGuildConfig",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MentionRoleDiscordId",
                table: "DiscordGuildConfig",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MonitorRoleDiscordId",
                table: "DiscordGuildConfig",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RoleToMention",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StreamSubscriptionId = table.Column<long>(type: "bigint", nullable: true),
                    DiscordRoleId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleToMention", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleToMention_StreamSubscription_StreamSubscriptionId",
                        column: x => x.StreamSubscriptionId,
                        principalTable: "StreamSubscription",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleToMention_StreamSubscriptionId",
                table: "RoleToMention",
                column: "StreamSubscriptionId");

            migrationBuilder.AddUniqueConstraint(
                name: "Unique_StreamSubscription_DiscordRoleId",
                table: "RoleToMention",
                columns: new[] { "StreamSubscriptionId", "DiscordRoleId" });

            migrationBuilder.Sql(sql: @"
UPDATE public.""DiscordGuildConfig""
SET
    ""AdminRoleDiscordId"" = ""Backup"".""AdminRoleDiscordId""
    ,""MentionRoleDiscordId"" = ""Backup"".""MentionRoleDiscordId""
    ,""MonitorRoleDiscordId"" = ""Backup"".""MonitorRoleDiscordId""

FROM
    public.""DiscordGuildConfig_Translation"" AS ""Backup""

WHERE
    ""Backup"".""Id"" = ""DiscordGuildConfig"".""Id"";

INSERT INTO public.""RoleToMention"" (""TimeStamp"", ""Deleted"", ""StreamSubscriptionId"", ""DiscordRoleId"")
SELECT
    CURRENT_TIMESTAMP
    ,FALSE
    ,""Backup"".""Id""
    ,""Backup"".""DiscordRoleId""

FROM
    public.""StreamSubscription_Translation"" AS ""Backup""

WHERE
    1=1;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleToMention");

            migrationBuilder.DropColumn(
                name: "AdminRoleDiscordId",
                table: "DiscordGuildConfig");

            migrationBuilder.DropColumn(
                name: "MentionRoleDiscordId",
                table: "DiscordGuildConfig");

            migrationBuilder.DropColumn(
                name: "MonitorRoleDiscordId",
                table: "DiscordGuildConfig");

            migrationBuilder.AddColumn<long>(
                name: "DiscordRoleId",
                table: "StreamSubscription",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFromRole",
                table: "StreamSubscription",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "AdminRoleId",
                table: "DiscordGuildConfig",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DiscordRoleId",
                table: "DiscordGuildConfig",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MonitorRoleId",
                table: "DiscordGuildConfig",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DiscordRole",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DiscordGuildId = table.Column<long>(type: "bigint", nullable: true),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    DiscordId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    TimeStamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordRole", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscordRole_DiscordGuild_DiscordGuildId",
                        column: x => x.DiscordGuildId,
                        principalTable: "DiscordGuild",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_StreamSubscription_DiscordRoleId",
                table: "StreamSubscription",
                column: "DiscordRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordGuildConfig_AdminRoleId",
                table: "DiscordGuildConfig",
                column: "AdminRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordGuildConfig_DiscordRoleId",
                table: "DiscordGuildConfig",
                column: "DiscordRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordGuildConfig_MonitorRoleId",
                table: "DiscordGuildConfig",
                column: "MonitorRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordRole_DiscordGuildId",
                table: "DiscordRole",
                column: "DiscordGuildId");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscordGuildConfig_DiscordRole_AdminRoleId",
                table: "DiscordGuildConfig",
                column: "AdminRoleId",
                principalTable: "DiscordRole",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscordGuildConfig_DiscordRole_DiscordRoleId",
                table: "DiscordGuildConfig",
                column: "DiscordRoleId",
                principalTable: "DiscordRole",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscordGuildConfig_DiscordRole_MonitorRoleId",
                table: "DiscordGuildConfig",
                column: "MonitorRoleId",
                principalTable: "DiscordRole",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StreamSubscription_DiscordRole_DiscordRoleId",
                table: "StreamSubscription",
                column: "DiscordRoleId",
                principalTable: "DiscordRole",
                principalColumn: "Id");
        }
    }
}