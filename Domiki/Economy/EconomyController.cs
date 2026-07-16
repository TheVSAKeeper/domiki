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
    public Response<OrderDto[]> GetOrders()
    {
        var playerId = GetPlayerId();

        var content = _orderManager.GetOrders(playerId).Select(x => x.ToDto()).ToArray();
        return new(content);
    }

    [HttpPost]
    [Route("/Domiki/CompleteOrder/{orderId}")]
    public Response CompleteOrder(int orderId)
    {
        var playerId = GetPlayerId();
        _orderManager.CompleteOrder(playerId, orderId);
        return new()
            { Type = ResponseType.Success };
    }

    [HttpGet]
    [Route("/Domiki/GetReputation")]
    public Response<NeighborReputationDto[]> GetReputation()
    {
        var playerId = GetPlayerId();

        var content = _orderManager.GetReputation(playerId).Select(x => x.ToDto()).ToArray();
        return new(content);
    }

    [HttpGet]
    [Route("/Domiki/GetMarket")]
    public Response<MarketStateDto> GetMarket()
    {
        var playerId = GetPlayerId();

        var content = _marketManager.GetMarket(playerId)?.ToDto();
        return new(content);
    }

    [HttpPost]
    [Route("/Domiki/PostLot")]
    public Response PostLot([FromQuery] int giveResourceTypeId, [FromQuery] int giveValue, [FromQuery] int wantResourceTypeId, [FromQuery] int wantValue)
    {
        var playerId = GetPlayerId();
        _marketManager.PostLot(playerId, giveResourceTypeId, giveValue, wantResourceTypeId, wantValue, DateTimeHelper.GetNowDate());
        return new()
            { Type = ResponseType.Success };
    }

    [HttpPost]
    [Route("/Domiki/AcceptLot/{lotId}")]
    public Response AcceptLot(int lotId)
    {
        var playerId = GetPlayerId();
        _marketManager.AcceptLot(playerId, lotId, DateTimeHelper.GetNowDate());
        return new()
            { Type = ResponseType.Success };
    }

    [HttpPost]
    [Route("/Domiki/CancelLot/{lotId}")]
    public Response CancelLot(int lotId)
    {
        var playerId = GetPlayerId();
        _marketManager.CancelLot(playerId, lotId, DateTimeHelper.GetNowDate());
        return new()
            { Type = ResponseType.Success };
    }
}
