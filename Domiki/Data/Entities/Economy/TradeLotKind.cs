namespace Domiki.Web.Data.Entities;

/// <summary>
/// Направление лота на Торговом дворе: намерение игрока, выставившего лот.
/// </summary>
/// <remarks>
/// На механику обмена не влияет (эскроу всегда на стороне <see cref="TradeLot.GiveResourceTypeId"/>,
/// приём симметричен), разводит лишь подачу: заявка на покупку физически неотличима от продажи
/// валюты, маркер даёт клиенту прочитать её как «игрок хочет товар».
/// </remarks>
public enum TradeLotKind
{
    /// <summary>
    /// Значение не задано.
    /// </summary>
    None = 0,

    /// <summary>
    /// Продажа: продавец отдаёт <see cref="TradeLot.GiveResourceTypeId"/>, хочет получить <see cref="TradeLot.WantResourceTypeId"/>.
    /// </summary>
    Sell = 1,

    /// <summary>
    /// Заявка на покупку: покупатель эскроуит оплату (золото) в <see cref="TradeLot.GiveResourceTypeId"/>, хочет получить товар <see cref="TradeLot.WantResourceTypeId"/>.
    /// </summary>
    Buy = 2,
}
