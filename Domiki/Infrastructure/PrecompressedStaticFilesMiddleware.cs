namespace Domiki.Web.Infrastructure;

public class PrecompressedStaticFilesMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;

    public PrecompressedStaticFilesMiddleware(RequestDelegate next, IWebHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        var originalPath = httpContext.Request.Path;
        var acceptEncoding = httpContext.Request.GetTypedHeaders().AcceptEncoding;
        var extension = acceptEncoding?.Any(x => x.Value.Equals("br", StringComparison.OrdinalIgnoreCase) && x.Quality.GetValueOrDefault(1) > 0) == true
            ? ".br"
            : acceptEncoding?.Any(x => x.Value.Equals("gzip", StringComparison.OrdinalIgnoreCase) && x.Quality.GetValueOrDefault(1) > 0) == true
                ? ".gz"
                : null;

        if (extension != null && originalPath.HasValue)
        {
            var compressedPath = _environment.WebRootFileProvider.GetFileInfo(originalPath.Value.TrimStart('/') + extension);
            if (compressedPath.Exists)
            {
                httpContext.Request.Path = originalPath + extension;
                httpContext.Response.Headers.ContentEncoding = extension == ".br" ? "br" : "gzip";
                httpContext.Response.Headers.Append("Vary", "Accept-Encoding");
                httpContext.Response.OnStarting(() =>
                {
                    httpContext.Response.ContentType = Path.GetExtension(originalPath.Value) switch
                    {
                        ".css" => "text/css",
                        ".html" => "text/html; charset=utf-8",
                        ".js" => "text/javascript",
                        ".json" => "application/json",
                        ".svg" => "image/svg+xml",
                        ".xml" => "text/xml",
                        _ => httpContext.Response.ContentType,
                    };

                    return Task.CompletedTask;
                });
            }
        }

        await _next(httpContext);
        httpContext.Request.Path = originalPath;
    }
}
