using Domiki.Web.Infrastructure;
using System.Net;
using System.Net.Sockets;
using System.Security;
using WebPush;

namespace Domiki.Web.Tests;

public sealed class PushTests
{
    /// <summary>
    /// Повторная подписка с тем же endpoint обновляет ключи существующей push-подписки, а не создаёт дубликат.
    /// </summary>
    [Test]
    public void PushSubscribeTest()
    {
        var player = TestPlayer.Create();
        var endpoint = "https://push.example.com/" + Guid.NewGuid();

        player.Subscribe(endpoint, "p256dh-1", "auth-1");

        var firstCount = App.Read(context => context.PlayerPushSubscriptions.Count(x => x.Endpoint == endpoint));
        Assert.That(firstCount, Is.EqualTo(1));

        player.Subscribe(endpoint, "p256dh-2", "auth-2");

        var subscriptions = App.Read(context => context.PlayerPushSubscriptions.Where(x => x.Endpoint == endpoint).ToList());
        Assert.That(subscriptions.Count, Is.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(subscriptions[0].P256dh, Is.EqualTo("p256dh-2"));
            Assert.That(subscriptions[0].Auth, Is.EqualTo("auth-2"));
        }
    }

    /// <summary>
    /// Payload push содержит категорию, по которой service worker разводит уведомления.
    /// </summary>
    [Test]
    public void PushPayloadContainsTagTest()
    {
        using var payload = System.Text.Json.JsonDocument.Parse(PushSender.SerializePayload("Заголовок", "Текст", "/domiki-page", PushSender.OrderTag));
        var root = payload.RootElement;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(root.GetProperty("title").GetString(), Is.EqualTo("Заголовок"));
            Assert.That(root.GetProperty("body").GetString(), Is.EqualTo("Текст"));
            Assert.That(root.GetProperty("url").GetString(), Is.EqualTo("/domiki-page"));
            Assert.That(root.GetProperty("tag").GetString(), Is.EqualTo(PushSender.OrderTag));
        }
    }

    /// <summary>
    /// Клиент отправки web-push не подключается к петлевому или служебному адресу, в том числе когда имя резолвится
    /// в него только в момент подключения: отказ приходит как <see cref="HttpRequestException"/> с причиной
    /// <see cref="SecurityException"/>, а петлевой слушатель не получает соединения.
    /// </summary>
    /// <param name="host">Хост push-эндпоинта.</param>
    [TestCase("localhost")]
    [TestCase("127.0.0.1")]
    [TestCase("169.254.169.254")]
    [TestCase("[64:ff9b::7f00:1]")]
    public void PushRejectsPrivateAddressTest(string host)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var vapidKeys = VapidHelper.GenerateVapidKeys();
        var vapidDetails = new VapidDetails("mailto:test@example.com", vapidKeys.PublicKey, vapidKeys.PrivateKey);
        var subscription = new PushSubscription($"https://{host}:{port}/push", "p256dh", "auth");
        using var client = new WebPushClient(PushEndpointGuard.CreateHttpClient());
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var exception = Assert.ThrowsAsync<HttpRequestException>(() => client.SendNotificationAsync(subscription, null, vapidDetails, timeout.Token));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.InnerException, Is.TypeOf<SecurityException>());
            Assert.That(listener.Pending(), Is.False);
        }
    }

    /// <summary>
    /// Адрес push-эндпоинта со вложенным IPv4 (NAT64 <c>64:ff9b::/96</c>, 6to4 <c>2002::/16</c>) допускается только
    /// с публичным вложенным IPv4; локальный NAT64, IPv4-совместимые, Teredo и документационные IPv6 запрещены
    /// целиком, как и IPv4-диапазоны 192.0.0.0/24, 198.18.0.0/15 и три TEST-NET. Приём подписки и отправка
    /// решают одинаково.
    /// </summary>
    /// <param name="host">Хост-литерал push-эндпоинта.</param>
    /// <param name="allowed">Допустим ли адрес.</param>
    [TestCase("[64:ff9b::a00:1]", false)]
    [TestCase("[64:ff9b::a9fe:a9fe]", false)]
    [TestCase("[64:ff9b::7f00:1]", false)]
    [TestCase("[64:ff9b::808:808]", true)]
    [TestCase("[2002:c0a8:101::1]", false)]
    [TestCase("[2002:7f00:1::]", false)]
    [TestCase("[2002:808:808::1]", true)]
    [TestCase("[64:ff9b:1::808:808]", false)]
    [TestCase("[::a00:1]", false)]
    [TestCase("[::808:808]", false)]
    [TestCase("[2001:0:4136:e378:8000:63bf:3fff:fdd2]", false)]
    [TestCase("[2001:db8::1]", false)]
    [TestCase("[2001:4860:4860::8888]", true)]
    [TestCase("[2a00:1450:4001:81c::200a]", true)]
    [TestCase("192.0.0.1", false)]
    [TestCase("192.0.2.1", false)]
    [TestCase("192.0.1.1", true)]
    [TestCase("198.18.0.1", false)]
    [TestCase("198.19.255.255", false)]
    [TestCase("198.17.255.255", true)]
    [TestCase("198.20.0.1", true)]
    [TestCase("198.51.100.1", false)]
    [TestCase("203.0.113.1", false)]
    [TestCase("8.8.8.8", true)]
    public void PushEndpointEmbeddedAddressTest(string host, bool allowed)
    {
        var endpoint = $"https://{host}/push";

        using (Assert.EnterMultipleScope())
        {
            Assert.That(PushEndpointGuard.IsSendable(endpoint), Is.EqualTo(allowed));
            Assert.That(IsRegisterable(endpoint), Is.EqualTo(allowed));
        }
    }

    private static bool IsRegisterable(string endpoint)
    {
        try
        {
            PushEndpointGuard.EnsureRegisterable(endpoint);
            return true;
        }
        catch (BusinessException)
        {
            return false;
        }
    }
}

file static class PushTestsActs
{
    public static TestPlayer Subscribe(this TestPlayer p, string endpoint, string p256dh, string auth)
    {
        App.Act<PushManager>(m => m.Subscribe(p.Id, endpoint, p256dh, auth));
        return p;
    }
}
