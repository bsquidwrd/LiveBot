using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveBot.Repository.Migrations
{
    /// <summary>
    /// Adds StreamNotification.OfflineAt and backfills it so the first startup catch-up after deploying
    /// does not re-mark streams that already ended.
    /// </summary>
    /// <remarks>
    /// The backfill leaves a row null only when its stream could still be live: it is the user's latest
    /// stream, started within the last 7 days, still has a message, and was never marked offline. Every
    /// other successful row gets the time parsed from its "Marked offline at" log message, or its
    /// TimeStamp when there is none. The update only touches null rows, so it can be run by hand before
    /// deploying and startup then finds nothing to do.
    /// </remarks>
    public partial class AddOfflineAttoStreamNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OfflineAt",
                table: "StreamNotification",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(@"
WITH latest AS (
    SELECT DISTINCT ON (""ServiceType"", ""User_SourceID"")
        ""ServiceType"", ""User_SourceID"", ""Stream_SourceID"", ""Stream_StartTime""
    FROM ""StreamNotification""
    WHERE ""Success"" AND ""DiscordMessage_DiscordId"" IS NOT NULL AND NOT ""Deleted""
    ORDER BY ""ServiceType"", ""User_SourceID"", ""Stream_StartTime"" DESC
)
UPDATE ""StreamNotification"" n
SET ""OfflineAt"" = CASE
    WHEN n.""LogMessage"" ~ '^Marked offline at \d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?Z'
        THEN substring(n.""LogMessage"" from '^Marked offline at (\S+)')::timestamptz
    ELSE n.""TimeStamp""
END
WHERE n.""OfflineAt"" IS NULL
    AND n.""Success""
    AND NOT (
        n.""DiscordMessage_DiscordId"" IS NOT NULL
        AND COALESCE(n.""LogMessage"", '') NOT LIKE 'Marked offline%'
        AND EXISTS (
            SELECT 1
            FROM latest l
            WHERE l.""ServiceType"" = n.""ServiceType""
                AND l.""User_SourceID"" = n.""User_SourceID""
                AND l.""Stream_SourceID"" IS NOT DISTINCT FROM n.""Stream_SourceID""
                AND l.""Stream_StartTime"" >= now() - interval '7 days'
        )
    );");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OfflineAt",
                table: "StreamNotification");
        }
    }
}
