using Domiki.Web.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Domiki.Web.Infrastructure;

[Authorize]
[ApiController]
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
        return _domikManager.GetPlayerId(userId);
    }
}
