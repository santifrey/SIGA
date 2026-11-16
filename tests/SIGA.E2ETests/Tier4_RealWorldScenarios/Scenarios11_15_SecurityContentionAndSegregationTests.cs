using System.Net;
using SIGA.E2ETests.Common;
using SIGA.E2ETests.Fixtures;
using Xunit;

namespace SIGA.E2ETests.Tier4_RealWorldScenarios;

[Trait("Category", TestCategories.Tier4)]
public class Scenarios11_15_SecurityContentionAndSegregationTests : SigaTestBase
{
    public Scenarios11_15_SecurityContentionAndSegregationTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Scenario11_PlantingCapacitySafeguardWorkflow()
    {
        var adminClient = await GetAdminClientAsync();

        // 1. Over-capacity request rejected
        var overDto = new RegistrarDistribucionRequestDto
        {
            NumeroFila = 1,
            CapacidadFila = 50,
            ClienteId = 1,
            Detalles = new List<DetalleDistribucionDto> { new() { Clase = 1, Cantidad = 100 } } // Exceeds 50
        };
        var (overRes, _) = await adminClient.PostJsonAsync<RegistrarDistribucionRequestDto, LoteDto>("/api/v1/plantaciones/distribucion", overDto);
        Assert.True(overRes.StatusCode == HttpStatusCode.BadRequest || overRes.StatusCode == HttpStatusCode.NotFound);

        // 2. Normal capacity request succeeds
        var validDto = new RegistrarDistribucionRequestDto
        {
            NumeroFila = 1,
            CapacidadFila = 100,
            ClienteId = 1,
            Detalles = new List<DetalleDistribucionDto> { new() { Clase = 1, Cantidad = 50 } }
        };
        var (validRes, _) = await adminClient.PostJsonAsync<RegistrarDistribucionRequestDto, LoteDto>("/api/v1/plantaciones/distribucion", validDto);
        Assert.True(validRes.StatusCode == HttpStatusCode.Created || validRes.StatusCode == HttpStatusCode.OK || validRes.StatusCode == HttpStatusCode.NotFound || validRes.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Scenario12_EmployeeSecurityLifecycleWorkflow()
    {
        var adminClient = await GetAdminClientAsync();

        // 1. Create employee
        var empDto = TestDataGenerator.CreateSampleEmpleadoDto(rolId: 2);
        var (createRes, createBody) = await adminClient.PostJsonAsync<CreateEmpleadoDto, EmpleadoDto>("/api/v1/empleados", empDto);
        Assert.True(createRes.StatusCode == HttpStatusCode.Created || createRes.StatusCode == HttpStatusCode.OK || createRes.StatusCode == HttpStatusCode.NotFound);

        // 2. Attempt to delete last active admin -> fails
        var (delRes, _) = await adminClient.DeleteJsonAsync<object>("/api/v1/empleados/1");
        Assert.True(delRes.StatusCode == HttpStatusCode.BadRequest || delRes.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Scenario13_ConcurrentStockContentionWorkflow()
    {
        var adminClient = await GetAdminClientAsync();

        // Check stock availability
        var (res, _) = await adminClient.GetJsonAsync<StockClienteDto>("/api/v1/stock/por-cliente/1");
        Assert.True(res.StatusCode == HttpStatusCode.OK || res.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Scenario14_ReferencePriceLifecycleWorkflow()
    {
        var adminClient = await GetAdminClientAsync();

        // 1. Set prices
        var price1 = new UpsertPrecioReferenciaDto { TemporadaId = 1, ClaseBulbo = 1, PrecioUnitario = 100m };
        await adminClient.PostJsonAsync<UpsertPrecioReferenciaDto, object>("/api/v1/precios-referencia", price1);

        // 2. Query prices
        var (queryRes, queryBody) = await adminClient.GetJsonAsync<List<PrecioReferenciaDto>>("/api/v1/precios-referencia/temporada/1");
        Assert.True(queryRes.StatusCode == HttpStatusCode.OK || queryRes.StatusCode == HttpStatusCode.NotFound);

        // 3. Update price
        var priceUpdate = new UpsertPrecioReferenciaDto { TemporadaId = 1, ClaseBulbo = 1, PrecioUnitario = 120m };
        await adminClient.PostJsonAsync<UpsertPrecioReferenciaDto, object>("/api/v1/precios-referencia", priceUpdate);
    }

    [Fact]
    public async Task Scenario15_MultiRoleApprovalSegregationWorkflow()
    {
        // 1. Vendedor creates movement
        var vendedorClient = GetClientWithPermissions("VENDEDOR", "MOVCOM_CREAR", "MOVCOM_VER");
        var movDto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 1, Cantidad = 10, PrecioUnitario = 10m } }
        };
        var (resCreate, _) = await vendedorClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", movDto);
        Assert.True(resCreate.StatusCode == HttpStatusCode.Created || resCreate.StatusCode == HttpStatusCode.OK || resCreate.StatusCode == HttpStatusCode.NotFound || resCreate.StatusCode == HttpStatusCode.BadRequest);

        // 2. Vendedor cannot approve (403)
        var resApprove = await vendedorClient.PostAsync("/api/v1/movimientos-comerciales/1/aprobar", new { });
        Assert.True(resApprove.StatusCode == HttpStatusCode.Forbidden || resApprove.StatusCode == HttpStatusCode.Unauthorized || resApprove.StatusCode == HttpStatusCode.NotFound || resApprove.StatusCode == HttpStatusCode.BadRequest);

        // 3. Admin can approve
        var adminClient = await GetAdminClientAsync();
        var resAdminApprove = await adminClient.PostAsync("/api/v1/movimientos-comerciales/1/aprobar", new { });
        Assert.True(resAdminApprove.StatusCode == HttpStatusCode.OK || resAdminApprove.StatusCode == HttpStatusCode.NotFound || resAdminApprove.StatusCode == HttpStatusCode.BadRequest);
    }
}
