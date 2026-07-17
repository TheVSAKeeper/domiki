namespace Domiki.Web.Core.Dto;

/// <summary>
/// Запущенное в домике производство по рецепту.
/// </summary>
public class ManufactureDto
{
    /// <summary>
    /// Идентификатор производства.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Момент завершения производства.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public DateTime FinishDate { get; set; }

    /// <summary>
    /// Сколько трудяг занято в этом производстве.
    /// </summary>
    public int PlodderCount { get; set; }

    /// <summary>
    /// Рецепт, по которому идёт производство – ссылка на <see cref="Reference.Dto.ReceiptDto.Id"/>.
    /// </summary>
    public int ReceiptId { get; set; }

    /// <summary>
    /// Перезапускать ли рецепт автоматически по завершении.
    /// </summary>
    /// <remarks>
    /// Работает, пока хватает ресурсов и свободных трудяг.
    /// </remarks>
    public bool AutoRepeat { get; set; }
}
