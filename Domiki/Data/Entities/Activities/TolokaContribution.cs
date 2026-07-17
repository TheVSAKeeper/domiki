using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Вклад одного игрока в одну инстанцию толоки.
/// </summary>
/// <remarks>
/// Строки не удаляются после завершения – по ним считается активность баффа и будущий сезонный рейтинг «Герой толоки».
/// </remarks>
[Table("TolokaContributions")]
public class TolokaContribution
{
    /// <summary>
    /// Часть составного ключа – инстанция толоки, в которую внесён вклад.
    /// </summary>
    [Key]
    [Column(Order = 1)]
    public int TolokaId { get; set; }

    /// <summary>
    /// Часть составного ключа – игрок, внёсший вклад.
    /// </summary>
    [Key]
    [Column(Order = 2)]
    public int PlayerId { get; set; }

    /// <summary>
    /// Сколько ресурса внёс игрок в эту инстанцию толоки суммарно.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Навигационное свойство к инстанции толоки.
    /// </summary>
    public Toloka Toloka { get; set; }

    /// <summary>
    /// Навигационное свойство к игроку-участнику.
    /// </summary>
    public Player Player { get; set; }
}
