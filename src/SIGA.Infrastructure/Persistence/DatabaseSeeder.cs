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
    public static readonly List<Permiso> CatalogoPermisos = new()
    {
        // Módulo Seguridad
        new Permiso { Codigo = "ROLES_VER", Nombre = "Ver Roles", Descripcion = "Permite visualizar el catálogo de roles del sistema", Modulo = "Seguridad" },
        new Permiso { Codigo = "ROLES_GESTIONAR", Nombre = "Gestionar Roles", Descripcion = "Permite crear, modificar y eliminar roles y asignar permisos", Modulo = "Seguridad" },
        new Permiso { Codigo = "PERMISOS_VER", Nombre = "Ver Permisos", Descripcion = "Permite consultar el catálogo completo de permisos del sistema", Modulo = "Seguridad" },

        // Módulo Personal
        new Permiso { Codigo = "EMPLEADOS_VER", Nombre = "Ver Empleados", Descripcion = "Permite consultar el listado y detalle de empleados", Modulo = "Personal" },
        new Permiso { Codigo = "EMPLEADOS_GESTIONAR", Nombre = "Gestionar Empleados", Descripcion = "Permite crear, modificar y dar de baja empleados", Modulo = "Personal" },

        // Módulo Clientes
        new Permiso { Codigo = "CLIENTES_VER", Nombre = "Ver Clientes", Descripcion = "Permite consultar el listado y detalle de clientes", Modulo = "Clientes" },
        new Permiso { Codigo = "CLIENTES_GESTIONAR", Nombre = "Gestionar Clientes", Descripcion = "Permite registrar, actualizar y dar de baja clientes", Modulo = "Clientes" },

        // Módulo Sucursales
        new Permiso { Codigo = "SUCURSALES_VER", Nombre = "Ver Sucursales", Descripcion = "Permite consultar sucursales registradas", Modulo = "Sucursales" },
        new Permiso { Codigo = "SUCURSALES_GESTIONAR", Nombre = "Gestionar Sucursales", Descripcion = "Permite crear y actualizar sucursales", Modulo = "Sucursales" },

        // Módulo Stock
        new Permiso { Codigo = "STOCK_VER", Nombre = "Ver Stock", Descripcion = "Permite consultar existencias de bulbos y disponibilidad", Modulo = "Stock" },
        new Permiso { Codigo = "STOCK_AJUSTAR", Nombre = "Ajustar Stock", Descripcion = "Permite registrar movimientos manuales de bulbos y ajustes de inventario", Modulo = "Stock" },

        // Módulo Comercial
        new Permiso { Codigo = "MOVCOM_VER", Nombre = "Ver Movimientos Comerciales", Descripcion = "Permite listar y ver detalle de ventas, compras y traspasos", Modulo = "Comercial" },
        new Permiso { Codigo = "MOVCOM_CREAR", Nombre = "Crear Movimiento Comercial", Descripcion = "Permite registrar nuevas operaciones de venta, compra y traspaso", Modulo = "Comercial" },
        new Permiso { Codigo = "MOVCOM_APROBAR", Nombre = "Aprobar Movimiento Comercial", Descripcion = "Permite dar aprobación administrativa y generar contratos", Modulo = "Comercial" },
        new Permiso { Codigo = "MOVCOM_FACTURAR", Nombre = "Facturar Movimiento Comercial", Descripcion = "Permite emitir y registrar facturas comerciales", Modulo = "Comercial" },
        new Permiso { Codigo = "MOVCOM_FINALIZAR", Nombre = "Finalizar Movimiento Comercial", Descripcion = "Permite dar aprobación final e impactar stock físico de bulbos", Modulo = "Comercial" },
        new Permiso { Codigo = "MOVCOM_RECHAZAR", Nombre = "Rechazar Movimiento Comercial", Descripcion = "Permite observar y rechazar operaciones comerciales con motivo obligatorio", Modulo = "Comercial" },
        new Permiso { Codigo = "MOVCOM_CORREGIR", Nombre = "Corregir Movimiento Rechazado", Descripcion = "Permite editar y reenviar a revisión movimientos rechazados", Modulo = "Comercial" },
        new Permiso { Codigo = "PRECIOS_REF_VER", Nombre = "Ver Precios de Referencia", Descripcion = "Permite consultar precios de referencia por calibre y temporada", Modulo = "Comercial" },
        new Permiso { Codigo = "PRECIOS_REF_GESTIONAR", Nombre = "Gestionar Precios de Referencia", Descripcion = "Permite establecer y actualizar precios de referencia de bulbos", Modulo = "Comercial" },

        // Módulo Producción
        new Permiso { Codigo = "TEMPORADAS_VER", Nombre = "Ver Temporadas", Descripcion = "Permite consultar las temporadas productivas", Modulo = "Producción" },
        new Permiso { Codigo = "TEMPORADAS_GESTIONAR", Nombre = "Gestionar Temporadas", Descripcion = "Permite crear y gestionar temporadas agrícolas", Modulo = "Producción" },
        new Permiso { Codigo = "PLANTACIONES_VER", Nombre = "Ver Plantaciones", Descripcion = "Permite consultar siembras, filas y lotes de campo", Modulo = "Producción" },
        new Permiso { Codigo = "PLANTACIONES_GESTIONAR", Nombre = "Gestionar Plantaciones", Descripcion = "Permite registrar plantaciones y distribuciones en lotes", Modulo = "Producción" },
        new Permiso { Codigo = "TRANSFERENCIAS_VER", Nombre = "Ver Transferencias de Lotes", Descripcion = "Permite consultar transferencias entre sucursales y clientes", Modulo = "Producción" },
        new Permiso { Codigo = "TRANSFERENCIAS_GESTIONAR", Nombre = "Gestionar Transferencias de Lotes", Descripcion = "Permite registrar y gestionar transferencias de lotes de campo", Modulo = "Producción" },
        new Permiso { Codigo = "COSECHAS_VER", Nombre = "Ver Cosechas", Descripcion = "Permite consultar cosechas registradas y rendimientos", Modulo = "Producción" },
        new Permiso { Codigo = "COSECHAS_GESTIONAR", Nombre = "Gestionar Cosechas", Descripcion = "Permite registrar pesajes, calibres y liquidaciones de cosecha", Modulo = "Producción" },

        // Módulo Reportes
        new Permiso { Codigo = "REPORTES_VER", Nombre = "Ver Reportes", Descripcion = "Permite consultar reportes integrales y estadísticas de gestión", Modulo = "Reportes" },

        // Módulo Mi Cuenta
        new Permiso { Codigo = "PERFIL_VER", Nombre = "Ver Perfil", Descripcion = "Permite consultar información personal y permisos propios", Modulo = "Mi Cuenta" },
        new Permiso { Codigo = "PERFIL_MODIFICAR", Nombre = "Modificar Perfil", Descripcion = "Permite actualizar datos personales propios y cambiar contraseña", Modulo = "Mi Cuenta" }
    };

    public static readonly Dictionary<string, string[]> MatrizRolesPermisos = new()
    {
        ["ADMINISTRADOR"] = CatalogoPermisos.Select(p => p.Codigo).ToArray(),
        ["VENDEDOR"] = new[]
        {
            "CLIENTES_VER", "CLIENTES_GESTIONAR",
            "STOCK_VER",
            "MOVCOM_VER", "MOVCOM_CREAR", "MOVCOM_CORREGIR",
            "PRECIOS_REF_VER",
            "TEMPORADAS_VER",
            "PLANTACIONES_VER",
            "TRANSFERENCIAS_VER",
            "COSECHAS_VER",
            "REPORTES_VER",
            "PERFIL_VER", "PERFIL_MODIFICAR"
        },
        ["SECRETARIA"] = new[]
        {
            "CLIENTES_VER", "CLIENTES_GESTIONAR",
            "SUCURSALES_VER",
            "STOCK_VER", "STOCK_AJUSTAR",
            "MOVCOM_VER", "MOVCOM_CREAR", "MOVCOM_APROBAR", "MOVCOM_FINALIZAR", "MOVCOM_RECHAZAR", "MOVCOM_CORREGIR",
            "PRECIOS_REF_VER",
            "TEMPORADAS_VER", "TEMPORADAS_GESTIONAR",
            "PLANTACIONES_VER", "PLANTACIONES_GESTIONAR",
            "TRANSFERENCIAS_VER", "TRANSFERENCIAS_GESTIONAR",
            "COSECHAS_VER", "COSECHAS_GESTIONAR",
            "REPORTES_VER",
            "PERFIL_VER", "PERFIL_MODIFICAR"
        },
        ["CONTABILIDAD"] = new[]
        {
            "CLIENTES_VER",
            "STOCK_VER",
            "MOVCOM_VER", "MOVCOM_FACTURAR", "MOVCOM_RECHAZAR",
            "PRECIOS_REF_VER", "PRECIOS_REF_GESTIONAR",
            "TEMPORADAS_VER",
            "REPORTES_VER",
            "PERFIL_VER", "PERFIL_MODIFICAR"
        },
        ["CLIENTE"] = new[]
        {
            "STOCK_VER",
            "MOVCOM_VER",
            "PLANTACIONES_VER",
            "COSECHAS_VER",
            "REPORTES_VER",
            "PERFIL_VER", "PERFIL_MODIFICAR"
        }
    };

    public static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SigaDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider.GetService<ILogger<SigaDbContext>>();

        try
        {
            if (context.Database.GetMigrations().Any())
            {
                await context.Database.MigrateAsync();
            }
            else
            {
                await context.Database.EnsureCreatedAsync();
            }

            // Asegurar columna Activado si la tabla Usuarios ya fue creada previamente
            try
            {
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE Usuarios ADD COLUMN Activado TINYINT(1) NOT NULL DEFAULT 0;");
            }
            catch
            {
                try
                {
                    await context.Database.ExecuteSqlRawAsync("ALTER TABLE Usuarios ADD COLUMN Activado INTEGER NOT NULL DEFAULT 0;");
                }
                catch { }
            }

            try
            {
                await context.Database.ExecuteSqlRawAsync("UPDATE Usuarios SET Activado = 1 WHERE Username = 'admin';");
            }
            catch { }

            // 1. Asegurar Roles del Sistema (EsSistema = true)
            var systemRoles = new (int Id, string Nombre, string Descripcion)[]
            {
                (1, "ADMINISTRADOR", "Acceso total a las funciones del sistema"),
                (2, "VENDEDOR", "Gestión comercial y relaciones con clientes"),
                (3, "SECRETARIA", "Gestión administrativa y atención"),
                (4, "CLIENTE", "Acceso a portal de cliente e inversiones"),
                (5, "CONTABILIDAD", "Gestión financiera y contable")
            };

            foreach (var r in systemRoles)
            {
                var roleEntity = await context.Roles.FirstOrDefaultAsync(ro => ro.Nombre == r.Nombre);
                if (roleEntity == null)
                {
                    await context.Roles.AddAsync(new Rol
                    {
                        Id = r.Id,
                        Nombre = r.Nombre,
                        Descripcion = r.Descripcion,
                        EsSistema = true
                    });
                }
                else
                {
                    roleEntity.EsSistema = true;
                }
            }
            await context.SaveChangesAsync();

            // 2. Sembrar Catálogo de Permisos
            foreach (var perm in CatalogoPermisos)
            {
                var existing = await context.Permisos.FirstOrDefaultAsync(p => p.Codigo == perm.Codigo);
                if (existing == null)
                {
                    await context.Permisos.AddAsync(new Permiso
                    {
                        Codigo = perm.Codigo,
                        Nombre = perm.Nombre,
                        Descripcion = perm.Descripcion,
                        Modulo = perm.Modulo
                    });
                }
                else
                {
                    existing.Nombre = perm.Nombre;
                    existing.Descripcion = perm.Descripcion;
                    existing.Modulo = perm.Modulo;
                }
            }
            await context.SaveChangesAsync();

            // 3. Sembrar Mapeo RolPermiso
            var allPermisos = await context.Permisos.ToListAsync();
            var permisosDict = allPermisos.ToDictionary(p => p.Codigo, p => p.Id);

            foreach (var (rolNombre, codigos) in MatrizRolesPermisos)
            {
                var rol = await context.Roles.FirstOrDefaultAsync(r => r.Nombre == rolNombre);
                if (rol == null) continue;

                foreach (var codigo in codigos)
                {
                    if (permisosDict.TryGetValue(codigo, out var permId))
                    {
                        var exists = await context.RolPermisos
                            .AnyAsync(rp => rp.RolId == rol.Id && rp.PermisoId == permId);
                        if (!exists)
                        {
                            await context.RolPermisos.AddAsync(new RolPermiso
                            {
                                RolId = rol.Id,
                                PermisoId = permId
                            });
                        }
                    }
                }
            }
            await context.SaveChangesAsync();

            // 4. Sembrar Usuario Administrador Inicial
            var adminRole = await context.Roles.FirstAsync(r => r.Nombre == "ADMINISTRADOR");
            var existeAdministrador = await context.Usuarios
                .AnyAsync(u => u.RolId == adminRole.Id && !u.IsDeleted);

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

                    var adminUser = new Usuario
                    {
                        PersonaId = adminPersona.Id,
                        Username = bootstrapUsername.Trim(),
                        PasswordHash = passwordHasher.HashPassword(bootstrapPassword),
                        Estado = EstadoUsuario.Activo,
                        Activado = true,
                        RolId = adminRole.Id, // Foreign key directa al rol ADMINISTRADOR
                        FechaCreacion = DateTime.UtcNow
                    };

                    await context.Usuarios.AddAsync(adminUser);
                    await context.SaveChangesAsync();
                    logger?.LogInformation("Usuario administrador inicial creado exitosamente con RolId = {RolId}.", adminRole.Id);
                }
                else
                {
                    logger?.LogWarning(
                        "No existe ningún usuario ADMINISTRADOR en la base de datos y no se definieron las variables 'BootstrapAdmin__Username' / 'BootstrapAdmin__Password'.");
                }
            }

            // 5. Sembrar sucursal de ejemplo únicamente si está habilitado en configuración (entorno Development) y no hay sucursales
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
