namespace Domiki.Web.Activities.Dto;

/// <summary>
/// Состояние экспедиций игрока.
/// </summary>
/// <remarks>
/// Отряды в походе (<see cref="Active"/>), справочник типов (<see cref="Types"/>) и счётчик гарантии редкой добычи
/// (<see cref="ExpeditionsSincePity"/>).
/// </remarks>
public class ExpeditionStateDto
{
    /// <summary>
    /// Отряды, находящиеся в походе прямо сейчас.
    /// </summary>
    /// <remarks>
    /// Пустой массив – ни одна экспедиция не запущена.
    /// </remarks>
    public ExpeditionDto[] Active { get; set; }

    /// <summary>
    /// Справочник доступных типов экспедиций.
    /// </summary>
    public ExpeditionTypeDto[] Types { get; set; }

    /// <summary>
    /// Число завершённых экспедиций подряд без редкой добычи – счётчик гарантии (<c>pity</c>).
    /// </summary>
    /// <remarks>
    /// Сравнивается с <see cref="PityThreshold"/>; сбрасывается в <c>0</c> при выпадении редкой добычи
    /// (см. <see cref="Activities.ExpeditionManager.FinishExpedition"/>).
    /// </remarks>
    public int ExpeditionsSincePity { get; set; }

    /// <summary>
    /// Порог <see cref="ExpeditionsSincePity"/>, при достижении которого следующая добыча гарантированно редкая.
    /// </summary>
    /// <remarks>
    /// Равен <see cref="Activities.ExpeditionManager.ExpeditionPityThreshold"/>.
    /// </remarks>
    public int PityThreshold { get; set; }

    /// <summary>
    /// Максимум одновременно идущих экспедиций.
    /// </summary>
    /// <remarks>
    /// Равен уровню разведывательной хижины (см. <see cref="Activities.ExpeditionManager.GetScoutHutLevel"/>).
    /// </remarks>
    public int MaxActive { get; set; }
}

/// <summary>
/// Отряд, отправленный в поход, – срок возвращения и итоговая добыча ещё не выданы.
/// </summary>
public class ExpeditionDto
{
    /// <summary>
    /// Идентификатор экспедиции.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Тип экспедиции – ссылка на <see cref="ExpeditionTypeDto.Id"/>.
    /// </summary>
    public int ExpeditionTypeId { get; set; }

    /// <summary>
    /// Название типа экспедиции.
    /// </summary>
    public string ExpeditionName { get; set; }

    /// <summary>
    /// Момент отправки отряда.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Момент возвращения отряда, когда добыча станет доступна.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public DateTime FinishDate { get; set; }
}

/// <summary>
/// Тип экспедиции – требуемое снаряжение, длительность похода и лут-таблица.
/// </summary>
public class ExpeditionTypeDto
{
    /// <summary>
    /// Идентификатор типа экспедиции.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Отображаемое название.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Техническое имя типа, используется как ключ на клиенте.
    /// </summary>
    public string LogicName { get; set; }

    /// <summary>
    /// Длительность похода.
    /// </summary>
    /// <value>Секунды.</value>
    public int DurationSeconds { get; set; }

    /// <summary>
    /// Сколько трудяг уходит в отряд.
    /// </summary>
    public int WorkerCount { get; set; }

    /// <summary>
    /// Стоимость снаряжения отряда в золоте.
    /// </summary>
    public int GoldCost { get; set; }

    /// <summary>
    /// Число бросков по лут-таблице при возвращении отряда.
    /// </summary>
    public int RollCount { get; set; }

    /// <summary>
    /// Лут-таблица экспедиции – возможная добыча по одному броску.
    /// </summary>
    public ExpeditionLootDto[] Loot { get; set; }

    /// <summary>
    /// Снаряжение, которое нужно собрать перед отправкой отряда.
    /// </summary>
    public ExpeditionEquipmentDto[] Equipment { get; set; }
}

/// <summary>
/// Ресурс снаряжения, требуемый для отправки отряда в поход.
/// </summary>
public class ExpeditionEquipmentDto
{
    /// <summary>
    /// Тип ресурса снаряжения – ссылка на <see cref="Reference.Dto.ResourceTypeDto.Id"/>.
    /// </summary>
    public int ResourceTypeId { get; set; }

    /// <summary>
    /// Количество ресурса, которое спишется при отправке отряда.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// <see langword="true"/> – ресурс необязателен, отряд можно отправить и без него.
    /// </summary>
    public bool IsOptional { get; set; }
}

/// <summary>
/// Одна строка лут-таблицы экспедиции – вид добычи и её вероятностные границы.
/// </summary>
public class ExpeditionLootDto
{
    /// <summary>
    /// Вид добычи – значение <see cref="Data.Entities.ExpeditionLootKind"/>.
    /// </summary>
    public int Kind { get; set; }

    /// <summary>
    /// Тип ресурса добычи – ссылка на <see cref="Reference.Dto.ResourceTypeDto.Id"/>.
    /// </summary>
    /// <remarks>
    /// Заполнено только при <see cref="Kind"/> = <see cref="Data.Entities.ExpeditionLootKind.Resource"/>.
    /// </remarks>
    public int? ResourceTypeId { get; set; }

    /// <summary>
    /// Тип декора добычи – ссылка на <see cref="Village.Dto.DecorTypeDto.Id"/>.
    /// </summary>
    /// <remarks>
    /// Заполнено только при <see cref="Kind"/> = <see cref="Data.Entities.ExpeditionLootKind.Decor"/>.
    /// </remarks>
    public int? DecorTypeId { get; set; }

    /// <summary>
    /// Конкретный чертёж добычи – ссылка на <see cref="BlueprintDto.Id"/>.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – случайный чертёж из ещё не полученных игроком (заполнено только при <see cref="Kind"/> =
    /// <see cref="Data.Entities.ExpeditionLootKind.Blueprint"/>).
    /// </remarks>
    public int? BlueprintId { get; set; }

    /// <summary>
    /// Нижняя граница случайного количества ресурса.
    /// </summary>
    public int MinValue { get; set; }

    /// <summary>
    /// Верхняя граница случайного количества ресурса.
    /// </summary>
    public int MaxValue { get; set; }

    /// <summary>
    /// <see langword="true"/> – добыча редкая, засчитывается в счётчик гарантии (<c>pity</c>).
    /// </summary>
    /// <remarks>
    /// Учитывается в <see cref="ExpeditionStateDto.ExpeditionsSincePity"/>.
    /// </remarks>
    public bool IsRare { get; set; }
}
