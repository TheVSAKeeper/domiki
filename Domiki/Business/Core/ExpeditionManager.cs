using Domiki.Web.Business.Models;

namespace Domiki.Web.Business.Core
{
    public class ExpeditionManager
    {
        public const int ExpeditionPityThreshold = 8;
        public const int ExpeditionRestSeconds = 2 * 3600;
        private const int GoldResourceTypeId = 5;

        private Data.ApplicationDbContext _context;
        private Data.UnitOfWork _uow;
        private ICalculator _calculator;
        private ResourceManager _resourceManager;
        private PlayerResourceManager _playerResourceManager;
        private WorkerManager _workerManager;
        private SeasonManager _seasonManager;
        private PlayerEventManager _playerEventManager;
        private DecorManager _decorManager;

        public ExpeditionManager(
            Data.UnitOfWork uow,
            Data.ApplicationDbContext context,
            ICalculator calculator,
            ResourceManager resourceManager,
            PlayerResourceManager playerResourceManager,
            WorkerManager workerManager,
            SeasonManager seasonManager,
            PlayerEventManager playerEventManager,
            DecorManager decorManager)
        {
            _uow = uow;
            _context = context;
            _calculator = calculator;
            _resourceManager = resourceManager;
            _playerResourceManager = playerResourceManager;
            _workerManager = workerManager;
            _seasonManager = seasonManager;
            _playerEventManager = playerEventManager;
            _decorManager = decorManager;
        }

        public ExpeditionState GetExpeditions(int playerId)
        {
            if (!HasBuilding(playerId, "scout_hut"))
            {
                return null;
            }

            _playerResourceManager.LockDbPlayerRow(playerId);

            var types = _resourceManager.GetExpeditionTypes();
            var dbPlayer = _context.Players.Single(x => x.Id == playerId);
            var scoutHutLevel = GetScoutHutLevel(playerId);
            var active = _context.Expeditions.Where(x => x.PlayerId == playerId).OrderBy(x => x.FinishDate).ToArray()
                .Select(x => new Expedition
                {
                    Id = x.Id,
                    ExpeditionType = types.First(y => y.Id == x.ExpeditionTypeId),
                    StartDate = x.StartDate,
                    FinishDate = x.FinishDate,
                }).ToArray();

            return new ExpeditionState
            {
                Active = active,
                Types = types,
                ExpeditionsSincePity = dbPlayer.ExpeditionsSincePity,
                PityThreshold = ExpeditionPityThreshold,
                MaxActive = scoutHutLevel,
            };
        }

        public void StartExpedition(int playerId, int expeditionTypeId, int[] workerIds = null)
        {
            var date = DateTimeHelper.GetNowDate();
            _playerResourceManager.LockDbPlayerRow(playerId);

            if (!HasBuilding(playerId, "scout_hut"))
            {
                throw new BusinessException("Нужна Сторожка");
            }

            var type = _resourceManager.GetExpeditionTypes().FirstOrDefault(x => x.Id == expeditionTypeId);
            if (type == null)
            {
                throw new BusinessException("Экспедиция не найдена");
            }

            if (_context.Expeditions.Count(x => x.PlayerId == playerId) >= GetScoutHutLevel(playerId))
            {
                throw new BusinessException("Все отряды в походе – улучшите Сторожку");
            }

            var workers = _workerManager.EnsureWorkers(playerId);
            var freeWorkers = workers.Where(x => WorkerManager.IsFree(x, date)).OrderBy(x => x.Id).ToArray();
            if (freeWorkers.Length < type.WorkerCount)
            {
                throw new BusinessException("Недостаточно трудяг");
            }

            Data.Worker[] selectedWorkers;
            if (workerIds == null || workerIds.Length == 0)
            {
                selectedWorkers = freeWorkers.Take(type.WorkerCount).ToArray();
            }
            else
            {
                if (workerIds.Length != type.WorkerCount)
                {
                    throw new BusinessException("Неверное число трудяг");
                }
                if (workerIds.Distinct().Count() != workerIds.Length)
                {
                    throw new BusinessException("Дублирующиеся трудяги");
                }

                var freeById = freeWorkers.ToDictionary(x => x.Id);
                selectedWorkers = workerIds.Select(id =>
                    freeById.TryGetValue(id, out var w) ? w : throw new BusinessException("Трудяга недоступен")).ToArray();
            }

            var resourceTypes = _resourceManager.GetResourceTypes().ToDictionary(x => x.Id, x => x);
            var writeOffResources = new[]
            {
                new Resource
                {
                    Type = resourceTypes[GoldResourceTypeId],
                    Value = type.GoldCost,
                },
            }.Concat(type.Equipment.Select(x => new Resource
            {
                Type = resourceTypes[x.ResourceTypeId],
                Value = x.Value,
            })).ToArray();
            _playerResourceManager.WriteOffResources(playerId, writeOffResources);

            var expedition = new Data.Expedition
            {
                PlayerId = playerId,
                ExpeditionTypeId = type.Id,
                StartDate = date,
                FinishDate = date.AddSeconds(type.DurationSeconds),
            };
            _context.Expeditions.Add(expedition);
            _context.SaveChanges();

            foreach (var worker in selectedWorkers)
            {
                worker.ExpeditionId = expedition.Id;
            }

            _uow.AfterEventAction = () =>
            {
                _calculator.Insert(new CalculateInfo
                {
                    PlayerId = playerId,
                    ObjectId = expedition.Id,
                    Type = CalculateTypes.Expedition,
                    Date = expedition.FinishDate,
                });
            };
        }

        public bool FinishExpedition(DateTime date, CalculateInfo calcInfo)
        {
            _playerResourceManager.LockDbPlayerRow(calcInfo.PlayerId);

            var dbExpedition = _context.Expeditions.FirstOrDefault(x => x.Id == calcInfo.ObjectId && x.PlayerId == calcInfo.PlayerId);
            if (dbExpedition == null)
            {
                return true;
            }

            if (date < dbExpedition.FinishDate)
            {
                return false;
            }

            var type = _resourceManager.GetExpeditionTypes().First(x => x.Id == dbExpedition.ExpeditionTypeId);
            var dbPlayer = _context.Players.Single(x => x.Id == calcInfo.PlayerId);
            var assignedWorkers = _context.Workers.Where(x => x.ExpeditionId == dbExpedition.Id).ToArray();
            var traits = _resourceManager.GetTraits().ToDictionary(x => x.Id, x => x);
            var groupLuck = assignedWorkers.Length == 0 ? 0 : assignedWorkers.Max(x => traits[x.TraitId].LuckWeightPercent);

            var forced = dbPlayer.ExpeditionsSincePity >= ExpeditionPityThreshold;
            var gotRare = false;
            var loot = new List<object>();
            for (var roll = 0; roll < type.RollCount; roll++)
            {
                var pool = forced && !gotRare ? type.Loot.Where(x => x.IsRare).ToArray() : type.Loot;
                if (pool.Length == 0)
                {
                    pool = type.Loot;
                }

                var entry = PickLoot(pool, groupLuck);
                if (entry.IsRare)
                {
                    gotRare = true;
                }

                loot.Add(ApplyLootEntry(calcInfo.PlayerId, type, entry, assignedWorkers, traits, groupLuck));
            }

            dbPlayer.ExpeditionsSincePity = gotRare ? 0 : dbPlayer.ExpeditionsSincePity + 1;
            _playerEventManager.Record(calcInfo.PlayerId, Data.PlayerEventType.ExpeditionReturned, new { expeditionTypeId = dbExpedition.ExpeditionTypeId, loot });
            _seasonManager.IncrementCounter(calcInfo.PlayerId, SeasonMetric.Expeditions, 1, date);

            foreach (var worker in assignedWorkers)
            {
                if (!traits[worker.TraitId].NoFatigue)
                {
                    worker.RestUntil = dbExpedition.FinishDate.AddSeconds(ExpeditionRestSeconds);
                }

                worker.ExpeditionId = null;
            }

            _context.Expeditions.Remove(dbExpedition);
            return true;
        }

        public object ApplyLootEntry(int playerId, ExpeditionType type, ExpeditionLoot entry, Data.Worker[] squadWorkers, Dictionary<int, Trait> traits, int groupLuck)
        {
            switch (entry.Kind)
            {
                case Data.ExpeditionLootKind.Decor:
                    _decorManager.GrantDecor(playerId, entry.DecorTypeId.Value, 1);
                    return new { kind = (int)Data.ExpeditionLootKind.Decor, decorTypeId = entry.DecorTypeId, isRare = entry.IsRare };

                case Data.ExpeditionLootKind.TraitUpgrade:
                    return ApplyTraitUpgrade(playerId, type, squadWorkers, traits, groupLuck, entry.IsRare);

                default:
                    var value = Random.Shared.Next(entry.MinValue, entry.MaxValue + 1);
                    _playerResourceManager.GrantResource(playerId, entry.ResourceTypeId.Value, value);
                    return new { kind = (int)Data.ExpeditionLootKind.Resource, resourceTypeId = entry.ResourceTypeId, value, isRare = entry.IsRare };
            }
        }

        private object ApplyTraitUpgrade(int playerId, ExpeditionType type, Data.Worker[] squadWorkers, Dictionary<int, Trait> traits, int groupLuck, bool isRare)
        {
            var ordinaryTrait = traits.Values.First(x => x.LogicName == "ordinary");
            var candidates = squadWorkers.Where(x => x.TraitId == ordinaryTrait.Id).ToArray();
            if (candidates.Length == 0)
            {
                var fallbackPool = type.Loot.Where(x => x.IsRare && x.Kind != Data.ExpeditionLootKind.TraitUpgrade).ToArray();
                var fallbackEntry = PickLoot(fallbackPool, groupLuck);
                return ApplyLootEntry(playerId, type, fallbackEntry, squadWorkers, traits, groupLuck);
            }

            var worker = candidates[Random.Shared.Next(candidates.Length)];
            var nonOrdinaryTraits = traits.Values.Where(x => x.LogicName != "ordinary").ToArray();
            var newTrait = nonOrdinaryTraits[Random.Shared.Next(nonOrdinaryTraits.Length)];
            worker.TraitId = newTrait.Id;
            return new { kind = (int)Data.ExpeditionLootKind.TraitUpgrade, workerName = worker.Name, newTrait = newTrait.Name, isRare };
        }

        private static ExpeditionLoot PickLoot(ExpeditionLoot[] loot, int luckPercent)
        {
            var totalWeight = loot.Sum(x => ScaleWeight(x.IsRare, x.Weight, luckPercent));
            if (totalWeight <= 0)
            {
                throw new InvalidOperationException("Нет доступной добычи экспедиции");
            }

            var roll = Random.Shared.Next(totalWeight);
            var cumulative = 0;
            foreach (var entry in loot)
            {
                cumulative += ScaleWeight(entry.IsRare, entry.Weight, luckPercent);
                if (roll < cumulative)
                {
                    return entry;
                }
            }

            return loot[^1];
        }

        public static int ScaleWeight(bool isRare, int weight, int luckPercent)
        {
            return isRare ? weight * (100 + luckPercent) / 100 : weight;
        }

        private bool HasBuilding(int playerId, string logicName)
        {
            var typeId = _resourceManager.GetDomikTypes().First(x => x.LogicName == logicName).Id;
            return _context.Domiks.Any(x => x.PlayerId == playerId && x.TypeId == typeId && x.Level >= 1);
        }

        private int GetScoutHutLevel(int playerId)
        {
            var typeId = _resourceManager.GetDomikTypes().First(x => x.LogicName == "scout_hut").Id;
            return _context.Domiks.Where(x => x.PlayerId == playerId && x.TypeId == typeId)
                .Select(x => (int?)x.Level).Max() ?? 0;
        }
    }
}
