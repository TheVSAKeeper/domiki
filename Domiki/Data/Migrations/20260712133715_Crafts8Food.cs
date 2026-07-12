using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Crafts8Food : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FeedWorkers",
                table: "Players",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "Provisioned",
                table: "Expeditions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOptional",
                table: "ExpeditionEquipment",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.InsertData("ResourceTypes",
                columns: new[] { "Id", "Name", "LogicName" },
                values: new object[,]
                {
                    { 13, "Зерно", "grain" },
                    { 14, "Мука", "flour" },
                    { 15, "Хлеб", "bread" },
                });

            migrationBuilder.InsertData("DomikTypes",
                columns: new[] { "Id", "Name", "LogicName", "MaxCount", "UnlockLevel" },
                values: new object[,]
                {
                    { 14, "Поле", "field", 2, 5 },
                    { 15, "Мельница", "mill", 1, 12 },
                    { 16, "Пекарня", "bakery", 1, 0 },
                });

            migrationBuilder.Sql(@"INSERT INTO ""DomikTypeLevels"" (""DomikTypeId"", ""Value"", ""UpgradeSeconds"", ""MaxManufactureCount"")
SELECT t.""Id"", l.""Value"", l.""UpgradeSeconds"", l.""MaxManufactureCount""
FROM ""DomikTypeLevels"" l
CROSS JOIN (VALUES (14)) AS t(""Id"")
WHERE l.""DomikTypeId"" = (SELECT ""Id"" FROM ""DomikTypes"" WHERE ""LogicName"" = 'clay_mine')
ORDER BY t.""Id"", l.""Value"";");

            migrationBuilder.Sql(@"INSERT INTO ""DomikTypeLevelResources"" (""DomikTypeLevelDomikTypeId"", ""DomikTypeLevelValue"", ""ResourceTypeId"", ""Value"")
SELECT t.""Id"", r.""DomikTypeLevelValue"", r.""ResourceTypeId"", r.""Value""
FROM ""DomikTypeLevelResources"" r
CROSS JOIN (VALUES (14)) AS t(""Id"")
WHERE r.""DomikTypeLevelDomikTypeId"" = (SELECT ""Id"" FROM ""DomikTypes"" WHERE ""LogicName"" = 'clay_mine')
ORDER BY t.""Id"", r.""DomikTypeLevelValue"", r.""ResourceTypeId"";");

            migrationBuilder.Sql(@"INSERT INTO ""DomikTypeLevels"" (""DomikTypeId"", ""Value"", ""UpgradeSeconds"", ""MaxManufactureCount"")
SELECT t.""Id"", l.""Value"", l.""UpgradeSeconds"", l.""MaxManufactureCount""
FROM ""DomikTypeLevels"" l
CROSS JOIN (VALUES (15), (16)) AS t(""Id"")
WHERE l.""DomikTypeId"" = (SELECT ""Id"" FROM ""DomikTypes"" WHERE ""LogicName"" = 'workshop')
ORDER BY t.""Id"", l.""Value"";");

            migrationBuilder.Sql(@"INSERT INTO ""DomikTypeLevelResources"" (""DomikTypeLevelDomikTypeId"", ""DomikTypeLevelValue"", ""ResourceTypeId"", ""Value"")
SELECT t.""Id"", r.""DomikTypeLevelValue"", r.""ResourceTypeId"", r.""Value""
FROM ""DomikTypeLevelResources"" r
CROSS JOIN (VALUES (15), (16)) AS t(""Id"")
WHERE r.""DomikTypeLevelDomikTypeId"" = (SELECT ""Id"" FROM ""DomikTypes"" WHERE ""LogicName"" = 'workshop')
ORDER BY t.""Id"", r.""DomikTypeLevelValue"", r.""ResourceTypeId"";");

            migrationBuilder.Sql(@"UPDATE ""DomikTypeLevelResources"" SET ""Value"" = 150
WHERE ""DomikTypeLevelDomikTypeId"" = 15 AND ""DomikTypeLevelValue"" = 1 AND ""ResourceTypeId"" = 1;");

            migrationBuilder.InsertData("DomikTypeLevelResources",
                columns: new[] { "DomikTypeLevelDomikTypeId", "DomikTypeLevelValue", "ResourceTypeId", "Value" },
                values: new object[,]
                {
                    { 15, 1, 11, 1 },
                    { 15, 3, 11, 1 },
                    { 15, 5, 11, 2 },
                    { 16, 1, 5, 10 },
                    { 16, 4, 12, 10 },
                });

            migrationBuilder.InsertData("Receipts",
                columns: new[] { "Id", "Name", "LogicName", "DurationSeconds", "PlodderCount", "OutputBonusPercent" },
                values: new object[,]
                {
                    { 50, "Растить зерно", "grain_dig", 3600, 1, 0 },
                    { 51, "Зерно, смена 8 ч", "grain_dig_8h", 28800, 1, 0 },
                    { 52, "Зерно, смена сутки", "grain_dig_24h", 86400, 1, 0 },
                    { 53, "Смолоть муку", "make_flour", 1800, 1, 0 },
                    { 54, "Мука, смена 8 ч", "make_flour_8h", 28800, 1, 0 },
                    { 55, "Испечь хлеб", "make_bread", 3600, 1, 0 },
                    { 56, "Продать зерно", "sell_grain", 60, 1, 0 },
                    { 57, "Продать муку", "sell_flour", 60, 1, 0 },
                    { 58, "Продать хлеб", "sell_bread", 60, 1, 0 },
                });

            migrationBuilder.InsertData("ReceiptResources",
                columns: new[] { "ReceiptId", "ResourceTypeId", "IsInput", "IsOptional", "Value" },
                values: new object[,]
                {
                    { 50, 13, false, false, 1 },
                    { 51, 13, false, false, 8 },
                    { 52, 13, false, false, 24 },
                    { 53, 13, true, false, 2 }, { 53, 14, false, false, 1 },
                    { 54, 13, true, false, 16 }, { 54, 14, false, false, 8 },
                    { 55, 14, true, false, 2 }, { 55, 15, false, false, 4 },
                    { 56, 13, true, false, 1 }, { 56, 1, false, false, 10 },
                    { 57, 14, true, false, 1 }, { 57, 1, false, false, 35 },
                    { 58, 15, true, false, 1 }, { 58, 1, false, false, 20 },
                });

            migrationBuilder.InsertData("DomikTypeLevelReceipts",
                columns: new[] { "DomikTypeLevelDomikTypeId", "DomikTypeLevelValue", "ReceiptId" },
                values: new object[,]
                {
                    { 14, 1, 50 }, { 14, 2, 50 }, { 14, 3, 50 }, { 14, 4, 50 }, { 14, 5, 50 },
                    { 14, 1, 51 }, { 14, 2, 51 }, { 14, 3, 51 }, { 14, 4, 51 }, { 14, 5, 51 },
                    { 14, 1, 52 }, { 14, 2, 52 }, { 14, 3, 52 }, { 14, 4, 52 }, { 14, 5, 52 },
                    { 15, 1, 53 }, { 15, 2, 53 }, { 15, 3, 53 }, { 15, 4, 53 }, { 15, 5, 53 },
                    { 15, 2, 54 }, { 15, 3, 54 }, { 15, 4, 54 }, { 15, 5, 54 },
                    { 16, 1, 55 }, { 16, 2, 55 }, { 16, 3, 55 }, { 16, 4, 55 }, { 16, 5, 55 },
                    { 7, 1, 56 }, { 7, 2, 56 }, { 7, 3, 56 }, { 7, 4, 56 }, { 7, 5, 56 },
                    { 7, 1, 57 }, { 7, 2, 57 }, { 7, 3, 57 }, { 7, 4, 57 }, { 7, 5, 57 },
                    { 7, 1, 58 }, { 7, 2, 58 }, { 7, 3, 58 }, { 7, 4, 58 }, { 7, 5, 58 },
                });

            migrationBuilder.Sql(@"INSERT INTO ""Blueprints"" (""Id"", ""Name"", ""LogicName"", ""DomikTypeId"", ""NeighborId"", ""ReputationThreshold"")
SELECT 4, 'Чертёж пекарни', 'bakery', 16, n.""Id"", 25 FROM ""Neighbors"" n WHERE n.""LogicName"" = 'dubrava';");

            migrationBuilder.UpdateData(
                table: "Neighbors",
                keyColumn: "Id",
                keyValue: 5,
                column: "SecondaryResourceTypeId",
                value: 15);

            migrationBuilder.InsertData("WeatherTypeEffects",
                columns: new[] { "WeatherTypeId", "DomikTypeId", "OutputPercent" },
                values: new object[,]
                {
                    { 2, 14, 125 },
                    { 3, 14, 75 },
                    { 4, 16, 125 },
                });

            migrationBuilder.InsertData("TolokaTypeEffects",
                columns: new[] { "TolokaTypeId", "DomikTypeId", "OutputPercent" },
                values: new object[,]
                {
                    { 2, 14, 140 },
                    { 3, 15, 140 },
                    { 3, 16, 140 },
                });

            migrationBuilder.InsertData("ExpeditionEquipment",
                columns: new[] { "ExpeditionTypeId", "ResourceTypeId", "Value", "IsOptional" },
                values: new object[,]
                {
                    { 1, 15, 2, true },
                    { 2, 15, 5, true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
