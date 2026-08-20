namespace Domiki.Web.Infrastructure;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string? _appVersion;
    private readonly bool _isDevelopment;
    private readonly string _contentSecurityPolicy;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        AppBuildVersion appVersion,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _next = next;
        _appVersion = appVersion.Version;
        _isDevelopment = environment.IsDevelopment();
        _contentSecurityPolicy = BuildContentSecurityPolicy(configuration["Oidc:Authority"]);
    }

    /// <summary>
    /// Политика безопасности контента. <c>style-src</c> допускает инлайн: React расставляет стили атрибутом,
    /// а <c>prepareInlineSprite</c> переносит правила спрайтов в общий тег <c>style</c>.
    /// <c>form-action</c> пускает внешнего провайдера входа: Chrome сверяет с этой директивой и редирект,
    /// которым отвечает <c>ExternalLogin</c>.
    /// </summary>
    internal static string BuildContentSecurityPolicy(string? oidcAuthority)
    {
        var formAction = "'self'";
        if (Uri.TryCreate(oidcAuthority, UriKind.Absolute, out var authority)
            && (authority.Scheme == Uri.UriSchemeHttps || authority.Scheme == Uri.UriSchemeHttp))
        {
            formAction += $" {authority.GetLeftPart(UriPartial.Authority)}";
        }

        return "default-src 'self'; " +
               "script-src 'self'; " +
               "style-src 'self' 'unsafe-inline'; " +
               "img-src 'self' data:; " +
               "font-src 'self'; " +
               "connect-src 'self'; " +
               "worker-src 'self'; " +
               "manifest-src 'self'; " +
               "base-uri 'self'; " +
               $"form-action {formAction}; " +
               "frame-ancestors 'none'";
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
            if (!_isDevelopment)
            {
                headers["Content-Security-Policy"] = _contentSecurityPolicy;
            }

            if (_appVersion != null)
            {
                headers["X-App-Version"] = _appVersion;
            }

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
