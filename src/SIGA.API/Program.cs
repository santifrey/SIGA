using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using SIGA.API.Middlewares;
using SIGA.Application;
using SIGA.Application.Common;
using SIGA.Infrastructure;
using SIGA.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// 0. Soporte de puerto dinámico para Google Cloud Run / App Engine (variable PORT)
var gcpPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(gcpPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{gcpPort}");
}

// 1. Configuración de Controladores y Serialización JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// 2. Inyección de dependencias de Capas Application e Infrastructure
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// 3. Configuración de Autenticación con JWT Bearer (sin claves hardcodeadas)
var jwtSecretKey = builder.Configuration["JwtSettings:SecretKey"]
    ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY");

if (string.IsNullOrWhiteSpace(jwtSecretKey) || jwtSecretKey.Length < 32)
{
    throw new InvalidOperationException(
        "Configuración inválida: Debe definir 'JwtSettings:SecretKey' (en appsettings.Development.json para entorno local o mediante la variable de entorno 'JwtSettings__SecretKey' en GCP) con al menos 32 caracteres.");
}

var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "SIGA.API";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "SIGA.Frontend";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 4. Rate Limiting nativo para proteger /api/v1/auth/login contra fuerza bruta
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        var resp = ApiResponse<object>.Failure(
            "Demasiados intentos. Por favor aguarde un minuto antes de volver a intentar.",
            StatusCodes.Status429TooManyRequests);
        await context.HttpContext.Response.WriteAsJsonAsync(resp, cancellationToken: token);
    };

    options.AddPolicy("LoginPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

// 5. Cabeceras ForwardedHeaders para balanceadores HTTPS de Google Cloud
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// 6. Configuración de CORS (estricta en Producción, permisiva para localhost sólo en Development)
var configuredOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

var isDevelopment = builder.Environment.IsDevelopment();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            if (!string.IsNullOrEmpty(origin) &&
                configuredOrigins.Length > 0 &&
                configuredOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            if (isDevelopment)
            {
                if (string.IsNullOrEmpty(origin) || origin == "null") return true;
                if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                {
                    return uri.Host == "localhost" || uri.Host == "127.0.0.1" || origin.StartsWith("file://");
                }
            }

            return false;
        })
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

var app = builder.Build();

app.UseForwardedHeaders();

// 7. Middleware Global de Excepciones
app.UseMiddleware<GlobalExceptionMiddleware>();

// 8. Pipeline HTTP
app.UseRouting();

app.UseCors("AllowFrontend");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Endpoint de verificación de salud para Google Cloud Run / App Engine
app.MapGet("/api/v1/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "SIGA.API",
    environment = app.Environment.EnvironmentName,
    timestamp = DateTime.UtcNow
}));

// 9. Inicialización y Siembra de Base de Datos
await DatabaseSeeder.InitializeDatabaseAsync(app.Services);

app.Run();

public partial class Program { }
