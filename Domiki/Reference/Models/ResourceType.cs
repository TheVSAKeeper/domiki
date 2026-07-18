namespace Domiki.Web.Reference.Models;

/// <summary>
/// Справочник типов ресурсов.
/// </summary>
/// <remarks>
/// Собирается в <see cref="Reference.ResourceManager.GetResourceTypes"/> и отдаётся на клиент как <see cref="Dto.ResourceTypeDto"/>;
/// рыночная стоимость типа считается отдельно, см. <see cref="Reference.ResourceManager.GetMarketValue"/>.
/// </remarks>
public class ResourceType
{
    /// <summary>
    /// Идентификатор типа ресурса.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Отображаемое название ресурса.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Технический код типа ресурса, используется как ключ на клиенте.
    /// </summary>
    public string? LogicName { get; set; }
}
