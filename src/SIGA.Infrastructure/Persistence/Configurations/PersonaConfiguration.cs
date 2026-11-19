using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGA.Domain.Entities;

namespace SIGA.Infrastructure.Persistence.Configurations;

public class PersonaConfiguration : IEntityTypeConfiguration<Persona>
{
    public void Configure(EntityTypeBuilder<Persona> builder)
    {
        builder.ToTable("Personas");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Nombre)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Apellido)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Documento)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(p => p.Documento)
            .IsUnique();

        builder.Property(p => p.Telefono)
            .HasMaxLength(50);

        builder.Property(p => p.Email)
            .HasMaxLength(150);

        builder.Property(p => p.FechaCreacion)
            .IsRequired();

        builder.Property(p => p.IsDeleted)
            .HasDefaultValue(false);

        builder.HasOne(p => p.Sucursal)
            .WithMany()
            .HasForeignKey(p => p.SucursalId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Ignore(p => p.NombreCompleto);
    }
}

public class EmpleadoConfiguration : IEntityTypeConfiguration<Empleado>
{
    public void Configure(EntityTypeBuilder<Empleado> builder)
    {
        builder.ToTable("Empleados", tb =>
        {
            tb.Property(e => e.Id).HasColumnName("IdPersona");
        });

        builder.Property(e => e.Legajo)
            .HasMaxLength(50);

        builder.Property(e => e.Posicion)
            .HasMaxLength(100);
    }
}
