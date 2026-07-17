using Domiki.Web.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Domiki.Web.Infrastructure;

[Authorize]
[ApiController]
public class GameStreamController : ControllerBase
{
    private readonly GameStateBroker _broker;
    private readonly IServiceScopeFactory _scopeFactory;

    public GameStreamController(GameStateBroker broker, IServiceScopeFactory scopeFactory)
    {
        _broker = broker;
        _scopeFactory = scopeFactory;
    }

    [HttpGet("/Domiki/Stream")]
    public async Task Stream()
    {
        int playerId;
        using (var scope = _scopeFactory.CreateScope())
        {
            var domikManager = scope.ServiceProvider.GetRequiredService<DomikManager>();
            playerId = domikManager.GetPlayerId(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            scope.ServiceProvider.GetRequiredService<UnitOfWork>().Commit();
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        await Response.WriteAsync(": connected\n\n");
        await Response.Body.FlushAsync(HttpContext.RequestAborted);

        using var subscription = _broker.Subscribe(playerId);
        try
        {
            while (!HttpContext.RequestAborted.IsCancellationRequested)
            {
                bool canRead;
                try
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted);
                    timeout.CancelAfter(TimeSpan.FromSeconds(15));
                    canRead = await subscription.Reader.WaitToReadAsync(timeout.Token);
                }
                catch (OperationCanceledException) when (!HttpContext.RequestAborted.IsCancellationRequested)
                {
                    await Response.WriteAsync(": ping\n\n");
                    await Response.Body.FlushAsync(HttpContext.RequestAborted);
                    continue;
                }

                if (!canRead)
                {
                    break;
                }

                while (subscription.Reader.TryRead(out var changedScope))
                {
                    await Response.WriteAsync($"data: {changedScope}\n\n");
                    await Response.Body.FlushAsync(HttpContext.RequestAborted);
                }
            }
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
        }
    }
}
