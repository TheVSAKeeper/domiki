namespace Domiki.Web.Reference.Dto;

/// <summary>
/// Количество ресурса одного типа.
/// </summary>
/// <remarks>
/// Используется и как остаток на складе игрока, и как позиция в стоимости или рецепте.
/// </remarks>
public class ResourceDto
{
    /// <summary>
    /// Тип ресурса – ссылка на <see cref="ResourceTypeDto.Id"/>.
    /// </summary>
    public int TypeId { get; set; }

    /// <summary>
    /// Количество.
    /// </summary>
    public int Value { get; set; }
}
