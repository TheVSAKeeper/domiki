using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class NeighborConvoy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "neighbor_convoys",
                columns: table => new
                {
                    player_id = table.Column<int>(type: "integer", nullable: false),
                    neighbor_id = table.Column<int>(type: "integer", nullable: false),
                    window_start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    bought_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_neighbor_convoys", x => new { x.player_id, x.neighbor_id });
                    table.ForeignKey(
                        name: "fk_neighbor_convoys_neighbors_neighbor_id",
                        column: x => x.neighbor_id,
                        principalTable: "neighbors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_neighbor_convoys_players_player_id",
                        column: x => x.player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_neighbor_convoys_neighbor_id",
                table: "neighbor_convoys",
                column: "neighbor_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "neighbor_convoys");
        }
    }
}
