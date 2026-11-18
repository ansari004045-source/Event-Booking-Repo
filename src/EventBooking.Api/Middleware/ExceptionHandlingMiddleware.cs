using System.Text.Json;
using EventBooking.Application.Exceptions;
using FluentValidation;

namespace EventBooking.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await WriteErrorAsync(context, ex);
        }
    }

    private async Task WriteErrorAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail, errors) = exception switch
        {
            ValidationException validation => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                "One or more validation errors occurred.",
                validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()) as IDictionary<string, string[]>),
            AppException app => (
                app.StatusCode,
                GetTitle(app.StatusCode),
                app.Message,
                null),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Unexpected error",
                "An unexpected error occurred.",
                null)
        };

        if (statusCode >= 500)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        else
        {
            logger.LogWarning(exception, "Request failed with {StatusCode} for {Method} {Path}", statusCode, context.Request.Method, context.Request.Path);
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var payload = new Dictionary<string, object?>
        {
            ["type"] = $"https://httpstatuses.com/{statusCode}",
            ["title"] = title,
            ["status"] = statusCode,
            ["detail"] = detail,
            ["traceId"] = context.TraceIdentifier
        };

        if (errors is not null)
        {
            payload["errors"] = errors;
        }

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }

    private static string GetTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Bad request",
        StatusCodes.Status404NotFound => "Not found",
        StatusCodes.Status409Conflict => "Conflict",
        _ => "Request failed"
    };
}
