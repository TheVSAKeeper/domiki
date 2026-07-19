using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// След визита гостя в деревню хозяина за один UTC-день, с необязательной записью в книге гостей.
/// </summary>
/// <remarks>
/// Одна строка на пару гость-хозяин в день: повторные визиты не плодят строк,
/// счётчик визитов за сезон выводится агрегатом по <see cref="Day"/>.
/// </remarks>
[PrimaryKey(nameof(HostPlayerId), nameof(GuestPlayerId), nameof(Day))]
public class GuestbookEntry
{
    /// <summary>
    /// Часть составного ключа – хозяин деревни, которую посетили.
    /// </summary>
    [Column(Order = 1)]
    public int HostPlayerId { get; set; }

    /// <summary>
    /// Часть составного ключа – гость.
    /// </summary>
    [Column(Order = 2)]
    public int GuestPlayerId { get; set; }

    /// <summary>
    /// Часть составного ключа – UTC-день визита.
    /// </summary>
    [Column(Order = 3)]
    public DateOnly Day { get; set; }

    /// <summary>
    /// Выбранная фраза книги гостей; null – визит без записи.
    /// </summary>
    public int? PhraseId { get; set; }

    /// <summary>
    /// Момент последнего действия гостя (визита или записи).
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Хозяин деревни.
    /// </summary>
    public Player HostPlayer { get; set; } = null!;

    /// <summary>
    /// Гость.
    /// </summary>
    public Player GuestPlayer { get; set; } = null!;
}
