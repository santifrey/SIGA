using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGA.Domain.Entities;

namespace SIGA.Infrastructure.Persistence.Configurations;

public class SucursalConfiguration : IEntityTypeConfiguration<Sucursal>
{
    public void Configure(EntityTypeBuilder<Sucursal> builder)
    {
        builder.ToTable("Sucursales");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Nombre)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Ubicacion)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.CapacidadTotal)
            .HasPrecision(18, 2);

        builder.Property(s => s.Estado)
            .IsRequired();

        builder.Property(s => s.FechaCreacion)
            .IsRequired();

        builder.Property(s => s.IsDeleted)
            .HasDefaultValue(false);
    }
}
