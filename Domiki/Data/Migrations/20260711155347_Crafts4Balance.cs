using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    public partial class Crafts4Balance : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SpeedupPercent",
                table: "Receipts",
                newName: "OutputBonusPercent");

            migrationBuilder.AddColumn<int>(
                name: "DurationSeconds",
                table: "Manufactures",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GoldMinedToday",
                table: "Players",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "GoldMinedDate",
                table: "Players",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Traits",
                keyColumn: "Id",
                keyValue: 4,
                column: "DurationPercent",
                value: 25);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OutputBonusPercent",
                table: "Receipts",
                newName: "SpeedupPercent");

            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                table: "Manufactures");

            migrationBuilder.DropColumn(
                name: "GoldMinedToday",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "GoldMinedDate",
                table: "Players");

            migrationBuilder.UpdateData(
                table: "Traits",
                keyColumn: "Id",
                keyValue: 4,
                column: "DurationPercent",
                value: 15);
        }
    }
}
