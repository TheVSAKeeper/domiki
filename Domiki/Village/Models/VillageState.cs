namespace Domiki.Web.Village.Models;

/// <summary>
/// Идентичность деревни – название и герб.
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
}
