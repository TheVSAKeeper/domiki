using Microsoft.AspNetCore.Identity;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Учётная запись ASP.NET Identity (кука-логин или внешний OIDC-провайдер).
/// </summary>
/// <remarks>
/// Игровой профиль – отдельная сущность <see cref="Player"/>, привязанная по <see cref="Player.AspNetUserId"/>.
/// </remarks>
public class ApplicationUser : IdentityUser
{
}
