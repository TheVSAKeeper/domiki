namespace Domiki.Web.Core.Models;

/// <summary>
/// Запущенное в домике производство по рецепту.
/// </summary>
/// <remarks>
/// Собирается в <see cref="DomikManager.GetDomiks"/> и отдаётся на клиент как <see cref="Dto.ManufactureDto"/>.
/// </remarks>
public class Manufacture
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
    /// Фактическая длительность производства.
    /// </summary>
    /// <value>Секунды.</value>
    /// <remarks>
    /// Учитывает скорость трудяг и стартовый бонус «нетронутые залежи», поэтому отличается от базовой
    /// <see cref="Reference.Models.Receipt.DurationSeconds"/>.
    /// </remarks>
    public int DurationSeconds { get; set; }

    /// <summary>
    /// Сколько трудяг занято в этом производстве.
    /// </summary>
    public int PlodderCount { get; set; }

    /// <summary>
    /// Рецепт, по которому идёт производство – ссылка на <see cref="Reference.Models.Receipt.Id"/>.
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
