using System.Net;
using System.Text.Json;
using SIGA.Application.Common;
using SIGA.Domain.Exceptions;

namespace SIGA.API.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error interceptado por el middleware global: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
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

            StockInsuficienteException stockEx => ApiResponse<object>.Failure(
                stockEx.Message,
                (int)HttpStatusCode.Conflict,
                new List<string> { stockEx.Message }),

            OperacionInvalidaException opEx => ApiResponse<object>.Failure(
                opEx.Message,
                (int)HttpStatusCode.BadRequest,
                new List<string> { opEx.Message }),

            DomainException domainEx => ApiResponse<object>.Failure(
                domainEx.Message,
                (int)HttpStatusCode.BadRequest,
                new List<string> { domainEx.Message }),

            _ => ApiResponse<object>.Failure(
                "Ocurrió un error interno en el servidor.",
                (int)HttpStatusCode.InternalServerError,
                _env.IsDevelopment() ? new List<string> { exception.Message } : null)
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
