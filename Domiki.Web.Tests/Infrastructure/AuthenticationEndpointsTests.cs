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

    private static string DemoUserName()
    {
        return App.Services.GetRequiredService<IConfiguration>()["Demo:UserName"]!;
    }
}
