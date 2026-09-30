using System.IO.Compression;
using System.Text;
using Domiki.Web.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Domiki.Web.Tests;

public sealed class StaticCacheHeadersTests
{
    private const string WorkerPath = "/sw.js";

    private const string AssetPath = "/assets/index-abc123.js";

    private const string ScriptBody = "self.addEventListener('install', () => self.skipWaiting());";

    private string _webRoot = null!;

    private WebApplicationFactory<Program> _host = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _webRoot = Path.Combine(Path.GetTempPath(), "domiki-webroot-" + Guid.NewGuid().ToString("N"));
        WriteScriptWithCompressedTwins(WorkerPath);
        WriteScriptWithCompressedTwins(AssetPath);
        _host = App.Derive(builder => builder.UseWebRoot(_webRoot));
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _host.Dispose();
        Directory.Delete(_webRoot, true);
    }

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

    /// <summary>
    /// Предсжатый скрипт уходит по HTTP с политикой кэша исходного пути: service worker – no-cache без immutable, хешированный ассет – год immutable; Content-Encoding совпадает с запрошенным, Vary называет Accept-Encoding, Content-Type – text/javascript, а тело разжимается в исходный скрипт.
    /// </summary>
    /// <param name="path">путь запроса к скрипту</param>
    /// <param name="encoding">кодирование в Accept-Encoding и ожидаемое в Content-Encoding</param>
    /// <param name="expectedCacheControl">ожидаемое значение Cache-Control</param>
    [TestCase(WorkerPath, "br", "no-cache")]
    [TestCase(WorkerPath, "gzip", "no-cache")]
    [TestCase(AssetPath, "br", "public, max-age=31536000, immutable")]
    [TestCase(AssetPath, "gzip", "public, max-age=31536000, immutable")]
    public async Task PrecompressedScriptHeadersTest(string path, string encoding, string expectedCacheControl)
    {
        using var client = _host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.AcceptEncoding.ParseAdd(encoding);

        using var response = await client.SendAsync(request);
        var body = await Decompress(response, encoding);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
            Assert.That(response.Headers.CacheControl?.ToString(), Is.EqualTo(expectedCacheControl));
            Assert.That(response.Content.Headers.ContentEncoding, Is.EqualTo(new[] { encoding }));
            Assert.That(response.Headers.Vary, Is.EqualTo(new[] { "Accept-Encoding" }));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/javascript"));
            Assert.That(body, Is.EqualTo(ScriptBody));
        });
    }

    private void WriteScriptWithCompressedTwins(string path)
    {
        var file = Path.Combine(_webRoot, path.TrimStart('/'));
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        var bytes = Encoding.UTF8.GetBytes(ScriptBody);
        File.WriteAllBytes(file, bytes);

        using (var br = new BrotliStream(File.Create(file + ".br"), CompressionLevel.Optimal))
        {
            br.Write(bytes);
        }

        using (var gz = new GZipStream(File.Create(file + ".gz"), CompressionLevel.Optimal))
        {
            gz.Write(bytes);
        }
    }

    private static async Task<string> Decompress(HttpResponseMessage response, string encoding)
    {
        await using var content = await response.Content.ReadAsStreamAsync();
        await using Stream decoder = encoding == "br"
            ? new BrotliStream(content, CompressionMode.Decompress)
            : new GZipStream(content, CompressionMode.Decompress);
        using var reader = new StreamReader(decoder, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }
}
