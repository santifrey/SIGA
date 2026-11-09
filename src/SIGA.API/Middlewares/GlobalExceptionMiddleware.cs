using System.Net;
using System.Text.Json;
using SIGA.Application.Common;

namespace SIGA.API.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
            _logger.LogError(ex, "Error no controlado interceptado por el middleware global: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = exception switch
        {
            NotFoundException notFoundEx => ApiResponse<object>.Failure(
                notFoundEx.Message, 
                notFoundEx.StatusCode, 
                notFoundEx.Errors),

            ValidationException valEx => ApiResponse<object>.Failure(
                valEx.Message, 
                valEx.StatusCode, 
                valEx.Errors),

            UnauthorizedException unauthEx => ApiResponse<object>.Failure(
                unauthEx.Message, 
                unauthEx.StatusCode, 
                unauthEx.Errors),

            AppException appEx => ApiResponse<object>.Failure(
                appEx.Message, 
                appEx.StatusCode, 
                appEx.Errors),

            _ => ApiResponse<object>.Failure(
                "Ocurrió un error interno en el servidor.", 
                (int)HttpStatusCode.InternalServerError, 
                new List<string> { exception.Message })
        };

        context.Response.StatusCode = response.StatusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}
