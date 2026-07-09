using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260709100000_Stage6GoldMineCap")]
    public partial class Stage6GoldMineCap : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"DomikTypes\" SET \"MaxCount\" = 1 WHERE \"LogicName\" = 'gold_mine';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"DomikTypes\" SET \"MaxCount\" = 2 WHERE \"LogicName\" = 'gold_mine';");
        }
    }
}
