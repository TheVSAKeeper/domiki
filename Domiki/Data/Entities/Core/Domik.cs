using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Экземпляр домика игрока: тип постройки, текущий уровень и состояние идущего улучшения.
/// </summary>
[Table("Domiks")]
[PrimaryKey(nameof(PlayerId), nameof(Id))]
public class Domik
{
    /// <summary>
    /// Часть составного ключа – игрок-владелец домика.
    /// </summary>
    [Column(Order = 1)]
    public int PlayerId { get; set; }

    /// <summary>
    /// Часть составного ключа – номер домика, уникальный в пределах игрока (не глобально).
    /// </summary>
    [Column(Order = 2)]
    public int Id { get; set; }

    /// <summary>
    /// Тип постройки – ссылка на <see cref="DomikType"/>.
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
    /// Сколько секунд длится текущее улучшение.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – улучшение не идёт.
    /// </remarks>
    public double? UpgradeSeconds { get; set; }

    /// <summary>
    /// Момент запуска текущего улучшения.
    /// </summary>
    /// <remarks>
    /// Вместе с <see cref="UpgradeSeconds"/> задаёт срок, когда планировщик <see cref="Core.Scheduling.Calculator"/> должен завершить улучшение
    /// (см. <see cref="Core.DomikManager.FinishDomik"/>).
    /// </remarks>
    public DateTime? UpgradeCalculateDate { get; set; }

    /// <summary>
    /// Производства, запущенные в этом домике.
    /// </summary>
    public ICollection<Manufacture> Manufactures { get; set; } = new List<Manufacture>();
}
