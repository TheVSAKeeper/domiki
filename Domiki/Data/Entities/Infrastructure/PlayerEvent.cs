using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Запись журнала событий игрока – источник витрины «Пока вас не было» и последней активности.
/// </summary>
/// <remarks>
/// Непрочитанные события отдаются один раз и помечаются прочитанными, старые прочитанные строки удаляются сверх последних <c>50</c>
/// (см. <see cref="Infrastructure.PlayerEventManager.TakeRecap"/>).
/// </remarks>
[Index(nameof(PlayerId), nameof(Date))]
public class PlayerEvent
{
    /// <summary>
    /// Идентификатор события.
    /// </summary>
    /// <remarks>
    /// <c>long</c> – события пишутся часто (в т.ч. слияние повторов производства), <c>int</c> мог бы переполниться.
    /// </remarks>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// Игрок, которому принадлежит событие.
    /// </summary>
    public int PlayerId { get; set; }

    /// <summary>
    /// Вид события.
    /// </summary>
    public PlayerEventType Type { get; set; }

    /// <summary>
    /// Момент события.
    /// </summary>
    /// <remarks>
    /// Для <see cref="PlayerEventType.ManufactureFinished"/> обновляется при слиянии повторных завершений в одну запись.
    /// </remarks>
    public DateTime Date { get; set; }

    /// <summary>
    /// Полезная нагрузка события в JSON – формат зависит от <see cref="Type"/>.
    /// </summary>
    /// <remarks>
    /// См. модели-конверты в <see cref="Infrastructure.PlayerEventManager"/>/<see cref="Infrastructure.Models.RecapModel"/>.
    /// </remarks>
    [Required(AllowEmptyStrings = false)]
    public required string Data { get; set; }

    /// <summary>
    /// Событие уже было отдано игроку через <see cref="Infrastructure.PlayerEventManager.TakeRecap"/>.
    /// </summary>
    /// <remarks>
    /// Непрочитанные события попадают в очередную витрину «Пока вас не было».
    /// </remarks>
    public bool Read { get; set; }
}
