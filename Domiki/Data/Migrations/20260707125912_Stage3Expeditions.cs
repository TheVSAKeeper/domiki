using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Stage3Expeditions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExpeditionId",
                table: "Workers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExpeditionsSincePity",
                table: "Players",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ExpeditionTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LogicName = table.Column<string>(type: "text", nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    WorkerCount = table.Column<int>(type: "integer", nullable: false),
                    GoldCost = table.Column<int>(type: "integer", nullable: false),
                    RollCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpeditionTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExpeditionLoot",
                columns: table => new
                {
                    ExpeditionTypeId = table.Column<int>(type: "integer", nullable: false),
                    ResourceTypeId = table.Column<int>(type: "integer", nullable: false),
                    MinValue = table.Column<int>(type: "integer", nullable: false),
                    MaxValue = table.Column<int>(type: "integer", nullable: false),
                    Weight = table.Column<int>(type: "integer", nullable: false),
                    IsRare = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpeditionLoot", x => new { x.ExpeditionTypeId, x.ResourceTypeId });
                    table.ForeignKey(
                        name: "FK_ExpeditionLoot_ExpeditionTypes_ExpeditionTypeId",
                        column: x => x.ExpeditionTypeId,
                        principalTable: "ExpeditionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Expeditions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    ExpeditionTypeId = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinishDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expeditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Expeditions_ExpeditionTypes_ExpeditionTypeId",
                        column: x => x.ExpeditionTypeId,
                        principalTable: "ExpeditionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Expeditions_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Workers_ExpeditionId",
                table: "Workers",
                column: "ExpeditionId");

            migrationBuilder.CreateIndex(
                name: "IX_Expeditions_ExpeditionTypeId",
                table: "Expeditions",
                column: "ExpeditionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Expeditions_PlayerId",
                table: "Expeditions",
                column: "PlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Workers_Expeditions_ExpeditionId",
                table: "Workers",
                column: "ExpeditionId",
                principalTable: "Expeditions",
                principalColumn: "Id");

            migrationBuilder.InsertData(
                table: "ExpeditionTypes",
                columns: new[] { "Id", "Name", "LogicName", "DurationSeconds", "WorkerCount", "GoldCost", "RollCount" },
                values: new object[,]
                {
                    { 1, "Ближняя вылазка", "short_scout", 14400, 2, 1, 1 },
                    { 2, "Дальний поход", "long_journey", 86400, 5, 2, 3 },
                });

            migrationBuilder.InsertData(
                table: "ExpeditionLoot",
                columns: new[] { "ExpeditionTypeId", "ResourceTypeId", "MinValue", "MaxValue", "Weight", "IsRare" },
                values: new object[,]
                {
                    { 1, 3, 5, 12, 30, false },
                    { 1, 2, 5, 12, 30, false },
                    { 1, 4, 5, 12, 30, false },
                    { 1, 8, 1, 2, 10, true },
                    { 2, 3, 12, 25, 25, false },
                    { 2, 2, 12, 25, 25, false },
                    { 2, 4, 12, 25, 25, false },
                    { 2, 8, 2, 4, 15, false },
                    { 2, 9, 1, 3, 10, true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Workers_Expeditions_ExpeditionId",
                table: "Workers");

            migrationBuilder.DropTable(
                name: "ExpeditionLoot");

            migrationBuilder.DropTable(
                name: "Expeditions");

            migrationBuilder.DropTable(
                name: "ExpeditionTypes");

            migrationBuilder.DropIndex(
                name: "IX_Workers_ExpeditionId",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "ExpeditionId",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "ExpeditionsSincePity",
                table: "Players");
        }
    }
}
