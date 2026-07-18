using Domiki.Web.Reference.Models;

namespace Domiki.Web.Economy.Models;

/// <summary>
/// Заказ соседа на доске заказов – корзина требуемых ресурсов и награда за сдачу.
/// </summary>
/// <remarks>
/// Собирается в <see cref="Economy.OrderManager.GetOrders"/> и отдаётся на клиент как <see cref="Dto.OrderDto"/>.
/// </remarks>
public class Order
{
    /// <summary>
    /// Идентификатор заказа.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Сосед, разместивший заказ.
    /// </summary>
    public required Neighbor Neighbor { get; set; }

    /// <summary>
    /// Момент создания заказа.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public DateTime CreateDate { get; set; }

    /// <summary>
    /// Момент истечения заказа.
    /// </summary>
    /// <value>Момент в UTC.</value>
    /// <remarks>
    /// После истечения заказ протухает и заменяется новым (см. <see cref="Economy.OrderManager.FinishOrder"/>).
    /// </remarks>
    public DateTime ExpireDate { get; set; }

    /// <summary>
    /// Корзина ресурсов, которую нужно сдать целиком, чтобы выполнить заказ.
    /// </summary>
    /// <remarks>
    /// Списывается со склада игрока целиком в <see cref="Economy.OrderManager.CompleteOrder"/>.
    /// </remarks>
    public Resource[] Resources { get; set; } = [];

    /// <summary>
    /// Монеты, начисляемые за выполнение заказа.
    /// </summary>
    public int RewardCoins { get; set; }

    /// <summary>
    /// Золото, начисляемое за выполнение заказа.
    /// </summary>
    public int RewardGold { get; set; }

    /// <summary>
    /// Очки репутации у соседа, начисляемые за выполнение заказа.
    /// </summary>
    /// <remarks>
    /// Прибавляются к <see cref="NeighborReputation.Points"/> у этого соседа (см. <see cref="Economy.OrderManager.CompleteOrder"/>).
    /// </remarks>
    public int RewardReputation { get; set; }
}
