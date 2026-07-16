using Microsoft.AspNetCore.Diagnostics;

namespace Domiki.Web.Infrastructure;

public class BusinessExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BusinessException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(new Response<string>(exception.Message) { Type = ResponseType.ErrorMessage },
            cancellationToken);

        return true;
    }
}
