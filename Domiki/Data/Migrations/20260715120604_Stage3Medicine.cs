using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Stage3Medicine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SickUntil",
                table: "Workers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NoSick",
                table: "Traits",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SickChance",
                table: "Manufactures",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Traits",
                keyColumn: "Id",
                keyValue: 1,
                column: "NoSick",
                value: false);

            migrationBuilder.UpdateData(
                table: "Traits",
                keyColumn: "Id",
                keyValue: 2,
                column: "NoSick",
                value: false);

            migrationBuilder.UpdateData(
                table: "Traits",
                keyColumn: "Id",
                keyValue: 3,
                column: "NoSick",
                value: false);

            migrationBuilder.UpdateData(
                table: "Traits",
                keyColumn: "Id",
                keyValue: 4,
                column: "NoSick",
                value: false);

            migrationBuilder.UpdateData(
                table: "Traits",
                keyColumn: "Id",
                keyValue: 5,
                column: "NoSick",
                value: false);

            migrationBuilder.InsertData(
                table: "Traits",
                columns: new[] { "Id", "DurationPercent", "LogicName", "LuckWeightPercent", "Name", "NoFatigue", "NoSick" },
                values: new object[] { 6, 0, "hardy", 0, "Крепкий", false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Traits",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DropColumn(
                name: "SickUntil",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "NoSick",
                table: "Traits");

            migrationBuilder.DropColumn(
                name: "SickChance",
                table: "Manufactures");
        }
    }
}
