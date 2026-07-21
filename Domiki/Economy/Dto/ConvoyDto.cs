namespace Domiki.Web.Economy.Dto;

/// <summary>
/// Одна позиция ассортимента обоза – тип ресурса и цена покупки одной единицы.
/// </summary>
public sealed record ConvoyItemDto
{
    /// <summary>
    /// Тип покупаемого ресурса – ссылка на справочник <see cref="Reference.Dto.ResourceTypeDto.Id"/>.
    /// </summary>
    public required int ResourceTypeId { get; init; }

    /// <summary>
    /// Цена одной единицы ресурса в монетах.
    /// </summary>
    public required int Price { get; init; }
}

/// <summary>
/// Обоз одного соседа – ассортимент, цены и остаток скользящего суточного лимита покупок.
/// </summary>
public sealed record ConvoyDto
{
    /// <summary>
    /// Идентификатор соседа.
    /// </summary>
    public required int NeighborId { get; init; }

    /// <summary>
    /// Отображаемое имя соседа.
    /// </summary>
    public required string NeighborName { get; init; }

    /// <summary>
    /// Технический код соседа, используется как ключ на клиенте.
    /// </summary>
    public required string NeighborLogicName { get; init; }

    /// <summary>
    /// Позиции ассортимента, доступные для покупки прямо сейчас.
    /// </summary>
    /// <remarks>
    /// Пусто, если <see cref="IsLocked"/>.
    /// </remarks>
    public required ConvoyItemDto[] Items { get; init; }

    /// <summary>
    /// Суточный лимит покупок у соседа.
    /// </summary>
    public required int Limit { get; init; }

    /// <summary>
    /// Сколько ещё единиц ресурса можно купить у соседа в текущем окне.
    /// </summary>
    public required int Remaining { get; init; }

    /// <summary>
    /// Момент, когда скользящее окно лимита обновится и остаток снова станет полным.
    /// </summary>
    /// <value>Момент в UTC.</value>
    /// <remarks>
    /// <see langword="null"/>, если окно ещё не начато.
    /// </remarks>
    public DateTime? WindowResetDate { get; init; }

    /// <summary>
    /// Признак того, что обоз соседа закрыт для покупок – не хватает репутации.
    /// </summary>
    public required bool IsLocked { get; init; }
}

/// <summary>
/// Запрос на покупку ресурса у обоза соседа.
/// </summary>
public sealed record BuyFromConvoyDto
{
    /// <summary>
    /// Сосед, у обоза которого совершается покупка – ссылка на справочник <see cref="Economy.Models.Neighbor.Id"/>.
    /// </summary>
    public required int NeighborId { get; init; }

    /// <summary>
    /// Покупаемый тип ресурса – ссылка на справочник <see cref="Reference.Dto.ResourceTypeDto.Id"/>.
    /// </summary>
    public required int ResourceTypeId { get; init; }

    /// <summary>
    /// Количество единиц ресурса.
    /// </summary>
    public required int Count { get; init; }
}
