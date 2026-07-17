namespace Domiki.Web.Reference.Dto;

/// <summary>
/// Количество ресурса одного типа.
/// </summary>
/// <remarks>
/// Используется и как остаток на складе игрока, и как позиция в стоимости или рецепте.
/// </remarks>
public sealed record ResourceDto
{
    /// <summary>
    /// Тип ресурса – ссылка на <see cref="ResourceTypeDto.Id"/>.
    /// </summary>
    public required int TypeId { get; init; }

    /// <summary>
    /// Количество.
    /// </summary>
    public required int Value { get; init; }
}
