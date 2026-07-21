using Domiki.Web.Core;
using Domiki.Web.Data.Entities;
using Domiki.Web.Economy.Dto;
using Domiki.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Domiki.Web.Economy;

public class EconomyController : GameControllerBase
{
    private readonly OrderManager _orderManager;
    private readonly MarketManager _marketManager;
    private readonly ErrandManager _errandManager;
    private readonly ConvoyManager _convoyManager;

    public EconomyController(DomikManager domikManager, OrderManager orderManager, MarketManager marketManager, ErrandManager errandManager, ConvoyManager convoyManager)
        : base(domikManager)
    {
        _orderManager = orderManager;
        _marketManager = marketManager;
        _errandManager = errandManager;
        _convoyManager = convoyManager;
    }

    [HttpGet]
    [Route("/Domiki/GetOrders")]
    public OrderDto[] GetOrders()
    {
        var playerId = GetPlayerId();

        return _orderManager.GetOrders(playerId).Select(x => x.ToDto()).ToArray();
    }

    [HttpPost]
    [Route("/Domiki/CompleteOrder/{orderId}")]
    public void CompleteOrder(int orderId)
    {
        var playerId = GetPlayerId();
        _orderManager.CompleteOrder(playerId, orderId);
    }

    /// <summary>
    /// Уступает заказ соседу без выполнения – заказ снимается с доски, слот освобождается на обычную задержку пополнения.
    /// </summary>
    /// <param name="orderId">Идентификатор заказа.</param>
    [HttpPost]
    [Route("/Domiki/CancelOrder/{orderId}")]
    public void CancelOrder(int orderId)
    {
        var playerId = GetPlayerId();
        _orderManager.CancelOrder(playerId, orderId);
    }

    [HttpGet]
    [Route("/Domiki/GetReputation")]
    public NeighborReputationDto[] GetReputation()
    {
        var playerId = GetPlayerId();

        return _orderManager.GetReputation(playerId).Select(x => x.ToDto()).ToArray();
    }

    /// <summary>
    /// Назначает (или снимает) дружбу игрока с соседом.
    /// </summary>
    /// <param name="request">Сосед, с которым назначается дружба; <see langword="null"/> в <see cref="SetFriendNeighborDto.NeighborId"/> снимает дружбу.</param>
    [HttpPost]
    [Route("/Domiki/SetFriendNeighbor")]
    public void SetFriendNeighbor([FromBody] SetFriendNeighborDto request)
    {
        var playerId = GetPlayerId();
        _orderManager.SetFriendNeighbor(playerId, request.NeighborId);
    }

    [HttpGet]
    [Route("/Domiki/GetMarket")]
    public MarketStateDto? GetMarket()
    {
        var playerId = GetPlayerId();

        return _marketManager.GetMarket(playerId)?.ToDto();
    }

    [HttpPost]
    [Route("/Domiki/PostLot")]
    public void PostLot([FromQuery] int giveResourceTypeId, [FromQuery] int giveValue, [FromQuery] int wantResourceTypeId, [FromQuery] int wantValue, [FromQuery] TradeLotKind kind = TradeLotKind.Sell)
    {
        var playerId = GetPlayerId();
        _marketManager.PostLot(playerId, kind, giveResourceTypeId, giveValue, wantResourceTypeId, wantValue, DateTimeHelper.GetNowDate());
    }

    [HttpPost]
    [Route("/Domiki/AcceptLot/{lotId}")]
    public void AcceptLot(int lotId)
    {
        var playerId = GetPlayerId();
        _marketManager.AcceptLot(playerId, lotId, DateTimeHelper.GetNowDate());
    }

    [HttpPost]
    [Route("/Domiki/CancelLot/{lotId}")]
    public void CancelLot(int lotId)
    {
        var playerId = GetPlayerId();
        _marketManager.CancelLot(playerId, lotId, DateTimeHelper.GetNowDate());
    }

    [HttpPost]
    [Route("/Domiki/BuyFromConvoy")]
    public void BuyFromConvoy([FromBody] BuyFromConvoyDto request)
    {
        var playerId = GetPlayerId();
        _convoyManager.Buy(playerId, request.NeighborId, request.ResourceTypeId, request.Count, DateTimeHelper.GetNowDate());
    }

    [HttpPost]
    [Route("/Domiki/AcceptErrand/{errandId}")]
    public void AcceptErrand(int errandId, [FromQuery] int clueId, [FromQuery] int[] workerIds)
    {
        var playerId = GetPlayerId();
        _errandManager.Accept(playerId, errandId, clueId, workerIds);
    }

    [HttpPost]
    [Route("/Domiki/CancelErrand/{errandId}")]
    public void CancelErrand(int errandId)
    {
        var playerId = GetPlayerId();
        _errandManager.Cancel(playerId, errandId);
    }
}
