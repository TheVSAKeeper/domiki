using Domiki.Web.Activities.Models;
using Domiki.Web.Core.Models;
using Domiki.Web.Economy.Models;
using Domiki.Web.Reference.Models;
using Domiki.Web.Reference;
using Domiki.Web.Village.Models;
using Domiki.Web.Workers.Models;
using StarterGoal = Domiki.Web.Data.Entities.StarterGoal;

namespace Domiki.BalanceSim;

public sealed class SimulationData
{
    /// <summary>
    /// Монеты – валюта, в которой считается монетный эквивалент стока.
    /// </summary>
    public const int CoinResourceTypeId = 1;

    /// <summary>
    /// Золото – премиальная валюта, тоже считается ликвидной.
    /// </summary>
    public const int GoldResourceTypeId = 5;

    public required DomikType[] DomikTypes { get; init; }
    public required Receipt[] Receipts { get; init; }
    public required ResourceType[] ResourceTypes { get; init; }
    public required Neighbor[] Neighbors { get; init; }
    public required VillageProfileEffect[] VillageProfileEffects { get; init; }
    public required Blueprint[] Blueprints { get; init; }
    public required WeatherType[] WeatherTypes { get; init; }
    public required ExpeditionType[] ExpeditionTypes { get; init; }
    public required Trait[] Traits { get; init; }
    public required ModificatorType[] ModificatorTypes { get; init; }
    public required StarterGoal[] StarterGoals { get; init; }
    public required Dictionary<int, DomikType> DomikTypeById { get; init; }
    public required Dictionary<int, Receipt> ReceiptById { get; init; }
    public required Dictionary<int, ResourceType> ResourceTypeById { get; init; }
    public required Dictionary<int, Neighbor> NeighborById { get; init; }
    public required Dictionary<int, WeatherType> WeatherTypeById { get; init; }
    public required Dictionary<int, Trait> TraitById { get; init; }
    public required Dictionary<(int NeighborId, int DomikTypeId), int> VillageProfileDurationPercentByKey { get; init; }
    public required Dictionary<(int DomikTypeId, int Ordinal), int> CountGateLevelByKey { get; init; }
    public required int PlodderModificatorId { get; init; }

    /// <summary>
    /// Типы ресурсов, которые обращаются в монеты: сами монеты и золото, спрос соседей и всё, что забирает рецепт продажи.
    /// </summary>
    /// <remarks>
    /// Остальное – ремесленные ключи и подобное – в монетном эквиваленте не считается: рыночная цена у них справочная,
    /// продать их некому, и без этой границы модель приняла бы неликвидный сток за доход.
    /// </remarks>
    public required HashSet<int> LiquidResourceTypeIds { get; init; }

    /// <summary>
    /// Обращается ли ресурс в монеты (см. <see cref="LiquidResourceTypeIds"/>).
    /// </summary>
    /// <param name="resourceTypeId">Проверяемый тип ресурса.</param>
    /// <returns><see langword="true"/> для ликвидного ресурса.</returns>
    public bool IsLiquid(int resourceTypeId)
    {
        return LiquidResourceTypeIds.Contains(resourceTypeId);
    }

    public static SimulationData Load(ResourceManager resourceManager)
    {
        var domikTypes = resourceManager.GetDomikTypes().OrderBy(x => x.Id).ToArray();
        var receipts = resourceManager.GetReceipts().OrderBy(x => x.Id).ToArray();
        var resourceTypes = resourceManager.GetResourceTypes().OrderBy(x => x.Id).ToArray();
        var neighbors = resourceManager.GetNeighbors().OrderBy(x => x.Id).ToArray();
        var villageProfileEffects = resourceManager.GetVillageProfileEffects().OrderBy(x => x.NeighborId).ThenBy(x => x.DomikTypeId).ToArray();
        var blueprints = resourceManager.GetBlueprints().OrderBy(x => x.Id).ToArray();
        var weatherTypes = resourceManager.GetWeatherTypes().OrderBy(x => x.Id).ToArray();
        var expeditionTypes = resourceManager.GetExpeditionTypes().OrderBy(x => x.Id).ToArray();
        var traits = resourceManager.GetTraits().OrderBy(x => x.Id).ToArray();
        var modificatorTypes = resourceManager.GetModificatorTypes().OrderBy(x => x.Id).ToArray();
        var starterGoals = resourceManager.GetStarterGoals().OrderBy(x => x.Ordinal).ToArray();
        var countGates = resourceManager.GetDomikTypeCountGates().OrderBy(x => x.DomikTypeId).ThenBy(x => x.Ordinal).ToArray();
        var plodder = modificatorTypes.Single(x => x.LogicName == "plodder").Id;

        var liquid = new HashSet<int> { CoinResourceTypeId, GoldResourceTypeId };
        liquid.UnionWith(neighbors.SelectMany(x => new[] { x.PrimaryResourceTypeId, x.SecondaryResourceTypeId ?? 0 }).Where(x => x != 0));
        liquid.UnionWith(receipts
            .Where(x => x.OutputResources.All(output => output.Type.Id == CoinResourceTypeId))
            .SelectMany(x => x.InputResources.Select(input => input.Type.Id)));

        return new SimulationData
        {
            DomikTypes = domikTypes,
            Receipts = receipts,
            ResourceTypes = resourceTypes,
            Neighbors = neighbors,
            VillageProfileEffects = villageProfileEffects,
            Blueprints = blueprints,
            WeatherTypes = weatherTypes,
            ExpeditionTypes = expeditionTypes,
            Traits = traits,
            ModificatorTypes = modificatorTypes,
            StarterGoals = starterGoals,
            DomikTypeById = domikTypes.ToDictionary(x => x.Id),
            ReceiptById = receipts.ToDictionary(x => x.Id),
            ResourceTypeById = resourceTypes.ToDictionary(x => x.Id),
            NeighborById = neighbors.ToDictionary(x => x.Id),
            WeatherTypeById = weatherTypes.ToDictionary(x => x.Id),
            TraitById = traits.ToDictionary(x => x.Id),
            VillageProfileDurationPercentByKey = villageProfileEffects.ToDictionary(x => (x.NeighborId, x.DomikTypeId), x => x.DurationPercent),
            CountGateLevelByKey = countGates.ToDictionary(x => (x.DomikTypeId, x.Ordinal), x => x.UnlockLevel),
            PlodderModificatorId = plodder,
            LiquidResourceTypeIds = liquid,
        };
    }
}
