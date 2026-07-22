using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class WeatherIllnesses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "sick_type_id",
                table: "workers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "cloak_wear_points",
                table: "players",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "cloak_count",
                table: "manufactures",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "sick_type_id",
                table: "manufactures",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("ALTER TABLE manufactures ALTER COLUMN cloak_count DROP DEFAULT;");
            migrationBuilder.Sql("ALTER TABLE players ALTER COLUMN cloak_wear_points DROP DEFAULT;");

            migrationBuilder.CreateTable(
                name: "sick_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    logic_name = table.Column<string>(type: "text", nullable: false),
                    weather_type_id = table.Column<int>(type: "integer", nullable: false),
                    cloak_protects = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sick_types", x => x.id);
                    table.ForeignKey(
                        name: "fk_sick_types_weather_types_weather_type_id",
                        column: x => x.weather_type_id,
                        principalTable: "weather_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData("sick_types",
                columns: new[] { "id", "name", "logic_name", "weather_type_id", "cloak_protects" },
                values: new object[,]
                {
                    { 1, "Простуда", "cold", 2, true },
                    { 2, "Перегрев", "heatstroke", 3, false },
                    { 3, "Озноб", "chill", 4, true },
                    { 4, "Прострел", "lumbago", 5, true },
                });

            migrationBuilder.InsertData("resource_types",
                columns: new[] { "id", "name", "logic_name" },
                values: new object[] { 20, "Плащ", "cloak" });

            migrationBuilder.InsertData("receipts",
                columns: new[] { "id", "name", "logic_name", "duration_seconds", "plodder_count", "output_bonus_percent" },
                values: new object[] { 73, "Сшить плащ", "sew_cloak", 14400, 1, 0 });

            migrationBuilder.InsertData("receipt_resources",
                columns: new[] { "receipt_id", "resource_type_id", "is_input", "is_optional", "value" },
                values: new object[,]
                {
                    { 73, 19, true, false, 2 },
                    { 73, 20, false, false, 1 },
                });

            migrationBuilder.InsertData("domik_type_level_recepts",
                columns: new[] { "domik_type_level_domik_type_id", "domik_type_level_value", "receipt_id" },
                values: new object[,]
                {
                    { 8, 1, 73 }, { 8, 2, 73 }, { 8, 3, 73 }, { 8, 4, 73 }, { 8, 5, 73 },
                });

            migrationBuilder.CreateIndex(
                name: "ix_workers_sick_type_id",
                table: "workers",
                column: "sick_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_manufactures_sick_type_id",
                table: "manufactures",
                column: "sick_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_sick_types_weather_type_id",
                table: "sick_types",
                column: "weather_type_id");

            migrationBuilder.AddForeignKey(
                name: "fk_manufactures_sick_types_sick_type_id",
                table: "manufactures",
                column: "sick_type_id",
                principalTable: "sick_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_workers_sick_types_sick_type_id",
                table: "workers",
                column: "sick_type_id",
                principalTable: "sick_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_manufactures_sick_types_sick_type_id",
                table: "manufactures");

            migrationBuilder.DropForeignKey(
                name: "fk_workers_sick_types_sick_type_id",
                table: "workers");

            migrationBuilder.DeleteData(
                table: "domik_type_level_recepts",
                keyColumns: new[] { "domik_type_level_domik_type_id", "domik_type_level_value", "receipt_id" },
                keyValues: new object[,]
                {
                    { 8, 1, 73 }, { 8, 2, 73 }, { 8, 3, 73 }, { 8, 4, 73 }, { 8, 5, 73 },
                });

            migrationBuilder.DeleteData(
                table: "receipt_resources",
                keyColumns: new[] { "receipt_id", "resource_type_id", "is_input" },
                keyValues: new object[,]
                {
                    { 73, 19, true },
                    { 73, 20, false },
                });

            migrationBuilder.DeleteData(
                table: "receipts",
                keyColumn: "id",
                keyValue: 73);

            migrationBuilder.DeleteData(
                table: "resource_types",
                keyColumn: "id",
                keyValue: 20);

            migrationBuilder.DropTable(
                name: "sick_types");

            migrationBuilder.DropIndex(
                name: "ix_workers_sick_type_id",
                table: "workers");

            migrationBuilder.DropIndex(
                name: "ix_manufactures_sick_type_id",
                table: "manufactures");

            migrationBuilder.DropColumn(
                name: "sick_type_id",
                table: "workers");

            migrationBuilder.DropColumn(
                name: "cloak_wear_points",
                table: "players");

            migrationBuilder.DropColumn(
                name: "cloak_count",
                table: "manufactures");

            migrationBuilder.DropColumn(
                name: "sick_type_id",
                table: "manufactures");
        }
    }
}
