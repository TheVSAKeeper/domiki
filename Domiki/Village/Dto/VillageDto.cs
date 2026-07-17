namespace Domiki.Web.Village.Dto;

/// <summary>
/// Идентичность деревни игрока – название, герб и настройка кормления трудяг.
/// </summary>
public class VillageDto
{
    /// <summary>
    /// Название деревни, выбранное игроком.
    /// </summary>
    public string VillageName { get; set; }

    /// <summary>
    /// Индекс пиктограммы герба из готового набора.
    /// </summary>
    public int CrestIcon { get; set; }

    /// <summary>
    /// Индекс цвета герба из готового набора.
    /// </summary>
    public int CrestColor { get; set; }

    /// <summary>
    /// Включено ли кормление трудяг хлебом.
    /// </summary>
    /// <remarks>
    /// <see langword="true"/> – кормление включено: вдвое сокращает время отдыха уставшего трудяги ценой <c>1</c> хлеба за случай.
    /// </remarks>
    public bool FeedWorkers { get; set; }
}

/// <summary>
/// Запрос на смену имени и герба деревни.
/// </summary>
/// <remarks>
/// Применяется к <see cref="VillageDto.VillageName"/>, <see cref="VillageDto.CrestIcon"/> и <see cref="VillageDto.CrestColor"/>.
/// </remarks>
public class SetVillageDto
{
    /// <summary>
    /// Новое название деревни.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Новый индекс пиктограммы герба.
    /// </summary>
    public int CrestIcon { get; set; }

    /// <summary>
    /// Новый индекс цвета герба.
    /// </summary>
    public int CrestColor { get; set; }
}

/// <summary>
/// Запрос на включение или выключение кормления трудяг.
/// </summary>
/// <remarks>
/// Устанавливает <see cref="VillageDto.FeedWorkers"/>.
/// </remarks>
public class SetFeedWorkersDto
{
    /// <summary>
    /// Новое значение флага кормления хлебом.
    /// </summary>
    /// <remarks>
    /// <see langword="true"/> – включить кормление, <see langword="false"/> – выключить.
    /// </remarks>
    public bool Enabled { get; set; }
}
