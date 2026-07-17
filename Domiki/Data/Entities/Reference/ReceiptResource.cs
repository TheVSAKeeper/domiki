using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Ресурс рецепта: входной (обязательный или опциональный) или выходной.
/// </summary>
[Table("ReceiptResources")]
public class ReceiptResource
{
    /// <summary>
    /// Часть составного ключа – рецепт.
    /// </summary>
    [Key]
    [Column(Order = 1)]
    public int ReceiptId { get; set; }

    /// <summary>
    /// Часть составного ключа – тип ресурса.
    /// </summary>
    [Key]
    [Column(Order = 2)]
    public int ResourceTypeId { get; set; }

    /// <summary>
    /// Часть составного ключа – <see langword="true"/> для сырья, которое рецепт потребляет, <see langword="false"/> – для того, что рецепт производит.
    /// </summary>
    [Key]
    [Column(Order = 3)]
    public bool IsInput { get; set; }

    /// <summary>
    /// Только для входных ресурсов: если <see langword="true"/> – ингредиент необязателен.
    /// </summary>
    /// <remarks>
    /// При использовании даёт бонус выхода <see cref="Receipt.OutputBonusPercent"/>.
    /// </remarks>
    public bool IsOptional { get; set; }

    /// <summary>
    /// Количество ресурса.
    /// </summary>
    /// <remarks>
    /// Для входа – сколько списывается при запуске производства, для выхода – сколько выдаётся при завершении
    /// (до модификатора <see cref="Manufacture.OutputPercent"/>).
    /// </remarks>
    public int Value { get; set; }

    /// <summary>
    /// Рецепт, к которому относится ресурс.
    /// </summary>
    public Receipt Receipt { get; set; }

    /// <summary>
    /// Тип ресурса.
    /// </summary>
    public ResourceType ResourceType { get; set; }
}
