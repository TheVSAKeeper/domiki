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
    public string PublicKey()
    {
        return _pushSender.PublicKey;
    }

    [HttpPost]
    [Route("/Push/Subscribe")]
    public void Subscribe([FromBody] PushSubscribeDto request)
    {
        var playerId = GetPlayerId();
        _pushManager.Subscribe(playerId, request?.Endpoint, request?.P256dh, request?.Auth);
    }

    [HttpPost]
    [Route("/Push/Unsubscribe")]
    public void Unsubscribe([FromBody] PushUnsubscribeDto request)
    {
        var playerId = GetPlayerId();
        _pushManager.Unsubscribe(playerId, request?.Endpoint);
    }
}
