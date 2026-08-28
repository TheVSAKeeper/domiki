using Domiki.Web.Data;
using Domiki.Web.Data.Entities;
using Domiki.Web.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Domiki.Web.Infrastructure;

public class PlayerEventManager
{
    /// <summary>
    /// Ширина окна слияния однотипных событий: события внутри одного часа схлопываются в одну запись.
    /// </summary>
    public static readonly TimeSpan MergeBucket = TimeSpan.FromHours(1);

    /// <summary>
    /// Окно хранения журнала: события старше удаляются фоновой чисткой.
    /// </summary>
    /// <remarks>
    /// Замер на проде 26.08.2026: у активного игрока около 6 записей в час, то есть порядка 4300 строк за окно – объём
    /// ограничением не является (см. <c>docs/journal-scale.md</c>).
    /// </remarks>
    public static readonly TimeSpan Retention = TimeSpan.FromDays(30);

    /// <summary>
    /// Потолок одной страницы журнала.
    /// </summary>
    public const int MaxPageSize = 100;

    /// <summary>
    /// Потолок одной витрины «Пока вас не было».
    /// </summary>
    public const int RecapBatch = 500;

    private readonly ApplicationDbContext _context;

    public PlayerEventManager(ApplicationDbContext context)
    {
        _context = context;
    }

    public void Record(int playerId, PlayerEventType type, object payload)
    {
        _context.PlayerEvents.Add(new()
        {
            PlayerId = playerId,
            Type = type,
            Date = DateTimeHelper.GetNowDate(),
            Data = JsonSerializer.Serialize(payload),
        });
    }

    /// <summary>
    /// Записывает срыв автоповтора наряда, сливая повторные срывы одной постройки по одной причине в текущем часовом окне.
    /// </summary>
    /// <remarks>
    /// Без слияния постройка с коротким рецептом и нехваткой сырья пишет одну и ту же строку каждый цикл – это второй по объёму
    /// источник шума в журнале после самих завершений производства.
    /// </remarks>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <param name="domikId">Идентификатор постройки, чей наряд сорвался.</param>
    /// <param name="domikTypeId">Тип постройки.</param>
    /// <param name="receiptId">Рецепт, который не удалось перезапустить.</param>
    /// <param name="reason">Текст причины срыва, видимый игроку.</param>
    public void RecordManufactureRepeatFailed(int playerId, int domikId, int domikTypeId, int receiptId, string reason)
    {
        var now = DateTimeHelper.GetNowDate();
        var events = GetMergeCandidates(playerId, PlayerEventType.ManufactureRepeatFailed, GetBucketStart(now));
        foreach (var playerEvent in events)
        {
            var payload = ReadPayload<ManufactureRepeatFailedPayload>(playerEvent.Data);
            if (payload == null || payload.DomikId != domikId || payload.Reason != reason)
            {
                continue;
            }

            payload.Count = Math.Max(1, payload.Count) + 1;
            playerEvent.Data = JsonSerializer.Serialize(payload);
            return;
        }

        _context.PlayerEvents.Add(new()
        {
            PlayerId = playerId,
            Type = PlayerEventType.ManufactureRepeatFailed,
            Date = now,
            Data = JsonSerializer.Serialize(new ManufactureRepeatFailedPayload
            {
                DomikId = domikId,
                DomikTypeId = domikTypeId,
                ReceiptId = receiptId,
                Reason = reason,
                Count = 1,
            }),
        });
    }

    /// <summary>
    /// Кандидаты на слияние: однотипные события игрока внутри текущего часового окна, новейшие первыми.
    /// </summary>
    /// <remarks>
    /// Учитывает и ещё не сохранённые записи из <see cref="Microsoft.EntityFrameworkCore.ChangeTracking.LocalView{TEntity}"/> –
    /// иначе два завершения в одной единице работы не сольются.
    /// Окно считается от <see cref="PlayerEvent.Date"/>, а не от факта доставки: витрина сдвигает курсор
    /// <see cref="Data.Entities.Player.LastDeliveredEventId"/>, поэтому уже отданное событие можно дополнять, не отдавая повторно.
    /// </remarks>
    private List<Data.Entities.PlayerEvent> GetMergeCandidates(int playerId, PlayerEventType type, DateTime bucketStart)
    {
        var bucketEnd = bucketStart + MergeBucket;
        return _context.PlayerEvents.Where(x => x.PlayerId == playerId && x.Type == type && x.Date >= bucketStart && x.Date < bucketEnd).ToArray()
            .Union(_context.PlayerEvents.Local.Where(x => x.PlayerId == playerId && x.Type == type && x.Date >= bucketStart && x.Date < bucketEnd))
            .OrderByDescending(x => _context.Entry(x).State == EntityState.Added)
            .ThenByDescending(x => x.Id)
            .ToList();
    }

    /// <summary>
    /// Разбирает полезную нагрузку события, подставляя пустой объект вместо повреждённой.
    /// </summary>
    /// <remarks>
    /// Одна битая строка не должна ронять всю страницу журнала: клиент нарисует такую запись нейтральной строкой
    /// (честный откат вместо исчезновения). Хранение растянуто на <see cref="Retention"/>, и чем дольше живёт запись,
    /// тем выше цена отказа на ней.
    /// </remarks>
    private static JsonElement ReadPayload(string data)
    {
        try
        {
            return JsonSerializer.Deserialize<JsonElement>(data);
        }
        catch (JsonException)
        {
            return JsonSerializer.Deserialize<JsonElement>("{}");
        }
    }

    /// <summary>
    /// Разбирает нагрузку кандидата на слияние; повреждённая или чужая по форме запись кандидатом не считается.
    /// </summary>
    /// <remarks>
    /// Слияние идёт по пути записи события: отказ на одной старой строке не должен ронять сам игровой ход.
    /// </remarks>
    private static T? ReadPayload<T>(string data)
        where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(data);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Начало часового окна слияния, в которое попадает <paramref name="now"/>.
    /// </summary>
    private static DateTime GetBucketStart(DateTime now)
    {
        return now.AddTicks(-(now.Ticks % MergeBucket.Ticks));
    }

    public void RecordManufactureFinished(int playerId, int domikTypeId, Dictionary<int, int> producedByResourceTypeId)
    {
        var now = DateTimeHelper.GetNowDate();
        var events = GetMergeCandidates(playerId, PlayerEventType.ManufactureFinished, GetBucketStart(now));
        foreach (var playerEvent in events)
        {
            var payload = ReadPayload<ManufactureFinishedPayload>(playerEvent.Data);
            if (payload?.DomikTypeId != domikTypeId)
            {
                continue;
            }

            foreach (var produced in producedByResourceTypeId)
            {
                var resource = payload.Resources.FirstOrDefault(x => x.ResourceTypeId == produced.Key);
                if (resource == null)
                {
                    payload.Resources.Add(new()
                    {
                        ResourceTypeId = produced.Key,
                        Value = produced.Value,
                    });
                }
                else
                {
                    resource.Value += produced.Value;
                }
            }

            payload.Cycles = Math.Max(1, payload.Cycles) + 1;
            playerEvent.Data = JsonSerializer.Serialize(payload);
            return;
        }

        _context.PlayerEvents.Add(new()
        {
            PlayerId = playerId,
            Type = PlayerEventType.ManufactureFinished,
            Date = now,
            Data = JsonSerializer.Serialize(new ManufactureFinishedPayload
            {
                DomikTypeId = domikTypeId,
                Resources = producedByResourceTypeId.Select(x => new ManufactureFinishedResourcePayload
                    {
                        ResourceTypeId = x.Key,
                        Value = x.Value,
                    })
                    .ToList(),
                Cycles = 1,
            }),
        });
    }

    /// <summary>
    /// Записывает в журнал итог кормления трудяги Корчмой после смены.
    /// </summary>
    /// <remarks>
    /// Сливает событие с уже существующим <see cref="PlayerEventType.WorkerMeal"/> текущего часового окна и с той же причиной
    /// <paramref name="reason"/>: счётчик и ресурсы суммируются, а имя и род трудяги обезличиваются (обнуляются).
    /// Новое (несливаемое) событие получает <c>variant</c> – случайный индекс текстового варианта.
    /// </remarks>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <param name="workerName">Имя накормленного трудяги; <see langword="null"/>, если кормление не состоялось.</param>
    /// <param name="workerGender">Грамматический род имени трудяги (<see cref="Workers.WorkerGender"/>); <see langword="null"/>, если кормление не состоялось.</param>
    /// <param name="reason"><see langword="null"/> – трудяга поел; <c>"forbidden"</c> – вся еда заповедана в кладовой; <c>"empty"</c> – еды нет вовсе.</param>
    /// <param name="resourcesByTypeId">Съеденные ресурсы по идентификатору типа; пустой словарь, если кормление не состоялось.</param>
    public void RecordWorkerMeal(int playerId, string? workerName, int? workerGender, string? reason, Dictionary<int, int> resourcesByTypeId)
    {
        var now = DateTimeHelper.GetNowDate();
        var events = GetMergeCandidates(playerId, PlayerEventType.WorkerMeal, GetBucketStart(now));
        foreach (var playerEvent in events)
        {
            var payload = ReadPayload<WorkerMealPayload>(playerEvent.Data);
            if (payload == null || payload.Reason != reason)
            {
                continue;
            }

            foreach (var resource in resourcesByTypeId)
            {
                var existing = payload.Resources.FirstOrDefault(x => x.ResourceTypeId == resource.Key);
                if (existing == null)
                {
                    payload.Resources.Add(new()
                    {
                        ResourceTypeId = resource.Key,
                        Value = resource.Value,
                    });
                }
                else
                {
                    existing.Value += resource.Value;
                }
            }

            payload.Count++;
            payload.WorkerName = null;
            payload.WorkerGender = null;
            playerEvent.Data = JsonSerializer.Serialize(payload);
            return;
        }

        _context.PlayerEvents.Add(new()
        {
            PlayerId = playerId,
            Type = PlayerEventType.WorkerMeal,
            Date = now,
            Data = JsonSerializer.Serialize(new WorkerMealPayload
            {
                Count = 1,
                WorkerName = workerName,
                WorkerGender = workerGender,
                Variant = reason == null ? Random.Shared.Next(8) : 0,
                Reason = reason,
                Resources = resourcesByTypeId.Select(x => new WorkerMealResourcePayload
                    {
                        ResourceTypeId = x.Key,
                        Value = x.Value,
                    })
                    .ToList(),
            }),
        });
    }

    /// <summary>
    /// Отдаёт недоставленные события игрока, двигает курсор доставки и чистит хвост журнала.
    /// </summary>
    /// <remarks>
    /// Недоставленное – всё, что больше <see cref="Data.Entities.Player.LastDeliveredEventId"/>; сами строки не мутируются,
    /// поэтому слияние может дополнять уже отданное событие, не рискуя выдать его второй раз.
    /// Ничего не удаляет: хранение – окно <see cref="Retention"/>, за которое отвечает фоновая чистка
    /// (<see cref="PlayerEventCleanupService"/>), а не горячий путь запроса.
    /// <para>
    /// За раз отдаётся не больше <see cref="RecapBatch"/> событий. Если порция вышла полной, недоставленное осталось,
    /// и <see cref="Data.Entities.Player.LastSeen"/> не сдвигается – иначе следующая порция приехала бы с
    /// <c>AwaySeconds = 0</c> и «Пока вас не было» соврала бы про длительность отсутствия.
    /// </para>
    /// </remarks>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <param name="now">Момент открытия витрины.</param>
    public RecapModel TakeRecap(int playerId, DateTime now)
    {
        var cursor = _context.Players.AsNoTracking()
            .Where(x => x.Id == playerId)
            .Select(x => new { x.LastSeen, x.LastDeliveredEventId })
            .FirstOrDefault();
        var lastSeen = cursor?.LastSeen;
        var lastDelivered = cursor?.LastDeliveredEventId ?? 0;

        var events = _context.PlayerEvents.AsNoTracking()
            .Where(x => x.PlayerId == playerId && x.Id > lastDelivered)
            .OrderBy(x => x.Id)
            .Take(RecapBatch)
            .ToList();
        var deliveredTo = events.Count > 0 ? events[^1].Id : lastDelivered;
        var overflowed = events.Count == RecapBatch;

        _context.Players.Where(x => x.Id == playerId)
            .ExecuteUpdate(s => s
                .SetProperty(x => x.LastSeen, p => overflowed ? p.LastSeen : now)
                .SetProperty(x => x.LastDeliveredEventId, (long?)deliveredTo));

        return new()
        {
            AwaySeconds = lastSeen == null ? 0 : Math.Max(0, (int)(now - lastSeen.Value).TotalSeconds),
            Events = events.Select(x => new RecapEventModel
                {
                    Id = x.Id,
                    Type = x.Type,
                    Date = x.Date,
                    Data = ReadPayload(x.Data),
                })
                .ToList(),
        };
    }

    public List<RecapEventModel> GetRecentEvents(int playerId, int count = 30)
    {
        return GetEventsBefore(playerId, beforeId: null, count);
    }

    /// <summary>
    /// Отдаёт страницу журнала игрока – записи старше курсора, новейшие первыми.
    /// </summary>
    /// <remarks>
    /// Курсор – <see cref="Data.Entities.PlayerEvent.Id"/> последней записи предыдущей страницы. Сортировка по нему же:
    /// <see cref="Data.Entities.PlayerEvent.Date"/> усечена до секунд, ничьих много, и опираться на неё курсор не может.
    /// Глубина ограничена окном хранения <see cref="Retention"/>.
    /// </remarks>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <param name="beforeId">Курсор: отдаются записи строго старше него. <see langword="null"/> – первая страница.</param>
    /// <param name="count">Размер страницы; больше <see cref="MaxPageSize"/> не отдаётся.</param>
    /// <param name="group">Группа для фильтра; <see cref="PlayerEventGroup.None"/> – без фильтра.</param>
    public List<RecapEventModel> GetEventsBefore(int playerId, long? beforeId, int count, PlayerEventGroup group = PlayerEventGroup.None)
    {
        var query = _context.PlayerEvents.AsNoTracking().Where(x => x.PlayerId == playerId);
        if (beforeId != null)
        {
            query = query.Where(x => x.Id < beforeId.Value);
        }

        if (group != PlayerEventGroup.None)
        {
            var types = PlayerEventGroups.TypesOf(group);
            query = query.Where(x => types.Contains(x.Type));
        }

        return query.OrderByDescending(x => x.Id)
            .Take(Math.Clamp(count, 1, MaxPageSize))
            .ToList()
            .Select(x => new RecapEventModel
            {
                Id = x.Id,
                Type = x.Type,
                Date = x.Date,
                Data = ReadPayload(x.Data),
            })
            .ToList();
    }

    /// <summary>
    /// Сколько всего событий игрока хранится – объём летописи, а не число показанных записей.
    /// </summary>
    /// <remarks>
    /// Счётчик в шапке журнала прежде называл длину выданной страницы, выдавая её за объём истории.
    /// </remarks>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <param name="group">Группа для фильтра; <see cref="PlayerEventGroup.None"/> – без фильтра.</param>
    public int CountEvents(int playerId, PlayerEventGroup group = PlayerEventGroup.None)
    {
        var query = _context.PlayerEvents.AsNoTracking().Where(x => x.PlayerId == playerId);
        if (group != PlayerEventGroup.None)
        {
            var types = PlayerEventGroups.TypesOf(group);
            query = query.Where(x => types.Contains(x.Type));
        }

        return query.Count();
    }

    /// <summary>
    /// Итоги за окно: сколько событий каждой группы случилось начиная с <paramref name="since"/>.
    /// </summary>
    /// <remarks>
    /// Ретроспектива считается по сохранённым строкам, а не по отдельным агрегатам: окно хранения
    /// <see cref="Retention"/> делает это возможным, а объём (порядка тысяч строк на игрока) – дешёвым.
    /// Группы без единого события в выдачу не попадают.
    /// </remarks>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <param name="since">Начало окна.</param>
    public Dictionary<PlayerEventGroup, int> CountByGroup(int playerId, DateTime since)
    {
        var countByType = _context.PlayerEvents.AsNoTracking()
            .Where(x => x.PlayerId == playerId && x.Date >= since)
            .GroupBy(x => x.Type)
            .Select(x => new { Type = x.Key, Count = x.Count() })
            .ToList();

        return countByType.GroupBy(x => PlayerEventGroups.Of(x.Type))
            .Where(x => x.Key != PlayerEventGroup.None)
            .ToDictionary(x => x.Key, x => x.Sum(y => y.Count));
    }

    /// <summary>
    /// Удаляет одну порцию событий старше <paramref name="cutoff"/> и сообщает, сколько строк удалено.
    /// </summary>
    /// <remarks>
    /// Порциями, чтобы не держать длинную транзакцию на всей таблице. Вызывающий повторяет, пока возвращается
    /// <paramref name="batchSize"/> – значит, чистить ещё есть что.
    /// </remarks>
    /// <param name="cutoff">Граница окна хранения; события строго старше неё удаляются.</param>
    /// <param name="batchSize">Потолок одной порции.</param>
    public int DeleteOlderThan(DateTime cutoff, int batchSize)
    {
        var doomedIds = _context.PlayerEvents.Where(x => x.Date < cutoff).OrderBy(x => x.Date).Take(batchSize).Select(x => x.Id);
        return _context.PlayerEvents.Where(x => doomedIds.Contains(x.Id)).ExecuteDelete();
    }

    private sealed class ManufactureFinishedPayload
    {
        [JsonPropertyName("domikTypeId")]
        public int DomikTypeId { get; set; }

        [JsonPropertyName("resources")]
        public List<ManufactureFinishedResourcePayload> Resources { get; set; } = new();

        [JsonPropertyName("cycles")]
        public int Cycles { get; set; }
    }

    private sealed class ManufactureFinishedResourcePayload
    {
        [JsonPropertyName("resourceTypeId")]
        public int ResourceTypeId { get; set; }

        [JsonPropertyName("value")]
        public int Value { get; set; }
    }

    private sealed class ManufactureRepeatFailedPayload
    {
        [JsonPropertyName("domikId")]
        public int DomikId { get; set; }

        [JsonPropertyName("domikTypeId")]
        public int DomikTypeId { get; set; }

        [JsonPropertyName("receiptId")]
        public int ReceiptId { get; set; }

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    private sealed class WorkerMealPayload
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("workerName")]
        public string? WorkerName { get; set; }

        [JsonPropertyName("workerGender")]
        public int? WorkerGender { get; set; }

        [JsonPropertyName("variant")]
        public int Variant { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }

        [JsonPropertyName("resources")]
        public List<WorkerMealResourcePayload> Resources { get; set; } = new();
    }

    private sealed class WorkerMealResourcePayload
    {
        [JsonPropertyName("resourceTypeId")]
        public int ResourceTypeId { get; set; }

        [JsonPropertyName("value")]
        public int Value { get; set; }
    }
}
