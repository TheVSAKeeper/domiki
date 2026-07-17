namespace Domiki.Web.Core.Dto;

/// <summary>
/// Запущенное в домике производство по рецепту.
/// </summary>
public sealed record ManufactureDto
{
    /// <summary>
    /// Идентификатор производства.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Момент завершения производства.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public required DateTime FinishDate { get; init; }

    /// <summary>
    /// Сколько трудяг занято в этом производстве.
    /// </summary>
    public required int PlodderCount { get; init; }

    /// <summary>
    /// Рецепт, по которому идёт производство – ссылка на <see cref="Reference.Dto.ReceiptDto.Id"/>.
    /// </summary>
    public required int ReceiptId { get; init; }

    /// <summary>
    /// Перезапускать ли рецепт автоматически по завершении.
    /// </summary>
    /// <remarks>
    /// Работает, пока хватает ресурсов и свободных трудяг.
    /// </remarks>
    public required bool AutoRepeat { get; init; }
}
