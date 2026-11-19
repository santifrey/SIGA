using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGA.Domain.Entities;

namespace SIGA.Infrastructure.Persistence.Configurations;

public class SucursalConfiguration : IEntityTypeConfiguration<Sucursal>
{
    public void Configure(EntityTypeBuilder<Sucursal> builder)
    {
        builder.ToTable("Sucursales");

        builder.Property(s => s.Nombre)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Ubicacion)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.CapacidadTotal)
            .HasPrecision(18, 2);

        builder.Property(s => s.Activo)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.EsCasaMatriz)
            .HasDefaultValue(false);

        builder.Property(s => s.RazonSocial)
            .HasMaxLength(150);

        builder.Property(s => s.CUIT)
            .HasMaxLength(20);

        builder.Property(s => s.DireccionFiscal)
            .HasMaxLength(200);

        builder.Property(s => s.Telefono)
            .HasMaxLength(50);

        builder.Property(s => s.Email)
            .HasMaxLength(150);

        builder.Property(s => s.FechaCreacion)
            .IsRequired();
    }
}
