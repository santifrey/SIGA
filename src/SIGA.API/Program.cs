using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using SIGA.API.Middlewares;
using SIGA.Application;
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

// 4. Cabeceras ForwardedHeaders para balanceadores HTTPS de Google Cloud
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// 5. Configuración de CORS para Frontend
var configuredOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            if (string.IsNullOrEmpty(origin) || origin == "null") return true;
            if (configuredOrigins.Length > 0 && configuredOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
            if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
            {
                return uri.Host == "localhost" || uri.Host == "127.0.0.1" || origin.StartsWith("file://");
            }
            return builder.Environment.IsDevelopment();
        })
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

var app = builder.Build();

app.UseForwardedHeaders();

// 6. Middleware Global de Excepciones
app.UseMiddleware<GlobalExceptionMiddleware>();

// 7. Pipeline HTTP
app.UseRouting();

app.UseCors("AllowFrontend");

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

// 8. Inicialización y Siembra de Base de Datos
await DatabaseSeeder.InitializeDatabaseAsync(app.Services);

app.Run();

public partial class Program { }
