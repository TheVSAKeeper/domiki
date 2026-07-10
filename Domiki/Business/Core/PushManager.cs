using Domiki.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Business.Core
{
    public class PushManager
    {
        private readonly ApplicationDbContext _context;

        public PushManager(ApplicationDbContext context)
        {
            _context = context;
        }

        public void Subscribe(int playerId, string endpoint, string p256dh, string auth)
        {
            if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(p256dh) || string.IsNullOrWhiteSpace(auth))
            {
                throw new BusinessException("Некорректная подписка");
            }

            var dbSubscription = _context.PlayerPushSubscriptions.SingleOrDefault(x => x.Endpoint == endpoint);
            if (dbSubscription == null)
            {
                _context.PlayerPushSubscriptions.Add(new PlayerPushSubscription
                {
                    PlayerId = playerId,
                    Endpoint = endpoint,
                    P256dh = p256dh,
                    Auth = auth,
                    CreatedDate = DateTimeHelper.GetNowDate(),
                });
            }
            else
            {
                dbSubscription.PlayerId = playerId;
                dbSubscription.P256dh = p256dh;
                dbSubscription.Auth = auth;
            }
        }

        public void Unsubscribe(int playerId, string endpoint)
        {
            var dbSubscription = _context.PlayerPushSubscriptions.SingleOrDefault(x => x.Endpoint == endpoint && x.PlayerId == playerId);
            if (dbSubscription != null)
            {
                _context.PlayerPushSubscriptions.Remove(dbSubscription);
            }
        }
    }
}
