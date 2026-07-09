using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    public partial class Stage6MechanicHouseRebalance : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"DomikTypeLevels\" SET \"UpgradeSeconds\" = CASE \"Value\" WHEN 2 THEN 300 WHEN 3 THEN 3600 WHEN 4 THEN 36000 WHEN 5 THEN 172800 END WHERE \"DomikTypeId\" IN (9, 10, 11) AND \"Value\" IN (2, 3, 4, 5);");
            migrationBuilder.Sql("DELETE FROM \"DomikTypeLevelResources\" WHERE \"DomikTypeLevelDomikTypeId\" IN (9, 10, 11) AND \"DomikTypeLevelValue\" IN (2, 3, 4, 5);");
            migrationBuilder.InsertData("DomikTypeLevelResources", new[] { "DomikTypeLevelDomikTypeId", "DomikTypeLevelValue", "ResourceTypeId", "Value" }, new object[,]
            {
                { 9, 2, 1, 150 }, { 9, 3, 1, 500 }, { 9, 3, 6, 10 }, { 9, 4, 1, 2000 }, { 9, 4, 6, 30 }, { 9, 4, 7, 20 }, { 9, 5, 1, 8000 }, { 9, 5, 6, 60 }, { 9, 5, 7, 40 }, { 9, 5, 5, 10 },
                { 10, 2, 1, 150 }, { 10, 3, 1, 500 }, { 10, 3, 6, 5 }, { 10, 3, 7, 5 }, { 10, 4, 1, 2000 }, { 10, 4, 6, 25 }, { 10, 4, 7, 25 }, { 10, 5, 1, 8000 }, { 10, 5, 6, 50 }, { 10, 5, 7, 50 }, { 10, 5, 5, 10 },
                { 11, 2, 1, 100 }, { 11, 3, 1, 350 }, { 11, 3, 7, 8 }, { 11, 4, 1, 1500 }, { 11, 4, 7, 25 }, { 11, 4, 6, 10 }, { 11, 5, 1, 6000 }, { 11, 5, 7, 50 }, { 11, 5, 6, 20 }, { 11, 5, 5, 8 },
            });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM \"DomikTypeLevelResources\" WHERE \"DomikTypeLevelDomikTypeId\" IN (9, 10, 11) AND \"DomikTypeLevelValue\" IN (2, 3, 4, 5);");
            migrationBuilder.InsertData("DomikTypeLevelResources", new[] { "DomikTypeLevelDomikTypeId", "DomikTypeLevelValue", "ResourceTypeId", "Value" }, new object[,]
            {
                { 9, 2, 1, 700 }, { 9, 3, 1, 2400 }, { 9, 4, 1, 8000 }, { 9, 5, 1, 25000 }, { 10, 2, 1, 700 }, { 10, 3, 1, 2400 }, { 10, 4, 1, 8000 }, { 10, 5, 1, 25000 }, { 11, 2, 1, 350 }, { 11, 3, 1, 1200 }, { 11, 4, 1, 4000 }, { 11, 5, 1, 12000 },
            });
            migrationBuilder.Sql("UPDATE \"DomikTypeLevels\" SET \"UpgradeSeconds\" = CASE \"Value\" WHEN 2 THEN 1800 WHEN 3 THEN 14400 WHEN 4 THEN 86400 WHEN 5 THEN 172800 END WHERE \"DomikTypeId\" IN (9, 10, 11) AND \"Value\" IN (2, 3, 4, 5);");
        }
    }
}
