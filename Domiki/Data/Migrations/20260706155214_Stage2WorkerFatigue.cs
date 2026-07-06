using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Stage2WorkerFatigue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "DomikTypeId",
                table: "WorkerSkills",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Relational:ColumnOrder", 2);

            migrationBuilder.AlterColumn<int>(
                name: "WorkerId",
                table: "WorkerSkills",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Relational:ColumnOrder", 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "RestUntil",
                table: "Workers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkedSeconds",
                table: "Workers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "NoFatigue",
                table: "Traits",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Traits",
                keyColumn: "Id",
                keyValue: 1,
                column: "NoFatigue",
                value: false);

            migrationBuilder.UpdateData(
                table: "Traits",
                keyColumn: "Id",
                keyValue: 2,
                column: "NoFatigue",
                value: false);

            migrationBuilder.UpdateData(
                table: "Traits",
                keyColumn: "Id",
                keyValue: 3,
                column: "NoFatigue",
                value: false);

            migrationBuilder.InsertData(
                table: "Traits",
                columns: new[] { "Id", "DurationPercent", "LogicName", "Name", "NoFatigue" },
                values: new object[] { 4, 15, "sonya", "Соня", true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Traits",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DropColumn(
                name: "RestUntil",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "WorkedSeconds",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "NoFatigue",
                table: "Traits");

            migrationBuilder.AlterColumn<int>(
                name: "DomikTypeId",
                table: "WorkerSkills",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Relational:ColumnOrder", 2);

            migrationBuilder.AlterColumn<int>(
                name: "WorkerId",
                table: "WorkerSkills",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Relational:ColumnOrder", 1);
        }
    }
}
