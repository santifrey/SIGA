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
        new Permiso { Codigo = "ROLES_VER", Nombre = "Ver Roles", Descripcion = "Permite consultar el catálogo de roles del sistema", Modulo = "Seguridad" },
        new Permiso { Codigo = "ROLES_GESTIONAR", Nombre = "Gestionar Roles", Descripcion = "Permite crear, modificar y eliminar roles y asignar permisos", Modulo = "Seguridad" },
        new Permiso { Codigo = "PERMISOS_VER", Nombre = "Ver Permisos", Descripcion = "Permite consultar el catálogo completo de permisos del sistema", Modulo = "Seguridad" },
        new Permiso { Codigo = "USUARIOS_VER", Nombre = "Ver Usuarios", Descripcion = "Permite consultar los usuarios del sistema", Modulo = "Seguridad" },
        new Permiso { Codigo = "USUARIOS_GESTIONAR", Nombre = "Gestionar Usuarios", Descripcion = "Permite crear, editar, bloquear y resetear claves de usuarios", Modulo = "Seguridad" },

        // Módulo Personal
        new Permiso { Codigo = "EMPLEADOS_VER_SUCURSAL", Nombre = "Ver Empleados (Solo Sucursal)", Descripcion = "Permite consultar solo empleados asignados a la sucursal del usuario", Modulo = "Personal" },
        new Permiso { Codigo = "EMPLEADOS_VER_TODOS", Nombre = "Ver Empleados (Todos)", Descripcion = "Permite consultar empleados de todas las sucursales", Modulo = "Personal" },
        new Permiso { Codigo = "EMPLEADOS_GESTIONAR_SUCURSAL", Nombre = "Gestionar Empleados (Solo Sucursal)", Descripcion = "Permite crear y modificar solo empleados de la sucursal del usuario", Modulo = "Personal" },
        new Permiso { Codigo = "EMPLEADOS_GESTIONAR_TODOS", Nombre = "Gestionar Empleados (Todos)", Descripcion = "Permite gestionar empleados en todas las sucursales", Modulo = "Personal" },

        // Módulo Clientes
        new Permiso { Codigo = "CLIENTES_VER_PROPIO", Nombre = "Ver Clientes (Solo Propios)", Descripcion = "Permite consultar únicamente clientes propios o asignados", Modulo = "Clientes" },
        new Permiso { Codigo = "CLIENTES_VER_SUCURSAL", Nombre = "Ver Clientes (Solo Sucursal)", Descripcion = "Permite consultar clientes radicados en la sucursal del usuario", Modulo = "Clientes" },
        new Permiso { Codigo = "CLIENTES_VER_TODOS", Nombre = "Ver Clientes (Todos)", Descripcion = "Permite consultar todos los clientes del sistema", Modulo = "Clientes" },
        new Permiso { Codigo = "CLIENTES_GESTIONAR_PROPIO", Nombre = "Gestionar Clientes (Solo Propios)", Descripcion = "Permite dar de alta y editar únicamente clientes propios", Modulo = "Clientes" },
        new Permiso { Codigo = "CLIENTES_GESTIONAR_SUCURSAL", Nombre = "Gestionar Clientes (Solo Sucursal)", Descripcion = "Permite dar de alta y editar clientes de la sucursal", Modulo = "Clientes" },
        new Permiso { Codigo = "CLIENTES_GESTIONAR_TODOS", Nombre = "Gestionar Clientes (Todos)", Descripcion = "Permite registrar y gestionar clientes globalmente", Modulo = "Clientes" },

        // Módulo Sucursales
        new Permiso { Codigo = "SUCURSALES_VER_SUCURSAL", Nombre = "Ver Sucursal (Solo Propia)", Descripcion = "Permite consultar los datos de la sucursal a la que pertenece", Modulo = "Sucursales" },
        new Permiso { Codigo = "SUCURSALES_VER_TODOS", Nombre = "Ver Sucursales (Todas)", Descripcion = "Permite consultar todas las sucursales de la empresa", Modulo = "Sucursales" },
        new Permiso { Codigo = "SUCURSALES_GESTIONAR", Nombre = "Gestionar Sucursales", Descripcion = "Permite crear y actualizar sucursales de la empresa", Modulo = "Sucursales" },

        // Módulo Stock
        new Permiso { Codigo = "STOCK_VER_PROPIO", Nombre = "Ver Stock (Solo Propio)", Descripcion = "Permite consultar únicamente el saldo de bulbos propios en custodia", Modulo = "Stock" },
        new Permiso { Codigo = "STOCK_VER_SUCURSAL", Nombre = "Ver Stock (Solo Sucursal)", Descripcion = "Permite consultar existencias únicamente de su sucursal", Modulo = "Stock" },
        new Permiso { Codigo = "STOCK_VER_TODOS", Nombre = "Ver Stock (Todas las Sucursales)", Descripcion = "Permite consultar existencias de bulbos de todas las sucursales", Modulo = "Stock" },
        new Permiso { Codigo = "STOCK_AJUSTAR_SUCURSAL", Nombre = "Ajustar Stock (Solo Sucursal)", Descripcion = "Permite registrar ajustes de stock solo en su sucursal", Modulo = "Stock" },
        new Permiso { Codigo = "STOCK_AJUSTAR_TODOS", Nombre = "Ajustar Stock (Todas las Sucursales)", Descripcion = "Permite realizar ajustes de inventario en cualquier sucursal", Modulo = "Stock" },

        // Módulo Comercial
        new Permiso { Codigo = "MOVCOM_VER_PROPIO", Nombre = "Ver Movimientos Comerciales (Solo Propios)", Descripcion = "Permite consultar solo operaciones registradas por el usuario", Modulo = "Comercial" },
        new Permiso { Codigo = "MOVCOM_VER_SUCURSAL", Nombre = "Ver Movimientos Comerciales (Solo Sucursal)", Descripcion = "Permite consultar operaciones de la sucursal del usuario", Modulo = "Comercial" },
        new Permiso { Codigo = "MOVCOM_VER_TODOS", Nombre = "Ver Movimientos Comerciales (Todos)", Descripcion = "Permite consultar operaciones comerciales de todo el sistema", Modulo = "Comercial" },
        new Permiso { Codigo = "MOVCOM_CREAR_PROPIO", Nombre = "Crear Movimiento Comercial (Solo Propios)", Descripcion = "Permite registrar ventas/compras como operador directo", Modulo = "Comercial" },
        new Permiso { Codigo = "MOVCOM_CREAR_SUCURSAL", Nombre = "Crear Movimiento Comercial (Solo Sucursal)", Descripcion = "Permite registrar operaciones originadas en la sucursal del usuario", Modulo = "Comercial" },
        new Permiso { Codigo = "MOVCOM_CREAR_TODOS", Nombre = "Crear Movimiento Comercial (Cualquier Sucursal)", Descripcion = "Permite registrar operaciones involucrando cualquier sucursal", Modulo = "Comercial" },
        new Permiso { Codigo = "MOVCOM_GESTIONAR_PROPIO", Nombre = "Gestionar Movimientos (Solo Propios)", Descripcion = "Permite gestionar solo operaciones propias", Modulo = "Comercial" },
        new Permiso { Codigo = "MOVCOM_GESTIONAR_SUCURSAL", Nombre = "Gestionar Movimientos (Solo Sucursal)", Descripcion = "Permite gestionar operaciones de la sucursal", Modulo = "Comercial" },
        new Permiso { Codigo = "MOVCOM_GESTIONAR_TODOS", Nombre = "Gestionar Movimientos (Todos)", Descripcion = "Permite gestionar cualquier operación comercial", Modulo = "Comercial" },
        new Permiso { Codigo = "MOVCOM_APROBAR_SUCURSAL", Nombre = "Aprobar Movimientos (Solo Sucursal)", Descripcion = "Permite aprobar operaciones de su sucursal", Modulo = "Comercial" },
        new Permiso { Codigo = "MOVCOM_APROBAR_TODOS", Nombre = "Aprobar Movimientos (Todos)", Descripcion = "Permite aprobar operaciones de cualquier sucursal", Modulo = "Comercial" },
        new Permiso { Codigo = "MOVCOM_FACTURAR_SUCURSAL", Nombre = "Facturar Movimientos (Solo Sucursal)", Descripcion = "Permite facturar operaciones de su sucursal", Modulo = "Comercial" },
        new Permiso { Codigo = "MOVCOM_FACTURAR_TODOS", Nombre = "Facturar Movimientos (Todos)", Descripcion = "Permite facturar operaciones de todo el sistema", Modulo = "Comercial" },
        new Permiso { Codigo = "MOVCOM_FINALIZAR_SUCURSAL", Nombre = "Finalizar Movimientos (Solo Sucursal)", Descripcion = "Permite finalizar operaciones de su sucursal", Modulo = "Comercial" },
        new Permiso { Codigo = "MOVCOM_FINALIZAR_TODOS", Nombre = "Finalizar Movimientos (Todos)", Descripcion = "Permite finalizar operaciones de cualquier sucursal", Modulo = "Comercial" },
        new Permiso { Codigo = "MOVCOM_RECHAZAR_SUCURSAL", Nombre = "Rechazar Movimientos (Solo Sucursal)", Descripcion = "Permite rechazar operaciones de su sucursal", Modulo = "Comercial" },
        new Permiso { Codigo = "MOVCOM_RECHAZAR_TODOS", Nombre = "Rechazar Movimientos (Todos)", Descripcion = "Permite rechazar operaciones de cualquier sucursal", Modulo = "Comercial" },
        new Permiso { Codigo = "MOVCOM_CORREGIR", Nombre = "Corregir Movimiento Rechazado", Descripcion = "Permite editar y reenviar operaciones rechazadas", Modulo = "Comercial" },
        new Permiso { Codigo = "PRECIOS_REF_VER", Nombre = "Ver Precios de Referencia", Descripcion = "Permite consultar precios de referencia", Modulo = "Comercial" },
        new Permiso { Codigo = "PRECIOS_REF_GESTIONAR", Nombre = "Gestionar Precios de Referencia", Descripcion = "Permite establecer y actualizar precios de referencia", Modulo = "Comercial" },

        // Módulo Producción
        new Permiso { Codigo = "TEMPORADAS_VER", Nombre = "Ver Temporadas", Descripcion = "Permite consultar temporadas productivas", Modulo = "Producción" },
        new Permiso { Codigo = "TEMPORADAS_GESTIONAR", Nombre = "Gestionar Temporadas", Descripcion = "Permite crear y gestionar temporadas", Modulo = "Producción" },
        new Permiso { Codigo = "PLANTACIONES_VER_PROPIO", Nombre = "Ver Plantaciones (Solo Propias)", Descripcion = "Permite consultar únicamente las filas y lotes asignados al usuario en la plantación", Modulo = "Producción" },
        new Permiso { Codigo = "PLANTACIONES_VER_SUCURSAL", Nombre = "Ver Plantaciones (Solo Sucursal)", Descripcion = "Permite consultar plantaciones de la sucursal del usuario", Modulo = "Producción" },
        new Permiso { Codigo = "PLANTACIONES_VER_TODOS", Nombre = "Ver Plantaciones (Todas)", Descripcion = "Permite consultar plantaciones de todas las sucursales", Modulo = "Producción" },
        new Permiso { Codigo = "PLANTACIONES_GESTIONAR_SUCURSAL", Nombre = "Gestionar Plantaciones (Solo Sucursal)", Descripcion = "Permite gestionar plantaciones en su propia sucursal", Modulo = "Producción" },
        new Permiso { Codigo = "PLANTACIONES_GESTIONAR_TODOS", Nombre = "Gestionar Plantaciones (Todas)", Descripcion = "Permite gestionar plantaciones en cualquier sucursal", Modulo = "Producción" },
        new Permiso { Codigo = "TRANSFERENCIAS_VER_PROPIO", Nombre = "Ver Transferencias (Solo Propias)", Descripcion = "Permite consultar el historial de transferencias que involucran lotes del usuario", Modulo = "Producción" },
        new Permiso { Codigo = "TRANSFERENCIAS_VER_SUCURSAL", Nombre = "Ver Transferencias (Solo Sucursal)", Descripcion = "Permite consultar transferencias que involucren su sucursal", Modulo = "Producción" },
        new Permiso { Codigo = "TRANSFERENCIAS_VER_TODOS", Nombre = "Ver Transferencias (Todas)", Descripcion = "Permite consultar todas las transferencias de lotes", Modulo = "Producción" },
        new Permiso { Codigo = "TRANSFERENCIAS_GESTIONAR_SUCURSAL", Nombre = "Gestionar Transferencias (Solo Sucursal)", Descripcion = "Permite transferir lotes dentro de su sucursal", Modulo = "Producción" },
        new Permiso { Codigo = "TRANSFERENCIAS_GESTIONAR_TODOS", Nombre = "Gestionar Transferencias (Todas)", Descripcion = "Permite transferir lotes en cualquier sucursal", Modulo = "Producción" },
        new Permiso { Codigo = "COSECHAS_VER_PROPIO", Nombre = "Ver Cosechas (Solo Propias)", Descripcion = "Permite consultar solo cosechas iniciadas por el usuario", Modulo = "Producción" },
        new Permiso { Codigo = "COSECHAS_VER_SUCURSAL", Nombre = "Ver Cosechas (Solo Sucursal)", Descripcion = "Permite consultar cosechas de la sucursal del usuario", Modulo = "Producción" },
        new Permiso { Codigo = "COSECHAS_VER_TODOS", Nombre = "Ver Cosechas (Todas)", Descripcion = "Permite consultar cosechas de todas las sucursales", Modulo = "Producción" },
        new Permiso { Codigo = "COSECHAS_GESTIONAR_PROPIO", Nombre = "Gestionar Cosechas (Solo Propias)", Descripcion = "Permite gestionar solo cosechas propias", Modulo = "Producción" },
        new Permiso { Codigo = "COSECHAS_GESTIONAR_SUCURSAL", Nombre = "Gestionar Cosechas (Solo Sucursal)", Descripcion = "Permite registrar y cerrar cosechas de su sucursal", Modulo = "Producción" },
        new Permiso { Codigo = "COSECHAS_GESTIONAR_TODOS", Nombre = "Gestionar Cosechas (Todas)", Descripcion = "Permite registrar y cerrar cosechas en cualquier sucursal", Modulo = "Producción" },

        // Módulo Reportes
        new Permiso { Codigo = "REPORTES_VER_PROPIO", Nombre = "Ver Reportes (Solo Propios)", Descripcion = "Permite consultar estadísticas de sus propias operaciones", Modulo = "Reportes" },
        new Permiso { Codigo = "REPORTES_VER_SUCURSAL", Nombre = "Ver Reportes (Solo Sucursal)", Descripcion = "Permite consultar reportes acotados a su sucursal", Modulo = "Reportes" },
        new Permiso { Codigo = "REPORTES_VER_TODOS", Nombre = "Ver Reportes (Global / Toda la Empresa)", Descripcion = "Permite consultar reportes integrales de toda la organización", Modulo = "Reportes" },

        // Módulo Mi Cuenta
        new Permiso { Codigo = "PERFIL_VER", Nombre = "Ver Perfil", Descripcion = "Permite consultar información personal y permisos propios", Modulo = "Mi Cuenta" },
        new Permiso { Codigo = "PERFIL_MODIFICAR", Nombre = "Modificar Perfil", Descripcion = "Permite actualizar datos personales propios y cambiar contraseña", Modulo = "Mi Cuenta" }
    };

    public static readonly Dictionary<string, string[]> MatrizRolesPermisos = new()
    {
        ["ADMINISTRADOR"] = CatalogoPermisos.Select(p => p.Codigo).ToArray(),
        ["VENDEDOR"] = new[]
        {
            "CLIENTES_VER_PROPIO", "CLIENTES_GESTIONAR_PROPIO",
            "STOCK_VER_SUCURSAL",
            "MOVCOM_VER_PROPIO", "MOVCOM_CREAR_PROPIO", "MOVCOM_CORREGIR",
            "PRECIOS_REF_VER",
            "TEMPORADAS_VER",
            "PLANTACIONES_VER_SUCURSAL",
            "TRANSFERENCIAS_VER_SUCURSAL",
            "COSECHAS_VER_PROPIO",
            "REPORTES_VER_PROPIO",
            "PERFIL_VER", "PERFIL_MODIFICAR"
        },
        ["SECRETARIA"] = new[]
        {
            "CLIENTES_VER_SUCURSAL", "CLIENTES_GESTIONAR_SUCURSAL",
            "SUCURSALES_VER_SUCURSAL",
            "STOCK_VER_SUCURSAL", "STOCK_AJUSTAR_SUCURSAL",
            "MOVCOM_VER_SUCURSAL", "MOVCOM_CREAR_SUCURSAL", "MOVCOM_APROBAR_SUCURSAL", "MOVCOM_FINALIZAR_SUCURSAL", "MOVCOM_RECHAZAR_SUCURSAL", "MOVCOM_CORREGIR",
            "PRECIOS_REF_VER",
            "TEMPORADAS_VER", "TEMPORADAS_GESTIONAR",
            "PLANTACIONES_VER_SUCURSAL", "PLANTACIONES_GESTIONAR_SUCURSAL",
            "TRANSFERENCIAS_VER_SUCURSAL", "TRANSFERENCIAS_GESTIONAR_SUCURSAL",
            "COSECHAS_VER_SUCURSAL", "COSECHAS_GESTIONAR_SUCURSAL",
            "REPORTES_VER_SUCURSAL",
            "PERFIL_VER", "PERFIL_MODIFICAR"
        },
        ["CONTABILIDAD"] = new[]
        {
            "CLIENTES_VER_TODOS",
            "STOCK_VER_TODOS",
            "MOVCOM_VER_TODOS", "MOVCOM_FACTURAR_TODOS", "MOVCOM_RECHAZAR_TODOS",
            "PRECIOS_REF_VER", "PRECIOS_REF_GESTIONAR",
            "TEMPORADAS_VER",
            "REPORTES_VER_TODOS",
            "PERFIL_VER", "PERFIL_MODIFICAR"
        },
        ["CLIENTE"] = new[]
        {
            "STOCK_VER_PROPIO",
            "MOVCOM_VER_PROPIO",
            "PLANTACIONES_VER_PROPIO",
            "TRANSFERENCIAS_VER_PROPIO",
            "COSECHAS_VER_PROPIO",
            "REPORTES_VER_PROPIO",
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

            // 1.5. Limpiar permisos obsoletos (que ya no están en el catálogo oficial)
            var codigosValidos = CatalogoPermisos.Select(p => p.Codigo).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var permisosObsoletos = await context.Permisos
                .Where(p => !codigosValidos.Contains(p.Codigo))
                .ToListAsync();

            if (permisosObsoletos.Any())
            {
                var obsoletosIds = permisosObsoletos.Select(p => p.Id).ToList();
                var rolPermisosObsoletos = await context.RolPermisos
                    .Where(rp => obsoletosIds.Contains(rp.PermisoId))
                    .ToListAsync();

                context.RolPermisos.RemoveRange(rolPermisosObsoletos);
                context.Permisos.RemoveRange(permisosObsoletos);
                await context.SaveChangesAsync();
                logger?.LogInformation("Se eliminaron {Count} permisos obsoletos del sistema.", permisosObsoletos.Count);
            }

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

                if (rol.EsSistema)
                {
                    // Sincronizar roles de sistema para remover permisos que ya no corresponden
                    var codigosEsperados = codigos.ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var asignacionesActuales = await context.RolPermisos
                        .Include(rp => rp.Permiso)
                        .Where(rp => rp.RolId == rol.Id)
                        .ToListAsync();

                    var paraEliminar = asignacionesActuales
                        .Where(rp => rp.Permiso == null || !codigosEsperados.Contains(rp.Permiso.Codigo))
                        .ToList();

                    if (paraEliminar.Any())
                    {
                        context.RolPermisos.RemoveRange(paraEliminar);
                    }
                }

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

            // 7. Sincronizar ContratoId en Clientes que registren operaciones comerciales con contrato
            var clientesSinContrato = await context.Clientes
                .Where(c => c.ContratoId == null && !c.IsDeleted)
                .ToListAsync();

            if (clientesSinContrato.Any())
            {
                foreach (var cliente in clientesSinContrato)
                {
                    var contratoValido = await context.MovimientosComerciales
                        .Where(m => (m.ClienteId == cliente.Id || (m.Tipo == TipoMovimientoComercial.VentaACliente && m.DestinoParticipanteId == cliente.Id))
                                    && m.Contrato != null && !m.Contrato.IsDeleted && m.Estado != EstadoMovimientoComercial.Rechazado)
                        .OrderByDescending(m => m.Estado == EstadoMovimientoComercial.Finalizado ? 1 : 0)
                        .ThenByDescending(m => m.Fecha)
                        .Select(m => m.Contrato)
                        .FirstOrDefaultAsync();

                    if (contratoValido != null)
                    {
                        cliente.ContratoId = contratoValido.Id;
                        logger?.LogInformation("Sincronizado Contrato #{ContratoId} para cliente #{ClienteId} ({Codigo}).", contratoValido.Id, cliente.Id, cliente.CodigoCliente);
                    }
                }
                await context.SaveChangesAsync();
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
