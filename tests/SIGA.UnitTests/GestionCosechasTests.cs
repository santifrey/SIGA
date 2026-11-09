using Moq;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Cosechas;
using SIGA.Application.Services;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Interfaces;
using Xunit;

namespace SIGA.UnitTests;

public class GestionCosechasTests
{
    private readonly Mock<ICosechaRepository> _cosechaRepoMock;
    private readonly Mock<ITemporadaRepository> _temporadaRepoMock;
    private readonly Mock<ISucursalRepository> _sucursalRepoMock;
    private readonly Mock<ILoteRepository> _loteRepoMock;
    private readonly Mock<IFilaRepository> _filaRepoMock;
    private readonly Mock<IClienteRepository> _clienteRepoMock;
    private readonly Mock<IRepository<ParticipanteComercial>> _participanteRepoMock;
    private readonly Mock<IOperacionComercialRepository> _operacionRepoMock;
    private readonly Mock<IMovimientoBulbosRepository> _movimientoRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly GestionCosechas _gestionCosechas;

    public GestionCosechasTests()
    {
        _cosechaRepoMock = new Mock<ICosechaRepository>();
        _temporadaRepoMock = new Mock<ITemporadaRepository>();
        _sucursalRepoMock = new Mock<ISucursalRepository>();
        _loteRepoMock = new Mock<ILoteRepository>();
        _filaRepoMock = new Mock<IFilaRepository>();
        _clienteRepoMock = new Mock<IClienteRepository>();
        _participanteRepoMock = new Mock<IRepository<ParticipanteComercial>>();
        _operacionRepoMock = new Mock<IOperacionComercialRepository>();
        _movimientoRepoMock = new Mock<IMovimientoBulbosRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _gestionCosechas = new GestionCosechas(
            _cosechaRepoMock.Object,
            _temporadaRepoMock.Object,
            _sucursalRepoMock.Object,
            _loteRepoMock.Object,
            _filaRepoMock.Object,
            _clienteRepoMock.Object,
            _participanteRepoMock.Object,
            _operacionRepoMock.Object,
            _movimientoRepoMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task CrearCosechaAsync_WhenSucursalAlreadyHasCosechaInSeason_ShouldThrowValidationException()
    {
        // Arrange (Regla DP-7: Una sola cosecha por sucursal en cada temporada)
        var temporada = new Temporada("Temporada 2026", 1) { Id = 1, Estado = EstadoTemporada.EnCrecimiento };
        var sucursal = new Sucursal { Id = 1, Nombre = "Sucursal Norte" };
        var existente = new Cosecha(1, 1, 1) { Id = 10 };

        _temporadaRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(temporada);
        _sucursalRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(sucursal);
        _cosechaRepoMock.Setup(r => r.GetByTemporadaAndSucursalAsync(1, 1, It.IsAny<CancellationToken>())).ReturnsAsync(existente);

        var dto = new CreateCosechaDto { TemporadaId = 1, SucursalId = 1, Observaciones = "Intento duplicado" };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _gestionCosechas.CrearCosechaAsync(dto, 1));
    }

    [Fact]
    public async Task CrearCosechaAsync_WhenValid_ShouldTransitionSeasonToCosechaAndCreateRecord()
    {
        // Arrange
        var temporada = new Temporada("Temporada 2026", 1) { Id = 1, Estado = EstadoTemporada.EnCrecimiento };
        var sucursal = new Sucursal { Id = 1, Nombre = "Sucursal Norte" };

        _temporadaRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(temporada);
        _sucursalRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(sucursal);
        _cosechaRepoMock.Setup(r => r.GetByTemporadaAndSucursalAsync(1, 1, It.IsAny<CancellationToken>())).ReturnsAsync((Cosecha?)null);

        var dto = new CreateCosechaDto { TemporadaId = 1, SucursalId = 1, Observaciones = "Cosecha inicial" };

        // Act
        var res = await _gestionCosechas.CrearCosechaAsync(dto, 1);

        // Assert
        Assert.True(res.Success);
        Assert.Equal(EstadoTemporada.Cosecha, temporada.Estado);
        Assert.Equal(EstadoCosecha.EnProceso, res.Data!.Estado);
        _cosechaRepoMock.Verify(r => r.AddAsync(It.IsAny<Cosecha>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarDetalleCosechaAsync_ShouldRegisterQuantitiesPerLotAndClass()
    {
        // Arrange
        var fila = new Fila(1, 1, "F-01", 1000) { Id = 5 };
        var lote = new Lote(5, 2, 1, 1) { Id = 10, Fila = fila, Estado = EstadoLote.Plantado };
        var cosecha = new Cosecha(1, 1, 1) { Id = 20, Estado = EstadoCosecha.EnProceso };

        _cosechaRepoMock.Setup(r => r.GetByIdWithDetailsAsync(20, It.IsAny<CancellationToken>())).ReturnsAsync(cosecha);
        _loteRepoMock.Setup(r => r.GetByIdWithDetailsAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(lote);

        var dto = new RegistrarDetalleCosechaDto
        {
            LoteId = 10,
            Cantidades = new List<ItemDetalleCosechaDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 400 },
                new() { ClaseBulbo = ClaseBulbo.Clase2, Cantidad = 350 }
            }
        };

        // Act
        var res = await _gestionCosechas.RegistrarDetalleCosechaAsync(20, dto, 1);

        // Assert
        Assert.True(res.Success);
        Assert.Equal(750, res.Data!.TotalCosechado);
        Assert.Equal(2, res.Data.Detalles.Count);
    }

    [Fact]
    public async Task CerrarCosechaAsync_WhenLotWasTransferred_ShouldCreditStockToNewTitularAndTransitionSeasonToComercializacion()
    {
        // Arrange:
        // Lote #10 fue plantado originalmente por Cliente #2, pero transferido al Cliente #3.
        // Al cerrar la cosecha, los bulbos cosechados deben acreditarse al Cliente #3 (Titular Vigente)
        // y la temporada debe pasar a Comercializacion al estar todas las sucursales cosechadas.
        var temporada = new Temporada("Temporada 2026", 1) { Id = 1, Estado = EstadoTemporada.Cosecha };
        var fila = new Fila(1, 1, "F-01", 1000) { Id = 5, Estado = EstadoFila.Plantada };
        var lote = new Lote(5, 2, 1, 1)
        {
            Id = 10,
            Fila = fila,
            Temporada = temporada,
            Estado = EstadoLote.Plantado
        };

        // Transferencia confirmada de Cliente 2 -> Cliente 3
        lote.Transferencias.Add(new TransferenciaLote(10, 2, 3, 1, "Transferencia completa")
        {
            Id = 99,
            Estado = EstadoTransferencia.Confirmada,
            Fecha = DateTime.UtcNow
        });

        var cosecha = new Cosecha(1, 1, 1)
        {
            Id = 20,
            Temporada = temporada,
            Estado = EstadoCosecha.EnProceso
        };
        cosecha.RegistrarDetalle(10, ClaseBulbo.Clase2, 600);
        cosecha.RegistrarDetalle(10, ClaseBulbo.Clase3, 250);
        foreach (var d in cosecha.DetallesCosecha)
        {
            d.Lote = lote;
        }

        _cosechaRepoMock.Setup(r => r.GetByIdWithDetailsAsync(20, It.IsAny<CancellationToken>())).ReturnsAsync(cosecha);
        _temporadaRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(temporada);
        _loteRepoMock.Setup(r => r.GetByTemporadaAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Lote> { lote });
        _filaRepoMock.Setup(r => r.GetBySucursalAndTemporadaAsync(1, 1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Fila> { fila });
        _cosechaRepoMock.Setup(r => r.GetByTemporadaAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Cosecha> { cosecha });

        MovimientoBulbos? movimientoCapturado = null;
        _movimientoRepoMock
            .Setup(r => r.AddAsync(It.IsAny<MovimientoBulbos>(), It.IsAny<CancellationToken>()))
            .Callback<MovimientoBulbos, CancellationToken>((m, _) => movimientoCapturado = m)
            .ReturnsAsync((MovimientoBulbos m, CancellationToken _) => m);

        _operacionRepoMock
            .Setup(r => r.AddAsync(It.IsAny<OperacionComercial>(), It.IsAny<CancellationToken>()))
            .Callback<OperacionComercial, CancellationToken>((op, _) => op.Id = 500)
            .ReturnsAsync((OperacionComercial op, CancellationToken _) => op);

        // Act
        var res = await _gestionCosechas.CerrarCosechaAsync(20, 1);

        // Assert
        Assert.True(res.Success);
        Assert.Equal(EstadoCosecha.Cerrada, cosecha.Estado);
        Assert.NotNull(cosecha.FechaFin);
        Assert.Equal(EstadoLote.Cosechado, lote.Estado);
        Assert.Equal(EstadoFila.Cosechada, fila.Estado);

        // Verificar que el movimiento confirmado se acreditó al Cliente #3 (Nuevo Titular Vigente) y NO al Cliente #2
        Assert.NotNull(movimientoCapturado);
        Assert.Equal(1, movimientoCapturado!.OrigenId);
        Assert.Equal(3, movimientoCapturado.DestinoId);
        Assert.Equal(EstadoMovimiento.Confirmado, movimientoCapturado.Estado);
        Assert.Equal(850, movimientoCapturado.CalcularTotal());

        // Verificar que la temporada pasó automáticamente a Comercializacion (DP-7)
        Assert.Equal(EstadoTemporada.Comercializacion, temporada.Estado);
    }
}
