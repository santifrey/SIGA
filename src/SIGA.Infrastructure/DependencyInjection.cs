using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SIGA.Application.Interfaces;
using SIGA.Domain.Interfaces;
using SIGA.Infrastructure.Persistence;
using SIGA.Infrastructure.Persistence.Repositories;
using SIGA.Infrastructure.Security;

namespace SIGA.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // 1. Configuración de Base de Datos (MySQL)
        var mySqlConnection = ResolveMySqlConnectionString(configuration);

        services.AddDbContext<SigaDbContext>(options =>
        {
            ServerVersion serverVersion;
            try
            {
                serverVersion = ServerVersion.AutoDetect(mySqlConnection);
            }
            catch
            {
                // Fallback a versión estándar si el servidor MySQL no responde durante la configuración
                serverVersion = new MySqlServerVersion(new Version(8, 0, 36));
            }

            options.UseMySql(mySqlConnection, serverVersion);
        });

        // 2. Servicios de Seguridad y Contexto
        services.AddHttpContextAccessor();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<ICurrentUserService, SIGA.Infrastructure.Services.CurrentUserService>();

        // 3. Repositorios y Unit of Work
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IPersonaRepository, PersonaRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<ISucursalRepository, SucursalRepository>();
        services.AddScoped<IRolRepository, RolRepository>();
        services.AddScoped<IMovimientoBulbosRepository, MovimientoBulbosRepository>();
        services.AddScoped<ITemporadaRepository, TemporadaRepository>();
        services.AddScoped<IFilaRepository, FilaRepository>();
        services.AddScoped<ILoteRepository, LoteRepository>();
        services.AddScoped<IPlantacionRepository, PlantacionRepository>();
        services.AddScoped<ITransferenciaLoteRepository, TransferenciaLoteRepository>();
        services.AddScoped<ICosechaRepository, CosechaRepository>();
        services.AddScoped<IPrecioClaseRepository, PrecioClaseRepository>();
        services.AddScoped<IMovimientoComercialRepository, MovimientoComercialRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    private static string ResolveMySqlConnectionString(IConfiguration configuration)
    {
        // 1. Cadena de conexión completa desde variables de entorno o appsettings
        var explicitConn = Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING")
            ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? configuration.GetConnectionString("MySqlConnection");

        // 2. Parámetros individuales desde variables de entorno (.env o contenedor)
        var dbHost = Environment.GetEnvironmentVariable("DB_HOST") 
            ?? Environment.GetEnvironmentVariable("MYSQL_HOST");
        var dbPort = Environment.GetEnvironmentVariable("DB_PORT") 
            ?? Environment.GetEnvironmentVariable("MYSQL_PORT") 
            ?? "3306";
        var dbName = Environment.GetEnvironmentVariable("DB_DATABASE") 
            ?? Environment.GetEnvironmentVariable("DB_NAME") 
            ?? Environment.GetEnvironmentVariable("MYSQL_DATABASE") 
            ?? "siga_db";
        var dbUser = Environment.GetEnvironmentVariable("DB_USER") 
            ?? Environment.GetEnvironmentVariable("MYSQL_USER");
        var dbPass = Environment.GetEnvironmentVariable("DB_PASSWORD") 
            ?? Environment.GetEnvironmentVariable("MYSQL_PASSWORD");

        if (!string.IsNullOrWhiteSpace(dbHost) && !string.IsNullOrWhiteSpace(dbUser))
        {
            return $"Server={dbHost};Port={dbPort};Database={dbName};User={dbUser};Password={dbPass};";
        }

        if (!string.IsNullOrWhiteSpace(explicitConn))
        {
            return explicitConn;
        }

        return "Server=localhost;Port=3306;Database=siga_db;User=root;Password=root;";
    }
}
