namespace Domiki.Web.Activities.Models;

/// <summary>
/// Активное происшествие-загадка в постройке.
/// </summary>
/// <remarks>
/// Собирается в <see cref="Activities.IncidentManager.GetDomik"/> и отдаётся на клиент как <see cref="Dto.DomikIncidentDto"/>.
/// </remarks>
public class DomikIncident
{
    /// <summary>
    /// Идентификатор происшествия.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Идентификатор типа постройки, в которой возникла загадка.
    /// </summary>
    public int DomikTypeId { get; set; }

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
    /// Момент самостоятельной развязки загадки без поисков.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public DateTime AutoResolveDate { get; set; }

    /// <summary>
    /// Идентификаторы трудяг, назначенных на поиски.
    /// </summary>
    public int[] SearchWorkerIds { get; set; } = [];
}
