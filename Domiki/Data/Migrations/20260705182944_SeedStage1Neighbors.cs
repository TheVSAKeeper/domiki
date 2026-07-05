using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedStage1Neighbors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Neighbors",
                columns: new[] { "Id", "LogicName", "Name", "PrimaryResourceTypeId" },
                values: new object[,]
                {
                    { 1, "zarechye", "Заречье", 6 },
                    { 2, "borovoe", "Боровое", 7 },
                    { 3, "kamenka", "Каменка", 2 },
                    { 4, "glinischi", "Глинищи", 4 },
                    { 5, "dubrava", "Дубрава", 3 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Neighbors",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Neighbors",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Neighbors",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Neighbors",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Neighbors",
                keyColumn: "Id",
                keyValue: 5);
        }
    }
}
