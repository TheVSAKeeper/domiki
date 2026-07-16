using Domiki.Web.Data.Entities;
using Domiki.Web.Data;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;
using WebPush;

namespace Domiki.Web.Infrastructure
{
    public class PushSender
    {
        private readonly string _vapidPublicKey;
        private readonly string _vapidPrivateKey;
        private readonly string _subject;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PushSender> _logger;
        private readonly WebPushClient _webPushClient = new WebPushClient();

        public PushSender(IConfiguration configuration, IServiceScopeFactory scopeFactory, ILogger<PushSender> logger)
        {
            _vapidPublicKey = configuration["Push:VapidPublicKey"];
            _vapidPrivateKey = configuration["Push:VapidPrivateKey"];
            _subject = configuration["Push:Subject"];
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public string PublicKey => _vapidPublicKey ?? string.Empty;

        public bool Enabled => !string.IsNullOrWhiteSpace(_vapidPublicKey) && !string.IsNullOrWhiteSpace(_vapidPrivateKey);

        public void Notify(int playerId, string title, string body, string url)
        {
            if (!Enabled)
            {
                return;
            }

            Task.Run(async () =>
            {
                try
                {
                    await SendAsync(playerId, title, body, url);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "PushSender - notify failed: " + playerId);
                }
            });
        }

        private async Task SendAsync(int playerId, string title, string body, string url)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var subscriptions = context.PlayerPushSubscriptions.Where(x => x.PlayerId == playerId).ToList();
            if (subscriptions.Count == 0)
            {
                return;
            }

            var payload = JsonSerializer.Serialize(new { title, body, url });
            var vapidDetails = new VapidDetails(_subject, _vapidPublicKey, _vapidPrivateKey);

            foreach (var subscription in subscriptions)
            {
                try
                {
                    var pushSubscription = new PushSubscription(subscription.Endpoint, subscription.P256dh, subscription.Auth);
                    await _webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails);
                }
                catch (WebPushException ex) when (ex.StatusCode is HttpStatusCode.Gone or HttpStatusCode.NotFound)
                {
                    context.PlayerPushSubscriptions.Remove(subscription);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "PushSender - send failed: " + playerId + " - subscription " + subscription.Id);
                }
            }

            context.SaveChanges();
        }
    }
}
