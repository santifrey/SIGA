using Moq;
using SIGA.Application.Common;
using SIGA.Application.DTOs.PreciosReferencia;
using SIGA.Application.Services;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Exceptions;
using SIGA.Domain.Interfaces;
using Xunit;

namespace SIGA.UnitTests;

public class GestionPreciosReferenciaTests
{
    private readonly Mock<IPrecioClaseRepository> _precioClaseRepoMock;
    private readonly Mock<ITemporadaRepository> _temporadaRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly GestionPreciosReferencia _gestionPrecios;

    public GestionPreciosReferenciaTests()
    {
        _precioClaseRepoMock = new Mock<IPrecioClaseRepository>();
        _temporadaRepoMock = new Mock<ITemporadaRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _gestionPrecios = new GestionPreciosReferencia(
            _precioClaseRepoMock.Object,
            _temporadaRepoMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task ConfigurarPrecioReferencia_CreatesNewPrice_WhenNotExists()
    {
        // Arrange
        var temporada = new Temporada("Temporada 2026", 1) { Id = 1 };
        _temporadaRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(temporada);
        _precioClaseRepoMock.Setup(r => r.GetByTemporadaAndClaseAsync(1, ClaseBulbo.Clase3, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PrecioClase?)null);

        var dto = new ConfigurarPrecioReferenciaDto
        {
            TemporadaId = 1,
            ClaseBulbo = ClaseBulbo.Clase3,
            PrecioReferencia = 120.50m
        };

        // Act
        var res = await _gestionPrecios.ConfigurarPrecioReferenciaAsync(dto);

        // Assert
        Assert.True(res.Success);
        Assert.Equal(201, res.StatusCode);
        Assert.Equal(120.50m, res.Data!.PrecioReferencia);
        Assert.Equal(ClaseBulbo.Clase3, res.Data.ClaseBulbo);
        Assert.Equal("Temporada 2026", res.Data.NombreTemporada);
        _precioClaseRepoMock.Verify(r => r.AddAsync(It.IsAny<PrecioClase>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfigurarPrecioReferencia_UpdatesPrice_WhenExists()
    {
        // Arrange
        var temporada = new Temporada("Temporada 2026", 1) { Id = 1 };
        var precioExistente = new PrecioClase(1, ClaseBulbo.MIX, 100.00m) { Id = 5 };

        _temporadaRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(temporada);
        _precioClaseRepoMock.Setup(r => r.GetByTemporadaAndClaseAsync(1, ClaseBulbo.MIX, It.IsAny<CancellationToken>()))
            .ReturnsAsync(precioExistente);

        var dto = new ConfigurarPrecioReferenciaDto
        {
            TemporadaId = 1,
            ClaseBulbo = ClaseBulbo.MIX,
            PrecioReferencia = 150.00m
        };

        // Act
        var res = await _gestionPrecios.ConfigurarPrecioReferenciaAsync(dto);

        // Assert
        Assert.True(res.Success);
        Assert.Equal(200, res.StatusCode);
        Assert.Equal(150.00m, res.Data!.PrecioReferencia);
        Assert.Equal(ClaseBulbo.MIX, res.Data.ClaseBulbo);
        _precioClaseRepoMock.Verify(r => r.UpdateAsync(precioExistente, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task ConfigurarPrecioReferencia_WithZeroOrNegativePrice_ThrowsOperacionInvalidaException(decimal precioInvalido)
    {
        var dto = new ConfigurarPrecioReferenciaDto
        {
            TemporadaId = 1,
            ClaseBulbo = ClaseBulbo.Clase1,
            PrecioReferencia = precioInvalido
        };

        await Assert.ThrowsAsync<OperacionInvalidaException>(() =>
            _gestionPrecios.ConfigurarPrecioReferenciaAsync(dto));
    }

    [Fact]
    public async Task ConfigurarPrecioReferencia_NonExistentSeason_ThrowsNotFoundException()
    {
        _temporadaRepoMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Temporada?)null);

        var dto = new ConfigurarPrecioReferenciaDto
        {
            TemporadaId = 999,
            ClaseBulbo = ClaseBulbo.Clase1,
            PrecioReferencia = 50m
        };

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _gestionPrecios.ConfigurarPrecioReferenciaAsync(dto));
    }

    [Fact]
    public async Task ConfigurarPreciosTemporada_BatchConfiguresClasses0Through4AndMix()
    {
        // Arrange
        var temporada = new Temporada("Temporada 2026", 1) { Id = 1 };
        _temporadaRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(temporada);

        var existentes = new List<PrecioClase>
        {
            new PrecioClase(1, ClaseBulbo.Clase1, 80m) { Id = 1 }
        };
        _precioClaseRepoMock.Setup(r => r.GetByTemporadaAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existentes);

        var dto = new ConfigurarPreciosTemporadaDto
        {
            TemporadaId = 1,
            Precios = new List<ItemPrecioTemporadaDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase0, PrecioReferencia = 50m },
                new() { ClaseBulbo = ClaseBulbo.Clase1, PrecioReferencia = 85m }, // Update
                new() { ClaseBulbo = ClaseBulbo.Clase2, PrecioReferencia = 110m },
                new() { ClaseBulbo = ClaseBulbo.Clase3, PrecioReferencia = 140m },
                new() { ClaseBulbo = ClaseBulbo.Clase4, PrecioReferencia = 180m },
                new() { ClaseBulbo = ClaseBulbo.MIX, PrecioReferencia = 115m }
            }
        };

        // Act
        var res = await _gestionPrecios.ConfigurarPreciosTemporadaAsync(dto);

        // Assert
        Assert.True(res.Success);
        Assert.NotNull(res.Data);
        var list = res.Data.ToList();
        Assert.Equal(6, list.Count);
        _precioClaseRepoMock.Verify(r => r.AddAsync(It.IsAny<PrecioClase>(), It.IsAny<CancellationToken>()), Times.Exactly(5));
        _precioClaseRepoMock.Verify(r => r.UpdateAsync(It.IsAny<PrecioClase>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListarPreciosPorTemporada_ReturnsAll6Classes_WithZeroForUnconfigured()
    {
        // Arrange
        var temporada = new Temporada("Temporada 2026", 1) { Id = 1 };
        _temporadaRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(temporada);

        // Only Clase1 and MIX are configured
        var existentes = new List<PrecioClase>
        {
            new PrecioClase(1, ClaseBulbo.Clase1, 95m) { Id = 10 },
            new PrecioClase(1, ClaseBulbo.MIX, 105m) { Id = 11 }
        };
        _precioClaseRepoMock.Setup(r => r.GetByTemporadaAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existentes);

        // Act
        var res = await _gestionPrecios.ListarPreciosPorTemporadaAsync(1);

        // Assert
        Assert.True(res.Success);
        Assert.NotNull(res.Data);
        var list = res.Data.ToList();
        Assert.Equal(6, list.Count);

        var c1 = list.First(p => p.ClaseBulbo == ClaseBulbo.Clase1);
        Assert.Equal(95m, c1.PrecioReferencia);

        var mix = list.First(p => p.ClaseBulbo == ClaseBulbo.MIX);
        Assert.Equal(105m, mix.PrecioReferencia);

        var c0 = list.First(p => p.ClaseBulbo == ClaseBulbo.Clase0);
        Assert.Equal(0m, c0.PrecioReferencia);

        var c3 = list.First(p => p.ClaseBulbo == ClaseBulbo.Clase3);
        Assert.Equal(0m, c3.PrecioReferencia);
    }

    [Fact]
    public async Task ObtenerPrecioVigente_ReturnsConfiguredPriceForClass()
    {
        // Arrange
        var temporada = new Temporada("Temporada 2026", 1) { Id = 1 };
        _temporadaRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(temporada);

        var precio = new PrecioClase(1, ClaseBulbo.MIX, 130m) { Id = 20 };
        _precioClaseRepoMock.Setup(r => r.GetByTemporadaAndClaseAsync(1, ClaseBulbo.MIX, It.IsAny<CancellationToken>()))
            .ReturnsAsync(precio);

        // Act
        var res = await _gestionPrecios.ObtenerPrecioVigenteAsync(1, ClaseBulbo.MIX);

        // Assert
        Assert.True(res.Success);
        Assert.Equal(130m, res.Data!.PrecioReferencia);
        Assert.Equal(ClaseBulbo.MIX, res.Data.ClaseBulbo);
        Assert.Equal("Temporada 2026", res.Data.NombreTemporada);
    }

    [Fact]
    public async Task ObtenerPrecioVigente_WhenPriceNotConfigured_ReturnsZeroPriceWithoutThrowing()
    {
        // Arrange
        var temporada = new Temporada("Temporada 2026", 1) { Id = 1 };
        _temporadaRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(temporada);
        _precioClaseRepoMock.Setup(r => r.GetByTemporadaAndClaseAsync(1, ClaseBulbo.Clase4, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PrecioClase?)null);

        // Act
        var res = await _gestionPrecios.ObtenerPrecioVigenteAsync(1, ClaseBulbo.Clase4);

        // Assert
        Assert.True(res.Success);
        Assert.Equal(0m, res.Data!.PrecioReferencia);
        Assert.Equal(ClaseBulbo.Clase4, res.Data.ClaseBulbo);
    }
}
