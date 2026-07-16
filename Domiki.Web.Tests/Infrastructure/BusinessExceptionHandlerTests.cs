using Domiki.Web.Infrastructure;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Text;

namespace Domiki.Web.Tests
{
    public class BusinessExceptionHandlerTests
    {
        /// <summary>
        /// BusinessException отдаётся клиенту как 400 с телом-конвертом { type = ErrorMessage, content = текст исключения }.
        /// </summary>
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

        /// <summary>
        /// Обработчик берёт на себя только BusinessException, остальные исключения не перехватывает и оставляет дальше по конвейеру.
        /// </summary>
        [Test]
        public async Task OtherExceptionsAreNotHandled()
        {
            var handled = await new BusinessExceptionHandler()
                .TryHandleAsync(new DefaultHttpContext(), new InvalidOperationException("boom"), CancellationToken.None);

            Assert.That(handled, Is.False);
        }
    }
}
