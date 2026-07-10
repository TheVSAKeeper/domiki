using Domiki.Web.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260710130000_BalancePacing")]
    public partial class BalancePacing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"DomikTypeLevelResources\" SET \"Value\" = CASE \"DomikTypeLevelDomikTypeId\" WHEN 1 THEN 400 WHEN 2 THEN 20 WHEN 3 THEN 120 WHEN 4 THEN 1000 WHEN 5 THEN 30 WHEN 6 THEN 30 WHEN 7 THEN 50 WHEN 8 THEN 600 WHEN 9 THEN 800 WHEN 10 THEN 800 WHEN 11 THEN 250 END WHERE \"DomikTypeLevelValue\" = 1 AND \"ResourceTypeId\" = 1;");
            migrationBuilder.Sql("UPDATE \"DomikTypes\" SET \"UnlockLevel\" = CASE \"Id\" WHEN 1 THEN 10 WHEN 4 THEN 20 WHEN 9 THEN 30 WHEN 10 THEN 30 END WHERE \"Id\" IN (1, 4, 9, 10);");

            migrationBuilder.Sql("INSERT INTO \"Receipts\" (\"Id\", \"Name\", \"LogicName\", \"DurationSeconds\", \"PlodderCount\") VALUES (32, 'Купить камень', 'buy_stone', 60, 1), (33, 'Купить дерево', 'buy_wood', 60, 1), (34, 'Купить глину', 'buy_clay', 60, 1);");
            migrationBuilder.Sql("INSERT INTO \"ReceiptResources\" (\"ReceiptId\", \"ResourceTypeId\", \"IsInput\", \"Value\") VALUES (32, 1, TRUE, 35), (32, 2, FALSE, 1), (33, 1, TRUE, 35), (33, 3, FALSE, 1), (34, 1, TRUE, 35), (34, 4, FALSE, 1);");
            migrationBuilder.Sql("INSERT INTO \"DomikTypeLevelReceipts\" (\"DomikTypeLevelDomikTypeId\", \"DomikTypeLevelValue\", \"ReceiptId\") VALUES (7, 3, 32), (7, 3, 33), (7, 3, 34), (7, 4, 32), (7, 4, 33), (7, 4, 34), (7, 5, 32), (7, 5, 33), (7, 5, 34);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM \"DomikTypeLevelReceipts\" WHERE \"DomikTypeLevelDomikTypeId\" = 7 AND \"DomikTypeLevelValue\" IN (3, 4, 5) AND \"ReceiptId\" IN (32, 33, 34);");
            migrationBuilder.Sql("DELETE FROM \"ReceiptResources\" WHERE \"ReceiptId\" IN (32, 33, 34);");
            migrationBuilder.Sql("DELETE FROM \"Receipts\" WHERE \"Id\" IN (32, 33, 34);");
            migrationBuilder.Sql("UPDATE \"DomikTypeLevelResources\" SET \"Value\" = CASE \"DomikTypeLevelDomikTypeId\" WHEN 9 THEN 200 WHEN 10 THEN 200 WHEN 11 THEN 100 ELSE 20 END WHERE \"DomikTypeLevelValue\" = 1 AND \"ResourceTypeId\" = 1 AND \"DomikTypeLevelDomikTypeId\" IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11);");
            migrationBuilder.Sql("UPDATE \"DomikTypes\" SET \"UnlockLevel\" = CASE \"Id\" WHEN 1 THEN 8 WHEN 4 THEN 15 WHEN 9 THEN 20 WHEN 10 THEN 20 END WHERE \"Id\" IN (1, 4, 9, 10);");
        }
    }
}
