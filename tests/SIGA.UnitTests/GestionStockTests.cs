using Moq;
using SIGA.Application.DTOs.Stock;
using SIGA.Application.Services;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Interfaces;
using Xunit;

namespace SIGA.UnitTests;

public class GestionStockTests
{
    private readonly Mock<IMovimientoBulbosRepository> _movimientoRepoMock;
    private readonly Mock<IClienteRepository> _clienteRepoMock;
    private readonly Mock<IRepository<ParticipanteComercial>> _participanteRepoMock;
    private readonly GestionStock _gestionStock;

    public GestionStockTests()
    {
        _movimientoRepoMock = new Mock<IMovimientoBulbosRepository>();
        _clienteRepoMock = new Mock<IClienteRepository>();
        _participanteRepoMock = new Mock<IRepository<ParticipanteComercial>>();
        _gestionStock = new GestionStock(
            _movimientoRepoMock.Object, 
            _clienteRepoMock.Object, 
            _participanteRepoMock.Object);
    }

    [Fact]
    public async Task ConsultarStockPorClienteAsync_ShouldCalculateCorrectBalanceAcrossConfirmedMovements()
    {
        // Arrange: Cliente con ID 2
        var cliente = new Cliente
        {
            Id = 2,
            CodigoCliente = "CLI-002",
            Persona = new Persona { Id = 10, Nombre = "María", Apellido = "González" }
        };

        _clienteRepoMock.Setup(r => r.GetByIdWithPersonaAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliente);

        // Movimiento 1 (Entrada al Cliente 2): Empresa (1) -> Cliente (2): 500 Clase1, 300 Clase2
        var movEntrada = new MovimientoBulbos(1, 1, 2, 1, "Entrada inicial")
        {
            Estado = EstadoMovimiento.Confirmado
        };
        movEntrada.Cantidades.Add(new CantidadBulbos { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 500 });
        movEntrada.Cantidades.Add(new CantidadBulbos { ClaseBulbo = ClaseBulbo.Clase2, Cantidad = 300 });

        // Movimiento 2 (Salida del Cliente 2): Cliente (2) -> Cliente (3): 100 Clase1
        var movSalida = new MovimientoBulbos(2, 2, 3, 1, "Venta parcial")
        {
            Estado = EstadoMovimiento.Confirmado
        };
        movSalida.Cantidades.Add(new CantidadBulbos { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 100 });

        // Movimiento 3 (Pendiente): no debe afectar el saldo
        var movPendiente = new MovimientoBulbos(3, 2, 4, 1, "Pendiente")
        {
            Estado = EstadoMovimiento.Pendiente
        };
        movPendiente.Cantidades.Add(new CantidadBulbos { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 50 });

        _movimientoRepoMock.Setup(r => r.GetConfirmadosByParticipanteAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MovimientoBulbos> { movEntrada, movSalida, movPendiente });

        // Act
        var result = await _gestionStock.ConsultarStockPorClienteAsync(2);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("CLI-002", result.Data.CodigoCliente);
        Assert.Equal(5, result.Data.StockPorClase.Count);

        // Clase 0: 0
        Assert.Equal(0, result.Data.StockPorClase.First(c => c.Clase == ClaseBulbo.Clase0).Cantidad);
        // Clase 1: 500 - 100 = 400
        Assert.Equal(400, result.Data.StockPorClase.First(c => c.Clase == ClaseBulbo.Clase1).Cantidad);
        // Clase 2: 300
        Assert.Equal(300, result.Data.StockPorClase.First(c => c.Clase == ClaseBulbo.Clase2).Cantidad);
        // Total bulbos = 400 + 300 = 700
        Assert.Equal(700, result.Data.TotalBulbos);
    }

    [Fact]
    public async Task VerificarDisponibilidadAsync_WhenStockIsSufficient_ShouldReturnTrue()
    {
        // Arrange
        var cliente = new Cliente { Id = 5, CodigoCliente = "CLI-005" };
        _clienteRepoMock.Setup(r => r.GetByIdWithPersonaAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliente);

        var movEntrada = new MovimientoBulbos(1, 1, 5, 1)
        {
            Estado = EstadoMovimiento.Confirmado
        };
        movEntrada.Cantidades.Add(new CantidadBulbos { ClaseBulbo = ClaseBulbo.Clase3, Cantidad = 250 });

        _movimientoRepoMock.Setup(r => r.GetConfirmadosByParticipanteAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MovimientoBulbos> { movEntrada });

        // Act (solicita 200 de Clase 3)
        var result = await _gestionStock.VerificarDisponibilidadAsync(5, ClaseBulbo.Clase3, 200);

        // Assert
        Assert.True(result.Data.Disponible);
        Assert.Equal(250, result.Data.StockDisponible);
        Assert.Equal(200, result.Data.CantidadSolicitada);
    }

    [Fact]
    public async Task VerificarDisponibilidadAsync_WhenStockIsInsufficient_ShouldReturnFalse()
    {
        // Arrange
        var cliente = new Cliente { Id = 5, CodigoCliente = "CLI-005" };
        _clienteRepoMock.Setup(r => r.GetByIdWithPersonaAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliente);

        var movEntrada = new MovimientoBulbos(1, 1, 5, 1)
        {
            Estado = EstadoMovimiento.Confirmado
        };
        movEntrada.Cantidades.Add(new CantidadBulbos { ClaseBulbo = ClaseBulbo.Clase3, Cantidad = 100 });

        _movimientoRepoMock.Setup(r => r.GetConfirmadosByParticipanteAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MovimientoBulbos> { movEntrada });

        // Act (solicita 150 de Clase 3, pero solo hay 100)
        var result = await _gestionStock.VerificarDisponibilidadAsync(5, ClaseBulbo.Clase3, 150);

        // Assert
        Assert.False(result.Data.Disponible);
        Assert.Equal(100, result.Data.StockDisponible);
        Assert.Equal(150, result.Data.CantidadSolicitada);
    }
}
