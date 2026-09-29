using Domiki.Web.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Security.Claims;

namespace Domiki.Web.Tests;

public sealed class GameStreamTests
{
    /// <summary>
    /// SSE-поток состояния без куки аутентификации отвечает 401.
    /// </summary>
    [Test]
    public async Task StreamWithoutLoginReturnsUnauthorizedTest()
    {
        var client = App.Client();

        var response = await client.GetAsync("/Domiki/Stream");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    /// <summary>
    /// Экшен потока возвращает нативный ServerSentEventsResult и ставит заголовок X-Accel-Buffering no, отключающий
    /// буферизацию на прокси.
    /// </summary>
    [Test]
    public void StreamActionReturnsServerSentEventsResultWithProxyBufferingDisabledTest()
    {
        var controller = DemoStreamController(out var httpContext);

        var result = controller.Stream();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.InstanceOf<ServerSentEventsResult<string>>());
            Assert.That(httpContext.Response.Headers["X-Accel-Buffering"].ToString(), Is.EqualTo("no"));
        }
    }

    /// <summary>
    /// Поток отдаёт первый ping сразу при подписке, а не через 15 секунд простоя: заголовки уходят с первым событием,
    /// и без него EventSource.onopen у клиента запаздывал бы на весь интервал.
    /// </summary>
    [Test]
    public async Task StreamSendsPingRightAfterSubscribeTest()
    {
        const int waitSeconds = 5;
        var client = App.Client();
        await client.PostAsync("/authentication/demo", null);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(waitSeconds));

        using var response = await client.GetAsync("/Domiki/Stream", HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(timeout.Token));
        var firstLine = await reader.ReadLineAsync(timeout.Token);

        Assert.That(firstLine, Is.EqualTo("event: ping"));
    }

    private static GameStreamController DemoStreamController(out HttpContext httpContext)
    {
        var demoUserName = App.Services.GetRequiredService<IConfiguration>()["Demo:UserName"]!;
        var demoUserId = App.Read(db => db.Users.Single(user => user.UserName == demoUserName).Id);

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, demoUserId)], "Test");
        httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };

        return new GameStreamController(
            App.Services.GetRequiredService<GameStateBroker>(),
            App.Services.GetRequiredService<IServiceScopeFactory>())
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
    }
}
