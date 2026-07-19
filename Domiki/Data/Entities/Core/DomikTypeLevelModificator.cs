using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Модификатор (например, добавка вместимости трудяг), действующий на этом уровне домика.
/// </summary>
[PrimaryKey(nameof(DomikTypeLevelDomikTypeId), nameof(DomikTypeLevelValue), nameof(ModificatorTypeId))]
public class DomikTypeLevelModificator
{
    /// <summary>
    /// Часть составного ключа – тип домика (первая половина составного FK на <see cref="DomikTypeLevel"/>).
    /// </summary>
    [Column(Order = 1)]
    public int DomikTypeLevelDomikTypeId { get; set; }

    /// <summary>
    /// Часть составного ключа – номер уровня домика (вторая половина составного FK на <see cref="DomikTypeLevel"/>).
    /// </summary>
    [Column(Order = 2)]
    public int DomikTypeLevelValue { get; set; }

    /// <summary>
    /// Часть составного ключа – вид модификатора.
    /// </summary>
    [Column(Order = 3)]
    public int ModificatorTypeId { get; set; }

    /// <summary>
    /// Величина модификатора, применяемая на этом уровне домика.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Уровень домика, к которому привязан модификатор.
    /// </summary>
    public DomikTypeLevel DomikTypeLevel { get; set; } = null!;

    /// <summary>
    /// Вид модификатора.
    /// </summary>
    public ModificatorType ModificatorType { get; set; } = null!;
}
