using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Interfaces;

namespace SIGA.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SigaDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider.GetService<ILogger<SigaDbContext>>();

        try
        {
            await context.Database.EnsureCreatedAsync();

            // 1. Verificar si existe al menos un usuario con rol ADMINISTRADOR
            var existeAdministrador = await context.UsuarioRoles
                .Include(ur => ur.Rol)
                .Include(ur => ur.Usuario)
                .AnyAsync(ur => ur.Rol.Nombre == "ADMINISTRADOR" && !ur.Usuario.IsDeleted);

            if (!existeAdministrador)
            {
                var bootstrapUsername = configuration["BootstrapAdmin:Username"];
                var bootstrapPassword = configuration["BootstrapAdmin:Password"];

                if (!string.IsNullOrWhiteSpace(bootstrapUsername) && !string.IsNullOrWhiteSpace(bootstrapPassword))
                {
                    logger?.LogInformation("Aprovisionando usuario administrador inicial ('{Username}') desde configuración/variables de entorno...", bootstrapUsername);

                    var doc = configuration["BootstrapAdmin:Documento"] ?? "00000000";
                    var nombre = configuration["BootstrapAdmin:Nombre"] ?? "Administrador";
                    var apellido = configuration["BootstrapAdmin:Apellido"] ?? "SIGA";
                    var email = configuration["BootstrapAdmin:Email"] ?? "admin@azafranmediterraneo.com";
                    var telefono = configuration["BootstrapAdmin:Telefono"] ?? "+54 351 0000000";

                    var adminPersona = await context.Personas
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(p => p.Documento == doc);

                    if (adminPersona == null)
                    {
                        adminPersona = new Persona
                        {
                            Nombre = nombre,
                            Apellido = apellido,
                            Documento = doc,
                            Telefono = telefono,
                            Email = email,
                            FechaCreacion = DateTime.UtcNow
                        };
                        await context.Personas.AddAsync(adminPersona);
                        await context.SaveChangesAsync();
                    }

                    var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Nombre == "ADMINISTRADOR");
                    if (adminRole == null)
                    {
                        adminRole = new Rol
                        {
                            Nombre = "ADMINISTRADOR",
                            Descripcion = "Acceso total a las funciones del sistema"
                        };
                        await context.Roles.AddAsync(adminRole);
                        await context.SaveChangesAsync();
                    }

                    var adminUser = new Usuario
                    {
                        PersonaId = adminPersona.Id,
                        Username = bootstrapUsername.Trim(),
                        PasswordHash = passwordHasher.HashPassword(bootstrapPassword),
                        Estado = EstadoUsuario.Activo,
                        FechaCreacion = DateTime.UtcNow
                    };

                    adminUser.UsuarioRoles.Add(new UsuarioRol
                    {
                        Usuario = adminUser,
                        RolId = adminRole.Id,
                        FechaAsignacion = DateTime.UtcNow
                    });

                    await context.Usuarios.AddAsync(adminUser);
                    await context.SaveChangesAsync();
                    logger?.LogInformation("Usuario administrador inicial creado exitosamente.");
                }
                else
                {
                    logger?.LogWarning(
                        "No existe ningún usuario ADMINISTRADOR en la base de datos y no se definieron las variables 'BootstrapAdmin__Username' / 'BootstrapAdmin__Password'.");
                }
            }

            // 2. Sembrar sucursal de ejemplo únicamente si está habilitado en configuración (entorno Development) y no hay sucursales
            var seedSampleSucursal = configuration.GetValue<bool>("SeedSampleSucursal", false);
            if (seedSampleSucursal && !await context.Sucursales.IgnoreQueryFilters().AnyAsync())
            {
                await context.Sucursales.AddAsync(new Sucursal
                {
                    Nombre = "Sede Central - Villa General Belgrano",
                    Ubicacion = "Ruta Prov. 5, Villa General Belgrano, Córdoba",
                    CapacidadTotal = 50000m,
                    Estado = EstadoSucursal.Activo,
                    FechaCreacion = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Ocurrió un error inicializando o sembrando la base de datos.");
            throw;
        }
    }
}
