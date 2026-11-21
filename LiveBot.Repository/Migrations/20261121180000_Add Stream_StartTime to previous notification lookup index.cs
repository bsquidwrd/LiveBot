using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveBot.Repository.Migrations
{
    /// <summary>
    /// Replaces IX_PreviousNotificationLookup with a copy that also covers Stream_StartTime, so the
    /// previous-notification window is an index range scan instead of a read of the whole history.
    /// </summary>
    /// <remarks>
    /// Every step is idempotent so the statements can be run by hand during a quiet window before
    /// deploying; startup then finds nothing to do. CONCURRENTLY keeps writes flowing during the build
    /// and cannot run inside a transaction.
    /// </remarks>
    public partial class AddStream_StartTimetopreviousnotificationlookupindex : Migration
    {
        private const string NewIndex = "IX_PreviousNotificationLookup_StartTime";
        private const string OldIndex = "IX_PreviousNotificationLookup";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // An interrupted concurrent build leaves an invalid index that IF NOT EXISTS would skip,
            // after which the old index would be dropped with nothing usable in its place.
            migrationBuilder.Sql($@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM pg_index i
        JOIN pg_class c ON c.oid = i.indexrelid
        WHERE c.relname = '{NewIndex}' AND NOT i.indisvalid
    ) THEN
        RAISE EXCEPTION 'Index ""{NewIndex}"" is invalid from an interrupted build. Run DROP INDEX CONCURRENTLY ""{NewIndex}""; then retry.';
    END IF;
END $$;");

            migrationBuilder.Sql(
                $@"CREATE INDEX CONCURRENTLY IF NOT EXISTS ""{NewIndex}"" ON ""StreamNotification"" (""User_SourceID"", ""DiscordGuild_DiscordId"", ""DiscordChannel_DiscordId"", ""Stream_StartTime"");",
                suppressTransaction: true);

            migrationBuilder.Sql(
                $@"DROP INDEX CONCURRENTLY IF EXISTS ""{OldIndex}"";",
                suppressTransaction: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $@"CREATE INDEX CONCURRENTLY IF NOT EXISTS ""{OldIndex}"" ON ""StreamNotification"" (""User_SourceID"", ""DiscordGuild_DiscordId"", ""DiscordChannel_DiscordId"");",
                suppressTransaction: true);

            migrationBuilder.Sql(
                $@"DROP INDEX CONCURRENTLY IF EXISTS ""{NewIndex}"";",
                suppressTransaction: true);
        }
    }
}
