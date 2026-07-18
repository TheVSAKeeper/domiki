namespace Domiki.Web.Economy.Models;

/// <summary>
/// Лот на ярмарке – продавец отдаёт один ресурс, хочет взамен другой.
/// </summary>
/// <remarks>
/// Собирается в <see cref="Economy.MarketManager.GetMarket"/> и отдаётся на клиент как <see cref="Dto.TradeLotDto"/>.
/// </remarks>
public class TradeLot
{
    /// <summary>
    /// Идентификатор лота.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Игрок-продавец.
    /// </summary>
    public int SellerId { get; set; }

    /// <summary>
    /// Название деревни продавца.
    /// </summary>
    public string? SellerVillageName { get; set; }

    /// <summary>
    /// Иконка герба деревни продавца.
    /// </summary>
    public int SellerCrestIcon { get; set; }

    /// <summary>
    /// Цвет герба деревни продавца.
    /// </summary>
    public int SellerCrestColor { get; set; }

    /// <summary>
    /// Тип ресурса, который продавец отдаёт – ссылка на справочник <see cref="Reference.Models.ResourceType.Id"/>.
    /// </summary>
    public int GiveResourceTypeId { get; set; }

    /// <summary>
    /// Количество отдаваемого ресурса, которое продавец обязался передать по лоту.
    /// </summary>
    /// <remarks>
    /// Уже списано со склада продавца и удерживается в эскроу лота до принятия или истечения.
    /// </remarks>
    public int GiveValue { get; set; }

    /// <summary>
    /// Тип ресурса, который продавец хочет получить – ссылка на справочник <see cref="Reference.Models.ResourceType.Id"/>.
    /// </summary>
    public int WantResourceTypeId { get; set; }

    /// <summary>
    /// Количество желаемого ресурса.
    /// </summary>
    public int WantValue { get; set; }

    /// <summary>
    /// Монеты, удержанные комиссией при выставлении лота.
    /// </summary>
    /// <remarks>
    /// Не возвращаются продавцу даже при отмене лота (см. <see cref="Economy.MarketManager.CancelLot"/>).
    /// </remarks>
    public int CommissionCoins { get; set; }

    /// <summary>
    /// Момент истечения лота.
    /// </summary>
    /// <value>Момент в UTC.</value>
    /// <remarks>
    /// После истечения отдаваемый ресурс возвращается продавцу (см. <see cref="Economy.MarketManager.FinishTradeLot"/>).
    /// </remarks>
    public DateTime ExpireDate { get; set; }
}

/// <summary>
/// Состояние ярмарки игрока – доска чужих лотов, свои лоты и текущие условия торговли.
/// </summary>
/// <remarks>
/// Собирается в <see cref="Economy.MarketManager.GetMarket"/> и отдаётся на клиент как <see cref="Dto.MarketStateDto"/>.
/// </remarks>
public class MarketState
{
    /// <summary>
    /// Лоты других игроков, доступные для принятия.
    /// </summary>
    /// <remarks>
    /// Принять чужой лот можно через <see cref="Economy.MarketManager.AcceptLot"/>.
    /// </remarks>
    public TradeLot[] Lots { get; set; } = [];

    /// <summary>
    /// Собственные выставленные лоты игрока.
    /// </summary>
    /// <remarks>
    /// Отменить свой лот можно через <see cref="Economy.MarketManager.CancelLot"/>.
    /// </remarks>
    public TradeLot[] MyLots { get; set; } = [];

    /// <summary>
    /// Уровень Торгового двора игрока.
    /// </summary>
    /// <remarks>
    /// Определяет <see cref="CommissionRate"/> и <see cref="MaxLots"/>.
    /// </remarks>
    public int BuildingLevel { get; set; }

    /// <summary>
    /// Текущая ставка комиссии при выставлении лота.
    /// </summary>
    /// <value>Доля от рыночной стоимости отдаваемого ресурса, не проценты.</value>
    public double CommissionRate { get; set; }

    /// <summary>
    /// Минимальная комиссия в монетах, ниже которой она не опускается даже для дешёвых лотов.
    /// </summary>
    public int CommissionMin { get; set; }

    /// <summary>
    /// Ставка комиссии на следующем уровне Торгового двора.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – <see cref="BuildingLevel"/> уже максимальный.
    /// </remarks>
    public double? NextCommissionRate { get; set; }

    /// <summary>
    /// Сколько лотов игрок может держать выставленными одновременно.
    /// </summary>
    /// <remarks>
    /// Вычисляется как <c>MaxLots = BuildingLevel + 1</c>.
    /// </remarks>
    public int MaxLots { get; set; }
}
