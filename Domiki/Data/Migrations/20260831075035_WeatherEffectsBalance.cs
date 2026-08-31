using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class WeatherEffectsBalance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "INSERT INTO weather_type_effects (weather_type_id, domik_type_id, output_percent) " +
                "SELECT w.id, d.id, 125 FROM weather_types w, domik_types d WHERE w.logic_name = 'drought' AND d.logic_name = 'stone_mine';");
            migrationBuilder.Sql(
                "INSERT INTO weather_type_effects (weather_type_id, domik_type_id, output_percent) " +
                "SELECT w.id, d.id, 150 FROM weather_types w, domik_types d WHERE w.logic_name = 'wind' AND d.logic_name = 'mill';");
            migrationBuilder.Sql(
                "INSERT INTO weather_type_effects (weather_type_id, domik_type_id, output_percent) " +
                "SELECT w.id, d.id, 75 FROM weather_types w, domik_types d WHERE w.logic_name = 'rain' AND d.logic_name = 'mill';");
            migrationBuilder.Sql(
                "INSERT INTO weather_type_effects (weather_type_id, domik_type_id, output_percent) " +
                "SELECT w.id, d.id, 75 FROM weather_types w, domik_types d WHERE w.logic_name = 'wind' AND d.logic_name = 'bakery';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DELETE FROM weather_type_effects e USING weather_types w, domik_types d " +
                "WHERE w.id = e.weather_type_id AND d.id = e.domik_type_id " +
                "AND (w.logic_name, d.logic_name) IN (('drought', 'stone_mine'), ('wind', 'mill'), ('rain', 'mill'), ('wind', 'bakery'));");
        }
    }
}
