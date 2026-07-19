using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class SnakeCaseNaming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_Blueprints_DomikTypes_DomikTypeId",
                table: "Blueprints");

            migrationBuilder.DropForeignKey(
                name: "FK_Blueprints_Neighbors_NeighborId",
                table: "Blueprints");

            migrationBuilder.DropForeignKey(
                name: "FK_DecorCosts_DecorTypes_DecorTypeId",
                table: "DecorCosts");

            migrationBuilder.DropForeignKey(
                name: "FK_DecorCosts_ResourceTypes_ResourceTypeId",
                table: "DecorCosts");

            migrationBuilder.DropForeignKey(
                name: "FK_DomikTypeLevelModificators_DomikTypeLevels_DomikTypeLevelDo~",
                table: "DomikTypeLevelModificators");

            migrationBuilder.DropForeignKey(
                name: "FK_DomikTypeLevelModificators_ModificatorTypes_ModificatorType~",
                table: "DomikTypeLevelModificators");

            migrationBuilder.DropForeignKey(
                name: "FK_DomikTypeLevelReceipts_DomikTypeLevels_DomikTypeLevelDomikT~",
                table: "DomikTypeLevelReceipts");

            migrationBuilder.DropForeignKey(
                name: "FK_DomikTypeLevelReceipts_Receipts_ReceiptId",
                table: "DomikTypeLevelReceipts");

            migrationBuilder.DropForeignKey(
                name: "FK_DomikTypeLevelResources_DomikTypeLevels_DomikTypeLevelDomik~",
                table: "DomikTypeLevelResources");

            migrationBuilder.DropForeignKey(
                name: "FK_DomikTypeLevelResources_ResourceTypes_ResourceTypeId",
                table: "DomikTypeLevelResources");

            migrationBuilder.DropForeignKey(
                name: "FK_DomikTypeLevels_DomikTypes_DomikTypeId",
                table: "DomikTypeLevels");

            migrationBuilder.DropForeignKey(
                name: "FK_Errands_Neighbors_NeighborId",
                table: "Errands");

            migrationBuilder.DropForeignKey(
                name: "FK_Errands_Players_PlayerId",
                table: "Errands");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpeditionEquipment_ExpeditionTypes_ExpeditionTypeId",
                table: "ExpeditionEquipment");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpeditionLoot_ExpeditionTypes_ExpeditionTypeId",
                table: "ExpeditionLoot");

            migrationBuilder.DropForeignKey(
                name: "FK_Expeditions_ExpeditionTypes_ExpeditionTypeId",
                table: "Expeditions");

            migrationBuilder.DropForeignKey(
                name: "FK_Expeditions_Players_PlayerId",
                table: "Expeditions");

            migrationBuilder.DropForeignKey(
                name: "FK_GuestbookEntries_Players_GuestPlayerId",
                table: "GuestbookEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_GuestbookEntries_Players_HostPlayerId",
                table: "GuestbookEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_Incidents_Players_PlayerId",
                table: "Incidents");

            migrationBuilder.DropForeignKey(
                name: "FK_Manufactures_Domiks_DomikPlayerId_DomikId",
                table: "Manufactures");

            migrationBuilder.DropForeignKey(
                name: "FK_NeighborReputations_Neighbors_NeighborId",
                table: "NeighborReputations");

            migrationBuilder.DropForeignKey(
                name: "FK_NeighborReputations_Players_PlayerId",
                table: "NeighborReputations");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderResources_Orders_OrderId",
                table: "OrderResources");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderResources_ResourceTypes_ResourceTypeId",
                table: "OrderResources");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Neighbors_NeighborId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Players_PlayerId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerBlueprints_Blueprints_BlueprintId",
                table: "PlayerBlueprints");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerBlueprints_Players_PlayerId",
                table: "PlayerBlueprints");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerDecors_DecorTypes_DecorTypeId",
                table: "PlayerDecors");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerDecors_Players_PlayerId",
                table: "PlayerDecors");

            migrationBuilder.DropForeignKey(
                name: "FK_ReceiptResources_Receipts_ReceiptId",
                table: "ReceiptResources");

            migrationBuilder.DropForeignKey(
                name: "FK_ReceiptResources_ResourceTypes_ResourceTypeId",
                table: "ReceiptResources");

            migrationBuilder.DropForeignKey(
                name: "FK_Resources_Players_PlayerId",
                table: "Resources");

            migrationBuilder.DropForeignKey(
                name: "FK_SeasonCounters_Players_PlayerId",
                table: "SeasonCounters");

            migrationBuilder.DropForeignKey(
                name: "FK_TolokaContributions_Players_PlayerId",
                table: "TolokaContributions");

            migrationBuilder.DropForeignKey(
                name: "FK_TolokaContributions_Tolokas_TolokaId",
                table: "TolokaContributions");

            migrationBuilder.DropForeignKey(
                name: "FK_TolokaPositions_Tolokas_TolokaId",
                table: "TolokaPositions");

            migrationBuilder.DropForeignKey(
                name: "FK_Tolokas_TolokaTypes_TolokaTypeId",
                table: "Tolokas");

            migrationBuilder.DropForeignKey(
                name: "FK_TolokaTypeEffects_TolokaTypes_TolokaTypeId",
                table: "TolokaTypeEffects");

            migrationBuilder.DropForeignKey(
                name: "FK_TolokaTypePositions_TolokaTypes_TolokaTypeId",
                table: "TolokaTypePositions");

            migrationBuilder.DropForeignKey(
                name: "FK_TolokaVotes_Tolokas_TolokaId",
                table: "TolokaVotes");

            migrationBuilder.DropForeignKey(
                name: "FK_TradeLots_Players_SellerId",
                table: "TradeLots");

            migrationBuilder.DropForeignKey(
                name: "FK_WeatherPeriods_WeatherTypes_WeatherTypeId",
                table: "WeatherPeriods");

            migrationBuilder.DropForeignKey(
                name: "FK_WeatherTypeEffects_WeatherTypes_WeatherTypeId",
                table: "WeatherTypeEffects");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkerMilestones_Workers_WorkerId",
                table: "WorkerMilestones");

            migrationBuilder.DropForeignKey(
                name: "FK_Workers_Errands_ErrandId",
                table: "Workers");

            migrationBuilder.DropForeignKey(
                name: "FK_Workers_Expeditions_ExpeditionId",
                table: "Workers");

            migrationBuilder.DropForeignKey(
                name: "FK_Workers_Incidents_IncidentId",
                table: "Workers");

            migrationBuilder.DropForeignKey(
                name: "FK_Workers_Manufactures_ManufactureId",
                table: "Workers");

            migrationBuilder.DropForeignKey(
                name: "FK_Workers_Players_PlayerId",
                table: "Workers");

            migrationBuilder.DropForeignKey(
                name: "FK_Workers_Traits_TraitId",
                table: "Workers");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkerSkills_Workers_WorkerId",
                table: "WorkerSkills");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Workers",
                table: "Workers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Traits",
                table: "Traits");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tolokas",
                table: "Tolokas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Resources",
                table: "Resources");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Receipts",
                table: "Receipts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Players",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Players_VillageName",
                table: "Players");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Orders",
                table: "Orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Neighbors",
                table: "Neighbors");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Manufactures",
                table: "Manufactures");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Incidents",
                table: "Incidents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Expeditions",
                table: "Expeditions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Errands",
                table: "Errands");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Domiks",
                table: "Domiks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Blueprints",
                table: "Blueprints");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkerSkills",
                table: "WorkerSkills");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkerMilestones",
                table: "WorkerMilestones");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WeatherTypes",
                table: "WeatherTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WeatherTypeEffects",
                table: "WeatherTypeEffects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WeatherPeriods",
                table: "WeatherPeriods");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TradeLots",
                table: "TradeLots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TolokaVotes",
                table: "TolokaVotes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TolokaTypes",
                table: "TolokaTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TolokaTypePositions",
                table: "TolokaTypePositions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TolokaTypeEffects",
                table: "TolokaTypeEffects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TolokaPositions",
                table: "TolokaPositions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TolokaContributions",
                table: "TolokaContributions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StarterGoals",
                table: "StarterGoals");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SeasonCounters",
                table: "SeasonCounters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ResourceTypes",
                table: "ResourceTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReceiptResources",
                table: "ReceiptResources");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PlayerPushSubscriptions",
                table: "PlayerPushSubscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PlayerGoals",
                table: "PlayerGoals");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PlayerEvents",
                table: "PlayerEvents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PlayerDecors",
                table: "PlayerDecors");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PlayerBlueprints",
                table: "PlayerBlueprints");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderResources",
                table: "OrderResources");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NeighborReputations",
                table: "NeighborReputations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ModificatorTypes",
                table: "ModificatorTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GuestbookEntries",
                table: "GuestbookEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExpeditionTypes",
                table: "ExpeditionTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExpeditionLoot",
                table: "ExpeditionLoot");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExpeditionEquipment",
                table: "ExpeditionEquipment");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DomikTypes",
                table: "DomikTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DomikTypeLevels",
                table: "DomikTypeLevels");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DomikTypeLevelResources",
                table: "DomikTypeLevelResources");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DomikTypeLevelReceipts",
                table: "DomikTypeLevelReceipts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DomikTypeLevelModificators",
                table: "DomikTypeLevelModificators");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DomikTypeCountGates",
                table: "DomikTypeCountGates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DecorTypes",
                table: "DecorTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DecorCosts",
                table: "DecorCosts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserTokens",
                table: "AspNetUserTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUsers",
                table: "AspNetUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserLogins",
                table: "AspNetUserLogins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserClaims",
                table: "AspNetUserClaims");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetRoles",
                table: "AspNetRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetRoleClaims",
                table: "AspNetRoleClaims");

            migrationBuilder.RenameTable(
                name: "Workers",
                newName: "workers");

            migrationBuilder.RenameTable(
                name: "Traits",
                newName: "traits");

            migrationBuilder.RenameTable(
                name: "Tolokas",
                newName: "tolokas");

            migrationBuilder.RenameTable(
                name: "Resources",
                newName: "resources");

            migrationBuilder.RenameTable(
                name: "Receipts",
                newName: "receipts");

            migrationBuilder.RenameTable(
                name: "Players",
                newName: "players");

            migrationBuilder.RenameTable(
                name: "Orders",
                newName: "orders");

            migrationBuilder.RenameTable(
                name: "Neighbors",
                newName: "neighbors");

            migrationBuilder.RenameTable(
                name: "Manufactures",
                newName: "manufactures");

            migrationBuilder.RenameTable(
                name: "Incidents",
                newName: "incidents");

            migrationBuilder.RenameTable(
                name: "Expeditions",
                newName: "expeditions");

            migrationBuilder.RenameTable(
                name: "Errands",
                newName: "errands");

            migrationBuilder.RenameTable(
                name: "Domiks",
                newName: "domiks");

            migrationBuilder.RenameTable(
                name: "Blueprints",
                newName: "blueprints");

            migrationBuilder.RenameTable(
                name: "WorkerSkills",
                newName: "worker_skills");

            migrationBuilder.RenameTable(
                name: "WorkerMilestones",
                newName: "worker_milestones");

            migrationBuilder.RenameTable(
                name: "WeatherTypes",
                newName: "weather_types");

            migrationBuilder.RenameTable(
                name: "WeatherTypeEffects",
                newName: "weather_type_effects");

            migrationBuilder.RenameTable(
                name: "WeatherPeriods",
                newName: "weather_periods");

            migrationBuilder.RenameTable(
                name: "TradeLots",
                newName: "trade_lots");

            migrationBuilder.RenameTable(
                name: "TolokaVotes",
                newName: "toloka_votes");

            migrationBuilder.RenameTable(
                name: "TolokaTypes",
                newName: "toloka_types");

            migrationBuilder.RenameTable(
                name: "TolokaTypePositions",
                newName: "toloka_type_positions");

            migrationBuilder.RenameTable(
                name: "TolokaTypeEffects",
                newName: "toloka_type_effects");

            migrationBuilder.RenameTable(
                name: "TolokaPositions",
                newName: "toloka_positions");

            migrationBuilder.RenameTable(
                name: "TolokaContributions",
                newName: "toloka_contributions");

            migrationBuilder.RenameTable(
                name: "StarterGoals",
                newName: "starter_goals");

            migrationBuilder.RenameTable(
                name: "SeasonCounters",
                newName: "season_counters");

            migrationBuilder.RenameTable(
                name: "ResourceTypes",
                newName: "resource_types");

            migrationBuilder.RenameTable(
                name: "ReceiptResources",
                newName: "receipt_resources");

            migrationBuilder.RenameTable(
                name: "PlayerPushSubscriptions",
                newName: "player_push_subscriptions");

            migrationBuilder.RenameTable(
                name: "PlayerGoals",
                newName: "player_goals");

            migrationBuilder.RenameTable(
                name: "PlayerEvents",
                newName: "player_events");

            migrationBuilder.RenameTable(
                name: "PlayerDecors",
                newName: "player_decors");

            migrationBuilder.RenameTable(
                name: "PlayerBlueprints",
                newName: "player_blueprints");

            migrationBuilder.RenameTable(
                name: "OrderResources",
                newName: "order_resources");

            migrationBuilder.RenameTable(
                name: "NeighborReputations",
                newName: "neighbor_reputations");

            migrationBuilder.RenameTable(
                name: "ModificatorTypes",
                newName: "modificator_types");

            migrationBuilder.RenameTable(
                name: "GuestbookEntries",
                newName: "guestbook_entries");

            migrationBuilder.RenameTable(
                name: "ExpeditionTypes",
                newName: "expedition_types");

            migrationBuilder.RenameTable(
                name: "ExpeditionLoot",
                newName: "expedition_loot");

            migrationBuilder.RenameTable(
                name: "ExpeditionEquipment",
                newName: "expedition_equipment");

            migrationBuilder.RenameTable(
                name: "DomikTypes",
                newName: "domik_types");

            migrationBuilder.RenameTable(
                name: "DomikTypeLevels",
                newName: "domik_type_levels");

            migrationBuilder.RenameTable(
                name: "DomikTypeLevelResources",
                newName: "domik_type_level_resources");

            migrationBuilder.RenameTable(
                name: "DomikTypeLevelReceipts",
                newName: "domik_type_level_recepts");

            migrationBuilder.RenameTable(
                name: "DomikTypeLevelModificators",
                newName: "domik_type_level_modificators");

            migrationBuilder.RenameTable(
                name: "DomikTypeCountGates",
                newName: "domik_type_count_gates");

            migrationBuilder.RenameTable(
                name: "DecorTypes",
                newName: "decor_types");

            migrationBuilder.RenameTable(
                name: "DecorCosts",
                newName: "decor_costs");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                newName: "asp_net_user_tokens");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                newName: "asp_net_users");

            migrationBuilder.RenameTable(
                name: "AspNetUserRoles",
                newName: "asp_net_user_roles");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                newName: "asp_net_user_logins");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                newName: "asp_net_user_claims");

            migrationBuilder.RenameTable(
                name: "AspNetRoles",
                newName: "asp_net_roles");

            migrationBuilder.RenameTable(
                name: "AspNetRoleClaims",
                newName: "asp_net_role_claims");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "workers",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "workers",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "WorkedSeconds",
                table: "workers",
                newName: "worked_seconds");

            migrationBuilder.RenameColumn(
                name: "TraitId",
                table: "workers",
                newName: "trait_id");

            migrationBuilder.RenameColumn(
                name: "SickUntil",
                table: "workers",
                newName: "sick_until");

            migrationBuilder.RenameColumn(
                name: "RestUntil",
                table: "workers",
                newName: "rest_until");

            migrationBuilder.RenameColumn(
                name: "PlayerId",
                table: "workers",
                newName: "player_id");

            migrationBuilder.RenameColumn(
                name: "ManufactureId",
                table: "workers",
                newName: "manufacture_id");

            migrationBuilder.RenameColumn(
                name: "IncidentId",
                table: "workers",
                newName: "incident_id");

            migrationBuilder.RenameColumn(
                name: "HireDate",
                table: "workers",
                newName: "hire_date");

            migrationBuilder.RenameColumn(
                name: "ExpeditionId",
                table: "workers",
                newName: "expedition_id");

            migrationBuilder.RenameColumn(
                name: "ExpeditionCount",
                table: "workers",
                newName: "expedition_count");

            migrationBuilder.RenameColumn(
                name: "ErrandId",
                table: "workers",
                newName: "errand_id");

            migrationBuilder.RenameIndex(
                name: "IX_Workers_TraitId",
                table: "workers",
                newName: "ix_workers_trait_id");

            migrationBuilder.RenameIndex(
                name: "IX_Workers_PlayerId",
                table: "workers",
                newName: "ix_workers_player_id");

            migrationBuilder.RenameIndex(
                name: "IX_Workers_ManufactureId",
                table: "workers",
                newName: "ix_workers_manufacture_id");

            migrationBuilder.RenameIndex(
                name: "IX_Workers_IncidentId",
                table: "workers",
                newName: "ix_workers_incident_id");

            migrationBuilder.RenameIndex(
                name: "IX_Workers_ExpeditionId",
                table: "workers",
                newName: "ix_workers_expedition_id");

            migrationBuilder.RenameIndex(
                name: "IX_Workers_ErrandId",
                table: "workers",
                newName: "ix_workers_errand_id");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "traits",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "traits",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "NoSick",
                table: "traits",
                newName: "no_sick");

            migrationBuilder.RenameColumn(
                name: "NoFatigue",
                table: "traits",
                newName: "no_fatigue");

            migrationBuilder.RenameColumn(
                name: "LuckWeightPercent",
                table: "traits",
                newName: "luck_weight_percent");

            migrationBuilder.RenameColumn(
                name: "LogicName",
                table: "traits",
                newName: "logic_name");

            migrationBuilder.RenameColumn(
                name: "DurationPercent",
                table: "traits",
                newName: "duration_percent");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "tolokas",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "TolokaTypeId",
                table: "tolokas",
                newName: "toloka_type_id");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "tolokas",
                newName: "start_date");

            migrationBuilder.RenameColumn(
                name: "CompletedDate",
                table: "tolokas",
                newName: "completed_date");

            migrationBuilder.RenameIndex(
                name: "IX_Tolokas_TolokaTypeId",
                table: "tolokas",
                newName: "ix_tolokas_toloka_type_id");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "resources",
                newName: "value");

            migrationBuilder.RenameColumn(
                name: "TypeId",
                table: "resources",
                newName: "type_id");

            migrationBuilder.RenameColumn(
                name: "PlayerId",
                table: "resources",
                newName: "player_id");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "receipts",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "receipts",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "PlodderCount",
                table: "receipts",
                newName: "plodder_count");

            migrationBuilder.RenameColumn(
                name: "OutputBonusPercent",
                table: "receipts",
                newName: "output_bonus_percent");

            migrationBuilder.RenameColumn(
                name: "LogicName",
                table: "receipts",
                newName: "logic_name");

            migrationBuilder.RenameColumn(
                name: "DurationSeconds",
                table: "receipts",
                newName: "duration_seconds");

            migrationBuilder.RenameColumn(
                name: "Version",
                table: "players",
                newName: "version");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "players",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "players",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ZealCharges",
                table: "players",
                newName: "zeal_charges");

            migrationBuilder.RenameColumn(
                name: "VisitsSinceBigGift",
                table: "players",
                newName: "visits_since_big_gift");

            migrationBuilder.RenameColumn(
                name: "VillageName",
                table: "players",
                newName: "village_name");

            migrationBuilder.RenameColumn(
                name: "NextOrderRefillAt",
                table: "players",
                newName: "next_order_refill_at");

            migrationBuilder.RenameColumn(
                name: "LastWorkerMilestoneDate",
                table: "players",
                newName: "last_worker_milestone_date");

            migrationBuilder.RenameColumn(
                name: "LastSeen",
                table: "players",
                newName: "last_seen");

            migrationBuilder.RenameColumn(
                name: "LastIncidentDate",
                table: "players",
                newName: "last_incident_date");

            migrationBuilder.RenameColumn(
                name: "LastHelpDate",
                table: "players",
                newName: "last_help_date");

            migrationBuilder.RenameColumn(
                name: "LastDomikIncidentDate",
                table: "players",
                newName: "last_domik_incident_date");

            migrationBuilder.RenameColumn(
                name: "HelpsReceivedToday",
                table: "players",
                newName: "helps_received_today");

            migrationBuilder.RenameColumn(
                name: "HelpsReceivedDate",
                table: "players",
                newName: "helps_received_date");

            migrationBuilder.RenameColumn(
                name: "GoldMinedToday",
                table: "players",
                newName: "gold_mined_today");

            migrationBuilder.RenameColumn(
                name: "GoldMinedDate",
                table: "players",
                newName: "gold_mined_date");

            migrationBuilder.RenameColumn(
                name: "FeedWorkers",
                table: "players",
                newName: "feed_workers");

            migrationBuilder.RenameColumn(
                name: "ExpeditionsSincePity",
                table: "players",
                newName: "expeditions_since_pity");

            migrationBuilder.RenameColumn(
                name: "CrestIcon",
                table: "players",
                newName: "crest_icon");

            migrationBuilder.RenameColumn(
                name: "CrestColor",
                table: "players",
                newName: "crest_color");

            migrationBuilder.RenameColumn(
                name: "AspNetUserId",
                table: "players",
                newName: "asp_net_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_Players_AspNetUserId",
                table: "players",
                newName: "ix_players_asp_net_user_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "orders",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "RewardReputation",
                table: "orders",
                newName: "reward_reputation");

            migrationBuilder.RenameColumn(
                name: "RewardGold",
                table: "orders",
                newName: "reward_gold");

            migrationBuilder.RenameColumn(
                name: "RewardCoins",
                table: "orders",
                newName: "reward_coins");

            migrationBuilder.RenameColumn(
                name: "PlayerId",
                table: "orders",
                newName: "player_id");

            migrationBuilder.RenameColumn(
                name: "NeighborId",
                table: "orders",
                newName: "neighbor_id");

            migrationBuilder.RenameColumn(
                name: "ExpireDate",
                table: "orders",
                newName: "expire_date");

            migrationBuilder.RenameColumn(
                name: "CreateDate",
                table: "orders",
                newName: "create_date");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_PlayerId",
                table: "orders",
                newName: "ix_orders_player_id");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_NeighborId",
                table: "orders",
                newName: "ix_orders_neighbor_id");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "neighbors",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "neighbors",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UnlockLevel",
                table: "neighbors",
                newName: "unlock_level");

            migrationBuilder.RenameColumn(
                name: "SecondaryResourceTypeId",
                table: "neighbors",
                newName: "secondary_resource_type_id");

            migrationBuilder.RenameColumn(
                name: "PrimaryResourceTypeId",
                table: "neighbors",
                newName: "primary_resource_type_id");

            migrationBuilder.RenameColumn(
                name: "LogicName",
                table: "neighbors",
                newName: "logic_name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "manufactures",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UseOptional",
                table: "manufactures",
                newName: "use_optional");

            migrationBuilder.RenameColumn(
                name: "SickChance",
                table: "manufactures",
                newName: "sick_chance");

            migrationBuilder.RenameColumn(
                name: "ReceiptId",
                table: "manufactures",
                newName: "receipt_id");

            migrationBuilder.RenameColumn(
                name: "PlodderCount",
                table: "manufactures",
                newName: "plodder_count");

            migrationBuilder.RenameColumn(
                name: "OutputPercent",
                table: "manufactures",
                newName: "output_percent");

            migrationBuilder.RenameColumn(
                name: "FinishDate",
                table: "manufactures",
                newName: "finish_date");

            migrationBuilder.RenameColumn(
                name: "DurationSeconds",
                table: "manufactures",
                newName: "duration_seconds");

            migrationBuilder.RenameColumn(
                name: "DomikPlayerId",
                table: "manufactures",
                newName: "domik_player_id");

            migrationBuilder.RenameColumn(
                name: "DomikId",
                table: "manufactures",
                newName: "domik_id");

            migrationBuilder.RenameColumn(
                name: "AutoRepeat",
                table: "manufactures",
                newName: "auto_repeat");

            migrationBuilder.RenameIndex(
                name: "IX_Manufactures_DomikPlayerId_DomikId",
                table: "manufactures",
                newName: "ix_manufactures_domik_player_id_domik_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "incidents",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "TemplateId",
                table: "incidents",
                newName: "template_id");

            migrationBuilder.RenameColumn(
                name: "SourceType",
                table: "incidents",
                newName: "source_type");

            migrationBuilder.RenameColumn(
                name: "SearchEndDate",
                table: "incidents",
                newName: "search_end_date");

            migrationBuilder.RenameColumn(
                name: "ResolvedDate",
                table: "incidents",
                newName: "resolved_date");

            migrationBuilder.RenameColumn(
                name: "PlayerId",
                table: "incidents",
                newName: "player_id");

            migrationBuilder.RenameColumn(
                name: "MissingWorkerId",
                table: "incidents",
                newName: "missing_worker_id");

            migrationBuilder.RenameColumn(
                name: "ExpeditionTypeId",
                table: "incidents",
                newName: "expedition_type_id");

            migrationBuilder.RenameColumn(
                name: "DomikTypeId",
                table: "incidents",
                newName: "domik_type_id");

            migrationBuilder.RenameColumn(
                name: "CreateDate",
                table: "incidents",
                newName: "create_date");

            migrationBuilder.RenameColumn(
                name: "ClueId",
                table: "incidents",
                newName: "clue_id");

            migrationBuilder.RenameIndex(
                name: "IX_Incidents_PlayerId",
                table: "incidents",
                newName: "ix_incidents_player_id");

            migrationBuilder.RenameColumn(
                name: "Provisioned",
                table: "expeditions",
                newName: "provisioned");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "expeditions",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "expeditions",
                newName: "start_date");

            migrationBuilder.RenameColumn(
                name: "PlayerId",
                table: "expeditions",
                newName: "player_id");

            migrationBuilder.RenameColumn(
                name: "FinishDate",
                table: "expeditions",
                newName: "finish_date");

            migrationBuilder.RenameColumn(
                name: "ExpeditionTypeId",
                table: "expeditions",
                newName: "expedition_type_id");

            migrationBuilder.RenameIndex(
                name: "IX_Expeditions_PlayerId",
                table: "expeditions",
                newName: "ix_expeditions_player_id");

            migrationBuilder.RenameIndex(
                name: "IX_Expeditions_ExpeditionTypeId",
                table: "expeditions",
                newName: "ix_expeditions_expedition_type_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "errands",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "TemplateId",
                table: "errands",
                newName: "template_id");

            migrationBuilder.RenameColumn(
                name: "ResolvedDate",
                table: "errands",
                newName: "resolved_date");

            migrationBuilder.RenameColumn(
                name: "PlayerId",
                table: "errands",
                newName: "player_id");

            migrationBuilder.RenameColumn(
                name: "NeighborId",
                table: "errands",
                newName: "neighbor_id");

            migrationBuilder.RenameColumn(
                name: "FinishDate",
                table: "errands",
                newName: "finish_date");

            migrationBuilder.RenameColumn(
                name: "ExpireDate",
                table: "errands",
                newName: "expire_date");

            migrationBuilder.RenameColumn(
                name: "ClueId",
                table: "errands",
                newName: "clue_id");

            migrationBuilder.RenameColumn(
                name: "AcceptDate",
                table: "errands",
                newName: "accept_date");

            migrationBuilder.RenameIndex(
                name: "IX_Errands_PlayerId",
                table: "errands",
                newName: "ix_errands_player_id");

            migrationBuilder.RenameIndex(
                name: "IX_Errands_NeighborId",
                table: "errands",
                newName: "ix_errands_neighbor_id");

            migrationBuilder.RenameColumn(
                name: "Level",
                table: "domiks",
                newName: "level");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "domiks",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpgradeSeconds",
                table: "domiks",
                newName: "upgrade_seconds");

            migrationBuilder.RenameColumn(
                name: "UpgradeCalculateDate",
                table: "domiks",
                newName: "upgrade_calculate_date");

            migrationBuilder.RenameColumn(
                name: "TypeId",
                table: "domiks",
                newName: "type_id");

            migrationBuilder.RenameColumn(
                name: "PlayerId",
                table: "domiks",
                newName: "player_id");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "blueprints",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "blueprints",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ReputationThreshold",
                table: "blueprints",
                newName: "reputation_threshold");

            migrationBuilder.RenameColumn(
                name: "NeighborId",
                table: "blueprints",
                newName: "neighbor_id");

            migrationBuilder.RenameColumn(
                name: "LogicName",
                table: "blueprints",
                newName: "logic_name");

            migrationBuilder.RenameColumn(
                name: "DomikTypeId",
                table: "blueprints",
                newName: "domik_type_id");

            migrationBuilder.RenameIndex(
                name: "IX_Blueprints_NeighborId",
                table: "blueprints",
                newName: "ix_blueprints_neighbor_id");

            migrationBuilder.RenameIndex(
                name: "IX_Blueprints_DomikTypeId",
                table: "blueprints",
                newName: "ix_blueprints_domik_type_id");

            migrationBuilder.RenameColumn(
                name: "Uses",
                table: "worker_skills",
                newName: "uses");

            migrationBuilder.RenameColumn(
                name: "DomikTypeId",
                table: "worker_skills",
                newName: "domik_type_id");

            migrationBuilder.RenameColumn(
                name: "WorkerId",
                table: "worker_skills",
                newName: "worker_id");

            migrationBuilder.RenameColumn(
                name: "GrantDate",
                table: "worker_milestones",
                newName: "grant_date");

            migrationBuilder.RenameColumn(
                name: "MilestoneType",
                table: "worker_milestones",
                newName: "milestone_type");

            migrationBuilder.RenameColumn(
                name: "WorkerId",
                table: "worker_milestones",
                newName: "worker_id");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "weather_types",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "weather_types",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "RotationWeight",
                table: "weather_types",
                newName: "rotation_weight");

            migrationBuilder.RenameColumn(
                name: "LogicName",
                table: "weather_types",
                newName: "logic_name");

            migrationBuilder.RenameColumn(
                name: "OutputPercent",
                table: "weather_type_effects",
                newName: "output_percent");

            migrationBuilder.RenameColumn(
                name: "DomikTypeId",
                table: "weather_type_effects",
                newName: "domik_type_id");

            migrationBuilder.RenameColumn(
                name: "WeatherTypeId",
                table: "weather_type_effects",
                newName: "weather_type_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "weather_periods",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "WeatherTypeId",
                table: "weather_periods",
                newName: "weather_type_id");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "weather_periods",
                newName: "start_date");

            migrationBuilder.RenameColumn(
                name: "EndDate",
                table: "weather_periods",
                newName: "end_date");

            migrationBuilder.RenameIndex(
                name: "IX_WeatherPeriods_WeatherTypeId",
                table: "weather_periods",
                newName: "ix_weather_periods_weather_type_id");

            migrationBuilder.RenameColumn(
                name: "Kind",
                table: "trade_lots",
                newName: "kind");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "trade_lots",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "WantValue",
                table: "trade_lots",
                newName: "want_value");

            migrationBuilder.RenameColumn(
                name: "WantResourceTypeId",
                table: "trade_lots",
                newName: "want_resource_type_id");

            migrationBuilder.RenameColumn(
                name: "SellerId",
                table: "trade_lots",
                newName: "seller_id");

            migrationBuilder.RenameColumn(
                name: "GiveValue",
                table: "trade_lots",
                newName: "give_value");

            migrationBuilder.RenameColumn(
                name: "GiveResourceTypeId",
                table: "trade_lots",
                newName: "give_resource_type_id");

            migrationBuilder.RenameColumn(
                name: "ExpireDate",
                table: "trade_lots",
                newName: "expire_date");

            migrationBuilder.RenameColumn(
                name: "CreateDate",
                table: "trade_lots",
                newName: "create_date");

            migrationBuilder.RenameColumn(
                name: "CommissionCoins",
                table: "trade_lots",
                newName: "commission_coins");

            migrationBuilder.RenameIndex(
                name: "IX_TradeLots_SellerId",
                table: "trade_lots",
                newName: "ix_trade_lots_seller_id");

            migrationBuilder.RenameColumn(
                name: "CandidateTolokaTypeId",
                table: "toloka_votes",
                newName: "candidate_toloka_type_id");

            migrationBuilder.RenameColumn(
                name: "PlayerId",
                table: "toloka_votes",
                newName: "player_id");

            migrationBuilder.RenameColumn(
                name: "TolokaId",
                table: "toloka_votes",
                newName: "toloka_id");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "toloka_types",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "toloka_types",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "RotationWeight",
                table: "toloka_types",
                newName: "rotation_weight");

            migrationBuilder.RenameColumn(
                name: "LogicName",
                table: "toloka_types",
                newName: "logic_name");

            migrationBuilder.RenameColumn(
                name: "Goal",
                table: "toloka_type_positions",
                newName: "goal");

            migrationBuilder.RenameColumn(
                name: "ResourceTypeId",
                table: "toloka_type_positions",
                newName: "resource_type_id");

            migrationBuilder.RenameColumn(
                name: "TolokaTypeId",
                table: "toloka_type_positions",
                newName: "toloka_type_id");

            migrationBuilder.RenameColumn(
                name: "OutputPercent",
                table: "toloka_type_effects",
                newName: "output_percent");

            migrationBuilder.RenameColumn(
                name: "DomikTypeId",
                table: "toloka_type_effects",
                newName: "domik_type_id");

            migrationBuilder.RenameColumn(
                name: "TolokaTypeId",
                table: "toloka_type_effects",
                newName: "toloka_type_id");

            migrationBuilder.RenameColumn(
                name: "Goal",
                table: "toloka_positions",
                newName: "goal");

            migrationBuilder.RenameColumn(
                name: "Collected",
                table: "toloka_positions",
                newName: "collected");

            migrationBuilder.RenameColumn(
                name: "ResourceTypeId",
                table: "toloka_positions",
                newName: "resource_type_id");

            migrationBuilder.RenameColumn(
                name: "TolokaId",
                table: "toloka_positions",
                newName: "toloka_id");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "toloka_contributions",
                newName: "value");

            migrationBuilder.RenameColumn(
                name: "ResourceTypeId",
                table: "toloka_contributions",
                newName: "resource_type_id");

            migrationBuilder.RenameColumn(
                name: "PlayerId",
                table: "toloka_contributions",
                newName: "player_id");

            migrationBuilder.RenameColumn(
                name: "TolokaId",
                table: "toloka_contributions",
                newName: "toloka_id");

            migrationBuilder.RenameIndex(
                name: "IX_TolokaContributions_PlayerId",
                table: "toloka_contributions",
                newName: "ix_toloka_contributions_player_id");

            migrationBuilder.RenameColumn(
                name: "Param2",
                table: "starter_goals",
                newName: "param2");

            migrationBuilder.RenameColumn(
                name: "Param",
                table: "starter_goals",
                newName: "param");

            migrationBuilder.RenameColumn(
                name: "Ordinal",
                table: "starter_goals",
                newName: "ordinal");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "starter_goals",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "starter_goals",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "RewardCoins",
                table: "starter_goals",
                newName: "reward_coins");

            migrationBuilder.RenameColumn(
                name: "ConditionType",
                table: "starter_goals",
                newName: "condition_type");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "season_counters",
                newName: "value");

            migrationBuilder.RenameColumn(
                name: "Metric",
                table: "season_counters",
                newName: "metric");

            migrationBuilder.RenameColumn(
                name: "PlayerId",
                table: "season_counters",
                newName: "player_id");

            migrationBuilder.RenameColumn(
                name: "SeasonId",
                table: "season_counters",
                newName: "season_id");

            migrationBuilder.RenameIndex(
                name: "IX_SeasonCounters_SeasonId_Metric",
                table: "season_counters",
                newName: "ix_season_counters_season_id_metric");

            migrationBuilder.RenameIndex(
                name: "IX_SeasonCounters_PlayerId",
                table: "season_counters",
                newName: "ix_season_counters_player_id");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "resource_types",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "resource_types",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "LogicName",
                table: "resource_types",
                newName: "logic_name");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "receipt_resources",
                newName: "value");

            migrationBuilder.RenameColumn(
                name: "IsOptional",
                table: "receipt_resources",
                newName: "is_optional");

            migrationBuilder.RenameColumn(
                name: "IsInput",
                table: "receipt_resources",
                newName: "is_input");

            migrationBuilder.RenameColumn(
                name: "ResourceTypeId",
                table: "receipt_resources",
                newName: "resource_type_id");

            migrationBuilder.RenameColumn(
                name: "ReceiptId",
                table: "receipt_resources",
                newName: "receipt_id");

            migrationBuilder.RenameIndex(
                name: "IX_ReceiptResources_ResourceTypeId",
                table: "receipt_resources",
                newName: "ix_receipt_resources_resource_type_id");

            migrationBuilder.RenameColumn(
                name: "P256dh",
                table: "player_push_subscriptions",
                newName: "p256dh");

            migrationBuilder.RenameColumn(
                name: "Endpoint",
                table: "player_push_subscriptions",
                newName: "endpoint");

            migrationBuilder.RenameColumn(
                name: "Auth",
                table: "player_push_subscriptions",
                newName: "auth");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "player_push_subscriptions",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "PlayerId",
                table: "player_push_subscriptions",
                newName: "player_id");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "player_push_subscriptions",
                newName: "created_date");

            migrationBuilder.RenameIndex(
                name: "IX_PlayerPushSubscriptions_Endpoint",
                table: "player_push_subscriptions",
                newName: "ix_player_push_subscriptions_endpoint");

            migrationBuilder.RenameColumn(
                name: "CompleteDate",
                table: "player_goals",
                newName: "complete_date");

            migrationBuilder.RenameColumn(
                name: "GoalId",
                table: "player_goals",
                newName: "goal_id");

            migrationBuilder.RenameColumn(
                name: "PlayerId",
                table: "player_goals",
                newName: "player_id");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "player_events",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "Read",
                table: "player_events",
                newName: "read");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "player_events",
                newName: "date");

            migrationBuilder.RenameColumn(
                name: "Data",
                table: "player_events",
                newName: "data");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "player_events",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "PlayerId",
                table: "player_events",
                newName: "player_id");

            migrationBuilder.RenameIndex(
                name: "IX_PlayerEvents_PlayerId_Date",
                table: "player_events",
                newName: "ix_player_events_player_id_date");

            migrationBuilder.RenameColumn(
                name: "Count",
                table: "player_decors",
                newName: "count");

            migrationBuilder.RenameColumn(
                name: "DecorTypeId",
                table: "player_decors",
                newName: "decor_type_id");

            migrationBuilder.RenameColumn(
                name: "PlayerId",
                table: "player_decors",
                newName: "player_id");

            migrationBuilder.RenameIndex(
                name: "IX_PlayerDecors_DecorTypeId",
                table: "player_decors",
                newName: "ix_player_decors_decor_type_id");

            migrationBuilder.RenameColumn(
                name: "BlueprintId",
                table: "player_blueprints",
                newName: "blueprint_id");

            migrationBuilder.RenameColumn(
                name: "PlayerId",
                table: "player_blueprints",
                newName: "player_id");

            migrationBuilder.RenameIndex(
                name: "IX_PlayerBlueprints_BlueprintId",
                table: "player_blueprints",
                newName: "ix_player_blueprints_blueprint_id");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "order_resources",
                newName: "value");

            migrationBuilder.RenameColumn(
                name: "ResourceTypeId",
                table: "order_resources",
                newName: "resource_type_id");

            migrationBuilder.RenameColumn(
                name: "OrderId",
                table: "order_resources",
                newName: "order_id");

            migrationBuilder.RenameIndex(
                name: "IX_OrderResources_ResourceTypeId",
                table: "order_resources",
                newName: "ix_order_resources_resource_type_id");

            migrationBuilder.RenameColumn(
                name: "Points",
                table: "neighbor_reputations",
                newName: "points");

            migrationBuilder.RenameColumn(
                name: "NeighborId",
                table: "neighbor_reputations",
                newName: "neighbor_id");

            migrationBuilder.RenameColumn(
                name: "PlayerId",
                table: "neighbor_reputations",
                newName: "player_id");

            migrationBuilder.RenameIndex(
                name: "IX_NeighborReputations_NeighborId",
                table: "neighbor_reputations",
                newName: "ix_neighbor_reputations_neighbor_id");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "modificator_types",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "modificator_types",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "LogicName",
                table: "modificator_types",
                newName: "logic_name");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "guestbook_entries",
                newName: "date");

            migrationBuilder.RenameColumn(
                name: "Day",
                table: "guestbook_entries",
                newName: "day");

            migrationBuilder.RenameColumn(
                name: "PhraseId",
                table: "guestbook_entries",
                newName: "phrase_id");

            migrationBuilder.RenameColumn(
                name: "GuestPlayerId",
                table: "guestbook_entries",
                newName: "guest_player_id");

            migrationBuilder.RenameColumn(
                name: "HostPlayerId",
                table: "guestbook_entries",
                newName: "host_player_id");

            migrationBuilder.RenameIndex(
                name: "IX_GuestbookEntries_GuestPlayerId",
                table: "guestbook_entries",
                newName: "ix_guestbook_entries_guest_player_id");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "expedition_types",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "expedition_types",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "WorkerCount",
                table: "expedition_types",
                newName: "worker_count");

            migrationBuilder.RenameColumn(
                name: "RollCount",
                table: "expedition_types",
                newName: "roll_count");

            migrationBuilder.RenameColumn(
                name: "LogicName",
                table: "expedition_types",
                newName: "logic_name");

            migrationBuilder.RenameColumn(
                name: "GoldCost",
                table: "expedition_types",
                newName: "gold_cost");

            migrationBuilder.RenameColumn(
                name: "DurationSeconds",
                table: "expedition_types",
                newName: "duration_seconds");

            migrationBuilder.RenameColumn(
                name: "Weight",
                table: "expedition_loot",
                newName: "weight");

            migrationBuilder.RenameColumn(
                name: "Kind",
                table: "expedition_loot",
                newName: "kind");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "expedition_loot",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ResourceTypeId",
                table: "expedition_loot",
                newName: "resource_type_id");

            migrationBuilder.RenameColumn(
                name: "MinValue",
                table: "expedition_loot",
                newName: "min_value");

            migrationBuilder.RenameColumn(
                name: "MaxValue",
                table: "expedition_loot",
                newName: "max_value");

            migrationBuilder.RenameColumn(
                name: "IsRare",
                table: "expedition_loot",
                newName: "is_rare");

            migrationBuilder.RenameColumn(
                name: "ExpeditionTypeId",
                table: "expedition_loot",
                newName: "expedition_type_id");

            migrationBuilder.RenameColumn(
                name: "DecorTypeId",
                table: "expedition_loot",
                newName: "decor_type_id");

            migrationBuilder.RenameColumn(
                name: "BlueprintId",
                table: "expedition_loot",
                newName: "blueprint_id");

            migrationBuilder.RenameIndex(
                name: "IX_ExpeditionLoot_ExpeditionTypeId",
                table: "expedition_loot",
                newName: "ix_expedition_loot_expedition_type_id");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "expedition_equipment",
                newName: "value");

            migrationBuilder.RenameColumn(
                name: "IsOptional",
                table: "expedition_equipment",
                newName: "is_optional");

            migrationBuilder.RenameColumn(
                name: "ResourceTypeId",
                table: "expedition_equipment",
                newName: "resource_type_id");

            migrationBuilder.RenameColumn(
                name: "ExpeditionTypeId",
                table: "expedition_equipment",
                newName: "expedition_type_id");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "domik_types",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "domik_types",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UnlockLevel",
                table: "domik_types",
                newName: "unlock_level");

            migrationBuilder.RenameColumn(
                name: "MaxCount",
                table: "domik_types",
                newName: "max_count");

            migrationBuilder.RenameColumn(
                name: "LogicName",
                table: "domik_types",
                newName: "logic_name");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "domik_type_levels",
                newName: "value");

            migrationBuilder.RenameColumn(
                name: "UpgradeSeconds",
                table: "domik_type_levels",
                newName: "upgrade_seconds");

            migrationBuilder.RenameColumn(
                name: "MaxManufactureCount",
                table: "domik_type_levels",
                newName: "max_manufacture_count");

            migrationBuilder.RenameColumn(
                name: "DomikTypeId",
                table: "domik_type_levels",
                newName: "domik_type_id");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "domik_type_level_resources",
                newName: "value");

            migrationBuilder.RenameColumn(
                name: "ResourceTypeId",
                table: "domik_type_level_resources",
                newName: "resource_type_id");

            migrationBuilder.RenameColumn(
                name: "DomikTypeLevelValue",
                table: "domik_type_level_resources",
                newName: "domik_type_level_value");

            migrationBuilder.RenameColumn(
                name: "DomikTypeLevelDomikTypeId",
                table: "domik_type_level_resources",
                newName: "domik_type_level_domik_type_id");

            migrationBuilder.RenameIndex(
                name: "IX_DomikTypeLevelResources_ResourceTypeId",
                table: "domik_type_level_resources",
                newName: "ix_domik_type_level_resources_resource_type_id");

            migrationBuilder.RenameColumn(
                name: "ReceiptId",
                table: "domik_type_level_recepts",
                newName: "receipt_id");

            migrationBuilder.RenameColumn(
                name: "DomikTypeLevelValue",
                table: "domik_type_level_recepts",
                newName: "domik_type_level_value");

            migrationBuilder.RenameColumn(
                name: "DomikTypeLevelDomikTypeId",
                table: "domik_type_level_recepts",
                newName: "domik_type_level_domik_type_id");

            migrationBuilder.RenameIndex(
                name: "IX_DomikTypeLevelReceipts_ReceiptId",
                table: "domik_type_level_recepts",
                newName: "ix_domik_type_level_recepts_receipt_id");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "domik_type_level_modificators",
                newName: "value");

            migrationBuilder.RenameColumn(
                name: "ModificatorTypeId",
                table: "domik_type_level_modificators",
                newName: "modificator_type_id");

            migrationBuilder.RenameColumn(
                name: "DomikTypeLevelValue",
                table: "domik_type_level_modificators",
                newName: "domik_type_level_value");

            migrationBuilder.RenameColumn(
                name: "DomikTypeLevelDomikTypeId",
                table: "domik_type_level_modificators",
                newName: "domik_type_level_domik_type_id");

            migrationBuilder.RenameIndex(
                name: "IX_DomikTypeLevelModificators_ModificatorTypeId",
                table: "domik_type_level_modificators",
                newName: "ix_domik_type_level_modificators_modificator_type_id");

            migrationBuilder.RenameColumn(
                name: "Ordinal",
                table: "domik_type_count_gates",
                newName: "ordinal");

            migrationBuilder.RenameColumn(
                name: "UnlockLevel",
                table: "domik_type_count_gates",
                newName: "unlock_level");

            migrationBuilder.RenameColumn(
                name: "DomikTypeId",
                table: "domik_type_count_gates",
                newName: "domik_type_id");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "decor_types",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "decor_types",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ReputationThreshold",
                table: "decor_types",
                newName: "reputation_threshold");

            migrationBuilder.RenameColumn(
                name: "NeighborId",
                table: "decor_types",
                newName: "neighbor_id");

            migrationBuilder.RenameColumn(
                name: "LogicName",
                table: "decor_types",
                newName: "logic_name");

            migrationBuilder.RenameColumn(
                name: "IsPurchasable",
                table: "decor_types",
                newName: "is_purchasable");

            migrationBuilder.RenameColumn(
                name: "ComfortPoints",
                table: "decor_types",
                newName: "comfort_points");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "decor_costs",
                newName: "value");

            migrationBuilder.RenameColumn(
                name: "ResourceTypeId",
                table: "decor_costs",
                newName: "resource_type_id");

            migrationBuilder.RenameColumn(
                name: "DecorTypeId",
                table: "decor_costs",
                newName: "decor_type_id");

            migrationBuilder.RenameIndex(
                name: "IX_DecorCosts_ResourceTypeId",
                table: "decor_costs",
                newName: "ix_decor_costs_resource_type_id");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "asp_net_user_tokens",
                newName: "value");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "asp_net_user_tokens",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "LoginProvider",
                table: "asp_net_user_tokens",
                newName: "login_provider");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "asp_net_user_tokens",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "asp_net_users",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "asp_net_users",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "asp_net_users",
                newName: "user_name");

            migrationBuilder.RenameColumn(
                name: "TwoFactorEnabled",
                table: "asp_net_users",
                newName: "two_factor_enabled");

            migrationBuilder.RenameColumn(
                name: "SecurityStamp",
                table: "asp_net_users",
                newName: "security_stamp");

            migrationBuilder.RenameColumn(
                name: "PhoneNumberConfirmed",
                table: "asp_net_users",
                newName: "phone_number_confirmed");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "asp_net_users",
                newName: "phone_number");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "asp_net_users",
                newName: "password_hash");

            migrationBuilder.RenameColumn(
                name: "NormalizedUserName",
                table: "asp_net_users",
                newName: "normalized_user_name");

            migrationBuilder.RenameColumn(
                name: "NormalizedEmail",
                table: "asp_net_users",
                newName: "normalized_email");

            migrationBuilder.RenameColumn(
                name: "LockoutEnd",
                table: "asp_net_users",
                newName: "lockout_end");

            migrationBuilder.RenameColumn(
                name: "LockoutEnabled",
                table: "asp_net_users",
                newName: "lockout_enabled");

            migrationBuilder.RenameColumn(
                name: "EmailConfirmed",
                table: "asp_net_users",
                newName: "email_confirmed");

            migrationBuilder.RenameColumn(
                name: "ConcurrencyStamp",
                table: "asp_net_users",
                newName: "concurrency_stamp");

            migrationBuilder.RenameColumn(
                name: "AccessFailedCount",
                table: "asp_net_users",
                newName: "access_failed_count");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "asp_net_user_roles",
                newName: "role_id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "asp_net_user_roles",
                newName: "user_id");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "asp_net_user_roles",
                newName: "ix_asp_net_user_roles_role_id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "asp_net_user_logins",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "ProviderDisplayName",
                table: "asp_net_user_logins",
                newName: "provider_display_name");

            migrationBuilder.RenameColumn(
                name: "ProviderKey",
                table: "asp_net_user_logins",
                newName: "provider_key");

            migrationBuilder.RenameColumn(
                name: "LoginProvider",
                table: "asp_net_user_logins",
                newName: "login_provider");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "asp_net_user_logins",
                newName: "ix_asp_net_user_logins_user_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "asp_net_user_claims",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "asp_net_user_claims",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "ClaimValue",
                table: "asp_net_user_claims",
                newName: "claim_value");

            migrationBuilder.RenameColumn(
                name: "ClaimType",
                table: "asp_net_user_claims",
                newName: "claim_type");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "asp_net_user_claims",
                newName: "ix_asp_net_user_claims_user_id");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "asp_net_roles",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "asp_net_roles",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "NormalizedName",
                table: "asp_net_roles",
                newName: "normalized_name");

            migrationBuilder.RenameColumn(
                name: "ConcurrencyStamp",
                table: "asp_net_roles",
                newName: "concurrency_stamp");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "asp_net_role_claims",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "asp_net_role_claims",
                newName: "role_id");

            migrationBuilder.RenameColumn(
                name: "ClaimValue",
                table: "asp_net_role_claims",
                newName: "claim_value");

            migrationBuilder.RenameColumn(
                name: "ClaimType",
                table: "asp_net_role_claims",
                newName: "claim_type");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "asp_net_role_claims",
                newName: "ix_asp_net_role_claims_role_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_workers",
                table: "workers",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_traits",
                table: "traits",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_tolokas",
                table: "tolokas",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_resources",
                table: "resources",
                columns: new[] { "player_id", "type_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_receipts",
                table: "receipts",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_players",
                table: "players",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_orders",
                table: "orders",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_neighbors",
                table: "neighbors",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_manufactures",
                table: "manufactures",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_incidents",
                table: "incidents",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_expeditions",
                table: "expeditions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_errands",
                table: "errands",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_domiks",
                table: "domiks",
                columns: new[] { "player_id", "id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_blueprints",
                table: "blueprints",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_worker_skills",
                table: "worker_skills",
                columns: new[] { "worker_id", "domik_type_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_worker_milestones",
                table: "worker_milestones",
                columns: new[] { "worker_id", "milestone_type" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_weather_types",
                table: "weather_types",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_weather_type_effects",
                table: "weather_type_effects",
                columns: new[] { "weather_type_id", "domik_type_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_weather_periods",
                table: "weather_periods",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_trade_lots",
                table: "trade_lots",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_toloka_votes",
                table: "toloka_votes",
                columns: new[] { "toloka_id", "player_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_toloka_types",
                table: "toloka_types",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_toloka_type_positions",
                table: "toloka_type_positions",
                columns: new[] { "toloka_type_id", "resource_type_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_toloka_type_effects",
                table: "toloka_type_effects",
                columns: new[] { "toloka_type_id", "domik_type_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_toloka_positions",
                table: "toloka_positions",
                columns: new[] { "toloka_id", "resource_type_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_toloka_contributions",
                table: "toloka_contributions",
                columns: new[] { "toloka_id", "player_id", "resource_type_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_starter_goals",
                table: "starter_goals",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_season_counters",
                table: "season_counters",
                columns: new[] { "season_id", "player_id", "metric" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_resource_types",
                table: "resource_types",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_receipt_resources",
                table: "receipt_resources",
                columns: new[] { "receipt_id", "resource_type_id", "is_input" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_player_push_subscriptions",
                table: "player_push_subscriptions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_player_goals",
                table: "player_goals",
                columns: new[] { "player_id", "goal_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_player_events",
                table: "player_events",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_player_decors",
                table: "player_decors",
                columns: new[] { "player_id", "decor_type_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_player_blueprints",
                table: "player_blueprints",
                columns: new[] { "player_id", "blueprint_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_order_resources",
                table: "order_resources",
                columns: new[] { "order_id", "resource_type_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_neighbor_reputations",
                table: "neighbor_reputations",
                columns: new[] { "player_id", "neighbor_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_modificator_types",
                table: "modificator_types",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_guestbook_entries",
                table: "guestbook_entries",
                columns: new[] { "host_player_id", "guest_player_id", "day" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_expedition_types",
                table: "expedition_types",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_expedition_loot",
                table: "expedition_loot",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_expedition_equipment",
                table: "expedition_equipment",
                columns: new[] { "expedition_type_id", "resource_type_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_domik_types",
                table: "domik_types",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_domik_type_levels",
                table: "domik_type_levels",
                columns: new[] { "domik_type_id", "value" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_domik_type_level_resources",
                table: "domik_type_level_resources",
                columns: new[] { "domik_type_level_domik_type_id", "domik_type_level_value", "resource_type_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_domik_type_level_recepts",
                table: "domik_type_level_recepts",
                columns: new[] { "domik_type_level_domik_type_id", "domik_type_level_value", "receipt_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_domik_type_level_modificators",
                table: "domik_type_level_modificators",
                columns: new[] { "domik_type_level_domik_type_id", "domik_type_level_value", "modificator_type_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_domik_type_count_gates",
                table: "domik_type_count_gates",
                columns: new[] { "domik_type_id", "ordinal" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_decor_types",
                table: "decor_types",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_decor_costs",
                table: "decor_costs",
                columns: new[] { "decor_type_id", "resource_type_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_user_tokens",
                table: "asp_net_user_tokens",
                columns: new[] { "user_id", "login_provider", "name" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_users",
                table: "asp_net_users",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_user_roles",
                table: "asp_net_user_roles",
                columns: new[] { "user_id", "role_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_user_logins",
                table: "asp_net_user_logins",
                columns: new[] { "login_provider", "provider_key" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_user_claims",
                table: "asp_net_user_claims",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_roles",
                table: "asp_net_roles",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_role_claims",
                table: "asp_net_role_claims",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_players_village_name",
                table: "players",
                column: "village_name",
                unique: true,
                filter: "\"village_name\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_role_claims_asp_net_roles_role_id",
                table: "asp_net_role_claims",
                column: "role_id",
                principalTable: "asp_net_roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_user_claims_asp_net_users_user_id",
                table: "asp_net_user_claims",
                column: "user_id",
                principalTable: "asp_net_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_user_logins_asp_net_users_user_id",
                table: "asp_net_user_logins",
                column: "user_id",
                principalTable: "asp_net_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_user_roles_asp_net_roles_role_id",
                table: "asp_net_user_roles",
                column: "role_id",
                principalTable: "asp_net_roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_user_roles_asp_net_users_user_id",
                table: "asp_net_user_roles",
                column: "user_id",
                principalTable: "asp_net_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_user_tokens_asp_net_users_user_id",
                table: "asp_net_user_tokens",
                column: "user_id",
                principalTable: "asp_net_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_blueprints_domik_types_domik_type_id",
                table: "blueprints",
                column: "domik_type_id",
                principalTable: "domik_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_blueprints_neighbors_neighbor_id",
                table: "blueprints",
                column: "neighbor_id",
                principalTable: "neighbors",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_decor_costs_decor_types_decor_type_id",
                table: "decor_costs",
                column: "decor_type_id",
                principalTable: "decor_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_decor_costs_resource_types_resource_type_id",
                table: "decor_costs",
                column: "resource_type_id",
                principalTable: "resource_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_domik_type_level_modificators_domik_type_levels_domik_type_",
                table: "domik_type_level_modificators",
                columns: new[] { "domik_type_level_domik_type_id", "domik_type_level_value" },
                principalTable: "domik_type_levels",
                principalColumns: new[] { "domik_type_id", "value" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_domik_type_level_modificators_modificator_types_modificator",
                table: "domik_type_level_modificators",
                column: "modificator_type_id",
                principalTable: "modificator_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_domik_type_level_recepts_domik_type_levels_domik_type_level",
                table: "domik_type_level_recepts",
                columns: new[] { "domik_type_level_domik_type_id", "domik_type_level_value" },
                principalTable: "domik_type_levels",
                principalColumns: new[] { "domik_type_id", "value" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_domik_type_level_recepts_receipts_receipt_id",
                table: "domik_type_level_recepts",
                column: "receipt_id",
                principalTable: "receipts",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_domik_type_level_resources_domik_type_levels_domik_type_lev",
                table: "domik_type_level_resources",
                columns: new[] { "domik_type_level_domik_type_id", "domik_type_level_value" },
                principalTable: "domik_type_levels",
                principalColumns: new[] { "domik_type_id", "value" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_domik_type_level_resources_resource_types_resource_type_id",
                table: "domik_type_level_resources",
                column: "resource_type_id",
                principalTable: "resource_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_domik_type_levels_domik_types_domik_type_id",
                table: "domik_type_levels",
                column: "domik_type_id",
                principalTable: "domik_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_errands_neighbors_neighbor_id",
                table: "errands",
                column: "neighbor_id",
                principalTable: "neighbors",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_errands_players_player_id",
                table: "errands",
                column: "player_id",
                principalTable: "players",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_expedition_equipment_expedition_types_expedition_type_id",
                table: "expedition_equipment",
                column: "expedition_type_id",
                principalTable: "expedition_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_expedition_loot_expedition_types_expedition_type_id",
                table: "expedition_loot",
                column: "expedition_type_id",
                principalTable: "expedition_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_expeditions_expedition_types_expedition_type_id",
                table: "expeditions",
                column: "expedition_type_id",
                principalTable: "expedition_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_expeditions_players_player_id",
                table: "expeditions",
                column: "player_id",
                principalTable: "players",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_guestbook_entries_players_guest_player_id",
                table: "guestbook_entries",
                column: "guest_player_id",
                principalTable: "players",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_guestbook_entries_players_host_player_id",
                table: "guestbook_entries",
                column: "host_player_id",
                principalTable: "players",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_incidents_players_player_id",
                table: "incidents",
                column: "player_id",
                principalTable: "players",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_manufactures_domiks_domik_player_id_domik_id",
                table: "manufactures",
                columns: new[] { "domik_player_id", "domik_id" },
                principalTable: "domiks",
                principalColumns: new[] { "player_id", "id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_neighbor_reputations_neighbors_neighbor_id",
                table: "neighbor_reputations",
                column: "neighbor_id",
                principalTable: "neighbors",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_neighbor_reputations_players_player_id",
                table: "neighbor_reputations",
                column: "player_id",
                principalTable: "players",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_order_resources_orders_order_id",
                table: "order_resources",
                column: "order_id",
                principalTable: "orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_order_resources_resource_types_resource_type_id",
                table: "order_resources",
                column: "resource_type_id",
                principalTable: "resource_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_orders_neighbors_neighbor_id",
                table: "orders",
                column: "neighbor_id",
                principalTable: "neighbors",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_orders_players_player_id",
                table: "orders",
                column: "player_id",
                principalTable: "players",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_player_blueprints_blueprints_blueprint_id",
                table: "player_blueprints",
                column: "blueprint_id",
                principalTable: "blueprints",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_player_blueprints_players_player_id",
                table: "player_blueprints",
                column: "player_id",
                principalTable: "players",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_player_decors_decor_types_decor_type_id",
                table: "player_decors",
                column: "decor_type_id",
                principalTable: "decor_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_player_decors_players_player_id",
                table: "player_decors",
                column: "player_id",
                principalTable: "players",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_receipt_resources_receipts_receipt_id",
                table: "receipt_resources",
                column: "receipt_id",
                principalTable: "receipts",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_receipt_resources_resource_types_resource_type_id",
                table: "receipt_resources",
                column: "resource_type_id",
                principalTable: "resource_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_resources_players_player_id",
                table: "resources",
                column: "player_id",
                principalTable: "players",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_season_counters_players_player_id",
                table: "season_counters",
                column: "player_id",
                principalTable: "players",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_toloka_contributions_players_player_id",
                table: "toloka_contributions",
                column: "player_id",
                principalTable: "players",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_toloka_contributions_tolokas_toloka_id",
                table: "toloka_contributions",
                column: "toloka_id",
                principalTable: "tolokas",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_toloka_positions_tolokas_toloka_id",
                table: "toloka_positions",
                column: "toloka_id",
                principalTable: "tolokas",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_toloka_type_effects_toloka_types_toloka_type_id",
                table: "toloka_type_effects",
                column: "toloka_type_id",
                principalTable: "toloka_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_toloka_type_positions_toloka_types_toloka_type_id",
                table: "toloka_type_positions",
                column: "toloka_type_id",
                principalTable: "toloka_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_toloka_votes_tolokas_toloka_id",
                table: "toloka_votes",
                column: "toloka_id",
                principalTable: "tolokas",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tolokas_toloka_types_toloka_type_id",
                table: "tolokas",
                column: "toloka_type_id",
                principalTable: "toloka_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_trade_lots_players_seller_id",
                table: "trade_lots",
                column: "seller_id",
                principalTable: "players",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_weather_periods_weather_types_weather_type_id",
                table: "weather_periods",
                column: "weather_type_id",
                principalTable: "weather_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_weather_type_effects_weather_types_weather_type_id",
                table: "weather_type_effects",
                column: "weather_type_id",
                principalTable: "weather_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_worker_milestones_workers_worker_id",
                table: "worker_milestones",
                column: "worker_id",
                principalTable: "workers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_worker_skills_workers_worker_id",
                table: "worker_skills",
                column: "worker_id",
                principalTable: "workers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_workers_errands_errand_id",
                table: "workers",
                column: "errand_id",
                principalTable: "errands",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_workers_expeditions_expedition_id",
                table: "workers",
                column: "expedition_id",
                principalTable: "expeditions",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_workers_incidents_incident_id",
                table: "workers",
                column: "incident_id",
                principalTable: "incidents",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_workers_manufactures_manufacture_id",
                table: "workers",
                column: "manufacture_id",
                principalTable: "manufactures",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_workers_players_player_id",
                table: "workers",
                column: "player_id",
                principalTable: "players",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_workers_traits_trait_id",
                table: "workers",
                column: "trait_id",
                principalTable: "traits",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_role_claims_asp_net_roles_role_id",
                table: "asp_net_role_claims");

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_user_claims_asp_net_users_user_id",
                table: "asp_net_user_claims");

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_user_logins_asp_net_users_user_id",
                table: "asp_net_user_logins");

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_user_roles_asp_net_roles_role_id",
                table: "asp_net_user_roles");

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_user_roles_asp_net_users_user_id",
                table: "asp_net_user_roles");

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_user_tokens_asp_net_users_user_id",
                table: "asp_net_user_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_blueprints_domik_types_domik_type_id",
                table: "blueprints");

            migrationBuilder.DropForeignKey(
                name: "fk_blueprints_neighbors_neighbor_id",
                table: "blueprints");

            migrationBuilder.DropForeignKey(
                name: "fk_decor_costs_decor_types_decor_type_id",
                table: "decor_costs");

            migrationBuilder.DropForeignKey(
                name: "fk_decor_costs_resource_types_resource_type_id",
                table: "decor_costs");

            migrationBuilder.DropForeignKey(
                name: "fk_domik_type_level_modificators_domik_type_levels_domik_type_",
                table: "domik_type_level_modificators");

            migrationBuilder.DropForeignKey(
                name: "fk_domik_type_level_modificators_modificator_types_modificator",
                table: "domik_type_level_modificators");

            migrationBuilder.DropForeignKey(
                name: "fk_domik_type_level_recepts_domik_type_levels_domik_type_level",
                table: "domik_type_level_recepts");

            migrationBuilder.DropForeignKey(
                name: "fk_domik_type_level_recepts_receipts_receipt_id",
                table: "domik_type_level_recepts");

            migrationBuilder.DropForeignKey(
                name: "fk_domik_type_level_resources_domik_type_levels_domik_type_lev",
                table: "domik_type_level_resources");

            migrationBuilder.DropForeignKey(
                name: "fk_domik_type_level_resources_resource_types_resource_type_id",
                table: "domik_type_level_resources");

            migrationBuilder.DropForeignKey(
                name: "fk_domik_type_levels_domik_types_domik_type_id",
                table: "domik_type_levels");

            migrationBuilder.DropForeignKey(
                name: "fk_errands_neighbors_neighbor_id",
                table: "errands");

            migrationBuilder.DropForeignKey(
                name: "fk_errands_players_player_id",
                table: "errands");

            migrationBuilder.DropForeignKey(
                name: "fk_expedition_equipment_expedition_types_expedition_type_id",
                table: "expedition_equipment");

            migrationBuilder.DropForeignKey(
                name: "fk_expedition_loot_expedition_types_expedition_type_id",
                table: "expedition_loot");

            migrationBuilder.DropForeignKey(
                name: "fk_expeditions_expedition_types_expedition_type_id",
                table: "expeditions");

            migrationBuilder.DropForeignKey(
                name: "fk_expeditions_players_player_id",
                table: "expeditions");

            migrationBuilder.DropForeignKey(
                name: "fk_guestbook_entries_players_guest_player_id",
                table: "guestbook_entries");

            migrationBuilder.DropForeignKey(
                name: "fk_guestbook_entries_players_host_player_id",
                table: "guestbook_entries");

            migrationBuilder.DropForeignKey(
                name: "fk_incidents_players_player_id",
                table: "incidents");

            migrationBuilder.DropForeignKey(
                name: "fk_manufactures_domiks_domik_player_id_domik_id",
                table: "manufactures");

            migrationBuilder.DropForeignKey(
                name: "fk_neighbor_reputations_neighbors_neighbor_id",
                table: "neighbor_reputations");

            migrationBuilder.DropForeignKey(
                name: "fk_neighbor_reputations_players_player_id",
                table: "neighbor_reputations");

            migrationBuilder.DropForeignKey(
                name: "fk_order_resources_orders_order_id",
                table: "order_resources");

            migrationBuilder.DropForeignKey(
                name: "fk_order_resources_resource_types_resource_type_id",
                table: "order_resources");

            migrationBuilder.DropForeignKey(
                name: "fk_orders_neighbors_neighbor_id",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "fk_orders_players_player_id",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "fk_player_blueprints_blueprints_blueprint_id",
                table: "player_blueprints");

            migrationBuilder.DropForeignKey(
                name: "fk_player_blueprints_players_player_id",
                table: "player_blueprints");

            migrationBuilder.DropForeignKey(
                name: "fk_player_decors_decor_types_decor_type_id",
                table: "player_decors");

            migrationBuilder.DropForeignKey(
                name: "fk_player_decors_players_player_id",
                table: "player_decors");

            migrationBuilder.DropForeignKey(
                name: "fk_receipt_resources_receipts_receipt_id",
                table: "receipt_resources");

            migrationBuilder.DropForeignKey(
                name: "fk_receipt_resources_resource_types_resource_type_id",
                table: "receipt_resources");

            migrationBuilder.DropForeignKey(
                name: "fk_resources_players_player_id",
                table: "resources");

            migrationBuilder.DropForeignKey(
                name: "fk_season_counters_players_player_id",
                table: "season_counters");

            migrationBuilder.DropForeignKey(
                name: "fk_toloka_contributions_players_player_id",
                table: "toloka_contributions");

            migrationBuilder.DropForeignKey(
                name: "fk_toloka_contributions_tolokas_toloka_id",
                table: "toloka_contributions");

            migrationBuilder.DropForeignKey(
                name: "fk_toloka_positions_tolokas_toloka_id",
                table: "toloka_positions");

            migrationBuilder.DropForeignKey(
                name: "fk_toloka_type_effects_toloka_types_toloka_type_id",
                table: "toloka_type_effects");

            migrationBuilder.DropForeignKey(
                name: "fk_toloka_type_positions_toloka_types_toloka_type_id",
                table: "toloka_type_positions");

            migrationBuilder.DropForeignKey(
                name: "fk_toloka_votes_tolokas_toloka_id",
                table: "toloka_votes");

            migrationBuilder.DropForeignKey(
                name: "fk_tolokas_toloka_types_toloka_type_id",
                table: "tolokas");

            migrationBuilder.DropForeignKey(
                name: "fk_trade_lots_players_seller_id",
                table: "trade_lots");

            migrationBuilder.DropForeignKey(
                name: "fk_weather_periods_weather_types_weather_type_id",
                table: "weather_periods");

            migrationBuilder.DropForeignKey(
                name: "fk_weather_type_effects_weather_types_weather_type_id",
                table: "weather_type_effects");

            migrationBuilder.DropForeignKey(
                name: "fk_worker_milestones_workers_worker_id",
                table: "worker_milestones");

            migrationBuilder.DropForeignKey(
                name: "fk_worker_skills_workers_worker_id",
                table: "worker_skills");

            migrationBuilder.DropForeignKey(
                name: "fk_workers_errands_errand_id",
                table: "workers");

            migrationBuilder.DropForeignKey(
                name: "fk_workers_expeditions_expedition_id",
                table: "workers");

            migrationBuilder.DropForeignKey(
                name: "fk_workers_incidents_incident_id",
                table: "workers");

            migrationBuilder.DropForeignKey(
                name: "fk_workers_manufactures_manufacture_id",
                table: "workers");

            migrationBuilder.DropForeignKey(
                name: "fk_workers_players_player_id",
                table: "workers");

            migrationBuilder.DropForeignKey(
                name: "fk_workers_traits_trait_id",
                table: "workers");

            migrationBuilder.DropPrimaryKey(
                name: "pk_workers",
                table: "workers");

            migrationBuilder.DropPrimaryKey(
                name: "pk_traits",
                table: "traits");

            migrationBuilder.DropPrimaryKey(
                name: "pk_tolokas",
                table: "tolokas");

            migrationBuilder.DropPrimaryKey(
                name: "pk_resources",
                table: "resources");

            migrationBuilder.DropPrimaryKey(
                name: "pk_receipts",
                table: "receipts");

            migrationBuilder.DropPrimaryKey(
                name: "pk_players",
                table: "players");

            migrationBuilder.DropIndex(
                name: "ix_players_village_name",
                table: "players");

            migrationBuilder.DropPrimaryKey(
                name: "pk_orders",
                table: "orders");

            migrationBuilder.DropPrimaryKey(
                name: "pk_neighbors",
                table: "neighbors");

            migrationBuilder.DropPrimaryKey(
                name: "pk_manufactures",
                table: "manufactures");

            migrationBuilder.DropPrimaryKey(
                name: "pk_incidents",
                table: "incidents");

            migrationBuilder.DropPrimaryKey(
                name: "pk_expeditions",
                table: "expeditions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_errands",
                table: "errands");

            migrationBuilder.DropPrimaryKey(
                name: "pk_domiks",
                table: "domiks");

            migrationBuilder.DropPrimaryKey(
                name: "pk_blueprints",
                table: "blueprints");

            migrationBuilder.DropPrimaryKey(
                name: "pk_worker_skills",
                table: "worker_skills");

            migrationBuilder.DropPrimaryKey(
                name: "pk_worker_milestones",
                table: "worker_milestones");

            migrationBuilder.DropPrimaryKey(
                name: "pk_weather_types",
                table: "weather_types");

            migrationBuilder.DropPrimaryKey(
                name: "pk_weather_type_effects",
                table: "weather_type_effects");

            migrationBuilder.DropPrimaryKey(
                name: "pk_weather_periods",
                table: "weather_periods");

            migrationBuilder.DropPrimaryKey(
                name: "pk_trade_lots",
                table: "trade_lots");

            migrationBuilder.DropPrimaryKey(
                name: "pk_toloka_votes",
                table: "toloka_votes");

            migrationBuilder.DropPrimaryKey(
                name: "pk_toloka_types",
                table: "toloka_types");

            migrationBuilder.DropPrimaryKey(
                name: "pk_toloka_type_positions",
                table: "toloka_type_positions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_toloka_type_effects",
                table: "toloka_type_effects");

            migrationBuilder.DropPrimaryKey(
                name: "pk_toloka_positions",
                table: "toloka_positions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_toloka_contributions",
                table: "toloka_contributions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_starter_goals",
                table: "starter_goals");

            migrationBuilder.DropPrimaryKey(
                name: "pk_season_counters",
                table: "season_counters");

            migrationBuilder.DropPrimaryKey(
                name: "pk_resource_types",
                table: "resource_types");

            migrationBuilder.DropPrimaryKey(
                name: "pk_receipt_resources",
                table: "receipt_resources");

            migrationBuilder.DropPrimaryKey(
                name: "pk_player_push_subscriptions",
                table: "player_push_subscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_player_goals",
                table: "player_goals");

            migrationBuilder.DropPrimaryKey(
                name: "pk_player_events",
                table: "player_events");

            migrationBuilder.DropPrimaryKey(
                name: "pk_player_decors",
                table: "player_decors");

            migrationBuilder.DropPrimaryKey(
                name: "pk_player_blueprints",
                table: "player_blueprints");

            migrationBuilder.DropPrimaryKey(
                name: "pk_order_resources",
                table: "order_resources");

            migrationBuilder.DropPrimaryKey(
                name: "pk_neighbor_reputations",
                table: "neighbor_reputations");

            migrationBuilder.DropPrimaryKey(
                name: "pk_modificator_types",
                table: "modificator_types");

            migrationBuilder.DropPrimaryKey(
                name: "pk_guestbook_entries",
                table: "guestbook_entries");

            migrationBuilder.DropPrimaryKey(
                name: "pk_expedition_types",
                table: "expedition_types");

            migrationBuilder.DropPrimaryKey(
                name: "pk_expedition_loot",
                table: "expedition_loot");

            migrationBuilder.DropPrimaryKey(
                name: "pk_expedition_equipment",
                table: "expedition_equipment");

            migrationBuilder.DropPrimaryKey(
                name: "pk_domik_types",
                table: "domik_types");

            migrationBuilder.DropPrimaryKey(
                name: "pk_domik_type_levels",
                table: "domik_type_levels");

            migrationBuilder.DropPrimaryKey(
                name: "pk_domik_type_level_resources",
                table: "domik_type_level_resources");

            migrationBuilder.DropPrimaryKey(
                name: "pk_domik_type_level_recepts",
                table: "domik_type_level_recepts");

            migrationBuilder.DropPrimaryKey(
                name: "pk_domik_type_level_modificators",
                table: "domik_type_level_modificators");

            migrationBuilder.DropPrimaryKey(
                name: "pk_domik_type_count_gates",
                table: "domik_type_count_gates");

            migrationBuilder.DropPrimaryKey(
                name: "pk_decor_types",
                table: "decor_types");

            migrationBuilder.DropPrimaryKey(
                name: "pk_decor_costs",
                table: "decor_costs");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_users",
                table: "asp_net_users");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_user_tokens",
                table: "asp_net_user_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_user_roles",
                table: "asp_net_user_roles");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_user_logins",
                table: "asp_net_user_logins");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_user_claims",
                table: "asp_net_user_claims");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_roles",
                table: "asp_net_roles");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_role_claims",
                table: "asp_net_role_claims");

            migrationBuilder.RenameTable(
                name: "workers",
                newName: "Workers");

            migrationBuilder.RenameTable(
                name: "traits",
                newName: "Traits");

            migrationBuilder.RenameTable(
                name: "tolokas",
                newName: "Tolokas");

            migrationBuilder.RenameTable(
                name: "resources",
                newName: "Resources");

            migrationBuilder.RenameTable(
                name: "receipts",
                newName: "Receipts");

            migrationBuilder.RenameTable(
                name: "players",
                newName: "Players");

            migrationBuilder.RenameTable(
                name: "orders",
                newName: "Orders");

            migrationBuilder.RenameTable(
                name: "neighbors",
                newName: "Neighbors");

            migrationBuilder.RenameTable(
                name: "manufactures",
                newName: "Manufactures");

            migrationBuilder.RenameTable(
                name: "incidents",
                newName: "Incidents");

            migrationBuilder.RenameTable(
                name: "expeditions",
                newName: "Expeditions");

            migrationBuilder.RenameTable(
                name: "errands",
                newName: "Errands");

            migrationBuilder.RenameTable(
                name: "domiks",
                newName: "Domiks");

            migrationBuilder.RenameTable(
                name: "blueprints",
                newName: "Blueprints");

            migrationBuilder.RenameTable(
                name: "worker_skills",
                newName: "WorkerSkills");

            migrationBuilder.RenameTable(
                name: "worker_milestones",
                newName: "WorkerMilestones");

            migrationBuilder.RenameTable(
                name: "weather_types",
                newName: "WeatherTypes");

            migrationBuilder.RenameTable(
                name: "weather_type_effects",
                newName: "WeatherTypeEffects");

            migrationBuilder.RenameTable(
                name: "weather_periods",
                newName: "WeatherPeriods");

            migrationBuilder.RenameTable(
                name: "trade_lots",
                newName: "TradeLots");

            migrationBuilder.RenameTable(
                name: "toloka_votes",
                newName: "TolokaVotes");

            migrationBuilder.RenameTable(
                name: "toloka_types",
                newName: "TolokaTypes");

            migrationBuilder.RenameTable(
                name: "toloka_type_positions",
                newName: "TolokaTypePositions");

            migrationBuilder.RenameTable(
                name: "toloka_type_effects",
                newName: "TolokaTypeEffects");

            migrationBuilder.RenameTable(
                name: "toloka_positions",
                newName: "TolokaPositions");

            migrationBuilder.RenameTable(
                name: "toloka_contributions",
                newName: "TolokaContributions");

            migrationBuilder.RenameTable(
                name: "starter_goals",
                newName: "StarterGoals");

            migrationBuilder.RenameTable(
                name: "season_counters",
                newName: "SeasonCounters");

            migrationBuilder.RenameTable(
                name: "resource_types",
                newName: "ResourceTypes");

            migrationBuilder.RenameTable(
                name: "receipt_resources",
                newName: "ReceiptResources");

            migrationBuilder.RenameTable(
                name: "player_push_subscriptions",
                newName: "PlayerPushSubscriptions");

            migrationBuilder.RenameTable(
                name: "player_goals",
                newName: "PlayerGoals");

            migrationBuilder.RenameTable(
                name: "player_events",
                newName: "PlayerEvents");

            migrationBuilder.RenameTable(
                name: "player_decors",
                newName: "PlayerDecors");

            migrationBuilder.RenameTable(
                name: "player_blueprints",
                newName: "PlayerBlueprints");

            migrationBuilder.RenameTable(
                name: "order_resources",
                newName: "OrderResources");

            migrationBuilder.RenameTable(
                name: "neighbor_reputations",
                newName: "NeighborReputations");

            migrationBuilder.RenameTable(
                name: "modificator_types",
                newName: "ModificatorTypes");

            migrationBuilder.RenameTable(
                name: "guestbook_entries",
                newName: "GuestbookEntries");

            migrationBuilder.RenameTable(
                name: "expedition_types",
                newName: "ExpeditionTypes");

            migrationBuilder.RenameTable(
                name: "expedition_loot",
                newName: "ExpeditionLoot");

            migrationBuilder.RenameTable(
                name: "expedition_equipment",
                newName: "ExpeditionEquipment");

            migrationBuilder.RenameTable(
                name: "domik_types",
                newName: "DomikTypes");

            migrationBuilder.RenameTable(
                name: "domik_type_levels",
                newName: "DomikTypeLevels");

            migrationBuilder.RenameTable(
                name: "domik_type_level_resources",
                newName: "DomikTypeLevelResources");

            migrationBuilder.RenameTable(
                name: "domik_type_level_recepts",
                newName: "DomikTypeLevelReceipts");

            migrationBuilder.RenameTable(
                name: "domik_type_level_modificators",
                newName: "DomikTypeLevelModificators");

            migrationBuilder.RenameTable(
                name: "domik_type_count_gates",
                newName: "DomikTypeCountGates");

            migrationBuilder.RenameTable(
                name: "decor_types",
                newName: "DecorTypes");

            migrationBuilder.RenameTable(
                name: "decor_costs",
                newName: "DecorCosts");

            migrationBuilder.RenameTable(
                name: "asp_net_users",
                newName: "AspNetUsers");

            migrationBuilder.RenameTable(
                name: "asp_net_user_tokens",
                newName: "AspNetUserTokens");

            migrationBuilder.RenameTable(
                name: "asp_net_user_roles",
                newName: "AspNetUserRoles");

            migrationBuilder.RenameTable(
                name: "asp_net_user_logins",
                newName: "AspNetUserLogins");

            migrationBuilder.RenameTable(
                name: "asp_net_user_claims",
                newName: "AspNetUserClaims");

            migrationBuilder.RenameTable(
                name: "asp_net_roles",
                newName: "AspNetRoles");

            migrationBuilder.RenameTable(
                name: "asp_net_role_claims",
                newName: "AspNetRoleClaims");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Workers",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Workers",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "worked_seconds",
                table: "Workers",
                newName: "WorkedSeconds");

            migrationBuilder.RenameColumn(
                name: "trait_id",
                table: "Workers",
                newName: "TraitId");

            migrationBuilder.RenameColumn(
                name: "sick_until",
                table: "Workers",
                newName: "SickUntil");

            migrationBuilder.RenameColumn(
                name: "rest_until",
                table: "Workers",
                newName: "RestUntil");

            migrationBuilder.RenameColumn(
                name: "player_id",
                table: "Workers",
                newName: "PlayerId");

            migrationBuilder.RenameColumn(
                name: "manufacture_id",
                table: "Workers",
                newName: "ManufactureId");

            migrationBuilder.RenameColumn(
                name: "incident_id",
                table: "Workers",
                newName: "IncidentId");

            migrationBuilder.RenameColumn(
                name: "hire_date",
                table: "Workers",
                newName: "HireDate");

            migrationBuilder.RenameColumn(
                name: "expedition_id",
                table: "Workers",
                newName: "ExpeditionId");

            migrationBuilder.RenameColumn(
                name: "expedition_count",
                table: "Workers",
                newName: "ExpeditionCount");

            migrationBuilder.RenameColumn(
                name: "errand_id",
                table: "Workers",
                newName: "ErrandId");

            migrationBuilder.RenameIndex(
                name: "ix_workers_trait_id",
                table: "Workers",
                newName: "IX_Workers_TraitId");

            migrationBuilder.RenameIndex(
                name: "ix_workers_player_id",
                table: "Workers",
                newName: "IX_Workers_PlayerId");

            migrationBuilder.RenameIndex(
                name: "ix_workers_manufacture_id",
                table: "Workers",
                newName: "IX_Workers_ManufactureId");

            migrationBuilder.RenameIndex(
                name: "ix_workers_incident_id",
                table: "Workers",
                newName: "IX_Workers_IncidentId");

            migrationBuilder.RenameIndex(
                name: "ix_workers_expedition_id",
                table: "Workers",
                newName: "IX_Workers_ExpeditionId");

            migrationBuilder.RenameIndex(
                name: "ix_workers_errand_id",
                table: "Workers",
                newName: "IX_Workers_ErrandId");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Traits",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Traits",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "no_sick",
                table: "Traits",
                newName: "NoSick");

            migrationBuilder.RenameColumn(
                name: "no_fatigue",
                table: "Traits",
                newName: "NoFatigue");

            migrationBuilder.RenameColumn(
                name: "luck_weight_percent",
                table: "Traits",
                newName: "LuckWeightPercent");

            migrationBuilder.RenameColumn(
                name: "logic_name",
                table: "Traits",
                newName: "LogicName");

            migrationBuilder.RenameColumn(
                name: "duration_percent",
                table: "Traits",
                newName: "DurationPercent");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Tolokas",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "toloka_type_id",
                table: "Tolokas",
                newName: "TolokaTypeId");

            migrationBuilder.RenameColumn(
                name: "start_date",
                table: "Tolokas",
                newName: "StartDate");

            migrationBuilder.RenameColumn(
                name: "completed_date",
                table: "Tolokas",
                newName: "CompletedDate");

            migrationBuilder.RenameIndex(
                name: "ix_tolokas_toloka_type_id",
                table: "Tolokas",
                newName: "IX_Tolokas_TolokaTypeId");

            migrationBuilder.RenameColumn(
                name: "value",
                table: "Resources",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "type_id",
                table: "Resources",
                newName: "TypeId");

            migrationBuilder.RenameColumn(
                name: "player_id",
                table: "Resources",
                newName: "PlayerId");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Receipts",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Receipts",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "plodder_count",
                table: "Receipts",
                newName: "PlodderCount");

            migrationBuilder.RenameColumn(
                name: "output_bonus_percent",
                table: "Receipts",
                newName: "OutputBonusPercent");

            migrationBuilder.RenameColumn(
                name: "logic_name",
                table: "Receipts",
                newName: "LogicName");

            migrationBuilder.RenameColumn(
                name: "duration_seconds",
                table: "Receipts",
                newName: "DurationSeconds");

            migrationBuilder.RenameColumn(
                name: "version",
                table: "Players",
                newName: "Version");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Players",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Players",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "zeal_charges",
                table: "Players",
                newName: "ZealCharges");

            migrationBuilder.RenameColumn(
                name: "visits_since_big_gift",
                table: "Players",
                newName: "VisitsSinceBigGift");

            migrationBuilder.RenameColumn(
                name: "village_name",
                table: "Players",
                newName: "VillageName");

            migrationBuilder.RenameColumn(
                name: "next_order_refill_at",
                table: "Players",
                newName: "NextOrderRefillAt");

            migrationBuilder.RenameColumn(
                name: "last_worker_milestone_date",
                table: "Players",
                newName: "LastWorkerMilestoneDate");

            migrationBuilder.RenameColumn(
                name: "last_seen",
                table: "Players",
                newName: "LastSeen");

            migrationBuilder.RenameColumn(
                name: "last_incident_date",
                table: "Players",
                newName: "LastIncidentDate");

            migrationBuilder.RenameColumn(
                name: "last_help_date",
                table: "Players",
                newName: "LastHelpDate");

            migrationBuilder.RenameColumn(
                name: "last_domik_incident_date",
                table: "Players",
                newName: "LastDomikIncidentDate");

            migrationBuilder.RenameColumn(
                name: "helps_received_today",
                table: "Players",
                newName: "HelpsReceivedToday");

            migrationBuilder.RenameColumn(
                name: "helps_received_date",
                table: "Players",
                newName: "HelpsReceivedDate");

            migrationBuilder.RenameColumn(
                name: "gold_mined_today",
                table: "Players",
                newName: "GoldMinedToday");

            migrationBuilder.RenameColumn(
                name: "gold_mined_date",
                table: "Players",
                newName: "GoldMinedDate");

            migrationBuilder.RenameColumn(
                name: "feed_workers",
                table: "Players",
                newName: "FeedWorkers");

            migrationBuilder.RenameColumn(
                name: "expeditions_since_pity",
                table: "Players",
                newName: "ExpeditionsSincePity");

            migrationBuilder.RenameColumn(
                name: "crest_icon",
                table: "Players",
                newName: "CrestIcon");

            migrationBuilder.RenameColumn(
                name: "crest_color",
                table: "Players",
                newName: "CrestColor");

            migrationBuilder.RenameColumn(
                name: "asp_net_user_id",
                table: "Players",
                newName: "AspNetUserId");

            migrationBuilder.RenameIndex(
                name: "ix_players_asp_net_user_id",
                table: "Players",
                newName: "IX_Players_AspNetUserId");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Orders",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "reward_reputation",
                table: "Orders",
                newName: "RewardReputation");

            migrationBuilder.RenameColumn(
                name: "reward_gold",
                table: "Orders",
                newName: "RewardGold");

            migrationBuilder.RenameColumn(
                name: "reward_coins",
                table: "Orders",
                newName: "RewardCoins");

            migrationBuilder.RenameColumn(
                name: "player_id",
                table: "Orders",
                newName: "PlayerId");

            migrationBuilder.RenameColumn(
                name: "neighbor_id",
                table: "Orders",
                newName: "NeighborId");

            migrationBuilder.RenameColumn(
                name: "expire_date",
                table: "Orders",
                newName: "ExpireDate");

            migrationBuilder.RenameColumn(
                name: "create_date",
                table: "Orders",
                newName: "CreateDate");

            migrationBuilder.RenameIndex(
                name: "ix_orders_player_id",
                table: "Orders",
                newName: "IX_Orders_PlayerId");

            migrationBuilder.RenameIndex(
                name: "ix_orders_neighbor_id",
                table: "Orders",
                newName: "IX_Orders_NeighborId");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Neighbors",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Neighbors",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "unlock_level",
                table: "Neighbors",
                newName: "UnlockLevel");

            migrationBuilder.RenameColumn(
                name: "secondary_resource_type_id",
                table: "Neighbors",
                newName: "SecondaryResourceTypeId");

            migrationBuilder.RenameColumn(
                name: "primary_resource_type_id",
                table: "Neighbors",
                newName: "PrimaryResourceTypeId");

            migrationBuilder.RenameColumn(
                name: "logic_name",
                table: "Neighbors",
                newName: "LogicName");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Manufactures",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "use_optional",
                table: "Manufactures",
                newName: "UseOptional");

            migrationBuilder.RenameColumn(
                name: "sick_chance",
                table: "Manufactures",
                newName: "SickChance");

            migrationBuilder.RenameColumn(
                name: "receipt_id",
                table: "Manufactures",
                newName: "ReceiptId");

            migrationBuilder.RenameColumn(
                name: "plodder_count",
                table: "Manufactures",
                newName: "PlodderCount");

            migrationBuilder.RenameColumn(
                name: "output_percent",
                table: "Manufactures",
                newName: "OutputPercent");

            migrationBuilder.RenameColumn(
                name: "finish_date",
                table: "Manufactures",
                newName: "FinishDate");

            migrationBuilder.RenameColumn(
                name: "duration_seconds",
                table: "Manufactures",
                newName: "DurationSeconds");

            migrationBuilder.RenameColumn(
                name: "domik_player_id",
                table: "Manufactures",
                newName: "DomikPlayerId");

            migrationBuilder.RenameColumn(
                name: "domik_id",
                table: "Manufactures",
                newName: "DomikId");

            migrationBuilder.RenameColumn(
                name: "auto_repeat",
                table: "Manufactures",
                newName: "AutoRepeat");

            migrationBuilder.RenameIndex(
                name: "ix_manufactures_domik_player_id_domik_id",
                table: "Manufactures",
                newName: "IX_Manufactures_DomikPlayerId_DomikId");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Incidents",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "template_id",
                table: "Incidents",
                newName: "TemplateId");

            migrationBuilder.RenameColumn(
                name: "source_type",
                table: "Incidents",
                newName: "SourceType");

            migrationBuilder.RenameColumn(
                name: "search_end_date",
                table: "Incidents",
                newName: "SearchEndDate");

            migrationBuilder.RenameColumn(
                name: "resolved_date",
                table: "Incidents",
                newName: "ResolvedDate");

            migrationBuilder.RenameColumn(
                name: "player_id",
                table: "Incidents",
                newName: "PlayerId");

            migrationBuilder.RenameColumn(
                name: "missing_worker_id",
                table: "Incidents",
                newName: "MissingWorkerId");

            migrationBuilder.RenameColumn(
                name: "expedition_type_id",
                table: "Incidents",
                newName: "ExpeditionTypeId");

            migrationBuilder.RenameColumn(
                name: "domik_type_id",
                table: "Incidents",
                newName: "DomikTypeId");

            migrationBuilder.RenameColumn(
                name: "create_date",
                table: "Incidents",
                newName: "CreateDate");

            migrationBuilder.RenameColumn(
                name: "clue_id",
                table: "Incidents",
                newName: "ClueId");

            migrationBuilder.RenameIndex(
                name: "ix_incidents_player_id",
                table: "Incidents",
                newName: "IX_Incidents_PlayerId");

            migrationBuilder.RenameColumn(
                name: "provisioned",
                table: "Expeditions",
                newName: "Provisioned");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Expeditions",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "start_date",
                table: "Expeditions",
                newName: "StartDate");

            migrationBuilder.RenameColumn(
                name: "player_id",
                table: "Expeditions",
                newName: "PlayerId");

            migrationBuilder.RenameColumn(
                name: "finish_date",
                table: "Expeditions",
                newName: "FinishDate");

            migrationBuilder.RenameColumn(
                name: "expedition_type_id",
                table: "Expeditions",
                newName: "ExpeditionTypeId");

            migrationBuilder.RenameIndex(
                name: "ix_expeditions_player_id",
                table: "Expeditions",
                newName: "IX_Expeditions_PlayerId");

            migrationBuilder.RenameIndex(
                name: "ix_expeditions_expedition_type_id",
                table: "Expeditions",
                newName: "IX_Expeditions_ExpeditionTypeId");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Errands",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "template_id",
                table: "Errands",
                newName: "TemplateId");

            migrationBuilder.RenameColumn(
                name: "resolved_date",
                table: "Errands",
                newName: "ResolvedDate");

            migrationBuilder.RenameColumn(
                name: "player_id",
                table: "Errands",
                newName: "PlayerId");

            migrationBuilder.RenameColumn(
                name: "neighbor_id",
                table: "Errands",
                newName: "NeighborId");

            migrationBuilder.RenameColumn(
                name: "finish_date",
                table: "Errands",
                newName: "FinishDate");

            migrationBuilder.RenameColumn(
                name: "expire_date",
                table: "Errands",
                newName: "ExpireDate");

            migrationBuilder.RenameColumn(
                name: "clue_id",
                table: "Errands",
                newName: "ClueId");

            migrationBuilder.RenameColumn(
                name: "accept_date",
                table: "Errands",
                newName: "AcceptDate");

            migrationBuilder.RenameIndex(
                name: "ix_errands_player_id",
                table: "Errands",
                newName: "IX_Errands_PlayerId");

            migrationBuilder.RenameIndex(
                name: "ix_errands_neighbor_id",
                table: "Errands",
                newName: "IX_Errands_NeighborId");

            migrationBuilder.RenameColumn(
                name: "level",
                table: "Domiks",
                newName: "Level");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Domiks",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "upgrade_seconds",
                table: "Domiks",
                newName: "UpgradeSeconds");

            migrationBuilder.RenameColumn(
                name: "upgrade_calculate_date",
                table: "Domiks",
                newName: "UpgradeCalculateDate");

            migrationBuilder.RenameColumn(
                name: "type_id",
                table: "Domiks",
                newName: "TypeId");

            migrationBuilder.RenameColumn(
                name: "player_id",
                table: "Domiks",
                newName: "PlayerId");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Blueprints",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Blueprints",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "reputation_threshold",
                table: "Blueprints",
                newName: "ReputationThreshold");

            migrationBuilder.RenameColumn(
                name: "neighbor_id",
                table: "Blueprints",
                newName: "NeighborId");

            migrationBuilder.RenameColumn(
                name: "logic_name",
                table: "Blueprints",
                newName: "LogicName");

            migrationBuilder.RenameColumn(
                name: "domik_type_id",
                table: "Blueprints",
                newName: "DomikTypeId");

            migrationBuilder.RenameIndex(
                name: "ix_blueprints_neighbor_id",
                table: "Blueprints",
                newName: "IX_Blueprints_NeighborId");

            migrationBuilder.RenameIndex(
                name: "ix_blueprints_domik_type_id",
                table: "Blueprints",
                newName: "IX_Blueprints_DomikTypeId");

            migrationBuilder.RenameColumn(
                name: "uses",
                table: "WorkerSkills",
                newName: "Uses");

            migrationBuilder.RenameColumn(
                name: "domik_type_id",
                table: "WorkerSkills",
                newName: "DomikTypeId");

            migrationBuilder.RenameColumn(
                name: "worker_id",
                table: "WorkerSkills",
                newName: "WorkerId");

            migrationBuilder.RenameColumn(
                name: "grant_date",
                table: "WorkerMilestones",
                newName: "GrantDate");

            migrationBuilder.RenameColumn(
                name: "milestone_type",
                table: "WorkerMilestones",
                newName: "MilestoneType");

            migrationBuilder.RenameColumn(
                name: "worker_id",
                table: "WorkerMilestones",
                newName: "WorkerId");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "WeatherTypes",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "WeatherTypes",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "rotation_weight",
                table: "WeatherTypes",
                newName: "RotationWeight");

            migrationBuilder.RenameColumn(
                name: "logic_name",
                table: "WeatherTypes",
                newName: "LogicName");

            migrationBuilder.RenameColumn(
                name: "output_percent",
                table: "WeatherTypeEffects",
                newName: "OutputPercent");

            migrationBuilder.RenameColumn(
                name: "domik_type_id",
                table: "WeatherTypeEffects",
                newName: "DomikTypeId");

            migrationBuilder.RenameColumn(
                name: "weather_type_id",
                table: "WeatherTypeEffects",
                newName: "WeatherTypeId");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "WeatherPeriods",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "weather_type_id",
                table: "WeatherPeriods",
                newName: "WeatherTypeId");

            migrationBuilder.RenameColumn(
                name: "start_date",
                table: "WeatherPeriods",
                newName: "StartDate");

            migrationBuilder.RenameColumn(
                name: "end_date",
                table: "WeatherPeriods",
                newName: "EndDate");

            migrationBuilder.RenameIndex(
                name: "ix_weather_periods_weather_type_id",
                table: "WeatherPeriods",
                newName: "IX_WeatherPeriods_WeatherTypeId");

            migrationBuilder.RenameColumn(
                name: "kind",
                table: "TradeLots",
                newName: "Kind");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "TradeLots",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "want_value",
                table: "TradeLots",
                newName: "WantValue");

            migrationBuilder.RenameColumn(
                name: "want_resource_type_id",
                table: "TradeLots",
                newName: "WantResourceTypeId");

            migrationBuilder.RenameColumn(
                name: "seller_id",
                table: "TradeLots",
                newName: "SellerId");

            migrationBuilder.RenameColumn(
                name: "give_value",
                table: "TradeLots",
                newName: "GiveValue");

            migrationBuilder.RenameColumn(
                name: "give_resource_type_id",
                table: "TradeLots",
                newName: "GiveResourceTypeId");

            migrationBuilder.RenameColumn(
                name: "expire_date",
                table: "TradeLots",
                newName: "ExpireDate");

            migrationBuilder.RenameColumn(
                name: "create_date",
                table: "TradeLots",
                newName: "CreateDate");

            migrationBuilder.RenameColumn(
                name: "commission_coins",
                table: "TradeLots",
                newName: "CommissionCoins");

            migrationBuilder.RenameIndex(
                name: "ix_trade_lots_seller_id",
                table: "TradeLots",
                newName: "IX_TradeLots_SellerId");

            migrationBuilder.RenameColumn(
                name: "candidate_toloka_type_id",
                table: "TolokaVotes",
                newName: "CandidateTolokaTypeId");

            migrationBuilder.RenameColumn(
                name: "player_id",
                table: "TolokaVotes",
                newName: "PlayerId");

            migrationBuilder.RenameColumn(
                name: "toloka_id",
                table: "TolokaVotes",
                newName: "TolokaId");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "TolokaTypes",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "TolokaTypes",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "rotation_weight",
                table: "TolokaTypes",
                newName: "RotationWeight");

            migrationBuilder.RenameColumn(
                name: "logic_name",
                table: "TolokaTypes",
                newName: "LogicName");

            migrationBuilder.RenameColumn(
                name: "goal",
                table: "TolokaTypePositions",
                newName: "Goal");

            migrationBuilder.RenameColumn(
                name: "resource_type_id",
                table: "TolokaTypePositions",
                newName: "ResourceTypeId");

            migrationBuilder.RenameColumn(
                name: "toloka_type_id",
                table: "TolokaTypePositions",
                newName: "TolokaTypeId");

            migrationBuilder.RenameColumn(
                name: "output_percent",
                table: "TolokaTypeEffects",
                newName: "OutputPercent");

            migrationBuilder.RenameColumn(
                name: "domik_type_id",
                table: "TolokaTypeEffects",
                newName: "DomikTypeId");

            migrationBuilder.RenameColumn(
                name: "toloka_type_id",
                table: "TolokaTypeEffects",
                newName: "TolokaTypeId");

            migrationBuilder.RenameColumn(
                name: "goal",
                table: "TolokaPositions",
                newName: "Goal");

            migrationBuilder.RenameColumn(
                name: "collected",
                table: "TolokaPositions",
                newName: "Collected");

            migrationBuilder.RenameColumn(
                name: "resource_type_id",
                table: "TolokaPositions",
                newName: "ResourceTypeId");

            migrationBuilder.RenameColumn(
                name: "toloka_id",
                table: "TolokaPositions",
                newName: "TolokaId");

            migrationBuilder.RenameColumn(
                name: "value",
                table: "TolokaContributions",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "resource_type_id",
                table: "TolokaContributions",
                newName: "ResourceTypeId");

            migrationBuilder.RenameColumn(
                name: "player_id",
                table: "TolokaContributions",
                newName: "PlayerId");

            migrationBuilder.RenameColumn(
                name: "toloka_id",
                table: "TolokaContributions",
                newName: "TolokaId");

            migrationBuilder.RenameIndex(
                name: "ix_toloka_contributions_player_id",
                table: "TolokaContributions",
                newName: "IX_TolokaContributions_PlayerId");

            migrationBuilder.RenameColumn(
                name: "param2",
                table: "StarterGoals",
                newName: "Param2");

            migrationBuilder.RenameColumn(
                name: "param",
                table: "StarterGoals",
                newName: "Param");

            migrationBuilder.RenameColumn(
                name: "ordinal",
                table: "StarterGoals",
                newName: "Ordinal");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "StarterGoals",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "StarterGoals",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "reward_coins",
                table: "StarterGoals",
                newName: "RewardCoins");

            migrationBuilder.RenameColumn(
                name: "condition_type",
                table: "StarterGoals",
                newName: "ConditionType");

            migrationBuilder.RenameColumn(
                name: "value",
                table: "SeasonCounters",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "metric",
                table: "SeasonCounters",
                newName: "Metric");

            migrationBuilder.RenameColumn(
                name: "player_id",
                table: "SeasonCounters",
                newName: "PlayerId");

            migrationBuilder.RenameColumn(
                name: "season_id",
                table: "SeasonCounters",
                newName: "SeasonId");

            migrationBuilder.RenameIndex(
                name: "ix_season_counters_season_id_metric",
                table: "SeasonCounters",
                newName: "IX_SeasonCounters_SeasonId_Metric");

            migrationBuilder.RenameIndex(
                name: "ix_season_counters_player_id",
                table: "SeasonCounters",
                newName: "IX_SeasonCounters_PlayerId");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "ResourceTypes",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "ResourceTypes",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "logic_name",
                table: "ResourceTypes",
                newName: "LogicName");

            migrationBuilder.RenameColumn(
                name: "value",
                table: "ReceiptResources",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "is_optional",
                table: "ReceiptResources",
                newName: "IsOptional");

            migrationBuilder.RenameColumn(
                name: "is_input",
                table: "ReceiptResources",
                newName: "IsInput");

            migrationBuilder.RenameColumn(
                name: "resource_type_id",
                table: "ReceiptResources",
                newName: "ResourceTypeId");

            migrationBuilder.RenameColumn(
                name: "receipt_id",
                table: "ReceiptResources",
                newName: "ReceiptId");

            migrationBuilder.RenameIndex(
                name: "ix_receipt_resources_resource_type_id",
                table: "ReceiptResources",
                newName: "IX_ReceiptResources_ResourceTypeId");

            migrationBuilder.RenameColumn(
                name: "p256dh",
                table: "PlayerPushSubscriptions",
                newName: "P256dh");

            migrationBuilder.RenameColumn(
                name: "endpoint",
                table: "PlayerPushSubscriptions",
                newName: "Endpoint");

            migrationBuilder.RenameColumn(
                name: "auth",
                table: "PlayerPushSubscriptions",
                newName: "Auth");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "PlayerPushSubscriptions",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "player_id",
                table: "PlayerPushSubscriptions",
                newName: "PlayerId");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "PlayerPushSubscriptions",
                newName: "CreatedDate");

            migrationBuilder.RenameIndex(
                name: "ix_player_push_subscriptions_endpoint",
                table: "PlayerPushSubscriptions",
                newName: "IX_PlayerPushSubscriptions_Endpoint");

            migrationBuilder.RenameColumn(
                name: "complete_date",
                table: "PlayerGoals",
                newName: "CompleteDate");

            migrationBuilder.RenameColumn(
                name: "goal_id",
                table: "PlayerGoals",
                newName: "GoalId");

            migrationBuilder.RenameColumn(
                name: "player_id",
                table: "PlayerGoals",
                newName: "PlayerId");

            migrationBuilder.RenameColumn(
                name: "type",
                table: "PlayerEvents",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "read",
                table: "PlayerEvents",
                newName: "Read");

            migrationBuilder.RenameColumn(
                name: "date",
                table: "PlayerEvents",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "data",
                table: "PlayerEvents",
                newName: "Data");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "PlayerEvents",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "player_id",
                table: "PlayerEvents",
                newName: "PlayerId");

            migrationBuilder.RenameIndex(
                name: "ix_player_events_player_id_date",
                table: "PlayerEvents",
                newName: "IX_PlayerEvents_PlayerId_Date");

            migrationBuilder.RenameColumn(
                name: "count",
                table: "PlayerDecors",
                newName: "Count");

            migrationBuilder.RenameColumn(
                name: "decor_type_id",
                table: "PlayerDecors",
                newName: "DecorTypeId");

            migrationBuilder.RenameColumn(
                name: "player_id",
                table: "PlayerDecors",
                newName: "PlayerId");

            migrationBuilder.RenameIndex(
                name: "ix_player_decors_decor_type_id",
                table: "PlayerDecors",
                newName: "IX_PlayerDecors_DecorTypeId");

            migrationBuilder.RenameColumn(
                name: "blueprint_id",
                table: "PlayerBlueprints",
                newName: "BlueprintId");

            migrationBuilder.RenameColumn(
                name: "player_id",
                table: "PlayerBlueprints",
                newName: "PlayerId");

            migrationBuilder.RenameIndex(
                name: "ix_player_blueprints_blueprint_id",
                table: "PlayerBlueprints",
                newName: "IX_PlayerBlueprints_BlueprintId");

            migrationBuilder.RenameColumn(
                name: "value",
                table: "OrderResources",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "resource_type_id",
                table: "OrderResources",
                newName: "ResourceTypeId");

            migrationBuilder.RenameColumn(
                name: "order_id",
                table: "OrderResources",
                newName: "OrderId");

            migrationBuilder.RenameIndex(
                name: "ix_order_resources_resource_type_id",
                table: "OrderResources",
                newName: "IX_OrderResources_ResourceTypeId");

            migrationBuilder.RenameColumn(
                name: "points",
                table: "NeighborReputations",
                newName: "Points");

            migrationBuilder.RenameColumn(
                name: "neighbor_id",
                table: "NeighborReputations",
                newName: "NeighborId");

            migrationBuilder.RenameColumn(
                name: "player_id",
                table: "NeighborReputations",
                newName: "PlayerId");

            migrationBuilder.RenameIndex(
                name: "ix_neighbor_reputations_neighbor_id",
                table: "NeighborReputations",
                newName: "IX_NeighborReputations_NeighborId");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "ModificatorTypes",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "ModificatorTypes",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "logic_name",
                table: "ModificatorTypes",
                newName: "LogicName");

            migrationBuilder.RenameColumn(
                name: "date",
                table: "GuestbookEntries",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "day",
                table: "GuestbookEntries",
                newName: "Day");

            migrationBuilder.RenameColumn(
                name: "phrase_id",
                table: "GuestbookEntries",
                newName: "PhraseId");

            migrationBuilder.RenameColumn(
                name: "guest_player_id",
                table: "GuestbookEntries",
                newName: "GuestPlayerId");

            migrationBuilder.RenameColumn(
                name: "host_player_id",
                table: "GuestbookEntries",
                newName: "HostPlayerId");

            migrationBuilder.RenameIndex(
                name: "ix_guestbook_entries_guest_player_id",
                table: "GuestbookEntries",
                newName: "IX_GuestbookEntries_GuestPlayerId");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "ExpeditionTypes",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "ExpeditionTypes",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "worker_count",
                table: "ExpeditionTypes",
                newName: "WorkerCount");

            migrationBuilder.RenameColumn(
                name: "roll_count",
                table: "ExpeditionTypes",
                newName: "RollCount");

            migrationBuilder.RenameColumn(
                name: "logic_name",
                table: "ExpeditionTypes",
                newName: "LogicName");

            migrationBuilder.RenameColumn(
                name: "gold_cost",
                table: "ExpeditionTypes",
                newName: "GoldCost");

            migrationBuilder.RenameColumn(
                name: "duration_seconds",
                table: "ExpeditionTypes",
                newName: "DurationSeconds");

            migrationBuilder.RenameColumn(
                name: "weight",
                table: "ExpeditionLoot",
                newName: "Weight");

            migrationBuilder.RenameColumn(
                name: "kind",
                table: "ExpeditionLoot",
                newName: "Kind");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "ExpeditionLoot",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "resource_type_id",
                table: "ExpeditionLoot",
                newName: "ResourceTypeId");

            migrationBuilder.RenameColumn(
                name: "min_value",
                table: "ExpeditionLoot",
                newName: "MinValue");

            migrationBuilder.RenameColumn(
                name: "max_value",
                table: "ExpeditionLoot",
                newName: "MaxValue");

            migrationBuilder.RenameColumn(
                name: "is_rare",
                table: "ExpeditionLoot",
                newName: "IsRare");

            migrationBuilder.RenameColumn(
                name: "expedition_type_id",
                table: "ExpeditionLoot",
                newName: "ExpeditionTypeId");

            migrationBuilder.RenameColumn(
                name: "decor_type_id",
                table: "ExpeditionLoot",
                newName: "DecorTypeId");

            migrationBuilder.RenameColumn(
                name: "blueprint_id",
                table: "ExpeditionLoot",
                newName: "BlueprintId");

            migrationBuilder.RenameIndex(
                name: "ix_expedition_loot_expedition_type_id",
                table: "ExpeditionLoot",
                newName: "IX_ExpeditionLoot_ExpeditionTypeId");

            migrationBuilder.RenameColumn(
                name: "value",
                table: "ExpeditionEquipment",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "is_optional",
                table: "ExpeditionEquipment",
                newName: "IsOptional");

            migrationBuilder.RenameColumn(
                name: "resource_type_id",
                table: "ExpeditionEquipment",
                newName: "ResourceTypeId");

            migrationBuilder.RenameColumn(
                name: "expedition_type_id",
                table: "ExpeditionEquipment",
                newName: "ExpeditionTypeId");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "DomikTypes",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "DomikTypes",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "unlock_level",
                table: "DomikTypes",
                newName: "UnlockLevel");

            migrationBuilder.RenameColumn(
                name: "max_count",
                table: "DomikTypes",
                newName: "MaxCount");

            migrationBuilder.RenameColumn(
                name: "logic_name",
                table: "DomikTypes",
                newName: "LogicName");

            migrationBuilder.RenameColumn(
                name: "value",
                table: "DomikTypeLevels",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "upgrade_seconds",
                table: "DomikTypeLevels",
                newName: "UpgradeSeconds");

            migrationBuilder.RenameColumn(
                name: "max_manufacture_count",
                table: "DomikTypeLevels",
                newName: "MaxManufactureCount");

            migrationBuilder.RenameColumn(
                name: "domik_type_id",
                table: "DomikTypeLevels",
                newName: "DomikTypeId");

            migrationBuilder.RenameColumn(
                name: "value",
                table: "DomikTypeLevelResources",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "resource_type_id",
                table: "DomikTypeLevelResources",
                newName: "ResourceTypeId");

            migrationBuilder.RenameColumn(
                name: "domik_type_level_value",
                table: "DomikTypeLevelResources",
                newName: "DomikTypeLevelValue");

            migrationBuilder.RenameColumn(
                name: "domik_type_level_domik_type_id",
                table: "DomikTypeLevelResources",
                newName: "DomikTypeLevelDomikTypeId");

            migrationBuilder.RenameIndex(
                name: "ix_domik_type_level_resources_resource_type_id",
                table: "DomikTypeLevelResources",
                newName: "IX_DomikTypeLevelResources_ResourceTypeId");

            migrationBuilder.RenameColumn(
                name: "receipt_id",
                table: "DomikTypeLevelReceipts",
                newName: "ReceiptId");

            migrationBuilder.RenameColumn(
                name: "domik_type_level_value",
                table: "DomikTypeLevelReceipts",
                newName: "DomikTypeLevelValue");

            migrationBuilder.RenameColumn(
                name: "domik_type_level_domik_type_id",
                table: "DomikTypeLevelReceipts",
                newName: "DomikTypeLevelDomikTypeId");

            migrationBuilder.RenameIndex(
                name: "ix_domik_type_level_recepts_receipt_id",
                table: "DomikTypeLevelReceipts",
                newName: "IX_DomikTypeLevelReceipts_ReceiptId");

            migrationBuilder.RenameColumn(
                name: "value",
                table: "DomikTypeLevelModificators",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "modificator_type_id",
                table: "DomikTypeLevelModificators",
                newName: "ModificatorTypeId");

            migrationBuilder.RenameColumn(
                name: "domik_type_level_value",
                table: "DomikTypeLevelModificators",
                newName: "DomikTypeLevelValue");

            migrationBuilder.RenameColumn(
                name: "domik_type_level_domik_type_id",
                table: "DomikTypeLevelModificators",
                newName: "DomikTypeLevelDomikTypeId");

            migrationBuilder.RenameIndex(
                name: "ix_domik_type_level_modificators_modificator_type_id",
                table: "DomikTypeLevelModificators",
                newName: "IX_DomikTypeLevelModificators_ModificatorTypeId");

            migrationBuilder.RenameColumn(
                name: "ordinal",
                table: "DomikTypeCountGates",
                newName: "Ordinal");

            migrationBuilder.RenameColumn(
                name: "unlock_level",
                table: "DomikTypeCountGates",
                newName: "UnlockLevel");

            migrationBuilder.RenameColumn(
                name: "domik_type_id",
                table: "DomikTypeCountGates",
                newName: "DomikTypeId");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "DecorTypes",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "DecorTypes",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "reputation_threshold",
                table: "DecorTypes",
                newName: "ReputationThreshold");

            migrationBuilder.RenameColumn(
                name: "neighbor_id",
                table: "DecorTypes",
                newName: "NeighborId");

            migrationBuilder.RenameColumn(
                name: "logic_name",
                table: "DecorTypes",
                newName: "LogicName");

            migrationBuilder.RenameColumn(
                name: "is_purchasable",
                table: "DecorTypes",
                newName: "IsPurchasable");

            migrationBuilder.RenameColumn(
                name: "comfort_points",
                table: "DecorTypes",
                newName: "ComfortPoints");

            migrationBuilder.RenameColumn(
                name: "value",
                table: "DecorCosts",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "resource_type_id",
                table: "DecorCosts",
                newName: "ResourceTypeId");

            migrationBuilder.RenameColumn(
                name: "decor_type_id",
                table: "DecorCosts",
                newName: "DecorTypeId");

            migrationBuilder.RenameIndex(
                name: "ix_decor_costs_resource_type_id",
                table: "DecorCosts",
                newName: "IX_DecorCosts_ResourceTypeId");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "AspNetUsers",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AspNetUsers",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_name",
                table: "AspNetUsers",
                newName: "UserName");

            migrationBuilder.RenameColumn(
                name: "two_factor_enabled",
                table: "AspNetUsers",
                newName: "TwoFactorEnabled");

            migrationBuilder.RenameColumn(
                name: "security_stamp",
                table: "AspNetUsers",
                newName: "SecurityStamp");

            migrationBuilder.RenameColumn(
                name: "phone_number_confirmed",
                table: "AspNetUsers",
                newName: "PhoneNumberConfirmed");

            migrationBuilder.RenameColumn(
                name: "phone_number",
                table: "AspNetUsers",
                newName: "PhoneNumber");

            migrationBuilder.RenameColumn(
                name: "password_hash",
                table: "AspNetUsers",
                newName: "PasswordHash");

            migrationBuilder.RenameColumn(
                name: "normalized_user_name",
                table: "AspNetUsers",
                newName: "NormalizedUserName");

            migrationBuilder.RenameColumn(
                name: "normalized_email",
                table: "AspNetUsers",
                newName: "NormalizedEmail");

            migrationBuilder.RenameColumn(
                name: "lockout_end",
                table: "AspNetUsers",
                newName: "LockoutEnd");

            migrationBuilder.RenameColumn(
                name: "lockout_enabled",
                table: "AspNetUsers",
                newName: "LockoutEnabled");

            migrationBuilder.RenameColumn(
                name: "email_confirmed",
                table: "AspNetUsers",
                newName: "EmailConfirmed");

            migrationBuilder.RenameColumn(
                name: "concurrency_stamp",
                table: "AspNetUsers",
                newName: "ConcurrencyStamp");

            migrationBuilder.RenameColumn(
                name: "access_failed_count",
                table: "AspNetUsers",
                newName: "AccessFailedCount");

            migrationBuilder.RenameColumn(
                name: "value",
                table: "AspNetUserTokens",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "AspNetUserTokens",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "login_provider",
                table: "AspNetUserTokens",
                newName: "LoginProvider");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "AspNetUserTokens",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "role_id",
                table: "AspNetUserRoles",
                newName: "RoleId");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "AspNetUserRoles",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "ix_asp_net_user_roles_role_id",
                table: "AspNetUserRoles",
                newName: "IX_AspNetUserRoles_RoleId");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "AspNetUserLogins",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "provider_display_name",
                table: "AspNetUserLogins",
                newName: "ProviderDisplayName");

            migrationBuilder.RenameColumn(
                name: "provider_key",
                table: "AspNetUserLogins",
                newName: "ProviderKey");

            migrationBuilder.RenameColumn(
                name: "login_provider",
                table: "AspNetUserLogins",
                newName: "LoginProvider");

            migrationBuilder.RenameIndex(
                name: "ix_asp_net_user_logins_user_id",
                table: "AspNetUserLogins",
                newName: "IX_AspNetUserLogins_UserId");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AspNetUserClaims",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "AspNetUserClaims",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "claim_value",
                table: "AspNetUserClaims",
                newName: "ClaimValue");

            migrationBuilder.RenameColumn(
                name: "claim_type",
                table: "AspNetUserClaims",
                newName: "ClaimType");

            migrationBuilder.RenameIndex(
                name: "ix_asp_net_user_claims_user_id",
                table: "AspNetUserClaims",
                newName: "IX_AspNetUserClaims_UserId");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "AspNetRoles",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AspNetRoles",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "normalized_name",
                table: "AspNetRoles",
                newName: "NormalizedName");

            migrationBuilder.RenameColumn(
                name: "concurrency_stamp",
                table: "AspNetRoles",
                newName: "ConcurrencyStamp");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AspNetRoleClaims",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "role_id",
                table: "AspNetRoleClaims",
                newName: "RoleId");

            migrationBuilder.RenameColumn(
                name: "claim_value",
                table: "AspNetRoleClaims",
                newName: "ClaimValue");

            migrationBuilder.RenameColumn(
                name: "claim_type",
                table: "AspNetRoleClaims",
                newName: "ClaimType");

            migrationBuilder.RenameIndex(
                name: "ix_asp_net_role_claims_role_id",
                table: "AspNetRoleClaims",
                newName: "IX_AspNetRoleClaims_RoleId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Workers",
                table: "Workers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Traits",
                table: "Traits",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tolokas",
                table: "Tolokas",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Resources",
                table: "Resources",
                columns: new[] { "PlayerId", "TypeId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Receipts",
                table: "Receipts",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Players",
                table: "Players",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Orders",
                table: "Orders",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Neighbors",
                table: "Neighbors",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Manufactures",
                table: "Manufactures",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Incidents",
                table: "Incidents",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Expeditions",
                table: "Expeditions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Errands",
                table: "Errands",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Domiks",
                table: "Domiks",
                columns: new[] { "PlayerId", "Id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Blueprints",
                table: "Blueprints",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkerSkills",
                table: "WorkerSkills",
                columns: new[] { "WorkerId", "DomikTypeId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkerMilestones",
                table: "WorkerMilestones",
                columns: new[] { "WorkerId", "MilestoneType" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_WeatherTypes",
                table: "WeatherTypes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WeatherTypeEffects",
                table: "WeatherTypeEffects",
                columns: new[] { "WeatherTypeId", "DomikTypeId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_WeatherPeriods",
                table: "WeatherPeriods",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TradeLots",
                table: "TradeLots",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TolokaVotes",
                table: "TolokaVotes",
                columns: new[] { "TolokaId", "PlayerId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_TolokaTypes",
                table: "TolokaTypes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TolokaTypePositions",
                table: "TolokaTypePositions",
                columns: new[] { "TolokaTypeId", "ResourceTypeId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_TolokaTypeEffects",
                table: "TolokaTypeEffects",
                columns: new[] { "TolokaTypeId", "DomikTypeId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_TolokaPositions",
                table: "TolokaPositions",
                columns: new[] { "TolokaId", "ResourceTypeId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_TolokaContributions",
                table: "TolokaContributions",
                columns: new[] { "TolokaId", "PlayerId", "ResourceTypeId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_StarterGoals",
                table: "StarterGoals",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SeasonCounters",
                table: "SeasonCounters",
                columns: new[] { "SeasonId", "PlayerId", "Metric" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ResourceTypes",
                table: "ResourceTypes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReceiptResources",
                table: "ReceiptResources",
                columns: new[] { "ReceiptId", "ResourceTypeId", "IsInput" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlayerPushSubscriptions",
                table: "PlayerPushSubscriptions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlayerGoals",
                table: "PlayerGoals",
                columns: new[] { "PlayerId", "GoalId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlayerEvents",
                table: "PlayerEvents",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlayerDecors",
                table: "PlayerDecors",
                columns: new[] { "PlayerId", "DecorTypeId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlayerBlueprints",
                table: "PlayerBlueprints",
                columns: new[] { "PlayerId", "BlueprintId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderResources",
                table: "OrderResources",
                columns: new[] { "OrderId", "ResourceTypeId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_NeighborReputations",
                table: "NeighborReputations",
                columns: new[] { "PlayerId", "NeighborId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ModificatorTypes",
                table: "ModificatorTypes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GuestbookEntries",
                table: "GuestbookEntries",
                columns: new[] { "HostPlayerId", "GuestPlayerId", "Day" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExpeditionTypes",
                table: "ExpeditionTypes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExpeditionLoot",
                table: "ExpeditionLoot",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExpeditionEquipment",
                table: "ExpeditionEquipment",
                columns: new[] { "ExpeditionTypeId", "ResourceTypeId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_DomikTypes",
                table: "DomikTypes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DomikTypeLevels",
                table: "DomikTypeLevels",
                columns: new[] { "DomikTypeId", "Value" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_DomikTypeLevelResources",
                table: "DomikTypeLevelResources",
                columns: new[] { "DomikTypeLevelDomikTypeId", "DomikTypeLevelValue", "ResourceTypeId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_DomikTypeLevelReceipts",
                table: "DomikTypeLevelReceipts",
                columns: new[] { "DomikTypeLevelDomikTypeId", "DomikTypeLevelValue", "ReceiptId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_DomikTypeLevelModificators",
                table: "DomikTypeLevelModificators",
                columns: new[] { "DomikTypeLevelDomikTypeId", "DomikTypeLevelValue", "ModificatorTypeId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_DomikTypeCountGates",
                table: "DomikTypeCountGates",
                columns: new[] { "DomikTypeId", "Ordinal" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_DecorTypes",
                table: "DecorTypes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DecorCosts",
                table: "DecorCosts",
                columns: new[] { "DecorTypeId", "ResourceTypeId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUsers",
                table: "AspNetUsers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserTokens",
                table: "AspNetUserTokens",
                columns: new[] { "UserId", "LoginProvider", "Name" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles",
                columns: new[] { "UserId", "RoleId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserLogins",
                table: "AspNetUserLogins",
                columns: new[] { "LoginProvider", "ProviderKey" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserClaims",
                table: "AspNetUserClaims",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetRoles",
                table: "AspNetRoles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetRoleClaims",
                table: "AspNetRoleClaims",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Players_VillageName",
                table: "Players",
                column: "VillageName",
                unique: true,
                filter: "\"VillageName\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Blueprints_DomikTypes_DomikTypeId",
                table: "Blueprints",
                column: "DomikTypeId",
                principalTable: "DomikTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Blueprints_Neighbors_NeighborId",
                table: "Blueprints",
                column: "NeighborId",
                principalTable: "Neighbors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DecorCosts_DecorTypes_DecorTypeId",
                table: "DecorCosts",
                column: "DecorTypeId",
                principalTable: "DecorTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DecorCosts_ResourceTypes_ResourceTypeId",
                table: "DecorCosts",
                column: "ResourceTypeId",
                principalTable: "ResourceTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DomikTypeLevelModificators_DomikTypeLevels_DomikTypeLevelDo~",
                table: "DomikTypeLevelModificators",
                columns: new[] { "DomikTypeLevelDomikTypeId", "DomikTypeLevelValue" },
                principalTable: "DomikTypeLevels",
                principalColumns: new[] { "DomikTypeId", "Value" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DomikTypeLevelModificators_ModificatorTypes_ModificatorType~",
                table: "DomikTypeLevelModificators",
                column: "ModificatorTypeId",
                principalTable: "ModificatorTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DomikTypeLevelReceipts_DomikTypeLevels_DomikTypeLevelDomikT~",
                table: "DomikTypeLevelReceipts",
                columns: new[] { "DomikTypeLevelDomikTypeId", "DomikTypeLevelValue" },
                principalTable: "DomikTypeLevels",
                principalColumns: new[] { "DomikTypeId", "Value" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DomikTypeLevelReceipts_Receipts_ReceiptId",
                table: "DomikTypeLevelReceipts",
                column: "ReceiptId",
                principalTable: "Receipts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DomikTypeLevelResources_DomikTypeLevels_DomikTypeLevelDomik~",
                table: "DomikTypeLevelResources",
                columns: new[] { "DomikTypeLevelDomikTypeId", "DomikTypeLevelValue" },
                principalTable: "DomikTypeLevels",
                principalColumns: new[] { "DomikTypeId", "Value" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DomikTypeLevelResources_ResourceTypes_ResourceTypeId",
                table: "DomikTypeLevelResources",
                column: "ResourceTypeId",
                principalTable: "ResourceTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DomikTypeLevels_DomikTypes_DomikTypeId",
                table: "DomikTypeLevels",
                column: "DomikTypeId",
                principalTable: "DomikTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Errands_Neighbors_NeighborId",
                table: "Errands",
                column: "NeighborId",
                principalTable: "Neighbors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Errands_Players_PlayerId",
                table: "Errands",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpeditionEquipment_ExpeditionTypes_ExpeditionTypeId",
                table: "ExpeditionEquipment",
                column: "ExpeditionTypeId",
                principalTable: "ExpeditionTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpeditionLoot_ExpeditionTypes_ExpeditionTypeId",
                table: "ExpeditionLoot",
                column: "ExpeditionTypeId",
                principalTable: "ExpeditionTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Expeditions_ExpeditionTypes_ExpeditionTypeId",
                table: "Expeditions",
                column: "ExpeditionTypeId",
                principalTable: "ExpeditionTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Expeditions_Players_PlayerId",
                table: "Expeditions",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GuestbookEntries_Players_GuestPlayerId",
                table: "GuestbookEntries",
                column: "GuestPlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GuestbookEntries_Players_HostPlayerId",
                table: "GuestbookEntries",
                column: "HostPlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Incidents_Players_PlayerId",
                table: "Incidents",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Manufactures_Domiks_DomikPlayerId_DomikId",
                table: "Manufactures",
                columns: new[] { "DomikPlayerId", "DomikId" },
                principalTable: "Domiks",
                principalColumns: new[] { "PlayerId", "Id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NeighborReputations_Neighbors_NeighborId",
                table: "NeighborReputations",
                column: "NeighborId",
                principalTable: "Neighbors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NeighborReputations_Players_PlayerId",
                table: "NeighborReputations",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderResources_Orders_OrderId",
                table: "OrderResources",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderResources_ResourceTypes_ResourceTypeId",
                table: "OrderResources",
                column: "ResourceTypeId",
                principalTable: "ResourceTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Neighbors_NeighborId",
                table: "Orders",
                column: "NeighborId",
                principalTable: "Neighbors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Players_PlayerId",
                table: "Orders",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerBlueprints_Blueprints_BlueprintId",
                table: "PlayerBlueprints",
                column: "BlueprintId",
                principalTable: "Blueprints",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerBlueprints_Players_PlayerId",
                table: "PlayerBlueprints",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerDecors_DecorTypes_DecorTypeId",
                table: "PlayerDecors",
                column: "DecorTypeId",
                principalTable: "DecorTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerDecors_Players_PlayerId",
                table: "PlayerDecors",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReceiptResources_Receipts_ReceiptId",
                table: "ReceiptResources",
                column: "ReceiptId",
                principalTable: "Receipts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReceiptResources_ResourceTypes_ResourceTypeId",
                table: "ReceiptResources",
                column: "ResourceTypeId",
                principalTable: "ResourceTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_Players_PlayerId",
                table: "Resources",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SeasonCounters_Players_PlayerId",
                table: "SeasonCounters",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TolokaContributions_Players_PlayerId",
                table: "TolokaContributions",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TolokaContributions_Tolokas_TolokaId",
                table: "TolokaContributions",
                column: "TolokaId",
                principalTable: "Tolokas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TolokaPositions_Tolokas_TolokaId",
                table: "TolokaPositions",
                column: "TolokaId",
                principalTable: "Tolokas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tolokas_TolokaTypes_TolokaTypeId",
                table: "Tolokas",
                column: "TolokaTypeId",
                principalTable: "TolokaTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TolokaTypeEffects_TolokaTypes_TolokaTypeId",
                table: "TolokaTypeEffects",
                column: "TolokaTypeId",
                principalTable: "TolokaTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TolokaTypePositions_TolokaTypes_TolokaTypeId",
                table: "TolokaTypePositions",
                column: "TolokaTypeId",
                principalTable: "TolokaTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TolokaVotes_Tolokas_TolokaId",
                table: "TolokaVotes",
                column: "TolokaId",
                principalTable: "Tolokas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TradeLots_Players_SellerId",
                table: "TradeLots",
                column: "SellerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WeatherPeriods_WeatherTypes_WeatherTypeId",
                table: "WeatherPeriods",
                column: "WeatherTypeId",
                principalTable: "WeatherTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WeatherTypeEffects_WeatherTypes_WeatherTypeId",
                table: "WeatherTypeEffects",
                column: "WeatherTypeId",
                principalTable: "WeatherTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkerMilestones_Workers_WorkerId",
                table: "WorkerMilestones",
                column: "WorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Workers_Errands_ErrandId",
                table: "Workers",
                column: "ErrandId",
                principalTable: "Errands",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Workers_Expeditions_ExpeditionId",
                table: "Workers",
                column: "ExpeditionId",
                principalTable: "Expeditions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Workers_Incidents_IncidentId",
                table: "Workers",
                column: "IncidentId",
                principalTable: "Incidents",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Workers_Manufactures_ManufactureId",
                table: "Workers",
                column: "ManufactureId",
                principalTable: "Manufactures",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Workers_Players_PlayerId",
                table: "Workers",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Workers_Traits_TraitId",
                table: "Workers",
                column: "TraitId",
                principalTable: "Traits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkerSkills_Workers_WorkerId",
                table: "WorkerSkills",
                column: "WorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
