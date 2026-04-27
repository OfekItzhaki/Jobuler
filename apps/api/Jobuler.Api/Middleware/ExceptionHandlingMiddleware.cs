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
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, "You do not have permission to perform this action.", (List<string>?)[]),
            KeyNotFoundException        => (HttpStatusCode.NotFound, ex.Message, (List<string>?)[]),
            Jobuler.Application.Common.ConflictException => (HttpStatusCode.Conflict, ex.Message, (List<string>?)[]),
            InvalidOperationException   => (HttpStatusCode.BadRequest, ex.Message, (List<string>?)[]),
            ArgumentException           => (HttpStatusCode.BadRequest, ex.Message, (List<string>?)[]),
            // EF unique constraint violations → 409 Conflict
            Microsoft.EntityFrameworkCore.DbUpdateException dbe when dbe.InnerException?.Message.Contains("unique") == true ||
                dbe.InnerException?.Message.Contains("23505") == true ||
                dbe.InnerException?.Message.Contains("duplicate key") == true
                => (HttpStatusCode.Conflict, "A record with this name or identifier already exists.", (List<string>?)[]),
            // All other EF/DB exceptions → 500, never expose DB internals to client
            Microsoft.EntityFrameworkCore.DbUpdateException
                => (HttpStatusCode.InternalServerError, "A database error occurred. Please try again.", (List<string>?)[]),
            _   => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", (List<string>?)[])
        };

        // Always log the full exception server-side
        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
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
