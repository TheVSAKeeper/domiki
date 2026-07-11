using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    public partial class Crafts5Balance : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData("Receipts",
                columns: new[] { "Id", "Name", "LogicName", "DurationSeconds", "PlodderCount" },
                values: new object[,]
                {
                    { 35, "Толпой рубить лес", "wood_dig_together", 3600, 5 },
                    { 36, "Толпой бить камень", "stone_dig_together", 3600, 5 },
                    { 37, "Продать кирпич x10", "sell_brick_x10", 300, 1 },
                    { 38, "Продать доску x10", "sell_board_x10", 300, 1 },
                    { 39, "Продать мебель x10", "sell_furniture_x10", 300, 1 },
                });

            migrationBuilder.InsertData("ReceiptResources",
                columns: new[] { "ReceiptId", "ResourceTypeId", "IsInput", "Value" },
                values: new object[,]
                {
                    { 35, 1, true, 5 }, { 35, 3, false, 8 },
                    { 36, 1, true, 5 }, { 36, 2, false, 8 },
                    { 37, 6, true, 10 }, { 37, 1, false, 350 },
                    { 38, 7, true, 10 }, { 38, 1, false, 350 },
                    { 39, 9, true, 10 }, { 39, 1, false, 950 },
                });

            migrationBuilder.InsertData("DomikTypeLevelReceipts",
                columns: new[] { "DomikTypeLevelDomikTypeId", "DomikTypeLevelValue", "ReceiptId" },
                values: new object[,]
                {
                    { 6, 2, 35 }, { 6, 3, 35 }, { 6, 4, 35 }, { 6, 5, 35 },
                    { 3, 2, 36 }, { 3, 3, 36 }, { 3, 4, 36 }, { 3, 5, 36 },
                    { 7, 5, 37 }, { 7, 5, 38 }, { 7, 5, 39 },
                });

            migrationBuilder.InsertData("WeatherTypes",
                columns: new[] { "Id", "Name", "LogicName", "RotationWeight" },
                values: new object[,]
                {
                    { 4, "Мороз", "frost", 15 },
                    { 5, "Ветер", "wind", 15 },
                });

            migrationBuilder.Sql("UPDATE \"WeatherTypes\" SET \"RotationWeight\" = 25 WHERE \"LogicName\" IN ('rain','drought');");
            migrationBuilder.Sql(
                "INSERT INTO \"WeatherTypeEffects\" (\"WeatherTypeId\", \"DomikTypeId\", \"OutputPercent\") " +
                "SELECT 4, \"Id\", 75 FROM \"DomikTypes\" WHERE \"LogicName\" = 'stone_mine';");
            migrationBuilder.Sql(
                "INSERT INTO \"WeatherTypeEffects\" (\"WeatherTypeId\", \"DomikTypeId\", \"OutputPercent\") " +
                "SELECT 4, \"Id\", 125 FROM \"DomikTypes\" WHERE \"LogicName\" = 'forge';");
            migrationBuilder.Sql(
                "INSERT INTO \"WeatherTypeEffects\" (\"WeatherTypeId\", \"DomikTypeId\", \"OutputPercent\") " +
                "SELECT 5, \"Id\", 125 FROM \"DomikTypes\" WHERE \"LogicName\" = 'lumber_mill';");
            migrationBuilder.Sql(
                "INSERT INTO \"WeatherTypeEffects\" (\"WeatherTypeId\", \"DomikTypeId\", \"OutputPercent\") " +
                "SELECT 5, \"Id\", 75 FROM \"DomikTypes\" WHERE \"LogicName\" = 'forge';");

            migrationBuilder.InsertData("ExpeditionTypes",
                columns: new[] { "Id", "Name", "LogicName", "DurationSeconds", "WorkerCount", "GoldCost", "RollCount" },
                values: new object[] { 3, "Пешая вылазка", "foot_scout", 7200, 1, 0, 1 });

            migrationBuilder.Sql(@"INSERT INTO ""ExpeditionLoot"" (""Id"", ""ExpeditionTypeId"", ""Kind"", ""ResourceTypeId"", ""DecorTypeId"", ""MinValue"", ""MaxValue"", ""Weight"", ""IsRare"") VALUES
    (13, 3, 1, 3, NULL, 3, 8, 1, false),
    (14, 3, 1, 2, NULL, 3, 8, 1, false),
    (15, 3, 1, 4, NULL, 3, 8, 1, false);");
            migrationBuilder.Sql("SELECT setval(pg_get_serial_sequence('\"ExpeditionLoot\"', 'Id'), 15, true);");

            migrationBuilder.UpdateData(
                table: "DomikTypes",
                keyColumn: "Id",
                keyValue: 7,
                column: "Name",
                value: "Лавка");

            migrationBuilder.UpdateData(
                table: "DomikTypes",
                keyColumn: "Id",
                keyValue: 10,
                column: "Name",
                value: "Сборня");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "DomikTypes",
                keyColumn: "Id",
                keyValue: 7,
                column: "Name",
                value: "Магазин");

            migrationBuilder.UpdateData(
                table: "DomikTypes",
                keyColumn: "Id",
                keyValue: 10,
                column: "Name",
                value: "Сходня");

            migrationBuilder.Sql("DELETE FROM \"ExpeditionLoot\" WHERE \"Id\" IN (13, 14, 15);");

            migrationBuilder.DeleteData(
                table: "ExpeditionTypes",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.Sql("DELETE FROM \"WeatherTypeEffects\" WHERE \"WeatherTypeId\" IN (4, 5);");
            migrationBuilder.DeleteData(
                table: "WeatherTypes",
                keyColumn: "Id",
                keyValue: 4);
            migrationBuilder.DeleteData(
                table: "WeatherTypes",
                keyColumn: "Id",
                keyValue: 5);
            migrationBuilder.Sql("UPDATE \"WeatherTypes\" SET \"RotationWeight\" = 30 WHERE \"LogicName\" IN ('rain','drought');");

            migrationBuilder.DeleteData(
                table: "DomikTypeLevelReceipts",
                keyColumns: new[] { "DomikTypeLevelDomikTypeId", "DomikTypeLevelValue", "ReceiptId" },
                keyValues: new object[,]
                {
                    { 6, 2, 35 }, { 6, 3, 35 }, { 6, 4, 35 }, { 6, 5, 35 },
                    { 3, 2, 36 }, { 3, 3, 36 }, { 3, 4, 36 }, { 3, 5, 36 },
                    { 7, 5, 37 }, { 7, 5, 38 }, { 7, 5, 39 },
                });

            migrationBuilder.DeleteData(
                table: "ReceiptResources",
                keyColumns: new[] { "ReceiptId", "ResourceTypeId", "IsInput" },
                keyValues: new object[,]
                {
                    { 35, 1, true }, { 35, 3, false },
                    { 36, 1, true }, { 36, 2, false },
                    { 37, 6, true }, { 37, 1, false },
                    { 38, 7, true }, { 38, 1, false },
                    { 39, 9, true }, { 39, 1, false },
                });

            migrationBuilder.DeleteData(
                table: "Receipts",
                keyColumn: "Id",
                keyValues: new object[] { 35, 36, 37, 38, 39 });
        }
    }
}
