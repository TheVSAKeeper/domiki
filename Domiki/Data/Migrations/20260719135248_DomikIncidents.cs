using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class DomikIncidents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Incidents_Workers_MissingWorkerId",
                table: "Incidents");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastDomikIncidentDate",
                table: "Players",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MissingWorkerId",
                table: "Incidents",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "ExpeditionTypeId",
                table: "Incidents",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "DomikTypeId",
                table: "Incidents",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceType",
                table: "Incidents",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddForeignKey(
                name: "FK_Incidents_Workers_MissingWorkerId",
                table: "Incidents",
                column: "MissingWorkerId",
                principalTable: "Workers",
                principalColumn: "Id");

            migrationBuilder.Sql("ALTER TABLE \"Incidents\" ALTER COLUMN \"SourceType\" DROP DEFAULT;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Incidents_Workers_MissingWorkerId",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "LastDomikIncidentDate",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "DomikTypeId",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "Incidents");

            migrationBuilder.AlterColumn<int>(
                name: "MissingWorkerId",
                table: "Incidents",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ExpeditionTypeId",
                table: "Incidents",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Incidents_Workers_MissingWorkerId",
                table: "Incidents",
                column: "MissingWorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
