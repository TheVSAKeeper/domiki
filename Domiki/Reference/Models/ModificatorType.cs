namespace Domiki.Web.Reference.Models;

/// <summary>
/// Справочник видов модификаторов, которые может давать уровень постройки (например, добавка вместимости трудяг).
/// </summary>
/// <remarks>
/// Собирается в <see cref="Reference.ResourceManager.GetModificatorTypes"/>.
/// </remarks>
public class ModificatorType
{
    /// <summary>
    /// Идентификатор вида модификатора.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Отображаемое название модификатора.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Технический код вида модификатора, используется как ключ на клиенте.
    /// </summary>
    public string? LogicName { get; set; }
}
