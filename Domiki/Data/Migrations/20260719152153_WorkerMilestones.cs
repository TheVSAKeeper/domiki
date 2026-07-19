using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class WorkerMilestones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExpeditionCount",
                table: "Workers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "HireDate",
                table: "Workers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now() at time zone 'utc'");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastWorkerMilestoneDate",
                table: "Players",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkerMilestones",
                columns: table => new
                {
                    WorkerId = table.Column<int>(type: "integer", nullable: false),
                    MilestoneType = table.Column<int>(type: "integer", nullable: false),
                    GrantDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerMilestones", x => new { x.WorkerId, x.MilestoneType });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkerMilestones");

            migrationBuilder.DropColumn(
                name: "ExpeditionCount",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "HireDate",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "LastWorkerMilestoneDate",
                table: "Players");
        }
    }
}
