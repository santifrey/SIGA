using Moq;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Personas;
using SIGA.Application.Services;
using SIGA.Domain.Entities;
using SIGA.Domain.Interfaces;
using Xunit;

namespace SIGA.UnitTests;

public class GestionPersonasTests
{
    private readonly Mock<IPersonaRepository> _personaRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly GestionPersonas _service;

    public GestionPersonasTests()
    {
        _personaRepoMock = new Mock<IPersonaRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _service = new GestionPersonas(_personaRepoMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenPersonaExists_ShouldReturnOkResponseWithDto()
    {
        // Arrange
        var persona = new Persona
        {
            Id = 1,
            Nombre = "Juan",
            Apellido = "Pérez",
            Documento = "30123456",
            Email = "juan@test.com"
        };

        _personaRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(persona);

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("Juan Pérez", result.Data.NombreCompleto);
        Assert.Equal("30123456", result.Data.Documento);
    }

    [Fact]
    public async Task GetByIdAsync_WhenPersonaNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        _personaRepoMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Persona?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(999));
    }

    [Fact]
    public async Task CreateAsync_WhenDocumentoAlreadyExists_ShouldThrowValidationException()
    {
        // Arrange
        var dto = new CreatePersonaDto
        {
            Nombre = "Carlos",
            Apellido = "Gómez",
            Documento = "12345678"
        };

        _personaRepoMock.Setup(r => r.GetByDocumentoAsync("12345678", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Persona { Id = 2, Documento = "12345678" });

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
    }
}
