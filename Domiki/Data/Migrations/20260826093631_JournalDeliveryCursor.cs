using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <summary>
    /// Заменяет флаг <c>player_events.read</c> курсором доставки <c>players.last_delivered_event_id</c>.
    /// </summary>
    /// <remarks>
    /// Откат восстанавливает флаг по курсору приблизительно: курсор хранит только границу, поэтому строка, пропущенная
    /// поздним коммитом, вернётся уже прочитанной. Обратной по данным миграция не является.
    /// </remarks>
    public partial class JournalDeliveryCursor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "last_delivered_event_id",
                table: "players",
                type: "bigint",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE players AS p
                SET last_delivered_event_id = c.cursor_id
                FROM (
                    SELECT player_id,
                           COALESCE(MIN(id) FILTER (WHERE NOT "read") - 1, MAX(id)) AS cursor_id
                    FROM player_events
                    GROUP BY player_id
                ) AS c
                WHERE p.id = c.player_id;
                """);

            migrationBuilder.DropIndex(
                name: "ix_player_events_player_id_date",
                table: "player_events");

            migrationBuilder.DropColumn(
                name: "read",
                table: "player_events");

            migrationBuilder.CreateIndex(
                name: "ix_player_events_player_id_id",
                table: "player_events",
                columns: new[] { "player_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_player_events_player_id_type_date",
                table: "player_events",
                columns: new[] { "player_id", "type", "date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_player_events_player_id_id",
                table: "player_events");

            migrationBuilder.DropIndex(
                name: "ix_player_events_player_id_type_date",
                table: "player_events");

            migrationBuilder.AddColumn<bool>(
                name: "read",
                table: "player_events",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE player_events AS e
                SET "read" = true
                FROM players AS p
                WHERE p.id = e.player_id AND e.id <= COALESCE(p.last_delivered_event_id, 0);
                """);

            migrationBuilder.DropColumn(
                name: "last_delivered_event_id",
                table: "players");

            migrationBuilder.CreateIndex(
                name: "ix_player_events_player_id_date",
                table: "player_events",
                columns: new[] { "player_id", "date" });
        }
    }
}
