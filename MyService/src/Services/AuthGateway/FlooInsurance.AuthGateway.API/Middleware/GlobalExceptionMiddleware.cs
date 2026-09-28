using System.Net;
using System.Text.Json;
using FlooInsurance.AuthGateway.Application.Common.Exceptions;
using FlooInsurance.AuthGateway.Application.DTOs.Common;

namespace FlooInsurance.AuthGateway.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
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

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;
        var statusCode = HttpStatusCode.InternalServerError;
        var message = "An unexpected error occurred. Please contact system support.";
        IDictionary<string, string[]>? errors = null;

        switch (exception)
        {
            case ValidationException validationException:
                statusCode = HttpStatusCode.BadRequest;
                message = validationException.Message;
                errors = validationException.Errors;
                _logger.LogWarning("Validation failure for trace {TraceId}: {Message}", traceId, message);
                break;

            case AuthenticationException authException:
                statusCode = HttpStatusCode.Unauthorized;
                message = authException.Message;
                _logger.LogWarning("Authentication failed for trace {TraceId}: {Message}", traceId, message);
                break;

            case AuthorizationException authzException:
                statusCode = HttpStatusCode.Forbidden;
                message = authzException.Message;
                _logger.LogWarning("Authorization failed for trace {TraceId}: {Message}", traceId, message);
                break;

            case NotFoundException notFoundException:
                statusCode = HttpStatusCode.NotFound;
                message = notFoundException.Message;
                _logger.LogInformation("Resource not found for trace {TraceId}: {Message}", traceId, message);
                break;

            case ConflictException conflictException:
                statusCode = HttpStatusCode.Conflict;
                message = conflictException.Message;
                _logger.LogWarning("Conflict detected for trace {TraceId}: {Message}", traceId, message);
                break;

            case TokenException tokenException:
                statusCode = HttpStatusCode.Unauthorized;
                message = tokenException.Message;
                _logger.LogWarning("Token security error for trace {TraceId}: {Message}", traceId, message);
                break;

            default:
                _logger.LogError(exception, "Unhandled exception occurred during request with trace {TraceId}: {Message}", traceId, exception.Message);
                if (_environment.IsDevelopment())
                {
                    // Include diagnostic message in development only, never in production
                    message = exception.Message;
                }
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var responseDto = new ErrorResponseDto((int)statusCode, message, traceId, errors);
        var json = JsonSerializer.Serialize(responseDto, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
