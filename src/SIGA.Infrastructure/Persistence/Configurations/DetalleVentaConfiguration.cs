using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGA.Domain.Entities;

namespace SIGA.Infrastructure.Persistence.Configurations;

public class DetalleVentaConfiguration : IEntityTypeConfiguration<DetalleVenta>
{
    public void Configure(EntityTypeBuilder<DetalleVenta> builder)
    {
        builder.ToTable("DetalleVentas");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.ClaseBulbo)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(d => d.Cantidad)
            .IsRequired();

        builder.Property(d => d.PrecioUnitarioPactado)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(d => d.Subtotal)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(d => new { d.VentaId, d.ClaseBulbo })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}
