using Domiki.Web.Data;

namespace Domiki.Web
{
    public class UnitOfWorkMiddleware
    {
        private readonly RequestDelegate _next;

        public UnitOfWorkMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        // IMessageWriter is injected into InvokeAsync
        public async Task InvokeAsync(HttpContext httpContext, UnitOfWork uow)
        {
            var originalBody = httpContext.Response.Body;
            using var buffer = new MemoryStream();
            httpContext.Response.Body = buffer;
            try
            {
                await _next(httpContext);
                uow.Commit();
            }
            finally
            {
                httpContext.Response.Body = originalBody;
            }

            buffer.Position = 0;
            await buffer.CopyToAsync(originalBody);
        }
    }
}
