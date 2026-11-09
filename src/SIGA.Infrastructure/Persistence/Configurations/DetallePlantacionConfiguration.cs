using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGA.Domain.Entities;

namespace SIGA.Infrastructure.Persistence.Configurations;

public class DetallePlantacionConfiguration : IEntityTypeConfiguration<DetallePlantacion>
{
    public void Configure(EntityTypeBuilder<DetallePlantacion> builder)
    {
        builder.ToTable("DetallesPlantacion");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.ClaseBulbo)
            .IsRequired();

        builder.Property(d => d.Cantidad)
            .IsRequired();

        builder.Property(d => d.FechaCreacion)
            .IsRequired();

        builder.Property(d => d.IsDeleted)
            .HasDefaultValue(false);

        builder.HasOne(d => d.Plantacion)
            .WithMany(p => p.DetallesPlantacion)
            .HasForeignKey(d => d.PlantacionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Lote)
            .WithMany(l => l.DetallesPlantacion)
            .HasForeignKey(d => d.LoteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
