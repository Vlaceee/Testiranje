using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ForgeMart.Api.Infrastructure;

public sealed partial class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var apiException = exception as ApiException;
        var status = apiException?.StatusCode ?? StatusCodes.Status500InternalServerError;

        if (status >= 500)
        {
            LogUnhandledException(logger, httpContext.Request.Path, exception);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = apiException?.Title ?? "Unexpected server error",
                Detail = apiException?.Message ?? DiagnosticDetail(exception, environment),
                Instance = httpContext.Request.Path
            },
            Exception = exception
        });
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Unhandled exception while processing {Path}")]
    private static partial void LogUnhandledException(ILogger logger, string path, Exception exception);

    private static string DiagnosticDetail(Exception exception, IHostEnvironment environment)
    {
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
        {
            return "An unexpected error occurred.";
        }

        if (environment.IsEnvironment("Testing") && exception is DbUpdateConcurrencyException concurrency)
        {
            var entries = string.Join(", ", concurrency.Entries.Select(entry =>
                $"{entry.Metadata.ClrType.Name}:{entry.State}"));
            return $"{exception.Message} Entries: {entries}.";
        }

        return exception.Message;
    }
}
