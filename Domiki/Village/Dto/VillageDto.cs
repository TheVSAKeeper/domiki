namespace Domiki.Web.Village.Dto;

/// <summary>
/// Идентичность деревни игрока – название, герб и настройка кормления трудяг.
/// </summary>
public sealed record VillageDto
{
    /// <summary>
    /// Название деревни, выбранное игроком.
    /// </summary>
    public string? VillageName { get; init; }

    /// <summary>
    /// Индекс пиктограммы герба из готового набора.
    /// </summary>
    public required int CrestIcon { get; init; }

    /// <summary>
    /// Индекс цвета герба из готового набора.
    /// </summary>
    public required int CrestColor { get; init; }

    /// <summary>
    /// Включено ли кормление трудяг хлебом.
    /// </summary>
    /// <remarks>
    /// <see langword="true"/> – кормление включено: вдвое сокращает время отдыха уставшего трудяги ценой <c>1</c> хлеба за случай.
    /// </remarks>
    public required bool FeedWorkers { get; init; }
}

/// <summary>
/// Запрос на смену имени и герба деревни.
/// </summary>
/// <remarks>
/// Применяется к <see cref="VillageDto.VillageName"/>, <see cref="VillageDto.CrestIcon"/> и <see cref="VillageDto.CrestColor"/>.
/// </remarks>
public sealed record SetVillageDto
{
    /// <summary>
    /// Новое название деревни.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Новый индекс пиктограммы герба.
    /// </summary>
    public int CrestIcon { get; init; }

    /// <summary>
    /// Новый индекс цвета герба.
    /// </summary>
    public int CrestColor { get; init; }
}

/// <summary>
/// Запрос на включение или выключение кормления трудяг.
/// </summary>
/// <remarks>
/// Устанавливает <see cref="VillageDto.FeedWorkers"/>.
/// </remarks>
public sealed record SetFeedWorkersDto
{
    /// <summary>
    /// Новое значение флага кормления хлебом.
    /// </summary>
    /// <remarks>
    /// <see langword="true"/> – включить кормление, <see langword="false"/> – выключить.
    /// </remarks>
    public bool Enabled { get; init; }
}
