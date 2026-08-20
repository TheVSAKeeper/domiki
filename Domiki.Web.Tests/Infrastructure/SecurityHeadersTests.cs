using Domiki.Web.Infrastructure;

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

    /// <summary>
    /// Директива form-action пускает origin внешнего провайдера входа, а при пустом или относительном адресе остаётся при 'self'.
    /// </summary>
    /// <param name="oidcAuthority">настройка Oidc:Authority</param>
    /// <param name="expected">ожидаемая директива в политике</param>
    [TestCase("https://auth.keep2space.ru/", "form-action 'self' https://auth.keep2space.ru;")]
    [TestCase("https://auth.keep2space.ru:8443/realms/keep", "form-action 'self' https://auth.keep2space.ru:8443;")]
    [TestCase("auth.keep2space.ru", "form-action 'self';")]
    [TestCase("", "form-action 'self';")]
    [TestCase(null, "form-action 'self';")]
    public void FormActionAllowsOidcAuthorityTest(string? oidcAuthority, string expected)
    {
        var policy = SecurityHeadersMiddleware.BuildContentSecurityPolicy(oidcAuthority);

        Assert.That(policy, Does.Contain(expected));
    }
}
