namespace Domiki.Web.Workers.Models;

/// <summary>
/// Состояние запаса и износа плащей игрока.
/// </summary>
public class CloakState
{
    /// <summary>
    /// Число плащей на складе игрока.
    /// </summary>
    public int Stock { get; set; }

    /// <summary>
    /// Число плащей, выданных на незавершённые смены.
    /// </summary>
    public int OutOnShifts { get; set; }

    /// <summary>
    /// Накопленный общий износ плащей в сменах.
    /// </summary>
    public int WearPoints { get; set; }

    /// <summary>
    /// Число смен, после которого один плащ изнашивается.
    /// </summary>
    public int LifetimeShifts { get; set; }
}
