using System.ComponentModel.DataAnnotations;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Справочник соседей деревни – кому игрок выполняет заказы и с кем растёт репутация.
/// </summary>
public class Neighbor
{
    /// <summary>
    /// Идентификатор соседа.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Отображаемое имя соседа.
    /// </summary>
    [MaxLength(100)]
    public required string Name { get; set; }

    /// <summary>
    /// Технический код соседа – по нему код находит конкретного соседа по смыслу, а не по <see cref="Id"/>.
    /// </summary>
    public required string LogicName { get; set; }

    /// <summary>
    /// Тип ресурса, который сосед просит в заказах в первую очередь.
    /// </summary>
    public int PrimaryResourceTypeId { get; set; }

    /// <summary>
    /// Дополнительный тип ресурса, который сосед может просить в заказах.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – у соседа только один вид запроса.
    /// </remarks>
    public int? SecondaryResourceTypeId { get; set; }

    /// <summary>
    /// Обжитость деревни, с которой сосед появляется на доске заказов игрока.
    /// </summary>
    public int UnlockLevel { get; set; }
}
