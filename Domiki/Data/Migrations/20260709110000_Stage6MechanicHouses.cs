using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260709110000_Stage6MechanicHouses")]
    public partial class Stage6MechanicHouses : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "DomikTypes",
                columns: new[] { "Id", "Name", "LogicName", "MaxCount", "UnlockLevel" },
                columnTypes: new[] { "integer", "text", "text", "integer", "integer" },
                values: new object[,]
                {
                    { 9, "Торговый двор", "market_yard", 1, 20 },
                    { 10, "Сходня", "gathering", 1, 20 },
                    { 11, "Сторожка", "scout_hut", 1, 0 },
                });

            migrationBuilder.InsertData(
                table: "DomikTypeLevels",
                columns: new[] { "DomikTypeId", "Value", "UpgradeSeconds", "MaxManufactureCount" },
                columnTypes: new[] { "integer", "integer", "integer", "integer" },
                values: new object[,]
                {
                    { 9, 1, 120, 1 },
                    { 10, 1, 120, 1 },
                    { 11, 1, 120, 1 },
                });

            migrationBuilder.InsertData(
                table: "DomikTypeLevelResources",
                columns: new[] { "DomikTypeLevelDomikTypeId", "DomikTypeLevelValue", "ResourceTypeId", "Value" },
                columnTypes: new[] { "integer", "integer", "integer", "integer" },
                values: new object[,]
                {
                    { 9, 1, 1, 200 },
                    { 10, 1, 1, 200 },
                    { 11, 1, 1, 100 },
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DomikTypeLevelResources",
                keyColumns: new[] { "DomikTypeLevelDomikTypeId", "DomikTypeLevelValue", "ResourceTypeId" },
                keyValues: new object[] { 9, 1, 1 });

            migrationBuilder.DeleteData(
                table: "DomikTypeLevelResources",
                keyColumns: new[] { "DomikTypeLevelDomikTypeId", "DomikTypeLevelValue", "ResourceTypeId" },
                keyValues: new object[] { 10, 1, 1 });

            migrationBuilder.DeleteData(
                table: "DomikTypeLevelResources",
                keyColumns: new[] { "DomikTypeLevelDomikTypeId", "DomikTypeLevelValue", "ResourceTypeId" },
                keyValues: new object[] { 11, 1, 1 });

            migrationBuilder.DeleteData(
                table: "DomikTypeLevels",
                keyColumns: new[] { "DomikTypeId", "Value" },
                keyValues: new object[] { 9, 1 });

            migrationBuilder.DeleteData(
                table: "DomikTypeLevels",
                keyColumns: new[] { "DomikTypeId", "Value" },
                keyValues: new object[] { 10, 1 });

            migrationBuilder.DeleteData(
                table: "DomikTypeLevels",
                keyColumns: new[] { "DomikTypeId", "Value" },
                keyValues: new object[] { 11, 1 });

            migrationBuilder.DeleteData(
                table: "DomikTypes",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "DomikTypes",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "DomikTypes",
                keyColumn: "Id",
                keyValue: 11);
        }
    }
}
