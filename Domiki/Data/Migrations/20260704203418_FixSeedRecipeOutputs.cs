using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    public partial class FixSeedRecipeOutputs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ReceiptResources",
                keyColumns: new[] { "ReceiptId", "ResourceTypeId", "IsInput" },
                keyValues: new object[] { 4, 3, false });
            migrationBuilder.InsertData(
                table: "ReceiptResources",
                columns: new[] { "ReceiptId", "ResourceTypeId", "IsInput", "Value" },
                values: new object[] { 4, 2, false, 1 });

            migrationBuilder.DeleteData(
                table: "ReceiptResources",
                keyColumns: new[] { "ReceiptId", "ResourceTypeId", "IsInput" },
                keyValues: new object[] { 5, 5, false });
            migrationBuilder.InsertData(
                table: "ReceiptResources",
                columns: new[] { "ReceiptId", "ResourceTypeId", "IsInput", "Value" },
                values: new object[] { 5, 3, false, 1 });

            migrationBuilder.UpdateData(
                table: "ReceiptResources",
                keyColumns: new[] { "ReceiptId", "ResourceTypeId", "IsInput" },
                keyValues: new object[] { 2, 4, false },
                column: "Value",
                value: 8);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
