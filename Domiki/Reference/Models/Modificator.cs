namespace Domiki.Web.Reference.Models;

/// <summary>
/// Эффект уровня постройки – например, требуемое число трудяг (тип <c>plodder</c>).
/// </summary>
/// <remarks>
/// Собирается в <see cref="Reference.ResourceManager.GetDomikTypes"/> как часть уровня постройки и отдаётся на клиент как
/// <see cref="Dto.ModificatorDto"/>.
/// </remarks>
public class Modificator
{
    /// <summary>
    /// Вид модификатора – ссылка на справочник <see cref="ModificatorType.Id"/>.
    /// </summary>
    public required ModificatorType Type { get; set; }

    /// <summary>
    /// Величина эффекта.
    /// </summary>
    public int Value { get; set; }
}
