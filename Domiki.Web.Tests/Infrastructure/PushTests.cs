namespace Domiki.Web.Tests
{
    public class PushTests : TestBase
    {
        /// <summary>
        /// Повторная подписка с тем же endpoint обновляет ключи существующей push-подписки, а не создаёт дубликат.
        /// </summary>
        [Test]
        public void PushSubscribeTest()
        {
            var playerId = GetPlayerId();
            var endpoint = "https://push.example.com/" + Guid.NewGuid();

            using (var uow = GetUow())
            {
                GetPushManager(uow).Subscribe(playerId, endpoint, "p256dh-1", "auth-1");
                uow.Commit();
            }

            using (var uow = GetUow())
            {
                var count = uow.Context.PlayerPushSubscriptions.Count(x => x.Endpoint == endpoint);
                Assert.That(count, Is.EqualTo(1));
                uow.Commit();
            }

            using (var uow = GetUow())
            {
                GetPushManager(uow).Subscribe(playerId, endpoint, "p256dh-2", "auth-2");
                uow.Commit();
            }

            using (var uow = GetUow())
            {
                var subscriptions = uow.Context.PlayerPushSubscriptions.Where(x => x.Endpoint == endpoint).ToList();
                Assert.That(subscriptions.Count, Is.EqualTo(1));
                Assert.That(subscriptions[0].P256dh, Is.EqualTo("p256dh-2"));
                Assert.That(subscriptions[0].Auth, Is.EqualTo("auth-2"));
                uow.Commit();
            }
        }

        private int GetPlayerId()
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow);
                var playerId = domikManager.GetPlayerId("testUser_" + Guid.NewGuid());
                uow.Commit();
                return playerId;
            }
        }
    }
}
