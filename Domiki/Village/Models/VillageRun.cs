namespace Domiki.Web.Village.Models;

/// <summary>
/// Отрезок жизни одной деревни игрока – от основания до переезда.
/// </summary>
/// <remarks>
/// Собирается из памятного столба и текущей деревни (<see cref="RelocationManager.GetVillageRuns"/>). Нужен журналу,
/// чтобы отнести событие к деревне по его времени: собственного ключа деревни события не несут.
/// </remarks>
public class VillageRun
{
    /// <summary>
    /// Имя деревни на этом отрезке.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – деревня осталась безымянной.
    /// </remarks>
    public string? VillageName { get; set; }

    /// <summary>
    /// Начало отрезка.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Конец отрезка – момент переезда.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – это текущая деревня, отрезок ещё не закрыт.
    /// </remarks>
    public DateTime? EndDate { get; set; }
}
