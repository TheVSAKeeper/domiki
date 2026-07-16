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
    public required DomikType[] DomikTypes { get; init; }
    public required Receipt[] Receipts { get; init; }
    public required ResourceType[] ResourceTypes { get; init; }
    public required Neighbor[] Neighbors { get; init; }
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
    public required int PlodderModificatorId { get; init; }

    public static SimulationData Load(ResourceManager resourceManager)
    {
        var domikTypes = resourceManager.GetDomikTypes().OrderBy(x => x.Id).ToArray();
        var receipts = resourceManager.GetReceipts().OrderBy(x => x.Id).ToArray();
        var resourceTypes = resourceManager.GetResourceTypes().OrderBy(x => x.Id).ToArray();
        var neighbors = resourceManager.GetNeighbors().OrderBy(x => x.Id).ToArray();
        var blueprints = resourceManager.GetBlueprints().OrderBy(x => x.Id).ToArray();
        var weatherTypes = resourceManager.GetWeatherTypes().OrderBy(x => x.Id).ToArray();
        var expeditionTypes = resourceManager.GetExpeditionTypes().OrderBy(x => x.Id).ToArray();
        var traits = resourceManager.GetTraits().OrderBy(x => x.Id).ToArray();
        var modificatorTypes = resourceManager.GetModificatorTypes().OrderBy(x => x.Id).ToArray();
        var starterGoals = resourceManager.GetStarterGoals().OrderBy(x => x.Ordinal).ToArray();
        var plodder = modificatorTypes.Single(x => x.LogicName == "plodder").Id;

        return new SimulationData
        {
            DomikTypes = domikTypes,
            Receipts = receipts,
            ResourceTypes = resourceTypes,
            Neighbors = neighbors,
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
            PlodderModificatorId = plodder,
        };
    }
}
