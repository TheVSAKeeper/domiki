using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace Domiki.Web.Tests;

public sealed class AuthenticationEndpointsTests
{
    /// <summary>
    /// Эндпоинт текущего пользователя без куки сообщает isAuthenticated false.
    /// </summary>
    [Test]
    public async Task UserWithoutLoginReportsNotAuthenticatedTest()
    {
        var client = App.Client();

        var response = await client.GetAsync("/authentication/user");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(json.RootElement.GetProperty("isAuthenticated").GetBoolean(), Is.False);
        }
    }

    /// <summary>
    /// После демо-входа эндпоинт текущего пользователя сообщает isAuthenticated true и имя демо-аккаунта.
    /// </summary>
    [Test]
    public async Task UserAfterLoginReportsAuthenticatedNameTest()
    {
        var client = App.Client();
        await client.PostAsync("/authentication/demo", null);

        var response = await client.GetAsync("/authentication/user");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(json.RootElement.GetProperty("isAuthenticated").GetBoolean(), Is.True);
            Assert.That(json.RootElement.GetProperty("name").GetString(), Is.EqualTo(DemoUserName()));
        }
    }

    /// <summary>
    /// Повторный демо-вход при уже установленной куке возвращает 200 с именем текущего аккаунта, не переавторизуя.
    /// </summary>
    [Test]
    public async Task RepeatDemoLoginReturnsAlreadyAuthenticatedTest()
    {
        var client = App.Client();
        await client.PostAsync("/authentication/demo", null);

        var response = await client.PostAsync("/authentication/demo", null);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(json.RootElement.GetProperty("isAuthenticated").GetBoolean(), Is.True);
            Assert.That(json.RootElement.GetProperty("name").GetString(), Is.EqualTo(DemoUserName()));
        }
    }

    /// <summary>
    /// Демо-аккаунту закрыт доступ к управлению профилем Identity: страница отвечает 403.
    /// </summary>
    [Test]
    public async Task DemoAccountBlockedFromIdentityManageTest()
    {
        var client = App.Client();
        await client.PostAsync("/authentication/demo", null);

        var response = await client.GetAsync("/Identity/Account/Manage");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    /// <summary>
    /// Лимит демо-входа – 10 запросов за 5 минут – делится по последнему адресу X-Forwarded-For, который дописывает
    /// Angie: подмена первых адресов не открывает новую корзину и одиннадцатый вход получает 429, а другой последний
    /// адрес считается в своей корзине и проходит.
    /// </summary>
    [Test]
    public async Task DemoLoginLimitByLastForwardedAddressTest()
    {
        const int demoLoginLimit = 10;
        const string proxyAddress = "198.51.100.10";
        const string otherProxyAddress = "198.51.100.11";
        var client = App.Client();
        client.DefaultRequestHeaders.Remove("X-Forwarded-For");

        var allowed = new List<HttpStatusCode>();
        for (var i = 0; i < demoLoginLimit; i++)
        {
            allowed.Add(await DemoLoginAsync(client, $"203.0.113.{i}, {proxyAddress}"));
        }

        var spoofedFirst = await DemoLoginAsync(client, $"203.0.113.{demoLoginLimit}, {proxyAddress}");
        var otherLast = await DemoLoginAsync(client, $"203.0.113.0, {otherProxyAddress}");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(allowed, Is.All.EqualTo(HttpStatusCode.OK));
            Assert.That(spoofedFirst, Is.EqualTo(HttpStatusCode.TooManyRequests));
            Assert.That(otherLast, Is.EqualTo(HttpStatusCode.OK));
        }
    }

    private static async Task<HttpStatusCode> DemoLoginAsync(HttpClient client, string forwardedFor)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/authentication/demo");
        request.Headers.Add("X-Forwarded-For", forwardedFor);
        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }

    private static string DemoUserName()
    {
        return App.Services.GetRequiredService<IConfiguration>()["Demo:UserName"]!;
    }
}
