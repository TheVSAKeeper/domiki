using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    public partial class FtueGoals : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ZealCharges",
                table: "Players",
                type: "integer",
                nullable: false,
                defaultValue: 24);

            migrationBuilder.CreateTable(
                name: "PlayerGoals",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    GoalId = table.Column<int>(type: "integer", nullable: false),
                    CompleteDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerGoals", x => new { x.PlayerId, x.GoalId });
                });

            migrationBuilder.CreateTable(
                name: "StarterGoals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ConditionType = table.Column<int>(type: "integer", nullable: false),
                    Param = table.Column<int>(type: "integer", nullable: false),
                    Param2 = table.Column<int>(type: "integer", nullable: false),
                    RewardCoins = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StarterGoals", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "StarterGoals",
                columns: new[] { "Id", "ConditionType", "Name", "Ordinal", "Param", "Param2", "RewardCoins" },
                values: new object[,]
                {
                    { 1, 2, "Поставь копку глины", 1, 0, 0, 10 },
                    { 2, 1, "Купи Лавку", 2, 7, 0, 20 },
                    { 3, 3, "Продай ресурс в Лавке", 3, 0, 0, 15 },
                    { 4, 1, "Купи Лесопилку", 4, 6, 0, 20 },
                    { 5, 4, "Улучши Барак до уровня 2", 5, 2, 2, 50 },
                    { 6, 5, "Сдай заказ соседям", 6, 0, 0, 30 },
                    { 7, 2, "Поставь смену на 8 часов", 7, 28800, 0, 20 },
                    { 8, 1, "Купи Каменоломню", 8, 3, 0, 40 },
                    { 9, 6, "Достигни обжитости 10", 9, 10, 0, 50 }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerGoals");

            migrationBuilder.DropTable(
                name: "StarterGoals");

            migrationBuilder.DropColumn(
                name: "ZealCharges",
                table: "Players");
        }
    }
}
