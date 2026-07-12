using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    public partial class Crafts7StoneClay : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SecondaryResourceTypeId",
                table: "Neighbors",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BlueprintId",
                table: "ExpeditionLoot",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NeighborId",
                table: "DecorTypes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReputationThreshold",
                table: "DecorTypes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Neighbors",
                keyColumn: "Id",
                keyValue: 1,
                column: "SecondaryResourceTypeId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Neighbors",
                keyColumn: "Id",
                keyValue: 2,
                column: "SecondaryResourceTypeId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Neighbors",
                keyColumn: "Id",
                keyValue: 3,
                column: "SecondaryResourceTypeId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Neighbors",
                keyColumn: "Id",
                keyValue: 4,
                column: "SecondaryResourceTypeId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Neighbors",
                keyColumn: "Id",
                keyValue: 5,
                column: "SecondaryResourceTypeId",
                value: null);

            migrationBuilder.InsertData("ResourceTypes",
                columns: new[] { "Id", "Name", "LogicName" },
                values: new object[,]
                {
                    { 10, "Блок", "block" },
                    { 11, "Жернова", "millstone" },
                    { 12, "Посуда", "dishes" },
                });

            migrationBuilder.InsertData("DomikTypes",
                columns: new[] { "Id", "Name", "LogicName", "MaxCount", "UnlockLevel" },
                values: new object[,]
                {
                    { 12, "Каменотёс", "stonecutter", 1, 0 },
                    { 13, "Гончарня", "pottery", 1, 0 },
                });

            migrationBuilder.Sql(@"INSERT INTO ""DomikTypeLevels"" (""DomikTypeId"", ""Value"", ""UpgradeSeconds"", ""MaxManufactureCount"")
SELECT t.""Id"", l.""Value"", l.""UpgradeSeconds"", l.""MaxManufactureCount""
FROM ""DomikTypeLevels"" l
CROSS JOIN (VALUES (12), (13)) AS t(""Id"")
WHERE l.""DomikTypeId"" = (SELECT ""Id"" FROM ""DomikTypes"" WHERE ""LogicName"" = 'workshop')
ORDER BY t.""Id"", l.""Value"";");

            migrationBuilder.Sql(@"INSERT INTO ""DomikTypeLevelResources"" (""DomikTypeLevelDomikTypeId"", ""DomikTypeLevelValue"", ""ResourceTypeId"", ""Value"")
SELECT t.""Id"", r.""DomikTypeLevelValue"", r.""ResourceTypeId"", r.""Value""
FROM ""DomikTypeLevelResources"" r
CROSS JOIN (VALUES (12), (13)) AS t(""Id"")
WHERE r.""DomikTypeLevelDomikTypeId"" = (SELECT ""Id"" FROM ""DomikTypes"" WHERE ""LogicName"" = 'workshop')
ORDER BY t.""Id"", r.""DomikTypeLevelValue"", r.""ResourceTypeId"";");

            migrationBuilder.InsertData("Receipts",
                columns: new[] { "Id", "Name", "LogicName", "DurationSeconds", "PlodderCount", "OutputBonusPercent" },
                values: new object[,]
                {
                    { 40, "Вытесать блок", "make_block", 1800, 1, 0 },
                    { 41, "Блоки, смена 8 ч", "make_block_8h", 28800, 1, 0 },
                    { 42, "Высечь жернова", "make_millstone", 3600, 1, 0 },
                    { 43, "Обжечь посуду", "make_dishes", 3600, 1, 0 },
                    { 44, "Продать блок", "sell_block", 60, 1, 0 },
                    { 45, "Продать посуду", "sell_dishes", 60, 1, 0 },
                    { 46, "Продать жернова", "sell_millstone", 60, 1, 0 },
                    { 47, "Продать блоки ×10", "sell_block_x10", 300, 1, 0 },
                    { 48, "Продать посуду ×10", "sell_dishes_x10", 300, 1, 0 },
                    { 49, "Инструменты, смена 8 ч", "make_tool_8h", 28800, 1, 0 },
                });

            migrationBuilder.InsertData("ReceiptResources",
                columns: new[] { "ReceiptId", "ResourceTypeId", "IsInput", "IsOptional", "Value" },
                values: new object[,]
                {
                    { 40, 2, true, false, 2 }, { 40, 10, false, false, 1 },
                    { 41, 2, true, false, 16 }, { 41, 10, false, false, 8 },
                    { 42, 10, true, false, 4 }, { 42, 11, false, false, 1 },
                    { 43, 4, true, false, 2 }, { 43, 12, false, false, 1 },
                    { 44, 10, true, false, 1 }, { 44, 1, false, false, 35 },
                    { 45, 12, true, false, 1 }, { 45, 1, false, false, 45 },
                    { 46, 11, true, false, 1 }, { 46, 1, false, false, 150 },
                    { 47, 10, true, false, 10 }, { 47, 1, false, false, 350 },
                    { 48, 12, true, false, 10 }, { 48, 1, false, false, 450 },
                    { 49, 6, true, false, 8 }, { 49, 7, true, false, 8 }, { 49, 8, false, false, 8 },
                });

            migrationBuilder.Sql(@"DELETE FROM ""DomikTypeLevelReceipts""
WHERE ""DomikTypeLevelDomikTypeId"" = 1 AND ""ReceiptId"" IN (22, 27);");

            migrationBuilder.InsertData("DomikTypeLevelReceipts",
                columns: new[] { "DomikTypeLevelDomikTypeId", "DomikTypeLevelValue", "ReceiptId" },
                values: new object[,]
                {
                    { 12, 1, 40 }, { 12, 2, 40 }, { 12, 3, 40 }, { 12, 4, 40 }, { 12, 5, 40 },
                    { 12, 2, 41 }, { 12, 3, 41 }, { 12, 4, 41 }, { 12, 5, 41 },
                    { 12, 3, 42 }, { 12, 4, 42 }, { 12, 5, 42 },
                    { 13, 1, 22 }, { 13, 2, 22 }, { 13, 3, 22 }, { 13, 4, 22 }, { 13, 5, 22 },
                    { 13, 2, 27 }, { 13, 3, 27 }, { 13, 4, 27 }, { 13, 5, 27 },
                    { 13, 1, 43 }, { 13, 2, 43 }, { 13, 3, 43 }, { 13, 4, 43 }, { 13, 5, 43 },
                    { 7, 1, 44 }, { 7, 2, 44 }, { 7, 3, 44 }, { 7, 4, 44 }, { 7, 5, 44 },
                    { 7, 1, 45 }, { 7, 2, 45 }, { 7, 3, 45 }, { 7, 4, 45 }, { 7, 5, 45 },
                    { 7, 1, 46 }, { 7, 2, 46 }, { 7, 3, 46 }, { 7, 4, 46 }, { 7, 5, 46 },
                    { 7, 5, 47 },
                    { 7, 5, 48 },
                    { 1, 1, 24 }, { 1, 2, 24 },
                    { 1, 3, 49 }, { 1, 4, 49 }, { 1, 5, 49 },
                });

            migrationBuilder.Sql(@"INSERT INTO ""Blueprints"" (""Id"", ""Name"", ""LogicName"", ""DomikTypeId"", ""NeighborId"", ""ReputationThreshold"")
SELECT 2, 'Чертёж каменотёса', 'stonecutter', 12, n.""Id"", 20 FROM ""Neighbors"" n WHERE n.""LogicName"" = 'kamenka';");
            migrationBuilder.Sql(@"INSERT INTO ""Blueprints"" (""Id"", ""Name"", ""LogicName"", ""DomikTypeId"", ""NeighborId"", ""ReputationThreshold"")
SELECT 3, 'Чертёж гончарни', 'pottery', 13, n.""Id"", 15 FROM ""Neighbors"" n WHERE n.""LogicName"" = 'glinischi';");

            migrationBuilder.Sql(@"UPDATE ""Neighbors"" SET ""SecondaryResourceTypeId"" = 12 WHERE ""LogicName"" = 'glinischi';");
            migrationBuilder.Sql(@"UPDATE ""Neighbors"" SET ""SecondaryResourceTypeId"" = 10 WHERE ""LogicName"" = 'kamenka';");
            migrationBuilder.Sql(@"UPDATE ""Neighbors"" SET ""SecondaryResourceTypeId"" = 2 WHERE ""LogicName"" = 'zarechye';");
            migrationBuilder.Sql(@"UPDATE ""Neighbors"" SET ""SecondaryResourceTypeId"" = 9 WHERE ""LogicName"" = 'borovoe';");

            migrationBuilder.Sql(@"INSERT INTO ""DecorTypes"" (""Id"", ""Name"", ""LogicName"", ""ComfortPoints"", ""IsPurchasable"", ""NeighborId"", ""ReputationThreshold"")
SELECT 8, 'Кирпичная арка', 'brick_arch', 10, true, n.""Id"", 30 FROM ""Neighbors"" n WHERE n.""LogicName"" = 'zarechye';");

            migrationBuilder.InsertData("DecorCosts",
                columns: new[] { "DecorTypeId", "ResourceTypeId", "Value" },
                values: new object[,]
                {
                    { 8, 6, 20 },
                    { 8, 10, 10 },
                });

            migrationBuilder.InsertData("TolokaTypeEffects",
                columns: new[] { "TolokaTypeId", "DomikTypeId", "OutputPercent" },
                values: new object[,]
                {
                    { 3, 12, 140 },
                    { 3, 13, 140 },
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
