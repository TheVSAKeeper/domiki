namespace Domiki.Web.Reference.Dto;

/// <summary>
/// Справочник типа ресурса.
/// </summary>
public class ResourceTypeDto
{
    /// <summary>
    /// Идентификатор типа ресурса.
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
    /// Цена в магазине с фиксированными ценами.
    /// </summary>
    /// <value>Монеты.</value>
    /// <remarks>
    /// Пол рынка: не влияет на цены ярмарки и заказов.
    /// </remarks>
    public int MarketValue { get; set; }
}
