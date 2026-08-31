using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Crafts12ArtelLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var (domikTypeId, receiptId) in new[] { (5, 78), (3, 79), (4, 80) })
            {
                migrationBuilder.DeleteData("domik_type_level_recepts",
                    keyColumns: new[] { "domik_type_level_domik_type_id", "domik_type_level_value", "receipt_id" },
                    keyValues: new object[] { domikTypeId, 3, receiptId });
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
