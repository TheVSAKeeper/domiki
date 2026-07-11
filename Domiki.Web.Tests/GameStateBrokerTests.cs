using Domiki.Web.Business.Core;

namespace Domiki.Web.Tests
{
    public class GameStateBrokerTests
    {
        [Test]
        public void PublishDeliversOnlyToTargetPlayer()
        {
            var broker = new GameStateBroker();
            using var target = broker.Subscribe(1);
            using var other = broker.Subscribe(2);

            broker.Publish(1, GameStateScopes.State);

            Assert.That(target.Reader.TryRead(out var scope), Is.True);
            Assert.That(scope, Is.EqualTo(GameStateScopes.State));
            Assert.That(other.Reader.TryRead(out _), Is.False);
        }

        [Test]
        public void BroadcastDeliversToAllPlayers()
        {
            var broker = new GameStateBroker();
            using var first = broker.Subscribe(1);
            using var second = broker.Subscribe(2);

            broker.Broadcast(GameStateScopes.Market);

            Assert.That(first.Reader.TryRead(out var firstScope), Is.True);
            Assert.That(firstScope, Is.EqualTo(GameStateScopes.Market));
            Assert.That(second.Reader.TryRead(out var secondScope), Is.True);
            Assert.That(secondScope, Is.EqualTo(GameStateScopes.Market));
        }

        [Test]
        public void DisposedSubscriptionDoesNotReceiveMessages()
        {
            var broker = new GameStateBroker();
            var subscription = broker.Subscribe(1);

            subscription.Dispose();
            broker.Publish(1, GameStateScopes.State);

            Assert.That(subscription.Reader.TryRead(out _), Is.False);
        }
    }
}
