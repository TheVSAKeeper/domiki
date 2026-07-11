using Domiki.Web.Business.Models;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Business.Core
{
    public class ResourceManager
    {
        public const int BaseMarketValue = 10;
        private Data.ApplicationDbContext _context;

        // todo избавится от статиков
        private static ModificatorType[] _modificatorTypes;
        private static ResourceType[] _resourceTypes;
        private static Receipt[] _receipts;
        private static DomikType[] _domikTypes;
        private static Neighbor[] _neighbors;
        private static Trait[] _traits;
        private static WeatherType[] _weatherTypes;
        private static Blueprint[] _blueprints;
        private static ExpeditionType[] _expeditionTypes;
        private static DecorType[] _decorTypes;
        private static TolokaType[] _tolokaTypes;

        public ResourceManager(Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public static int GetMarketValue(int resourceTypeId) => resourceTypeId switch
        {
            5 => 100,
            6 or 7 => 35,
            8 => 55,
            9 => 95,
            _ => BaseMarketValue,
        };

        public ModificatorType[] GetModificatorTypes()
        {
            if (_modificatorTypes == null)
            {
                _modificatorTypes = _context.ModificatorTypes.Select(x => new ModificatorType
                {
                    Id = x.Id,
                    LogicName = x.LogicName,
                    Name = x.Name,
                }).ToArray();
            }

            return _modificatorTypes;
        }

        public Trait[] GetTraits()
        {
            if (_traits == null)
            {
                _traits = _context.Traits.Select(x => new Trait
                {
                    Id = x.Id,
                    LogicName = x.LogicName,
                    Name = x.Name,
                    DurationPercent = x.DurationPercent,
                    NoFatigue = x.NoFatigue,
                    LuckWeightPercent = x.LuckWeightPercent,
                }).ToArray();
            }

            return _traits;
        }

        public ResourceType[] GetResourceTypes()
        {
            if (_resourceTypes == null)
            {
                _resourceTypes = _context.ResourceTypes
                    .Select(x => new ResourceType { Id = x.Id, LogicName = x.LogicName, Name = x.Name }).ToArray();
            }

            return _resourceTypes;
        }

        public Receipt[] GetReceipts()
        {
            if (_receipts == null)
            {
                _receipts = _context.Receipts.Include(x => x.Resources).Select(x => new Receipt
                {
                    Id = x.Id,
                    LogicName = x.LogicName,
                    Name = x.Name,
                    PlodderCount = x.PlodderCount,
                    DurationSeconds = x.DurationSeconds,
                    SpeedupPercent = x.SpeedupPercent,
                    InputResources = x.Resources.Where(x => x.IsInput && !x.IsOptional)
                        .Select(x => new Resource
                        {
                            Type = new ResourceType { Id = x.ResourceTypeId },
                            Value = x.Value
                        }).ToArray(),
                    OptionalInputResources = x.Resources.Where(x => x.IsInput && x.IsOptional)
                        .Select(x => new Resource
                        {
                            Type = new ResourceType { Id = x.ResourceTypeId },
                            Value = x.Value
                        }).ToArray(),
                    OutputResources = x.Resources.Where(x => !x.IsInput)
                        .Select(x => new Resource
                        {
                            Type = new ResourceType { Id = x.ResourceTypeId },
                            Value = x.Value
                        }).ToArray(),
                }).ToArray();
            }
            return _receipts;
        }

        public Neighbor[] GetNeighbors()
        {
            if (_neighbors == null)
            {
                _neighbors = _context.Neighbors.Select(x => new Neighbor
                {
                    Id = x.Id,
                    Name = x.Name,
                    LogicName = x.LogicName,
                    PrimaryResourceTypeId = x.PrimaryResourceTypeId,
                    UnlockLevel = x.UnlockLevel,
                }).ToArray();
            }

            return _neighbors;
        }

        public Blueprint[] GetBlueprints()
        {
            if (_blueprints == null)
            {
                _blueprints = _context.Blueprints.Select(x => new Blueprint
                {
                    Id = x.Id,
                    Name = x.Name,
                    LogicName = x.LogicName,
                    DomikTypeId = x.DomikTypeId,
                    NeighborId = x.NeighborId,
                    ReputationThreshold = x.ReputationThreshold,
                }).ToArray();
            }

            return _blueprints;
        }

        public WeatherType[] GetWeatherTypes()
        {
            if (_weatherTypes == null)
            {
                var effects = _context.WeatherTypeEffects.ToArray();
                _weatherTypes = _context.WeatherTypes.Select(x => new WeatherType
                {
                    Id = x.Id,
                    Name = x.Name,
                    LogicName = x.LogicName,
                    RotationWeight = x.RotationWeight,
                }).ToArray();

                foreach (var weatherType in _weatherTypes)
                {
                    weatherType.Effects = effects
                        .Where(x => x.WeatherTypeId == weatherType.Id)
                        .Select(x => new WeatherTypeEffect { DomikTypeId = x.DomikTypeId, OutputPercent = x.OutputPercent })
                        .ToArray();
                }
            }

            return _weatherTypes;
        }

        public ExpeditionType[] GetExpeditionTypes()
        {
            if (_expeditionTypes == null)
            {
                var loot = _context.ExpeditionLoot.ToArray();
                var equipment = _context.ExpeditionEquipment.ToArray();
                _expeditionTypes = _context.ExpeditionTypes.Select(x => new ExpeditionType
                {
                    Id = x.Id,
                    Name = x.Name,
                    LogicName = x.LogicName,
                    DurationSeconds = x.DurationSeconds,
                    WorkerCount = x.WorkerCount,
                    GoldCost = x.GoldCost,
                    RollCount = x.RollCount,
                }).ToArray();

                foreach (var expeditionType in _expeditionTypes)
                {
                    expeditionType.Loot = loot
                        .Where(x => x.ExpeditionTypeId == expeditionType.Id)
                        .Select(x => new ExpeditionLoot
                        {
                            Kind = x.Kind,
                            ResourceTypeId = x.ResourceTypeId,
                            DecorTypeId = x.DecorTypeId,
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
                        })
                        .ToArray();
                }
            }

            return _expeditionTypes;
        }

        public TolokaType[] GetTolokaTypes()
        {
            if (_tolokaTypes == null)
            {
                _tolokaTypes = _context.TolokaTypes.Select(x => new TolokaType
                {
                    Id = x.Id,
                    Name = x.Name,
                    LogicName = x.LogicName,
                    ResourceTypeId = x.ResourceTypeId,
                    Goal = x.Goal,
                    RotationWeight = x.RotationWeight,
                }).ToArray();
            }

            return _tolokaTypes;
        }

        public DecorType[] GetDecorTypes()
        {
            if (_decorTypes == null)
            {
                var costs = _context.DecorCosts.ToArray();
                _decorTypes = _context.DecorTypes.Select(x => new DecorType
                {
                    Id = x.Id,
                    Name = x.Name,
                    LogicName = x.LogicName,
                    ComfortPoints = x.ComfortPoints,
                    IsPurchasable = x.IsPurchasable,
                }).ToArray();

                foreach (var decorType in _decorTypes)
                {
                    decorType.Cost = costs
                        .Where(x => x.DecorTypeId == decorType.Id)
                        .Select(x => new Resource
                        {
                            Type = new ResourceType { Id = x.ResourceTypeId },
                            Value = x.Value,
                        })
                        .ToArray();
                }
            }

            return _decorTypes;
        }

        public DomikType[] GetDomikTypes()
        {
            if (_domikTypes == null)
            {
                var modificators = _context.DomikTypeLevelModificators.ToArray();
                var recepts = _context.DomikTypeLevelRecepts.ToArray();
                var resources = _context.DomikTypeLevelResources.ToArray();
                _domikTypes = _context.DomikTypes.Include(x => x.Levels).ToArray().Select(domikType => new DomikType
                {
                    Id = domikType.Id,
                    LogicName = domikType.LogicName,
                    Name = domikType.Name,
                    MaxCount = domikType.MaxCount,
                    UnlockLevel = domikType.UnlockLevel,
                    Levels = domikType.Levels.Select(level => new UpgradeLevel
                    {
                        Value = level.Value,
                        UpgradeSeconds = level.UpgradeSeconds,
                        MaxManufactureCount = level.MaxManufactureCount,
                        Modificators = modificators
                            .Where(m => m.DomikTypeLevelDomikTypeId == domikType.Id
                                && m.DomikTypeLevelValue == level.Value)
                            .Select(x => new Modificator { Type = new ModificatorType { Id = x.ModificatorTypeId }, Value = x.Value }).ToArray(),
                        Receipts = recepts
                            .Where(m => m.DomikTypeLevelDomikTypeId == domikType.Id
                                && m.DomikTypeLevelValue == level.Value)
                            .Select(x => new Receipt { Id = x.ReceiptId }).ToArray(),
                        Resources = resources
                            .Where(m => m.DomikTypeLevelDomikTypeId == domikType.Id
                                && m.DomikTypeLevelValue == level.Value)
                            .Select(x => new Resource { Type = new ResourceType { Id = x.ResourceTypeId }, Value = x.Value }).ToArray(),
                    }).ToArray(),
                }).ToArray();
            }
            return _domikTypes;
        }
    }
}
