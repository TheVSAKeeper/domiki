using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Stage2Workers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Traits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LogicName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DurationPercent = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Traits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Workers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TraitId = table.Column<int>(type: "integer", nullable: false),
                    ManufactureId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workers_Manufactures_ManufactureId",
                        column: x => x.ManufactureId,
                        principalTable: "Manufactures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Workers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Workers_Traits_TraitId",
                        column: x => x.TraitId,
                        principalTable: "Traits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Traits",
                columns: new[] { "Id", "DurationPercent", "LogicName", "Name" },
                values: new object[,]
                {
                    { 1, 0, "ordinary", "Обычный" },
                    { 2, -10, "nimble", "Проворный" },
                    { 3, -20, "diligent", "Работящий" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Workers_ManufactureId",
                table: "Workers",
                column: "ManufactureId");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_PlayerId",
                table: "Workers",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_TraitId",
                table: "Workers",
                column: "TraitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Workers");

            migrationBuilder.DropTable(
                name: "Traits");
        }
    }
}
