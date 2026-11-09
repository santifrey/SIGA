using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGA.Domain.Entities;

namespace SIGA.Infrastructure.Persistence.Configurations;

public class DetalleCosechaConfiguration : IEntityTypeConfiguration<DetalleCosecha>
{
    public void Configure(EntityTypeBuilder<DetalleCosecha> builder)
    {
        builder.ToTable("DetallesCosecha");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.ClaseBulbo)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(d => d.Cantidad)
            .IsRequired();

        builder.HasIndex(d => new { d.CosechaId, d.LoteId, d.ClaseBulbo })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(d => d.Lote)
            .WithMany(l => l.DetallesCosecha)
            .HasForeignKey(d => d.LoteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
