using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Stage3Weather : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OutputPercent",
                table: "Manufactures",
                type: "integer",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.CreateTable(
                name: "WeatherTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LogicName = table.Column<string>(type: "text", nullable: true),
                    RotationWeight = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeatherPeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WeatherTypeId = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeatherPeriods_WeatherTypes_WeatherTypeId",
                        column: x => x.WeatherTypeId,
                        principalTable: "WeatherTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeatherTypeEffects",
                columns: table => new
                {
                    WeatherTypeId = table.Column<int>(type: "integer", nullable: false),
                    DomikTypeId = table.Column<int>(type: "integer", nullable: false),
                    OutputPercent = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherTypeEffects", x => new { x.WeatherTypeId, x.DomikTypeId });
                    table.ForeignKey(
                        name: "FK_WeatherTypeEffects_WeatherTypes_WeatherTypeId",
                        column: x => x.WeatherTypeId,
                        principalTable: "WeatherTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeatherPeriods_WeatherTypeId",
                table: "WeatherPeriods",
                column: "WeatherTypeId");

            migrationBuilder.InsertData(
                table: "WeatherTypes",
                columns: new[] { "Id", "Name", "LogicName", "RotationWeight" },
                values: new object[,]
                {
                    { 1, "Ясно", "clear", 40 },
                    { 2, "Дождь", "rain", 30 },
                    { 3, "Сушь", "drought", 30 },
                });

            migrationBuilder.Sql(
                "INSERT INTO \"WeatherTypeEffects\" (\"WeatherTypeId\", \"DomikTypeId\", \"OutputPercent\") " +
                "SELECT 2, \"Id\", 150 FROM \"DomikTypes\" WHERE \"LogicName\" = 'clay_mine';");
            migrationBuilder.Sql(
                "INSERT INTO \"WeatherTypeEffects\" (\"WeatherTypeId\", \"DomikTypeId\", \"OutputPercent\") " +
                "SELECT 2, \"Id\", 75 FROM \"DomikTypes\" WHERE \"LogicName\" = 'lumber_mill';");
            migrationBuilder.Sql(
                "INSERT INTO \"WeatherTypeEffects\" (\"WeatherTypeId\", \"DomikTypeId\", \"OutputPercent\") " +
                "SELECT 3, \"Id\", 150 FROM \"DomikTypes\" WHERE \"LogicName\" = 'lumber_mill';");
            migrationBuilder.Sql(
                "INSERT INTO \"WeatherTypeEffects\" (\"WeatherTypeId\", \"DomikTypeId\", \"OutputPercent\") " +
                "SELECT 3, \"Id\", 75 FROM \"DomikTypes\" WHERE \"LogicName\" = 'clay_mine';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeatherPeriods");

            migrationBuilder.DropTable(
                name: "WeatherTypeEffects");

            migrationBuilder.DropTable(
                name: "WeatherTypes");

            migrationBuilder.DropColumn(
                name: "OutputPercent",
                table: "Manufactures");
        }
    }
}
