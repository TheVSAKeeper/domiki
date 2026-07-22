using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class HeatstrokeRename : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "sick_types",
                keyColumn: "id",
                keyValue: 2,
                column: "name",
                value: "Жар");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "sick_types",
                keyColumn: "id",
                keyValue: 2,
                column: "name",
                value: "Перегрев");
        }
    }
}
