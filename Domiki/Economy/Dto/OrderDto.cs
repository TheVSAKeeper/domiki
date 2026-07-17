namespace Domiki.Web.Economy.Dto;

/// <summary>
/// Заказ соседа на доске заказов – корзина требуемых ресурсов и награда за сдачу.
/// </summary>
public class OrderDto
{
    /// <summary>
    /// Идентификатор заказа.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Сосед, разместивший заказ.
    /// </summary>
    public int NeighborId { get; set; }

    /// <summary>
    /// Отображаемое имя соседа.
    /// </summary>
    public string NeighborName { get; set; }

    /// <summary>
    /// Техническое имя соседа, используется как ключ на клиенте.
    /// </summary>
    public string NeighborLogicName { get; set; }

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
    public OrderResourceDto[] Required { get; set; }

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
    /// Прибавляются к <see cref="NeighborReputationDto.Points"/> у этого соседа (см. <see cref="Economy.OrderManager.CompleteOrder"/>).
    /// </remarks>
    public int RewardReputation { get; set; }
}

/// <summary>
/// Один ресурс из корзины требований заказа.
/// </summary>
public class OrderResourceDto
{
    /// <summary>
    /// Тип требуемого ресурса – ссылка на <see cref="Reference.Dto.ResourceTypeDto.Id"/>.
    /// </summary>
    public int ResourceTypeId { get; set; }

    /// <summary>
    /// Требуемое количество.
    /// </summary>
    public int Value { get; set; }
}
