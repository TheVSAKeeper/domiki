using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Stage0OptionalTool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SpeedupPercent",
                table: "Receipts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsOptional",
                table: "ReceiptResources",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.InsertData("ReceiptResources",
                columns: new[] { "ReceiptId", "ResourceTypeId", "IsInput", "IsOptional", "Value" },
                values: new object[,]
                {
                    { 14, 8, true, true, 1 },
                    { 15, 8, true, true, 1 },
                    { 16, 8, true, true, 1 },
                    { 17, 8, true, true, 1 },
                    { 18, 8, true, true, 1 },
                    { 19, 8, true, true, 1 },
                    { 20, 8, true, true, 1 },
                    { 21, 8, true, true, 1 },
                });

            foreach (var receiptId in new[] { 14, 15, 16, 17, 18, 19, 20, 21 })
            {
                migrationBuilder.UpdateData(
                    table: "Receipts",
                    keyColumn: "Id",
                    keyValue: receiptId,
                    column: "SpeedupPercent",
                    value: 40);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ReceiptResources",
                keyColumns: new[] { "ReceiptId", "ResourceTypeId", "IsInput" },
                keyValues: new object[,]
                {
                    { 14, 8, true },
                    { 15, 8, true },
                    { 16, 8, true },
                    { 17, 8, true },
                    { 18, 8, true },
                    { 19, 8, true },
                    { 20, 8, true },
                    { 21, 8, true },
                });

            foreach (var receiptId in new[] { 14, 15, 16, 17, 18, 19, 20, 21 })
            {
                migrationBuilder.UpdateData(
                    table: "Receipts",
                    keyColumn: "Id",
                    keyValue: receiptId,
                    column: "SpeedupPercent",
                    value: 0);
            }

            migrationBuilder.DropColumn(
                name: "SpeedupPercent",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "IsOptional",
                table: "ReceiptResources");
        }
    }
}
