namespace Domiki.Web.Core.Models;

/// <summary>
/// Экземпляр домика игрока – тип, уровень и идущее улучшение.
/// </summary>
/// <remarks>
/// Собирается в <see cref="DomikManager.GetDomiks"/> и отдаётся на клиент как <see cref="Dto.DomikDto"/>.
/// </remarks>
public class Domik
{
    /// <summary>
    /// Номер домика, уникальный в пределах игрока.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Тип постройки.
    /// </summary>
    public required DomikType Type { get; set; }

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
    public Manufacture[] Manufactures { get; set; } = [];
}
