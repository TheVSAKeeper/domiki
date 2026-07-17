using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Заказ соседа на доске игрока: что сдать и какая награда.
/// </summary>
[Table("Orders")]
public class Order
{
    /// <summary>
    /// Идентификатор заказа.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Игрок, на чьей доске заказов лежит заказ.
    /// </summary>
    public int PlayerId { get; set; }

    /// <summary>
    /// Сосед, разместивший заказ.
    /// </summary>
    public int NeighborId { get; set; }

    /// <summary>
    /// Момент создания заказа.
    /// </summary>
    public DateTime CreateDate { get; set; }

    /// <summary>
    /// Момент, когда заказ протухает, если не выполнен.
    /// </summary>
    /// <remarks>
    /// Планировщик <see cref="Core.Scheduling.Calculator"/> снимает его с доски и запускает задержку перед пополнением доски
    /// (<see cref="Economy.OrderManager.OrderRefillDelaySeconds"/>).
    /// </remarks>
    public DateTime ExpireDate { get; set; }

    /// <summary>
    /// Монеты, выдаваемые за выполнение заказа (с учётом бонуса толоки).
    /// </summary>
    public int RewardCoins { get; set; }

    /// <summary>
    /// Золото, выдаваемое за выполнение заказа.
    /// </summary>
    public int RewardGold { get; set; }

    /// <summary>
    /// Очки репутации соседа, начисляемые за выполнение заказа.
    /// </summary>
    public int RewardReputation { get; set; }

    /// <summary>
    /// Игрок, которому принадлежит заказ.
    /// </summary>
    public Player Player { get; set; } = null!;

    /// <summary>
    /// Навигационное свойство к соседу из <see cref="NeighborId"/>.
    /// </summary>
    public Neighbor Neighbor { get; set; } = null!;

    /// <summary>
    /// Ресурсы и их количество, которые нужно сдать, чтобы выполнить заказ.
    /// </summary>
    public ICollection<OrderResource> Resources { get; set; } = new List<OrderResource>();
}
