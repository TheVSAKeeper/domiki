using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260707150000_Stage4Decor")]
    public partial class Stage4Decor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DecorTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LogicName = table.Column<string>(type: "text", nullable: true),
                    ComfortPoints = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecorTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DecorCosts",
                columns: table => new
                {
                    DecorTypeId = table.Column<int>(type: "integer", nullable: false),
                    ResourceTypeId = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecorCosts", x => new { x.DecorTypeId, x.ResourceTypeId });
                    table.ForeignKey(
                        name: "FK_DecorCosts_DecorTypes_DecorTypeId",
                        column: x => x.DecorTypeId,
                        principalTable: "DecorTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DecorCosts_ResourceTypes_ResourceTypeId",
                        column: x => x.ResourceTypeId,
                        principalTable: "ResourceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerDecors",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    DecorTypeId = table.Column<int>(type: "integer", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerDecors", x => new { x.PlayerId, x.DecorTypeId });
                    table.ForeignKey(
                        name: "FK_PlayerDecors_DecorTypes_DecorTypeId",
                        column: x => x.DecorTypeId,
                        principalTable: "DecorTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerDecors_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DecorCosts_ResourceTypeId",
                table: "DecorCosts",
                column: "ResourceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerDecors_DecorTypeId",
                table: "PlayerDecors",
                column: "DecorTypeId");

            migrationBuilder.Sql(@"
INSERT INTO ""DecorTypes"" (""Id"", ""Name"", ""LogicName"", ""ComfortPoints"") VALUES
    (1, 'Забор', 'fence', 2),
    (2, 'Клумба', 'flowerbed', 3),
    (3, 'Сад', 'garden', 5),
    (4, 'Фонтан', 'fountain', 8);
");

            migrationBuilder.Sql(@"
INSERT INTO ""DecorCosts"" (""DecorTypeId"", ""ResourceTypeId"", ""Value"") VALUES
    (1, 3, 10),
    (1, 2, 10),
    (2, 4, 15),
    (2, 3, 5),
    (3, 3, 20),
    (3, 7, 10),
    (4, 2, 20),
    (4, 6, 10),
    (4, 8, 5);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DecorCosts");

            migrationBuilder.DropTable(
                name: "PlayerDecors");

            migrationBuilder.DropTable(
                name: "DecorTypes");
        }
    }
}
