using Domiki.Web.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Security.Claims;

namespace Domiki.Web.Infrastructure;

[Authorize]
[ApiController]
public class GameStreamController : ControllerBase
{
    private static readonly TimeSpan KeepAliveInterval = TimeSpan.FromSeconds(15);

    private readonly GameStateBroker _broker;
    private readonly IServiceScopeFactory _scopeFactory;

    public GameStreamController(GameStateBroker broker, IServiceScopeFactory scopeFactory)
    {
        _broker = broker;
        _scopeFactory = scopeFactory;
    }

    [HttpGet("/Domiki/Stream")]
    public IResult Stream()
    {
        int playerId;
        using (var scope = _scopeFactory.CreateScope())
        {
            var domikManager = scope.ServiceProvider.GetRequiredService<DomikManager>();
            playerId = domikManager.GetPlayerId(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            scope.ServiceProvider.GetRequiredService<UnitOfWork>().Commit();
        }

        Response.Headers["X-Accel-Buffering"] = "no";

        return TypedResults.ServerSentEvents(StreamScopes(playerId, HttpContext.RequestAborted));
    }

    private async IAsyncEnumerable<SseItem<string>> StreamScopes(int playerId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var subscription = _broker.Subscribe(playerId);
        while (!cancellationToken.IsCancellationRequested)
        {
            bool canRead;
            var keptAlive = false;
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(KeepAliveInterval);
                canRead = await subscription.Reader.WaitToReadAsync(timeout.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                keptAlive = true;
                canRead = false;
            }

            if (keptAlive)
            {
                yield return new SseItem<string>(string.Empty, "ping");
                continue;
            }

            if (!canRead)
            {
                break;
            }

            while (subscription.Reader.TryRead(out var changedScope))
            {
                yield return new SseItem<string>(changedScope);
            }
        }
    }
}
