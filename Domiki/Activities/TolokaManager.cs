using Domiki.Web.Activities.Models;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference.Models;
using Domiki.Web.Reference;
using Domiki.Web.Village.Models;
using Domiki.Web.Village;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Activities;

public class TolokaManager
{
    public const int BridgeOrderBonusPercent = 40;

    /// <summary>
    /// Число последних завершённых толок, отдаваемых летописью экрана «Мир».
    /// </summary>
    /// <remarks>
    /// См. <see cref="GetArtifacts"/>.
    /// </remarks>
    public const int TolokaArtifactShowCount = 10;

    private const string BridgeLogicName = "bridge";

    private readonly UnitOfWork _uow;
    private readonly Data.ApplicationDbContext _context;
    private readonly ResourceManager _resourceManager;
    private readonly PlayerResourceManager _playerResourceManager;
    private readonly SeasonManager _seasonManager;
    private readonly PlayerEventManager _playerEventManager;
    private readonly GameStateBroker _broker;
    private readonly PushSender _pushSender;

    public TolokaManager(UnitOfWork uow, Data.ApplicationDbContext context, ResourceManager resourceManager, PlayerResourceManager playerResourceManager, SeasonManager seasonManager, PlayerEventManager playerEventManager, GameStateBroker broker, PushSender pushSender)
    {
        _uow = uow;
        _context = context;
        _resourceManager = resourceManager;
        _playerResourceManager = playerResourceManager;
        _seasonManager = seasonManager;
        _playerEventManager = playerEventManager;
        _broker = broker;
        _pushSender = pushSender;
    }

    public static int GetBuffSeconds(int level)
    {
        return (6 + 2 * level) * 3600;
    }

    public TolokaState? GetToloka(DateTime date, int playerId)
    {
        if (!HasBuilding(playerId, "gathering"))
        {
            return null;
        }

        var tolokaTypes = _resourceManager.GetTolokaTypes();
        var dbToloka = _context.Tolokas.Single(x => x.CompletedDate == null);
        var positions = _context.TolokaPositions.Where(x => x.TolokaId == dbToloka.Id).ToArray();
        var myContributions = _context.TolokaContributions
            .Where(x => x.TolokaId == dbToloka.Id && x.PlayerId == playerId)
            .ToDictionary(x => x.ResourceTypeId, x => x.Value);

        var voteCounts = _context.TolokaVotes
            .Where(x => x.TolokaId == dbToloka.Id)
            .GroupBy(x => x.CandidateTolokaTypeId)
            .Select(g => new { CandidateTolokaTypeId = g.Key, Votes = g.Count() })
            .ToDictionary(x => x.CandidateTolokaTypeId, x => x.Votes);

        var myVote = _context.TolokaVotes
            .Where(x => x.TolokaId == dbToloka.Id && x.PlayerId == playerId)
            .Select(x => (int?)x.CandidateTolokaTypeId)
            .FirstOrDefault();

        var activeBuffs = GetActiveBuffs(playerId, date);
        var level = Math.Max(1, GetGatheringLevel(playerId));
        var maxLevel = _resourceManager.GetDomikTypes().First(x => x.LogicName == "gathering").MaxLevel;

        return new()
        {
            Active = ToModel(dbToloka, positions, myContributions, tolokaTypes),
            ActiveBuffs = activeBuffs.Select(buff =>
                {
                    var tolokaType = tolokaTypes.First(x => x.Id == buff.TolokaTypeId);
                    return new TolokaActiveBuff
                    {
                        LogicName = tolokaType.LogicName,
                        Label = GetBuffLabel(tolokaType),
                        Percent = tolokaType.Effects.Length > 0 ? tolokaType.Effects[0].OutputPercent - 100 : BridgeOrderBonusPercent,
                        BuffUntil = buff.BuffUntil,
                    };
                })
                .ToArray(),
            BuffHours = GetBuffSeconds(level) / 3600,
            NextBuffHours = level < maxLevel ? GetBuffSeconds(level + 1) / 3600 : null,
            Candidates = tolokaTypes.Select(t => new TolokaVoteCandidate
                {
                    TolokaTypeId = t.Id,
                    Name = t.Name,
                    LogicName = t.LogicName,
                    Votes = voteCounts.GetValueOrDefault(t.Id),
                })
                .ToArray(),
            MyVoteTolokaTypeId = myVote,
        };
    }

    public void Contribute(int playerId, int resourceTypeId, int amount, DateTime date)
    {
        if (amount <= 0)
        {
            throw new BusinessException("Неверное количество");
        }

        _playerResourceManager.LockDbPlayerRow(playerId);

        if (!HasBuilding(playerId, "gathering"))
        {
            throw new BusinessException("Нужна Сходня");
        }

        var dbToloka = LockActiveToloka();
        var tolokaTypes = _resourceManager.GetTolokaTypes();
        var tolokaType = tolokaTypes.First(x => x.Id == dbToloka.TolokaTypeId);

        var position = _context.TolokaPositions.FirstOrDefault(x => x.TolokaId == dbToloka.Id && x.ResourceTypeId == resourceTypeId);
        if (position == null)
        {
            throw new BusinessException("Этот ресурс толоке не нужен");
        }

        var remaining = position.Goal - position.Collected;
        if (remaining <= 0)
        {
            throw new BusinessException("Позиция уже собрана");
        }

        var accepted = Math.Min(amount, remaining);

        _playerResourceManager.WriteOffResources(playerId, new[]
        {
            new Resource
            {
                Type = new()
                    { Id = resourceTypeId },
                Value = accepted,
            },
        });

        var contribution = _context.TolokaContributions.Local.FirstOrDefault(x => x.TolokaId == dbToloka.Id && x.PlayerId == playerId && x.ResourceTypeId == resourceTypeId)
                           ?? _context.TolokaContributions.FirstOrDefault(x => x.TolokaId == dbToloka.Id && x.PlayerId == playerId && x.ResourceTypeId == resourceTypeId);

        if (contribution == null)
        {
            contribution = new()
                { TolokaId = dbToloka.Id, PlayerId = playerId, ResourceTypeId = resourceTypeId };

            _context.TolokaContributions.Add(contribution);
        }

        contribution.Value += accepted;
        position.Collected += accepted;
        _seasonManager.IncrementCounter(playerId, SeasonMetric.Toloka, accepted, date);

        int[]? notifyRecipients = null;
        var completedTolokaName = tolokaType.Name;

        var allPositions = _context.TolokaPositions.Where(x => x.TolokaId == dbToloka.Id).ToArray();
        if (allPositions.All(p => p.Collected >= p.Goal))
        {
            dbToloka.CompletedDate = date;
            _context.SaveChanges();
            var contributors = _context.TolokaContributions.Where(x => x.TolokaId == dbToloka.Id).Select(x => x.PlayerId).Distinct().ToArray();
            foreach (var contributor in contributors)
            {
                _playerEventManager.Record(contributor, Data.Entities.PlayerEventType.TolokaCompleted, new { tolokaTypeId = dbToloka.TolokaTypeId });
            }

            notifyRecipients = contributors.Where(x => x != playerId).ToArray();

            var prevContributors = contributors.Length;
            var picked = TallyVotes(dbToloka.Id, tolokaTypes) ?? PickTolokaType(tolokaTypes);
            var next = new Data.Entities.Toloka
            {
                TolokaTypeId = picked.Id,
                StartDate = date,
                CompletedDate = null,
            };

            _context.Tolokas.Add(next);
            _context.SaveChanges();

            foreach (var pickedPosition in picked.Positions)
            {
                _context.TolokaPositions.Add(new()
                {
                    TolokaId = next.Id,
                    ResourceTypeId = pickedPosition.ResourceTypeId,
                    Goal = pickedPosition.Goal * Math.Max(1, prevContributors),
                    Collected = 0,
                });
            }
        }

        _context.SaveChanges();

        var afterEventAction = _uow.AfterEventAction;
        _uow.AfterEventAction = () =>
        {
            afterEventAction?.Invoke();
            _broker.Broadcast(GameStateScopes.Toloka);

            if (notifyRecipients != null)
            {
                foreach (var recipientId in notifyRecipients)
                {
                    _pushSender.Notify(recipientId, "Домики", $"Толока «{completedTolokaName}» завершена – бафф получен", "/domiki-page");
                }
            }
        };
    }

    /// <summary>
    /// Отдаёт голос игрока за тип следующей толоки в текущей активной инстанции.
    /// </summary>
    /// <remarks>
    /// Тот же порядок блокировок, что и <see cref="Contribute"/> (<see cref="PlayerResourceManager.LockDbPlayerRow"/>,
    /// затем <see cref="LockActiveToloka"/>), – голос не ляжет на завершающуюся толоку. Гейт постройкой «Сходня».
    /// Смена выбора – UPDATE строки голоса. Голос без вклада разрешён.
    /// </remarks>
    /// <param name="playerId">Игрок, отдающий голос.</param>
    /// <param name="candidateTolokaTypeId">Тип толоки, за который голосует игрок.</param>
    /// <param name="date">Момент действия в UTC.</param>
    public void Vote(int playerId, int candidateTolokaTypeId, DateTime date)
    {
        _playerResourceManager.LockDbPlayerRow(playerId);

        if (!HasBuilding(playerId, "gathering"))
        {
            throw new BusinessException("Нужна Сходня");
        }

        var tolokaTypes = _resourceManager.GetTolokaTypes();
        if (tolokaTypes.All(x => x.Id != candidateTolokaTypeId))
        {
            throw new BusinessException("Нет такого проекта толоки");
        }

        var dbToloka = LockActiveToloka();

        var vote = _context.TolokaVotes.FirstOrDefault(x => x.TolokaId == dbToloka.Id && x.PlayerId == playerId);
        if (vote == null)
        {
            vote = new()
                { TolokaId = dbToloka.Id, PlayerId = playerId };

            _context.TolokaVotes.Add(vote);
        }

        vote.CandidateTolokaTypeId = candidateTolokaTypeId;
        _context.SaveChanges();

        var afterEventAction = _uow.AfterEventAction;
        _uow.AfterEventAction = () =>
        {
            afterEventAction?.Invoke();
            _broker.Broadcast(GameStateScopes.Toloka);
        };
    }

    /// <summary>
    /// Летопись последних завершённых толок для экрана «Мир».
    /// </summary>
    /// <remarks>
    /// Не более <see cref="TolokaArtifactShowCount"/> штук, отсортированы по убыванию <see cref="Data.Entities.Toloka.CompletedDate"/>.
    /// Число участников каждой инстанции считается одним групповым запросом по <see cref="Data.Entities.TolokaContribution"/>,
    /// без N+1.
    /// </remarks>
    /// <returns>Массив завершённых толок, свежие первыми.</returns>
    public TolokaArtifact[] GetArtifacts()
    {
        var tolokaTypes = _resourceManager.GetTolokaTypes();
        var resourceTypes = _resourceManager.GetResourceTypes();

        var completed = _context.Tolokas
            .Where(x => x.CompletedDate != null)
            .OrderByDescending(x => x.CompletedDate)
            .Take(TolokaArtifactShowCount)
            .ToArray();

        var tolokaIds = completed.Select(x => x.Id).ToArray();
        var participants = _context.TolokaContributions
            .Where(x => tolokaIds.Contains(x.TolokaId))
            .Select(x => new { x.TolokaId, x.PlayerId })
            .ToArray()
            .GroupBy(x => x.TolokaId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.PlayerId).Distinct().Count());

        var positionsByToloka = _context.TolokaPositions
            .Where(x => tolokaIds.Contains(x.TolokaId))
            .ToArray()
            .GroupBy(x => x.TolokaId)
            .ToDictionary(g => g.Key, g => g.ToArray());

        return completed.Select(x =>
            {
                var tolokaType = tolokaTypes.First(t => t.Id == x.TolokaTypeId);
                var resourcesText = string.Join(" + ", positionsByToloka.GetValueOrDefault(x.Id, [])
                    .OrderBy(p => p.ResourceTypeId)
                    .Select(p => $"{p.Goal} {resourceTypes.First(r => r.Id == p.ResourceTypeId).Name}"));

                return new TolokaArtifact
                {
                    Name = tolokaType.Name,
                    ResourcesText = resourcesText,
                    SeasonNumber = _seasonManager.GetCurrentSeason(x.CompletedDate!.Value).Number,
                    Participants = participants.GetValueOrDefault(x.Id),
                    CompletedDate = x.CompletedDate!.Value,
                };
            })
            .ToArray();
    }

    public bool HasActiveBuff(int playerId, DateTime date)
    {
        return GetActiveBuffs(playerId, date).Length > 0;
    }

    public int GetTolokaOutputPercent(int playerId, int domikTypeId, DateTime date)
    {
        var tolokaTypes = _resourceManager.GetTolokaTypes();
        foreach (var buff in GetActiveBuffs(playerId, date))
        {
            var effect = tolokaTypes.First(x => x.Id == buff.TolokaTypeId).Effects.FirstOrDefault(x => x.DomikTypeId == domikTypeId);
            if (effect != null)
            {
                return effect.OutputPercent;
            }
        }

        return 100;
    }

    public int GetOrderRewardBonusPercent(int playerId, DateTime date)
    {
        var bridgeTypeId = _resourceManager.GetTolokaTypes().First(x => x.LogicName == BridgeLogicName).Id;
        return GetActiveBuffs(playerId, date).Any(x => x.TolokaTypeId == bridgeTypeId) ? BridgeOrderBonusPercent : 0;
    }

    private static string GetBuffLabel(TolokaType tolokaType)
    {
        return tolokaType.LogicName switch
        {
            "bridge" => "заказы",
            "granary" => "добыча дерева и глины",
            "kiln" => "переделы",
            "caravan" => "продажа",
            _ => tolokaType.Name,
        };
    }

    private TolokaType? TallyVotes(int tolokaId, TolokaType[] tolokaTypes)
    {
        // TODO: голосование игнорирует RotationWeight – при вводе внесписочных (RotationWeight = 0) или беспозиционных типов фильтровать кандидатов по rotation-eligible, иначе голоса могут посеять неигровую толоку
        var tally = _context.TolokaVotes
            .Where(x => x.TolokaId == tolokaId)
            .GroupBy(x => x.CandidateTolokaTypeId)
            .Select(g => new { CandidateTolokaTypeId = g.Key, Votes = g.Count() })
            .ToArray();

        if (tally.Length == 0)
        {
            return null;
        }

        var maxVotes = tally.Max(x => x.Votes);
        var leaders = tally.Where(x => x.Votes == maxVotes).Select(x => x.CandidateTolokaTypeId).ToArray();
        var winnerId = leaders[Random.Shared.Next(leaders.Length)];
        return tolokaTypes.FirstOrDefault(x => x.Id == winnerId);
    }

    private static TolokaType PickTolokaType(TolokaType[] tolokaTypes)
    {
        var totalWeight = tolokaTypes.Sum(x => x.RotationWeight);
        var roll = Random.Shared.Next(totalWeight);
        var cumulative = 0;
        foreach (var tolokaType in tolokaTypes)
        {
            cumulative += tolokaType.RotationWeight;
            if (roll < cumulative)
            {
                return tolokaType;
            }
        }

        return tolokaTypes[^1];
    }

    private static Toloka ToModel(Data.Entities.Toloka dbToloka, Data.Entities.TolokaPosition[] positions, Dictionary<int, int> myContributions, TolokaType[] tolokaTypes)
    {
        return new()
        {
            Id = dbToloka.Id,
            TolokaType = tolokaTypes.First(x => x.Id == dbToloka.TolokaTypeId),
            Positions = positions.Select(p => new TolokaPosition
                {
                    ResourceTypeId = p.ResourceTypeId,
                    Goal = p.Goal,
                    Collected = p.Collected,
                    MyContribution = myContributions.GetValueOrDefault(p.ResourceTypeId),
                })
                .ToArray(),
            StartDate = dbToloka.StartDate,
        };
    }

    private Data.Entities.Toloka LockActiveToloka()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var tolokaId = _context.Tolokas.Where(x => x.CompletedDate == null).Select(x => x.Id).Single();
            _context.LockRowForUpdate<Data.Entities.Toloka>(tolokaId);
            var toloka = _context.Tolokas.Single(x => x.Id == tolokaId);
            if (toloka.CompletedDate == null)
            {
                return toloka;
            }
        }

        throw new BusinessException("Толока обновляется, повторите");
    }

    private (int TolokaTypeId, DateTime BuffUntil)[] GetActiveBuffs(int playerId, DateTime date)
    {
        var buffSeconds = GetBuffSeconds(Math.Max(1, GetGatheringLevel(playerId)));
        return _context.TolokaContributions
            .Where(x => x.PlayerId == playerId && x.Toloka.CompletedDate != null && x.Toloka.CompletedDate > date.AddSeconds(-buffSeconds))
            .Select(x => new { x.Toloka.TolokaTypeId, Completed = x.Toloka.CompletedDate!.Value })
            .ToArray()
            .GroupBy(x => x.TolokaTypeId)
            .Select(g => (g.Key, g.Max(x => x.Completed).AddSeconds(buffSeconds)))
            .ToArray();
    }

    private bool HasBuilding(int playerId, string logicName)
    {
        var typeId = _resourceManager.GetDomikTypes().First(x => x.LogicName == logicName).Id;
        return _context.Domiks.Any(x => x.PlayerId == playerId && x.TypeId == typeId && x.Level >= 1);
    }

    private int GetGatheringLevel(int playerId)
    {
        var typeId = _resourceManager.GetDomikTypes().First(x => x.LogicName == "gathering").Id;
        return _context.Domiks.Where(x => x.PlayerId == playerId && x.TypeId == typeId)
                   .Select(x => (int?)x.Level)
                   .Max()
               ?? 0;
    }
}
