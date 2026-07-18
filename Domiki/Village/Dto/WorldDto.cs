using Domiki.Web.Activities.Dto;

namespace Domiki.Web.Village.Dto;

/// <summary>
/// Каталог деревень для экрана «Мир» – рейтинг обжитости и текущий сезон номинаций.
/// </summary>
public sealed record WorldDto
{
    /// <summary>
    /// Все видимые деревни (реальные игроки и NPC-соседи).
    /// </summary>
    /// <remarks>
    /// Отсортированы по убыванию <see cref="WorldVillageDto.Level"/>.
    /// </remarks>
    public required WorldVillageDto[] Villages { get; init; }

    /// <summary>
    /// Текущий сезонный период рейтингов.
    /// </summary>
    /// <remarks>
    /// Определяет период подсчёта <see cref="WorldVillageDto.SeasonOrders"/>, <see cref="WorldVillageDto.SeasonToloka"/>
    /// и <see cref="WorldVillageDto.SeasonExpeditions"/>.
    /// </remarks>
    public required SeasonDto Season { get; init; }

    /// <summary>
    /// Летопись последних завершённых толок.
    /// </summary>
    /// <remarks>
    /// Не более <see cref="Activities.TolokaManager.TolokaArtifactShowCount"/> штук, свежие первыми.
    /// </remarks>
    public required TolokaArtifactDto[] TolokaArtifacts { get; init; }
}

/// <summary>
/// Одна деревня в каталоге экрана «Мир» – реальный игрок или декоративный NPC-сосед.
/// </summary>
public sealed record WorldVillageDto
{
    /// <summary>
    /// Идентификатор игрока-владельца.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – деревня NPC (см. <see cref="IsNpc"/>), не привязана к игроку.
    /// </remarks>
    public int? PlayerId { get; init; }

    /// <summary>
    /// Название деревни.
    /// </summary>
    public required string VillageName { get; init; }

    /// <summary>
    /// Индекс пиктограммы герба.
    /// </summary>
    public required int CrestIcon { get; init; }

    /// <summary>
    /// Индекс цвета герба.
    /// </summary>
    public required int CrestColor { get; init; }

    /// <summary>
    /// Обжитость деревни, по которой отсортирован каталог.
    /// </summary>
    /// <remarks>
    /// См. <see cref="VillageLevelDto.Level"/>.
    /// </remarks>
    public required int Level { get; init; }

    /// <summary>
    /// Является ли деревня NPC-соседом.
    /// </summary>
    /// <remarks>
    /// <see langword="true"/> – NPC-сосед с фиксированным представлением, не реальный игрок.
    /// </remarks>
    public required bool IsNpc { get; init; }

    /// <summary>
    /// Принадлежит ли деревня текущему игроку.
    /// </summary>
    /// <remarks>
    /// <see langword="true"/> – это деревня текущего игрока.
    /// </remarks>
    public required bool IsMe { get; init; }

    /// <summary>
    /// Ресурс, которым торгует NPC-сосед – ссылка на <see cref="Reference.Dto.ResourceTypeDto.Id"/>.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> для деревень игроков (см. <see cref="IsNpc"/>).
    /// </remarks>
    public int? NpcResourceTypeId { get; init; }

    /// <summary>
    /// Технический код NPC-соседа.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> для деревень игроков (см. <see cref="IsNpc"/>).
    /// </remarks>
    public string? NpcLogicName { get; init; }

    /// <summary>
    /// Число выполненных заказов за текущий сезон – счётчик номинации «Лучший поставщик».
    /// </summary>
    public required int SeasonOrders { get; init; }

    /// <summary>
    /// Вклад в толоку за текущий сезон – счётчик номинации «Герой толоки».
    /// </summary>
    public required int SeasonToloka { get; init; }

    /// <summary>
    /// Число завершённых экспедиций за текущий сезон – счётчик номинации «Дальние странники».
    /// </summary>
    public required int SeasonExpeditions { get; init; }

    /// <summary>
    /// Очки уюта деревни – основа номинации «Самая уютная деревня».
    /// </summary>
    /// <remarks>
    /// Совпадает с <see cref="VillageLevelDto.Comfort"/>.
    /// </remarks>
    public required int Comfort { get; init; }
}

/// <summary>
/// Снимок чужой деревни при read-only визите игрока из экрана «Мир».
/// </summary>
public sealed record VillageVisitDto
{
    /// <summary>
    /// Название посещаемой деревни.
    /// </summary>
    public required string VillageName { get; init; }

    /// <summary>
    /// Индекс пиктограммы герба.
    /// </summary>
    public required int CrestIcon { get; init; }

    /// <summary>
    /// Индекс цвета герба.
    /// </summary>
    public required int CrestColor { get; init; }

    /// <summary>
    /// Обжитость посещаемой деревни и её слагаемые.
    /// </summary>
    public required VillageLevelDto Level { get; init; }

    /// <summary>
    /// Постройки посещаемой деревни с их уровнями.
    /// </summary>
    public required VisitBuildingDto[] Buildings { get; init; }

    /// <summary>
    /// Лента последних записей в книге гостей посещаемой деревни.
    /// </summary>
    /// <remarks>
    /// Только визиты с оставленной фразой, не более <see cref="Village.GuestbookManager.GuestbookShowCount"/> штук.
    /// </remarks>
    public required GuestbookEntryDto[] Guestbook { get; init; }

    /// <summary>
    /// Может ли текущий игрок (гость) оставить запись хозяину сегодня.
    /// </summary>
    /// <remarks>
    /// <see langword="false"/>, если гость и хозяин – один игрок, у гостя не названа деревня, запись уже оставлена сегодня
    /// (см. <see cref="AlreadyLeftToday"/>) или обжитость гостя ниже <see cref="GuestbookUnlockLevel"/>.
    /// </remarks>
    public required bool CanLeaveEntry { get; init; }

    /// <summary>
    /// Оставлял ли текущий игрок запись хозяину сегодня.
    /// </summary>
    public required bool AlreadyLeftToday { get; init; }

    /// <summary>
    /// Порог обжитости, открывающий книгу гостей.
    /// </summary>
    /// <remarks>
    /// Значение константы <see cref="Village.GuestbookManager.GuestbookUnlockLevel"/>; сравнивается с обжитостью самого
    /// гостя, не хозяина (см. <see cref="CanLeaveEntry"/>).
    /// </remarks>
    public required int GuestbookUnlockLevel { get; init; }
}

/// <summary>
/// Одна постройка в списке визита в чужую деревню.
/// </summary>
public sealed record VisitBuildingDto
{
    /// <summary>
    /// Название типа постройки.
    /// </summary>
    public required string TypeName { get; init; }

    /// <summary>
    /// Уровень постройки.
    /// </summary>
    public required int Level { get; init; }
}
