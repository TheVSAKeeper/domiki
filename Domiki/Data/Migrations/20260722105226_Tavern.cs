using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Tavern : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "feed_workers",
                table: "players");

            migrationBuilder.AddColumn<bool>(
                name: "is_food",
                table: "resource_types",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("ALTER TABLE resource_types ALTER COLUMN is_food DROP DEFAULT;");

            migrationBuilder.InsertData("resource_types",
                columns: new[] { "id", "name", "logic_name", "is_food" },
                values: new object[] { 21, "Сыр", "cheese", true });

            migrationBuilder.UpdateData(
                table: "resource_types",
                keyColumn: "id",
                keyValue: 15,
                column: "is_food",
                value: true);

            migrationBuilder.InsertData("domik_types",
                columns: new[] { "id", "name", "logic_name", "max_count", "unlock_level" },
                values: new object[] { 18, "Корчма", "tavern", 1, 16 });

            migrationBuilder.InsertData("domik_type_levels",
                columns: new[] { "domik_type_id", "value", "upgrade_seconds", "max_manufacture_count" },
                values: new object[,]
                {
                    { 18, 1, 60, 1 },
                    { 18, 2, 3600, 1 },
                    { 18, 3, 36000, 1 },
                });

            migrationBuilder.InsertData("domik_type_level_resources",
                columns: new[] { "domik_type_level_domik_type_id", "domik_type_level_value", "resource_type_id", "value" },
                values: new object[,]
                {
                    { 18, 1, 1, 300 },
                    { 18, 2, 1, 800 }, { 18, 2, 6, 10 }, { 18, 2, 7, 10 },
                    { 18, 3, 1, 2500 }, { 18, 3, 6, 30 }, { 18, 3, 7, 20 }, { 18, 3, 17, 10 },
                });

            migrationBuilder.InsertData("receipts",
                columns: new[] { "id", "name", "logic_name", "duration_seconds", "plodder_count", "output_bonus_percent" },
                values: new object[,]
                {
                    { 74, "Сварить сыр", "make_cheese", 7200, 1, 0 },
                    { 75, "Продать сыр", "sell_cheese", 60, 1, 0 },
                });

            migrationBuilder.InsertData("receipt_resources",
                columns: new[] { "receipt_id", "resource_type_id", "is_input", "is_optional", "value" },
                values: new object[,]
                {
                    { 74, 13, true, false, 2 }, { 74, 21, false, false, 2 },
                    { 75, 21, true, false, 1 }, { 75, 1, false, false, 25 },
                });

            migrationBuilder.InsertData("domik_type_level_recepts",
                columns: new[] { "domik_type_level_domik_type_id", "domik_type_level_value", "receipt_id" },
                values: new object[,]
                {
                    { 17, 1, 74 }, { 17, 2, 74 }, { 17, 3, 74 }, { 17, 4, 74 }, { 17, 5, 74 },
                    { 7, 1, 75 }, { 7, 2, 75 }, { 7, 3, 75 }, { 7, 4, 75 }, { 7, 5, 75 },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "domik_type_level_recepts",
                keyColumns: new[] { "domik_type_level_domik_type_id", "domik_type_level_value", "receipt_id" },
                keyValues: new object[,]
                {
                    { 17, 1, 74 }, { 17, 2, 74 }, { 17, 3, 74 }, { 17, 4, 74 }, { 17, 5, 74 },
                    { 7, 1, 75 }, { 7, 2, 75 }, { 7, 3, 75 }, { 7, 4, 75 }, { 7, 5, 75 },
                });

            migrationBuilder.DeleteData(
                table: "receipt_resources",
                keyColumns: new[] { "receipt_id", "resource_type_id", "is_input" },
                keyValues: new object[,]
                {
                    { 74, 13, true }, { 74, 21, false },
                    { 75, 21, true }, { 75, 1, false },
                });

            migrationBuilder.DeleteData(
                table: "receipts",
                keyColumn: "id",
                keyValues: new object[] { 74, 75 });

            migrationBuilder.DeleteData(
                table: "domik_type_level_resources",
                keyColumns: new[] { "domik_type_level_domik_type_id", "domik_type_level_value", "resource_type_id" },
                keyValues: new object[,]
                {
                    { 18, 1, 1 },
                    { 18, 2, 1 }, { 18, 2, 6 }, { 18, 2, 7 },
                    { 18, 3, 1 }, { 18, 3, 6 }, { 18, 3, 7 }, { 18, 3, 17 },
                });

            migrationBuilder.DeleteData(
                table: "domik_type_levels",
                keyColumns: new[] { "domik_type_id", "value" },
                keyValues: new object[,]
                {
                    { 18, 1 },
                    { 18, 2 },
                    { 18, 3 },
                });

            migrationBuilder.DeleteData(
                table: "domik_types",
                keyColumn: "id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "resource_types",
                keyColumn: "id",
                keyValue: 21);

            migrationBuilder.UpdateData(
                table: "resource_types",
                keyColumn: "id",
                keyValue: 15,
                column: "is_food",
                value: false);

            migrationBuilder.DropColumn(
                name: "is_food",
                table: "resource_types");

            migrationBuilder.AddColumn<bool>(
                name: "feed_workers",
                table: "players",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("ALTER TABLE players ALTER COLUMN feed_workers DROP DEFAULT;");
        }
    }
}
