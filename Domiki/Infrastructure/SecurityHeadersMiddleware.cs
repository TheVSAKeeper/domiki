namespace Domiki.Web.Infrastructure;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        httpContext.Response.OnStarting(() =>
        {
            var headers = httpContext.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["X-Frame-Options"] = "DENY";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            if (httpContext.Response.ContentType?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true
                && !headers.ContainsKey("Cache-Control"))
            {
                headers.CacheControl = "no-cache";
            }

            return Task.CompletedTask;
        });

        await _next(httpContext);
    }
}
