using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Crafts11ArtelShift : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var receiptId in new[] { 78, 79, 80 })
            {
                migrationBuilder.UpdateData("receipts", "id", receiptId, "duration_seconds", 72000);
                migrationBuilder.UpdateData("receipt_resources",
                    keyColumns: new[] { "receipt_id", "resource_type_id", "is_input" },
                    keyValues: new object[] { receiptId, 1, true },
                    column: "value",
                    value: 200);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
