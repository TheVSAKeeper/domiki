using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReceiptBlueprints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_blueprints_domik_types_domik_type_id",
                table: "blueprints");

            migrationBuilder.AlterColumn<int>(
                name: "domik_type_id",
                table: "blueprints",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "receipt_id",
                table: "blueprints",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_blueprints_receipt_id",
                table: "blueprints",
                column: "receipt_id");

            migrationBuilder.AddForeignKey(
                name: "fk_blueprints_domik_types_domik_type_id",
                table: "blueprints",
                column: "domik_type_id",
                principalTable: "domik_types",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_blueprints_receipts_receipt_id",
                table: "blueprints",
                column: "receipt_id",
                principalTable: "receipts",
                principalColumn: "id");

            migrationBuilder.Sql(
                "ALTER TABLE blueprints ADD CONSTRAINT ck_blueprints_target " +
                "CHECK ((domik_type_id IS NULL) <> (receipt_id IS NULL));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE blueprints DROP CONSTRAINT ck_blueprints_target;");

            migrationBuilder.DropForeignKey(
                name: "fk_blueprints_domik_types_domik_type_id",
                table: "blueprints");

            migrationBuilder.DropForeignKey(
                name: "fk_blueprints_receipts_receipt_id",
                table: "blueprints");

            migrationBuilder.DropIndex(
                name: "ix_blueprints_receipt_id",
                table: "blueprints");

            migrationBuilder.DropColumn(
                name: "receipt_id",
                table: "blueprints");

            migrationBuilder.AlterColumn<int>(
                name: "domik_type_id",
                table: "blueprints",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_blueprints_domik_types_domik_type_id",
                table: "blueprints",
                column: "domik_type_id",
                principalTable: "domik_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
