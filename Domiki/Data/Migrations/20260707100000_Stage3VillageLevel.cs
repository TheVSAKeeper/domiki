using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260707100000_Stage3VillageLevel")]
    public partial class Stage3VillageLevel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UnlockLevel",
                table: "DomikTypes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UnlockLevel",
                table: "Neighbors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE \"DomikTypes\" SET \"UnlockLevel\" = 3 WHERE \"LogicName\" = 'stone_mine';");
            migrationBuilder.Sql("UPDATE \"DomikTypes\" SET \"UnlockLevel\" = 8 WHERE \"LogicName\" = 'forge';");
            migrationBuilder.Sql("UPDATE \"DomikTypes\" SET \"UnlockLevel\" = 15 WHERE \"LogicName\" = 'gold_mine';");

            migrationBuilder.Sql("UPDATE \"Neighbors\" SET \"UnlockLevel\" = 3 WHERE \"LogicName\" = 'kamenka';");
            migrationBuilder.Sql("UPDATE \"Neighbors\" SET \"UnlockLevel\" = 8 WHERE \"LogicName\" = 'zarechye';");
            migrationBuilder.Sql("UPDATE \"Neighbors\" SET \"UnlockLevel\" = 8 WHERE \"LogicName\" = 'borovoe';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnlockLevel",
                table: "DomikTypes");

            migrationBuilder.DropColumn(
                name: "UnlockLevel",
                table: "Neighbors");
        }
    }
}
