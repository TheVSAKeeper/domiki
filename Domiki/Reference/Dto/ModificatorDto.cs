namespace Domiki.Web.Reference.Dto;

/// <summary>
/// Эффект уровня постройки – например, требуемое число трудяг (тип <c>plodder</c>).
/// </summary>
public sealed record ModificatorDto
{
    /// <summary>
    /// Тип модификатора – ссылка на <see cref="ModificatorTypeDto.Id"/>.
    /// </summary>
    public required int TypeId { get; init; }

    /// <summary>
    /// Величина эффекта.
    /// </summary>
    public required int Value { get; init; }
}
