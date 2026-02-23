using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace PennaiWise.Api.Middleware;

/// <summary>
/// Catches any unhandled exception and returns a consistent RFC-7807 ProblemDetails
/// JSON response instead of exposing stack traces.
/// </summary>
internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(
            exception,
            "Unhandled exception on {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        var (statusCode, title) = exception switch
        {
            BadHttpRequestException bad => (bad.StatusCode, "Bad request"),
            OperationCanceledException  => (StatusCodes.Status499ClientClosedRequest, "Request cancelled"),
            _                           => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        var problem = new ProblemDetails
        {
            Status   = statusCode,
            Title    = title,
            Detail   = httpContext.RequestServices
                           .GetRequiredService<IHostEnvironment>()
                           .IsDevelopment()
                       ? exception.ToString()
                       : null,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode  = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
