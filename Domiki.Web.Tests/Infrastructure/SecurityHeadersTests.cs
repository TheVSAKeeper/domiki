namespace Domiki.Web.Tests;

public sealed class SecurityHeadersTests
{
    /// <summary>
    /// Каждый ответ несёт защитные заголовки nosniff, DENY, строгий Referrer-Policy и запрет камеры/микрофона/геолокации.
    /// </summary>
    /// <param name="header">имя заголовка ответа</param>
    /// <param name="expected">ожидаемое значение заголовка</param>
    [TestCase("X-Content-Type-Options", "nosniff")]
    [TestCase("X-Frame-Options", "DENY")]
    [TestCase("Referrer-Policy", "strict-origin-when-cross-origin")]
    [TestCase("Permissions-Policy", "camera=(), microphone=(), geolocation=()")]
    public async Task ResponseCarriesSecurityHeaderTest(string header, string expected)
    {
        var client = App.Client();

        var response = await client.GetAsync("/healthz");

        Assert.That(response.Headers.GetValues(header), Does.Contain(expected));
    }
}
