namespace Domiki.Web.Infrastructure;

/// <summary>
/// Политика заголовка <c>Cache-Control</c> для статики SPA.
/// </summary>
public static class StaticCacheHeaders
{
    /// <summary>
    /// Возвращает значение <c>Cache-Control</c> для пути статики или <c>null</c>, если для пути политики нет.
    /// Путь сначала очищается от суффикса предсжатого файла: <see cref="PrecompressedStaticFilesMiddleware"/>
    /// подменяет <c>/sw.js</c> на <c>/sw.js.br</c> ещё до раздачи, и сравнение с исходным именем перестаёт совпадать.
    /// </summary>
    /// <param name="path">путь запроса, каким его видит раздача статики</param>
    public static string? ResolveCacheControl(string? path)
    {
        if (path == null)
        {
            return null;
        }

        var originalPath = path.EndsWith(".br", StringComparison.Ordinal)
            ? path[..^3]
            : path.EndsWith(".gz", StringComparison.Ordinal)
                ? path[..^3]
                : path;

        if (originalPath.StartsWith("/assets/", StringComparison.Ordinal))
        {
            return "public, max-age=31536000, immutable";
        }

        if (originalPath.StartsWith("/fonts/", StringComparison.Ordinal))
        {
            return "public, max-age=2592000";
        }

        return originalPath == "/sw.js" ? "no-cache" : null;
    }
}
