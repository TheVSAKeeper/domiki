using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Domiki.Web.Business.Core
{
    public static class GameStateScopes
    {
        public const string State = "state";
        public const string Market = "market";
        public const string Toloka = "toloka";
    }

    public class GameStateBroker
    {
        private readonly ConcurrentDictionary<Guid, Subscription> _subscriptions = new();

        public Subscription Subscribe(int playerId)
        {
            var id = Guid.NewGuid();
            var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(8)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
            });
            var subscription = new Subscription(this, id, playerId, channel);
            _subscriptions[id] = subscription;
            return subscription;
        }

        public void Publish(int playerId, string scope)
        {
            foreach (var subscription in _subscriptions.Values)
            {
                if (subscription.PlayerId == playerId)
                {
                    subscription.Writer.TryWrite(scope);
                }
            }
        }

        public void Broadcast(string scope)
        {
            foreach (var subscription in _subscriptions.Values)
            {
                subscription.Writer.TryWrite(scope);
            }
        }

        private void Unsubscribe(Guid id)
        {
            _subscriptions.TryRemove(id, out _);
        }

        public class Subscription : IDisposable
        {
            private readonly GameStateBroker _broker;
            private readonly Guid _id;
            private int _isDisposed;

            internal Subscription(GameStateBroker broker, Guid id, int playerId, Channel<string> channel)
            {
                _broker = broker;
                _id = id;
                PlayerId = playerId;
                Reader = channel.Reader;
                Writer = channel.Writer;
            }

            public int PlayerId { get; }
            public ChannelReader<string> Reader { get; }
            internal ChannelWriter<string> Writer { get; }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
                {
                    return;
                }

                _broker.Unsubscribe(_id);
                Writer.TryComplete();
            }
        }
    }
}
