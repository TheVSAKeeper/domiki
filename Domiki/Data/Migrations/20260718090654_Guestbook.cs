using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Guestbook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuestbookEntries",
                columns: table => new
                {
                    HostPlayerId = table.Column<int>(type: "integer", nullable: false),
                    GuestPlayerId = table.Column<int>(type: "integer", nullable: false),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    PhraseId = table.Column<int>(type: "integer", nullable: true),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestbookEntries", x => new { x.HostPlayerId, x.GuestPlayerId, x.Day });
                    table.ForeignKey(
                        name: "FK_GuestbookEntries_Players_GuestPlayerId",
                        column: x => x.GuestPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GuestbookEntries_Players_HostPlayerId",
                        column: x => x.HostPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuestbookEntries_GuestPlayerId",
                table: "GuestbookEntries",
                column: "GuestPlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuestbookEntries");
        }
    }
}
