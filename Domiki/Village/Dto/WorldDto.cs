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
    /// Название ресурса, которым торгует NPC-сосед.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> для деревень игроков (см. <see cref="IsNpc"/>).
    /// </remarks>
    public string? NpcResourceName { get; init; }

    /// <summary>
    /// Технический код NPC-соседа.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> для деревень игроков (см. <see cref="IsNpc"/>).
    /// </remarks>
    public string? NpcLogicName { get; init; }

    /// <summary>
    /// Публичный идентификатор принятого деревней уклада – технический код соседа-источника (см.
    /// <see cref="Reference.Dto.ResourceTypeDto.LogicName"/> у ресурса соседа, тот же код у <see cref="NpcLogicName"/>).
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – деревня уклад не приняла. Наружу отдаётся только код уклада, а не внутренний id соседа;
    /// по коду клиент показывает бейдж «уклад …» рядом с гербом (см. GAMEDESIGN.md §3 Слой 4).
    /// </remarks>
    public string? ProfileLogicName { get; init; }

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

    /// <summary>
    /// Число переездов хозяина деревни в новую долину.
    /// </summary>
    /// <remarks>
    /// Единственное, что памятный столб показывает публично в каталоге, – значок числа переездов (GAMEDESIGN.md §3.7).
    /// У NPC-соседей всегда <c>0</c>.
    /// </remarks>
    public required int RelocationCount { get; init; }
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
    /// Публичный идентификатор принятого хозяином уклада – технический код соседа-источника (тот же код, что у
    /// NPC-соседа в списке мира).
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – хозяин уклад не принял. При визите наружу отдаётся только код уклада, а не внутренний id соседа.
    /// </remarks>
    public string? ProfileLogicName { get; init; }

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

    /// <summary>
    /// Может ли текущий игрок (гость) подсобить хозяину прямо сейчас.
    /// </summary>
    /// <remarks>
    /// <see langword="false"/>, если гость и хозяин – один игрок, у гостя не названа деревня, обжитость гостя ниже
    /// <see cref="HelpUnlockLevel"/>, гость уже подсобил сегодня (<see cref="AlreadyHelpedToday"/>), у хозяина
    /// исчерпан суточный кап (<see cref="HostCapReached"/>) или у деревни хозяина нет активных работ (<see cref="HasActiveWork"/>).
    /// </remarks>
    public required bool CanHelp { get; init; }

    /// <summary>
    /// Подсобил ли текущий игрок сегодня уже какой-то деревне.
    /// </summary>
    public required bool AlreadyHelpedToday { get; init; }

    /// <summary>
    /// Исчерпан ли суточный кап деревни хозяина на число визитов «подсобить».
    /// </summary>
    /// <remarks>
    /// Значение константы <see cref="Village.HelpManager.HostHelpCapPerDay"/>.
    /// </remarks>
    public required bool HostCapReached { get; init; }

    /// <summary>
    /// Есть ли у деревни хозяина хотя бы одна активная работа (улучшение домика или производство) с остатком больше нуля.
    /// </summary>
    public required bool HasActiveWork { get; init; }

    /// <summary>
    /// Порог обжитости, открывающий «подсобить».
    /// </summary>
    /// <remarks>
    /// Значение константы <see cref="Village.HelpManager.HelpUnlockLevel"/>; сравнивается с обжитостью самого гостя,
    /// не хозяина (см. <see cref="CanHelp"/>).
    /// </remarks>
    public required int HelpUnlockLevel { get; init; }

    /// <summary>
    /// Сколько раз хозяин переезжал в новую долину.
    /// </summary>
    public required int RelocationCount { get; init; }

    /// <summary>
    /// Суммарная обжитость всех прожитых хозяином деревень на дни их отъездов.
    /// </summary>
    /// <remarks>
    /// Вместе с <see cref="RelocationCount"/> составляет публичную строку памятного столба в карточке визита.
    /// </remarks>
    public required int ChronicleLevelSum { get; init; }
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
