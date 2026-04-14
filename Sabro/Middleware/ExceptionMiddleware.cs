using Microsoft.AspNetCore.Mvc;
using Sabro.Exeptions;
using System.Text.Json;

namespace Sabro.Middleware
{
    public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                var correlationId = context.TraceIdentifier;

                logger.LogError(ex, "Unhandled exception. CorrelationId: {CorrelationId}, Path: {Path}",
                    correlationId, context.Request.Path);

                await WriteErrorResponse(context, ex, correlationId);
            }
        }

        private static async Task WriteErrorResponse(HttpContext context, Exception ex, string correlationId)
        {
            var (statusCode, error, details) = ex switch
            {
                NotFoundException e        => (StatusCodes.Status404NotFound,        "Not Found",            (object?)e.Message),
                ValidationException e      => (StatusCodes.Status422UnprocessableEntity, "Validation Failed", e.Errors ?? (object?)e.Message),
                UnauthorizedException e    => (StatusCodes.Status401Unauthorized,    "Unauthorized",         (object?)e.Message),
                ForbiddenException e       => (StatusCodes.Status403Forbidden,       "Forbidden",            (object?)e.Message),
                ConcurrencyException e     => (StatusCodes.Status409Conflict,        "Conflict",             (object?)e.Message),
                _                          => (StatusCodes.Status500InternalServerError, "Internal Server Error", (object?)"An unexpected error occurred.")
            };

            var body = new
            {
                error,
                details,
                timestamp = DateTime.UtcNow,
                path = context.Request.Path.Value,
                correlationId
            };

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(JsonSerializer.Serialize(body, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }
    }
}
