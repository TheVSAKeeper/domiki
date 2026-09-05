using Microsoft.AspNetCore.Diagnostics;

namespace Domiki.Web.Infrastructure;

public class BusinessExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<BusinessExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BusinessException)
        {
            return false;
        }

        logger.LogInformation("Бизнес-отказ {Path}: {Message}", httpContext.Request.Path, exception.Message);
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = StatusCodes.Status400BadRequest,
                Detail = exception.Message,
            },
        });
    }
}
