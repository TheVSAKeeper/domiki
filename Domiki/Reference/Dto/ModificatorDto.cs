namespace Domiki.Web.Reference.Dto;

/// <summary>
/// Эффект уровня постройки – например, требуемое число трудяг (тип <c>plodder</c>).
/// </summary>
public class ModificatorDto
{
    /// <summary>
    /// Тип модификатора – ссылка на <see cref="ModificatorTypeDto.Id"/>.
    /// </summary>
    public int TypeId { get; set; }

    /// <summary>
    /// Величина эффекта.
    /// </summary>
    public int Value { get; set; }
}
