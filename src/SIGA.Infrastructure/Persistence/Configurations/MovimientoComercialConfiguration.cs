using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGA.Domain.Entities;

namespace SIGA.Infrastructure.Persistence.Configurations;

public class MovimientoComercialConfiguration : IEntityTypeConfiguration<MovimientoComercial>
{
    public void Configure(EntityTypeBuilder<MovimientoComercial> builder)
    {
        builder.ToTable("MovimientosComerciales");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Tipo)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(m => m.Estado)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(m => m.MontoBruto)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(m => m.PorcentajeComision)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(m => m.MontoComision)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(m => m.MontoNeto)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(m => m.MontoTotal)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(m => m.Observaciones)
            .HasMaxLength(1000);

        builder.HasOne(m => m.OrigenParticipante)
            .WithMany()
            .HasForeignKey(m => m.OrigenParticipanteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.DestinoParticipante)
            .WithMany()
            .HasForeignKey(m => m.DestinoParticipanteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Cliente)
            .WithMany()
            .HasForeignKey(m => m.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Temporada)
            .WithMany()
            .HasForeignKey(m => m.TemporadaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.UsuarioRegistro)
            .WithMany()
            .HasForeignKey(m => m.UsuarioRegistroId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.OperacionComercial)
            .WithMany()
            .HasForeignKey(m => m.OperacionComercialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(m => m.Detalles)
            .WithOne(d => d.MovimientoComercial)
            .HasForeignKey(d => d.MovimientoComercialId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Contrato)
            .WithOne(c => c.MovimientoComercial)
            .HasForeignKey<Contrato>(c => c.MovimientoComercialId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Factura)
            .WithOne(f => f.MovimientoComercial)
            .HasForeignKey<Factura>(f => f.MovimientoComercialId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.HistorialAprobaciones)
            .WithOne(r => r.MovimientoComercial)
            .HasForeignKey(r => r.MovimientoComercialId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.Tipo);
        builder.HasIndex(m => m.Estado);
        builder.HasIndex(m => m.Fecha);
        builder.HasIndex(m => m.ClienteId);
        builder.HasIndex(m => m.TemporadaId);
    }
}
