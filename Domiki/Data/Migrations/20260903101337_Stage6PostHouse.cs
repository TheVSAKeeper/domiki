using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Stage6PostHouse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "last_free_concession_at",
                table: "players",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                INSERT INTO domik_types (id, name, logic_name, max_count, unlock_level)
                VALUES (20, 'Ямская изба', 'post_house', 1, 0)
                ON CONFLICT (id) DO NOTHING;

                INSERT INTO domik_type_levels (domik_type_id, value, upgrade_seconds, max_manufacture_count)
                VALUES (20, 1, 0, 0), (20, 2, 300, 0), (20, 3, 3600, 0), (20, 4, 36000, 0), (20, 5, 172800, 0)
                ON CONFLICT (domik_type_id, value) DO NOTHING;

                INSERT INTO domik_type_level_resources (domik_type_level_domik_type_id, domik_type_level_value, resource_type_id, value)
                VALUES (20, 2, 1, 150),
                       (20, 3, 1, 500), (20, 3, 7, 8),
                       (20, 4, 1, 2000), (20, 4, 6, 25), (20, 4, 7, 15),
                       (20, 5, 1, 8000), (20, 5, 6, 50), (20, 5, 7, 40), (20, 5, 5, 10)
                ON CONFLICT (domik_type_level_domik_type_id, domik_type_level_value, resource_type_id) DO NOTHING;

                INSERT INTO domiks (player_id, id, type_id, level)
                SELECT p.id, COALESCE(m.max_id, 0) + 1, 20, 1
                FROM players p
                LEFT JOIN (SELECT player_id, MAX(id) AS max_id FROM domiks GROUP BY player_id) m ON m.player_id = p.id
                WHERE NOT EXISTS (SELECT 1 FROM domiks d WHERE d.player_id = p.id AND d.type_id = 20);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_free_concession_at",
                table: "players");
        }
    }
}
