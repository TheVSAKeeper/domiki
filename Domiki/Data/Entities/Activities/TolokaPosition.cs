using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Позиция корзины сбора конкретной инстанции толоки – цель и собранное по одному ресурсу.
/// </summary>
[Table("TolokaPositions")]
public class TolokaPosition
{
    /// <summary>
    /// Часть составного ключа – инстанция толоки, которой принадлежит позиция.
    /// </summary>
    [Key]
    [Column(Order = 1)]
    public int TolokaId { get; set; }

    /// <summary>
    /// Часть составного ключа – тип ресурса позиции.
    /// </summary>
    [Key]
    [Column(Order = 2)]
    public int ResourceTypeId { get; set; }

    /// <summary>
    /// Целевое количество ресурса, при достижении которого позиция считается набранной.
    /// </summary>
    /// <remarks>
    /// Масштабируется числом участников предыдущей инстанции (см. <see cref="Activities.TolokaManager.Contribute"/>).
    /// </remarks>
    public int Goal { get; set; }

    /// <summary>
    /// Сколько ресурса уже внесено всеми игроками по этой позиции.
    /// </summary>
    public int Collected { get; set; }

    /// <summary>
    /// Навигационное свойство к инстанции толоки.
    /// </summary>
    public Toloka Toloka { get; set; } = null!;
}
