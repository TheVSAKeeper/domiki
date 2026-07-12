using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    public partial class Crafts7BlueprintLoot : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData("ExpeditionLoot",
                columns: new[] { "ExpeditionTypeId", "Kind", "ResourceTypeId", "DecorTypeId", "BlueprintId", "MinValue", "MaxValue", "Weight", "IsRare" },
                values: new object[,]
                {
                    { 1, 4, null, null, null, 1, 1, 4, true },
                    { 2, 4, null, null, null, 1, 1, 6, true },
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
