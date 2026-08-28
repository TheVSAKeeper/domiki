using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Запись журнала событий игрока – источник витрины «Пока вас не было» и последней активности.
/// </summary>
/// <remarks>
/// Недоставленным считается всё, что больше курсора <see cref="Player.LastDeliveredEventId"/>; такие события отдаются один раз,
/// после чего курсор сдвигается (см. <see cref="Infrastructure.PlayerEventManager.TakeRecap"/>). Хранятся записи в течение
/// <see cref="Infrastructure.PlayerEventManager.Retention"/>, дальше их удаляет
/// <see cref="Infrastructure.PlayerEventCleanupService"/>. Индексы: <c>(PlayerId, Id)</c> – курсор и страницы,
/// <c>(PlayerId, Type, Date)</c> – окно слияния, <c>(Date)</c> – чистка по сроку.
/// </remarks>
[Index(nameof(PlayerId), nameof(Id))]
[Index(nameof(PlayerId), nameof(Type), nameof(Date))]
[Index(nameof(Date))]
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
    /// При слиянии однотипных событий не обновляется: запись описывает начало своего часового окна, а не последний вошедший в него
    /// цикл, и сдвиг даты переставлял бы её в ленте.
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
}
