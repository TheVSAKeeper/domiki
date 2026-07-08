using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260708160000_Stage4Market")]
    public partial class Stage4Market : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TradeLots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SellerId = table.Column<int>(type: "integer", nullable: false),
                    GiveResourceTypeId = table.Column<int>(type: "integer", nullable: false),
                    GiveValue = table.Column<int>(type: "integer", nullable: false),
                    WantResourceTypeId = table.Column<int>(type: "integer", nullable: false),
                    WantValue = table.Column<int>(type: "integer", nullable: false),
                    CommissionCoins = table.Column<int>(type: "integer", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpireDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeLots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradeLots_Players_SellerId",
                        column: x => x.SellerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TradeLots_ExpireDate",
                table: "TradeLots",
                column: "ExpireDate");

            migrationBuilder.CreateIndex(
                name: "IX_TradeLots_SellerId",
                table: "TradeLots",
                column: "SellerId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TradeLots");
        }
    }
}
