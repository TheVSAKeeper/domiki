using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Incidents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IncidentId",
                table: "Workers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastIncidentDate",
                table: "Players",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    MissingWorkerId = table.Column<int>(type: "integer", nullable: false),
                    ExpeditionTypeId = table.Column<int>(type: "integer", nullable: false),
                    TemplateId = table.Column<int>(type: "integer", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClueId = table.Column<int>(type: "integer", nullable: true),
                    SearchEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Incidents_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Incidents_Workers_MissingWorkerId",
                        column: x => x.MissingWorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Workers_IncidentId",
                table: "Workers",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_MissingWorkerId",
                table: "Incidents",
                column: "MissingWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_PlayerId",
                table: "Incidents",
                column: "PlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Workers_Incidents_IncidentId",
                table: "Workers",
                column: "IncidentId",
                principalTable: "Incidents",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Workers_Incidents_IncidentId",
                table: "Workers");

            migrationBuilder.DropTable(
                name: "Incidents");

            migrationBuilder.DropIndex(
                name: "IX_Workers_IncidentId",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "IncidentId",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "LastIncidentDate",
                table: "Players");
        }
    }
}
