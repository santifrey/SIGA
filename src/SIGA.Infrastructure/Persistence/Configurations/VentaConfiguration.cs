using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGA.Domain.Entities;

namespace SIGA.Infrastructure.Persistence.Configurations;

public class VentaConfiguration : IEntityTypeConfiguration<Venta>
{
    public void Configure(EntityTypeBuilder<Venta> builder)
    {
        builder.ToTable("Ventas");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.MontoTotal)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(v => v.Estado)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(v => v.Observaciones)
            .HasMaxLength(500);

        builder.HasOne(v => v.OperacionComercial)
            .WithMany()
            .HasForeignKey(v => v.OperacionComercialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Vendedor)
            .WithMany()
            .HasForeignKey(v => v.VendedorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.OrigenParticipante)
            .WithMany()
            .HasForeignKey(v => v.OrigenParticipanteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Cliente)
            .WithMany()
            .HasForeignKey(v => v.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Temporada)
            .WithMany()
            .HasForeignKey(v => v.TemporadaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(v => v.Detalles)
            .WithOne(d => d.Venta)
            .HasForeignKey(d => d.VentaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.Contrato)
            .WithOne(c => c.Venta)
            .HasForeignKey<Contrato>(c => c.VentaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.Factura)
            .WithOne(f => f.Venta)
            .HasForeignKey<Factura>(f => f.VentaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.HistorialAprobaciones)
            .WithOne(r => r.Venta)
            .HasForeignKey(r => r.VentaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
