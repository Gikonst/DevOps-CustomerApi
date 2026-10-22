using Microsoft.AspNetCore.Diagnostics;

namespace CustomerApi.ExceptionHandling;

// Malformed request bodies are client errors; without this the exception handler reports them as 500.
public sealed class BadHttpRequestExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BadHttpRequestException badRequest)
        {
            return false;
        }

        httpContext.Response.StatusCode = badRequest.StatusCode;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception
        });
    }
}
