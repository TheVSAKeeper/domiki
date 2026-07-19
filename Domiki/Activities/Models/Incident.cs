namespace Domiki.Web.Activities.Models;

/// <summary>
/// Активное происшествие с пропавшим в походе трудягой.
/// </summary>
/// <remarks>
/// Собирается в <see cref="Activities.IncidentManager.Get"/> и отдаётся на клиент как <see cref="Dto.IncidentDto"/>.
/// </remarks>
public class Incident
{
    /// <summary>
    /// Идентификатор происшествия.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Идентификатор пропавшего трудяги.
    /// </summary>
    public int MissingWorkerId { get; set; }

    /// <summary>
    /// Идентификатор типа похода, в котором пропал трудяга.
    /// </summary>
    public int ExpeditionTypeId { get; set; }

    /// <summary>
    /// Индекс клиентского шаблона текста происшествия.
    /// </summary>
    public int TemplateId { get; set; }

    /// <summary>
    /// Момент создания происшествия.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public DateTime CreateDate { get; set; }

    /// <summary>
    /// Выбранная игроком зацепка.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – поиски ещё не начаты.
    /// </remarks>
    public int? ClueId { get; set; }

    /// <summary>
    /// Момент завершения поисков.
    /// </summary>
    /// <value>Момент в UTC.</value>
    /// <remarks>
    /// <see langword="null"/> – поиски ещё не начаты.
    /// </remarks>
    public DateTime? SearchEndDate { get; set; }

    /// <summary>
    /// Момент самостоятельного возвращения пропавшего без поисков.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public DateTime AutoReturnDate { get; set; }

    /// <summary>
    /// Идентификаторы трудяг, назначенных на поиски, без пропавшего.
    /// </summary>
    public int[] SearchWorkerIds { get; set; } = [];
}
