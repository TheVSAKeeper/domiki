using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    public partial class Stage3Blueprints : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Blueprints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    LogicName = table.Column<string>(type: "text", nullable: true),
                    DomikTypeId = table.Column<int>(type: "integer", nullable: false),
                    NeighborId = table.Column<int>(type: "integer", nullable: false),
                    ReputationThreshold = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blueprints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Blueprints_DomikTypes_DomikTypeId",
                        column: x => x.DomikTypeId,
                        principalTable: "DomikTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Blueprints_Neighbors_NeighborId",
                        column: x => x.NeighborId,
                        principalTable: "Neighbors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerBlueprints",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    BlueprintId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerBlueprints", x => new { x.PlayerId, x.BlueprintId });
                    table.ForeignKey(
                        name: "FK_PlayerBlueprints_Blueprints_BlueprintId",
                        column: x => x.BlueprintId,
                        principalTable: "Blueprints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerBlueprints_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Blueprints_DomikTypeId",
                table: "Blueprints",
                column: "DomikTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Blueprints_NeighborId",
                table: "Blueprints",
                column: "NeighborId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerBlueprints_BlueprintId",
                table: "PlayerBlueprints",
                column: "BlueprintId");

            migrationBuilder.InsertData("ResourceTypes",
                columns: new[] { "Id", "Name", "LogicName" },
                values: new object[] { 9, "Мебель", "furniture" });

            migrationBuilder.InsertData("DomikTypes",
                columns: new[] { "Id", "Name", "LogicName", "MaxCount", "UnlockLevel" },
                values: new object[] { 8, "Мастерская", "workshop", 1, 0 });

            migrationBuilder.Sql(@"INSERT INTO ""DomikTypeLevels"" (""DomikTypeId"", ""Value"", ""UpgradeSeconds"", ""MaxManufactureCount"")
SELECT 8, ""Value"", ""UpgradeSeconds"", ""MaxManufactureCount""
FROM ""DomikTypeLevels""
WHERE ""DomikTypeId"" = (SELECT ""Id"" FROM ""DomikTypes"" WHERE ""LogicName"" = 'forge')
ORDER BY ""Value"";");

            migrationBuilder.Sql(@"INSERT INTO ""DomikTypeLevelResources"" (""DomikTypeLevelDomikTypeId"", ""DomikTypeLevelValue"", ""ResourceTypeId"", ""Value"")
SELECT 8, ""DomikTypeLevelValue"", ""ResourceTypeId"", ""Value""
FROM ""DomikTypeLevelResources""
WHERE ""DomikTypeLevelDomikTypeId"" = (SELECT ""Id"" FROM ""DomikTypes"" WHERE ""LogicName"" = 'forge')
ORDER BY ""DomikTypeLevelValue"", ""ResourceTypeId"";");

            migrationBuilder.InsertData("Receipts",
                columns: new[] { "Id", "Name", "LogicName", "DurationSeconds", "PlodderCount", "SpeedupPercent" },
                values: new object[,]
                {
                    { 29, "Собрать мебель", "make_furniture", 3600, 1, 0 },
                    { 30, "Собрать партию мебели", "make_furniture_8h", 28800, 1, 0 },
                    { 31, "Продать мебель", "sell_furniture", 60, 1, 0 },
                });

            migrationBuilder.InsertData("ReceiptResources",
                columns: new[] { "ReceiptId", "ResourceTypeId", "IsInput", "IsOptional", "Value" },
                values: new object[,]
                {
                    { 29, 7, true, false, 2 },
                    { 29, 9, false, false, 1 },
                    { 30, 7, true, false, 16 },
                    { 30, 9, false, false, 8 },
                    { 31, 9, true, false, 1 },
                    { 31, 1, false, false, 70 },
                });

            migrationBuilder.InsertData("DomikTypeLevelReceipts",
                columns: new[] { "DomikTypeLevelDomikTypeId", "DomikTypeLevelValue", "ReceiptId" },
                values: new object[,]
                {
                    { 8, 1, 29 }, { 8, 1, 30 },
                    { 8, 2, 29 }, { 8, 2, 30 },
                    { 8, 3, 29 }, { 8, 3, 30 },
                    { 8, 4, 29 }, { 8, 4, 30 },
                    { 8, 5, 29 }, { 8, 5, 30 },
                    { 7, 1, 31 },
                    { 7, 2, 31 },
                    { 7, 3, 31 },
                    { 7, 4, 31 },
                    { 7, 5, 31 },
                });

            migrationBuilder.Sql(@"INSERT INTO ""Blueprints"" (""Id"", ""Name"", ""LogicName"", ""DomikTypeId"", ""NeighborId"", ""ReputationThreshold"")
SELECT 1, 'Чертёж мастерской', 'workshop', dt.""Id"", n.""Id"", 30
FROM ""DomikTypes"" dt
CROSS JOIN ""Neighbors"" n
WHERE dt.""LogicName"" = 'workshop' AND n.""LogicName"" = 'borovoe';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
