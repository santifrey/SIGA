using Moq;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Movimientos;
using SIGA.Application.Interfaces;
using SIGA.Application.Services;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Exceptions;
using SIGA.Domain.Interfaces;
using Xunit;

namespace SIGA.UnitTests;

public class GestionMovimientosTests
{
    private readonly Mock<IMovimientoBulbosRepository> _movimientoRepoMock;
    private readonly Mock<IOperacionComercialRepository> _operacionRepoMock;
    private readonly Mock<IClienteRepository> _clienteRepoMock;
    private readonly Mock<IRepository<ParticipanteComercial>> _participanteRepoMock;
    private readonly Mock<IGestionStock> _gestionStockMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly GestionMovimientos _service;

    public GestionMovimientosTests()
    {
        _movimientoRepoMock = new Mock<IMovimientoBulbosRepository>();
        _operacionRepoMock = new Mock<IOperacionComercialRepository>();
        _clienteRepoMock = new Mock<IClienteRepository>();
        _participanteRepoMock = new Mock<IRepository<ParticipanteComercial>>();
        _gestionStockMock = new Mock<IGestionStock>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _service = new GestionMovimientos(
            _movimientoRepoMock.Object,
            _operacionRepoMock.Object,
            _clienteRepoMock.Object,
            _participanteRepoMock.Object,
            _gestionStockMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task CrearMovimientoAsync_WhenSameOrigenAndDestino_ShouldThrowOperacionInvalidaException()
    {
        // Arrange
        var dto = new CrearMovimientoDto
        {
            OrigenId = 1,
            DestinoId = 1, // Mismo origen y destino
            Cantidades = new List<CantidadBulbosDto>
            {
                new() { Clase = ClaseBulbo.Clase1, Cantidad = 100 }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<OperacionInvalidaException>(() => _service.CrearMovimientoAsync(dto, 1));
    }

    [Fact]
    public async Task CrearMovimientoAsync_WithValidData_ShouldCreateInPendienteState()
    {
        // Arrange
        var dto = new CrearMovimientoDto
        {
            OrigenId = 1, // Empresa
            DestinoId = 2, // Cliente
            Cantidades = new List<CantidadBulbosDto>
            {
                new() { Clase = ClaseBulbo.Clase2, Cantidad = 500 }
            },
            Observaciones = "Entrega inicial"
        };

        _participanteRepoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ParticipanteComercial, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CrearMovimientoAsync(dto, 1);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(EstadoMovimiento.Pendiente, result.Data.Estado);
        _movimientoRepoMock.Verify(r => r.AddAsync(It.IsAny<MovimientoBulbos>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmarMovimientoAsync_WhenOrigenIsClienteAndHasInsufficientStock_ShouldThrowStockInsuficienteException()
    {
        // Arrange
        var mov = new MovimientoBulbos(1, 2, 3, 1) // Origen: Cliente 2, Destino: Cliente 3
        {
            Id = 10,
            Estado = EstadoMovimiento.Pendiente
        };
        mov.Cantidades.Add(new CantidadBulbos { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 300 });

        _movimientoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);

        var clienteOrigen = new Cliente { Id = 2 };
        _clienteRepoMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(clienteOrigen);

        // Stock disponible para Cliente 2: solo 150 de Clase 1 (necesita 300)
        var saldos = new Dictionary<ClaseBulbo, int>
        {
            { ClaseBulbo.Clase1, 150 }
        };
        _gestionStockMock.Setup(s => s.ObtenerSaldoPorClaseAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(saldos);

        // Act & Assert
        await Assert.ThrowsAsync<StockInsuficienteException>(() => _service.ConfirmarMovimientoAsync(10, 1));
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmarMovimientoAsync_WhenStockIsSufficient_ShouldTransitionToConfirmado()
    {
        // Arrange
        var operacion = new OperacionComercial { Id = 1, Estado = EstadoOperacion.Pendiente };
        var mov = new MovimientoBulbos(1, 2, 3, 1)
        {
            Id = 10,
            Estado = EstadoMovimiento.Pendiente,
            OperacionComercial = operacion
        };
        mov.Cantidades.Add(new CantidadBulbos { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 200 });
        operacion.Movimientos.Add(mov);

        _movimientoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);

        var clienteOrigen = new Cliente { Id = 2 };
        _clienteRepoMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(clienteOrigen);

        // Stock disponible para Cliente 2: 500 de Clase 1 (suficiente para 200)
        var saldos = new Dictionary<ClaseBulbo, int>
        {
            { ClaseBulbo.Clase1, 500 }
        };
        _gestionStockMock.Setup(s => s.ObtenerSaldoPorClaseAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(saldos);

        // Act
        var result = await _service.ConfirmarMovimientoAsync(10, 1);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(EstadoMovimiento.Confirmado, result.Data.Estado);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
