using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGA.Domain.Entities;

namespace SIGA.Infrastructure.Persistence.Configurations;

public class CosechaConfiguration : IEntityTypeConfiguration<Cosecha>
{
    public void Configure(EntityTypeBuilder<Cosecha> builder)
    {
        builder.ToTable("Cosechas");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Estado)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(c => c.Observaciones)
            .HasMaxLength(500);

        // Regla DP-7: Una sola cosecha por sucursal en cada temporada
        builder.HasIndex(c => new { c.TemporadaId, c.SucursalId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(c => c.Temporada)
            .WithMany(t => t.Cosechas)
            .HasForeignKey(c => c.TemporadaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Sucursal)
            .WithMany()
            .HasForeignKey(c => c.SucursalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.UsuarioCreador)
            .WithMany()
            .HasForeignKey(c => c.UsuarioCreadorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.DetallesCosecha)
            .WithOne(d => d.Cosecha)
            .HasForeignKey(d => d.CosechaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
