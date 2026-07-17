namespace Domiki.Web.Core.Dto;

/// <summary>
/// Экземпляр домика игрока в снимке состояния игры – тип, уровень и идущее улучшение.
/// </summary>
public sealed record DomikDto
{
    /// <summary>
    /// Номер домика, уникальный в пределах игрока.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Тип постройки – ссылка на <see cref="DomikTypeDto.Id"/>.
    /// </summary>
    public required int TypeId { get; init; }

    /// <summary>
    /// Текущий уровень домика.
    /// </summary>
    /// <remarks>
    /// <c>Level == 0</c> – домик ещё строится и к производству недоступен.
    /// </remarks>
    public required int Level { get; init; }

    /// <summary>
    /// Момент завершения текущего улучшения.
    /// </summary>
    /// <value>Момент в UTC.</value>
    /// <remarks>
    /// <see langword="null"/> – улучшение не идёт.
    /// </remarks>
    public DateTime? FinishDate { get; init; }

    /// <summary>
    /// Длительность текущего улучшения.
    /// </summary>
    /// <value>Секунды.</value>
    /// <remarks>
    /// <see langword="null"/> – улучшение не идёт.
    /// </remarks>
    public int? UpgradeSeconds { get; init; }

    /// <summary>
    /// Производства, запущенные в этом домике.
    /// </summary>
    /// <remarks>
    /// Пустой массив, если ничего не производится.
    /// </remarks>
    public required ManufactureDto[] Manufactures { get; init; }
}
