using Domiki.Web.Core;
using Domiki.Web.Economy.Dto;
using Domiki.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Domiki.Web.Economy;

public class EconomyController : GameControllerBase
{
    private readonly OrderManager _orderManager;
    private readonly MarketManager _marketManager;

    public EconomyController(DomikManager domikManager, OrderManager orderManager, MarketManager marketManager)
        : base(domikManager)
    {
        _orderManager = orderManager;
        _marketManager = marketManager;
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

    [HttpGet]
    [Route("/Domiki/GetReputation")]
    public NeighborReputationDto[] GetReputation()
    {
        var playerId = GetPlayerId();

        return _orderManager.GetReputation(playerId).Select(x => x.ToDto()).ToArray();
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
    public void PostLot([FromQuery] int giveResourceTypeId, [FromQuery] int giveValue, [FromQuery] int wantResourceTypeId, [FromQuery] int wantValue)
    {
        var playerId = GetPlayerId();
        _marketManager.PostLot(playerId, giveResourceTypeId, giveValue, wantResourceTypeId, wantValue, DateTimeHelper.GetNowDate());
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
}
