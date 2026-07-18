namespace Domiki.Web.Village.Dto;

/// <summary>
/// Книга гостей хозяина – лента последних записей и счётчик визитов за сезон.
/// </summary>
public sealed record GuestbookDto
{
    /// <summary>
    /// Число визитов гостей за текущий сезон.
    /// </summary>
    /// <remarks>
    /// Деривация по дням текущего сезона, не накопитель.
    /// </remarks>
    public required int VisitsThisSeason { get; init; }

    /// <summary>
    /// Лента последних записей в книге гостей.
    /// </summary>
    /// <remarks>
    /// Только визиты с оставленной фразой, не более <see cref="Village.GuestbookManager.GuestbookShowCount"/> штук.
    /// </remarks>
    public required GuestbookEntryDto[] Entries { get; init; }
}

/// <summary>
/// Одна запись в книге гостей.
/// </summary>
/// <remarks>
/// О госте наружу отдаются только название деревни и герб – приватность.
/// </remarks>
public sealed record GuestbookEntryDto
{
    /// <summary>
    /// Идентификатор гостя.
    /// </summary>
    public required int GuestPlayerId { get; init; }

    /// <summary>
    /// Название деревни гостя.
    /// </summary>
    public required string GuestVillageName { get; init; }

    /// <summary>
    /// Индекс пиктограммы герба гостя.
    /// </summary>
    public required int GuestCrestIcon { get; init; }

    /// <summary>
    /// Индекс цвета герба гостя.
    /// </summary>
    public required int GuestCrestColor { get; init; }

    /// <summary>
    /// Выбранная гостем фраза записи.
    /// </summary>
    /// <value>От <c>1</c> до <see cref="Village.GuestbookManager.GuestbookPhraseCount"/>; тексты фраз – клиентские константы.</value>
    public required int PhraseId { get; init; }

    /// <summary>
    /// Момент, когда запись была оставлена.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public required DateTime Date { get; init; }
}
