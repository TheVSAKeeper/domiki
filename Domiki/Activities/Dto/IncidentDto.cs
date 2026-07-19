namespace Domiki.Web.Activities.Dto;

/// <summary>
/// Активное происшествие с пропавшим в походе трудягой.
/// </summary>
public sealed record IncidentDto
{
    /// <summary>
    /// Идентификатор происшествия.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Идентификатор пропавшего трудяги – ссылка на <see cref="Workers.Dto.WorkerDto.Id"/>.
    /// </summary>
    public required int MissingWorkerId { get; init; }

    /// <summary>
    /// Идентификатор типа похода, в котором пропал трудяга.
    /// </summary>
    public required int ExpeditionTypeId { get; init; }

    /// <summary>
    /// Индекс клиентского шаблона текста происшествия.
    /// </summary>
    public required int TemplateId { get; init; }

    /// <summary>
    /// Момент создания происшествия.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public required DateTime CreateDate { get; init; }

    /// <summary>
    /// Выбранная игроком зацепка.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – поиски ещё не начаты.
    /// </remarks>
    public int? ClueId { get; init; }

    /// <summary>
    /// Момент завершения поисков.
    /// </summary>
    /// <value>Момент в UTC.</value>
    /// <remarks>
    /// <see langword="null"/> – поиски ещё не начаты.
    /// </remarks>
    public DateTime? SearchEndDate { get; init; }

    /// <summary>
    /// Момент самостоятельного возвращения пропавшего без поисков.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public required DateTime AutoReturnDate { get; init; }

    /// <summary>
    /// Идентификаторы трудяг, назначенных на поиски, без пропавшего.
    /// </summary>
    public required int[] SearchWorkerIds { get; init; }
}
