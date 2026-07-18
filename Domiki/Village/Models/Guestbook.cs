namespace Domiki.Web.Village.Models;

/// <summary>
/// Книга гостей хозяина – лента последних записей и счётчик визитов за сезон.
/// </summary>
/// <remarks>
/// Собирается в <see cref="GuestbookManager.GetGuestbook"/> и отдаётся на клиент как <see cref="Dto.GuestbookDto"/>.
/// </remarks>
public class GuestbookModel
{
    /// <summary>
    /// Число визитов гостей за текущий сезон.
    /// </summary>
    /// <remarks>
    /// Деривация – COUNT строк <see cref="Data.Entities.GuestbookEntry"/> по дням текущего сезона, не накопитель.
    /// </remarks>
    public int VisitsThisSeason { get; set; }

    /// <summary>
    /// Лента последних записей в книге гостей.
    /// </summary>
    /// <remarks>
    /// Только визиты с оставленной фразой (см. <see cref="GuestbookEntryModel.PhraseId"/>), не более
    /// <see cref="GuestbookManager.GuestbookShowCount"/> штук.
    /// </remarks>
    public GuestbookEntryModel[] Entries { get; set; } = [];
}

/// <summary>
/// Книга гостей при read-only визите игрока в чужую деревню.
/// </summary>
/// <remarks>
/// Собирается в <see cref="GuestbookManager.GetVisitGuestbook"/> и отдаётся на клиент как часть <see cref="Dto.VillageVisitDto"/>.
/// </remarks>
public class VisitGuestbookModel
{
    /// <summary>
    /// Лента последних записей в книге гостей хозяина.
    /// </summary>
    /// <remarks>
    /// Только визиты с оставленной фразой, не более <see cref="GuestbookManager.GuestbookShowCount"/> штук.
    /// </remarks>
    public GuestbookEntryModel[] Entries { get; set; } = [];

    /// <summary>
    /// Может ли текущий гость оставить запись хозяину сегодня.
    /// </summary>
    /// <remarks>
    /// <see langword="false"/>, если гость и хозяин – один игрок, у гостя не названа деревня, запись уже оставлена сегодня
    /// (см. <see cref="AlreadyLeftToday"/>) или обжитость гостя ниже <see cref="GuestbookUnlockLevel"/>.
    /// </remarks>
    public bool CanLeaveEntry { get; set; }

    /// <summary>
    /// Оставлял ли гость запись хозяину сегодня.
    /// </summary>
    public bool AlreadyLeftToday { get; set; }

    /// <summary>
    /// Порог обжитости, открывающий книгу гостей.
    /// </summary>
    /// <remarks>
    /// Значение константы <see cref="GuestbookManager.GuestbookUnlockLevel"/>; сравнивается с обжитостью самого гостя,
    /// не хозяина.
    /// </remarks>
    public int GuestbookUnlockLevel { get; set; }
}

/// <summary>
/// Одна запись в книге гостей.
/// </summary>
/// <remarks>
/// О госте наружу отдаются только название деревни и герб – приватность.
/// </remarks>
public class GuestbookEntryModel
{
    /// <summary>
    /// Идентификатор гостя.
    /// </summary>
    public int GuestPlayerId { get; set; }

    /// <summary>
    /// Название деревни гостя.
    /// </summary>
    public required string GuestVillageName { get; set; }

    /// <summary>
    /// Индекс пиктограммы герба гостя.
    /// </summary>
    public int GuestCrestIcon { get; set; }

    /// <summary>
    /// Индекс цвета герба гостя.
    /// </summary>
    public int GuestCrestColor { get; set; }

    /// <summary>
    /// Выбранная гостем фраза записи.
    /// </summary>
    /// <value>От <c>1</c> до <see cref="GuestbookManager.GuestbookPhraseCount"/>; тексты фраз – клиентские константы.</value>
    public int PhraseId { get; set; }

    /// <summary>
    /// Момент, когда запись была оставлена.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public DateTime Date { get; set; }
}
