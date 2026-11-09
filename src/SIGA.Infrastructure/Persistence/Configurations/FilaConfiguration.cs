using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGA.Domain.Entities;

namespace SIGA.Infrastructure.Persistence.Configurations;

public class FilaConfiguration : IEntityTypeConfiguration<Fila>
{
    public void Configure(EntityTypeBuilder<Fila> builder)
    {
        builder.ToTable("Filas");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Identificacion)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(f => f.Capacidad)
            .IsRequired();

        builder.Property(f => f.Estado)
            .IsRequired();

        builder.Property(f => f.FechaCreacion)
            .IsRequired();

        builder.Property(f => f.IsDeleted)
            .HasDefaultValue(false);

        builder.HasOne(f => f.Sucursal)
            .WithMany()
            .HasForeignKey(f => f.SucursalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Temporada)
            .WithMany(t => t.Filas)
            .HasForeignKey(f => f.TemporadaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(f => f.Lotes)
            .WithOne(l => l.Fila)
            .HasForeignKey(l => l.FilaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(f => new { f.SucursalId, f.TemporadaId, f.Identificacion });
    }
}
