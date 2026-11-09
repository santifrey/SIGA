using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGA.Domain.Entities;

namespace SIGA.Infrastructure.Persistence.Configurations;

public class TemporadaConfiguration : IEntityTypeConfiguration<Temporada>
{
    public void Configure(EntityTypeBuilder<Temporada> builder)
    {
        builder.ToTable("Temporadas");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Nombre)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.FechaInicio)
            .IsRequired();

        builder.Property(t => t.FechaFin)
            .IsRequired(false);

        builder.Property(t => t.Estado)
            .IsRequired();

        builder.Property(t => t.FechaCreacion)
            .IsRequired();

        builder.Property(t => t.IsDeleted)
            .HasDefaultValue(false);

        builder.HasOne(t => t.UsuarioCreador)
            .WithMany()
            .HasForeignKey(t => t.UsuarioCreadorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Filas)
            .WithOne(f => f.Temporada)
            .HasForeignKey(f => f.TemporadaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Plantaciones)
            .WithOne(p => p.Temporada)
            .HasForeignKey(p => p.TemporadaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Lotes)
            .WithOne(l => l.Temporada)
            .HasForeignKey(l => l.TemporadaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
