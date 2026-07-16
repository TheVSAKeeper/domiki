using Domiki.Web.Infrastructure;
using System.Net;
using System.Text.Json;

namespace Domiki.Web.Tests;

public sealed class HttpSmokeTests
{
    /// <summary>
    /// BusinessException из менеджера доходит через HTTP-конвейер как 400 с конвертом { type = ErrorMessage }, а
    /// UnitOfWorkMiddleware
    /// откатывает транзакцию, не коммитя частичных изменений.
    /// </summary>
    [Test]
    public async Task BuyUnknownDomikTypeReturnsErrorEnvelopeTest()
    {
        var client = App.Client();
        await client.PostAsync("/authentication/demo", null);

        var response = await client.PostAsync("/Domiki/BuyDomik/999999", null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(json.RootElement.GetProperty("type").GetInt32(), Is.EqualTo((int)ResponseType.ErrorMessage));
            Assert.That(json.RootElement.GetProperty("content").GetString(), Is.Not.Empty);
        }
    }

    /// <summary>
    /// Демо-вход выдаёт куку аутентификации, и авторизованный запрос состояния игры возвращает конверт { type = Success }.
    /// </summary>
    [Test]
    public async Task DemoLoginThenGetGameStateReturnsSuccessEnvelopeTest()
    {
        var client = App.Client();

        var loginResponse = await client.PostAsync("/authentication/demo", null);
        Assert.That(loginResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var gameStateResponse = await client.GetAsync("/Domiki/GetGameState");
        Assert.That(gameStateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using var json = JsonDocument.Parse(await gameStateResponse.Content.ReadAsStringAsync());
        Assert.That(json.RootElement.GetProperty("type").GetInt32(), Is.EqualTo((int)ResponseType.Success));
    }

    /// <summary>
    /// Игровой эндпоинт без куки аутентификации отвечает 401, а не редиректом на страницу логина.
    /// </summary>
    [Test]
    public async Task GetGameStateWithoutLoginReturnsUnauthorizedTest()
    {
        var client = App.Client();

        var response = await client.GetAsync("/Domiki/GetGameState");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
