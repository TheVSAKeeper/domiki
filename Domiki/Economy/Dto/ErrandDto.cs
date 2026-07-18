namespace Domiki.Web.Economy.Dto;

/// <summary>
/// Поручение соседа – квест-оффер, оплачиваемый временем трудяг.
/// </summary>
public sealed record ErrandDto
{
    /// <summary>
    /// Идентификатор поручения.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Сосед, приславший поручение.
    /// </summary>
    public required int NeighborId { get; init; }

    /// <summary>
    /// Отображаемое имя соседа.
    /// </summary>
    public required string NeighborName { get; init; }

    /// <summary>
    /// Техническое имя соседа, используется как ключ на клиенте.
    /// </summary>
    public required string NeighborLogicName { get; init; }

    /// <summary>
    /// Индекс клиентского шаблона текста поручения.
    /// </summary>
    public required int TemplateId { get; init; }

    /// <summary>
    /// Момент истечения офферной фазы поручения.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public required DateTime ExpireDate { get; init; }

    /// <summary>
    /// Момент принятия поручения игроком.
    /// </summary>
    /// <value>Момент в UTC.</value>
    /// <remarks>
    /// <see langword="null"/> – поручение ещё в офферной фазе.
    /// </remarks>
    public DateTime? AcceptDate { get; init; }

    /// <summary>
    /// Выбранная игроком зацепка, задающая длительность поисков.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – зацепка ещё не выбрана.
    /// </remarks>
    public int? ClueId { get; init; }

    /// <summary>
    /// Момент завершения поисков.
    /// </summary>
    /// <value>Момент в UTC.</value>
    /// <remarks>
    /// <see langword="null"/> – поручение ещё не принято.
    /// </remarks>
    public DateTime? FinishDate { get; init; }

    /// <summary>
    /// Трудяги, занятые в поисках по поручению – ссылки на <see cref="Workers.Dto.WorkerDto.Id"/>.
    /// </summary>
    /// <remarks>
    /// Пустой массив – поручение ещё не принято.
    /// </remarks>
    public required int[] WorkerIds { get; init; }
}
