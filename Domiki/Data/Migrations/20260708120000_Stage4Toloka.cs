using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260708120000_Stage4Toloka")]
    public partial class Stage4Toloka : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TolokaTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LogicName = table.Column<string>(type: "text", nullable: true),
                    ResourceTypeId = table.Column<int>(type: "integer", nullable: false),
                    Goal = table.Column<int>(type: "integer", nullable: false),
                    RotationWeight = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TolokaTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TolokaTypes_ResourceTypes_ResourceTypeId",
                        column: x => x.ResourceTypeId,
                        principalTable: "ResourceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tolokas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TolokaTypeId = table.Column<int>(type: "integer", nullable: false),
                    Collected = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tolokas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tolokas_TolokaTypes_TolokaTypeId",
                        column: x => x.TolokaTypeId,
                        principalTable: "TolokaTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TolokaContributions",
                columns: table => new
                {
                    TolokaId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TolokaContributions", x => new { x.TolokaId, x.PlayerId });
                    table.ForeignKey(
                        name: "FK_TolokaContributions_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TolokaContributions_Tolokas_TolokaId",
                        column: x => x.TolokaId,
                        principalTable: "Tolokas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TolokaContributions_PlayerId",
                table: "TolokaContributions",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Tolokas_TolokaTypeId",
                table: "Tolokas",
                column: "TolokaTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TolokaTypes_ResourceTypeId",
                table: "TolokaTypes",
                column: "ResourceTypeId");

            migrationBuilder.Sql(@"CREATE UNIQUE INDEX ""IX_Tolokas_Active"" ON ""Tolokas"" ((true)) WHERE ""CompletedDate"" IS NULL");

            migrationBuilder.Sql(@"
INSERT INTO ""TolokaTypes"" (""Id"", ""Name"", ""LogicName"", ""ResourceTypeId"", ""Goal"", ""RotationWeight"") VALUES
    (1, 'Мост через реку', 'bridge', 2, 2000, 1),
    (2, 'Общий амбар', 'granary', 3, 2000, 1),
    (3, 'Гончарная печь', 'kiln', 4, 2000, 1);
");

            migrationBuilder.Sql(@"
INSERT INTO ""Tolokas"" (""TolokaTypeId"", ""Collected"", ""StartDate"", ""CompletedDate"") VALUES
    (1, 0, now(), NULL);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TolokaContributions");

            migrationBuilder.DropTable(
                name: "Tolokas");

            migrationBuilder.DropTable(
                name: "TolokaTypes");
        }
    }
}
