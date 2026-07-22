using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ArtisanDecor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "max_count",
                table: "decor_types",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "requires_decor_type_id",
                table: "decor_types",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_decor_types_requires_decor_type_id",
                table: "decor_types",
                column: "requires_decor_type_id");

            migrationBuilder.AddForeignKey(
                name: "fk_decor_types_decor_types_requires_decor_type_id",
                table: "decor_types",
                column: "requires_decor_type_id",
                principalTable: "decor_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.InsertData("decor_types",
                columns: new[] { "id", "name", "logic_name", "comfort_points", "max_count", "is_purchasable", "neighbor_id", "reputation_threshold", "requires_decor_type_id" },
                values: new object[,]
                {
                    { 10, "Резная калитка", "carved_gate", 2, 1, true, null, 0, null },
                    { 11, "Колодец-журавль", "crane_well", 0, 1, true, null, 0, 10 },
                    { 12, "Беседка", "gazebo", 0, 1, true, null, 0, 11 },
                    { 13, "Пруд с карасями", "carp_pond", 0, 1, true, null, 0, 12 },
                });

            migrationBuilder.InsertData("decor_costs",
                columns: new[] { "decor_type_id", "resource_type_id", "value" },
                values: new object[,]
                {
                    { 10, 1, 600 },
                    { 11, 1, 4000 },
                    { 12, 1, 25000 },
                    { 13, 1, 70000 },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "decor_costs",
                keyColumns: new[] { "decor_type_id", "resource_type_id" },
                keyValues: new object[,]
                {
                    { 10, 1 },
                    { 11, 1 },
                    { 12, 1 },
                    { 13, 1 },
                });

            migrationBuilder.DeleteData(
                table: "decor_types",
                keyColumn: "id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "decor_types",
                keyColumn: "id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "decor_types",
                keyColumn: "id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "decor_types",
                keyColumn: "id",
                keyValue: 10);

            migrationBuilder.DropForeignKey(
                name: "fk_decor_types_decor_types_requires_decor_type_id",
                table: "decor_types");

            migrationBuilder.DropIndex(
                name: "ix_decor_types_requires_decor_type_id",
                table: "decor_types");

            migrationBuilder.DropColumn(
                name: "max_count",
                table: "decor_types");

            migrationBuilder.DropColumn(
                name: "requires_decor_type_id",
                table: "decor_types");
        }
    }
}
