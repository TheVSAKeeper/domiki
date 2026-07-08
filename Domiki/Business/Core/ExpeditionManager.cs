using Domiki.Web.Business.Models;

namespace Domiki.Web.Business.Core
{
    public class ExpeditionManager
    {
        public const int ExpeditionPityThreshold = 8;
        private const int GoldResourceTypeId = 5;

        private Data.ApplicationDbContext _context;
        private Data.UnitOfWork _uow;
        private ICalculator _calculator;
        private ResourceManager _resourceManager;
        private PlayerResourceManager _playerResourceManager;
        private WorkerManager _workerManager;
        private SeasonManager _seasonManager;

        public ExpeditionManager(
            Data.UnitOfWork uow,
            Data.ApplicationDbContext context,
            ICalculator calculator,
            ResourceManager resourceManager,
            PlayerResourceManager playerResourceManager,
            WorkerManager workerManager,
            SeasonManager seasonManager)
        {
            _uow = uow;
            _context = context;
            _calculator = calculator;
            _resourceManager = resourceManager;
            _playerResourceManager = playerResourceManager;
            _workerManager = workerManager;
            _seasonManager = seasonManager;
        }

        public ExpeditionState GetExpeditions(int playerId)
        {
            _playerResourceManager.LockDbPlayerRow(playerId);

            var types = _resourceManager.GetExpeditionTypes();
            var dbPlayer = _context.Players.Single(x => x.Id == playerId);
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
            };
        }

        public void StartExpedition(int playerId, int expeditionTypeId)
        {
            var date = DateTimeHelper.GetNowDate();
            _playerResourceManager.LockDbPlayerRow(playerId);

            var type = _resourceManager.GetExpeditionTypes().FirstOrDefault(x => x.Id == expeditionTypeId);
            if (type == null)
            {
                throw new BusinessException("Экспедиция не найдена");
            }

            var workers = _workerManager.EnsureWorkers(playerId);
            var freeWorkers = workers.Where(x => WorkerManager.IsFree(x, date)).OrderBy(x => x.Id).ToArray();
            if (freeWorkers.Length < type.WorkerCount)
            {
                throw new BusinessException("Недостаточно трудяг");
            }

            var goldType = _resourceManager.GetResourceTypes().First(x => x.Id == GoldResourceTypeId);
            _playerResourceManager.WriteOffResources(playerId, new[]
            {
                new Resource
                {
                    Type = goldType,
                    Value = type.GoldCost,
                },
            });

            var expedition = new Data.Expedition
            {
                PlayerId = playerId,
                ExpeditionTypeId = type.Id,
                StartDate = date,
                FinishDate = date.AddSeconds(type.DurationSeconds),
            };
            _context.Expeditions.Add(expedition);
            _context.SaveChanges();

            foreach (var worker in freeWorkers.Take(type.WorkerCount))
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

            var forced = dbPlayer.ExpeditionsSincePity >= ExpeditionPityThreshold;
            var gotRare = false;
            for (var roll = 0; roll < type.RollCount; roll++)
            {
                var pool = forced && !gotRare ? type.Loot.Where(x => x.IsRare).ToArray() : type.Loot;
                if (pool.Length == 0)
                {
                    pool = type.Loot;
                }

                var entry = PickLoot(pool);
                if (entry.IsRare)
                {
                    gotRare = true;
                }

                var value = Random.Shared.Next(entry.MinValue, entry.MaxValue + 1);
                _playerResourceManager.GrantResource(calcInfo.PlayerId, entry.ResourceTypeId, value);
            }

            dbPlayer.ExpeditionsSincePity = gotRare ? 0 : dbPlayer.ExpeditionsSincePity + 1;
            _seasonManager.IncrementCounter(calcInfo.PlayerId, SeasonMetric.Expeditions, 1, date);

            foreach (var worker in _context.Workers.Where(x => x.ExpeditionId == dbExpedition.Id).ToArray())
            {
                worker.ExpeditionId = null;
            }

            _context.Expeditions.Remove(dbExpedition);
            return true;
        }

        private static ExpeditionLoot PickLoot(ExpeditionLoot[] loot)
        {
            var totalWeight = loot.Sum(x => x.Weight);
            var roll = Random.Shared.Next(totalWeight);
            var cumulative = 0;
            foreach (var entry in loot)
            {
                cumulative += entry.Weight;
                if (roll < cumulative)
                {
                    return entry;
                }
            }

            return loot[^1];
        }
    }
}
