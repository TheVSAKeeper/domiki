using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Справочник рецептов производства: что домик потребляет и что выдаёт.
/// </summary>
[Table("Receipts")]
public class Receipt
{
    /// <summary>
    /// Идентификатор рецепта.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Отображаемое название рецепта.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Технический код рецепта – по нему код находит конкретный рецепт по смыслу, а не по <see cref="Id"/>.
    /// </summary>
    public string LogicName { get; set; }

    /// <summary>
    /// Базовая длительность производства в секундах, до бонусов трудяг и стартовых модификаторов.
    /// </summary>
    public int DurationSeconds { get; set; }

    /// <summary>
    /// Бонус к проценту выхода ресурсов, если производство использует опциональный ингредиент рецепта.
    /// </summary>
    /// <value>Проценты.</value>
    /// <remarks>
    /// См. <see cref="ReceiptResource.IsOptional"/>.
    /// </remarks>
    public int OutputBonusPercent { get; set; }

    /// <summary>
    /// Сколько трудяг требуется, чтобы запустить производство по этому рецепту.
    /// </summary>
    public int PlodderCount { get; set; }

    /// <summary>
    /// Входные, опциональные и выходные ресурсы рецепта.
    /// </summary>
    /// <remarks>
    /// Различаются флагами <see cref="ReceiptResource.IsInput"/> и <see cref="ReceiptResource.IsOptional"/>.
    /// </remarks>
    public ICollection<ReceiptResource> Resources { get; set; }
}
