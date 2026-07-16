using Domiki.Web.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;

namespace Domiki.Web.Tests;

public sealed class BusinessExceptionHandlerTests
{
    /// <summary>
    /// BusinessException отдаётся клиенту как 400 ProblemDetails с текстом исключения в поле detail.
    /// </summary>
    [Test]
    public async Task BusinessExceptionIsWrittenAsProblemDetails()
    {
        var services = new ServiceCollection();
        services.AddProblemDetails();
        services.AddLogging();
        var provider = services.BuildServiceProvider();

        var context = new DefaultHttpContext();
        var body = new MemoryStream();
        context.Response.Body = body;
        context.RequestServices = provider;
        context.Request.Headers.Accept = "application/json";

        var handled = await new BusinessExceptionHandler(provider.GetRequiredService<IProblemDetailsService>())
            .TryHandleAsync(context, new BusinessException("Не хватает монет"), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(handled, Is.True);
            Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        }

        using var json = JsonDocument.Parse(Encoding.UTF8.GetString(body.ToArray()));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(json.RootElement.GetProperty("status").GetInt32(), Is.EqualTo(StatusCodes.Status400BadRequest));
            Assert.That(json.RootElement.GetProperty("detail").GetString(), Is.EqualTo("Не хватает монет"));
        }
    }

    /// <summary>
    /// Обработчик берёт на себя только BusinessException, остальные исключения не перехватывает и оставляет дальше по
    /// конвейеру.
    /// </summary>
    [Test]
    public async Task OtherExceptionsAreNotHandled()
    {
        var services = new ServiceCollection();
        services.AddProblemDetails();
        services.AddLogging();
        var provider = services.BuildServiceProvider();

        var handled = await new BusinessExceptionHandler(provider.GetRequiredService<IProblemDetailsService>())
            .TryHandleAsync(new DefaultHttpContext(), new InvalidOperationException("boom"), CancellationToken.None);

        Assert.That(handled, Is.False);
    }
}
