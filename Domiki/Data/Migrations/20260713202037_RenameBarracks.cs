using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameBarracks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "StarterGoals",
                keyColumn: "Id",
                keyValue: 5,
                column: "Name",
                value: "Улучши Артельную избу до уровня 2");

            migrationBuilder.UpdateData(
                table: "DomikTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Артельная изба");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "StarterGoals",
                keyColumn: "Id",
                keyValue: 5,
                column: "Name",
                value: "Улучши Барак до уровня 2");

            migrationBuilder.UpdateData(
                table: "DomikTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Барак");
        }
    }
}
