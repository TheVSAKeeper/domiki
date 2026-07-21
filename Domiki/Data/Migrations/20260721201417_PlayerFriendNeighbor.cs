using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class PlayerFriendNeighbor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "friend_neighbor_id",
                table: "players",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "friend_neighbor_id",
                table: "players");
        }
    }
}
