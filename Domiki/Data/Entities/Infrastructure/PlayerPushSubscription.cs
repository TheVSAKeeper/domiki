using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Подписка браузера игрока на веб-push (Push API).
/// </summary>
/// <remarks>
/// <see cref="Infrastructure.PushSender"/> шлёт по ней уведомления, удаляется при истечении подписки на стороне браузера
/// (<c>410</c>/<c>404</c> от push-сервиса).
/// </remarks>
[Index(nameof(Endpoint), IsUnique = true)]
public class PlayerPushSubscription
{
    /// <summary>
    /// Идентификатор подписки.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Игрок, которому принадлежит подписка.
    /// </summary>
    public int PlayerId { get; set; }

    /// <summary>
    /// URL push-сервиса браузера, уникально идентифицирующий конкретную подписку.
    /// </summary>
    /// <remarks>
    /// <see cref="Infrastructure.PushManager"/> ищет по нему при повторной <see cref="Infrastructure.PushManager.Subscribe"/>.
    /// </remarks>
    [Required(AllowEmptyStrings = false)]
    public required string Endpoint { get; set; }

    /// <summary>
    /// Публичный ключ шифрования payload для этой подписки (часть Web Push API).
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public required string P256dh { get; set; }

    /// <summary>
    /// Секрет аутентификации для этой подписки (часть Web Push API).
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public required string Auth { get; set; }

    /// <summary>
    /// Момент создания подписки.
    /// </summary>
    public DateTime CreatedDate { get; set; }
}
