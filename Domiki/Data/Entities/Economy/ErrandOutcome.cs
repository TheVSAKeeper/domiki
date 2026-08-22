namespace Domiki.Web.Data.Entities;

/// <summary>
/// Чем закончилось поручение соседа.
/// </summary>
public enum ErrandOutcome
{
    /// <summary>
    /// Значение не задано – поручение ещё активно.
    /// </summary>
    None = 0,

    /// <summary>
    /// Поиски дошли до развязки, награды выданы.
    /// </summary>
    Resolved = 1,

    /// <summary>
    /// Оффер не приняли до истечения срока.
    /// </summary>
    Expired = 2,

    /// <summary>
    /// Игрок отказался от оффера или прервал начатые поиски.
    /// </summary>
    Cancelled = 3,
}
