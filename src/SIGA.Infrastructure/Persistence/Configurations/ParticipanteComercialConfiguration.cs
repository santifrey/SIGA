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

        builder.HasDiscriminator(p => p.TipoParticipante)
            .HasValue<Cliente>(TipoParticipante.Cliente)
            .HasValue<Empresa>(TipoParticipante.Empresa);

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
        builder.Property(c => c.CodigoCliente)
            .HasMaxLength(50);

        builder.HasIndex(c => c.CodigoCliente);

        builder.Property(c => c.Observaciones)
            .HasMaxLength(500);

        builder.HasOne(c => c.Persona)
            .WithOne(p => p.Cliente)
            .HasForeignKey<Cliente>(c => c.PersonaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmpresaConfiguration : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> builder)
    {
        builder.Property(e => e.RazonSocial)
            .HasMaxLength(150);

        builder.Property(e => e.CUIT)
            .HasMaxLength(20);

        builder.Property(e => e.DireccionFiscal)
            .HasMaxLength(200);

        builder.Property(e => e.Telefono)
            .HasMaxLength(50);

        builder.Property(e => e.Email)
            .HasMaxLength(150);
    }
}
