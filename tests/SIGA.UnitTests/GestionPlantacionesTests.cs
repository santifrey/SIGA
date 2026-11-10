using Moq;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Plantaciones;
using SIGA.Application.DTOs.Stock;
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
}
