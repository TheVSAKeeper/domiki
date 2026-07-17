using Domiki.Web.Reference.Dto;

namespace Domiki.Web.Village.Dto;

/// <summary>
/// Декор игрока и накопленный уют деревни.
/// </summary>
public sealed record DecorStateDto
{
    /// <summary>
    /// Справочник типов декора вместе с ценой и доступностью для игрока.
    /// </summary>
    public required DecorTypeDto[] Types { get; init; }

    /// <summary>
    /// Декор, уже купленный игроком, по типам.
    /// </summary>
    public required PlayerDecorDto[] Owned { get; init; }

    /// <summary>
    /// Суммарные очки уюта от всего декора игрока (сумма <see cref="DecorTypeDto.ComfortPoints"/>) – ускоряют отдых трудяг в бараке.
    /// </summary>
    public required int Comfort { get; init; }
}

/// <summary>
/// Тип декора – цена, источник получения и вклад в уют.
/// </summary>
public sealed record DecorTypeDto
{
    /// <summary>
    /// Идентификатор типа декора.
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
    /// Очки уюта, которые даёт один экземпляр декора.
    /// </summary>
    public required int ComfortPoints { get; init; }

    /// <summary>
    /// Можно ли купить декор в магазине напрямую.
    /// </summary>
    /// <remarks>
    /// <see langword="true"/> – доступен в магазине; <see langword="false"/> – только через экспедиции или репутацию.
    /// </remarks>
    public required bool IsPurchasable { get; init; }

    /// <summary>
    /// Сосед, у которого декор открывается репутацией.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – декор не привязан к соседу.
    /// </remarks>
    public int? NeighborId { get; init; }

    /// <summary>
    /// Имя соседа-владельца декора.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/>, если <see cref="NeighborId"/> не задан.
    /// </remarks>
    public string? NeighborName { get; init; }

    /// <summary>
    /// Порог репутации у соседа, открывающий покупку декора.
    /// </summary>
    /// <remarks>
    /// Сравнивается с <see cref="Economy.Dto.NeighborReputationDto.Points"/> у этого соседа.
    /// </remarks>
    public required int ReputationThreshold { get; init; }

    /// <summary>
    /// Стоимость одного экземпляра декора в ресурсах.
    /// </summary>
    public required ResourceDto[] Cost { get; init; }
}

/// <summary>
/// Количество декора одного типа, купленного игроком.
/// </summary>
public sealed record PlayerDecorDto
{
    /// <summary>
    /// Тип декора – ссылка на <see cref="DecorTypeDto.Id"/>.
    /// </summary>
    public required int DecorTypeId { get; init; }

    /// <summary>
    /// Сколько экземпляров этого декора куплено.
    /// </summary>
    public required int Count { get; init; }
}
