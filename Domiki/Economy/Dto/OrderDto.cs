namespace Domiki.Web.Economy.Dto;

/// <summary>
/// Заказ соседа на доске заказов – корзина требуемых ресурсов и награда за сдачу.
/// </summary>
public sealed record OrderDto
{
    /// <summary>
    /// Идентификатор заказа.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Сосед, разместивший заказ.
    /// </summary>
    public required int NeighborId { get; init; }

    /// <summary>
    /// Отображаемое имя соседа.
    /// </summary>
    public required string NeighborName { get; init; }

    /// <summary>
    /// Техническое имя соседа, используется как ключ на клиенте.
    /// </summary>
    public required string NeighborLogicName { get; init; }

    /// <summary>
    /// Момент истечения заказа.
    /// </summary>
    /// <value>Момент в UTC.</value>
    /// <remarks>
    /// После истечения заказ протухает и заменяется новым (см. <see cref="Economy.OrderManager.FinishOrder"/>).
    /// </remarks>
    public required DateTime ExpireDate { get; init; }

    /// <summary>
    /// Корзина ресурсов, которую нужно сдать целиком, чтобы выполнить заказ.
    /// </summary>
    /// <remarks>
    /// Списывается со склада игрока целиком в <see cref="Economy.OrderManager.CompleteOrder"/>.
    /// </remarks>
    public required OrderResourceDto[] Required { get; init; }

    /// <summary>
    /// Монеты, начисляемые за выполнение заказа.
    /// </summary>
    public required int RewardCoins { get; init; }

    /// <summary>
    /// Золото, начисляемое за выполнение заказа.
    /// </summary>
    public required int RewardGold { get; init; }

    /// <summary>
    /// Очки репутации у соседа, начисляемые за выполнение заказа.
    /// </summary>
    /// <remarks>
    /// Прибавляются к <see cref="NeighborReputationDto.Points"/> у этого соседа (см. <see cref="Economy.OrderManager.CompleteOrder"/>).
    /// </remarks>
    public required int RewardReputation { get; init; }
}

/// <summary>
/// Один ресурс из корзины требований заказа.
/// </summary>
public sealed record OrderResourceDto
{
    /// <summary>
    /// Тип требуемого ресурса – ссылка на <see cref="Reference.Dto.ResourceTypeDto.Id"/>.
    /// </summary>
    public required int ResourceTypeId { get; init; }

    /// <summary>
    /// Требуемое количество.
    /// </summary>
    public required int Value { get; init; }
}
