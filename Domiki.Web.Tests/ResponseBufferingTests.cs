using System.Text;
using Domiki.Web;
using Microsoft.AspNetCore.Http;

namespace Domiki.Web.Tests
{
    public class ResponseBufferingTests : TestBase
    {
        [Test]
        public async Task SuccessfulRequestFlushesBufferedBody()
        {
            using var uow = GetUow();
            var context = new DefaultHttpContext();
            var realBody = new MemoryStream();
            context.Response.Body = realBody;

            var middleware = new UnitOfWorkMiddleware(async ctx => await ctx.Response.WriteAsync("hello"));

            await middleware.InvokeAsync(context, uow);

            Assert.That(Encoding.UTF8.GetString(realBody.ToArray()), Is.EqualTo("hello"));
        }

        [Test]
        public void FailingRequestDoesNotFlushPartialBody()
        {
            using var uow = GetUow();
            var context = new DefaultHttpContext();
            var realBody = new MemoryStream();
            context.Response.Body = realBody;

            var middleware = new UnitOfWorkMiddleware(async ctx =>
            {
                await ctx.Response.WriteAsync("partial");
                throw new InvalidOperationException("boom");
            });

            Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context, uow));
            Assert.That(realBody.ToArray(), Is.Empty);
        }
    }
}
