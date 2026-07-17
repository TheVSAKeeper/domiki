using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Рецепт производства, открывающийся на этом уровне домика.
/// </summary>
[Table("DomikTypeLevelReceipts")]
public class DomikTypeLevelReceipt
{
    /// <summary>
    /// Часть составного ключа – тип домика (первая половина составного FK на <see cref="DomikTypeLevel"/>).
    /// </summary>
    [Key]
    [Column(Order = 1)]
    public int DomikTypeLevelDomikTypeId { get; set; }

    /// <summary>
    /// Часть составного ключа – номер уровня домика (вторая половина составного FK на <see cref="DomikTypeLevel"/>).
    /// </summary>
    [Key]
    [Column(Order = 2)]
    public int DomikTypeLevelValue { get; set; }

    /// <summary>
    /// Часть составного ключа – рецепт, который открывается на этом уровне.
    /// </summary>
    [Key]
    [Column(Order = 3)]
    public int ReceiptId { get; set; }

    /// <summary>
    /// Уровень домика, на котором открывается рецепт.
    /// </summary>
    public DomikTypeLevel DomikTypeLevel { get; set; }

    /// <summary>
    /// Рецепт, открывающийся на этом уровне.
    /// </summary>
    public Receipt Receipt { get; set; }
}
