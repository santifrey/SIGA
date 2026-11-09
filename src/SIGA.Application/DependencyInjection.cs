using Microsoft.Extensions.DependencyInjection;
using SIGA.Application.Interfaces;
using SIGA.Application.Services;

namespace SIGA.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IGestionUsuarios, GestionUsuarios>();
        services.AddScoped<IGestionPersonas, GestionPersonas>();
        services.AddScoped<IGestionClientes, GestionClientes>();
        services.AddScoped<IGestionSucursales, GestionSucursales>();
        services.AddScoped<IGestionStock, GestionStock>();
        services.AddScoped<IGestionMovimientos, GestionMovimientos>();
        services.AddScoped<IGestionTemporadas, GestionTemporadas>();
        services.AddScoped<IGestionPlantaciones, GestionPlantaciones>();
        services.AddScoped<IGestionLotes, GestionLotes>();
        services.AddScoped<IGestionFilas, GestionFilas>();
        services.AddScoped<IGestionTransferencias, GestionTransferencias>();
        services.AddScoped<IGestionCosechas, GestionCosechas>();
        services.AddScoped<IGestionVentas, GestionVentas>();
        services.AddScoped<IGestionReportes, GestionReportes>();

        return services;
    }
}
