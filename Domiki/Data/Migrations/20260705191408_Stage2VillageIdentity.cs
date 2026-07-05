using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    public partial class Stage2VillageIdentity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CrestColor",
                table: "Players",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CrestIcon",
                table: "Players",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VillageName",
                table: "Players",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_VillageName",
                table: "Players",
                column: "VillageName",
                unique: true,
                filter: "\"VillageName\" IS NOT NULL");
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Players_VillageName",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "CrestColor",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "CrestIcon",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "VillageName",
                table: "Players");
        }
    }
}
