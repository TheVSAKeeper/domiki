using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class BlueprintReceiptUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_blueprints_receipt_id",
                table: "blueprints");

            migrationBuilder.CreateIndex(
                name: "ix_blueprints_receipt_id",
                table: "blueprints",
                column: "receipt_id",
                unique: true,
                filter: "\"receipt_id\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_blueprints_receipt_id",
                table: "blueprints");

            migrationBuilder.CreateIndex(
                name: "ix_blueprints_receipt_id",
                table: "blueprints",
                column: "receipt_id");
        }
    }
}
