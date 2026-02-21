// ============================================================
// Middleware/ErrorHandlingMiddleware.cs — Global Exception Handler
//
// MIDDLEWARE PRIMER:
//   Middleware is code that runs in the request pipeline.
//   This one wraps the entire downstream pipeline in a try/catch.
//   Any unhandled exception thrown anywhere (controller, service,
//   EF query) bubbles up here and gets converted to a clean
//   JSON error response instead of crashing or exposing a stack trace.
//
//   The pattern: RequestDelegate represents "everything that comes after
//   this middleware in the pipeline." Calling _next(context) passes
//   the request onward.
// ============================================================

using System.Net;
using System.Text.Json;

namespace MiniFlightPlan.API.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    // RequestDelegate = the next middleware in the pipeline
    // ILogger = injected automatically by DI
    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    // InvokeAsync is called by ASP.NET Core for every request
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Pass the request to the next middleware (eventually hits a controller)
            await _next(context);
        }
        catch (Exception ex)
        {
            // Any unhandled exception lands here
            _logger.LogError(ex, "Unhandled exception processing {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        // Map exception types to HTTP status codes
        // InvalidOperationException → 400 Bad Request (business rule violations)
        // Everything else → 500 Internal Server Error
        var (statusCode, message) = exception switch
        {
            InvalidOperationException => (HttpStatusCode.BadRequest, exception.Message),
            ArgumentException         => (HttpStatusCode.BadRequest, exception.Message),
            _                         => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            error = message,
            statusCode = (int)statusCode
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
    }
}
