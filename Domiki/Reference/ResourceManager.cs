using Domiki.Web.Core.Models;
using Domiki.Web.Data;
using Domiki.Web.Data.Entities;
using Domiki.Web.Reference.Models;
using Microsoft.EntityFrameworkCore;
using Blueprint = Domiki.Web.Activities.Models.Blueprint;
using DecorType = Domiki.Web.Village.Models.DecorType;
using DomikType = Domiki.Web.Core.Models.DomikType;
using DomikTypeCountGate = Domiki.Web.Core.Models.DomikTypeCountGate;
using ExpeditionEquipment = Domiki.Web.Activities.Models.ExpeditionEquipment;
using ExpeditionLoot = Domiki.Web.Activities.Models.ExpeditionLoot;
using ExpeditionType = Domiki.Web.Activities.Models.ExpeditionType;
using ModificatorType = Domiki.Web.Reference.Models.ModificatorType;
using Neighbor = Domiki.Web.Economy.Models.Neighbor;
using Receipt = Domiki.Web.Reference.Models.Receipt;
using Resource = Domiki.Web.Reference.Models.Resource;
using ResourceType = Domiki.Web.Reference.Models.ResourceType;
using SickType = Domiki.Web.Village.Models.SickType;
using TolokaType = Domiki.Web.Activities.Models.TolokaType;
using TolokaTypePosition = Domiki.Web.Activities.Models.TolokaTypePosition;
using Trait = Domiki.Web.Workers.Models.Trait;
using WeatherType = Domiki.Web.Village.Models.WeatherType;
using WeatherTypeEffect = Domiki.Web.Village.Models.WeatherTypeEffect;

namespace Domiki.Web.Reference;

public class ResourceManager
{
    public const int BaseMarketValue = 10;

    private sealed class Snapshot
    {
        public required ModificatorType[] ModificatorTypes { get; init; }
        public required ResourceType[] ResourceTypes { get; init; }
        public required Receipt[] Receipts { get; init; }
        public required DomikType[] DomikTypes { get; init; }
        public required DomikTypeCountGate[] DomikTypeCountGates { get; init; }
        public required Neighbor[] Neighbors { get; init; }
        public required Trait[] Traits { get; init; }
        public required WeatherType[] WeatherTypes { get; init; }
        public required SickType[] SickTypes { get; init; }
        public required Blueprint[] Blueprints { get; init; }
        public required ExpeditionType[] ExpeditionTypes { get; init; }
        public required DecorType[] DecorTypes { get; init; }
        public required TolokaType[] TolokaTypes { get; init; }
        public required StarterGoal[] StarterGoals { get; init; }
    }

    private readonly Lazy<Snapshot> _data;

    public ResourceManager(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _data = new Lazy<Snapshot>(() =>
        {
            using var context = contextFactory.CreateDbContext();
            return new Snapshot
            {
                ModificatorTypes = LoadModificatorTypes(context),
                ResourceTypes = LoadResourceTypes(context),
                Receipts = LoadReceipts(context),
                DomikTypes = LoadDomikTypes(context),
                DomikTypeCountGates = LoadDomikTypeCountGates(context),
                Neighbors = LoadNeighbors(context),
                Traits = LoadTraits(context),
                WeatherTypes = LoadWeatherTypes(context),
                SickTypes = LoadSickTypes(context),
                Blueprints = LoadBlueprints(context),
                ExpeditionTypes = LoadExpeditionTypes(context),
                DecorTypes = LoadDecorTypes(context),
                TolokaTypes = LoadTolokaTypes(context),
                StarterGoals = LoadStarterGoals(context),
            };
        });
    }

    public static int GetMarketValue(int resourceTypeId)
    {
        return resourceTypeId switch
        {
            5 => 100,
            6 or 7 or 10 or 14 or 17 => 35,
            8 => 55,
            9 => 95,
            11 => 150,
            12 => 45,
            13 => 10,
            16 => 10,
            18 => 10,
            19 => 40,
            20 => 120,
            15 => 20,
            _ => BaseMarketValue,
        };
    }

    public ModificatorType[] GetModificatorTypes() => _data.Value.ModificatorTypes;

    public Trait[] GetTraits() => _data.Value.Traits;

    public ResourceType[] GetResourceTypes() => _data.Value.ResourceTypes;

    public StarterGoal[] GetStarterGoals() => _data.Value.StarterGoals;

    public Receipt[] GetReceipts() => _data.Value.Receipts;

    public Neighbor[] GetNeighbors() => _data.Value.Neighbors;

    public Blueprint[] GetBlueprints() => _data.Value.Blueprints;

    public WeatherType[] GetWeatherTypes() => _data.Value.WeatherTypes;

    /// <summary>
    /// Возвращает справочник хворей, связанных с погодой.
    /// </summary>
    /// <returns>Все типы хворей.</returns>
    public SickType[] GetSickTypes() => _data.Value.SickTypes;

    public ExpeditionType[] GetExpeditionTypes() => _data.Value.ExpeditionTypes;

    public TolokaType[] GetTolokaTypes() => _data.Value.TolokaTypes;

    public DecorType[] GetDecorTypes() => _data.Value.DecorTypes;

    public DomikType[] GetDomikTypes() => _data.Value.DomikTypes;

    public DomikTypeCountGate[] GetDomikTypeCountGates() => _data.Value.DomikTypeCountGates;

    private static ModificatorType[] LoadModificatorTypes(ApplicationDbContext context)
    {
        return context.ModificatorTypes.Select(x => new ModificatorType
            {
                Id = x.Id,
                LogicName = x.LogicName,
                Name = x.Name,
            })
            .ToArray();
    }

    private static Trait[] LoadTraits(ApplicationDbContext context)
    {
        return context.Traits.Select(x => new Trait
            {
                Id = x.Id,
                LogicName = x.LogicName,
                Name = x.Name,
                DurationPercent = x.DurationPercent,
                NoFatigue = x.NoFatigue,
                NoSick = x.NoSick,
                LuckWeightPercent = x.LuckWeightPercent,
            })
            .ToArray();
    }

    private static ResourceType[] LoadResourceTypes(ApplicationDbContext context)
    {
        return context.ResourceTypes
            .Select(x => new ResourceType { Id = x.Id, LogicName = x.LogicName, Name = x.Name })
            .ToArray();
    }

    private static StarterGoal[] LoadStarterGoals(ApplicationDbContext context)
    {
        return context.StarterGoals.AsNoTracking().OrderBy(x => x.Ordinal).ToArray();
    }

    private static Receipt[] LoadReceipts(ApplicationDbContext context)
    {
        return context.Receipts.Include(x => x.Resources)
            .ToArray()
            .Select(x => new Receipt
            {
                Id = x.Id,
                LogicName = x.LogicName,
                Name = x.Name,
                PlodderCount = x.PlodderCount,
                DurationSeconds = x.DurationSeconds,
                OutputBonusPercent = x.OutputBonusPercent,
                InputResources = x.Resources.Where(x => x.IsInput && !x.IsOptional)
                    .Select(x => new Resource
                    {
                        Type = new()
                            { Id = x.ResourceTypeId },
                        Value = x.Value,
                    })
                    .ToArray(),
                OptionalInputResources = x.Resources.Where(x => x.IsInput && x.IsOptional)
                    .Select(x => new Resource
                    {
                        Type = new()
                            { Id = x.ResourceTypeId },
                        Value = x.Value,
                    })
                    .ToArray(),
                OutputResources = x.Resources.Where(x => !x.IsInput)
                    .Select(x => new Resource
                    {
                        Type = new()
                            { Id = x.ResourceTypeId },
                        Value = x.Value,
                    })
                    .ToArray(),
            })
            .ToArray();
    }

    private static Neighbor[] LoadNeighbors(ApplicationDbContext context)
    {
        return context.Neighbors.Select(x => new Neighbor
            {
                Id = x.Id,
                Name = x.Name,
                LogicName = x.LogicName,
                PrimaryResourceTypeId = x.PrimaryResourceTypeId,
                SecondaryResourceTypeId = x.SecondaryResourceTypeId,
                UnlockLevel = x.UnlockLevel,
            })
            .ToArray();
    }

    private static Blueprint[] LoadBlueprints(ApplicationDbContext context)
    {
        return context.Blueprints.Select(x => new Blueprint
            {
                Id = x.Id,
                Name = x.Name,
                LogicName = x.LogicName,
                DomikTypeId = x.DomikTypeId,
                NeighborId = x.NeighborId,
                ReputationThreshold = x.ReputationThreshold,
            })
            .ToArray();
    }

    private static WeatherType[] LoadWeatherTypes(ApplicationDbContext context)
    {
        var effects = context.WeatherTypeEffects.ToArray();
        var weatherTypes = context.WeatherTypes.Select(x => new WeatherType
            {
                Id = x.Id,
                Name = x.Name,
                LogicName = x.LogicName,
                RotationWeight = x.RotationWeight,
            })
            .ToArray();

        foreach (var weatherType in weatherTypes)
        {
            weatherType.Effects = effects
                .Where(x => x.WeatherTypeId == weatherType.Id)
                .Select(x => new WeatherTypeEffect { DomikTypeId = x.DomikTypeId, OutputPercent = x.OutputPercent })
                .ToArray();
        }

        return weatherTypes;
    }

    /// <summary>
    /// Загружает справочник хворей из базы данных.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    /// <returns>Все типы хворей.</returns>
    private static SickType[] LoadSickTypes(ApplicationDbContext context)
    {
        return context.SickTypes.Select(x => new SickType
            {
                Id = x.Id,
                Name = x.Name,
                LogicName = x.LogicName,
                WeatherTypeId = x.WeatherTypeId,
                CloakProtects = x.CloakProtects,
            })
            .ToArray();
    }

    private static ExpeditionType[] LoadExpeditionTypes(ApplicationDbContext context)
    {
        var loot = context.ExpeditionLoot.ToArray();
        var equipment = context.ExpeditionEquipment.ToArray();
        var expeditionTypes = context.ExpeditionTypes.Select(x => new ExpeditionType
            {
                Id = x.Id,
                Name = x.Name,
                LogicName = x.LogicName,
                DurationSeconds = x.DurationSeconds,
                WorkerCount = x.WorkerCount,
                GoldCost = x.GoldCost,
                RollCount = x.RollCount,
            })
            .ToArray();

        foreach (var expeditionType in expeditionTypes)
        {
            expeditionType.Loot = loot
                .Where(x => x.ExpeditionTypeId == expeditionType.Id)
                .Select(x => new ExpeditionLoot
                {
                    Kind = x.Kind,
                    ResourceTypeId = x.ResourceTypeId,
                    DecorTypeId = x.DecorTypeId,
                    BlueprintId = x.BlueprintId,
                    MinValue = x.MinValue,
                    MaxValue = x.MaxValue,
                    Weight = x.Weight,
                    IsRare = x.IsRare,
                })
                .ToArray();

            expeditionType.Equipment = equipment
                .Where(x => x.ExpeditionTypeId == expeditionType.Id)
                .Select(x => new ExpeditionEquipment
                {
                    ResourceTypeId = x.ResourceTypeId,
                    Value = x.Value,
                    IsOptional = x.IsOptional,
                })
                .ToArray();
        }

        return expeditionTypes;
    }

    private static TolokaType[] LoadTolokaTypes(ApplicationDbContext context)
    {
        var effects = context.TolokaTypeEffects.ToArray();
        var positions = context.TolokaTypePositions.ToArray();
        var tolokaTypes = context.TolokaTypes.Select(x => new TolokaType
            {
                Id = x.Id,
                Name = x.Name,
                LogicName = x.LogicName,
                RotationWeight = x.RotationWeight,
            })
            .ToArray();

        foreach (var tolokaType in tolokaTypes)
        {
            tolokaType.Effects = effects
                .Where(x => x.TolokaTypeId == tolokaType.Id)
                .Select(x => new TolokaTypeEffect { DomikTypeId = x.DomikTypeId, OutputPercent = x.OutputPercent })
                .ToArray();

            tolokaType.Positions = positions
                .Where(x => x.TolokaTypeId == tolokaType.Id)
                .Select(x => new TolokaTypePosition { ResourceTypeId = x.ResourceTypeId, Goal = x.Goal })
                .ToArray();
        }

        return tolokaTypes;
    }

    private static DecorType[] LoadDecorTypes(ApplicationDbContext context)
    {
        var costs = context.DecorCosts.ToArray();
        var decorTypes = context.DecorTypes.Select(x => new DecorType
            {
                Id = x.Id,
                Name = x.Name,
                LogicName = x.LogicName,
                ComfortPoints = x.ComfortPoints,
                MaxCount = x.MaxCount,
                IsPurchasable = x.IsPurchasable,
                NeighborId = x.NeighborId,
                ReputationThreshold = x.ReputationThreshold,
                RequiresDecorTypeId = x.RequiresDecorTypeId,
            })
            .ToArray();

        foreach (var decorType in decorTypes)
        {
            decorType.Cost = costs
                .Where(x => x.DecorTypeId == decorType.Id)
                .Select(x => new Resource
                {
                    Type = new()
                        { Id = x.ResourceTypeId },
                    Value = x.Value,
                })
                .ToArray();
        }

        return decorTypes;
    }

    private static DomikType[] LoadDomikTypes(ApplicationDbContext context)
    {
        var modificators = context.DomikTypeLevelModificators.ToArray();
        var recepts = context.DomikTypeLevelRecepts.ToArray();
        var resources = context.DomikTypeLevelResources.ToArray();
        return context.DomikTypes.Include(x => x.Levels)
            .ToArray()
            .OrderBy(x => x.Id)
            .Select(domikType => new DomikType
            {
                Id = domikType.Id,
                LogicName = domikType.LogicName,
                Name = domikType.Name,
                MaxCount = domikType.MaxCount,
                UnlockLevel = domikType.UnlockLevel,
                Levels = domikType.Levels.OrderBy(level => level.Value)
                    .Select(level => new UpgradeLevel
                    {
                        Value = level.Value,
                        UpgradeSeconds = level.UpgradeSeconds,
                        MaxManufactureCount = level.MaxManufactureCount,
                        Modificators = modificators
                            .Where(m => m.DomikTypeLevelDomikTypeId == domikType.Id
                                        && m.DomikTypeLevelValue == level.Value)
                            .OrderBy(m => m.ModificatorTypeId)
                            .Select(x => new Modificator
                            {
                                Type = new()
                                    { Id = x.ModificatorTypeId },
                                Value = x.Value,
                            })
                            .ToArray(),
                        Receipts = recepts
                            .Where(m => m.DomikTypeLevelDomikTypeId == domikType.Id
                                        && m.DomikTypeLevelValue == level.Value)
                            .OrderBy(m => m.ReceiptId)
                            .Select(x => new Receipt { Id = x.ReceiptId })
                            .ToArray(),
                        Resources = resources
                            .Where(m => m.DomikTypeLevelDomikTypeId == domikType.Id
                                        && m.DomikTypeLevelValue == level.Value)
                            .OrderBy(m => m.ResourceTypeId)
                            .Select(x => new Resource
                            {
                                Type = new()
                                    { Id = x.ResourceTypeId },
                                Value = x.Value,
                            })
                            .ToArray(),
                    })
                    .ToArray(),
            })
            .ToArray();
    }

    private static DomikTypeCountGate[] LoadDomikTypeCountGates(ApplicationDbContext context)
    {
        return context.DomikTypeCountGates.Select(x => new DomikTypeCountGate
            {
                DomikTypeId = x.DomikTypeId,
                Ordinal = x.Ordinal,
                UnlockLevel = x.UnlockLevel,
            })
            .ToArray();
    }
}
