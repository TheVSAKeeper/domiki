using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    public partial class Stage1BalanceRecalibration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DomikTypeLevelReceipts",
                keyColumns: new[] { "DomikTypeLevelDomikTypeId", "DomikTypeLevelValue", "ReceiptId" },
                keyValues: new object[,]
                {
                    { 4, 1, 17 }, { 4, 1, 21 },
                    { 4, 2, 17 }, { 4, 2, 21 },
                    { 4, 3, 17 }, { 4, 3, 21 },
                    { 4, 4, 17 }, { 4, 4, 21 },
                    { 4, 5, 17 }, { 4, 5, 21 },
                    { 7, 1, 8 },
                    { 7, 2, 8 },
                    { 7, 3, 8 },
                    { 7, 4, 8 },
                    { 7, 5, 8 }, { 7, 5, 12 },
                });

            migrationBuilder.Sql("UPDATE \"DomikTypeLevelResources\" SET \"Value\" = 1500 WHERE \"DomikTypeLevelValue\" = 4 AND \"ResourceTypeId\" = 1 AND \"Value\" = 1000;");
            migrationBuilder.Sql("UPDATE \"DomikTypeLevelResources\" SET \"Value\" = 9000 WHERE \"DomikTypeLevelValue\" = 5 AND \"ResourceTypeId\" = 1 AND \"Value\" = 6000;");

            migrationBuilder.InsertData("Receipts",
                columns: new[] { "Id", "Name", "LogicName", "DurationSeconds", "PlodderCount", "SpeedupPercent" },
                values: new object[,]
                {
                    { 27, "Обжечь партию кирпича", "make_brick_8h", 28800, 1, 0 },
                    { 28, "Напилить партию досок", "make_board_8h", 28800, 1, 0 },
                });

            migrationBuilder.InsertData("ReceiptResources",
                columns: new[] { "ReceiptId", "ResourceTypeId", "IsInput", "IsOptional", "Value" },
                values: new object[,]
                {
                    { 27, 4, true, false, 16 },
                    { 27, 6, false, false, 8 },
                    { 28, 3, true, false, 16 },
                    { 28, 7, false, false, 8 },
                });

            migrationBuilder.InsertData("DomikTypeLevelReceipts",
                columns: new[] { "DomikTypeLevelDomikTypeId", "DomikTypeLevelValue", "ReceiptId" },
                values: new object[,]
                {
                    { 1, 1, 27 }, { 1, 1, 28 },
                    { 1, 2, 27 }, { 1, 2, 28 },
                    { 1, 3, 27 }, { 1, 3, 28 },
                    { 1, 4, 27 }, { 1, 4, 28 },
                    { 1, 5, 27 }, { 1, 5, 28 },
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
