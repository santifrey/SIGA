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

            // Limpieza y compatibilidad de FK obsoletas a Ventas en RegistrosAprobacion, Contratos y Facturas
            try
            {
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE RegistrosAprobacion DROP FOREIGN KEY FK_RegistrosAprobacion_Ventas_VentaId;");
            }
            catch { }
            try
            {
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE RegistrosAprobacion MODIFY COLUMN VentaId INT NULL;");
            }
            catch { }
            try
            {
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE Contratos DROP FOREIGN KEY FK_Contratos_Ventas_VentaId;");
            }
            catch { }
            try
            {
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE Contratos MODIFY COLUMN VentaId INT NULL;");
            }
            catch { }
            try
            {
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE Facturas DROP FOREIGN KEY FK_Facturas_Ventas_VentaId;");
            }
            catch { }
            try
            {
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE Facturas DROP INDEX IX_Facturas_VentaId;");
            }
            catch { }
            try
            {
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE Facturas MODIFY COLUMN VentaId INT NULL DEFAULT NULL;");
            }
            catch { }

            // Limpieza de columnas de discriminador o campos redundantes en Personas si existieran de migraciones previas
            try
            {
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE Personas DROP COLUMN Discriminator;");
            }
            catch { }

            try
            {
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE Personas DROP COLUMN Legajo;");
            }
            catch { }

            try
            {
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE Personas DROP COLUMN Posicion;");
            }
            catch { }

            // Asegurar tabla Empleados con Foreign Key a Personas (Estrategia TPT - sin discriminador)
            try
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS Empleados (
                        Id INT NOT NULL PRIMARY KEY,
                        Legajo VARCHAR(50) NULL,
                        Posicion VARCHAR(100) NULL,
                        CONSTRAINT FK_Empleados_Personas_Id FOREIGN KEY (Id) REFERENCES Personas(Id) ON DELETE CASCADE
                    );
                ");
            }
            catch { }

            try
            {
                // Migrar o asegurar que usuarios internos tengan registro en Empleados
                var personalSinEmpleado = await context.Usuarios
                    .Include(u => u.Rol)
                    .Where(u => u.Rol != null && u.Rol.Nombre != "CLIENTE")
                    .Select(u => u.PersonaId)
                    .ToListAsync();

                foreach (var personaId in personalSinEmpleado)
                {
                    var exists = await context.Empleados.AnyAsync(e => e.Id == personaId);
                    if (!exists)
                    {
                        await context.Database.ExecuteSqlRawAsync(
                            "INSERT INTO Empleados (Id, Legajo, Posicion) VALUES ({0}, {1}, {2});",
                            personaId, $"LEG-{personaId:D4}", "Personal Interno");
                    }
                }
            }
            catch { }

            // Asegurar tabla Clientes con Foreign Key a Personas y a Contratos (Estrategia TPT)
            try
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS Clientes (
                        Id INT NOT NULL PRIMARY KEY,
                        PersonaId INT NOT NULL,
                        ContratoId INT NULL,
                        CodigoCliente VARCHAR(50) NULL,
                        Observaciones VARCHAR(500) NULL,
                        EstadoCliente INT NOT NULL DEFAULT 1,
                        CONSTRAINT FK_Clientes_ParticipantesComerciales_Id FOREIGN KEY (Id) REFERENCES ParticipantesComerciales(Id) ON DELETE CASCADE,
                        CONSTRAINT FK_Clientes_Personas_PersonaId FOREIGN KEY (PersonaId) REFERENCES Personas(Id) ON DELETE RESTRICT,
                        CONSTRAINT FK_Clientes_Contratos_ContratoId FOREIGN KEY (ContratoId) REFERENCES Contratos(Id) ON DELETE SET NULL
                    );
                ");
            }
            catch { }

            try
            {
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE Clientes ADD COLUMN ContratoId INT NULL;");
            }
            catch { }

            try
            {
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE Clientes ADD CONSTRAINT UQ_Clientes_ContratoId UNIQUE (ContratoId);");
            }
            catch { }

            try
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    INSERT IGNORE INTO Clientes (Id, PersonaId, CodigoCliente, Observaciones, EstadoCliente)
                    SELECT Id, PersonaId, CodigoCliente, Observaciones, COALESCE(EstadoCliente, 1)
                    FROM ParticipantesComerciales
                    WHERE TipoParticipante = 1 AND PersonaId IS NOT NULL;
                ");
            }
            catch { }

            // Asegurar tabla Empresas con Foreign Key a ParticipantesComerciales (Estrategia TPT)
            try
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS Empresas (
                        Id INT NOT NULL PRIMARY KEY,
                        RazonSocial VARCHAR(150) NULL,
                        CUIT VARCHAR(20) NULL,
                        DireccionFiscal VARCHAR(200) NULL,
                        Telefono VARCHAR(50) NULL,
                        Email VARCHAR(150) NULL,
                        EsCasaMatriz TINYINT(1) NOT NULL DEFAULT 0,
                        CONSTRAINT FK_Empresas_ParticipantesComerciales_Id FOREIGN KEY (Id) REFERENCES ParticipantesComerciales(Id) ON DELETE CASCADE
                    );
                ");
            }
            catch { }

            // Asegurar columnas autónomas en MovimientosBulbos tras la eliminación de OperacionComercial
            try
            {
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE MovimientosBulbos ADD COLUMN MovimientoComercialId INT NULL;");
            }
            catch { }
            try
            {
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE MovimientosBulbos ADD COLUMN CosechaId INT NULL;");
            }
            catch { }
            try
            {
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE MovimientosBulbos ADD COLUMN PlantacionId INT NULL;");
            }
            catch { }
            try
            {
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE MovimientosBulbos ADD COLUMN TipoOperacion INT NOT NULL DEFAULT 1;");
            }
            catch { }
            try
            {
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE MovimientosBulbos MODIFY COLUMN OperacionComercialId INT NULL;");
            }
            catch { }
            try
            {
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE MovimientosBulbos DROP FOREIGN KEY FK_MovimientosBulbos_OperacionesComerciales_OperacionComercialId;");
            }
            catch { }
            try
            {
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE TransferenciasLotes DROP FOREIGN KEY FK_TransferenciasLotes_OperacionesComerciales_OperacionComercialId;");
            }
            catch { }
            try
            {
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE TransferenciasLotes MODIFY COLUMN OperacionComercialId INT NULL;");
            }
            catch { }
            try
            {
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE MovimientosComerciales DROP FOREIGN KEY FK_MovimientosComerciales_OperacionesComerciales_OperacionComercialId;");
            }
            catch { }
            try
            {
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE MovimientosComerciales MODIFY COLUMN OperacionComercialId INT NULL;");
            }
            catch { }

            try
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    INSERT IGNORE INTO Empresas (Id, RazonSocial, CUIT, DireccionFiscal, Telefono, Email, EsCasaMatriz)
                    SELECT Id, RazonSocial, CUIT, DireccionFiscal, Telefono, Email, COALESCE(EsCasaMatriz, 0)
                    FROM ParticipantesComerciales
                    WHERE TipoParticipante = 2;
                ");
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

            // 6. Asegurar entidad de contraparte para Ajustes de Inventario
            var existeAjuste = await context.Empresas.IgnoreQueryFilters().AnyAsync(e => e.RazonSocial == "Ajustes de Inventario");
            if (!existeAjuste)
            {
                var empresaAjustes = new Empresa
                {
                    TipoParticipante = TipoParticipante.Empresa,
                    Estado = EstadoParticipante.Activo,
                    FechaRegistro = DateTime.UtcNow,
                    RazonSocial = "Ajustes de Inventario",
                    CUIT = "00-00000000-0",
                    IsDeleted = false
                };
                await context.Empresas.AddAsync(empresaAjustes);
                await context.SaveChangesAsync();
                logger?.LogInformation("Entidad 'Ajustes de Inventario' aprovisionada exitosamente con Id = {Id}.", empresaAjustes.Id);
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Ocurrió un error inicializando o sembrando la base de datos.");
            throw;
        }
    }
}
