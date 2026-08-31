using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ToolBonusOnEightHourShifts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE receipts SET output_bonus_percent = 100 " +
                "WHERE logic_name IN ('clay_dig_8h', 'stone_dig_8h', 'wood_dig_8h', 'ore_dig_8h');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE receipts SET output_bonus_percent = 40 " +
                "WHERE logic_name IN ('clay_dig_8h', 'stone_dig_8h', 'wood_dig_8h', 'ore_dig_8h');");
        }
    }
}
