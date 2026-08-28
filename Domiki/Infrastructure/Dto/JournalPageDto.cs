namespace Domiki.Web.Infrastructure.Dto;

/// <summary>
/// Страница журнала событий вместе с тем, что нужно для её показа.
/// </summary>
public sealed record JournalPageDto
{
    /// <summary>
    /// Записи страницы, новейшие первыми.
    /// </summary>
    /// <remarks>
    /// Пустой массив – летопись кончилась, дальше листать нечего.
    /// </remarks>
    public required RecapEventDto[] Events { get; init; }

    /// <summary>
    /// Сколько всего событий хранится с учётом выбранного фильтра.
    /// </summary>
    /// <remarks>
    /// Объём летописи, а не длина страницы: счётчик в шапке журнала называет именно его.
    /// </remarks>
    public required int TotalCount { get; init; }

    /// <summary>
    /// Прогоны деревень игрока – по ним журнал расставляет разделители переезда.
    /// </summary>
    /// <remarks>
    /// Отдаются только с первой страницей: на последующих они те же самые.
    /// </remarks>
    public required VillageRunDto[] VillageRuns { get; init; }

    /// <summary>
    /// Итоги за последнюю неделю по группам событий – ретроспектива в шапке журнала.
    /// </summary>
    /// <remarks>
    /// Отдаются только с первой страницей. Пустой массив – за неделю не случилось ничего.
    /// </remarks>
    public required JournalDigestEntryDto[] Digest { get; init; }
}

/// <summary>
/// Строка ретроспективы – одна группа событий и её счёт за окно.
/// </summary>
public sealed record JournalDigestEntryDto
{
    /// <summary>
    /// Группа событий.
    /// </summary>
    /// <remarks>
    /// Строковое имя значения <see cref="Data.Entities.PlayerEventGroup"/>.
    /// </remarks>
    public required string Group { get; init; }

    /// <summary>
    /// Сколько событий группы случилось за окно.
    /// </summary>
    public required int Count { get; init; }
}

/// <summary>
/// Отрезок жизни одной деревни игрока.
/// </summary>
public sealed record VillageRunDto
{
    /// <summary>
    /// Имя деревни на этом отрезке; <see langword="null"/> – деревня осталась безымянной.
    /// </summary>
    public required string? VillageName { get; init; }

    /// <summary>
    /// Начало отрезка.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public required DateTime StartDate { get; init; }

    /// <summary>
    /// Конец отрезка; <see langword="null"/> – текущая деревня.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public required DateTime? EndDate { get; init; }
}
