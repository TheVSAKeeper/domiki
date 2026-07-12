using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class FtueEconomy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DomikTypeCountGates",
                columns: table => new
                {
                    DomikTypeId = table.Column<int>(type: "integer", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    UnlockLevel = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DomikTypeCountGates", x => new { x.DomikTypeId, x.Ordinal });
                });

            migrationBuilder.Sql("DELETE FROM \"ReceiptResources\" WHERE \"ReceiptId\" IN (1, 3, 4, 5) AND \"ResourceTypeId\" = 1 AND \"IsInput\" = TRUE;");
            migrationBuilder.Sql("UPDATE \"DomikTypes\" SET \"UnlockLevel\" = 6 WHERE \"Id\" = 3;");

            migrationBuilder.InsertData("DomikTypeCountGates",
                columns: new[] { "DomikTypeId", "Ordinal", "UnlockLevel" },
                values: new object[,]
                {
                    { 2, 2, 5 },
                    { 2, 3, 10 },
                    { 2, 4, 16 },
                    { 2, 5, 24 },
                    { 5, 2, 8 },
                    { 6, 2, 8 },
                    { 3, 2, 12 },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DomikTypeCountGates");
        }
    }
}
