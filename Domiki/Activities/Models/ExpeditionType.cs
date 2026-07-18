using Domiki.Web.Data.Entities;

namespace Domiki.Web.Activities.Models;

/// <summary>
/// Справочный тип экспедиции – требуемое снаряжение, длительность похода и лут-таблица.
/// </summary>
/// <remarks>
/// Загружается из БД целиком (см. <see cref="Reference.ResourceManager.GetExpeditionTypes"/>); на клиент отдаётся как
/// <see cref="Dto.ExpeditionTypeDto"/>.
/// </remarks>
public class ExpeditionType
{
    /// <summary>
    /// Идентификатор типа экспедиции.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Отображаемое название.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Техническое имя типа, используется как ключ на клиенте.
    /// </summary>
    public required string LogicName { get; set; }

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
    public ExpeditionLoot[] Loot { get; set; } = [];

    /// <summary>
    /// Снаряжение, которое нужно собрать перед отправкой отряда.
    /// </summary>
    public ExpeditionEquipment[] Equipment { get; set; } = [];
}

/// <summary>
/// Ресурс снаряжения, требуемый для отправки отряда в поход.
/// </summary>
public class ExpeditionEquipment
{
    /// <summary>
    /// Тип ресурса снаряжения – ссылка на <see cref="Reference.Models.ResourceType.Id"/>.
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
/// Одна строка лут-таблицы экспедиции – вид добычи, её вероятностные границы и вес в ролле.
/// </summary>
public class ExpeditionLoot
{
    /// <summary>
    /// Вид добычи.
    /// </summary>
    public ExpeditionLootKind Kind { get; set; }

    /// <summary>
    /// Тип ресурса добычи – ссылка на <see cref="Reference.Models.ResourceType.Id"/>.
    /// </summary>
    /// <remarks>
    /// Заполнено только при <see cref="Kind"/> = <see cref="ExpeditionLootKind.Resource"/>.
    /// </remarks>
    public int? ResourceTypeId { get; set; }

    /// <summary>
    /// Тип декора добычи – ссылка на <see cref="Village.Models.DecorType.Id"/>.
    /// </summary>
    /// <remarks>
    /// Заполнено только при <see cref="Kind"/> = <see cref="ExpeditionLootKind.Decor"/>.
    /// </remarks>
    public int? DecorTypeId { get; set; }

    /// <summary>
    /// Конкретный чертёж добычи – ссылка на <see cref="Blueprint.Id"/>.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – случайный чертёж из ещё не полученных игроком (заполнено только при <see cref="Kind"/> =
    /// <see cref="ExpeditionLootKind.Blueprint"/>).
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
    /// Вес строки во взвешенном ролле лута.
    /// </summary>
    /// <remarks>
    /// Чем больше, тем чаще выпадает относительно других строк того же похода (см. <see cref="ExpeditionManager.PickLoot"/>); для
    /// редкой добычи дополнительно масштабируется удачей отряда (см. <see cref="ExpeditionManager.ScaleWeight"/>). На клиент не
    /// передаётся.
    /// </remarks>
    public int Weight { get; set; }

    /// <summary>
    /// <see langword="true"/> – добыча редкая, засчитывается в счётчик гарантии (<c>pity</c>).
    /// </summary>
    /// <remarks>
    /// Учитывается в <see cref="ExpeditionState.ExpeditionsSincePity"/>.
    /// </remarks>
    public bool IsRare { get; set; }
}
