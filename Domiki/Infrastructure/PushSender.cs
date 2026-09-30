using Domiki.Web.Data;
using System.Net;
using System.Text.Json;
using WebPush;

namespace Domiki.Web.Infrastructure;

public class PushSender
{
    /// <summary>
    /// Категория push для завершения строительства или улучшения.
    /// </summary>
    public const string ConstructionTag = "construction";

    /// <summary>
    /// Категория push для производства и остановки наряда.
    /// </summary>
    public const string ProductionTag = "production";

    /// <summary>
    /// Категория push для появления нового заказа.
    /// </summary>
    public const string OrderTag = "order";

    /// <summary>
    /// Категория push для начала и развязки происшествия.
    /// </summary>
    public const string IncidentTag = "incident";

    /// <summary>
    /// Категория push для возвращения похода без происшествия.
    /// </summary>
    public const string ExpeditionTag = "expedition";

    /// <summary>
    /// Категория push для завершения поручения.
    /// </summary>
    public const string ErrandTag = "errand";

    /// <summary>
    /// Категория push для операций ярмарки.
    /// </summary>
    public const string MarketTag = "market";

    /// <summary>
    /// Категория push для завершения толоки.
    /// </summary>
    public const string TolokaTag = "toloka";

    /// <summary>
    /// Категория push для новых записей в книге гостей.
    /// </summary>
    public const string GuestbookTag = "guestbook";

    /// <summary>
    /// Категория push для помощи деревне.
    /// </summary>
    public const string HelpTag = "help";

    private readonly string? _vapidPublicKey;
    private readonly string? _vapidPrivateKey;
    private readonly string? _subject;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PushSender> _logger;
    private readonly WebPushClient _webPushClient = new(PushEndpointGuard.CreateHttpClient());

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

    /// <summary>
    /// Отправляет игроку Web Push с категорией, используемой для группировки уведомлений в service worker.
    /// </summary>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <param name="title">Заголовок уведомления.</param>
    /// <param name="body">Текст уведомления.</param>
    /// <param name="url">Маршрут, открываемый по клику.</param>
    /// <param name="tag">Категория уведомления.</param>
    public void Notify(int playerId, string title, string body, string url, string tag)
    {
        if (!Enabled)
        {
            return;
        }

        Task.Run(async () =>
        {
            try
            {
                await SendAsync(playerId, title, body, url, tag);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PushSender - notify failed: " + playerId);
            }
        });
    }

    private async Task SendAsync(int playerId, string title, string body, string url, string tag)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var subscriptions = context.PlayerPushSubscriptions.Where(x => x.PlayerId == playerId).ToList();
        if (subscriptions.Count == 0)
        {
            return;
        }

        var payload = SerializePayload(title, body, url, tag);
        var vapidDetails = new VapidDetails(_subject, _vapidPublicKey, _vapidPrivateKey);

        foreach (var subscription in subscriptions)
        {
            if (!PushEndpointGuard.IsSendable(subscription.Endpoint))
            {
                _logger.LogWarning("PushSender - blocked non-public endpoint: " + playerId + " - subscription " + subscription.Id);
                continue;
            }

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

    /// <summary>
    /// Сериализует контракт payload для браузерного service worker.
    /// </summary>
    internal static string SerializePayload(string title, string body, string url, string tag)
    {
        return JsonSerializer.Serialize(new { title, body, url, tag });
    }
}
