namespace Domiki.Web.Activities.Dto;

/// <summary>
/// Активное происшествие-загадка в постройке.
/// </summary>
public sealed record DomikIncidentDto
{
    /// <summary>
    /// Идентификатор происшествия.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Идентификатор типа постройки, в которой возникла загадка.
    /// </summary>
    public required int DomikTypeId { get; init; }

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
    /// Момент самостоятельной развязки загадки без поисков.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public required DateTime AutoResolveDate { get; init; }

    /// <summary>
    /// Идентификаторы трудяг, назначенных на поиски.
    /// </summary>
    public required int[] SearchWorkerIds { get; init; }
}
