using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGA.Domain.Entities;

namespace SIGA.Infrastructure.Persistence.Configurations;

public class RolConfiguration : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Nombre)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(r => r.Nombre)
            .IsUnique();

        builder.Property(r => r.Descripcion)
            .HasMaxLength(255);

        builder.Property(r => r.EsSistema)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasMany(r => r.Usuarios)
            .WithOne(u => u.Rol)
            .HasForeignKey(u => u.RolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.RolPermisos)
            .WithOne(rp => rp.Rol)
            .HasForeignKey(rp => rp.RolId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
