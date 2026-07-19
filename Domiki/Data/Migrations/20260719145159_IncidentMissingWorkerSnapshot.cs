using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class IncidentMissingWorkerSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Incidents_Workers_MissingWorkerId",
                table: "Incidents");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_MissingWorkerId",
                table: "Incidents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Incidents_MissingWorkerId",
                table: "Incidents",
                column: "MissingWorkerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Incidents_Workers_MissingWorkerId",
                table: "Incidents",
                column: "MissingWorkerId",
                principalTable: "Workers",
                principalColumn: "Id");
        }
    }
}
