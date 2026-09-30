using Domiki.Web.Core;
using Domiki.Web.Data;
using Domiki.Web.Data.Entities;
using Domiki.Web.Infrastructure;
using Domiki.Web.Village.Models;

namespace Domiki.Web.Village;

/// <summary>
/// Лесенка перков узелков памяти: справочник ступеней, покупка и величина эффектов.
/// </summary>
/// <remarks>
/// Отделён от <see cref="RelocationManager"/> нарочно: эффекты читают <see cref="Workers.WorkerManager.GetCapacity"/>
/// и <see cref="DomikManager.StartManufacture"/>, а сам переезд считает обжитость, которая в свою очередь считает койки, –
/// общий менеджер замкнул бы граф зависимостей в кольцо. Лесенка конечна и трогает только время, койки и разовый
/// капитал (GAMEDESIGN.md §3 Слой 4).
/// </remarks>
public class PerkManager
{
    /// <summary>
    /// Монеты в казну новой деревни за каждую ступень «Подъёмных».
    /// </summary>
    public const int LiftingsCoinsPerStep = 500;

    /// <summary>
    /// Сокращение времени всех производств за каждую ступень «Долгой привычки», проценты.
    /// </summary>
    public const int LongHabitDurationPercentPerStep = 5;

    /// <summary>
    /// Лишние койки за каждую ступень «Запасной койки».
    /// </summary>
    public const int SpareBunkBedsPerStep = 1;

    /// <summary>
    /// Справочник перков лесенки: цена каждой ступени в узелках памяти.
    /// </summary>
    /// <remarks>
    /// Свой топор, Перина и Дальняя родня отложены во вторую версию – они добавляются строкой сюда и членом
    /// <see cref="RelocationPerkType"/>, схема БД от этого не меняется.
    /// </remarks>
    public static readonly Perk[] Perks =
    [
        new()
        {
            Type = RelocationPerkType.Liftings,
            Name = "Подъёмные",
            Description = $"Артель уходит не с пустым кошелём: в казне новой деревни сразу лишние {LiftingsCoinsPerStep} монет.",
            Costs = [2, 4, 6],
            Level = 0,
        },
        new()
        {
            Type = RelocationPerkType.LongHabit,
            Name = "Долгая привычка",
            Description = "Руки помнят порядок: любая смена во дворе идёт на 5 % скорее.",
            Costs = [3, 9],
            Level = 0,
        },
        new()
        {
            Type = RelocationPerkType.SpareBunk,
            Name = "Запасная койка",
            Description = $"В обозе едет лишняя койка: пока артельные избы не достроены, во дворе работает на одного трудягу больше. Отстроенная деревня и без неё упирается в свои {Workers.WorkerManager.MaxCapacity}.",
            Costs = [6, 12],
            Level = 0,
        },
    ];

    private readonly ApplicationDbContext _context;
    private readonly PlayerResourceManager _playerResourceManager;

    public PerkManager(ApplicationDbContext context, PlayerResourceManager playerResourceManager)
    {
        _context = context;
        _playerResourceManager = playerResourceManager;
    }

    /// <summary>
    /// Возвращает лесенку перков игрока – справочник вместе с купленными ступенями.
    /// </summary>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <returns>Перки справочника в порядке лесенки.</returns>
    public Perk[] GetPerks(int playerId)
    {
        var levels = GetLevels(playerId);
        return Perks.Select(perk => new Perk
            {
                Type = perk.Type,
                Name = perk.Name,
                Description = perk.Description,
                Costs = perk.Costs,
                Level = levels.GetValueOrDefault(perk.Type),
            })
            .ToArray();
    }

    /// <summary>
    /// Возвращает число купленных ступеней одного перка.
    /// </summary>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <param name="type">Перк лесенки.</param>
    /// <returns>Число купленных ступеней, <c>0</c> – перк не куплен.</returns>
    public int GetLevel(int playerId, RelocationPerkType type)
    {
        return _context.PlayerPerks
            .Where(x => x.PlayerId == playerId && x.PerkType == type)
            .Select(x => (int?)x.Level)
            .FirstOrDefault() ?? 0;
    }

    /// <summary>
    /// Возвращает монеты, с которыми начинает жить новая деревня игрока.
    /// </summary>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <returns>Стартовые монеты вместе с подъёмными.</returns>
    public int GetStartingCoins(int playerId)
    {
        return DomikManager.StartingCoins + GetLevel(playerId, RelocationPerkType.Liftings) * LiftingsCoinsPerStep;
    }

    /// <summary>
    /// Возвращает множитель длительности производств от «Долгой привычки».
    /// </summary>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <returns>Проценты от базовой длительности: <c>100</c> – перк не куплен.</returns>
    /// <remarks>
    /// Живёт в общем ярусе личных постоянных модификаторов рядом с чертой, навыком и укладом и подчиняется тому же
    /// клампу 0.6× (GAMEDESIGN.md §4.4).
    /// </remarks>
    public int GetDurationPercent(int playerId)
    {
        return 100 - GetLevel(playerId, RelocationPerkType.LongHabit) * LongHabitDurationPercentPerStep;
    }

    /// <summary>
    /// Возвращает прибавку коек от «Запасной койки».
    /// </summary>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <returns>Число лишних коек.</returns>
    public int GetBedBonus(int playerId)
    {
        return GetLevel(playerId, RelocationPerkType.SpareBunk) * SpareBunkBedsPerStep;
    }

    /// <summary>
    /// Покупает следующую ступень перка за узелки памяти.
    /// </summary>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <param name="type">Перк лесенки.</param>
    public void BuyPerk(int playerId, RelocationPerkType type)
    {
        _playerResourceManager.LockDbPlayerRow(playerId);

        var perk = Perks.FirstOrDefault(x => x.Type == type) ?? throw new BusinessException("Такого перка нет");
        var dbPerk = _context.PlayerPerks.FirstOrDefault(x => x.PlayerId == playerId && x.PerkType == type);
        var level = dbPerk?.Level ?? 0;
        if (level >= perk.Costs.Length)
        {
            throw new BusinessException($"«{perk.Name}» – все ступени уже взяты");
        }

        var cost = perk.Costs[level];
        var dbPlayer = _context.Players.Single(x => x.Id == playerId);
        if (dbPlayer.MemoryKnots < cost)
        {
            throw new BusinessException($"Не хватает узелков памяти: нужно {cost}, есть {dbPlayer.MemoryKnots}");
        }

        dbPlayer.MemoryKnots -= cost;
        if (dbPerk == null)
        {
            _context.PlayerPerks.Add(new()
                { PlayerId = playerId, PerkType = type, Level = 1 });
        }
        else
        {
            dbPerk.Level++;
        }
    }

    private Dictionary<RelocationPerkType, int> GetLevels(int playerId)
    {
        return _context.PlayerPerks
            .Where(x => x.PlayerId == playerId)
            .ToDictionary(x => x.PerkType, x => x.Level);
    }
}
