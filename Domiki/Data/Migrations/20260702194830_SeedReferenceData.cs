using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedReferenceData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData("ResourceTypes",
                columns: new[] { "Id", "Name", "LogicName" },
                values: new object[,]
                {
                    { 1, "Деньга", "coin" },
                    { 2, "Камень", "stone" },
                    { 3, "Дерево", "wood" },
                    { 4, "Глина", "clay" },
                    { 5, "Золото", "gold" },
                });

            migrationBuilder.InsertData("ModificatorTypes",
                columns: new[] { "Id", "Name", "LogicName" },
                values: new object[,]
                {
                    { 1, "Работяга", "plodder" },
                });

            migrationBuilder.InsertData("DomikTypes",
                columns: new[] { "Id", "Name", "LogicName", "MaxCount" },
                values: new object[,]
                {
                    { 1, "Кузница", "forge", 1 },
                    { 2, "Барак", "barracks", 5 },
                    { 3, "Каменоломня", "stone_mine", 2 },
                    { 4, "Золотой рудник", "gold_mine", 2 },
                    { 5, "Глиняный карьер", "clay_mine", 2 },
                    { 6, "Лесопилка", "lumber_mill", 2 },
                    { 7, "Магазин", "market", 1 },
                });

            migrationBuilder.InsertData("Receipts",
                columns: new[] { "Id", "Name", "LogicName", "DurationSeconds", "PlodderCount" },
                values: new object[,]
                {
                    { 1, "Копать глину", "clay_dig", 3600, 1 },
                    { 2, "Толпой копать глину", "clay_dig_together", 3600, 5 },
                    { 3, "Надоблить золотишка", "gold_dig", 3600, 1 },
                    { 4, "Подобрать камень", "stone_dig", 3600, 1 },
                    { 5, "Срубить сосну", "wood_dig", 3600, 1 },
                    { 6, "Продать глину", "sell_clay", 60, 1 },
                    { 7, "Продать дерево", "sell_wood", 60, 1 },
                    { 8, "Продать золото", "sell_gold", 60, 1 },
                    { 9, "Продать камень", "sell_stone", 60, 1 },
                    { 10, "Продать глину x10", "sell_clay_x10", 300, 1 },
                    { 11, "Продать дерево x10", "sell_wood_x10", 300, 1 },
                    { 12, "Продать золото x10", "sell_gold_x10", 300, 1 },
                    { 13, "Продать камень x10", "sell_stone_x10", 300, 1 },
                });

            migrationBuilder.InsertData("DomikTypeLevels",
                columns: new[] { "DomikTypeId", "Value", "UpgradeSeconds", "MaxManufactureCount" },
                values: new object[,]
                {
                    { 1, 1, 60, 1 }, { 1, 2, 300, 1 }, { 1, 3, 3600, 2 }, { 1, 4, 36000, 2 }, { 1, 5, 172800, 3 },
                    { 2, 1, 60, 1 }, { 2, 2, 300, 1 }, { 2, 3, 3600, 2 }, { 2, 4, 36000, 2 }, { 2, 5, 172800, 3 },
                    { 3, 1, 60, 1 }, { 3, 2, 300, 1 }, { 3, 3, 3600, 2 }, { 3, 4, 36000, 2 }, { 3, 5, 172800, 3 },
                    { 4, 1, 60, 1 }, { 4, 2, 300, 1 }, { 4, 3, 3600, 2 }, { 4, 4, 36000, 2 }, { 4, 5, 172800, 3 },
                    { 5, 1, 60, 1 }, { 5, 2, 300, 1 }, { 5, 3, 3600, 2 }, { 5, 4, 36000, 2 }, { 5, 5, 172800, 3 },
                    { 6, 1, 60, 1 }, { 6, 2, 300, 1 }, { 6, 3, 3600, 2 }, { 6, 4, 36000, 2 }, { 6, 5, 172800, 3 },
                    { 7, 1, 60, 1 }, { 7, 2, 300, 2 }, { 7, 3, 3600, 3 }, { 7, 4, 36000, 4 }, { 7, 5, 172800, 5 },
                });

            migrationBuilder.InsertData("DomikTypeLevelModificators",
                columns: new[] { "DomikTypeLevelDomikTypeId", "DomikTypeLevelValue", "ModificatorTypeId", "Value" },
                values: new object[,]
                {
                    { 2, 1, 1, 1 },
                    { 2, 2, 1, 2 },
                    { 2, 3, 1, 3 },
                    { 2, 4, 1, 4 },
                    { 2, 5, 1, 5 },
                });

            migrationBuilder.InsertData("DomikTypeLevelReceipts",
                columns: new[] { "DomikTypeLevelDomikTypeId", "DomikTypeLevelValue", "ReceiptId" },
                values: new object[,]
                {
                    { 5, 1, 1 }, { 5, 2, 1 }, { 5, 2, 2 }, { 5, 3, 1 }, { 5, 3, 2 },
                    { 5, 4, 1 }, { 5, 4, 2 }, { 5, 5, 1 }, { 5, 5, 2 },
                    { 4, 1, 3 }, { 4, 2, 3 }, { 4, 3, 3 }, { 4, 4, 3 }, { 4, 5, 3 },
                    { 6, 1, 5 }, { 6, 2, 5 }, { 6, 3, 5 }, { 6, 4, 5 }, { 6, 5, 5 },
                    { 3, 1, 4 }, { 3, 2, 4 }, { 3, 3, 4 }, { 3, 4, 4 }, { 3, 5, 4 },
                    { 7, 1, 6 }, { 7, 1, 7 }, { 7, 1, 8 }, { 7, 1, 9 },
                    { 7, 2, 6 }, { 7, 2, 7 }, { 7, 2, 8 }, { 7, 2, 9 },
                    { 7, 3, 6 }, { 7, 3, 7 }, { 7, 3, 8 }, { 7, 3, 9 },
                    { 7, 4, 6 }, { 7, 4, 7 }, { 7, 4, 8 }, { 7, 4, 9 },
                    { 7, 5, 6 }, { 7, 5, 7 }, { 7, 5, 8 }, { 7, 5, 9 },
                    { 7, 5, 10 }, { 7, 5, 11 }, { 7, 5, 12 }, { 7, 5, 13 },
                });

            migrationBuilder.InsertData("DomikTypeLevelResources",
                columns: new[] { "DomikTypeLevelDomikTypeId", "DomikTypeLevelValue", "ResourceTypeId", "Value" },
                values: new object[,]
                {
                    { 1, 1, 1, 10 }, { 2, 1, 1, 10 }, { 3, 1, 1, 10 }, { 4, 1, 1, 10 }, { 5, 1, 1, 10 }, { 6, 1, 1, 10 }, { 7, 1, 1, 10 },
                    { 1, 2, 1, 20 }, { 2, 2, 1, 20 }, { 3, 2, 1, 20 }, { 4, 2, 1, 20 }, { 5, 2, 1, 20 }, { 6, 2, 1, 20 }, { 7, 2, 1, 20 },
                    { 1, 3, 1, 30 }, { 2, 3, 1, 30 }, { 3, 3, 1, 30 }, { 4, 3, 1, 30 }, { 5, 3, 1, 30 }, { 6, 3, 1, 30 }, { 7, 3, 1, 30 },
                    { 1, 4, 1, 40 }, { 2, 4, 1, 40 }, { 3, 4, 1, 40 }, { 4, 4, 1, 40 }, { 5, 4, 1, 40 }, { 6, 4, 1, 40 }, { 7, 4, 1, 40 },
                    { 1, 5, 1, 50 }, { 2, 5, 1, 50 }, { 3, 5, 1, 50 }, { 4, 5, 1, 50 }, { 5, 5, 1, 50 }, { 6, 5, 1, 50 }, { 7, 5, 1, 50 },
                });

            migrationBuilder.InsertData("ReceiptResources",
                columns: new[] { "ReceiptId", "ResourceTypeId", "IsInput", "Value" },
                values: new object[,]
                {
                    { 1, 1, true, 1 }, { 1, 4, false, 1 },
                    { 2, 1, true, 5 }, { 2, 4, false, 10 },
                    { 3, 1, true, 1 }, { 3, 5, false, 1 },
                    { 4, 1, true, 1 }, { 4, 3, false, 1 },
                    { 5, 1, true, 1 }, { 5, 5, false, 1 },
                    { 6, 4, true, 1 }, { 6, 1, false, 10 },
                    { 7, 3, true, 1 }, { 7, 1, false, 10 },
                    { 8, 5, true, 1 }, { 8, 1, false, 10 },
                    { 9, 2, true, 1 }, { 9, 1, false, 10 },
                    { 10, 4, true, 10 }, { 10, 1, false, 100 },
                    { 11, 3, true, 10 }, { 11, 1, false, 100 },
                    { 12, 5, true, 10 }, { 12, 1, false, 100 },
                    { 13, 2, true, 10 }, { 13, 1, false, 100 },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
