using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Crafts10ToolKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData("resource_types",
                columns: new[] { "id", "name", "logic_name", "is_food" },
                values: new object[,]
                {
                    { 22, "Кайло", "pick", false },
                    { 23, "Клещи", "tongs", false },
                });

            migrationBuilder.InsertData("receipts",
                columns: new[] { "id", "name", "logic_name", "duration_seconds", "plodder_count", "output_bonus_percent" },
                values: new object[,]
                {
                    { 76, "Сковать кайло", "make_pick", 7200, 1, 0 },
                    { 77, "Сковать клещи", "make_tongs", 3600, 1, 0 },
                    { 78, "Артелью копать глину", "clay_dig_artel", 86400, 5, 0 },
                    { 79, "Артелью ломать камень", "stone_dig_artel", 86400, 5, 0 },
                    { 80, "Артелью бить руду", "ore_dig_artel", 86400, 5, 0 },
                    { 81, "Расколоть глыбу", "split_block", 28800, 1, 0 },
                    { 82, "Обжечь большую садку", "big_kiln", 28800, 1, 0 },
                    { 83, "Пустить домницу", "blast_furnace", 28800, 1, 0 },
                });

            migrationBuilder.InsertData("receipt_resources",
                columns: new[] { "receipt_id", "resource_type_id", "is_input", "is_optional", "value" },
                values: new object[,]
                {
                    { 76, 17, true, false, 3 }, { 76, 7, true, false, 1 }, { 76, 22, false, false, 1 },
                    { 77, 17, true, false, 2 }, { 77, 7, true, false, 1 }, { 77, 23, false, false, 1 },
                    { 78, 1, true, false, 240 }, { 78, 22, true, false, 1 }, { 78, 4, false, false, 160 },
                    { 79, 1, true, false, 240 }, { 79, 22, true, false, 1 }, { 79, 2, false, false, 160 },
                    { 80, 1, true, false, 240 }, { 80, 22, true, false, 1 }, { 80, 16, false, false, 160 },
                    { 81, 2, true, false, 48 }, { 81, 22, true, false, 1 }, { 81, 10, false, false, 30 },
                    { 82, 4, true, false, 48 }, { 82, 23, true, false, 1 }, { 82, 6, false, false, 28 },
                    { 83, 16, true, false, 48 }, { 83, 23, true, false, 1 }, { 83, 17, false, false, 28 },
                });

            migrationBuilder.InsertData("domik_type_level_recepts",
                columns: new[] { "domik_type_level_domik_type_id", "domik_type_level_value", "receipt_id" },
                values: new object[,]
                {
                    { 1, 3, 76 }, { 1, 4, 76 }, { 1, 5, 76 },
                    { 1, 4, 77 }, { 1, 5, 77 },
                    { 5, 3, 78 }, { 5, 4, 78 }, { 5, 5, 78 },
                    { 3, 3, 79 }, { 3, 4, 79 }, { 3, 5, 79 },
                    { 4, 3, 80 }, { 4, 4, 80 }, { 4, 5, 80 },
                    { 12, 3, 81 }, { 12, 4, 81 }, { 12, 5, 81 },
                    { 13, 4, 82 }, { 13, 5, 82 },
                    { 1, 4, 83 }, { 1, 5, 83 },
                });

            migrationBuilder.InsertData("blueprints",
                columns: new[] { "id", "name", "logic_name", "domik_type_id", "receipt_id", "neighbor_id", "reputation_threshold" },
                values: new object[,]
                {
                    { 5, "Чертёж кайла", "pick", null, 76, 3, 40 },
                    { 6, "Чертёж клещей", "tongs", null, 77, 4, 50 },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM player_blueprints WHERE blueprint_id IN (5, 6);");
            migrationBuilder.Sql("DELETE FROM blueprints WHERE id IN (5, 6);");
            migrationBuilder.Sql("DELETE FROM domik_type_level_recepts WHERE receipt_id BETWEEN 76 AND 83;");
            migrationBuilder.Sql("DELETE FROM receipt_resources WHERE receipt_id BETWEEN 76 AND 83;");
            migrationBuilder.Sql("DELETE FROM receipts WHERE id BETWEEN 76 AND 83;");
            migrationBuilder.Sql("DELETE FROM resources WHERE type_id IN (22, 23);");
            migrationBuilder.Sql("DELETE FROM resource_types WHERE id IN (22, 23);");
        }
    }
}
