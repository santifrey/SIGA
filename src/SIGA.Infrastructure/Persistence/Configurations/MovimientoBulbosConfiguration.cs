using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGA.Domain.Entities;

namespace SIGA.Infrastructure.Persistence.Configurations;

public class MovimientoBulbosConfiguration : IEntityTypeConfiguration<MovimientoBulbos>
{
    public void Configure(EntityTypeBuilder<MovimientoBulbos> builder)
    {
        builder.ToTable("MovimientosBulbos");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Fecha)
            .IsRequired();

        builder.Property(m => m.Estado)
            .IsRequired();

        builder.Property(m => m.Observaciones)
            .HasMaxLength(500);

        builder.Property(m => m.IsDeleted)
            .HasDefaultValue(false);

        builder.HasOne(m => m.Origen)
            .WithMany()
            .HasForeignKey(m => m.OrigenId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Destino)
            .WithMany()
            .HasForeignKey(m => m.DestinoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.UsuarioCreador)
            .WithMany()
            .HasForeignKey(m => m.UsuarioCreadorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(m => m.Cantidades)
            .WithOne(c => c.MovimientoBulbos)
            .HasForeignKey(c => c.MovimientoBulbosId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
