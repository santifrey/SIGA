using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;

namespace SIGA.Infrastructure.Persistence.Configurations;

public class ParticipanteComercialConfiguration : IEntityTypeConfiguration<ParticipanteComercial>
{
    public void Configure(EntityTypeBuilder<ParticipanteComercial> builder)
    {
        builder.ToTable("ParticipantesComerciales");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.TipoParticipante)
            .IsRequired();

        builder.Property(p => p.Estado)
            .IsRequired();

        builder.Property(p => p.FechaRegistro)
            .IsRequired();

        builder.Property(p => p.IsDeleted)
            .HasDefaultValue(false);
    }
}

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");

        builder.Property(c => c.CodigoCliente)
            .HasMaxLength(50);

        builder.HasIndex(c => c.CodigoCliente);

        builder.Property(c => c.Observaciones)
            .HasMaxLength(500);

        builder.HasOne(c => c.Persona)
            .WithOne(p => p.Cliente)
            .HasForeignKey<Cliente>(c => c.PersonaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Contrato)
            .WithOne(co => co.Cliente)
            .HasForeignKey<Cliente>(c => c.ContratoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => c.ContratoId)
            .IsUnique();
    }
}
