using Domiki.Web.Business.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Linq;
using System.Text.RegularExpressions;

namespace Domiki.Web.Business.Core
{
    public class DomikManager
    {
        private const int CrestIconCount = 8;
        private const int CrestColorCount = 8;
        public const int FatigueThresholdSeconds = 8 * 3600;
        public const int RestSeconds = 2 * 3600;
        public const int RestComfortMaxPercent = 50;
        public const int StartingCoins = 200;
        private const int InstaFinishSecondsPerGold = 3600;
        private const int InstaFinishMaxGold = 6;
        private const int GoldResourceTypeId = 5;

        private static readonly Regex SpaceRegex = new Regex(" +", RegexOptions.Compiled);
        private static readonly string[] VillageNameForbiddenWords =
        {
            "admin",
            "moderator",
            "keep",
            "domiki",
            "fuck",
            "shit",
            "админ",
            "модератор",
            "домики",
            "хуй",
            "пизд",
            "еба",
            "ёба",
            "бля",
            "сука",
        };

        private Data.ApplicationDbContext _context;
        private ICalculator _calculator;
        private Data.UnitOfWork _uow;
        private ResourceManager _resourceManager;
        private PlayerResourceManager _playerResourceManager;
        private WorkerManager _workerManager;
        private WeatherManager _weatherManager;
        private VillageLevelCalculator _villageLevelCalculator;
        private BlueprintManager _blueprintManager;
        private TolokaManager _tolokaManager;
        private PlayerEventManager _playerEventManager;

        public DomikManager(Data.UnitOfWork uow, Data.ApplicationDbContext context, ICalculator calculator, ResourceManager resourceManager, PlayerResourceManager playerResourceManager, WorkerManager workerManager, WeatherManager weatherManager, VillageLevelCalculator villageLevelCalculator, BlueprintManager blueprintManager, TolokaManager tolokaManager, PlayerEventManager playerEventManager)
        {
            _context = context;
            _calculator = calculator;
            _uow = uow;
            _resourceManager = resourceManager;
            _playerResourceManager = playerResourceManager;
            _workerManager = workerManager;
            _weatherManager = weatherManager;
            _villageLevelCalculator = villageLevelCalculator;
            _blueprintManager = blueprintManager;
            _tolokaManager = tolokaManager;
            _playerEventManager = playerEventManager;
        }

        public int GetPlayerId(string aspNetUserId)
        {
            var dbPlayer = _context.Players.FirstOrDefault(x => x.AspNetUserId == aspNetUserId);
            if (dbPlayer == null)
            {
                dbPlayer = new Data.Player();
                dbPlayer.AspNetUserId = aspNetUserId;
                dbPlayer.Name = "Держатель домиков";
                _context.Players.Add(dbPlayer);
                _context.Resources.Add(new Data.Resource { TypeId = 1, Player = dbPlayer, Value = StartingCoins });

                _context.SaveChanges();
            }
            return dbPlayer.Id;
        }

        public Village GetVillage(int playerId)
        {
            var dbPlayer = _context.Players.Single(x => x.Id == playerId);
            return new Village
            {
                VillageName = dbPlayer.VillageName,
                CrestIcon = dbPlayer.CrestIcon,
                CrestColor = dbPlayer.CrestColor,
            };
        }

        public void SetVillageIdentity(int playerId, string name, int crestIcon, int crestColor)
        {
            _playerResourceManager.LockDbPlayerRow(playerId);

            var villageName = NormalizeVillageName(name);
            ValidateVillageName(villageName);
            ValidateCrest(crestIcon, crestColor);

            if (_context.Players.Any(x => x.Id != playerId && x.VillageName == villageName))
            {
                throw new BusinessException("Имя деревни занято");
            }

            var dbPlayer = _context.Players.Single(x => x.Id == playerId);
            dbPlayer.VillageName = villageName;
            dbPlayer.CrestIcon = crestIcon;
            dbPlayer.CrestColor = crestColor;

            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateException ex) when (IsVillageNameUniqueViolation(ex))
            {
                throw new BusinessException("Имя деревни занято");
            }
        }

        public IEnumerable<(DomikType Type, int AvailableCount)> GetPurchaseAvailableDomiks(int playerId)
        {
            var available = new List<(DomikType Type, int AvailableCount)>();
            var domiks = GetDomiks(playerId);
            foreach (var domikType in _resourceManager.GetDomikTypes())
            {
                var current = domiks.Count(x => x.Type.Id == domikType.Id);
                var availableCount = Math.Max(0, domikType.MaxCount - current);
                if (availableCount > 0)
                {
                    available.Add((domikType, availableCount));
                }
            }
            return available;
        }

        public IEnumerable<Domik> GetDomiks(int playerId)
        {
            var manufactureGroups = _context.Manufactures.Where(x => x.DomikPlayerId == playerId)
                .ToArray().GroupBy(x => x.DomikId);
            var domikTypes = _resourceManager.GetDomikTypes();
            return _context.Domiks.Where(x => x.PlayerId == playerId).OrderBy(x => x.TypeId).ThenBy(x => x.Id).ToArray().Select(domik =>
                new Domik
                {
                    Id = domik.Id,
                    Type = domikTypes.First(y => y.Id == domik.TypeId),
                    Level = domik.Level,
                    FinishDate = domik.UpgradeSeconds == null ? null : (domik.UpgradeCalculateDate.Value.AddSeconds((int)domik.UpgradeSeconds)),
                    UpgradeSeconds = (int?)domik.UpgradeSeconds,
                    Manufactures = manufactureGroups.FirstOrDefault(m => m.Key == domik.Id)?.Select(x => new Manufacture
                    {
                        Id = x.Id,
                        FinishDate = x.FinishDate,
                        PlodderCount = x.PlodderCount,
                        ReceiptId = x.ReceiptId,
                        AutoRepeat = x.AutoRepeat,
                    }).ToArray(),
                }).ToList();
        }

        public void BuyDomik(int playerId, int typeId)
        {
            _playerResourceManager.LockDbPlayerRow(playerId);
            var available = GetPurchaseAvailableDomiks(playerId);
            if (available.Any(x => x.Type.Id == typeId))
            {
                var domikType = _resourceManager.GetDomikTypes().First(x => x.Id == typeId);
                if (!_villageLevelCalculator.CanBuyDomik(playerId, domikType))
                {
                    throw new BusinessException($"Откроется при обжитости {domikType.UnlockLevel}");
                }

                _blueprintManager.EnsureBlueprints(playerId);
                var blueprint = _resourceManager.GetBlueprints().FirstOrDefault(x => x.DomikTypeId == typeId);
                if (blueprint != null && !_blueprintManager.IsOwned(playerId, blueprint.Id))
                {
                    var neighbor = _resourceManager.GetNeighbors().First(x => x.Id == blueprint.NeighborId);
                    throw new BusinessException($"Нужен чертёж (репутация {neighbor.Name} {blueprint.ReputationThreshold})");
                }

                var domikLevel = domikType.Levels.First(x => x.Value == 1);
                _playerResourceManager.WriteOffResources(playerId, domikLevel.Resources);

                var currentId = _context.Domiks.Where(x => x.PlayerId == playerId).Max(x => (int?)x.Id) ?? 0;
                var nextId = currentId + 1;
                var date = DateTimeHelper.GetNowDate();
                _context.Domiks.Add(new Data.Domik { PlayerId = playerId, TypeId = typeId, Level = 0, Id = nextId, UpgradeSeconds = domikLevel.UpgradeSeconds, UpgradeCalculateDate = date });

                _uow.AfterEventAction = () =>
                {
                    _calculator.Insert(new CalculateInfo
                    {
                        PlayerId = playerId,
                        ObjectId = nextId,
                        Type = CalculateTypes.Domiks,
                        Date = date.AddSeconds(domikLevel.UpgradeSeconds),
                    });
                };
            }
            else
            {
                throw new BusinessException("Превышено максимальное количество");
            }
        }

        public void UpgradeDomik(int playerId, int id)
        {
            var date = DateTimeHelper.GetNowDate();

            // todo перечитать и попробовать повтоно выполнить. обработка оптимистика
            _playerResourceManager.LockDbPlayerRow(playerId);

            var dbDomik = _context.Domiks.First(x => x.PlayerId == playerId && x.Id == id);
            var domikType = _resourceManager.GetDomikTypes().First(x => x.Id == dbDomik.TypeId);
            if (dbDomik.Level >= domikType.MaxLevel)
            {
                throw new BusinessException("Максимальный уровень");
            }
            if (dbDomik.UpgradeSeconds != null)
            {
                throw new BusinessException("Домик уже улучшается");
            }

            var nextLevel = dbDomik.Level + 1;
            var domikLevel = domikType.Levels.First(x => x.Value == nextLevel);
            _playerResourceManager.WriteOffResources(playerId, domikLevel.Resources);
            dbDomik.UpgradeSeconds = domikLevel.UpgradeSeconds;
            dbDomik.UpgradeCalculateDate = date;

            _uow.AfterEventAction = () =>
            {
                _calculator.Insert(new CalculateInfo
                {
                    PlayerId = playerId,
                    ObjectId = dbDomik.Id,
                    Type = CalculateTypes.Domiks,
                    Date = date.AddSeconds(domikLevel.UpgradeSeconds),
                });
            };
        }

        public IEnumerable<Resource> GetResources(int playerId)
        {
            var resourceTypes = _resourceManager.GetResourceTypes().ToDictionary(x => x.Id, x => x);

            return _context.Resources.Where(x => x.PlayerId == playerId).ToArray().Select(x =>
                new Resource
                {
                    Type = resourceTypes[x.TypeId],
                    Value = x.Value,
                }).ToList();
        }

        public bool FinishDomik(DateTime date, CalculateInfo calcInfo)
        {
            _playerResourceManager.LockDbPlayerRow(calcInfo.PlayerId);

            var dbDomik = _context.Domiks.Single(x => x.Id == calcInfo.ObjectId && x.PlayerId == calcInfo.PlayerId);
            if (dbDomik.UpgradeSeconds != null)
            {
                var period = (date - (DateTime)dbDomik.UpgradeCalculateDate).TotalSeconds;
                var lostTime = dbDomik.UpgradeSeconds - period;
                if (lostTime <= 0)
                {
                    dbDomik.UpgradeCalculateDate = null;
                    dbDomik.UpgradeSeconds = null;
                    dbDomik.Level++;
                    _playerEventManager.Record(calcInfo.PlayerId, Data.PlayerEventType.DomikUpgraded, new { domikTypeId = dbDomik.TypeId, level = dbDomik.Level });

                    return true;
                }
                return false;
            }
            return true;
        }

        public void HurryDomik(int playerId, int domikId)
        {
            var date = DateTimeHelper.GetNowDate();
            _playerResourceManager.LockDbPlayerRow(playerId);

            var dbDomik = _context.Domiks.SingleOrDefault(x => x.Id == domikId && x.PlayerId == playerId);
            if (dbDomik == null)
            {
                throw new BusinessException("Домик не найден");
            }

            if (dbDomik.UpgradeSeconds == null || dbDomik.UpgradeCalculateDate == null)
            {
                throw new BusinessException("Домик не улучшается");
            }

            var finishDate = dbDomik.UpgradeCalculateDate.Value.AddSeconds((int)dbDomik.UpgradeSeconds);
            var cost = GetInstaFinishCost(finishDate, date);
            if (cost <= 0)
            {
                return;
            }

            WriteOffGold(playerId, cost);
            dbDomik.UpgradeCalculateDate = date.AddSeconds(-(double)dbDomik.UpgradeSeconds);

            FinishDomik(date, new CalculateInfo
            {
                PlayerId = playerId,
                ObjectId = domikId,
                Type = CalculateTypes.Domiks,
                Date = date,
            });

            _uow.AfterEventAction = () => _calculator.Remove(playerId, domikId, CalculateTypes.Domiks);
        }

        public void StartManufacture(int playerId, int domikId, int receiptId, bool useOptional = false, int[] workerIds = null, bool autoRepeat = false)
        {
            var date = DateTimeHelper.GetNowDate();

            _playerResourceManager.LockDbPlayerRow(playerId);

            var dbManufactures = _context.Manufactures.Where(x => x.DomikPlayerId == playerId);
            var currentManufactureCount = dbManufactures.Where(x => x.DomikId == domikId).Count();

            var workers = _workerManager.EnsureWorkers(playerId);
            var freeWorkers = workers.Where(x => WorkerManager.IsFree(x, date)).OrderBy(x => x.Id).ToArray();

            var domiks = _context.Domiks.Where(x=>x.PlayerId == playerId).ToArray();
            var domikTypes = _resourceManager.GetDomikTypes();

            var dbDomik = domiks.First(x => x.PlayerId == playerId && x.Id == domikId);
            if (dbDomik.Level == 0)
            {
                throw new BusinessException("Домик ещё строится");
            }
            var domikType = domikTypes.First(x => x.Id == dbDomik.TypeId);
            var domikLevel = domikType.Levels.First(x => x.Value == dbDomik.Level);
            var levelReceipt = domikLevel.Receipts.First(x => x.Id == receiptId);
            var receipt = _resourceManager.GetReceipts().First(x => x.Id == levelReceipt.Id);
            var needPlodderCount = receipt.PlodderCount;
            if (freeWorkers.Length < needPlodderCount)
            {
                throw new BusinessException("Недостаточно трудяг");
            }

            var freeIds = freeWorkers.Select(x => x.Id).ToArray();
            var skillByWorkerId = _context.WorkerSkills
                .Where(x => x.DomikTypeId == domikType.Id && freeIds.Contains(x.WorkerId))
                .ToDictionary(x => x.WorkerId, x => x.Uses);
            var traits = _resourceManager.GetTraits().ToDictionary(x => x.Id, x => x);

            if (domikLevel.MaxManufactureCount < currentManufactureCount + 1)
            {
                throw new BusinessException("Максимальное количество одновременных производств");
            }

            var writeOffResources = receipt.InputResources;
            var duration = receipt.DurationSeconds;
            if (useOptional && receipt.OptionalInputResources is not null && receipt.OptionalInputResources.Length > 0)
            {
                writeOffResources = writeOffResources.Concat(receipt.OptionalInputResources).ToArray();
                duration = receipt.DurationSeconds * (100 - receipt.SpeedupPercent) / 100;
            }

            Data.Worker[] selectedWorkers;
            if (workerIds == null || workerIds.Length == 0)
            {
                var autoWorkers = freeWorkers.AsEnumerable();
                if (_villageLevelCalculator.IsSmartAutoUnlocked(playerId))
                {
                    autoWorkers = autoWorkers
                        .OrderByDescending(w => -traits[w.TraitId].DurationPercent + WorkerSkillCalculator.GetBonusPercent(skillByWorkerId.GetValueOrDefault(w.Id)))
                        .ThenBy(w => w.Id);
                }
                else
                {
                    autoWorkers = autoWorkers.OrderBy(w => w.Id);
                }

                selectedWorkers = autoWorkers
                    .Take(needPlodderCount)
                    .ToArray();
            }
            else
            {
                if (workerIds.Length != needPlodderCount)
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

            var avgSpeedup = selectedWorkers.Average(x => -traits[x.TraitId].DurationPercent);
            duration = (int)Math.Ceiling(duration * (100 - avgSpeedup) / 100);
            var avgSkill = selectedWorkers.Average(x => WorkerSkillCalculator.GetBonusPercent(skillByWorkerId.GetValueOrDefault(x.Id)));
            duration = (int)Math.Ceiling(duration * (100 - avgSkill) / 100);
            duration = Math.Max(duration, (int)Math.Ceiling(receipt.DurationSeconds * 0.6));

            var weatherPercent = _weatherManager.GetOutputPercent(date, domikType.Id);
            var tolokaPercent = _tolokaManager.HasActiveBuff(playerId, date) ? 100 + TolokaManager.TolokaBuffPercent : 100;
            var outputPercent = (int)Math.Round(weatherPercent * tolokaPercent / 100.0);

            _playerResourceManager.WriteOffResources(playerId, writeOffResources);

            var manufacture = new Data.Manufacture
            {
                DomikId = domikId,
                DomikPlayerId = playerId,
                ReceiptId = receiptId,
                FinishDate = date.AddSeconds(duration),
                PlodderCount = needPlodderCount,
                OutputPercent = outputPercent,
                AutoRepeat = autoRepeat,
                UseOptional = useOptional && receipt.OptionalInputResources is not null && receipt.OptionalInputResources.Length > 0,
            };
            _context.Manufactures.Add(manufacture);
            _context.SaveChanges();
            foreach (var worker in selectedWorkers)
            {
                worker.ManufactureId = manufacture.Id;
            }

            _uow.AfterEventAction = () =>
            {
                _calculator.Insert(new CalculateInfo
                {
                    PlayerId = playerId,
                    ObjectId = manufacture.Id,
                    Type = CalculateTypes.Manufacture,
                    Date = manufacture.FinishDate,
                });
            };
        }

        public bool FinishManufacture(DateTime date, CalculateInfo calcInfo)
        {
            _playerResourceManager.LockDbPlayerRow(calcInfo.PlayerId);

            var dbManufacture = _context.Manufactures.Single(x => x.Id == calcInfo.ObjectId);
            if (date >= dbManufacture.FinishDate)
            {
                var recept = _resourceManager.GetReceipts().First(x => x.Id == dbManufacture.ReceiptId);
                var receiptId = dbManufacture.ReceiptId;
                var useOptional = dbManufacture.UseOptional;
                var autoRepeat = dbManufacture.AutoRepeat;
                var domikId = dbManufacture.DomikId;
                var playerId = calcInfo.PlayerId;
                var produced = new Dictionary<int, int>();
                foreach (var resource in recept.OutputResources)
                {
                    var granted = Math.Max(1, (int)Math.Round(resource.Value * dbManufacture.OutputPercent / 100.0));
                    _playerResourceManager.GrantResource(calcInfo.PlayerId, resource.Type.Id, granted);
                    if (produced.TryGetValue(resource.Type.Id, out var value))
                    {
                        produced[resource.Type.Id] = value + granted;
                    }
                    else
                    {
                        produced[resource.Type.Id] = granted;
                    }
                }
                var dbDomik = _context.Domiks.Single(x => x.PlayerId == calcInfo.PlayerId && x.Id == dbManufacture.DomikId);
                var comfort = DecorCalculator.GetComfort(
                    _context.PlayerDecors.Where(x => x.PlayerId == dbManufacture.DomikPlayerId).Select(x => new PlayerDecor { DecorTypeId = x.DecorTypeId, Count = x.Count }).ToArray(),
                    _resourceManager.GetDecorTypes());
                var restSeconds = RestSeconds * (100 - Math.Min(RestComfortMaxPercent, comfort)) / 100;
                var traits = _resourceManager.GetTraits().ToDictionary(x => x.Id, x => x);
                var freedWorkerIds = new List<int>();
                foreach (var worker in _context.Workers.Where(x => x.ManufactureId == dbManufacture.Id).ToArray())
                {
                    IncrementWorkerSkill(worker.Id, dbDomik.TypeId);
                    if (!traits[worker.TraitId].NoFatigue)
                    {
                        worker.WorkedSeconds += recept.DurationSeconds;
                        if (worker.WorkedSeconds >= FatigueThresholdSeconds)
                        {
                            worker.RestUntil = date.AddSeconds(restSeconds);
                            worker.WorkedSeconds = 0;
                        }
                    }

                    worker.ManufactureId = null;
                    freedWorkerIds.Add(worker.Id);
                }
                _context.Manufactures.Remove(dbManufacture);
                _playerEventManager.RecordManufactureFinished(calcInfo.PlayerId, dbDomik.TypeId, produced);
                if (autoRepeat)
                {
                    _context.SaveChanges();
                    try
                    {
                        StartManufacture(playerId, domikId, receiptId, useOptional, freedWorkerIds.ToArray(), autoRepeat: true);
                    }
                    catch (BusinessException)
                    {
                    }
                }
                return true;
            }
            return false;
        }

        public void HurryManufacture(int playerId, int manufactureId)
        {
            var date = DateTimeHelper.GetNowDate();
            _playerResourceManager.LockDbPlayerRow(playerId);

            var dbManufacture = _context.Manufactures.SingleOrDefault(x => x.Id == manufactureId && x.DomikPlayerId == playerId);
            if (dbManufacture == null)
            {
                throw new BusinessException("Производство не найдено");
            }

            var cost = GetInstaFinishCost(dbManufacture.FinishDate, date);
            if (cost <= 0)
            {
                return;
            }

            WriteOffGold(playerId, cost);
            dbManufacture.FinishDate = date;

            FinishManufacture(date, new CalculateInfo
            {
                PlayerId = playerId,
                ObjectId = manufactureId,
                Type = CalculateTypes.Manufacture,
                Date = date,
            });

            var afterFinishAction = _uow.AfterEventAction;
            _uow.AfterEventAction = () =>
            {
                afterFinishAction?.Invoke();
                _calculator.Remove(playerId, manufactureId, CalculateTypes.Manufacture);
            };
        }

        public void SetManufactureAutoRepeat(int playerId, int manufactureId, bool autoRepeat)
        {
            _playerResourceManager.LockDbPlayerRow(playerId);

            var dbManufacture = _context.Manufactures.SingleOrDefault(x => x.Id == manufactureId && x.DomikPlayerId == playerId);
            if (dbManufacture == null)
            {
                throw new BusinessException("Производство не найдено");
            }

            dbManufacture.AutoRepeat = autoRepeat;
        }

        private void IncrementWorkerSkill(int workerId, int domikTypeId)
        {
            var skill = _context.WorkerSkills.SingleOrDefault(x => x.WorkerId == workerId && x.DomikTypeId == domikTypeId);
            if (skill == null)
            {
                _context.WorkerSkills.Add(new Data.WorkerSkill
                {
                    WorkerId = workerId,
                    DomikTypeId = domikTypeId,
                    Uses = 1,
                });
                return;
            }

            skill.Uses++;
        }

        private int GetInstaFinishCost(DateTime finishDate, DateTime date)
        {
            var remaining = (finishDate - date).TotalSeconds;
            if (remaining <= 0)
            {
                return 0;
            }

            var cost = (int)Math.Ceiling(remaining / InstaFinishSecondsPerGold);
            if (cost > InstaFinishMaxGold)
            {
                throw new BusinessException("До конца ещё далеко");
            }

            return cost;
        }

        private void WriteOffGold(int playerId, int cost)
        {
            var goldType = _resourceManager.GetResourceTypes().First(x => x.Id == GoldResourceTypeId);
            _playerResourceManager.WriteOffResources(playerId, new[]
            {
                new Resource
                {
                    Type = goldType,
                    Value = cost,
                },
            });
        }

        private string NormalizeVillageName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            return SpaceRegex.Replace(name.Trim(), " ");
        }

        private void ValidateVillageName(string name)
        {
            if (name.Length < 3 || name.Length > 24)
            {
                throw new BusinessException("Имя деревни должно быть 3–24 символа");
            }

            if (name.Any(x => !IsAllowedVillageNameChar(x)))
            {
                throw new BusinessException("В имени деревни можно использовать буквы, цифры, пробел и дефис");
            }

            var lowerName = name.ToLowerInvariant();
            if (VillageNameForbiddenWords.Any(x => lowerName.Contains(x)))
            {
                throw new BusinessException("Имя деревни содержит запрещённое слово");
            }
        }

        private bool IsAllowedVillageNameChar(char value)
        {
            return value == ' '
                || value == '-'
                || char.IsDigit(value)
                || value >= 'A' && value <= 'Z'
                || value >= 'a' && value <= 'z'
                || value >= 'А' && value <= 'я'
                || value == 'Ё'
                || value == 'ё';
        }

        private void ValidateCrest(int crestIcon, int crestColor)
        {
            if (crestIcon < 0 || crestIcon >= CrestIconCount)
            {
                throw new BusinessException("Неизвестная пиктограмма герба");
            }

            if (crestColor < 0 || crestColor >= CrestColorCount)
            {
                throw new BusinessException("Неизвестный цвет герба");
            }
        }

        private bool IsVillageNameUniqueViolation(DbUpdateException ex)
        {
            return ex.InnerException is PostgresException postgresException
                && postgresException.SqlState == PostgresErrorCodes.UniqueViolation
                && postgresException.ConstraintName == "IX_Players_VillageName";
        }
    }
}
