namespace Domiki.Web.Core.Dto;

/// <summary>
/// Экземпляр домика игрока в снимке состояния игры – тип, уровень и идущее улучшение.
/// </summary>
public class DomikDto
{
    /// <summary>
    /// Номер домика, уникальный в пределах игрока.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Тип постройки – ссылка на <see cref="DomikTypeDto.Id"/>.
    /// </summary>
    public int TypeId { get; set; }

    /// <summary>
    /// Текущий уровень домика.
    /// </summary>
    /// <remarks>
    /// <c>Level == 0</c> – домик ещё строится и к производству недоступен.
    /// </remarks>
    public int Level { get; set; }

    /// <summary>
    /// Момент завершения текущего улучшения.
    /// </summary>
    /// <value>Момент в UTC.</value>
    /// <remarks>
    /// <see langword="null"/> – улучшение не идёт.
    /// </remarks>
    public DateTime? FinishDate { get; set; }

    /// <summary>
    /// Длительность текущего улучшения.
    /// </summary>
    /// <value>Секунды.</value>
    /// <remarks>
    /// <see langword="null"/> – улучшение не идёт.
    /// </remarks>
    public int? UpgradeSeconds { get; set; }

    /// <summary>
    /// Производства, запущенные в этом домике.
    /// </summary>
    /// <remarks>
    /// Пустой массив, если ничего не производится.
    /// </remarks>
    public ManufactureDto[] Manufactures { get; set; }
}
