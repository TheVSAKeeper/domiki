using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Stage6ExpeditionLoop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LuckWeightPercent",
                table: "Traits",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ExpeditionEquipment",
                columns: table => new
                {
                    ExpeditionTypeId = table.Column<int>(type: "integer", nullable: false),
                    ResourceTypeId = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpeditionEquipment", x => new { x.ExpeditionTypeId, x.ResourceTypeId });
                    table.ForeignKey(
                        name: "FK_ExpeditionEquipment_ExpeditionTypes_ExpeditionTypeId",
                        column: x => x.ExpeditionTypeId,
                        principalTable: "ExpeditionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Traits",
                keyColumn: "Id",
                keyValue: 1,
                column: "LuckWeightPercent",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Traits",
                keyColumn: "Id",
                keyValue: 2,
                column: "LuckWeightPercent",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Traits",
                keyColumn: "Id",
                keyValue: 3,
                column: "LuckWeightPercent",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Traits",
                keyColumn: "Id",
                keyValue: 4,
                column: "LuckWeightPercent",
                value: 0);

            migrationBuilder.InsertData(
                table: "Traits",
                columns: new[] { "Id", "DurationPercent", "LogicName", "LuckWeightPercent", "Name", "NoFatigue" },
                values: new object[] { 5, 0, "lucky", 100, "Везучий", false });

            migrationBuilder.InsertData(
                table: "ExpeditionEquipment",
                columns: new[] { "ExpeditionTypeId", "ResourceTypeId", "Value" },
                values: new object[,]
                {
                    { 1, 7, 2 },
                    { 2, 7, 6 },
                });

            migrationBuilder.UpdateData(
                table: "DomikTypes",
                keyColumn: "Id",
                keyValue: 4,
                column: "MaxCount",
                value: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "DomikTypes",
                keyColumn: "Id",
                keyValue: 4,
                column: "MaxCount",
                value: 2);

            migrationBuilder.DropTable(
                name: "ExpeditionEquipment");

            migrationBuilder.DeleteData(
                table: "Traits",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DropColumn(
                name: "LuckWeightPercent",
                table: "Traits");
        }
    }
}
