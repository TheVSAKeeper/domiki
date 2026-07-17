namespace Domiki.Web.Reference.Dto;

/// <summary>
/// Рецепт производства – N входов дают M выходов за время силами трудяг.
/// </summary>
public sealed record ReceiptDto
{
    /// <summary>
    /// Идентификатор рецепта.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Отображаемое название.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Техническое имя рецепта, используется как ключ на клиенте.
    /// </summary>
    public required string LogicName { get; init; }

    /// <summary>
    /// Ресурсы, обязательные для запуска производства.
    /// </summary>
    public required ResourceDto[] InputResources { get; init; }

    /// <summary>
    /// Дополнительные ресурсы (например, инструмент), не обязательные для запуска.
    /// </summary>
    /// <remarks>
    /// Дают <see cref="OutputBonusPercent"/> к выходу при списании. Может быть пустым.
    /// </remarks>
    public required ResourceDto[] OptionalInputResources { get; init; }

    /// <summary>
    /// Длительность одного цикла производства.
    /// </summary>
    /// <value>Секунды.</value>
    public required int DurationSeconds { get; init; }

    /// <summary>
    /// Прибавка к проценту выхода, если при запуске были списаны <see cref="OptionalInputResources"/>.
    /// </summary>
    /// <value>Проценты.</value>
    public required int OutputBonusPercent { get; init; }

    /// <summary>
    /// Ресурсы, выдаваемые по завершении производства.
    /// </summary>
    public required ResourceDto[] OutputResources { get; init; }

    /// <summary>
    /// Сколько трудяг требуется, чтобы запустить рецепт.
    /// </summary>
    public required int PlodderCount { get; init; }
}
