using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Crafts9Metal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData("ResourceTypes",
                columns: new[] { "Id", "Name", "LogicName" },
                values: new object[,]
                {
                    { 16, "Руда", "ore" },
                    { 17, "Железо", "iron" },
                });

            migrationBuilder.InsertData("Receipts",
                columns: new[] { "Id", "Name", "LogicName", "DurationSeconds", "PlodderCount", "OutputBonusPercent" },
                values: new object[,]
                {
                    { 59, "Подобрать руду", "ore_dig", 3600, 1, 0 },
                    { 60, "Ломать руду (смена)", "ore_dig_8h", 28800, 1, 40 },
                    { 61, "Ломать руду (сутки)", "ore_dig_24h", 86400, 1, 40 },
                    { 62, "Выплавить железо", "make_iron", 1800, 1, 0 },
                    { 63, "Железо, смена 8 ч", "make_iron_8h", 28800, 1, 0 },
                    { 64, "Продать руду", "sell_ore", 60, 1, 0 },
                    { 65, "Продать железо", "sell_iron", 60, 1, 0 },
                    { 66, "Продать железо ×10", "sell_iron_x10", 300, 1, 0 },
                    { 67, "Продать руду ×10", "sell_ore_x10", 300, 1, 0 },
                });

            migrationBuilder.InsertData("ReceiptResources",
                columns: new[] { "ReceiptId", "ResourceTypeId", "IsInput", "IsOptional", "Value" },
                values: new object[,]
                {
                    { 59, 16, false, false, 1 },
                    { 60, 16, false, false, 8 }, { 60, 8, true, true, 1 }, { 60, 1, true, false, 8 },
                    { 61, 16, false, false, 24 }, { 61, 8, true, true, 1 }, { 61, 1, true, false, 24 },
                    { 62, 16, true, false, 2 }, { 62, 17, false, false, 1 },
                    { 63, 16, true, false, 16 }, { 63, 17, false, false, 8 },
                    { 64, 16, true, false, 1 }, { 64, 1, false, false, 10 },
                    { 65, 17, true, false, 1 }, { 65, 1, false, false, 35 },
                    { 66, 17, true, false, 10 }, { 66, 1, false, false, 350 },
                    { 67, 16, true, false, 10 }, { 67, 1, false, false, 100 },
                });

            migrationBuilder.InsertData("DomikTypeLevelReceipts",
                columns: new[] { "DomikTypeLevelDomikTypeId", "DomikTypeLevelValue", "ReceiptId" },
                values: new object[,]
                {
                    { 4, 1, 59 }, { 4, 2, 59 }, { 4, 3, 59 }, { 4, 4, 59 }, { 4, 5, 59 },
                    { 4, 1, 60 }, { 4, 2, 60 }, { 4, 3, 60 }, { 4, 4, 60 }, { 4, 5, 60 },
                    { 4, 1, 61 }, { 4, 2, 61 }, { 4, 3, 61 }, { 4, 4, 61 }, { 4, 5, 61 },
                    { 1, 1, 62 }, { 1, 2, 62 }, { 1, 3, 62 }, { 1, 4, 62 }, { 1, 5, 62 },
                    { 1, 3, 63 }, { 1, 4, 63 }, { 1, 5, 63 },
                    { 7, 1, 64 }, { 7, 2, 64 }, { 7, 3, 64 }, { 7, 4, 64 }, { 7, 5, 64 },
                    { 7, 1, 65 }, { 7, 2, 65 }, { 7, 3, 65 }, { 7, 4, 65 }, { 7, 5, 65 },
                    { 7, 5, 66 },
                    { 7, 5, 67 },
                });

            migrationBuilder.Sql("UPDATE \"ReceiptResources\" SET \"ResourceTypeId\" = 17 WHERE \"ReceiptId\" IN (24, 49) AND \"ResourceTypeId\" = 6 AND \"IsInput\" = TRUE;");
            migrationBuilder.Sql("UPDATE \"DomikTypes\" SET \"UnlockLevel\" = 20 WHERE \"LogicName\" = 'forge';");
            migrationBuilder.Sql("UPDATE \"DomikTypes\" SET \"Name\" = 'Рудник' WHERE \"Id\" = 4;");

            migrationBuilder.Sql(@"INSERT INTO ""DecorTypes"" (""Id"", ""Name"", ""LogicName"", ""ComfortPoints"", ""IsPurchasable"", ""NeighborId"", ""ReputationThreshold"") VALUES
    (9, 'Фонарь', 'lantern', 5, true, NULL, 0);");

            migrationBuilder.InsertData("DecorCosts",
                columns: new[] { "DecorTypeId", "ResourceTypeId", "Value" },
                values: new object[,]
                {
                    { 9, 17, 10 },
                    { 9, 7, 4 },
                });

            migrationBuilder.Sql(@"INSERT INTO ""ExpeditionLoot"" (""ExpeditionTypeId"", ""Kind"", ""ResourceTypeId"", ""DecorTypeId"", ""BlueprintId"", ""MinValue"", ""MaxValue"", ""Weight"", ""IsRare"") VALUES
    (1, 1, 16, NULL, NULL, 15, 28, 28, false),
    (2, 1, 16, NULL, NULL, 40, 70, 22, false);");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM \"ExpeditionLoot\" WHERE \"ExpeditionTypeId\" IN (1, 2) AND \"Kind\" = 1 AND \"ResourceTypeId\" = 16;");

            migrationBuilder.DeleteData(
                table: "DecorCosts",
                keyColumns: new[] { "DecorTypeId", "ResourceTypeId" },
                keyValues: new object[,]
                {
                    { 9, 17 },
                    { 9, 7 },
                });

            migrationBuilder.Sql("DELETE FROM \"DecorTypes\" WHERE \"Id\" = 9;");
            migrationBuilder.Sql("UPDATE \"DomikTypes\" SET \"Name\" = 'Золотой рудник' WHERE \"Id\" = 4;");
            migrationBuilder.Sql("UPDATE \"DomikTypes\" SET \"UnlockLevel\" = 10 WHERE \"LogicName\" = 'forge';");
            migrationBuilder.Sql("UPDATE \"ReceiptResources\" SET \"ResourceTypeId\" = 6 WHERE \"ReceiptId\" IN (24, 49) AND \"ResourceTypeId\" = 17 AND \"IsInput\" = TRUE;");

            migrationBuilder.DeleteData(
                table: "DomikTypeLevelReceipts",
                keyColumns: new[] { "DomikTypeLevelDomikTypeId", "DomikTypeLevelValue", "ReceiptId" },
                keyValues: new object[,]
                {
                    { 4, 1, 59 }, { 4, 2, 59 }, { 4, 3, 59 }, { 4, 4, 59 }, { 4, 5, 59 },
                    { 4, 1, 60 }, { 4, 2, 60 }, { 4, 3, 60 }, { 4, 4, 60 }, { 4, 5, 60 },
                    { 4, 1, 61 }, { 4, 2, 61 }, { 4, 3, 61 }, { 4, 4, 61 }, { 4, 5, 61 },
                    { 1, 1, 62 }, { 1, 2, 62 }, { 1, 3, 62 }, { 1, 4, 62 }, { 1, 5, 62 },
                    { 1, 3, 63 }, { 1, 4, 63 }, { 1, 5, 63 },
                    { 7, 1, 64 }, { 7, 2, 64 }, { 7, 3, 64 }, { 7, 4, 64 }, { 7, 5, 64 },
                    { 7, 1, 65 }, { 7, 2, 65 }, { 7, 3, 65 }, { 7, 4, 65 }, { 7, 5, 65 },
                    { 7, 5, 66 },
                    { 7, 5, 67 },
                });

            migrationBuilder.DeleteData(
                table: "ReceiptResources",
                keyColumns: new[] { "ReceiptId", "ResourceTypeId", "IsInput" },
                keyValues: new object[,]
                {
                    { 59, 16, false },
                    { 60, 16, false }, { 60, 8, true }, { 60, 1, true },
                    { 61, 16, false }, { 61, 8, true }, { 61, 1, true },
                    { 62, 16, true }, { 62, 17, false },
                    { 63, 16, true }, { 63, 17, false },
                    { 64, 16, true }, { 64, 1, false },
                    { 65, 17, true }, { 65, 1, false },
                    { 66, 17, true }, { 66, 1, false },
                    { 67, 16, true }, { 67, 1, false },
                });

            migrationBuilder.DeleteData(
                table: "Receipts",
                keyColumn: "Id",
                keyValues: new object[] { 59, 60, 61, 62, 63, 64, 65, 66, 67 });

            migrationBuilder.DeleteData(
                table: "ResourceTypes",
                keyColumn: "Id",
                keyValues: new object[] { 16, 17 });

        }
    }
}
