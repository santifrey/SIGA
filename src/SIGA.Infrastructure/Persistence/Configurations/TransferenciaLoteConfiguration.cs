using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGA.Domain.Entities;

namespace SIGA.Infrastructure.Persistence.Configurations;

public class TransferenciaLoteConfiguration : IEntityTypeConfiguration<TransferenciaLote>
{
    public void Configure(EntityTypeBuilder<TransferenciaLote> builder)
    {
        builder.ToTable("TransferenciasLotes");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Fecha)
            .IsRequired();

        builder.Property(t => t.Estado)
            .IsRequired();

        builder.Property(t => t.Observaciones)
            .HasMaxLength(500);

        builder.Property(t => t.IsDeleted)
            .HasDefaultValue(false);

        builder.HasOne(t => t.Lote)
            .WithMany(l => l.Transferencias)
            .HasForeignKey(t => t.LoteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.TitularAnterior)
            .WithMany()
            .HasForeignKey(t => t.TitularAnteriorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.NuevoTitular)
            .WithMany()
            .HasForeignKey(t => t.NuevoTitularId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.UsuarioCreador)
            .WithMany()
            .HasForeignKey(t => t.UsuarioCreadorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.OperacionComercial)
            .WithMany()
            .HasForeignKey(t => t.OperacionComercialId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
