using Domiki.Web.Core;
using Domiki.Web.Infrastructure.Dto;
using Microsoft.AspNetCore.Mvc;

namespace Domiki.Web.Infrastructure;

public class PushController : GameControllerBase
{
    private readonly PushManager _pushManager;
    private readonly PushSender _pushSender;

    public PushController(DomikManager domikManager, PushManager pushManager, PushSender pushSender)
        : base(domikManager)
    {
        _pushManager = pushManager;
        _pushSender = pushSender;
    }

    [HttpGet]
    [Route("/Push/PublicKey")]
    public Response<string> PublicKey()
    {
        return new(_pushSender.PublicKey);
    }

    [HttpPost]
    [Route("/Push/Subscribe")]
    public Response Subscribe([FromBody] PushSubscribeDto request)
    {
        var playerId = GetPlayerId();
        _pushManager.Subscribe(playerId, request?.Endpoint, request?.P256dh, request?.Auth);
        return new()
            { Type = ResponseType.Success };
    }

    [HttpPost]
    [Route("/Push/Unsubscribe")]
    public Response Unsubscribe([FromBody] PushUnsubscribeDto request)
    {
        var playerId = GetPlayerId();
        _pushManager.Unsubscribe(playerId, request?.Endpoint);
        return new()
            { Type = ResponseType.Success };
    }
}
