using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGA.Domain.Entities;

namespace SIGA.Infrastructure.Persistence.Configurations;

public class RegistroAprobacionConfiguration : IEntityTypeConfiguration<RegistroAprobacion>
{
    public void Configure(EntityTypeBuilder<RegistroAprobacion> builder)
    {
        builder.ToTable("RegistrosAprobacion");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.TipoEntidad)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Accion)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(r => r.Observaciones)
            .HasMaxLength(500);

        builder.HasOne(r => r.UsuarioAprobador)
            .WithMany()
            .HasForeignKey(r => r.UsuarioAprobadorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
