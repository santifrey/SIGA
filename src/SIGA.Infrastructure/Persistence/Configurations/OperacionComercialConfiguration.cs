using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGA.Domain.Entities;

namespace SIGA.Infrastructure.Persistence.Configurations;

public class OperacionComercialConfiguration : IEntityTypeConfiguration<OperacionComercial>
{
    public void Configure(EntityTypeBuilder<OperacionComercial> builder)
    {
        builder.ToTable("OperacionesComerciales");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Fecha)
            .IsRequired();

        builder.Property(o => o.Tipo)
            .IsRequired();

        builder.Property(o => o.Estado)
            .IsRequired();

        builder.Property(o => o.Observaciones)
            .HasMaxLength(500);

        builder.Property(o => o.IsDeleted)
            .HasDefaultValue(false);

        builder.HasOne(o => o.UsuarioCreador)
            .WithMany()
            .HasForeignKey(o => o.UsuarioCreadorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.Movimientos)
            .WithOne(m => m.OperacionComercial)
            .HasForeignKey(m => m.OperacionComercialId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
