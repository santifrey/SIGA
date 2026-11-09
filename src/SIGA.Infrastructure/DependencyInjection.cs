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
        // 1. Configuración de Base de Datos (SQL Server o fallback a SQLite)
        var useSqlite = configuration.GetValue<bool>("UseSqlite", true);
        var sqliteConnection = configuration.GetConnectionString("SqliteConnection") 
            ?? "Data Source=siga_dev.db";
        var sqlServerConnection = configuration.GetConnectionString("SqlServerConnection") 
            ?? "Server=(localdb)\\mssqllocaldb;Database=SIGA_Db;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        services.AddDbContext<SigaDbContext>(options =>
        {
            if (useSqlite)
            {
                options.UseSqlite(sqliteConnection);
            }
            else
            {
                options.UseSqlServer(sqlServerConnection);
            }
        });

        // 2. Servicios de Seguridad
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();

        // 3. Repositorios y Unit of Work
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IPersonaRepository, PersonaRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<ISucursalRepository, SucursalRepository>();
        services.AddScoped<IRolRepository, RolRepository>();
        services.AddScoped<IMovimientoBulbosRepository, MovimientoBulbosRepository>();
        services.AddScoped<IOperacionComercialRepository, OperacionComercialRepository>();
        services.AddScoped<ITemporadaRepository, TemporadaRepository>();
        services.AddScoped<IFilaRepository, FilaRepository>();
        services.AddScoped<ILoteRepository, LoteRepository>();
        services.AddScoped<IPlantacionRepository, PlantacionRepository>();
        services.AddScoped<ITransferenciaLoteRepository, TransferenciaLoteRepository>();
        services.AddScoped<ICosechaRepository, CosechaRepository>();
        services.AddScoped<IPrecioClaseRepository, PrecioClaseRepository>();
        services.AddScoped<IVentaRepository, VentaRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
