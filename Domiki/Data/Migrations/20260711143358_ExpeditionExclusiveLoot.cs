using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExpeditionExclusiveLoot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ExpeditionLoot",
                table: "ExpeditionLoot");

            migrationBuilder.AlterColumn<int>(
                name: "ResourceTypeId",
                table: "ExpeditionLoot",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Relational:ColumnOrder", 2);

            migrationBuilder.AlterColumn<int>(
                name: "ExpeditionTypeId",
                table: "ExpeditionLoot",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Relational:ColumnOrder", 1);

            migrationBuilder.AddColumn<int>(
                name: "DecorTypeId",
                table: "ExpeditionLoot",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "ExpeditionLoot",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsPurchasable",
                table: "DecorTypes",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql("ALTER TABLE \"ExpeditionLoot\" ADD COLUMN \"Id\" SERIAL;");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExpeditionLoot",
                table: "ExpeditionLoot",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ExpeditionLoot_ExpeditionTypeId",
                table: "ExpeditionLoot",
                column: "ExpeditionTypeId");

            migrationBuilder.Sql("UPDATE \"ExpeditionLoot\" SET \"Kind\" = 1;");

            migrationBuilder.Sql(@"INSERT INTO ""DecorTypes"" (""Id"", ""Name"", ""LogicName"", ""ComfortPoints"", ""IsPurchasable"") VALUES
    (6, 'Походный идол', 'trail_idol', 3, false),
    (7, 'Штандарт странников', 'wanderer_banner', 6, false);");

            migrationBuilder.Sql(@"UPDATE ""ExpeditionLoot"" SET ""MinValue"" = 15, ""MaxValue"" = 28, ""Weight"" = 28
WHERE ""ExpeditionTypeId"" = 1 AND ""ResourceTypeId"" IN (2, 3, 4);");

            migrationBuilder.Sql(@"UPDATE ""ExpeditionLoot"" SET ""MinValue"" = 2, ""MaxValue"" = 3, ""Weight"" = 8
WHERE ""ExpeditionTypeId"" = 1 AND ""ResourceTypeId"" = 8;");

            migrationBuilder.Sql(@"INSERT INTO ""ExpeditionLoot"" (""ExpeditionTypeId"", ""Kind"", ""ResourceTypeId"", ""DecorTypeId"", ""MinValue"", ""MaxValue"", ""Weight"", ""IsRare"") VALUES
    (1, 2, NULL, 6, 1, 1, 4, true);");

            migrationBuilder.Sql(@"UPDATE ""ExpeditionLoot"" SET ""MinValue"" = 40, ""MaxValue"" = 70, ""Weight"" = 22
WHERE ""ExpeditionTypeId"" = 2 AND ""ResourceTypeId"" IN (2, 3, 4);");

            migrationBuilder.Sql(@"UPDATE ""ExpeditionLoot"" SET ""MinValue"" = 3, ""MaxValue"" = 5, ""Weight"" = 12
WHERE ""ExpeditionTypeId"" = 2 AND ""ResourceTypeId"" = 8;");

            migrationBuilder.Sql(@"UPDATE ""ExpeditionLoot"" SET ""MinValue"" = 2, ""MaxValue"" = 4, ""Weight"" = 8
WHERE ""ExpeditionTypeId"" = 2 AND ""ResourceTypeId"" = 9;");

            migrationBuilder.Sql(@"INSERT INTO ""ExpeditionLoot"" (""ExpeditionTypeId"", ""Kind"", ""ResourceTypeId"", ""DecorTypeId"", ""MinValue"", ""MaxValue"", ""Weight"", ""IsRare"") VALUES
    (2, 2, NULL, 7, 1, 1, 4, true),
    (2, 3, NULL, NULL, 1, 1, 4, true);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM ""ExpeditionLoot"" WHERE ""ExpeditionTypeId"" = 1 AND ""Kind"" = 2 AND ""DecorTypeId"" = 6;");
            migrationBuilder.Sql(@"DELETE FROM ""ExpeditionLoot"" WHERE ""ExpeditionTypeId"" = 2 AND ""Kind"" IN (2, 3);");

            migrationBuilder.Sql(@"UPDATE ""ExpeditionLoot"" SET ""MinValue"" = 5, ""MaxValue"" = 12, ""Weight"" = 30
WHERE ""ExpeditionTypeId"" = 1 AND ""ResourceTypeId"" IN (2, 3, 4);");

            migrationBuilder.Sql(@"UPDATE ""ExpeditionLoot"" SET ""MinValue"" = 1, ""MaxValue"" = 2, ""Weight"" = 10
WHERE ""ExpeditionTypeId"" = 1 AND ""ResourceTypeId"" = 8;");

            migrationBuilder.Sql(@"UPDATE ""ExpeditionLoot"" SET ""MinValue"" = 12, ""MaxValue"" = 25, ""Weight"" = 25
WHERE ""ExpeditionTypeId"" = 2 AND ""ResourceTypeId"" IN (2, 3, 4);");

            migrationBuilder.Sql(@"UPDATE ""ExpeditionLoot"" SET ""MinValue"" = 2, ""MaxValue"" = 4, ""Weight"" = 15
WHERE ""ExpeditionTypeId"" = 2 AND ""ResourceTypeId"" = 8;");

            migrationBuilder.Sql(@"UPDATE ""ExpeditionLoot"" SET ""MinValue"" = 1, ""MaxValue"" = 3, ""Weight"" = 10
WHERE ""ExpeditionTypeId"" = 2 AND ""ResourceTypeId"" = 9;");

            migrationBuilder.Sql(@"DELETE FROM ""DecorTypes"" WHERE ""Id"" IN (6, 7);");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExpeditionLoot",
                table: "ExpeditionLoot");

            migrationBuilder.DropIndex(
                name: "IX_ExpeditionLoot_ExpeditionTypeId",
                table: "ExpeditionLoot");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ExpeditionLoot");

            migrationBuilder.DropColumn(
                name: "DecorTypeId",
                table: "ExpeditionLoot");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "ExpeditionLoot");

            migrationBuilder.DropColumn(
                name: "IsPurchasable",
                table: "DecorTypes");

            migrationBuilder.AlterColumn<int>(
                name: "ResourceTypeId",
                table: "ExpeditionLoot",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 2);

            migrationBuilder.AlterColumn<int>(
                name: "ExpeditionTypeId",
                table: "ExpeditionLoot",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Relational:ColumnOrder", 1);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExpeditionLoot",
                table: "ExpeditionLoot",
                columns: new[] { "ExpeditionTypeId", "ResourceTypeId" });
        }
    }
}
