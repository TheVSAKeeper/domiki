using Domiki.Web.Data.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Domiki.Web.Infrastructure;

[AllowAnonymous]
[ApiController]
public class AuthenticationController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;

    public AuthenticationController(SignInManager<ApplicationUser> signInManager, IConfiguration configuration)
    {
        _signInManager = signInManager;
        _configuration = configuration;
    }

    [HttpGet("/authentication/login")]
    public IActionResult Login([FromQuery] string? returnUrl)
    {
        var target = !string.IsNullOrEmpty(returnUrl) && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative)
            ? returnUrl
            : "/";

        return Redirect($"/Identity/Account/Login?returnUrl={Uri.EscapeDataString(target)}");
    }

    [HttpGet("/authentication/logout")]
    public IActionResult Logout()
    {
        return SignOut(new AuthenticationProperties { RedirectUri = "/" }, IdentityConstants.ApplicationScheme);
    }

    [HttpGet("/authentication/user")]
    public IActionResult GetUser()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Ok(new { isAuthenticated = false });
        }

        var name = User.FindFirstValue("name")
                   ?? User.FindFirstValue("preferred_username")
                   ?? User.FindFirstValue("email")
                   ?? User.Identity?.Name;

        return Ok(new { isAuthenticated = true, name });
    }

    [HttpPost("/authentication/demo")]
    public async Task<IActionResult> Demo()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return Ok(new { isAuthenticated = true, name = User.Identity.Name });
        }

        var demoUserName = _configuration["Demo:UserName"]!;
        var demoPassword = _configuration["Demo:Password"]!;

        var result = await _signInManager.PasswordSignInAsync(demoUserName, demoPassword, false, false);
        return result.Succeeded
            ? Ok(new { isAuthenticated = true, name = demoUserName })
            : Unauthorized();
    }
}
