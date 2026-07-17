namespace Domiki.Web.Infrastructure;

public class DemoAccountGuardMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string? _demoUserName;

    public DemoAccountGuardMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _demoUserName = configuration["Demo:UserName"];
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        var path = httpContext.Request.Path;
        if (!string.IsNullOrEmpty(_demoUserName)
            && httpContext.User.Identity?.Name == _demoUserName
            && (path.StartsWithSegments("/Identity/Account/Manage")
                || path.StartsWithSegments("/Identity/Account/ExternalLogin")))
        {
            httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        await _next(httpContext);
    }
}
