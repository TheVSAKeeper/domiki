using Domiki.Web.Reference.Dto;

namespace Domiki.Web.Village.Dto;

/// <summary>
/// Декор игрока и накопленный уют деревни.
/// </summary>
public class DecorStateDto
{
    /// <summary>
    /// Справочник типов декора вместе с ценой и доступностью для игрока.
    /// </summary>
    public DecorTypeDto[] Types { get; set; }

    /// <summary>
    /// Декор, уже купленный игроком, по типам.
    /// </summary>
    public PlayerDecorDto[] Owned { get; set; }

    /// <summary>
    /// Суммарные очки уюта от всего декора игрока (сумма <see cref="DecorTypeDto.ComfortPoints"/>) – ускоряют отдых трудяг в бараке.
    /// </summary>
    public int Comfort { get; set; }
}

/// <summary>
/// Тип декора – цена, источник получения и вклад в уют.
/// </summary>
public class DecorTypeDto
{
    /// <summary>
    /// Идентификатор типа декора.
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
    /// Очки уюта, которые даёт один экземпляр декора.
    /// </summary>
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
    /// Имя соседа-владельца декора.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/>, если <see cref="NeighborId"/> не задан.
    /// </remarks>
    public string NeighborName { get; set; }

    /// <summary>
    /// Порог репутации у соседа, открывающий покупку декора.
    /// </summary>
    /// <remarks>
    /// Сравнивается с <see cref="Economy.Dto.NeighborReputationDto.Points"/> у этого соседа.
    /// </remarks>
    public int ReputationThreshold { get; set; }

    /// <summary>
    /// Стоимость одного экземпляра декора в ресурсах.
    /// </summary>
    public ResourceDto[] Cost { get; set; }
}

/// <summary>
/// Количество декора одного типа, купленного игроком.
/// </summary>
public class PlayerDecorDto
{
    /// <summary>
    /// Тип декора – ссылка на <see cref="DecorTypeDto.Id"/>.
    /// </summary>
    public int DecorTypeId { get; set; }

    /// <summary>
    /// Сколько экземпляров этого декора куплено.
    /// </summary>
    public int Count { get; set; }
}
