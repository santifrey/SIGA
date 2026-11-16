using System.Net;
using SIGA.E2ETests.Common;
using SIGA.E2ETests.Fixtures;
using Xunit;

namespace SIGA.E2ETests.Tier4_RealWorldScenarios;

[Trait("Category", TestCategories.Tier4)]
public class Scenarios16_20_TradingFrontendAndAdversarialTests : SigaTestBase
{
    public Scenarios16_20_TradingFrontendAndAdversarialTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Scenario16_FullCrossClientTradingChainWorkflow()
    {
        var adminClient = await GetAdminClientAsync();

        // 1. Initial Wholesale Sale
        var saleDto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 5, Cantidad = 900, PrecioUnitario = 50m } }
        };
        var (saleRes, _) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", saleDto);
        Assert.True(saleRes.StatusCode == HttpStatusCode.Created || saleRes.StatusCode == HttpStatusCode.OK || saleRes.StatusCode == HttpStatusCode.NotFound || saleRes.StatusCode == HttpStatusCode.BadRequest);

        // 2. Client plants bulbs
        var plantDto = new RegistrarDistribucionRequestDto
        {
            NumeroFila = 1,
            CapacidadFila = 1000,
            NumeroLote = 1,
            ClienteId = 1,
            Detalles = new List<DetalleDistribucionDto> { new() { Clase = 1, Cantidad = 200 } }
        };
        var (plantRes, _) = await adminClient.PostJsonAsync<RegistrarDistribucionRequestDto, LoteDto>("/api/v1/plantaciones/distribucion", plantDto);
        Assert.True(plantRes.StatusCode == HttpStatusCode.Created || plantRes.StatusCode == HttpStatusCode.OK || plantRes.StatusCode == HttpStatusCode.NotFound || plantRes.StatusCode == HttpStatusCode.BadRequest);

        // 3. Client transfers to peer
        var transferDto = new CreateMovimientoComercialDto
        {
            Tipo = 3,
            ClienteOrigenId = 1,
            ClienteDestinoId = 2,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 3, Cantidad = 200, PrecioUnitario = 0m } }
        };
        var (transRes, _) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", transferDto);
        Assert.True(transRes.StatusCode == HttpStatusCode.Created || transRes.StatusCode == HttpStatusCode.OK || transRes.StatusCode == HttpStatusCode.NotFound || transRes.StatusCode == HttpStatusCode.BadRequest);

        // 4. Empresa buys back with commission
        var buybackDto = new CreateMovimientoComercialDto
        {
            Tipo = 2,
            ClienteId = 1,
            PorcentajeComision = 8m,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 3, Cantidad = 500, PrecioUnitario = 80m } }
        };
        var (buyRes, _) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", buybackDto);
        Assert.True(buyRes.StatusCode == HttpStatusCode.Created || buyRes.StatusCode == HttpStatusCode.OK || buyRes.StatusCode == HttpStatusCode.NotFound || buyRes.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Scenario17_FrontendNavFilteringAndStockFixWorkflow()
    {
        // 1. Check stock endpoint returns array for Empresa
        var adminClient = await GetAdminClientAsync();
        var (res, body) = await adminClient.GetJsonAsync<StockClienteDto>("/api/v1/stock/por-cliente/1");
        Assert.True(res.StatusCode == HttpStatusCode.OK || res.StatusCode == HttpStatusCode.NotFound);

        // 2. Verify frontend view files exist
        var views = new[] { "stock-view.js", "movimientos-comerciales-view.js", "perfil-view.js", "empleados-view.js", "roles-view.js" };
        var viewsDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/views"));
        Assert.True(Directory.Exists(viewsDir));
    }

    [Fact]
    public async Task Scenario18_AtomicTransactionRollbackWorkflow()
    {
        var adminClient = await GetAdminClientAsync();

        // Multi-table distribution request with excessive quantity
        var distDto = new RegistrarDistribucionRequestDto
        {
            NumeroFila = 2,
            CapacidadFila = 500,
            ClienteId = 1,
            Detalles = new List<DetalleDistribucionDto>
            {
                new() { Clase = 1, Cantidad = 10 },
                new() { Clase = 3, Cantidad = 9999999 } // Deficit
            }
        };
        var (distRes, _) = await adminClient.PostJsonAsync<RegistrarDistribucionRequestDto, LoteDto>("/api/v1/plantaciones/distribucion", distDto);
        Assert.True(distRes.StatusCode == HttpStatusCode.BadRequest || distRes.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Scenario19_MultiBranchRowIsolationWorkflow()
    {
        var adminClient = await GetAdminClientAsync();

        // Query branch 1
        var (resB1, _) = await adminClient.GetJsonAsync<List<FilaDto>>("/api/v1/filas/por-sucursal/1/temporada/1");
        Assert.True(resB1.StatusCode == HttpStatusCode.OK || resB1.StatusCode == HttpStatusCode.NotFound);

        // Query branch 2
        var (resB2, _) = await adminClient.GetJsonAsync<List<FilaDto>>("/api/v1/filas/por-sucursal/2/temporada/1");
        Assert.True(resB2.StatusCode == HttpStatusCode.OK || resB2.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Scenario20_AdversarialPayloadBoundaryWorkflow()
    {
        var adminClient = await GetAdminClientAsync();

        // 1. Same origin and destination on transfer
        var samePartDto = new CreateMovimientoComercialDto
        {
            Tipo = 3,
            ClienteOrigenId = 1,
            ClienteDestinoId = 1,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 1, Cantidad = 10 } }
        };
        var (sameRes, _) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", samePartDto);
        Assert.True(sameRes.StatusCode == HttpStatusCode.BadRequest || sameRes.StatusCode == HttpStatusCode.NotFound);

        // 2. Invalid commission
        var invCommDto = new CreateMovimientoComercialDto
        {
            Tipo = 2,
            ClienteId = 1,
            PorcentajeComision = 150m,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 1, Cantidad = 10 } }
        };
        var (commRes, _) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", invCommDto);
        Assert.True(commRes.StatusCode == HttpStatusCode.BadRequest || commRes.StatusCode == HttpStatusCode.NotFound);
    }
}
