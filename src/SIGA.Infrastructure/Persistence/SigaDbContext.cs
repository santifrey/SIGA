using Microsoft.EntityFrameworkCore;
using SIGA.Domain.Common;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Infrastructure.Persistence.Configurations;

namespace SIGA.Infrastructure.Persistence;

public class SigaDbContext : DbContext
{
    public SigaDbContext(DbContextOptions<SigaDbContext> options) : base(options)
    {
    }

    public DbSet<Persona> Personas => Set<Persona>();
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Permiso> Permisos => Set<Permiso>();
    public DbSet<RolPermiso> RolPermisos => Set<RolPermiso>();
    public DbSet<ParticipanteComercial> ParticipantesComerciales => Set<ParticipanteComercial>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Sucursal> Sucursales => Set<Sucursal>();
    public DbSet<MovimientoBulbos> MovimientosBulbos => Set<MovimientoBulbos>();
    public DbSet<CantidadBulbos> CantidadesBulbos => Set<CantidadBulbos>();
    public DbSet<Temporada> Temporadas => Set<Temporada>();
    public DbSet<Fila> Filas => Set<Fila>();
    public DbSet<Lote> Lotes => Set<Lote>();
    public DbSet<Plantacion> Plantaciones => Set<Plantacion>();
    public DbSet<DetallePlantacion> DetallesPlantacion => Set<DetallePlantacion>();
    public DbSet<TransferenciaLote> TransferenciasLotes => Set<TransferenciaLote>();
    public DbSet<Cosecha> Cosechas => Set<Cosecha>();
    public DbSet<DetalleCosecha> DetallesCosecha => Set<DetalleCosecha>();
    public DbSet<PrecioClase> PreciosClase => Set<PrecioClase>();
    public DbSet<PrecioClase> PreciosClases => PreciosClase;
    public DbSet<MovimientoComercial> MovimientosComerciales => Set<MovimientoComercial>();
    public DbSet<DetalleMovimientoComercial> DetallesMovimientosComerciales => Set<DetalleMovimientoComercial>();
    public DbSet<Contrato> Contratos => Set<Contrato>();
    public DbSet<Factura> Facturas => Set<Factura>();
    public DbSet<RegistroAprobacion> RegistrosAprobacion => Set<RegistroAprobacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplicar configuraciones de entidades
        modelBuilder.ApplyConfiguration(new PersonaConfiguration());
        modelBuilder.ApplyConfiguration(new EmpleadoConfiguration());
        modelBuilder.ApplyConfiguration(new UsuarioConfiguration());
        modelBuilder.ApplyConfiguration(new RolConfiguration());
        modelBuilder.ApplyConfiguration(new PermisoConfiguration());
        modelBuilder.ApplyConfiguration(new RolPermisoConfiguration());
        modelBuilder.ApplyConfiguration(new ParticipanteComercialConfiguration());
        modelBuilder.ApplyConfiguration(new ClienteConfiguration());
        modelBuilder.ApplyConfiguration(new SucursalConfiguration());
        modelBuilder.ApplyConfiguration(new MovimientoBulbosConfiguration());
        modelBuilder.ApplyConfiguration(new CantidadBulbosConfiguration());
        modelBuilder.ApplyConfiguration(new TemporadaConfiguration());
        modelBuilder.ApplyConfiguration(new FilaConfiguration());
        modelBuilder.ApplyConfiguration(new LoteConfiguration());
        modelBuilder.ApplyConfiguration(new PlantacionConfiguration());
        modelBuilder.ApplyConfiguration(new DetallePlantacionConfiguration());
        modelBuilder.ApplyConfiguration(new TransferenciaLoteConfiguration());
        modelBuilder.ApplyConfiguration(new CosechaConfiguration());
        modelBuilder.ApplyConfiguration(new DetalleCosechaConfiguration());
        modelBuilder.ApplyConfiguration(new PrecioClaseConfiguration());
        modelBuilder.ApplyConfiguration(new MovimientoComercialConfiguration());
        modelBuilder.ApplyConfiguration(new DetalleMovimientoComercialConfiguration());
        modelBuilder.ApplyConfiguration(new ContratoConfiguration());
        modelBuilder.ApplyConfiguration(new FacturaConfiguration());
        modelBuilder.ApplyConfiguration(new RegistroAprobacionConfiguration());

        // Global Query Filters para Soft Delete (en jerarquía TPH, se aplica en la raíz)
        modelBuilder.Entity<Persona>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Usuario>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ParticipanteComercial>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<MovimientoBulbos>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Temporada>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Fila>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Lote>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Plantacion>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<DetallePlantacion>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TransferenciaLote>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Cosecha>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<DetalleCosecha>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<PrecioClase>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<MovimientoComercial>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<DetalleMovimientoComercial>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Contrato>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Factura>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RegistroAprobacion>().HasQueryFilter(e => !e.IsDeleted);

        // Data Seeding inicial
        SeedInitialData(modelBuilder);
    }

    private static void SeedInitialData(ModelBuilder modelBuilder)
    {
        // 1. Roles estructurales del sistema (EsSistema = true)
        modelBuilder.Entity<Rol>().HasData(
            new Rol { Id = 1, Nombre = "ADMINISTRADOR", Descripcion = "Acceso total a las funciones del sistema", EsSistema = true },
            new Rol { Id = 2, Nombre = "VENDEDOR", Descripcion = "Gestión comercial y relaciones con clientes", EsSistema = true },
            new Rol { Id = 3, Nombre = "SECRETARIA", Descripcion = "Gestión administrativa y atención", EsSistema = true },
            new Rol { Id = 4, Nombre = "CLIENTE", Descripcion = "Acceso a portal de cliente e inversiones", EsSistema = true },
            new Rol { Id = 5, Nombre = "CONTABILIDAD", Descripcion = "Gestión financiera y contable", EsSistema = true }
        );

        // 2. Sucursal Matriz (Sede Central) del Dominio
        modelBuilder.Entity<Sucursal>().HasData(
            new Sucursal
            {
                Id = 1,
                TipoParticipante = TipoParticipante.Sucursal,
                Estado = EstadoParticipante.Activo,
                FechaRegistro = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsDeleted = false,
                Nombre = "Sede Central - Villa General Belgrano",
                Ubicacion = "Ruta Prov. 5, Villa General Belgrano, Córdoba",
                CapacidadTotal = 50000m,
                Activo = true,
                EsCasaMatriz = true,
                RazonSocial = "Azafrán Mediterráneo S.A.",
                CUIT = "30-71234567-8",
                DireccionFiscal = "Córdoba, Argentina",
                Telefono = "+54 351 1234567",
                Email = "info@azafranmediterraneo.com",
                FechaCreacion = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Manejo automático de Soft-Delete
        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.FechaEliminacion = DateTime.UtcNow;
            }
        }

        // Manejo automático de Fechas de Auditoría
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.FechaCreacion == default)
                {
                    entry.Entity.FechaCreacion = DateTime.UtcNow;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.FechaModificacion = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
