using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class BuyPriceBridgeFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE receipt_resources SET value = 45 WHERE receipt_id IN (32, 33, 34) AND is_input = TRUE AND resource_type_id = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE receipt_resources SET value = 35 WHERE receipt_id IN (32, 33, 34) AND is_input = TRUE AND resource_type_id = 1;");
        }
    }
}
