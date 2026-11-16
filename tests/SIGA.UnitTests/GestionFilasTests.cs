using Moq;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Filas;
using SIGA.Application.Common;
using SIGA.Application.Services;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Interfaces;
using Xunit;

namespace SIGA.UnitTests;

public class GestionFilasTests
{
    private readonly Mock<IFilaRepository> _filaRepoMock;
    private readonly Mock<ISucursalRepository> _sucursalRepoMock;
    private readonly Mock<ITemporadaRepository> _temporadaRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly GestionFilas _service;

    public GestionFilasTests()
    {
        _filaRepoMock = new Mock<IFilaRepository>();
        _sucursalRepoMock = new Mock<ISucursalRepository>();
        _temporadaRepoMock = new Mock<ITemporadaRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _service = new GestionFilas(
            _filaRepoMock.Object,
            _sucursalRepoMock.Object,
            _temporadaRepoMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task ListarPorSucursalYTemporadaAsync_WhenSucursalNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        _sucursalRepoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sucursal?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.ListarPorSucursalYTemporadaAsync(99, 1));
        Assert.Contains(nameof(Sucursal), ex.Message);
    }

    [Fact]
    public async Task ListarPorSucursalYTemporadaAsync_WhenTemporadaNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var sucursal = new Sucursal { Id = 1, Nombre = "Sucursal Norte" };
        _sucursalRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sucursal);

        _temporadaRepoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Temporada?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.ListarPorSucursalYTemporadaAsync(1, 99));
        Assert.Contains(nameof(Temporada), ex.Message);
    }

    [Fact]
    public async Task ListarPorSucursalYTemporadaAsync_WhenValid_ShouldReturnMappedDtosWithCapacityMetrics()
    {
        // Arrange
        var sucursal = new Sucursal { Id = 2, Nombre = "Sucursal Principal" };
        var temporada = new Temporada("2026-Verano", 1) { Id = 3 };

        _sucursalRepoMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sucursal);
        _temporadaRepoMock.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(temporada);

        var fila = new Fila(2, 3, 7, 5000)
        {
            Id = 15,
            Sucursal = sucursal,
            Temporada = temporada,
            Lotes = new List<Lote>
            {
                new Lote(15, 1, 3, 1) { CantidadPlantadaTotal = 1200, IsDeleted = false },
                new Lote(15, 2, 3, 2) { CantidadPlantadaTotal = 1800, IsDeleted = false },
                new Lote(15, 1, 3, 3) { CantidadPlantadaTotal = 600, IsDeleted = true } // Deleted lot should be ignored
            }
        };

        _filaRepoMock.Setup(r => r.GetBySucursalAndTemporadaAsync(2, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Fila> { fila });

        // Act
        var result = await _service.ListarPorSucursalYTemporadaAsync(2, 3);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);

        var dtoList = result.Data.ToList();
        Assert.Single(dtoList);

        var dto = dtoList[0];
        Assert.Equal(15, dto.Id);
        Assert.Equal(7, dto.NumeroFila);
        Assert.Equal(7, dto.Numero); // Verify Numero alias
        Assert.Equal("7", dto.Identificacion);
        Assert.Equal(5000, dto.Capacidad);
        Assert.Equal(3000, dto.OcupacionActual); // 1200 + 1800 (deleted 600 excluded)
        Assert.Equal(2000, dto.CapacidadDisponible); // 5000 - 3000
        Assert.Equal(2, dto.TotalLotes); // only non-deleted lots counted
        Assert.Equal("Sucursal Principal", dto.NombreSucursal);
        Assert.Equal("2026-Verano", dto.NombreTemporada);
    }

    [Fact]
    public async Task CrearFilaAsync_WhenNumeroFilaIsPositiveAndNew_ShouldCreateFila()
    {
        // Arrange
        var dto = new CreateFilaDto
        {
            SucursalId = 1,
            TemporadaId = 2,
            NumeroFila = 10,
            Capacidad = 4000
        };

        var sucursal = new Sucursal { Id = 1, Nombre = "Sucursal Sur" };
        var temporada = new Temporada("2026", 1) { Id = 2 };

        _sucursalRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sucursal);
        _temporadaRepoMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(temporada);
        _filaRepoMock.Setup(r => r.GetByIdentificacionAsync(1, 2, "10", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Fila?)null);

        // Act
        var result = await _service.CrearFilaAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(10, result.Data.NumeroFila);
        Assert.Equal(10, result.Data.Numero);
        Assert.Equal("10", result.Data.Identificacion);
        Assert.Equal(4000, result.Data.Capacidad);

        _filaRepoMock.Verify(r => r.AddAsync(It.Is<Fila>(f =>
            f.SucursalId == 1 &&
            f.TemporadaId == 2 &&
            f.NumeroFila == 10 &&
            f.Identificacion == "10" &&
            f.Capacidad == 4000), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CrearFilaAsync_WhenDuplicateRowInSameBranchAndSeason_ShouldThrowValidationException()
    {
        // Arrange
        var dto = new CreateFilaDto
        {
            SucursalId = 1,
            TemporadaId = 2,
            NumeroFila = 5,
            Capacidad = 3000
        };

        var sucursal = new Sucursal { Id = 1, Nombre = "Sucursal Sur" };
        var temporada = new Temporada("2026", 1) { Id = 2 };

        _sucursalRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sucursal);
        _temporadaRepoMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(temporada);

        var existingFila = new Fila(1, 2, 5, 3000) { Id = 44 };
        _filaRepoMock.Setup(r => r.GetByIdentificacionAsync(1, 2, "5", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFila);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CrearFilaAsync(dto));
        Assert.Contains("Ya existe una fila con la identificación '5'", ex.Message);
        _filaRepoMock.Verify(r => r.AddAsync(It.IsAny<Fila>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CrearFilaAsync_WhenNegativeCapacity_ShouldThrowValidationException()
    {
        // Arrange
        var dto = new CreateFilaDto
        {
            SucursalId = 1,
            TemporadaId = 1,
            NumeroFila = 1,
            Capacidad = -500
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CrearFilaAsync(dto));
        Assert.Contains("capacidad no puede ser negativa", ex.Message, StringComparison.OrdinalIgnoreCase);
        _filaRepoMock.Verify(r => r.AddAsync(It.IsAny<Fila>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CrearFilaAsync_WhenNumeroFilaIsZeroOrNegative_ShouldThrowValidationException()
    {
        // Arrange
        var dto = new CreateFilaDto
        {
            SucursalId = 1,
            TemporadaId = 1,
            NumeroFila = 0,
            Capacidad = 1000
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CrearFilaAsync(dto));
        Assert.Contains("número de fila debe ser un entero mayor a cero", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CrearFilaAsync_WhenEmptyIdentificationAndNoNumero_ShouldThrowValidationException()
    {
        // Arrange
        var dto = new CreateFilaDto
        {
            SucursalId = 1,
            TemporadaId = 1,
            NumeroFila = null,
            Identificacion = "   ",
            Capacidad = 1000
        };

        var sucursal = new Sucursal { Id = 1, Nombre = "Sucursal Sur" };
        var temporada = new Temporada("2026", 1) { Id = 1 };

        _sucursalRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sucursal);
        _temporadaRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(temporada);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CrearFilaAsync(dto));
        Assert.Contains("especificar la identificación", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_WhenFilaNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        _filaRepoMock.Setup(r => r.GetByIdWithLotesAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Fila?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.ObtenerPorIdAsync(123));
        Assert.Contains(nameof(Fila), ex.Message);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_WhenFilaExists_ShouldReturnMappedDto()
    {
        // Arrange
        var fila = new Fila(1, 2, "F-12", 2000)
        {
            Id = 55,
            NumeroFila = 12
        };

        _filaRepoMock.Setup(r => r.GetByIdWithLotesAsync(55, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fila);

        // Act
        var result = await _service.ObtenerPorIdAsync(55);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(55, result.Data.Id);
        Assert.Equal(12, result.Data.NumeroFila);
        Assert.Equal(12, result.Data.Numero);
        Assert.Equal("F-12", result.Data.Identificacion);
        Assert.Equal(2000, result.Data.Capacidad);
    }
}
