using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGA.Domain.Entities;

namespace SIGA.Infrastructure.Persistence.Configurations;

public class FacturaConfiguration : IEntityTypeConfiguration<Factura>
{
    public void Configure(EntityTypeBuilder<Factura> builder)
    {
        builder.ToTable("Facturas");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.NumeroFactura)
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(f => f.MontoTotal)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(f => f.ArchivoUrl)
            .HasMaxLength(500);

        builder.Property(f => f.Estado)
            .HasConversion<int>()
            .IsRequired();

        builder.HasIndex(f => f.NumeroFactura)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(f => f.UsuarioEmision)
            .WithMany()
            .HasForeignKey(f => f.UsuarioEmisionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
