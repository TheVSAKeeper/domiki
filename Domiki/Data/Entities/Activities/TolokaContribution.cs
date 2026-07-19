using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Вклад одного игрока в одну позицию (ресурс) одной инстанции толоки.
/// </summary>
/// <remarks>
/// Одна строка на пару (толока, игрок, ресурс) – вклад пер-ресурсный, адресован в конкретную позицию корзины.
/// Строки не удаляются после завершения – по ним считается активность баффа и будущий сезонный рейтинг «Герой толоки».
/// </remarks>
[Table("TolokaContributions")]
[PrimaryKey(nameof(TolokaId), nameof(PlayerId), nameof(ResourceTypeId))]
public class TolokaContribution
{
    /// <summary>
    /// Часть составного ключа – инстанция толоки, в которую внесён вклад.
    /// </summary>
    [Column(Order = 1)]
    public int TolokaId { get; set; }

    /// <summary>
    /// Часть составного ключа – игрок, внёсший вклад.
    /// </summary>
    [Column(Order = 2)]
    public int PlayerId { get; set; }

    /// <summary>
    /// Часть составного ключа – ресурс позиции корзины, в которую внесён вклад.
    /// </summary>
    [Column(Order = 3)]
    public int ResourceTypeId { get; set; }

    /// <summary>
    /// Сколько ресурса внёс игрок в эту позицию инстанции толоки суммарно.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Навигационное свойство к инстанции толоки.
    /// </summary>
    public Toloka Toloka { get; set; } = null!;

    /// <summary>
    /// Навигационное свойство к игроку-участнику.
    /// </summary>
    public Player Player { get; set; } = null!;
}
