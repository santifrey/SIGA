using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGA.Domain.Entities;

namespace SIGA.Infrastructure.Persistence.Configurations;

public class PlantacionConfiguration : IEntityTypeConfiguration<Plantacion>
{
    public void Configure(EntityTypeBuilder<Plantacion> builder)
    {
        builder.ToTable("Plantaciones");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.FechaInicio)
            .IsRequired();

        builder.Property(p => p.FechaFin)
            .IsRequired(false);

        builder.Property(p => p.Estado)
            .IsRequired();

        builder.Property(p => p.Observaciones)
            .HasMaxLength(500);

        builder.Property(p => p.FechaCreacion)
            .IsRequired();

        builder.Property(p => p.IsDeleted)
            .HasDefaultValue(false);

        builder.HasOne(p => p.Temporada)
            .WithMany(t => t.Plantaciones)
            .HasForeignKey(p => p.TemporadaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.UsuarioCreador)
            .WithMany()
            .HasForeignKey(p => p.UsuarioCreadorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.DetallesPlantacion)
            .WithOne(d => d.Plantacion)
            .HasForeignKey(d => d.PlantacionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
