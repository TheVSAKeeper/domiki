using Domiki.Web.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Domiki.Web.Infrastructure;

[Authorize]
[ApiController]
// todo разобраться с роут префиксом: у всех экшенов абсолютный /Domiki/, даже вне домена домиков
public abstract class GameControllerBase : ControllerBase
{
    private readonly DomikManager _domikManager;

    protected GameControllerBase(DomikManager domikManager)
    {
        _domikManager = domikManager;
    }

    protected int GetPlayerId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return _domikManager.GetPlayerId(userId!);
    }
}
