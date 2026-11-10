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
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<UsuarioRol> UsuarioRoles => Set<UsuarioRol>();
    public DbSet<ParticipanteComercial> ParticipantesComerciales => Set<ParticipanteComercial>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Sucursal> Sucursales => Set<Sucursal>();
    public DbSet<OperacionComercial> OperacionesComerciales => Set<OperacionComercial>();
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
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<DetalleVenta> DetalleVentas => Set<DetalleVenta>();
    public DbSet<Contrato> Contratos => Set<Contrato>();
    public DbSet<Factura> Facturas => Set<Factura>();
    public DbSet<RegistroAprobacion> RegistrosAprobacion => Set<RegistroAprobacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplicar configuraciones de entidades
        modelBuilder.ApplyConfiguration(new PersonaConfiguration());
        modelBuilder.ApplyConfiguration(new UsuarioConfiguration());
        modelBuilder.ApplyConfiguration(new RolConfiguration());
        modelBuilder.ApplyConfiguration(new UsuarioRolConfiguration());
        modelBuilder.ApplyConfiguration(new ParticipanteComercialConfiguration());
        modelBuilder.ApplyConfiguration(new ClienteConfiguration());
        modelBuilder.ApplyConfiguration(new EmpresaConfiguration());
        modelBuilder.ApplyConfiguration(new SucursalConfiguration());
        modelBuilder.ApplyConfiguration(new OperacionComercialConfiguration());
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
        modelBuilder.ApplyConfiguration(new VentaConfiguration());
        modelBuilder.ApplyConfiguration(new DetalleVentaConfiguration());
        modelBuilder.ApplyConfiguration(new ContratoConfiguration());
        modelBuilder.ApplyConfiguration(new FacturaConfiguration());
        modelBuilder.ApplyConfiguration(new RegistroAprobacionConfiguration());

        // Global Query Filters para Soft Delete (en jerarquía TPH, se aplica en la raíz)
        modelBuilder.Entity<Persona>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Usuario>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ParticipanteComercial>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Sucursal>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<OperacionComercial>().HasQueryFilter(e => !e.IsDeleted);
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
        modelBuilder.Entity<Venta>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<DetalleVenta>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Contrato>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Factura>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RegistroAprobacion>().HasQueryFilter(e => !e.IsDeleted);

        // Data Seeding inicial
        SeedInitialData(modelBuilder);
    }

    private static void SeedInitialData(ModelBuilder modelBuilder)
    {
        // 1. Roles estructurales del sistema
        modelBuilder.Entity<Rol>().HasData(
            new Rol { Id = 1, Nombre = "ADMINISTRADOR", Descripcion = "Acceso total a las funciones del sistema" },
            new Rol { Id = 2, Nombre = "VENDEDOR", Descripcion = "Gestión comercial y relaciones con clientes" },
            new Rol { Id = 3, Nombre = "SECRETARIA", Descripcion = "Gestión administrativa y atención" },
            new Rol { Id = 4, Nombre = "CLIENTE", Descripcion = "Acceso a portal de cliente e inversiones" },
            new Rol { Id = 5, Nombre = "CONTABILIDAD", Descripcion = "Gestión financiera y contable" }
        );

        // 2. Empresa Singleton del Dominio: Azafrán Mediterráneo S.A.
        modelBuilder.Entity<Empresa>().HasData(
            new Empresa
            {
                Id = Empresa.SingletonId,
                TipoParticipante = TipoParticipante.Empresa,
                Estado = EstadoParticipante.Activo,
                FechaRegistro = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsDeleted = false,
                RazonSocial = "Azafrán Mediterráneo S.A.",
                CUIT = "30-71234567-8",
                DireccionFiscal = "Córdoba, Argentina",
                Telefono = "+54 351 1234567",
                Email = "info@azafranmediterraneo.com",
                EsCasaMatriz = true
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
