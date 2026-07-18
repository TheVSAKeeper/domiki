namespace Domiki.Web.Reference.Models;

/// <summary>
/// Количество ресурса одного типа.
/// </summary>
/// <remarks>
/// Используется и как остаток на складе игрока, и как позиция в стоимости или рецепте.
/// </remarks>
public class Resource
{
    /// <summary>
    /// Тип ресурса – ссылка на справочник <see cref="ResourceType.Id"/>.
    /// </summary>
    public required ResourceType Type { get; set; }

    /// <summary>
    /// Количество.
    /// </summary>
    public int Value { get; set; }
}
