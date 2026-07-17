namespace Domiki.Web.Infrastructure.Dto;

/// <summary>
/// Запрос на подписку браузера игрока на web push.
/// </summary>
/// <remarks>
/// Несёт ключи Web Push API: endpoint и параметры шифрования payload (см. <see cref="Infrastructure.PushManager.Subscribe"/>).
/// </remarks>
public sealed record PushSubscribeDto
{
    /// <summary>
    /// URL push-сервиса браузера, на который отправляются уведомления.
    /// </summary>
    public string? Endpoint { get; init; }

    /// <summary>
    /// Публичный ключ подписки для шифрования payload (Web Push, P-256 ECDH).
    /// </summary>
    public string? P256dh { get; init; }

    /// <summary>
    /// Секрет аутентификации подписки (Web Push).
    /// </summary>
    public string? Auth { get; init; }
}

/// <summary>
/// Запрос на отписку браузера игрока от web push.
/// </summary>
/// <remarks>
/// См. <see cref="Infrastructure.PushManager.Unsubscribe"/>.
/// </remarks>
public sealed record PushUnsubscribeDto
{
    /// <summary>
    /// URL push-сервиса браузера, подписку на который нужно снять.
    /// </summary>
    public string? Endpoint { get; init; }
}
