namespace Domiki.Web.Activities.Dto;

/// <summary>
/// Состояние экспедиций игрока.
/// </summary>
/// <remarks>
/// Отряды в походе (<see cref="Active"/>), справочник типов (<see cref="Types"/>) и счётчик гарантии редкой добычи
/// (<see cref="ExpeditionsSincePity"/>).
/// </remarks>
public sealed record ExpeditionStateDto
{
    /// <summary>
    /// Отряды, находящиеся в походе прямо сейчас.
    /// </summary>
    /// <remarks>
    /// Пустой массив – ни одна экспедиция не запущена.
    /// </remarks>
    public required ExpeditionDto[] Active { get; init; }

    /// <summary>
    /// Справочник доступных типов экспедиций.
    /// </summary>
    public required ExpeditionTypeDto[] Types { get; init; }

    /// <summary>
    /// Число завершённых экспедиций подряд без редкой добычи – счётчик гарантии (<c>pity</c>).
    /// </summary>
    /// <remarks>
    /// Сравнивается с <see cref="PityThreshold"/>; сбрасывается в <c>0</c> при выпадении редкой добычи
    /// (см. <see cref="Activities.ExpeditionManager.FinishExpedition"/>).
    /// </remarks>
    public required int ExpeditionsSincePity { get; init; }

    /// <summary>
    /// Порог <see cref="ExpeditionsSincePity"/>, при достижении которого следующая добыча гарантированно редкая.
    /// </summary>
    /// <remarks>
    /// Равен <see cref="Activities.ExpeditionManager.ExpeditionPityThreshold"/>.
    /// </remarks>
    public required int PityThreshold { get; init; }

    /// <summary>
    /// Максимум одновременно идущих экспедиций.
    /// </summary>
    /// <remarks>
    /// Равен уровню разведывательной хижины (см. <see cref="Activities.ExpeditionManager.GetScoutHutLevel"/>).
    /// </remarks>
    public required int MaxActive { get; init; }
}

/// <summary>
/// Отряд, отправленный в поход, – срок возвращения и итоговая добыча ещё не выданы.
/// </summary>
public sealed record ExpeditionDto
{
    /// <summary>
    /// Идентификатор экспедиции.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Тип экспедиции – ссылка на <see cref="ExpeditionTypeDto.Id"/>.
    /// </summary>
    public required int ExpeditionTypeId { get; init; }

    /// <summary>
    /// Название типа экспедиции.
    /// </summary>
    public required string ExpeditionName { get; init; }

    /// <summary>
    /// Момент отправки отряда.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public required DateTime StartDate { get; init; }

    /// <summary>
    /// Момент возвращения отряда, когда добыча станет доступна.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public required DateTime FinishDate { get; init; }
}

/// <summary>
/// Тип экспедиции – требуемое снаряжение, длительность похода и лут-таблица.
/// </summary>
public sealed record ExpeditionTypeDto
{
    /// <summary>
    /// Идентификатор типа экспедиции.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Отображаемое название.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Техническое имя типа, используется как ключ на клиенте.
    /// </summary>
    public required string LogicName { get; init; }

    /// <summary>
    /// Длительность похода.
    /// </summary>
    /// <value>Секунды.</value>
    public required int DurationSeconds { get; init; }

    /// <summary>
    /// Сколько трудяг уходит в отряд.
    /// </summary>
    public required int WorkerCount { get; init; }

    /// <summary>
    /// Стоимость снаряжения отряда в золоте.
    /// </summary>
    public required int GoldCost { get; init; }

    /// <summary>
    /// Число бросков по лут-таблице при возвращении отряда.
    /// </summary>
    public required int RollCount { get; init; }

    /// <summary>
    /// Лут-таблица экспедиции – возможная добыча по одному броску.
    /// </summary>
    public required ExpeditionLootDto[] Loot { get; init; }

    /// <summary>
    /// Снаряжение, которое нужно собрать перед отправкой отряда.
    /// </summary>
    public required ExpeditionEquipmentDto[] Equipment { get; init; }
}

/// <summary>
/// Ресурс снаряжения, требуемый для отправки отряда в поход.
/// </summary>
public sealed record ExpeditionEquipmentDto
{
    /// <summary>
    /// Тип ресурса снаряжения – ссылка на <see cref="Reference.Dto.ResourceTypeDto.Id"/>.
    /// </summary>
    public required int ResourceTypeId { get; init; }

    /// <summary>
    /// Количество ресурса, которое спишется при отправке отряда.
    /// </summary>
    public required int Value { get; init; }

    /// <summary>
    /// <see langword="true"/> – ресурс необязателен, отряд можно отправить и без него.
    /// </summary>
    public required bool IsOptional { get; init; }
}

/// <summary>
/// Одна строка лут-таблицы экспедиции – вид добычи и её вероятностные границы.
/// </summary>
public sealed record ExpeditionLootDto
{
    /// <summary>
    /// Вид добычи – значение <see cref="Data.Entities.ExpeditionLootKind"/>.
    /// </summary>
    public required int Kind { get; init; }

    /// <summary>
    /// Тип ресурса добычи – ссылка на <see cref="Reference.Dto.ResourceTypeDto.Id"/>.
    /// </summary>
    /// <remarks>
    /// Заполнено только при <see cref="Kind"/> = <see cref="Data.Entities.ExpeditionLootKind.Resource"/>.
    /// </remarks>
    public int? ResourceTypeId { get; init; }

    /// <summary>
    /// Тип декора добычи – ссылка на <see cref="Village.Dto.DecorTypeDto.Id"/>.
    /// </summary>
    /// <remarks>
    /// Заполнено только при <see cref="Kind"/> = <see cref="Data.Entities.ExpeditionLootKind.Decor"/>.
    /// </remarks>
    public int? DecorTypeId { get; init; }

    /// <summary>
    /// Конкретный чертёж добычи – ссылка на <see cref="BlueprintDto.Id"/>.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – случайный чертёж из ещё не полученных игроком (заполнено только при <see cref="Kind"/> =
    /// <see cref="Data.Entities.ExpeditionLootKind.Blueprint"/>).
    /// </remarks>
    public int? BlueprintId { get; init; }

    /// <summary>
    /// Нижняя граница случайного количества ресурса.
    /// </summary>
    public required int MinValue { get; init; }

    /// <summary>
    /// Верхняя граница случайного количества ресурса.
    /// </summary>
    public required int MaxValue { get; init; }

    /// <summary>
    /// <see langword="true"/> – добыча редкая, засчитывается в счётчик гарантии (<c>pity</c>).
    /// </summary>
    /// <remarks>
    /// Учитывается в <see cref="ExpeditionStateDto.ExpeditionsSincePity"/>.
    /// </remarks>
    public required bool IsRare { get; init; }
}
