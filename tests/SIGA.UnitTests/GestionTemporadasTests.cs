using Moq;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Temporadas;
using SIGA.Application.Services;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Interfaces;
using Xunit;

namespace SIGA.UnitTests;

public class GestionTemporadasTests
{
    private readonly Mock<ITemporadaRepository> _temporadaRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly GestionTemporadas _service;

    public GestionTemporadasTests()
    {
        _temporadaRepoMock = new Mock<ITemporadaRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _service = new GestionTemporadas(_temporadaRepoMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task CrearTemporadaAsync_WithValidData_ShouldCreateInPlanificadaState()
    {
        // Arrange
        var dto = new CreateTemporadaDto
        {
            Nombre = "Temporada 2026-2027",
            FechaInicio = DateTime.UtcNow
        };

        _temporadaRepoMock.Setup(r => r.GetActivaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((Temporada?)null);

        // Act
        var result = await _service.CrearTemporadaAsync(dto, 1);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(EstadoTemporada.Planificada, result.Data.Estado);
        Assert.Equal("Temporada 2026-2027", result.Data.Nombre);
        _temporadaRepoMock.Verify(r => r.AddAsync(It.IsAny<Temporada>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CambiarEstadoAsync_FromPlanificadaToPlantacion_ShouldSucceed()
    {
        // Arrange
        var temporada = new Temporada("Temporada 2026", 1)
        {
            Id = 5,
            Estado = EstadoTemporada.Planificada
        };

        _temporadaRepoMock.Setup(r => r.GetByIdWithDetailsAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(temporada);

        // Act
        var result = await _service.CambiarEstadoAsync(5, EstadoTemporada.Plantacion);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(EstadoTemporada.Plantacion, result.Data.Estado);
        _temporadaRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Temporada>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CerrarAsync_WhenSeasonIsClosed_ShouldSetFechaFinAndCerradaState()
    {
        // Arrange
        var temporada = new Temporada("Temporada 2025", 1)
        {
            Id = 3,
            Estado = EstadoTemporada.Comercializacion
        };

        _temporadaRepoMock.Setup(r => r.GetByIdWithDetailsAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(temporada);

        // Act
        var result = await _service.CerrarAsync(3);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(EstadoTemporada.Cerrada, result.Data.Estado);
        Assert.NotNull(temporada.FechaFin);
    }
}
