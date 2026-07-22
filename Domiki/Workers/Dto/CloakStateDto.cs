namespace Domiki.Web.Workers.Dto;

/// <summary>
/// Состояние запаса и износа плащей игрока для клиента.
/// </summary>
public sealed record CloakStateDto
{
    /// <summary>
    /// Число плащей на складе игрока.
    /// </summary>
    public required int Stock { get; init; }

    /// <summary>
    /// Число плащей, выданных на незавершённые смены.
    /// </summary>
    /// <remarks>
    /// Может превышать <see cref="Stock"/>, если выданный плащ продан на рынке. Клиенту следует ограничивать доступный остаток снизу нулём.
    /// </remarks>
    public required int OutOnShifts { get; init; }

    /// <summary>
    /// Накопленный общий износ плащей в сменах.
    /// </summary>
    public required int WearPoints { get; init; }

    /// <summary>
    /// Число смен, после которого один плащ изнашивается.
    /// </summary>
    public required int LifetimeShifts { get; init; }
}
