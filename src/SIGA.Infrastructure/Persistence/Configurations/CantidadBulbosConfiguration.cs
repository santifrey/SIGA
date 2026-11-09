using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGA.Domain.Entities;

namespace SIGA.Infrastructure.Persistence.Configurations;

public class CantidadBulbosConfiguration : IEntityTypeConfiguration<CantidadBulbos>
{
    public void Configure(EntityTypeBuilder<CantidadBulbos> builder)
    {
        builder.ToTable("CantidadesBulbos");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.ClaseBulbo)
            .IsRequired();

        builder.Property(c => c.Cantidad)
            .IsRequired();

        builder.HasOne(c => c.MovimientoBulbos)
            .WithMany(m => m.Cantidades)
            .HasForeignKey(c => c.MovimientoBulbosId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
