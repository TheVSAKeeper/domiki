using Domiki.Web.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260710160000_BalanceBoardThroughput")]
    public partial class BalanceBoardThroughput : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Receipts\" SET \"DurationSeconds\" = 900 WHERE \"Id\" = 23;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Receipts\" SET \"DurationSeconds\" = 1800 WHERE \"Id\" = 23;");
        }
    }
}
