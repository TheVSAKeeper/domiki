namespace Domiki.Web.Reference.Dto;

/// <summary>
/// Справочник типа ресурса.
/// </summary>
public sealed record ResourceTypeDto
{
    /// <summary>
    /// Идентификатор типа ресурса.
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
    /// Можно ли использовать ресурс как еду для трудяг и провиант в походах.
    /// </summary>
    public required bool IsFood { get; init; }

    /// <summary>
    /// Цена в магазине с фиксированными ценами.
    /// </summary>
    /// <value>Монеты.</value>
    /// <remarks>
    /// Пол рынка: не влияет на цены ярмарки и заказов.
    /// </remarks>
    public required int MarketValue { get; init; }
}
