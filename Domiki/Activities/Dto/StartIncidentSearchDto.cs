namespace Domiki.Web.Activities.Dto;

/// <summary>
/// Параметры начала поисков пропавшего трудяги по происшествию.
/// </summary>
/// <remarks>
/// Передаётся в <see cref="Activities.ActivityController.StartIncidentSearch"/> телом POST-запроса.
/// </remarks>
public sealed record StartIncidentSearchDto
{
    /// <summary>
    /// Идентификатор происшествия.
    /// </summary>
    public required int IncidentId { get; init; }

    /// <summary>
    /// Выбранная зацепка, индекс в <see cref="Activities.IncidentManager.ClueDurationHours"/>.
    /// </summary>
    public required int ClueId { get; init; }

    /// <summary>
    /// Идентификаторы свободных трудяг, отправляемых на поиски.
    /// </summary>
    public required int[] WorkerIds { get; init; }
}
