using System.Text;
using System.Text.Json;
using Domiki.Web;
using Domiki.Web.Business.Core;
using Microsoft.AspNetCore.Http;

namespace Domiki.Web.Tests
{
    public class BusinessExceptionHandlerTests
    {
        [Test]
        public async Task BusinessExceptionIsWrittenAsErrorEnvelope()
        {
            var context = new DefaultHttpContext();
            var body = new MemoryStream();
            context.Response.Body = body;

            var handled = await new BusinessExceptionHandler()
                .TryHandleAsync(context, new BusinessException("Не хватает монет"), CancellationToken.None);

            Assert.That(handled, Is.True);
            Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));

            using var json = JsonDocument.Parse(Encoding.UTF8.GetString(body.ToArray()));
            Assert.That(json.RootElement.GetProperty("type").GetInt32(), Is.EqualTo((int)ResponseType.ErrorMessage));
            Assert.That(json.RootElement.GetProperty("content").GetString(), Is.EqualTo("Не хватает монет"));
        }

        [Test]
        public async Task OtherExceptionsAreNotHandled()
        {
            var handled = await new BusinessExceptionHandler()
                .TryHandleAsync(new DefaultHttpContext(), new InvalidOperationException("boom"), CancellationToken.None);

            Assert.That(handled, Is.False);
        }
    }
}
