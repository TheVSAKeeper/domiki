using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class WeatherRiskInFlightReset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE \"manufactures\" SET \"sick_chance\" = 0, \"cloak_count\" = 0 " +
                "WHERE \"finish_date\" > now() AND (\"sick_chance\" > 0 OR \"cloak_count\" > 0);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
