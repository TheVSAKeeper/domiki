using System.Text.RegularExpressions;

namespace Domiki.Web.Infrastructure;

/// <summary>
/// Идентификатор текущей клиентской сборки, которую отдаёт сервер.
/// </summary>
/// <remarks>
/// Singleton: при старте один раз читает <c>wwwroot/index.html</c> и извлекает хеш входного чанка
/// (<c>/assets/index-&lt;hash&gt;.js</c>). Значение уходит клиенту заголовком <c>X-App-Version</c>
/// (<see cref="SecurityHeadersMiddleware"/>); фронт сверяет его со своей загруженной сборкой и при
/// расхождении после деплоя предлагает обновиться. В Development собранного <c>index.html</c> нет –
/// значение равно <see langword="null"/>, и проверка версий отключена.
/// </remarks>
public sealed partial class AppBuildVersion
{
    /// <summary>
    /// Хеш входного чанка текущей сборки либо <see langword="null"/>, если собранный <c>index.html</c> недоступен.
    /// </summary>
    public string? Version { get; }

    /// <summary>
    /// Читает идентификатор сборки из собранного <c>index.html</c> веб-корня.
    /// </summary>
    /// <param name="environment">Окружение хоста – источник пути к веб-корню.</param>
    public AppBuildVersion(IWebHostEnvironment environment)
    {
        var indexPath = Path.Combine(environment.WebRootPath ?? string.Empty, "index.html");
        if (!File.Exists(indexPath))
        {
            return;
        }

        var match = EntryChunkRegex().Match(File.ReadAllText(indexPath));
        if (match.Success)
        {
            Version = match.Groups[1].Value;
        }
    }

    [GeneratedRegex("""/assets/index-([A-Za-z0-9_-]+)\.js""")]
    private static partial Regex EntryChunkRegex();
}
