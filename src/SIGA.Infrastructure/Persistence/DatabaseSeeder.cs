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
                        adminPersona = new Empleado
                        {
                            Nombre = nombre,
                            Apellido = apellido,
                            Documento = doc,
                            Telefono = telefono,
                            Email = email,
                            Legajo = "LEG-0001",
                            Posicion = "Administrador General",
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

            // 5. Asegurar entidad de contraparte para Ajustes de Inventario
            var sucursalAjustes = await context.Sucursales
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Nombre == "Ajustes de Inventario");

            if (sucursalAjustes == null)
            {
                sucursalAjustes = new Sucursal
                {
                    TipoParticipante = TipoParticipante.Sucursal,
                    Estado = EstadoParticipante.Activo,
                    FechaRegistro = DateTime.UtcNow,
                    Nombre = "Ajustes de Inventario",
                    RazonSocial = "Ajustes de Inventario",
                    CUIT = "00-00000000-0",
                    Activo = false,
                    IsDeleted = false
                };
                await context.Sucursales.AddAsync(sucursalAjustes);
                await context.SaveChangesAsync();
                logger?.LogInformation("Entidad 'Ajustes de Inventario' aprovisionada exitosamente con Id = {Id}.", sucursalAjustes.Id);
            }

            // 6. Sembrar datos de ejemplo cuando la base de datos se recrea vacía en entorno de desarrollo
            var seedSampleData = configuration.GetValue<bool>("SeedSampleSucursal", false);
            if (seedSampleData && !await context.Clientes.IgnoreQueryFilters().AnyAsync())
            {
                await SeedSampleDevelopmentDataAsync(context, sucursalAjustes.Id, logger);
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Ocurrió un error inicializando o sembrando la base de datos.");
            throw;
        }
    }

    private static async Task SeedSampleDevelopmentDataAsync(
        SigaDbContext context,
        int ajustesParticipanteId,
        ILogger<SigaDbContext>? logger)
    {
        var adminUser = await context.Usuarios.FirstOrDefaultAsync(u => !u.IsDeleted);
        if (adminUser == null) return;

        // 1. Sucursal principal
        var sucursal = await context.Sucursales.FirstOrDefaultAsync();
        if (sucursal == null)
        {
            sucursal = new Sucursal
            {
                Nombre = "Sede Central - Villa General Belgrano",
                Ubicacion = "Ruta Prov. 5, Villa General Belgrano, Córdoba",
                CapacidadTotal = 50000m,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };
            await context.Sucursales.AddAsync(sucursal);
            await context.SaveChangesAsync();
        }

        // 2. Clientes de ejemplo con Nombre y Apellido reales
        var datosClientes = new[]
        {
            (Nombre: "Carlos", Apellido: "López", Doc: "28999111", Tel: "+54 351 4112233", Email: "carlos.lopez@ejemplo.com", Codigo: "CLI-2026-0001", Obs: "Productor asociado - Valle de Calamuchita"),
            (Nombre: "Mariana", Apellido: "Sosa", Doc: "37753506", Tel: "+54 351 7778899", Email: "mariana.sosa@ejemplo.com", Codigo: "CLI-2026-0002", Obs: "Inversora inicial de temporada"),
            (Nombre: "Gonzalo", Apellido: "Pérez", Doc: "36735074", Tel: "+54 351 4445566", Email: "gonzalo.perez@ejemplo.com", Codigo: "CLI-2026-0003", Obs: "Cliente mayorista de bulbos")
        };

        var clientesCreados = new List<Cliente>();
        foreach (var item in datosClientes)
        {
            var persona = new Persona
            {
                Nombre = item.Nombre,
                Apellido = item.Apellido,
                Documento = item.Doc,
                Telefono = item.Tel,
                Email = item.Email,
                FechaCreacion = DateTime.UtcNow
            };
            await context.Personas.AddAsync(persona);
            await context.SaveChangesAsync();

            var cliente = new Cliente
            {
                PersonaId = persona.Id,
                TipoParticipante = TipoParticipante.Cliente,
                Estado = EstadoParticipante.Activo,
                EstadoCliente = EstadoCliente.Activo,
                CodigoCliente = item.Codigo,
                Observaciones = item.Obs,
                FechaRegistro = DateTime.UtcNow
            };
            await context.Clientes.AddAsync(cliente);
            await context.SaveChangesAsync();
            clientesCreados.Add(cliente);
        }

        // 3. Temporada activa con filas y precios de referencia por calibre
        var temporada = new Temporada("Temporada 2026 - Calamuchita", adminUser.Id, DateTime.UtcNow.AddMonths(-2))
        {
            Estado = EstadoTemporada.Comercializacion
        };
        await context.Temporadas.AddAsync(temporada);
        await context.SaveChangesAsync();

        var filas = new[]
        {
            new Fila(sucursal.Id, temporada.Id, 1, 5000),
            new Fila(sucursal.Id, temporada.Id, 2, 5000),
            new Fila(sucursal.Id, temporada.Id, 3, 5000)
        };
        await context.Filas.AddRangeAsync(filas);

        var precios = new[]
        {
            new PrecioClase(temporada.Id, ClaseBulbo.Clase0, 2800m),
            new PrecioClase(temporada.Id, ClaseBulbo.Clase1, 2200m),
            new PrecioClase(temporada.Id, ClaseBulbo.Clase2, 1700m),
            new PrecioClase(temporada.Id, ClaseBulbo.Clase3, 1200m),
            new PrecioClase(temporada.Id, ClaseBulbo.Clase4, 750m),
            new PrecioClase(temporada.Id, ClaseBulbo.MIX, 1700m)
        };
        await context.PreciosClase.AddRangeAsync(precios);
        await context.SaveChangesAsync();

        // 4. Ajustes de Stock iniciales confirmados (sin FK a MovimientoComercial, Cosecha ni Plantación)
        var casaMatriz = await context.Sucursales.FirstOrDefaultAsync(s => s.EsCasaMatriz && !s.IsDeleted);
        var casaMatrizId = casaMatriz?.Id ?? 1;

        var ajusteCasaMatriz = new MovimientoBulbos(
            ajustesParticipanteId,
            casaMatrizId,
            adminUser.Id,
            "[Ingreso a Sede Central] Stock inicial de depósito central por inventario físico");
        ajusteCasaMatriz.AgregarCantidad(ClaseBulbo.Clase0, 500);
        ajusteCasaMatriz.AgregarCantidad(ClaseBulbo.Clase1, 1500);
        ajusteCasaMatriz.AgregarCantidad(ClaseBulbo.Clase2, 2000);
        ajusteCasaMatriz.AgregarCantidad(ClaseBulbo.Clase3, 1000);
        ajusteCasaMatriz.AgregarCantidad(ClaseBulbo.Clase4, 500);
        ajusteCasaMatriz.Confirmar();

        var ajusteCliente1 = new MovimientoBulbos(
            ajustesParticipanteId,
            clientesCreados[0].Id,
            adminUser.Id,
            "[Ingreso a Cliente] Saldo inicial de bulbos en custodia para Carlos López");
        ajusteCliente1.AgregarCantidad(ClaseBulbo.Clase1, 200);
        ajusteCliente1.AgregarCantidad(ClaseBulbo.Clase2, 300);
        ajusteCliente1.AgregarCantidad(ClaseBulbo.Clase3, 100);
        ajusteCliente1.Confirmar();

        var ajusteCliente2 = new MovimientoBulbos(
            ajustesParticipanteId,
            clientesCreados[1].Id,
            adminUser.Id,
            "[Ingreso a Cliente] Saldo inicial de bulbos en custodia para Mariana Sosa");
        ajusteCliente2.AgregarCantidad(ClaseBulbo.Clase1, 150);
        ajusteCliente2.AgregarCantidad(ClaseBulbo.Clase2, 200);
        ajusteCliente2.AgregarCantidad(ClaseBulbo.Clase3, 100);
        ajusteCliente2.Confirmar();

        await context.MovimientosBulbos.AddRangeAsync(ajusteCasaMatriz, ajusteCliente1, ajusteCliente2);
        await context.SaveChangesAsync();

        logger?.LogInformation("Datos de ejemplo iniciales (Sucursal, 3 Clientes, Temporada 2026, Precios y Stock inicial) sembrados con éxito.");
    }
}
