using Moq;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Movimientos;
using SIGA.Application.DTOs.Stock;
using SIGA.Application.DTOs.Ventas;
using SIGA.Application.Interfaces;
using SIGA.Application.Services;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Interfaces;
using Xunit;

namespace SIGA.UnitTests;

public class GestionReportesTests
{
    private readonly Mock<IClienteRepository> _clienteRepoMock;
    private readonly Mock<ISucursalRepository> _sucursalRepoMock;
    private readonly Mock<ITemporadaRepository> _temporadaRepoMock;
    private readonly Mock<ILoteRepository> _loteRepoMock;
    private readonly Mock<ICosechaRepository> _cosechaRepoMock;
    private readonly Mock<ITransferenciaLoteRepository> _transferenciaRepoMock;
    private readonly Mock<IVentaRepository> _ventaRepoMock;
    private readonly Mock<IGestionStock> _gestionStockMock;
    private readonly Mock<IGestionMovimientos> _gestionMovimientosMock;
    private readonly Mock<IGestionVentas> _gestionVentasMock;
    private readonly GestionReportes _gestionReportes;

    public GestionReportesTests()
    {
        _clienteRepoMock = new Mock<IClienteRepository>();
        _sucursalRepoMock = new Mock<ISucursalRepository>();
        _temporadaRepoMock = new Mock<ITemporadaRepository>();
        _loteRepoMock = new Mock<ILoteRepository>();
        _cosechaRepoMock = new Mock<ICosechaRepository>();
        _transferenciaRepoMock = new Mock<ITransferenciaLoteRepository>();
        _ventaRepoMock = new Mock<IVentaRepository>();
        _gestionStockMock = new Mock<IGestionStock>();
        _gestionMovimientosMock = new Mock<IGestionMovimientos>();
        _gestionVentasMock = new Mock<IGestionVentas>();

        _gestionReportes = new GestionReportes(
            _clienteRepoMock.Object,
            _sucursalRepoMock.Object,
            _temporadaRepoMock.Object,
            _loteRepoMock.Object,
            _cosechaRepoMock.Object,
            _transferenciaRepoMock.Object,
            _ventaRepoMock.Object,
            _gestionStockMock.Object,
            _gestionMovimientosMock.Object,
            _gestionVentasMock.Object);
    }

    [Fact]
    public async Task ObtenerEstadoIntegralClienteAsync_ShouldConsolidateStockAndProductiveRightsIncludingTransferredLots()
    {
        // Arrange: Cliente #3 tiene un lote propio (#11) y un lote adquirido por transferencia (#10, plantado originalmente por Cliente #2)
        var cliente3 = new Cliente
        {
            Id = 3,
            CodigoCliente = "CLI-003",
            EstadoCliente = EstadoCliente.Activo,
            Persona = new Persona { Id = 30, Nombre = "Lucía", Apellido = "Fernández", Documento = "33444555" }
        };

        var temporada = new Temporada("Temporada 2026", 1) { Id = 1, Estado = EstadoTemporada.Cosecha };
        var sucursal = new Sucursal { Id = 1, Nombre = "Sucursal Central" };
        var fila = new Fila(1, 1, "F-01", 2000) { Id = 5, Sucursal = sucursal };

        // Lote #10 plantado por Cliente #2 (400 bulbos), transferido a Cliente #3
        var loteAdquirido = new Lote(5, 2, 1, 1)
        {
            Id = 10,
            Fila = fila,
            Temporada = temporada,
            CantidadPlantadaTotal = 400,
            Estado = EstadoLote.Cosechado
        };
        loteAdquirido.Transferencias.Add(new TransferenciaLote(10, 2, 3, 1, "Adquisición de lote")
        {
            Id = 50,
            Estado = EstadoTransferencia.Confirmada,
            Fecha = DateTime.UtcNow
        });

        // Lote #11 plantado directamente por Cliente #3 (600 bulbos)
        var lotePropio = new Lote(5, 3, 1, 2)
        {
            Id = 11,
            Fila = fila,
            Temporada = temporada,
            CantidadPlantadaTotal = 600,
            Estado = EstadoLote.Cosechado
        };

        var cosecha = new Cosecha(1, 1, 1) { Id = 100 };
        cosecha.RegistrarDetalle(10, ClaseBulbo.Clase2, 550);
        cosecha.RegistrarDetalle(11, ClaseBulbo.Clase3, 850);
        cosecha.Cerrar();

        _clienteRepoMock.Setup(r => r.GetByIdWithPersonaAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(cliente3);
        _gestionStockMock.Setup(s => s.ConsultarStockPorClienteAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<StockClienteDto>.Ok(new StockClienteDto
            {
                ClienteId = 3,
                TotalBulbos = 1400,
                StockPorClase = new List<StockPorClaseDto>
                {
                    new() { Clase = ClaseBulbo.Clase2, Cantidad = 550 },
                    new() { Clase = ClaseBulbo.Clase3, Cantidad = 850 }
                }
            }));

        _temporadaRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Temporada> { temporada });
        _loteRepoMock.Setup(r => r.GetByTemporadaAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Lote> { loteAdquirido, lotePropio });
        _cosechaRepoMock.Setup(r => r.GetByTemporadaAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Cosecha> { cosecha });
        _gestionMovimientosMock.Setup(m => m.ObtenerPorClienteAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<IEnumerable<MovimientoBulbosDto>>.Ok(new List<MovimientoBulbosDto>()));
        _transferenciaRepoMock.Setup(r => r.GetAllWithDetailsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(loteAdquirido.Transferencias);
        _gestionVentasMock.Setup(v => v.ListarPorClienteAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<IEnumerable<VentaDto>>.Ok(new List<VentaDto>()));

        // Act
        var res = await _gestionReportes.ObtenerEstadoIntegralClienteAsync(3);

        // Assert
        Assert.True(res.Success);
        Assert.Equal(1400, res.Data!.TotalBulbosEnStock);
        Assert.Equal(2, res.Data.TotalLotesVigentes);
        Assert.Equal(1000, res.Data.TotalBulbosPlantadosEnLotesVigentes);

        // Verificar que distingue lote adquirido por transferencia vs lote plantado originalmente
        var derechoLote10 = res.Data.DerechosSobreLotesPlantados.Single(d => d.LoteId == 10);
        Assert.True(derechoLote10.AdquiridoPorTransferencia);
        Assert.Equal(2, derechoLote10.TitularOriginalId);

        var derechoLote11 = res.Data.DerechosSobreLotesPlantados.Single(d => d.LoteId == 11);
        Assert.False(derechoLote11.AdquiridoPorTransferencia);

        // Historial productivo: plantó 600 originalmente y cosechó 1400 (550 + 850) de sus 2 lotes vigentes
        var hist = Assert.Single(res.Data.HistorialProductivoPorTemporada);
        Assert.Equal(600, hist.BulbosPlantados);
        Assert.Equal(1400, hist.BulbosCosechados);
    }

    [Fact]
    public async Task ObtenerRendimientoTemporadaAsync_ShouldComputePlantedVsHarvestedByClassAndBranch()
    {
        // Arrange
        var temporada = new Temporada("Temporada 2026", 1) { Id = 1, Estado = EstadoTemporada.Comercializacion };
        var sucursal = new Sucursal { Id = 1, Nombre = "Sucursal Norte" };
        var fila = new Fila(1, 1, "F-01", 1000) { Id = 5, Sucursal = sucursal };

        var lote = new Lote(5, 2, 1, 1)
        {
            Id = 10,
            Fila = fila,
            Temporada = temporada,
            CantidadPlantadaTotal = 500,
            Estado = EstadoLote.Cosechado
        };
        lote.DetallesPlantacion.Add(new DetallePlantacion(1, 10, ClaseBulbo.Clase1, 300));
        lote.DetallesPlantacion.Add(new DetallePlantacion(1, 10, ClaseBulbo.Clase2, 200));

        var cosecha = new Cosecha(1, 1, 1) { Id = 20, Sucursal = sucursal };
        cosecha.RegistrarDetalle(10, ClaseBulbo.Clase1, 250);
        cosecha.RegistrarDetalle(10, ClaseBulbo.Clase2, 400);
        cosecha.RegistrarDetalle(10, ClaseBulbo.Clase3, 150);
        cosecha.Cerrar();

        _temporadaRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(temporada);
        _loteRepoMock.Setup(r => r.GetByTemporadaAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Lote> { lote });
        _cosechaRepoMock.Setup(r => r.GetByTemporadaAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Cosecha> { cosecha });
        _sucursalRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Sucursal> { sucursal });

        // Act
        var res = await _gestionReportes.ObtenerRendimientoTemporadaAsync(1);

        // Assert
        Assert.True(res.Success);
        Assert.Equal(500, res.Data!.TotalPlantadoTemporada);
        Assert.Equal(800, res.Data.TotalCosechadoTemporada);
        Assert.Equal(300, res.Data.DiferenciaNetaTemporada);
        Assert.Equal(160m, res.Data.PorcentajeRendimientoGlobal); // 800 / 500 * 100 = 160%

        var suc = Assert.Single(res.Data.DesglosePorSucursal);
        Assert.Equal(500, suc.TotalPlantado);
        Assert.Equal(800, suc.TotalCosechado);
    }

    [Fact]
    public async Task ObtenerResumenEjecutivoAsync_ShouldReturnConsolidatedSystemKpis()
    {
        // Arrange
        var clientes = new List<Cliente>
        {
            new() { Id = 2, EstadoCliente = EstadoCliente.Activo },
            new() { Id = 3, EstadoCliente = EstadoCliente.Activo }
        };
        var sucursales = new List<Sucursal>
        {
            new() { Id = 1, Estado = EstadoSucursal.Activo }
        };
        var temporadaActiva = new Temporada("Temporada 2026", 1) { Id = 1, Estado = EstadoTemporada.Comercializacion };

        var ventaCompletada = new Venta(1, 2, 1) { Id = 1, Estado = EstadoVenta.Registrada };
        ventaCompletada.AgregarDetalle(ClaseBulbo.Clase2, 100, 150m); // $15,000
        ventaCompletada.Estado = EstadoVenta.Completada;

        _clienteRepoMock.Setup(r => r.GetAllWithPersonaAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(clientes);
        _sucursalRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sucursales);
        _temporadaRepoMock.Setup(r => r.GetActivaAsync(It.IsAny<CancellationToken>())).ReturnsAsync(temporadaActiva);
        _loteRepoMock.Setup(r => r.GetByTemporadaAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Lote>());
        _cosechaRepoMock.Setup(r => r.GetByTemporadaAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Cosecha>());
        _ventaRepoMock.Setup(r => r.GetAllWithDetailsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Venta> { ventaCompletada });

        // Act
        var res = await _gestionReportes.ObtenerResumenEjecutivoAsync();

        // Assert
        Assert.True(res.Success);
        Assert.Equal(2, res.Data!.TotalClientesActivos);
        Assert.Equal(1, res.Data.TotalSucursalesActivas);
        Assert.Equal(15000m, res.Data.MontoTotalVentasCompletadas);
    }
}
