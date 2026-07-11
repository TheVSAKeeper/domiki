using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class BalanceCrafts2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM \"DomikTypeLevelReceipts\" WHERE \"DomikTypeLevelDomikTypeId\" = 1 AND \"ReceiptId\" IN (23, 28);");

            migrationBuilder.Sql(@"INSERT INTO ""DomikTypeLevelReceipts"" (""DomikTypeLevelDomikTypeId"", ""DomikTypeLevelValue"", ""ReceiptId"") VALUES
(6, 2, 23), (6, 3, 23), (6, 4, 23), (6, 5, 23),
(6, 2, 28), (6, 3, 28), (6, 4, 28), (6, 5, 28);");

            migrationBuilder.Sql("DELETE FROM \"ReceiptResources\" WHERE \"ReceiptId\" IN (8, 12, 17, 21);");
            migrationBuilder.Sql("DELETE FROM \"Receipts\" WHERE \"Id\" IN (8, 12, 17, 21);");

            migrationBuilder.Sql(@"INSERT INTO ""DomikTypeLevelResources"" (""DomikTypeLevelDomikTypeId"", ""DomikTypeLevelValue"", ""ResourceTypeId"", ""Value"") VALUES
(1, 4, 6, 6), (1, 5, 6, 15), (1, 5, 7, 15), (1, 5, 8, 4),
(2, 4, 6, 6), (2, 5, 6, 15), (2, 5, 7, 15), (2, 5, 8, 4),
(3, 4, 6, 6), (3, 5, 6, 15), (3, 5, 7, 15), (3, 5, 8, 4),
(4, 4, 6, 6), (4, 5, 6, 15), (4, 5, 7, 15), (4, 5, 8, 4),
(5, 4, 6, 6), (5, 5, 6, 15), (5, 5, 7, 15), (5, 5, 8, 4),
(6, 4, 6, 6), (6, 5, 6, 15), (6, 5, 7, 15), (6, 5, 8, 4),
(7, 4, 6, 6), (7, 5, 6, 15), (7, 5, 7, 15), (7, 5, 8, 4),
(8, 4, 6, 6), (8, 5, 6, 15), (8, 5, 7, 15), (8, 5, 8, 4);");

            migrationBuilder.Sql(@"UPDATE ""DomikTypeLevelResources"" SET ""Value"" = CASE ""Value""
    WHEN 100 THEN 150
    WHEN 300 THEN 450
    WHEN 1500 THEN 2200
    WHEN 9000 THEN 13000
    END
WHERE ""DomikTypeLevelDomikTypeId"" IN (4, 7)
    AND ""DomikTypeLevelValue"" IN (2, 3, 4, 5)
    AND ""ResourceTypeId"" = 1
    AND ""Value"" IN (100, 300, 1500, 9000);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE ""DomikTypeLevelResources"" SET ""Value"" = CASE ""Value""
    WHEN 150 THEN 100
    WHEN 450 THEN 300
    WHEN 2200 THEN 1500
    WHEN 13000 THEN 9000
    END
WHERE ""DomikTypeLevelDomikTypeId"" IN (4, 7)
    AND ""DomikTypeLevelValue"" IN (2, 3, 4, 5)
    AND ""ResourceTypeId"" = 1
    AND ""Value"" IN (150, 450, 2200, 13000);");

            migrationBuilder.Sql(@"DELETE FROM ""DomikTypeLevelResources"" WHERE
    (""DomikTypeLevelValue"" = 4 AND ""ResourceTypeId"" = 6 AND ""Value"" = 6)
    OR (""DomikTypeLevelValue"" = 5 AND ""ResourceTypeId"" IN (6, 7) AND ""Value"" = 15)
    OR (""DomikTypeLevelValue"" = 5 AND ""ResourceTypeId"" = 8 AND ""Value"" = 4);");

            migrationBuilder.InsertData("Receipts",
                columns: new[] { "Id", "Name", "LogicName", "DurationSeconds", "PlodderCount", "SpeedupPercent" },
                values: new object[,]
                {
                    { 8, "Продать золото", "sell_gold", 60, 1, 0 },
                    { 12, "Продать золото x10", "sell_gold_x10", 300, 1, 0 },
                    { 17, "Мыть золото (смена)", "gold_dig_8h", 28800, 1, 40 },
                    { 21, "Мыть золото (сутки)", "gold_dig_24h", 86400, 1, 40 },
                });

            migrationBuilder.InsertData("ReceiptResources",
                columns: new[] { "ReceiptId", "ResourceTypeId", "IsInput", "IsOptional", "Value" },
                values: new object[,]
                {
                    { 8, 5, true, false, 1 },
                    { 8, 1, false, false, 10 },
                    { 12, 5, true, false, 10 },
                    { 12, 1, false, false, 100 },
                    { 17, 1, true, false, 8 },
                    { 17, 5, false, false, 8 },
                    { 17, 8, true, true, 1 },
                    { 21, 1, true, false, 24 },
                    { 21, 5, false, false, 24 },
                    { 21, 8, true, true, 1 },
                });

            migrationBuilder.Sql("DELETE FROM \"DomikTypeLevelReceipts\" WHERE \"DomikTypeLevelDomikTypeId\" = 6 AND \"ReceiptId\" IN (23, 28);");

            migrationBuilder.Sql(@"INSERT INTO ""DomikTypeLevelReceipts"" (""DomikTypeLevelDomikTypeId"", ""DomikTypeLevelValue"", ""ReceiptId"") VALUES
(1, 1, 23), (1, 2, 23), (1, 3, 23), (1, 4, 23), (1, 5, 23),
(1, 1, 28), (1, 2, 28), (1, 3, 28), (1, 4, 28), (1, 5, 28);");
        }
    }
}
