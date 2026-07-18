using Domiki.Web.Data.Entities;

namespace Domiki.Web.Economy.Dto;

/// <summary>
/// Лот на ярмарке – продавец отдаёт один ресурс, хочет взамен другой.
/// </summary>
public sealed record TradeLotDto
{
    /// <summary>
    /// Идентификатор лота.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Игрок-продавец.
    /// </summary>
    public required int SellerId { get; init; }

    /// <summary>
    /// Направление лота: продажа или заявка на покупку.
    /// </summary>
    /// <remarks>
    /// Для <see cref="Data.Entities.TradeLotKind.Buy"/> <see cref="GiveResourceTypeId"/> – оплата (золото, эскроуится),
    /// <see cref="WantResourceTypeId"/> – покупаемый товар.
    /// </remarks>
    /// <seealso cref="Data.Entities.TradeLotKind"/>
    public required TradeLotKind Kind { get; init; }

    /// <summary>
    /// Название деревни продавца.
    /// </summary>
    public string? SellerVillageName { get; init; }

    /// <summary>
    /// Иконка герба деревни продавца.
    /// </summary>
    public required int SellerCrestIcon { get; init; }

    /// <summary>
    /// Цвет герба деревни продавца.
    /// </summary>
    public required int SellerCrestColor { get; init; }

    /// <summary>
    /// Тип ресурса, который продавец отдаёт – ссылка на <see cref="Reference.Dto.ResourceTypeDto.Id"/>.
    /// </summary>
    public required int GiveResourceTypeId { get; init; }

    /// <summary>
    /// Количество отдаваемого ресурса, которое продавец обязался передать по лоту.
    /// </summary>
    /// <remarks>
    /// Уже списано со склада продавца и удерживается в эскроу лота до принятия или истечения.
    /// </remarks>
    public required int GiveValue { get; init; }

    /// <summary>
    /// Тип ресурса, который продавец хочет получить – ссылка на <see cref="Reference.Dto.ResourceTypeDto.Id"/>.
    /// </summary>
    public required int WantResourceTypeId { get; init; }

    /// <summary>
    /// Количество желаемого ресурса.
    /// </summary>
    public required int WantValue { get; init; }

    /// <summary>
    /// Монеты, удержанные комиссией при выставлении лота.
    /// </summary>
    /// <remarks>
    /// Не возвращаются продавцу даже при отмене лота (см. <see cref="Economy.MarketManager.CancelLot"/>).
    /// </remarks>
    public required int CommissionCoins { get; init; }

    /// <summary>
    /// Момент истечения лота.
    /// </summary>
    /// <value>Момент в UTC.</value>
    /// <remarks>
    /// После истечения отдаваемый ресурс возвращается продавцу (см. <see cref="Economy.MarketManager.FinishTradeLot"/>).
    /// </remarks>
    public required DateTime ExpireDate { get; init; }
}

/// <summary>
/// Состояние ярмарки игрока – доска чужих лотов, свои лоты и текущие условия торговли.
/// </summary>
public sealed record MarketStateDto
{
    /// <summary>
    /// Лоты других игроков, доступные для принятия.
    /// </summary>
    /// <remarks>
    /// Принять чужой лот можно через <see cref="Economy.MarketManager.AcceptLot"/>.
    /// </remarks>
    public required TradeLotDto[] Lots { get; init; }

    /// <summary>
    /// Собственные выставленные лоты игрока.
    /// </summary>
    /// <remarks>
    /// Отменить свой лот можно через <see cref="Economy.MarketManager.CancelLot"/>.
    /// </remarks>
    public required TradeLotDto[] MyLots { get; init; }

    /// <summary>
    /// Уровень Торгового двора игрока.
    /// </summary>
    /// <remarks>
    /// Определяет <see cref="CommissionRate"/> и <see cref="MaxLots"/>.
    /// </remarks>
    public required int BuildingLevel { get; init; }

    /// <summary>
    /// Текущая ставка комиссии при выставлении лота.
    /// </summary>
    /// <value>Доля от рыночной стоимости отдаваемого ресурса, не проценты.</value>
    public required double CommissionRate { get; init; }

    /// <summary>
    /// Минимальная комиссия в монетах, ниже которой она не опускается даже для дешёвых лотов.
    /// </summary>
    public required int CommissionMin { get; init; }

    /// <summary>
    /// Ставка комиссии на следующем уровне Торгового двора.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – <see cref="BuildingLevel"/> уже максимальный.
    /// </remarks>
    public double? NextCommissionRate { get; init; }

    /// <summary>
    /// Сколько лотов игрок может держать выставленными одновременно.
    /// </summary>
    /// <remarks>
    /// Вычисляется как <c>MaxLots = BuildingLevel + 1</c>.
    /// </remarks>
    public required int MaxLots { get; init; }
}
