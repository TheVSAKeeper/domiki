using Domiki.Web.Data;
using Domiki.Web.Data.Entities;
using Domiki.Web.Economy.Models;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;
using Microsoft.EntityFrameworkCore;
using Resource = Domiki.Web.Reference.Models.Resource;

namespace Domiki.Web.Economy;

/// <summary>
/// Правила Избы старосты: её уровень, запись счётной книги и чтение суточного итога.
/// </summary>
/// <remarks>
/// Изба продаёт глубину, а не право видеть: бесплатная доска «Хозяйство» отвечает бинарно и про сейчас,
/// книга – количественно и про сутки. Книга фактическая: она складывает то, что действительно прошло через
/// <see cref="PlayerResourceManager"/>, и потому считает только с часа постройки избы.
/// </remarks>
public class ElderHouseManager
{
    /// <summary>
    /// Логическое имя типа постройки в справочнике.
    /// </summary>
    public const string DomikLogicName = "elder_house";

    /// <summary>
    /// Уровень Избы, с которого староста ведёт счётную книгу.
    /// </summary>
    public const int CountingBookMinLevel = 1;

    /// <summary>
    /// Уровень Избы, с которого наряду можно назначить меру.
    /// </summary>
    public const int MeasureMinLevel = 2;

    /// <summary>
    /// Уровень Избы, с которого припас можно заповедать от нарядов.
    /// </summary>
    public const int ReserveMinLevel = 3;

    /// <summary>
    /// Сколько суток книги хранится, прежде чем строки сметаются по дате.
    /// </summary>
    public const int LedgerKeepDays = 8;

    /// <summary>
    /// Сколько суток должно пройти, прежде чем расход начинают экстраполировать.
    /// </summary>
    /// <remarks>
    /// Раньше этого срока средний расход за сутки считать не на чем: одна списанная партия дала бы прогноз «кончится через час».
    /// </remarks>
    private const int ForecastMinElapsedSeconds = 3600;

    /// <summary>
    /// Горизонт прогноза: запас, которого хватает дольше, за нехватку не считается.
    /// </summary>
    public const int ShortageHorizonHours = 24;

    private const int CoinResourceTypeId = 1;
    private const int GoldResourceTypeId = 5;

    private readonly ApplicationDbContext _context;
    private readonly ResourceManager _resourceManager;
    private readonly Dictionary<int, int> _levels = new();
    private readonly HashSet<int> _sweptPlayerIds = [];

    /// <summary>
    /// Создаёт менеджер правил Избы старосты.
    /// </summary>
    /// <param name="context">Контекст данных текущего запроса.</param>
    /// <param name="resourceManager">Кэш справочников игры.</param>
    public ElderHouseManager(ApplicationDbContext context, ResourceManager resourceManager)
    {
        _context = context;
        _resourceManager = resourceManager;
    }

    /// <summary>
    /// Возвращает наивысший уровень Избы старосты у игрока.
    /// </summary>
    /// <remarks>
    /// Ответ кэшируется на время запроса: хук книги спрашивает уровень на каждом движении ресурса.
    /// </remarks>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <returns>Наивысший уровень Избы либо <c>0</c>, если её нет.</returns>
    public int GetLevel(int playerId)
    {
        if (_levels.TryGetValue(playerId, out var cached))
        {
            return cached;
        }

        var typeId = _resourceManager.GetDomikTypes().FirstOrDefault(x => x.LogicName == DomikLogicName)?.Id;
        var level = typeId == null
            ? 0
            : _context.Domiks
                  .Where(x => x.PlayerId == playerId && x.TypeId == typeId.Value)
                  .Select(x => (int?)x.Level)
                  .Max()
              ?? 0;

        _levels[playerId] = level;
        return level;
    }

    /// <summary>
    /// Записывает в книгу приход ресурса.
    /// </summary>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <param name="resourceTypeId">Тип пришедшего ресурса.</param>
    /// <param name="value">Число единиц, положительное.</param>
    public void RecordGain(int playerId, int resourceTypeId, int value)
    {
        if (value <= 0 || GetLevel(playerId) < CountingBookMinLevel)
        {
            return;
        }

        GetOrCreateFlow(playerId, resourceTypeId).Gained += value;
    }

    /// <summary>
    /// Записывает в книгу расход ресурса.
    /// </summary>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <param name="resourceTypeId">Тип ушедшего ресурса.</param>
    /// <param name="value">Число единиц, положительное.</param>
    public void RecordSpend(int playerId, int resourceTypeId, int value)
    {
        if (value <= 0 || GetLevel(playerId) < CountingBookMinLevel)
        {
            return;
        }

        GetOrCreateFlow(playerId, resourceTypeId).Spent += value;
    }

    /// <summary>
    /// Записывает отработанное смены время в суточный счётчик занятости двора.
    /// </summary>
    /// <remarks>
    /// Смену, начавшуюся вчера, счётчик делит между сутками по фактическому перекрытию,
    /// иначе доля простоя за сутки уходила бы за 100 %.
    /// </remarks>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <param name="finishDate">Момент завершения смены в UTC.</param>
    /// <param name="durationSeconds">Длительность смены в секундах.</param>
    /// <param name="slots">Сколько производственных мест смена занимала.</param>
    public void RecordShift(int playerId, DateTime finishDate, int durationSeconds, int slots)
    {
        if (durationSeconds <= 0 || slots <= 0 || GetLevel(playerId) < CountingBookMinLevel)
        {
            return;
        }

        var start = finishDate.AddSeconds(-durationSeconds);
        for (var day = start.Date; day <= finishDate.Date; day = day.AddDays(1))
        {
            var from = day > start ? day : start;
            var to = day.AddDays(1) < finishDate ? day.AddDays(1) : finishDate;
            var seconds = (long)(to - from).TotalSeconds;
            if (seconds > 0)
            {
                GetOrCreateLaborDay(playerId, day).WorkedSeconds += seconds * slots;
            }
        }
    }

    /// <summary>
    /// Собирает счётную книгу за текущие сутки.
    /// </summary>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <returns>Книга либо <see langword="null"/>, если Избы старосты у игрока нет.</returns>
    public Ledger? GetLedger(int playerId)
    {
        var level = GetLevel(playerId);
        if (level < CountingBookMinLevel)
        {
            return null;
        }

        var now = DateTimeHelper.GetNowDate();
        var day = now.Date;
        var elapsedSeconds = (long)(now - day).TotalSeconds;
        var flows = _context.PlayerResourceFlows
            .Where(x => x.PlayerId == playerId && x.Date == day)
            .OrderBy(x => x.ResourceTypeId)
            .ToArray();

        return new()
        {
            Level = level,
            HasEntries = flows.Length > 0,
            Flows = flows.Select(x => new LedgerFlow
                {
                    ResourceTypeId = x.ResourceTypeId,
                    Gained = x.Gained,
                    Spent = x.Spent,
                })
                .ToArray(),
            Shortage = GetShortage(playerId, flows, elapsedSeconds),
            IdlePercent = GetIdlePercent(playerId, day, elapsedSeconds),
        };
    }

    private LedgerShortage? GetShortage(int playerId, PlayerResourceFlow[] flows, long elapsedSeconds)
    {
        if (elapsedSeconds < ForecastMinElapsedSeconds)
        {
            return null;
        }

        var stocks = _context.Resources.Where(x => x.PlayerId == playerId).ToArray()
            .Union(_context.Resources.Local.Where(x => x.PlayerId == playerId))
            .ToDictionary(x => x.TypeId, x => x.Value);
        LedgerShortage? worst = null;
        foreach (var flow in flows)
        {
            // Монеты и золото тратятся покупками разом, а не расходуются на нарядах: линейный прогноз по ним всегда врёт.
            if (flow.Spent <= 0 || flow.ResourceTypeId is CoinResourceTypeId or GoldResourceTypeId)
            {
                continue;
            }

            var hours = (int)(stocks.GetValueOrDefault(flow.ResourceTypeId) * (elapsedSeconds / 3600.0) / flow.Spent);
            if (hours > ShortageHorizonHours)
            {
                continue;
            }

            if (worst == null || hours < worst.Hours)
            {
                worst = new() { ResourceTypeId = flow.ResourceTypeId, Hours = hours };
            }
        }

        return worst;
    }

    private int? GetIdlePercent(int playerId, DateTime day, long elapsedSeconds)
    {
        var domikTypes = _resourceManager.GetDomikTypes().ToDictionary(x => x.Id, x => x);
        var slots = _context.Domiks
            .Where(x => x.PlayerId == playerId)
            .Select(x => new { x.TypeId, x.Level })
            .ToArray()
            .Sum(x =>
            {
                var upgradeLevel = domikTypes.GetValueOrDefault(x.TypeId)?.Levels.FirstOrDefault(l => l.Value == x.Level);
                return upgradeLevel is { Receipts.Length: > 0 } ? upgradeLevel.MaxManufactureCount : 0;
            });

        var capacity = slots * elapsedSeconds;
        if (capacity <= 0)
        {
            return null;
        }

        var worked = _context.PlayerLaborDays
                         .Where(x => x.PlayerId == playerId && x.Date == day)
                         .Select(x => (long?)x.WorkedSeconds)
                         .FirstOrDefault()
                     ?? 0;

        return (int)Math.Clamp(100 - worked * 100 / capacity, 0, 100);
    }

    /// <summary>
    /// Возвращает заповеданные припасы игрока.
    /// </summary>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <returns>Заповедь по каждому ресурсу, у которого она больше нуля.</returns>
    public ResourceReserve[] GetReserves(int playerId)
    {
        return _context.PlayerResourceReserves
            .Where(x => x.PlayerId == playerId && x.Reserve > 0)
            .OrderBy(x => x.ResourceTypeId)
            .Select(x => new ResourceReserve { ResourceTypeId = x.ResourceTypeId, Reserve = x.Reserve })
            .ToArray();
    }

    /// <summary>
    /// Возвращает припас, заповеданный от нарядов задумкой игрока (§4.4 дизайна).
    /// </summary>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <returns>По каждому ресурсу цены следующего уровня – <c>min(запас, цена)</c>, то есть уже накопленная часть; пустой массив, если задумки нет.</returns>
    /// <remarks>
    /// Заповедь копилки растёт вместе с накоплением и никогда не превышает цену уровня, поэтому припас сверх нужного наряды берут свободно.
    /// Доступна с уровня <see cref="ReserveMinLevel"/> Избы старосты – ступень продаёт именно то, что копилка защищает себя сама.
    /// </remarks>
    public ResourceReserve[] GetIntentReserves(int playerId)
    {
        if (GetLevel(playerId) < ReserveMinLevel)
        {
            return [];
        }

        var intentDomikId = _context.Players.Where(x => x.Id == playerId).Select(x => x.IntentDomikId).FirstOrDefault();
        if (intentDomikId == null)
        {
            return [];
        }

        var dbDomik = _context.Domiks.Local.FirstOrDefault(x => x.PlayerId == playerId && x.Id == intentDomikId.Value)
                      ?? _context.Domiks.FirstOrDefault(x => x.PlayerId == playerId && x.Id == intentDomikId.Value);

        var domikType = dbDomik == null ? null : _resourceManager.GetDomikTypes().FirstOrDefault(x => x.Id == dbDomik.TypeId);
        var domikLevel = domikType?.Levels.FirstOrDefault(x => x.Value == dbDomik!.Level + 1);
        if (domikLevel == null)
        {
            return [];
        }

        return domikLevel.Resources
            .Where(x => x.Value > 0)
            .GroupBy(x => x.Type.Id)
            .Select(x => new ResourceReserve { ResourceTypeId = x.Key, Reserve = Math.Min(GetStock(playerId, x.Key), x.Sum(resource => resource.Value)) })
            .Where(x => x.Reserve > 0)
            .OrderBy(x => x.ResourceTypeId)
            .ToArray();
    }

    /// <summary>
    /// Отвечает, чем именно удержан припас: копилкой задумки или меткой, поставленной своей рукой.
    /// </summary>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <param name="resourceTypeId">Тип ресурса, на котором встал наряд.</param>
    /// <returns><see langword="true"/> – заповедь задумки строже ручной, и наряду стоит объяснить остановку копилкой.</returns>
    public bool IsIntentHold(int playerId, int resourceTypeId)
    {
        var intent = GetIntentReserves(playerId).FirstOrDefault(x => x.ResourceTypeId == resourceTypeId)?.Reserve ?? 0;
        if (intent == 0)
        {
            return false;
        }

        var manual = _context.PlayerResourceReserves
            .Where(x => x.PlayerId == playerId && x.ResourceTypeId == resourceTypeId)
            .Select(x => (int?)x.Reserve)
            .FirstOrDefault() ?? 0;

        return intent > manual;
    }

    /// <summary>
    /// Заповедует припас от нарядов либо снимает заповедь нулём.
    /// </summary>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <param name="resourceTypeId">Тип ресурса.</param>
    /// <param name="reserve">Сколько единиц отложить; <c>0</c> снимает заповедь.</param>
    /// <exception cref="BusinessException">Изба не доросла до заповеди, ресурс неизвестен или число отрицательное.</exception>
    public void SaveReserve(int playerId, int resourceTypeId, int reserve)
    {
        if (GetLevel(playerId) < ReserveMinLevel)
        {
            throw new BusinessException("Заповедать припас некому: в Избе старосты нет заповедного ларя");
        }

        if (_resourceManager.GetResourceTypes().All(x => x.Id != resourceTypeId))
        {
            throw new BusinessException("Такого припаса не бывает");
        }

        if (reserve < 0)
        {
            throw new BusinessException("Заповедь не может быть отрицательной");
        }

        var row = _context.PlayerResourceReserves.Local.FirstOrDefault(x => x.PlayerId == playerId && x.ResourceTypeId == resourceTypeId)
                  ?? _context.PlayerResourceReserves.FirstOrDefault(x => x.PlayerId == playerId && x.ResourceTypeId == resourceTypeId);

        if (row == null)
        {
            row = new() { PlayerId = playerId, ResourceTypeId = resourceTypeId };
            _context.PlayerResourceReserves.Add(row);
        }

        row.Reserve = reserve;
    }

    /// <summary>
    /// Проверяет, набралось ли меры наряда.
    /// </summary>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <param name="resourceTypeId">Тип ресурса меры.</param>
    /// <param name="value">Сколько единиц нужно набрать.</param>
    /// <returns><see langword="true"/> – на складе уже не меньше <paramref name="value"/>, наряд отстоял меру.</returns>
    public bool IsMeasureMet(int playerId, int resourceTypeId, int value)
    {
        return GetStock(playerId, resourceTypeId) >= value;
    }

    /// <summary>
    /// Ищет припас, который наряд не вправе тронуть из-за заповеди.
    /// </summary>
    /// <remarks>
    /// Заповедь смотрит на остаток после списания: наряд встаёт, если хоть один вход увёл бы запас ниже отложенного.
    /// </remarks>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <param name="inputs">Входы рецепта вместе с опциональным, если он применяется.</param>
    /// <returns>Тип первого упёршегося ресурса либо <see langword="null"/>, если заповедь наряду не мешает.</returns>
    public int? GetHeldResourceTypeId(int playerId, Resource[] inputs)
    {
        if (GetLevel(playerId) < ReserveMinLevel)
        {
            return null;
        }

        var reserves = _context.PlayerResourceReserves
            .Where(x => x.PlayerId == playerId && x.Reserve > 0)
            .ToDictionary(x => x.ResourceTypeId, x => x.Reserve);

        foreach (var intent in GetIntentReserves(playerId))
        {
            reserves[intent.ResourceTypeId] = Math.Max(reserves.GetValueOrDefault(intent.ResourceTypeId), intent.Reserve);
        }

        if (reserves.Count == 0)
        {
            return null;
        }

        foreach (var group in inputs.Where(x => x.Value > 0).GroupBy(x => x.Type.Id))
        {
            if (reserves.TryGetValue(group.Key, out var reserve)
                && GetStock(playerId, group.Key) - group.Sum(x => x.Value) < reserve)
            {
                return group.Key;
            }
        }

        return null;
    }

    private int GetStock(int playerId, int resourceTypeId)
    {
        return _context.Resources.Local.FirstOrDefault(x => x.PlayerId == playerId && x.TypeId == resourceTypeId)?.Value
               ?? _context.Resources.Where(x => x.PlayerId == playerId && x.TypeId == resourceTypeId).Select(x => (int?)x.Value).FirstOrDefault()
               ?? 0;
    }

    private PlayerResourceFlow GetOrCreateFlow(int playerId, int resourceTypeId)
    {
        var day = DateTimeHelper.GetNowDate().Date;
        var flow = _context.PlayerResourceFlows.Local.FirstOrDefault(x => x.PlayerId == playerId && x.Date == day && x.ResourceTypeId == resourceTypeId)
                   ?? _context.PlayerResourceFlows.FirstOrDefault(x => x.PlayerId == playerId && x.Date == day && x.ResourceTypeId == resourceTypeId);

        if (flow == null)
        {
            flow = new() { PlayerId = playerId, Date = day, ResourceTypeId = resourceTypeId };
            _context.PlayerResourceFlows.Add(flow);
            SweepOldRecords(playerId, day);
        }

        return flow;
    }

    private PlayerLaborDay GetOrCreateLaborDay(int playerId, DateTime day)
    {
        var laborDay = _context.PlayerLaborDays.Local.FirstOrDefault(x => x.PlayerId == playerId && x.Date == day)
                       ?? _context.PlayerLaborDays.FirstOrDefault(x => x.PlayerId == playerId && x.Date == day);

        if (laborDay == null)
        {
            laborDay = new() { PlayerId = playerId, Date = day };
            _context.PlayerLaborDays.Add(laborDay);
            SweepOldRecords(playerId, day);
        }

        return laborDay;
    }

    /// <summary>
    /// Сметает записи книги старше <see cref="LedgerKeepDays"/> суток.
    /// </summary>
    /// <remarks>
    /// Зовётся при заведении первой строки на новые сутки и не чаще раза за запрос: сметание по дате, а не по игроку,
    /// иначе dev-база с десятками тысяч тестовых игроков перебиралась бы целиком.
    /// </remarks>
    private void SweepOldRecords(int playerId, DateTime day)
    {
        if (!_sweptPlayerIds.Add(playerId))
        {
            return;
        }

        var oldest = day.AddDays(-LedgerKeepDays);
        _context.PlayerResourceFlows.Where(x => x.PlayerId == playerId && x.Date < oldest).ExecuteDelete();
        _context.PlayerLaborDays.Where(x => x.PlayerId == playerId && x.Date < oldest).ExecuteDelete();
    }
}
