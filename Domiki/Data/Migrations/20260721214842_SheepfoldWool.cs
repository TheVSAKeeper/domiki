using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class SheepfoldWool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData("domik_types",
                columns: new[] { "id", "name", "logic_name", "max_count", "unlock_level" },
                values: new object[] { 17, "Овчарня", "sheepfold", 1, 14 });

            migrationBuilder.InsertData("domik_type_levels",
                columns: new[] { "domik_type_id", "value", "upgrade_seconds", "max_manufacture_count" },
                values: new object[,]
                {
                    { 17, 1, 60, 1 },
                    { 17, 2, 300, 1 },
                    { 17, 3, 3600, 2 },
                    { 17, 4, 36000, 2 },
                    { 17, 5, 172800, 3 },
                });

            migrationBuilder.InsertData("domik_type_level_resources",
                columns: new[] { "domik_type_level_domik_type_id", "domik_type_level_value", "resource_type_id", "value" },
                values: new object[,]
                {
                    { 17, 1, 1, 250 },
                    { 17, 2, 1, 100 },
                    { 17, 3, 1, 300 }, { 17, 3, 2, 15 }, { 17, 3, 3, 15 },
                    { 17, 4, 1, 1500 }, { 17, 4, 2, 50 }, { 17, 4, 3, 50 }, { 17, 4, 4, 30 }, { 17, 4, 6, 6 },
                    { 17, 5, 1, 9000 }, { 17, 5, 2, 150 }, { 17, 5, 3, 150 }, { 17, 5, 4, 100 }, { 17, 5, 5, 20 }, { 17, 5, 6, 15 }, { 17, 5, 7, 15 }, { 17, 5, 8, 4 },
                });

            migrationBuilder.InsertData("resource_types",
                columns: new[] { "id", "name", "logic_name" },
                values: new object[,]
                {
                    { 18, "Шерсть", "wool" },
                    { 19, "Сукно", "cloth" },
                });

            migrationBuilder.InsertData("receipts",
                columns: new[] { "id", "name", "logic_name", "duration_seconds", "plodder_count", "output_bonus_percent" },
                values: new object[,]
                {
                    { 68, "Пасти овец", "sheep_graze", 10800, 1, 0 },
                    { 69, "Стричь овец", "sheep_shear", 7200, 1, 0 },
                    { 70, "Соткать сукно", "make_cloth", 7200, 1, 0 },
                    { 71, "Продать шерсть", "sell_wool", 60, 1, 0 },
                    { 72, "Продать сукно", "sell_cloth", 60, 1, 0 },
                });

            migrationBuilder.InsertData("receipt_resources",
                columns: new[] { "receipt_id", "resource_type_id", "is_input", "is_optional", "value" },
                values: new object[,]
                {
                    { 68, 18, false, false, 3 },
                    { 69, 13, true, false, 2 }, { 69, 18, false, false, 4 },
                    { 70, 18, true, false, 2 }, { 70, 19, false, false, 1 },
                    { 71, 18, true, false, 1 }, { 71, 1, false, false, 10 },
                    { 72, 19, true, false, 1 }, { 72, 1, false, false, 40 },
                });

            migrationBuilder.InsertData("domik_type_level_recepts",
                columns: new[] { "domik_type_level_domik_type_id", "domik_type_level_value", "receipt_id" },
                values: new object[,]
                {
                    { 17, 1, 68 }, { 17, 2, 68 }, { 17, 3, 68 }, { 17, 4, 68 }, { 17, 5, 68 },
                    { 17, 1, 69 }, { 17, 2, 69 }, { 17, 3, 69 }, { 17, 4, 69 }, { 17, 5, 69 },
                    { 8, 1, 70 }, { 8, 2, 70 }, { 8, 3, 70 }, { 8, 4, 70 }, { 8, 5, 70 },
                    { 7, 1, 71 }, { 7, 2, 71 }, { 7, 3, 71 }, { 7, 4, 71 }, { 7, 5, 71 },
                    { 7, 1, 72 }, { 7, 2, 72 }, { 7, 3, 72 }, { 7, 4, 72 }, { 7, 5, 72 },
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
                    { 17, 1, 68 }, { 17, 2, 68 }, { 17, 3, 68 }, { 17, 4, 68 }, { 17, 5, 68 },
                    { 17, 1, 69 }, { 17, 2, 69 }, { 17, 3, 69 }, { 17, 4, 69 }, { 17, 5, 69 },
                    { 8, 1, 70 }, { 8, 2, 70 }, { 8, 3, 70 }, { 8, 4, 70 }, { 8, 5, 70 },
                    { 7, 1, 71 }, { 7, 2, 71 }, { 7, 3, 71 }, { 7, 4, 71 }, { 7, 5, 71 },
                    { 7, 1, 72 }, { 7, 2, 72 }, { 7, 3, 72 }, { 7, 4, 72 }, { 7, 5, 72 },
                });

            migrationBuilder.DeleteData(
                table: "receipt_resources",
                keyColumns: new[] { "receipt_id", "resource_type_id", "is_input" },
                keyValues: new object[,]
                {
                    { 68, 18, false },
                    { 69, 13, true }, { 69, 18, false },
                    { 70, 18, true }, { 70, 19, false },
                    { 71, 18, true }, { 71, 1, false },
                    { 72, 19, true }, { 72, 1, false },
                });

            migrationBuilder.DeleteData(
                table: "receipts",
                keyColumn: "id",
                keyValues: new object[] { 68, 69, 70, 71, 72 });

            migrationBuilder.DeleteData(
                table: "domik_type_level_resources",
                keyColumns: new[] { "domik_type_level_domik_type_id", "domik_type_level_value", "resource_type_id" },
                keyValues: new object[,]
                {
                    { 17, 1, 1 },
                    { 17, 2, 1 },
                    { 17, 3, 1 }, { 17, 3, 2 }, { 17, 3, 3 },
                    { 17, 4, 1 }, { 17, 4, 2 }, { 17, 4, 3 }, { 17, 4, 4 }, { 17, 4, 6 },
                    { 17, 5, 1 }, { 17, 5, 2 }, { 17, 5, 3 }, { 17, 5, 4 }, { 17, 5, 5 }, { 17, 5, 6 }, { 17, 5, 7 }, { 17, 5, 8 },
                });

            migrationBuilder.DeleteData(
                table: "domik_type_levels",
                keyColumns: new[] { "domik_type_id", "value" },
                keyValues: new object[,]
                {
                    { 17, 1 },
                    { 17, 2 },
                    { 17, 3 },
                    { 17, 4 },
                    { 17, 5 },
                });

            migrationBuilder.DeleteData(
                table: "domik_types",
                keyColumn: "id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "resource_types",
                keyColumn: "id",
                keyValues: new object[] { 18, 19 });

        }
    }
}
