namespace Domiki.Web.Village.Models;

/// <summary>
/// Идентичность деревни игрока – название, герб и настройка кормления трудяг.
/// </summary>
/// <remarks>
/// Отдаётся на клиент как <see cref="Dto.VillageDto"/>.
/// </remarks>
public class VillageState
{
    /// <summary>
    /// Название деревни, выбранное игроком.
    /// </summary>
    public string? VillageName { get; set; }

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
