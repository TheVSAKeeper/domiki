using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class TolokaBasket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TolokaTypePositions",
                columns: table => new
                {
                    TolokaTypeId = table.Column<int>(type: "integer", nullable: false),
                    ResourceTypeId = table.Column<int>(type: "integer", nullable: false),
                    Goal = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TolokaTypePositions", x => new { x.TolokaTypeId, x.ResourceTypeId });
                    table.ForeignKey(
                        name: "FK_TolokaTypePositions_TolokaTypes_TolokaTypeId",
                        column: x => x.TolokaTypeId,
                        principalTable: "TolokaTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TolokaPositions",
                columns: table => new
                {
                    TolokaId = table.Column<int>(type: "integer", nullable: false),
                    ResourceTypeId = table.Column<int>(type: "integer", nullable: false),
                    Goal = table.Column<int>(type: "integer", nullable: false),
                    Collected = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TolokaPositions", x => new { x.TolokaId, x.ResourceTypeId });
                    table.ForeignKey(
                        name: "FK_TolokaPositions_Tolokas_TolokaId",
                        column: x => x.TolokaId,
                        principalTable: "Tolokas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(@"INSERT INTO ""TolokaTypePositions"" (""TolokaTypeId"", ""ResourceTypeId"", ""Goal"") SELECT ""Id"", ""ResourceTypeId"", ""Goal"" FROM ""TolokaTypes"";");
            migrationBuilder.Sql(@"INSERT INTO ""TolokaPositions"" (""TolokaId"", ""ResourceTypeId"", ""Goal"", ""Collected"") SELECT t.""Id"", tt.""ResourceTypeId"", t.""Goal"", t.""Collected"" FROM ""Tolokas"" t JOIN ""TolokaTypes"" tt ON tt.""Id"" = t.""TolokaTypeId"";");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TolokaContributions",
                table: "TolokaContributions");

            migrationBuilder.AddColumn<int>(
                name: "ResourceTypeId",
                table: "TolokaContributions",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(@"UPDATE ""TolokaContributions"" c SET ""ResourceTypeId"" = tt.""ResourceTypeId"" FROM ""Tolokas"" t JOIN ""TolokaTypes"" tt ON tt.""Id"" = t.""TolokaTypeId"" WHERE t.""Id"" = c.""TolokaId"";");
            migrationBuilder.Sql(@"ALTER TABLE ""TolokaContributions"" ALTER COLUMN ""ResourceTypeId"" SET NOT NULL;");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TolokaContributions",
                table: "TolokaContributions",
                columns: new[] { "TolokaId", "PlayerId", "ResourceTypeId" });

            migrationBuilder.DropForeignKey(
                name: "FK_TolokaTypes_ResourceTypes_ResourceTypeId",
                table: "TolokaTypes");

            migrationBuilder.DropIndex(
                name: "IX_TolokaTypes_ResourceTypeId",
                table: "TolokaTypes");

            migrationBuilder.DropColumn(
                name: "Goal",
                table: "TolokaTypes");

            migrationBuilder.DropColumn(
                name: "ResourceTypeId",
                table: "TolokaTypes");

            migrationBuilder.DropColumn(
                name: "Collected",
                table: "Tolokas");

            migrationBuilder.DropColumn(
                name: "Goal",
                table: "Tolokas");

            migrationBuilder.Sql(@"INSERT INTO ""TolokaTypes"" (""Id"", ""Name"", ""LogicName"", ""RotationWeight"") VALUES (4, 'Торговый караван', 'caravan', 1);");
            migrationBuilder.Sql(@"INSERT INTO ""TolokaTypePositions"" (""TolokaTypeId"", ""ResourceTypeId"", ""Goal"") VALUES (4, 6, 150), (4, 7, 150), (4, 8, 50);");
            migrationBuilder.Sql(@"INSERT INTO ""TolokaTypeEffects"" (""TolokaTypeId"", ""DomikTypeId"", ""OutputPercent"") SELECT 4, ""Id"", 140 FROM ""DomikTypes"" WHERE ""LogicName"" = 'market';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM ""TolokaTypeEffects"" WHERE ""TolokaTypeId"" = 4;");
            migrationBuilder.Sql(@"DELETE FROM ""TolokaTypes"" WHERE ""Id"" = 4;");

            migrationBuilder.AddColumn<int>(
                name: "Goal",
                table: "TolokaTypes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResourceTypeId",
                table: "TolokaTypes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Collected",
                table: "Tolokas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Goal",
                table: "Tolokas",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(@"UPDATE ""TolokaTypes"" tt SET ""ResourceTypeId"" = p.""ResourceTypeId"", ""Goal"" = p.""Goal"" FROM ""TolokaTypePositions"" p WHERE p.""TolokaTypeId"" = tt.""Id"";");
            migrationBuilder.Sql(@"UPDATE ""Tolokas"" t SET ""Goal"" = p.""Goal"", ""Collected"" = p.""Collected"" FROM ""TolokaPositions"" p WHERE p.""TolokaId"" = t.""Id"";");
            migrationBuilder.Sql(@"ALTER TABLE ""TolokaTypes"" ALTER COLUMN ""ResourceTypeId"" SET NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE ""TolokaTypes"" ALTER COLUMN ""Goal"" SET NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE ""Tolokas"" ALTER COLUMN ""Collected"" SET NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE ""Tolokas"" ALTER COLUMN ""Goal"" SET NOT NULL;");

            migrationBuilder.DropTable(
                name: "TolokaPositions");

            migrationBuilder.DropTable(
                name: "TolokaTypePositions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TolokaContributions",
                table: "TolokaContributions");

            migrationBuilder.DropColumn(
                name: "ResourceTypeId",
                table: "TolokaContributions");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TolokaContributions",
                table: "TolokaContributions",
                columns: new[] { "TolokaId", "PlayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_TolokaTypes_ResourceTypeId",
                table: "TolokaTypes",
                column: "ResourceTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_TolokaTypes_ResourceTypes_ResourceTypeId",
                table: "TolokaTypes",
                column: "ResourceTypeId",
                principalTable: "ResourceTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
