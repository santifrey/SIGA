using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGA.Domain.Entities;

namespace SIGA.Infrastructure.Persistence.Configurations;

public class ContratoConfiguration : IEntityTypeConfiguration<Contrato>
{
    public void Configure(EntityTypeBuilder<Contrato> builder)
    {
        builder.ToTable("Contratos");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.NumeroContrato)
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(c => c.ArchivoUrl)
            .HasMaxLength(500);

        builder.Property(c => c.Estado)
            .HasConversion<int>()
            .IsRequired();

        builder.HasIndex(c => c.NumeroContrato)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(c => c.UsuarioGenerador)
            .WithMany()
            .HasForeignKey(c => c.UsuarioGeneradorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
