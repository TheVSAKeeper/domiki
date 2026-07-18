using Domiki.Web.Reference.Models;

namespace Domiki.Web.Village.Models;

/// <summary>
/// Тип декора – цена, источник получения и вклад в уют.
/// </summary>
/// <remarks>
/// Справочник наполняется через <see cref="Reference.ResourceManager.GetDecorTypes"/>, используется в <see cref="DecorManager"/>
/// и отдаётся на клиент как <see cref="Dto.DecorTypeDto"/>.
/// </remarks>
public class DecorType
{
    /// <summary>
    /// Идентификатор типа декора.
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
    /// Очки уюта, которые даёт один экземпляр декора.
    /// </summary>
    /// <remarks>
    /// Ускоряют отдых и сокращают болезнь трудяг в бараке (см. <see cref="Core.DomikManager.RestComfortMaxPercent"/>).
    /// </remarks>
    public int ComfortPoints { get; set; }

    /// <summary>
    /// Можно ли купить декор в магазине напрямую.
    /// </summary>
    /// <remarks>
    /// <see langword="true"/> – доступен в магазине; <see langword="false"/> – только через экспедиции или репутацию.
    /// </remarks>
    public bool IsPurchasable { get; set; }

    /// <summary>
    /// Сосед, у которого декор открывается репутацией.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – декор не привязан к соседу.
    /// </remarks>
    public int? NeighborId { get; set; }

    /// <summary>
    /// Порог репутации у соседа, открывающий покупку декора.
    /// </summary>
    public int ReputationThreshold { get; set; }

    /// <summary>
    /// Стоимость одного экземпляра декора в ресурсах.
    /// </summary>
    public Resource[] Cost { get; set; } = [];
}

/// <summary>
/// Количество декора одного типа, купленного игроком.
/// </summary>
public class PlayerDecor
{
    /// <summary>
    /// Тип декора – ссылка на <see cref="DecorType.Id"/>.
    /// </summary>
    public int DecorTypeId { get; set; }

    /// <summary>
    /// Сколько экземпляров этого декора куплено.
    /// </summary>
    public int Count { get; set; }
}

/// <summary>
/// Декор игрока и накопленный уют деревни.
/// </summary>
/// <remarks>
/// Собирается в <see cref="DecorManager.GetDecor"/> и отдаётся на клиент как <see cref="Dto.DecorStateDto"/>.
/// </remarks>
public class DecorState
{
    /// <summary>
    /// Справочник типов декора вместе с ценой и доступностью для игрока.
    /// </summary>
    public DecorType[] Types { get; set; } = [];

    /// <summary>
    /// Декор, уже купленный игроком, по типам.
    /// </summary>
    public PlayerDecor[] Owned { get; set; } = [];

    /// <summary>
    /// Суммарные очки уюта от всего декора игрока – ускоряют отдых трудяг в бараке.
    /// </summary>
    /// <remarks>
    /// Вычисляется в <see cref="DecorCalculator.GetComfort"/> как сумма <see cref="DecorType.ComfortPoints"/>.
    /// </remarks>
    public int Comfort { get; set; }
}
