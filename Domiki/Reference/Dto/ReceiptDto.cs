namespace Domiki.Web.Reference.Dto;

/// <summary>
/// Рецепт производства – N входов дают M выходов за время силами трудяг.
/// </summary>
public class ReceiptDto
{
    /// <summary>
    /// Идентификатор рецепта.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Отображаемое название.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Техническое имя рецепта, используется как ключ на клиенте.
    /// </summary>
    public string LogicName { get; set; }

    /// <summary>
    /// Ресурсы, обязательные для запуска производства.
    /// </summary>
    public ResourceDto[] InputResources { get; set; }

    /// <summary>
    /// Дополнительные ресурсы (например, инструмент), не обязательные для запуска.
    /// </summary>
    /// <remarks>
    /// Дают <see cref="OutputBonusPercent"/> к выходу при списании. Может быть пустым.
    /// </remarks>
    public ResourceDto[] OptionalInputResources { get; set; }

    /// <summary>
    /// Длительность одного цикла производства.
    /// </summary>
    /// <value>Секунды.</value>
    public int DurationSeconds { get; set; }

    /// <summary>
    /// Прибавка к проценту выхода, если при запуске были списаны <see cref="OptionalInputResources"/>.
    /// </summary>
    /// <value>Проценты.</value>
    public int OutputBonusPercent { get; set; }

    /// <summary>
    /// Ресурсы, выдаваемые по завершении производства.
    /// </summary>
    public ResourceDto[] OutputResources { get; set; }

    /// <summary>
    /// Сколько трудяг требуется, чтобы запустить рецепт.
    /// </summary>
    public int PlodderCount { get; set; }
}
