using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Errands : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ErrandId",
                table: "Workers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Errands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    NeighborId = table.Column<int>(type: "integer", nullable: false),
                    TemplateId = table.Column<int>(type: "integer", nullable: false),
                    ExpireDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcceptDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClueId = table.Column<int>(type: "integer", nullable: true),
                    FinishDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Errands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Errands_Neighbors_NeighborId",
                        column: x => x.NeighborId,
                        principalTable: "Neighbors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Errands_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Workers_ErrandId",
                table: "Workers",
                column: "ErrandId");

            migrationBuilder.CreateIndex(
                name: "IX_Errands_NeighborId",
                table: "Errands",
                column: "NeighborId");

            migrationBuilder.CreateIndex(
                name: "IX_Errands_PlayerId",
                table: "Errands",
                column: "PlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Workers_Errands_ErrandId",
                table: "Workers",
                column: "ErrandId",
                principalTable: "Errands",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Workers_Errands_ErrandId",
                table: "Workers");

            migrationBuilder.DropTable(
                name: "Errands");

            migrationBuilder.DropIndex(
                name: "IX_Workers_ErrandId",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "ErrandId",
                table: "Workers");
        }
    }
}
