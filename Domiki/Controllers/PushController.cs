using Domiki.Web.Core;
using Domiki.Web.Infrastructure.Dto;
using Domiki.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Domiki.Web.Controllers
{
    [Authorize]
    [ApiController]
    public class PushController : ControllerBase
    {
        private readonly DomikManager _domikManager;
        private readonly PushManager _pushManager;
        private readonly PushSender _pushSender;

        public PushController(DomikManager domikManager, PushManager pushManager, PushSender pushSender)
        {
            _domikManager = domikManager;
            _pushManager = pushManager;
            _pushSender = pushSender;
        }

        [HttpGet]
        [Route("/Push/PublicKey")]
        public Response<string> PublicKey()
        {
            return new Response<string>(_pushSender.PublicKey);
        }

        [HttpPost]
        [Route("/Push/Subscribe")]
        public Response Subscribe([FromBody] PushSubscribeDto request)
        {
            int playerId = GetPlayerId();
            _pushManager.Subscribe(playerId, request?.Endpoint, request?.P256dh, request?.Auth);
            return new Response { Type = ResponseType.Success };
        }

        [HttpPost]
        [Route("/Push/Unsubscribe")]
        public Response Unsubscribe([FromBody] PushUnsubscribeDto request)
        {
            int playerId = GetPlayerId();
            _pushManager.Unsubscribe(playerId, request?.Endpoint);
            return new Response { Type = ResponseType.Success };
        }

        private int GetPlayerId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var playerId = _domikManager.GetPlayerId(userId);
            return playerId;
        }
    }
}
