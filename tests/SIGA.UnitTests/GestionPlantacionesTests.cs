using Moq;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Plantaciones;
using SIGA.Application.DTOs.Stock;
using SIGA.Application.Common;
using SIGA.Application.Interfaces;
using SIGA.Application.Services;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Interfaces;
using Xunit;

namespace SIGA.UnitTests;

public class GestionPlantacionesTests
{
    private readonly Mock<ITemporadaRepository> _temporadaRepoMock;
    private readonly Mock<IFilaRepository> _filaRepoMock;
    private readonly Mock<ILoteRepository> _loteRepoMock;
    private readonly Mock<IPlantacionRepository> _plantacionRepoMock;
    private readonly Mock<IClienteRepository> _clienteRepoMock;
    private readonly Mock<ISucursalRepository> _sucursalRepoMock;
    private readonly Mock<IGestionStock> _gestionStockMock;
    private readonly Mock<IOperacionComercialRepository> _operacionRepoMock;
    private readonly Mock<IMovimientoBulbosRepository> _movimientoRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly GestionPlantaciones _service;

    public GestionPlantacionesTests()
    {
        _temporadaRepoMock = new Mock<ITemporadaRepository>();
        _filaRepoMock = new Mock<IFilaRepository>();
        _loteRepoMock = new Mock<ILoteRepository>();
        _plantacionRepoMock = new Mock<IPlantacionRepository>();
        _clienteRepoMock = new Mock<IClienteRepository>();
        _sucursalRepoMock = new Mock<ISucursalRepository>();
        _gestionStockMock = new Mock<IGestionStock>();
        _operacionRepoMock = new Mock<IOperacionComercialRepository>();
        _movimientoRepoMock = new Mock<IMovimientoBulbosRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _service = new GestionPlantaciones(
            _temporadaRepoMock.Object,
            _filaRepoMock.Object,
            _loteRepoMock.Object,
            _plantacionRepoMock.Object,
            _clienteRepoMock.Object,
            _sucursalRepoMock.Object,
            _gestionStockMock.Object,
            _operacionRepoMock.Object,
            _movimientoRepoMock.Object,
            _unitOfWorkMock.Object);
    }

    private RegistrarDistribucionDto CreateValidDto()
    {
        return new RegistrarDistribucionDto
        {
            TemporadaId = 1,
            SucursalId = 1,
            ClienteId = 2,
            IdentificacionFila = "1",
            CapacidadFila = 5000,
            Detalles = new List<DetalleDistribucionDto>
            {
                new() { Clase = ClaseBulbo.Clase1, Cantidad = 300 },
                new() { Clase = ClaseBulbo.Clase2, Cantidad = 200 }
            }
        };
    }

    private void SetupValidMocks(RegistrarDistribucionDto dto)
    {
        var cliente = new Cliente { Id = dto.ClienteId, CodigoCliente = "CLI-002" };
        _clienteRepoMock.Setup(r => r.GetByIdWithPersonaAsync(dto.ClienteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliente);

        var temporada = new Temporada("2026", 1) { Id = dto.TemporadaId, Estado = EstadoTemporada.Plantacion };
        _temporadaRepoMock.Setup(r => r.GetByIdAsync(dto.TemporadaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(temporada);

        var sucursal = new Sucursal { Id = dto.SucursalId, Nombre = "Sucursal Central" };
        _sucursalRepoMock.Setup(r => r.GetByIdAsync(dto.SucursalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sucursal);

        _gestionStockMock.Setup(s => s.VerificarDisponibilidadAsync(dto.ClienteId, It.IsAny<ClaseBulbo>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int cId, ClaseBulbo c, int cant, CancellationToken ct) =>
                ApiResponse<DisponibilidadStockResponseDto>.Ok(new DisponibilidadStockResponseDto
                {
                    ClienteId = cId,
                    Clase = c,
                    CantidadSolicitada = cant,
                    StockDisponible = 50000,
                    Disponible = true
                }));

        var plantacion = new Plantacion(dto.TemporadaId, 1) { Id = 20, Estado = EstadoPlantacion.EnProceso };
        _plantacionRepoMock.Setup(r => r.GetByTemporadaIdAsync(dto.TemporadaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plantacion);
    }

    [Fact]
    public async Task RegistrarDistribucionAsync_WhenClientHasInsufficientStock_ShouldThrowValidationException()
    {
        // Arrange
        var dto = new RegistrarDistribucionDto
        {
            TemporadaId = 1,
            SucursalId = 1,
            ClienteId = 2,
            IdentificacionFila = "F-01",
            CapacidadFila = 1000,
            Detalles = new List<DetalleDistribucionDto>
            {
                new() { Clase = ClaseBulbo.Clase1, Cantidad = 500 }
            }
        };

        var cliente = new Cliente { Id = 2, CodigoCliente = "CLI-002" };
        _clienteRepoMock.Setup(r => r.GetByIdWithPersonaAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliente);

        var temporada = new Temporada("2026", 1) { Id = 1, Estado = EstadoTemporada.Plantacion };
        _temporadaRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(temporada);

        var sucursal = new Sucursal { Id = 1, Nombre = "Sucursal Central" };
        _sucursalRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sucursal);

        // Mock stock insuficiente: Disponible = false
        var dispResponse = ApiResponse<DisponibilidadStockResponseDto>.Ok(new DisponibilidadStockResponseDto
        {
            ClienteId = 2,
            Clase = ClaseBulbo.Clase1,
            CantidadSolicitada = 500,
            StockDisponible = 200,
            Disponible = false
        });

        _gestionStockMock.Setup(s => s.VerificarDisponibilidadAsync(2, ClaseBulbo.Clase1, 500, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dispResponse);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.RegistrarDistribucionAsync(dto, 1));
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarDistribucionAsync_WhenClientHasStock_ShouldCreateLoteAndDetallesSuccessfully()
    {
        // Arrange
        var dto = new RegistrarDistribucionDto
        {
            TemporadaId = 1,
            SucursalId = 1,
            ClienteId = 2,
            IdentificacionFila = "F-01",
            CapacidadFila = 1000,
            Detalles = new List<DetalleDistribucionDto>
            {
                new() { Clase = ClaseBulbo.Clase1, Cantidad = 300 },
                new() { Clase = ClaseBulbo.Clase2, Cantidad = 200 }
            }
        };

        var cliente = new Cliente { Id = 2, CodigoCliente = "CLI-002" };
        _clienteRepoMock.Setup(r => r.GetByIdWithPersonaAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliente);

        var temporada = new Temporada("2026", 1) { Id = 1, Estado = EstadoTemporada.Plantacion };
        _temporadaRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(temporada);

        var sucursal = new Sucursal { Id = 1, Nombre = "Sucursal Central" };
        _sucursalRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sucursal);

        // Stock disponible = true para ambas clases
        _gestionStockMock.Setup(s => s.VerificarDisponibilidadAsync(2, It.IsAny<ClaseBulbo>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int cId, ClaseBulbo c, int cant, CancellationToken ct) => 
                ApiResponse<DisponibilidadStockResponseDto>.Ok(new DisponibilidadStockResponseDto
                {
                    ClienteId = cId,
                    Clase = c,
                    CantidadSolicitada = cant,
                    StockDisponible = 1000,
                    Disponible = true
                }));

        var fila = new Fila(1, 1, "F-01", 1000) { Id = 10 };
        _filaRepoMock.Setup(r => r.GetByIdentificacionAsync(1, 1, "F-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fila);

        var plantacion = new Plantacion(1, 1) { Id = 20, Estado = EstadoPlantacion.EnProceso };
        _plantacionRepoMock.Setup(r => r.GetByTemporadaIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plantacion);

        _loteRepoMock.Setup(r => r.GetNextOrdenAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.RegistrarDistribucionAsync(dto, 1);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(500, result.Data.CantidadPlantadaTotal); // 300 + 200
        Assert.Equal(EstadoLote.Plantado, result.Data.Estado);
        _loteRepoMock.Verify(r => r.AddAsync(It.IsAny<Lote>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public async Task RegistrarDistribucionAsync_WhenNumeroFilaIsZeroOrNegative_ShouldThrowValidationException(int invalidRowNumber)
    {
        // Arrange
        var dto = CreateValidDto();
        dto.NumeroFila = invalidRowNumber;

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.RegistrarDistribucionAsync(dto, 1));
        Assert.Contains("número de fila", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegistrarDistribucionAsync_WhenNoRowIdentifierProvided_ShouldThrowValidationException()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.FilaId = null;
        dto.NumeroFila = null;
        dto.IdentificacionFila = "";

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.RegistrarDistribucionAsync(dto, 1));
        Assert.Contains("especificar la fila", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5)]
    public async Task RegistrarDistribucionAsync_WhenNumeroLoteIsZeroOrNegative_ShouldThrowValidationException(int invalidLotOrder)
    {
        // Arrange
        var dto = CreateValidDto();
        dto.NumeroLote = invalidLotOrder;

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.RegistrarDistribucionAsync(dto, 1));
        Assert.Contains("orden", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegistrarDistribucionAsync_WhenDuplicateLotOrderInSameRow_ShouldThrowValidationException()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.FilaId = 10;
        dto.NumeroLote = 2; // Requesting order 2

        SetupValidMocks(dto);

        var existingFila = new Fila(1, 1, 10, 5000) { Id = 10 };
        // Already has a lot with Orden = 2
        existingFila.Lotes.Add(new Lote(10, 2, 1, 2) { CantidadPlantadaTotal = 500 });

        _filaRepoMock.Setup(r => r.GetByIdWithLotesAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFila);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.RegistrarDistribucionAsync(dto, 1));
        Assert.Contains("ya se encuentra ocupado", ex.Message, StringComparison.OrdinalIgnoreCase);
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarDistribucionAsync_WhenExplicitValidLotOrderProvided_ShouldUseProvidedOrder()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.FilaId = 10;
        dto.NumeroLote = 4; // Requesting explicit order 4

        SetupValidMocks(dto);

        var existingFila = new Fila(1, 1, 10, 5000) { Id = 10 };
        _filaRepoMock.Setup(r => r.GetByIdWithLotesAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFila);
        _loteRepoMock.Setup(r => r.ExisteOrdenEnFilaAsync(10, 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.RegistrarDistribucionAsync(dto, 1);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(4, result.Data.Orden);
        _loteRepoMock.Verify(r => r.GetNextOrdenAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _loteRepoMock.Verify(r => r.AddAsync(It.Is<Lote>(l => l.Orden == 4), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarDistribucionAsync_WhenNumeroLoteIsNull_ShouldUseNextAvailableOrderFromRepository()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.FilaId = 10;
        dto.NumeroLote = null;

        SetupValidMocks(dto);

        var existingFila = new Fila(1, 1, 10, 5000) { Id = 10 };
        _filaRepoMock.Setup(r => r.GetByIdWithLotesAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFila);
        _loteRepoMock.Setup(r => r.GetNextOrdenAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        // Act
        var result = await _service.RegistrarDistribucionAsync(dto, 1);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(7, result.Data.Orden);
        _loteRepoMock.Verify(r => r.GetNextOrdenAsync(10, It.IsAny<CancellationToken>()), Times.Once);
        _loteRepoMock.Verify(r => r.AddAsync(It.Is<Lote>(l => l.Orden == 7), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarDistribucionAsync_WhenSingleDistributionExceedsRowCapacity_ShouldThrowValidationException()
    {
        // Arrange: Fila capacity is 1,000. DTO requests 1,200 bulbs.
        var dto = CreateValidDto();
        dto.FilaId = 15;
        dto.Detalles = new List<DetalleDistribucionDto>
        {
            new() { Clase = ClaseBulbo.Clase1, Cantidad = 1200 }
        };

        SetupValidMocks(dto);

        var existingFila = new Fila(1, 1, 15, 1000) { Id = 15 };
        _filaRepoMock.Setup(r => r.GetByIdWithLotesAsync(15, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFila);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.RegistrarDistribucionAsync(dto, 1));
        Assert.Contains("capacidad máxima de la fila", ex.Message, StringComparison.OrdinalIgnoreCase);
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarDistribucionAsync_WhenAccumulatedLotsExceedRowCapacity_ShouldThrowValidationException()
    {
        // Arrange: Fila capacity is 1,000. Existing lot has 700 bulbs. New distribution requests 400 (Total = 1,100 > 1,000).
        var dto = CreateValidDto();
        dto.FilaId = 15;
        dto.Detalles = new List<DetalleDistribucionDto>
        {
            new() { Clase = ClaseBulbo.Clase1, Cantidad = 400 }
        };

        SetupValidMocks(dto);

        var existingFila = new Fila(1, 1, 15, 1000) { Id = 15 };
        existingFila.Lotes.Add(new Lote(15, 2, 1, 1) { CantidadPlantadaTotal = 700 });

        _filaRepoMock.Setup(r => r.GetByIdWithLotesAsync(15, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFila);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.RegistrarDistribucionAsync(dto, 1));
        Assert.Contains("capacidad máxima de la fila", ex.Message, StringComparison.OrdinalIgnoreCase);
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarDistribucionAsync_WhenAccumulatedBulbsExactlyEqualsRowCapacity_ShouldSucceed()
    {
        // Arrange: Fila capacity is 1,000. Existing lot has 700 bulbs. New distribution requests 300 (Total = 1,000 == 1,000).
        var dto = CreateValidDto();
        dto.FilaId = 15;
        dto.Detalles = new List<DetalleDistribucionDto>
        {
            new() { Clase = ClaseBulbo.Clase1, Cantidad = 300 }
        };

        SetupValidMocks(dto);

        var existingFila = new Fila(1, 1, 15, 1000) { Id = 15 };
        existingFila.Lotes.Add(new Lote(15, 2, 1, 1) { CantidadPlantadaTotal = 700 });

        _filaRepoMock.Setup(r => r.GetByIdWithLotesAsync(15, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFila);
        _loteRepoMock.Setup(r => r.GetNextOrdenAsync(15, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        var result = await _service.RegistrarDistribucionAsync(dto, 1);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(300, result.Data.CantidadPlantadaTotal);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarDistribucionAsync_WhenCreatingNewRowWithNegativeCapacity_ShouldThrowValidationException()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.CapacidadFila = -100;

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.RegistrarDistribucionAsync(dto, 1));
        Assert.Contains("capacidad de la fila no puede ser negativa", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegistrarDistribucionAsync_WhenFilaIdProvided_ShouldReuseExistingRowWithoutCreatingNew()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.FilaId = 25;

        SetupValidMocks(dto);

        var existingFila = new Fila(1, 1, 25, 5000) { Id = 25 };
        _filaRepoMock.Setup(r => r.GetByIdWithLotesAsync(25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFila);
        _loteRepoMock.Setup(r => r.GetNextOrdenAsync(25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.RegistrarDistribucionAsync(dto, 1);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(25, result.Data.FilaId);
        _filaRepoMock.Verify(r => r.AddAsync(It.IsAny<Fila>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarDistribucionAsync_WhenFilaIdNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.FilaId = 999;

        SetupValidMocks(dto);

        _filaRepoMock.Setup(r => r.GetByIdWithLotesAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Fila?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.RegistrarDistribucionAsync(dto, 1));
    }

    [Fact]
    public async Task RegistrarDistribucionAsync_WhenFilaIdBelongsToDifferentBranchOrSeason_ShouldThrowValidationException()
    {
        // Arrange: DTO is for Sucursal 1, Temporada 1, but Fila is in Sucursal 2
        var dto = CreateValidDto();
        dto.SucursalId = 1;
        dto.TemporadaId = 1;
        dto.FilaId = 30;

        SetupValidMocks(dto);

        var foreignFila = new Fila(2, 1, 30, 5000) { Id = 30 }; // SucursalId = 2
        _filaRepoMock.Setup(r => r.GetByIdWithLotesAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(foreignFila);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.RegistrarDistribucionAsync(dto, 1));
        Assert.Contains("no corresponde a la sucursal o temporada", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegistrarDistribucionAsync_WhenNumeroFilaMatchesExistingRow_ShouldReuseExistingRow()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.FilaId = null;
        dto.NumeroFila = 12;

        SetupValidMocks(dto);

        var existingFila = new Fila(1, 1, 12, 5000) { Id = 12 };
        _filaRepoMock.Setup(r => r.GetByNumeroAsync(1, 1, 12, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFila);
        _loteRepoMock.Setup(r => r.GetNextOrdenAsync(12, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.RegistrarDistribucionAsync(dto, 1);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(12, result.Data.FilaId);
        _filaRepoMock.Verify(r => r.AddAsync(It.IsAny<Fila>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarDistribucionAsync_WhenNumeroFilaIsNew_ShouldCreateNewRowWithSpecifiedCapacity()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.FilaId = null;
        dto.NumeroFila = 42;
        dto.CapacidadFila = 3500;

        SetupValidMocks(dto);

        _filaRepoMock.Setup(r => r.GetByNumeroAsync(1, 1, 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Fila?)null);
        _filaRepoMock.Setup(r => r.GetByIdentificacionAsync(1, 1, "42", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Fila?)null);

        _loteRepoMock.Setup(r => r.GetNextOrdenAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.RegistrarDistribucionAsync(dto, 1);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        _filaRepoMock.Verify(r => r.AddAsync(It.Is<Fila>(f => 
            f.Numero == 42 && 
            f.Capacidad == 3500 && 
            f.SucursalId == 1 && 
            f.TemporadaId == 1), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarDistribucionAsync_WhenOnlyLegacyIdentificacionFilaProvided_ShouldSucceed()
    {
        // Arrange: Only IdentificacionFila = "Bancal-Norte" provided
        var dto = CreateValidDto();
        dto.FilaId = null;
        dto.NumeroFila = null;
        dto.IdentificacionFila = "Bancal-Norte";
        dto.CapacidadFila = 2000;

        SetupValidMocks(dto);

        var existingFila = new Fila(1, 1, "Bancal-Norte", 2000) { Id = 77 };
        _filaRepoMock.Setup(r => r.GetByIdentificacionAsync(1, 1, "Bancal-Norte", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFila);
        _loteRepoMock.Setup(r => r.GetNextOrdenAsync(77, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.RegistrarDistribucionAsync(dto, 1);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(77, result.Data.FilaId);
    }

    [Fact]
    public async Task RegistrarDistribucionAsync_WhenTemporadaIsClosed_ShouldThrowValidationException()
    {
        // Arrange
        var dto = CreateValidDto();

        var cliente = new Cliente { Id = dto.ClienteId, CodigoCliente = "CLI-002" };
        _clienteRepoMock.Setup(r => r.GetByIdWithPersonaAsync(dto.ClienteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliente);

        var temporadaCerrada = new Temporada("2025", 1) { Id = dto.TemporadaId, Estado = EstadoTemporada.Cerrada };
        _temporadaRepoMock.Setup(r => r.GetByIdAsync(dto.TemporadaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(temporadaCerrada);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.RegistrarDistribucionAsync(dto, 1));
        Assert.Contains("temporada cerrada", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegistrarDistribucionAsync_WhenDetallesIsEmpty_ShouldThrowValidationException()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.Detalles = new List<DetalleDistribucionDto>();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.RegistrarDistribucionAsync(dto, 1));
        Assert.Contains("al menos una clase", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegistrarDistribucionAsync_WhenDetalleQuantityIsZero_ShouldThrowValidationException()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.Detalles = new List<DetalleDistribucionDto>
        {
            new() { Clase = ClaseBulbo.Clase1, Cantidad = 0 }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.RegistrarDistribucionAsync(dto, 1));
        Assert.Contains("mayores a cero", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegistrarDistribucionAsync_WhenRowHasZeroCapacity_ShouldAllowAnyBulbQuantity()
    {
        // Arrange: Capacidad 0 means unlimited capacity in domain logic
        var dto = CreateValidDto();
        dto.Detalles = new List<DetalleDistribucionDto>
        {
            new() { Clase = ClaseBulbo.Clase1, Cantidad = 15000 },
            new() { Clase = ClaseBulbo.Clase2, Cantidad = 25000 }
        };
        dto.CapacidadFila = 0;

        SetupValidMocks(dto);

        var unlimitedFila = new Fila(1, 1, 1, 0) { Id = 10 };
        _filaRepoMock.Setup(r => r.GetByNumeroAsync(1, 1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(unlimitedFila);
        _loteRepoMock.Setup(r => r.GetNextOrdenAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.RegistrarDistribucionAsync(dto, 1);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(40000, result.Data.CantidadPlantadaTotal);
    }

    [Fact]
    public async Task RegistrarDistribucionAsync_WhenNoPlantacionExists_ShouldAutoCreatePlantacionAndAdvanceTemporada()
    {
        // Arrange
        var dto = CreateValidDto();
        SetupValidMocks(dto);

        var temporadaPlanificada = new Temporada("2026", 1) { Id = dto.TemporadaId, Estado = EstadoTemporada.Planificada };
        _temporadaRepoMock.Setup(r => r.GetByIdAsync(dto.TemporadaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(temporadaPlanificada);

        // Return null for existing plantacion to trigger auto-creation
        _plantacionRepoMock.Setup(r => r.GetByTemporadaIdAsync(dto.TemporadaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Plantacion?)null);

        var existingFila = new Fila(1, 1, 1, 5000) { Id = 10 };
        _filaRepoMock.Setup(r => r.GetByNumeroAsync(1, 1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFila);
        _loteRepoMock.Setup(r => r.GetNextOrdenAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.RegistrarDistribucionAsync(dto, 1);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(EstadoTemporada.Plantacion, temporadaPlanificada.Estado);
        _temporadaRepoMock.Verify(r => r.UpdateAsync(It.Is<Temporada>(t => t.Estado == EstadoTemporada.Plantacion), It.IsAny<CancellationToken>()), Times.Once);
        _plantacionRepoMock.Verify(r => r.AddAsync(It.Is<Plantacion>(p => p.TemporadaId == dto.TemporadaId && p.Estado == EstadoPlantacion.EnProceso), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CrearPlantacionAsync_WhenTemporadaIsPlanificada_ShouldTransitionToPlantacionAndCreateRecord()
    {
        // Arrange
        var dto = new CreatePlantacionDto
        {
            TemporadaId = 3,
            Observaciones = "Inicio de ciclo"
        };
        var temporada = new Temporada("2026-Ciclo", 1) { Id = 3, Estado = EstadoTemporada.Planificada };
        _temporadaRepoMock.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(temporada);

        _plantacionRepoMock.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken ct) => new Plantacion(3, 1, "Inicio de ciclo")
            {
                Id = 15,
                Estado = EstadoPlantacion.EnProceso,
                Temporada = temporada
            });

        // Act
        var result = await _service.CrearPlantacionAsync(dto, 1);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(EstadoTemporada.Plantacion, temporada.Estado);
        _temporadaRepoMock.Verify(r => r.UpdateAsync(It.Is<Temporada>(t => t.Estado == EstadoTemporada.Plantacion), It.IsAny<CancellationToken>()), Times.Once);
        _plantacionRepoMock.Verify(r => r.AddAsync(It.Is<Plantacion>(p => p.TemporadaId == 3 && p.Estado == EstadoPlantacion.EnProceso), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CrearPlantacionAsync_WhenTemporadaIsClosed_ShouldThrowValidationException()
    {
        // Arrange
        var dto = new CreatePlantacionDto { TemporadaId = 4 };
        var temporadaCerrada = new Temporada("2025", 1) { Id = 4, Estado = EstadoTemporada.Cerrada };
        _temporadaRepoMock.Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(temporadaCerrada);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CrearPlantacionAsync(dto, 1));
        Assert.Contains("temporada cerrada", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ObtenerPorTemporadaAsync_WhenPlantacionDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        _plantacionRepoMock.Setup(r => r.GetByTemporadaIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Plantacion?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.ObtenerPorTemporadaAsync(99));
        Assert.Contains("99", ex.Message);
    }

    [Fact]
    public async Task ObtenerPorTemporadaAsync_WhenPlantacionExists_ShouldReturnPlantacionDto()
    {
        // Arrange
        var temporada = new Temporada("2026", 1) { Id = 8 };
        var plantacion = new Plantacion(8, 1, "Plantacion activa")
        {
            Id = 22,
            Estado = EstadoPlantacion.EnProceso,
            Temporada = temporada
        };
        _plantacionRepoMock.Setup(r => r.GetByTemporadaIdAsync(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plantacion);

        // Act
        var result = await _service.ObtenerPorTemporadaAsync(8);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(22, result.Data.Id);
        Assert.Equal(8, result.Data.TemporadaId);
        Assert.Equal("2026", result.Data.NombreTemporada);
        Assert.Equal(EstadoPlantacion.EnProceso, result.Data.Estado);
    }
}
