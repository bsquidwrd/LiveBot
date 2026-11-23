using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveBot.Repository.Migrations
{
    /// <summary>
    /// Fills in StreamSubscription rows saved with a null or blank Message before #108, which crashed
    /// notifications for them.
    /// </summary>
    /// <remarks>
    /// Mirrors the app's fallback: the guild's configured message when it has one, otherwise the text of
    /// Defaults.NotificationMessage, with "{role} " prepended when the subscription has roles to mention
    /// and the message has no {role} placeholder. Data only, and only touches blank rows, so it is safe
    /// to run against a database that was already fixed by hand.
    /// </remarks>
    public partial class BackfillmissingStreamSubscriptionmessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
WITH fallback AS (
    SELECT s.""Id"",
        COALESCE(
            (SELECT NULLIF(btrim(c.""Message""), '')
             FROM ""DiscordGuildConfig"" c
             WHERE c.""DiscordGuildId"" = s.""DiscordGuildId"" AND NOT c.""Deleted""
             ORDER BY c.""Id""
             LIMIT 1),
            '{role} {name} is live and playing {game}! {url}'
        ) AS ""Message""
    FROM ""StreamSubscription"" s
    WHERE NULLIF(btrim(s.""Message""), '') IS NULL
)
UPDATE ""StreamSubscription"" s
SET ""Message"" = CASE
    WHEN f.""Message"" !~* '\{role\}'
        AND EXISTS (
            SELECT 1
            FROM ""RoleToMention"" r
            WHERE r.""StreamSubscriptionId"" = s.""Id"" AND NOT r.""Deleted""
        )
        THEN '{role} ' || f.""Message""
    ELSE f.""Message""
END
FROM fallback f
WHERE f.""Id"" = s.""Id"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // A filled-in message can't be told apart from one a user chose, so there is nothing to undo.
        }
    }
}
