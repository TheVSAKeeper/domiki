using System.Net;
using System.Text.Json;

namespace Domiki.Web.Tests;

public sealed class HttpSmokeTests
{
    /// <summary>
    /// BusinessException из менеджера доходит через HTTP-конвейер как 400 application/problem+json с текстом ошибки в
    /// поле detail.
    /// </summary>
    [Test]
    public async Task BuyUnknownDomikTypeReturnsProblemDetailsTest()
    {
        var client = App.Client();
        await client.PostAsync("/authentication/demo", null);

        var response = await client.PostAsync("/Domiki/BuyDomik/999999", null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
        }

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(json.RootElement.GetProperty("status").GetInt32(), Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(json.RootElement.GetProperty("detail").GetString(), Is.Not.Empty);
        }
    }

    /// <summary>
    /// Демо-вход выдаёт куку аутентификации, и авторизованный запрос состояния игры возвращает 200 с телом-DTO без
    /// конверта.
    /// </summary>
    [Test]
    public async Task DemoLoginThenGetGameStateReturnsStateTest()
    {
        var client = App.Client();

        var loginResponse = await client.PostAsync("/authentication/demo", null);
        Assert.That(loginResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var gameStateResponse = await client.GetAsync("/Domiki/GetGameState");
        Assert.That(gameStateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using var json = JsonDocument.Parse(await gameStateResponse.Content.ReadAsStringAsync());
        Assert.That(json.RootElement.GetProperty("domikTypes").GetArrayLength(), Is.GreaterThan(0));
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
