using FluentValidation;
using System.Net;
using System.Text.Json;

namespace Jobuler.Api.Middleware;

/// <summary>
/// Converts unhandled exceptions into consistent JSON error responses.
/// Prevents stack traces from leaking to clients in production.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, message, errors) = ex switch
        {
            ValidationException ve => (
                HttpStatusCode.BadRequest,
                "Validation failed.",
                ve.Errors.Select(e => e.ErrorMessage).ToList()),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, ex.Message, (List<string>?)[]),
            KeyNotFoundException        => (HttpStatusCode.NotFound, ex.Message, (List<string>?)[]),
            InvalidOperationException   => (HttpStatusCode.BadRequest, ex.Message, (List<string>?)[]),
            ArgumentException           => (HttpStatusCode.BadRequest, ex.Message, (List<string>?)[]),
            _                           => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", (List<string>?)[])
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(ex, "Unhandled exception");
        else
            _logger.LogWarning(ex, "Handled exception: {Message}", ex.Message);

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var body = JsonSerializer.Serialize(new
        {
            error = message,
            errors = errors?.Count > 0 ? errors : null
        });
        await context.Response.WriteAsync(body);
    }
}
