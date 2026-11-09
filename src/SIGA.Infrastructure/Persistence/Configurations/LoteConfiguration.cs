using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGA.Domain.Entities;

namespace SIGA.Infrastructure.Persistence.Configurations;

public class LoteConfiguration : IEntityTypeConfiguration<Lote>
{
    public void Configure(EntityTypeBuilder<Lote> builder)
    {
        builder.ToTable("Lotes");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Orden)
            .IsRequired();

        builder.Property(l => l.CantidadPlantadaTotal)
            .IsRequired();

        builder.Property(l => l.Estado)
            .IsRequired();

        builder.Property(l => l.FechaCreacion)
            .IsRequired();

        builder.Property(l => l.IsDeleted)
            .HasDefaultValue(false);

        builder.HasOne(l => l.Fila)
            .WithMany(f => f.Lotes)
            .HasForeignKey(l => l.FilaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Cliente)
            .WithMany()
            .HasForeignKey(l => l.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Temporada)
            .WithMany(t => t.Lotes)
            .HasForeignKey(l => l.TemporadaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(l => l.DetallesPlantacion)
            .WithOne(d => d.Lote)
            .HasForeignKey(d => d.LoteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
