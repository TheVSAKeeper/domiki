using Domiki.Web.Infrastructure;

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
}

file static class PushTestsActs
{
    public static TestPlayer Subscribe(this TestPlayer p, string endpoint, string p256dh, string auth)
    {
        App.Act<PushManager>(m => m.Subscribe(p.Id, endpoint, p256dh, auth));
        return p;
    }
}
