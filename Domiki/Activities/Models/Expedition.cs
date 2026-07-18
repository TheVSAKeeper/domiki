namespace Domiki.Web.Activities.Models;

/// <summary>
/// Отряд, отправленный в поход, – срок возвращения и итоговая добыча ещё не выданы.
/// </summary>
/// <remarks>
/// На клиент отдаётся как <see cref="Dto.ExpeditionDto"/> (см. <see cref="ExpeditionManager.GetExpeditions"/>).
/// </remarks>
public class Expedition
{
    /// <summary>
    /// Идентификатор экспедиции.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Тип похода, по которому отправлен отряд.
    /// </summary>
    public required ExpeditionType ExpeditionType { get; set; }

    /// <summary>
    /// Момент отправки отряда.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Момент возвращения отряда, когда добыча станет доступна.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public DateTime FinishDate { get; set; }
}
