namespace Domiki.Web.Infrastructure.Dto;

/// <summary>
/// Запрос на подписку браузера игрока на web push.
/// </summary>
/// <remarks>
/// Несёт ключи Web Push API: endpoint и параметры шифрования payload (см. <see cref="Infrastructure.PushManager.Subscribe"/>).
/// </remarks>
public class PushSubscribeDto
{
    /// <summary>
    /// URL push-сервиса браузера, на который отправляются уведомления.
    /// </summary>
    public string Endpoint { get; set; }

    /// <summary>
    /// Публичный ключ подписки для шифрования payload (Web Push, P-256 ECDH).
    /// </summary>
    public string P256dh { get; set; }

    /// <summary>
    /// Секрет аутентификации подписки (Web Push).
    /// </summary>
    public string Auth { get; set; }
}

/// <summary>
/// Запрос на отписку браузера игрока от web push.
/// </summary>
/// <remarks>
/// См. <see cref="Infrastructure.PushManager.Unsubscribe"/>.
/// </remarks>
public class PushUnsubscribeDto
{
    /// <summary>
    /// URL push-сервиса браузера, подписку на который нужно снять.
    /// </summary>
    public string Endpoint { get; set; }
}
