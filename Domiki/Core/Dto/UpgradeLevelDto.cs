using Domiki.Web.Reference.Dto;

namespace Domiki.Web.Core.Dto;

/// <summary>
/// Параметры одного уровня апгрейда постройки.
/// </summary>
/// <remarks>
/// Стоимость перехода, эффекты и доступные на этом уровне рецепты.
/// </remarks>
public class UpgradeLevelDto
{
    /// <summary>
    /// Значение уровня.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Сколько нужно ресурсов для перехода на этот уровень.
    /// </summary>
    public ResourceDto[] Resources { get; set; }

    /// <summary>
    /// Что нам даёт этот уровень.
    /// </summary>
    public ModificatorDto[] Modificators { get; set; }

    /// <summary>
    /// Что можно производить в постройке.
    /// </summary>
    public int[] ReceiptIds { get; set; }

    /// <summary>
    /// Сколько производств можно держать запущенными в домике одновременно на этом уровне.
    /// </summary>
    public int MaxManufactureCount { get; set; }
}
