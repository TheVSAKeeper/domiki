using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    public partial class BalanceCrafts1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"ReceiptResources\" SET \"Value\" = 95 WHERE \"ReceiptId\" = 31 AND \"ResourceTypeId\" = 1 AND \"IsInput\" = FALSE;");

            migrationBuilder.Sql("INSERT INTO \"DomikTypeLevelResources\" (\"DomikTypeLevelDomikTypeId\", \"DomikTypeLevelValue\", \"ResourceTypeId\", \"Value\") VALUES (2, 4, 9, 3), (2, 5, 9, 8);");

            migrationBuilder.Sql("UPDATE \"Neighbors\" SET \"UnlockLevel\" = 10 WHERE \"Id\" IN (1, 2);");

            migrationBuilder.Sql("INSERT INTO \"DecorTypes\" (\"Id\", \"Name\", \"LogicName\", \"ComfortPoints\") VALUES (5, 'Скамейка', 'bench', 4);");
            migrationBuilder.Sql("INSERT INTO \"DecorCosts\" (\"DecorTypeId\", \"ResourceTypeId\", \"Value\") VALUES (5, 9, 2), (5, 3, 10), (5, 2, 10);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM \"DecorCosts\" WHERE \"DecorTypeId\" = 5;");
            migrationBuilder.Sql("DELETE FROM \"DecorTypes\" WHERE \"Id\" = 5;");

            migrationBuilder.Sql("UPDATE \"Neighbors\" SET \"UnlockLevel\" = 8 WHERE \"Id\" IN (1, 2);");

            migrationBuilder.Sql("DELETE FROM \"DomikTypeLevelResources\" WHERE \"DomikTypeLevelDomikTypeId\" = 2 AND \"DomikTypeLevelValue\" IN (4, 5) AND \"ResourceTypeId\" = 9;");

            migrationBuilder.Sql("UPDATE \"ReceiptResources\" SET \"Value\" = 70 WHERE \"ReceiptId\" = 31 AND \"ResourceTypeId\" = 1 AND \"IsInput\" = FALSE;");
        }
    }
}
