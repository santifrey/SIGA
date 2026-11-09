using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGA.Domain.Entities;

namespace SIGA.Infrastructure.Persistence.Configurations;

public class PrecioClaseConfiguration : IEntityTypeConfiguration<PrecioClase>
{
    public void Configure(EntityTypeBuilder<PrecioClase> builder)
    {
        builder.ToTable("PreciosClase");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.ClaseBulbo)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(p => p.PrecioReferencia)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(p => new { p.TemporadaId, p.ClaseBulbo })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(p => p.Temporada)
            .WithMany(t => t.PreciosClase)
            .HasForeignKey(p => p.TemporadaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
