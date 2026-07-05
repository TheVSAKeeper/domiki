using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Stage1Orders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Neighbors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LogicName = table.Column<string>(type: "text", nullable: true),
                    PrimaryResourceTypeId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Neighbors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NeighborReputations",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    NeighborId = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NeighborReputations", x => new { x.PlayerId, x.NeighborId });
                    table.ForeignKey(
                        name: "FK_NeighborReputations_Neighbors_NeighborId",
                        column: x => x.NeighborId,
                        principalTable: "Neighbors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NeighborReputations_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    NeighborId = table.Column<int>(type: "integer", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpireDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RewardCoins = table.Column<int>(type: "integer", nullable: false),
                    RewardGold = table.Column<int>(type: "integer", nullable: false),
                    RewardReputation = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Neighbors_NeighborId",
                        column: x => x.NeighborId,
                        principalTable: "Neighbors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Orders_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderResources",
                columns: table => new
                {
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    ResourceTypeId = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderResources", x => new { x.OrderId, x.ResourceTypeId });
                    table.ForeignKey(
                        name: "FK_OrderResources_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderResources_ResourceTypes_ResourceTypeId",
                        column: x => x.ResourceTypeId,
                        principalTable: "ResourceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NeighborReputations_NeighborId",
                table: "NeighborReputations",
                column: "NeighborId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderResources_ResourceTypeId",
                table: "OrderResources",
                column: "ResourceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_NeighborId",
                table: "Orders",
                column: "NeighborId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PlayerId",
                table: "Orders",
                column: "PlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NeighborReputations");

            migrationBuilder.DropTable(
                name: "OrderResources");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Neighbors");
        }
    }
}
