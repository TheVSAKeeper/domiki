namespace Domiki.Web.Economy.Models;

/// <summary>
/// Поручение соседа – квест-оффер, оплачиваемый временем трудяг.
/// </summary>
/// <remarks>
/// Собирается в <see cref="Economy.ErrandManager.Get"/> и отдаётся на клиент как <see cref="Dto.ErrandDto"/>.
/// </remarks>
public class Errand
{
    /// <summary>
    /// Идентификатор поручения.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Сосед, приславший поручение.
    /// </summary>
    public required Neighbor Neighbor { get; set; }

    /// <summary>
    /// Индекс клиентского шаблона текста поручения.
    /// </summary>
    public int TemplateId { get; set; }

    /// <summary>
    /// Момент истечения офферной фазы поручения.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public DateTime ExpireDate { get; set; }

    /// <summary>
    /// Момент принятия поручения игроком.
    /// </summary>
    /// <value>Момент в UTC.</value>
    /// <remarks>
    /// <see langword="null"/> – поручение ещё в офферной фазе.
    /// </remarks>
    public DateTime? AcceptDate { get; set; }

    /// <summary>
    /// Выбранная игроком зацепка, задающая длительность поисков.
    /// </summary>
    /// <remarks>
    /// Индекс в <see cref="Economy.ErrandManager.ClueDurationHours"/> и <see cref="Economy.ErrandManager.ClueReputation"/>.
    /// <see langword="null"/> – зацепка ещё не выбрана.
    /// </remarks>
    public int? ClueId { get; set; }

    /// <summary>
    /// Момент завершения поисков.
    /// </summary>
    /// <value>Момент в UTC.</value>
    /// <remarks>
    /// <see langword="null"/> – поручение ещё не принято.
    /// </remarks>
    public DateTime? FinishDate { get; set; }

    /// <summary>
    /// Трудяги, занятые в поисках по поручению.
    /// </summary>
    /// <remarks>
    /// Пустой массив – поручение ещё не принято.
    /// </remarks>
    public int[] WorkerIds { get; set; } = [];
}
