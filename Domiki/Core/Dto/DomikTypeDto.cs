namespace Domiki.Web.Core.Dto;

/// <summary>
/// Справочник типа постройки вместе с параметрами, персонализированными под игрока.
/// </summary>
/// <remarks>
/// Персонализация: доступное количество, чертёж, гейты.
/// </remarks>
public class DomikTypeDto
{
    /// <summary>
    /// Идентификатор типа постройки.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Отображаемое название.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Техническое имя типа, используется как ключ на клиенте (иконки, тексты).
    /// </summary>
    public string LogicName { get; set; }

    /// <summary>
    /// Сколько домиков этого типа игрок может иметь одновременно.
    /// </summary>
    /// <remarks>
    /// С учётом уже открытых гейтов количества.
    /// </remarks>
    public int MaxCount { get; internal set; }

    /// <summary>
    /// Сколько домиков этого типа ещё можно купить прямо сейчас.
    /// </summary>
    public int AvailableCount { get; internal set; }

    /// <summary>
    /// Потолок уровня для построек этого типа.
    /// </summary>
    public int MaxLevel { get; internal set; }

    /// <summary>
    /// Обжитость деревни, начиная с которой тип открывается в магазине построек.
    /// </summary>
    public int UnlockLevel { get; internal set; }

    /// <summary>
    /// Чертёж, дающий право построить этот тип.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – тип доступен без чертежа.
    /// </remarks>
    public int? BlueprintId { get; internal set; }

    /// <summary>
    /// Обжитость, на которой откроется следующий слот сверх текущего <see cref="MaxCount"/>.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – приростов количества больше нет.
    /// </remarks>
    public int? NextCountGateLevel { get; internal set; }

    /// <summary>
    /// Параметры каждого уровня апгрейда – стоимость, модификаторы, доступные рецепты.
    /// </summary>
    public UpgradeLevelDto[] Levels { get; set; }
}
