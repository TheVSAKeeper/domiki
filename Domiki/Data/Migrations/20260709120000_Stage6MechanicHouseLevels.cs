using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260709120000_Stage6MechanicHouseLevels")]
    public partial class Stage6MechanicHouseLevels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "DomikTypeLevels",
                columns: new[] { "DomikTypeId", "Value", "UpgradeSeconds", "MaxManufactureCount" },
                columnTypes: new[] { "integer", "integer", "integer", "integer" },
                values: new object[,]
                {
                    { 9, 2, 1800, 1 }, { 9, 3, 14400, 1 }, { 9, 4, 86400, 1 }, { 9, 5, 172800, 1 },
                    { 10, 2, 1800, 1 }, { 10, 3, 14400, 1 }, { 10, 4, 86400, 1 }, { 10, 5, 172800, 1 },
                    { 11, 2, 1800, 1 }, { 11, 3, 14400, 1 }, { 11, 4, 86400, 1 }, { 11, 5, 172800, 1 },
                });

            migrationBuilder.InsertData(
                table: "DomikTypeLevelResources",
                columns: new[] { "DomikTypeLevelDomikTypeId", "DomikTypeLevelValue", "ResourceTypeId", "Value" },
                columnTypes: new[] { "integer", "integer", "integer", "integer" },
                values: new object[,]
                {
                    { 9, 2, 1, 700 }, { 9, 3, 1, 2400 }, { 9, 4, 1, 8000 }, { 9, 5, 1, 25000 },
                    { 10, 2, 1, 700 }, { 10, 3, 1, 2400 }, { 10, 4, 1, 8000 }, { 10, 5, 1, 25000 },
                    { 11, 2, 1, 350 }, { 11, 3, 1, 1200 }, { 11, 4, 1, 4000 }, { 11, 5, 1, 12000 },
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var typeId in new[] { 9, 10, 11 })
            {
                foreach (var value in new[] { 2, 3, 4, 5 })
                {
                    migrationBuilder.DeleteData(
                        table: "DomikTypeLevelResources",
                        keyColumns: new[] { "DomikTypeLevelDomikTypeId", "DomikTypeLevelValue", "ResourceTypeId" },
                        keyValues: new object[] { typeId, value, 1 });

                    migrationBuilder.DeleteData(
                        table: "DomikTypeLevels",
                        keyColumns: new[] { "DomikTypeId", "Value" },
                        keyValues: new object[] { typeId, value });
                }
            }
        }
    }
}
