using Domiki.Web.Infrastructure;

namespace Domiki.Web.Tests;

public sealed class StaticCacheHeadersTests
{
    /// <summary>
    /// Политика Cache-Control опознаёт статику и по исходному пути, и по пути предсжатого файла: хешированные ассеты живут год как immutable, шрифты 30 суток без immutable, service worker не кэшируется.
    /// </summary>
    /// <param name="path">путь запроса, каким его видит раздача статики</param>
    /// <param name="expected">ожидаемое значение заголовка или null, если политики для пути нет</param>
    [TestCase("/assets/index-abc123.js", "public, max-age=31536000, immutable")]
    [TestCase("/assets/index-abc123.js.br", "public, max-age=31536000, immutable")]
    [TestCase("/assets/index-abc123.css.gz", "public, max-age=31536000, immutable")]
    [TestCase("/fonts/golos-text-cyrillic.woff2", "public, max-age=2592000")]
    [TestCase("/sw.js", "no-cache")]
    [TestCase("/sw.js.br", "no-cache")]
    [TestCase("/sw.js.gz", "no-cache")]
    [TestCase("/manifest.json", null)]
    [TestCase("/index.html.br", null)]
    [TestCase(null, null)]
    public void ResolveCacheControlTest(string? path, string? expected)
    {
        var cacheControl = StaticCacheHeaders.ResolveCacheControl(path);

        Assert.That(cacheControl, Is.EqualTo(expected));
    }
}
