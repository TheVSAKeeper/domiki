namespace Domiki.Web.Core.Dto;

/// <summary>
/// Справочник типа постройки вместе с параметрами, персонализированными под игрока.
/// </summary>
/// <remarks>
/// Персонализация: доступное количество, чертёж, гейты.
/// </remarks>
public sealed record DomikTypeDto
{
    /// <summary>
    /// Идентификатор типа постройки.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Отображаемое название.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Техническое имя типа, используется как ключ на клиенте (иконки, тексты).
    /// </summary>
    public required string LogicName { get; init; }

    /// <summary>
    /// Сколько домиков этого типа игрок может иметь одновременно.
    /// </summary>
    /// <remarks>
    /// С учётом уже открытых гейтов количества.
    /// </remarks>
    public required int MaxCount { get; init; }

    /// <summary>
    /// Сколько домиков этого типа ещё можно купить прямо сейчас.
    /// </summary>
    public required int AvailableCount { get; init; }

    /// <summary>
    /// Потолок уровня для построек этого типа.
    /// </summary>
    public required int MaxLevel { get; init; }

    /// <summary>
    /// Обжитость деревни, начиная с которой тип открывается в магазине построек.
    /// </summary>
    public required int UnlockLevel { get; init; }

    /// <summary>
    /// Чертёж, дающий право построить этот тип.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – тип доступен без чертежа.
    /// </remarks>
    public int? BlueprintId { get; init; }

    /// <summary>
    /// Обжитость, на которой откроется следующий слот сверх текущего <see cref="MaxCount"/>.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – приростов количества больше нет.
    /// </remarks>
    public int? NextCountGateLevel { get; init; }

    /// <summary>
    /// Параметры каждого уровня апгрейда – стоимость, модификаторы, доступные рецепты.
    /// </summary>
    public required UpgradeLevelDto[] Levels { get; init; }
}
