using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    public partial class Crafts6Toloka : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Goal",
                table: "Tolokas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE \"Tolokas\" SET \"Goal\" = 2000 WHERE \"CompletedDate\" IS NULL;");
            migrationBuilder.Sql("UPDATE \"TolokaTypes\" SET \"Goal\" = 800;");

            migrationBuilder.CreateTable(
                name: "TolokaTypeEffects",
                columns: table => new
                {
                    TolokaTypeId = table.Column<int>(type: "integer", nullable: false),
                    DomikTypeId = table.Column<int>(type: "integer", nullable: false),
                    OutputPercent = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TolokaTypeEffects", x => new { x.TolokaTypeId, x.DomikTypeId });
                    table.ForeignKey(
                        name: "FK_TolokaTypeEffects_TolokaTypes_TolokaTypeId",
                        column: x => x.TolokaTypeId,
                        principalTable: "TolokaTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("INSERT INTO \"TolokaTypeEffects\" (\"TolokaTypeId\", \"DomikTypeId\", \"OutputPercent\") SELECT 2, \"Id\", 140 FROM \"DomikTypes\" WHERE \"LogicName\" IN ('clay_mine','lumber_mill');");
            migrationBuilder.Sql("INSERT INTO \"TolokaTypeEffects\" (\"TolokaTypeId\", \"DomikTypeId\", \"OutputPercent\") SELECT 3, \"Id\", 140 FROM \"DomikTypes\" WHERE \"LogicName\" IN ('forge','workshop');");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TolokaTypeEffects");

            migrationBuilder.Sql("UPDATE \"TolokaTypes\" SET \"Goal\" = 2000;");

            migrationBuilder.DropColumn(
                name: "Goal",
                table: "Tolokas");
        }
    }
}
