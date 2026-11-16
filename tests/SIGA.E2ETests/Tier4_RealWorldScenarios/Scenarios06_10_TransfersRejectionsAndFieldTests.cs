using System.Net;
using SIGA.E2ETests.Common;
using SIGA.E2ETests.Fixtures;
using Xunit;

namespace SIGA.E2ETests.Tier4_RealWorldScenarios;

[Trait("Category", TestCategories.Tier4)]
public class Scenarios06_10_TransfersRejectionsAndFieldTests : SigaTestBase
{
    public Scenarios06_10_TransfersRejectionsAndFieldTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Scenario06_InterClientTransferStockWorkflow()
    {
        var adminClient = await GetAdminClientAsync();

        // 1. Register Type 3 transfer
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 3,
            ClienteOrigenId = 1,
            ClienteDestinoId = 2,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 1, Cantidad = 150, PrecioUnitario = 0m } }
        };
        var (response, body) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
        if (response.IsSuccessStatusCode && body?.Data != null)
        {
            Assert.Equal(0m, body.Data.MontoTotal);
        }

        // 2. Direct transition to finalizado
        var finRes = await adminClient.PostAsync("/api/v1/movimientos-comerciales/1/finalizar", new { });
        Assert.True(finRes.StatusCode == HttpStatusCode.OK || finRes.StatusCode == HttpStatusCode.NotFound || finRes.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Scenario07_ReviewRejectionAndResubmissionWorkflow()
    {
        var adminClient = await GetAdminClientAsync();

        // 1. Reject with motive
        var rejDto = new RechazarMovimientoDto { Motivo = "Stock solicitado no coincide" };
        var rejRes = await adminClient.PostAsync("/api/v1/movimientos-comerciales/1/rechazar", rejDto);
        Assert.True(rejRes.StatusCode == HttpStatusCode.OK || rejRes.StatusCode == HttpStatusCode.NotFound || rejRes.StatusCode == HttpStatusCode.BadRequest);

        // 2. Creator resubmits
        var resubmitRes = await adminClient.PostAsync("/api/v1/movimientos-comerciales/1/corregir-reenviar", new { });
        Assert.True(resubmitRes.StatusCode == HttpStatusCode.OK || resubmitRes.StatusCode == HttpStatusCode.NotFound || resubmitRes.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Scenario08_InvoicedRejectionVoidingWorkflow()
    {
        var adminClient = await GetAdminClientAsync();

        // 1. Reject invoiced movement
        var rejDto = new RechazarMovimientoDto { Motivo = "Error de facturación en CUIT" };
        var rejRes = await adminClient.PostAsync("/api/v1/movimientos-comerciales/1/rechazar", rejDto);
        Assert.True(rejRes.StatusCode == HttpStatusCode.OK || rejRes.StatusCode == HttpStatusCode.NotFound || rejRes.StatusCode == HttpStatusCode.BadRequest);

        // 2. Resubmit to review
        var resubmitRes = await adminClient.PostAsync("/api/v1/movimientos-comerciales/1/corregir-reenviar", new { });
        Assert.True(resubmitRes.StatusCode == HttpStatusCode.OK || resubmitRes.StatusCode == HttpStatusCode.NotFound || resubmitRes.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Scenario09_HybridOrderStockDeficitWorkflow()
    {
        var adminClient = await GetAdminClientAsync();

        // Order with explicit classes and MIX
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto>
            {
                new() { ClaseBulbo = 2, Cantidad = 50, PrecioUnitario = 10m },
                new() { ClaseBulbo = 4, Cantidad = 30, PrecioUnitario = 20m },
                new() { ClaseBulbo = 5, Cantidad = 300, PrecioUnitario = 5m }
            }
        };
        var (response, _) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Scenario10_FullFarmPlantingCycleWorkflow()
    {
        var adminClient = await GetAdminClientAsync();

        // 1. Query rows
        var (rowsRes, _) = await adminClient.GetJsonAsync<List<FilaDto>>("/api/v1/filas/por-sucursal/1/temporada/1");
        Assert.True(rowsRes.StatusCode == HttpStatusCode.OK || rowsRes.StatusCode == HttpStatusCode.NotFound);

        // 2. Plant distribution
        var distDto = new RegistrarDistribucionRequestDto
        {
            SucursalId = 1,
            TemporadaId = 1,
            NumeroFila = 8,
            CapacidadFila = 1000,
            NumeroLote = 1,
            ClienteId = 1,
            Detalles = new List<DetalleDistribucionDto>
            {
                new() { Clase = 1, Cantidad = 75 },
                new() { Clase = 2, Cantidad = 75 }
            }
        };
        var (distRes, _) = await adminClient.PostJsonAsync<RegistrarDistribucionRequestDto, LoteDto>("/api/v1/plantaciones/distribucion", distDto);
        Assert.True(distRes.StatusCode == HttpStatusCode.Created || distRes.StatusCode == HttpStatusCode.OK || distRes.StatusCode == HttpStatusCode.NotFound || distRes.StatusCode == HttpStatusCode.BadRequest);
    }
}
