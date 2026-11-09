using Moq;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Ventas;
using SIGA.Application.Interfaces;
using SIGA.Application.Services;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Exceptions;
using SIGA.Domain.Interfaces;
using Xunit;

namespace SIGA.UnitTests;

public class GestionVentasTests
{
    private readonly Mock<IVentaRepository> _ventaRepoMock;
    private readonly Mock<IPrecioClaseRepository> _precioClaseRepoMock;
    private readonly Mock<IClienteRepository> _clienteRepoMock;
    private readonly Mock<ITemporadaRepository> _temporadaRepoMock;
    private readonly Mock<IGestionStock> _gestionStockMock;
    private readonly Mock<IOperacionComercialRepository> _operacionRepoMock;
    private readonly Mock<IMovimientoBulbosRepository> _movimientoRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly GestionVentas _gestionVentas;

    public GestionVentasTests()
    {
        _ventaRepoMock = new Mock<IVentaRepository>();
        _precioClaseRepoMock = new Mock<IPrecioClaseRepository>();
        _clienteRepoMock = new Mock<IClienteRepository>();
        _temporadaRepoMock = new Mock<ITemporadaRepository>();
        _gestionStockMock = new Mock<IGestionStock>();
        _operacionRepoMock = new Mock<IOperacionComercialRepository>();
        _movimientoRepoMock = new Mock<IMovimientoBulbosRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _gestionVentas = new GestionVentas(
            _ventaRepoMock.Object,
            _precioClaseRepoMock.Object,
            _clienteRepoMock.Object,
            _temporadaRepoMock.Object,
            _gestionStockMock.Object,
            _operacionRepoMock.Object,
            _movimientoRepoMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task ConfigurarPrecioClaseAsync_ShouldCreateOrUpdateReferencePrice()
    {
        // Arrange
        var temporada = new Temporada("Temporada 2026", 1) { Id = 1 };
        _temporadaRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(temporada);
        _precioClaseRepoMock.Setup(r => r.GetByTemporadaAndClaseAsync(1, ClaseBulbo.Clase3, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PrecioClase?)null);

        var dto = new UpsertPrecioClaseDto
        {
            TemporadaId = 1,
            ClaseBulbo = ClaseBulbo.Clase3,
            PrecioReferencia = 120.50m
        };

        // Act
        var res = await _gestionVentas.ConfigurarPrecioClaseAsync(dto);

        // Assert
        Assert.True(res.Success);
        Assert.Equal(120.50m, res.Data!.PrecioReferencia);
        Assert.Equal(ClaseBulbo.Clase3, res.Data.ClaseBulbo);
        _precioClaseRepoMock.Verify(r => r.AddAsync(It.IsAny<PrecioClase>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CrearVentaAsync_ShouldResolveReferencePriceAndSupportNegotiatedPriceOverride()
    {
        // Arrange (Regla DP-4: Precio de referencia por clase + excepción negociada en una venta particular)
        var cliente = new Cliente
        {
            Id = 2,
            CodigoCliente = "CLI-002",
            Persona = new Persona { Id = 10, Nombre = "Carlos", Apellido = "Pérez" }
        };
        var temporada = new Temporada("Temporada 2026", 1) { Id = 1 };

        _clienteRepoMock.Setup(r => r.GetByIdWithPersonaAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(cliente);
        _temporadaRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(temporada);

        // Precio de referencia para Clase 2 = $100.00
        _precioClaseRepoMock.Setup(r => r.GetByTemporadaAndClaseAsync(1, ClaseBulbo.Clase2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PrecioClase(1, ClaseBulbo.Clase2, 100m));

        // Precio de referencia para Clase 4 = $200.00 (pero en la venta se negocia a $180.00)
        _precioClaseRepoMock.Setup(r => r.GetByTemporadaAndClaseAsync(1, ClaseBulbo.Clase4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PrecioClase(1, ClaseBulbo.Clase4, 200m));

        var dto = new CreateVentaDto
        {
            ClienteId = 2,
            TemporadaId = 1,
            Observaciones = "Venta con precio de lista y precio negociado",
            Detalles = new List<ItemDetalleVentaCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase2, Cantidad = 100, PrecioUnitarioPactado = null },  // Usa referencia: 100 * 100 = 10,000
                new() { ClaseBulbo = ClaseBulbo.Clase4, Cantidad = 50, PrecioUnitarioPactado = 180m }    // Usa negociado: 50 * 180 = 9,000
            }
        };

        // Act
        var res = await _gestionVentas.CrearVentaAsync(dto, 1);

        // Assert
        Assert.True(res.Success);
        Assert.Equal(EstadoVenta.Registrada, res.Data!.Estado);
        Assert.Equal(150, res.Data.TotalBulbos);
        Assert.Equal(19000m, res.Data.MontoTotal);
    }

    [Fact]
    public async Task AprobarAdministracionAsync_WithoutGeneratedContract_ShouldThrowOperacionInvalidaException()
    {
        // Arrange
        var venta = new Venta(1, 2, 1) { Id = 10 };
        venta.AgregarDetalle(ClaseBulbo.Clase2, 100, 100m);
        venta.EnviarARevision(1, "Enviada sin contrato aún");

        _ventaRepoMock.Setup(r => r.GetByIdWithDetailsAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(venta);

        // Act & Assert
        await Assert.ThrowsAsync<OperacionInvalidaException>(() =>
            _gestionVentas.AprobarAdministracionAsync(10, new AccionCircuitoVentaDto(), 1));
    }

    [Fact]
    public async Task CircuitoCompletoVenta_ShouldTransitionThroughAllStagesAndCreateConfirmedStockMovement()
    {
        // Arrange: Venta #15 registrada para Cliente #2 (300 bulbos Clase 3 a $150 = $45,000)
        var venta = new Venta(1, 2, 1, 1, "Venta circuito administrativo completo") { Id = 15 };
        venta.AgregarDetalle(ClaseBulbo.Clase3, 300, 150m);

        _ventaRepoMock.Setup(r => r.GetByIdWithDetailsAsync(15, It.IsAny<CancellationToken>())).ReturnsAsync(venta);

        _operacionRepoMock
            .Setup(r => r.AddAsync(It.IsAny<OperacionComercial>(), It.IsAny<CancellationToken>()))
            .Callback<OperacionComercial, CancellationToken>((op, _) => op.Id = 700)
            .ReturnsAsync((OperacionComercial op, CancellationToken _) => op);

        MovimientoBulbos? movimientoConfirmado = null;
        _movimientoRepoMock
            .Setup(r => r.AddAsync(It.IsAny<MovimientoBulbos>(), It.IsAny<CancellationToken>()))
            .Callback<MovimientoBulbos, CancellationToken>((mov, _) => movimientoConfirmado = mov)
            .ReturnsAsync((MovimientoBulbos mov, CancellationToken _) => mov);

        // Paso 1 -> 2: Vendedor envía a revisión de Administración/Secretaría
        var r1 = await _gestionVentas.EnviarARevisionAsync(15, new AccionCircuitoVentaDto { Observaciones = "Revisión solicitada" }, 2);
        Assert.Equal(EstadoVenta.EnRevisionAdministracion, r1.Data!.Estado);

        // Paso 2: Generación de Contrato
        var r2 = await _gestionVentas.GenerarContratoAsync(15, new GenerarContratoDto { NumeroContrato = "CON-2026-0015" }, 3);
        Assert.NotNull(r2.Data!.Contrato);
        Assert.Equal("CON-2026-0015", r2.Data.Contrato!.NumeroContrato);
        Assert.Equal(EstadoContrato.Generado, r2.Data.Contrato.Estado);

        // Paso 3: Secretaría / Administración aprueba el contrato y la operación
        var r3 = await _gestionVentas.AprobarAdministracionAsync(15, new AccionCircuitoVentaDto { Observaciones = "Contrato conforme" }, 3);
        Assert.Equal(EstadoVenta.AprobadaAdministracion, r3.Data!.Estado);
        Assert.Equal(EstadoContrato.Firmado, r3.Data.Contrato!.Estado);

        // Paso 4: Contabilidad emite la factura
        var r4 = await _gestionVentas.EmitirFacturaAsync(15, new EmitirFacturaDto { NumeroFactura = "FAC-A-0001-00000015" }, 5);
        Assert.Equal(EstadoVenta.Facturada, r4.Data!.Estado);
        Assert.NotNull(r4.Data.Factura);
        Assert.Equal(45000m, r4.Data.Factura!.MontoTotal);

        // Paso 5: Aprobación final y generación del movimiento comercial confirmado
        var r5 = await _gestionVentas.AprobarFinalAsync(15, new AccionCircuitoVentaDto { Observaciones = "Entrega autorizada" }, 1);
        Assert.Equal(EstadoVenta.Completada, r5.Data!.Estado);
        Assert.Equal(EstadoFactura.Pagada, r5.Data.Factura!.Estado);
        Assert.Equal(700, r5.Data.OperacionComercialId);
        Assert.Equal(4, r5.Data.HistorialAprobaciones.Count);

        // Verificar movimiento comercial confirmado hacia el Cliente #2
        Assert.NotNull(movimientoConfirmado);
        Assert.Equal(1, movimientoConfirmado!.OrigenId);
        Assert.Equal(2, movimientoConfirmado.DestinoId);
        Assert.Equal(EstadoMovimiento.Confirmado, movimientoConfirmado.Estado);
        Assert.Equal(300, movimientoConfirmado.CalcularTotal());
    }
}
