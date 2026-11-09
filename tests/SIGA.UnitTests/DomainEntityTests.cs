using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using Xunit;

namespace SIGA.UnitTests;

public class DomainEntityTests
{
    [Fact]
    public void Persona_NombreCompleto_ShouldCombineNombreAndApellido()
    {
        // Arrange
        var persona = new Persona
        {
            Nombre = "Martín",
            Apellido = "González"
        };

        // Act & Assert
        Assert.Equal("Martín González", persona.NombreCompleto);
    }

    [Fact]
    public void Usuario_InitialState_ShouldBeActivoAndNotDeleted()
    {
        // Arrange & Act
        var usuario = new Usuario
        {
            Username = "mgonzalez",
            PasswordHash = "hash"
        };

        // Assert
        Assert.Equal(EstadoUsuario.Activo, usuario.Estado);
        Assert.False(usuario.IsDeleted);
        Assert.Null(usuario.FechaEliminacion);
    }

    [Fact]
    public void Cliente_ShouldHaveTipoParticipanteCliente()
    {
        // Arrange & Act
        var cliente = new Cliente
        {
            CodigoCliente = "CLI-001"
        };

        // Assert
        Assert.Equal(TipoParticipante.Cliente, cliente.TipoParticipante);
        Assert.Equal(EstadoCliente.Activo, cliente.EstadoCliente);
    }
}
