namespace Domiki.Web.Village.Dto;

/// <summary>
/// Идентичность деревни – название и герб.
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
    public string? Name { get; init; }

    /// <summary>
    /// Новый индекс пиктограммы герба.
    /// </summary>
    public int CrestIcon { get; init; }

    /// <summary>
    /// Новый индекс цвета герба.
    /// </summary>
    public int CrestColor { get; init; }
}
