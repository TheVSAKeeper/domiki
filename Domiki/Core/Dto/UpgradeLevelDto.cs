using Domiki.Web.Reference.Dto;

namespace Domiki.Web.Core.Dto;

/// <summary>
/// Параметры одного уровня апгрейда постройки.
/// </summary>
/// <remarks>
/// Стоимость перехода, эффекты и доступные на этом уровне рецепты.
/// </remarks>
public sealed record UpgradeLevelDto
{
    /// <summary>
    /// Значение уровня.
    /// </summary>
    public required int Value { get; init; }

    /// <summary>
    /// Сколько нужно ресурсов для перехода на этот уровень.
    /// </summary>
    public required ResourceDto[] Resources { get; init; }

    /// <summary>
    /// Что нам даёт этот уровень.
    /// </summary>
    public required ModificatorDto[] Modificators { get; init; }

    /// <summary>
    /// Что можно производить в постройке.
    /// </summary>
    public required int[] ReceiptIds { get; init; }

    /// <summary>
    /// Сколько производств можно держать запущенными в домике одновременно на этом уровне.
    /// </summary>
    public required int MaxManufactureCount { get; init; }
}
