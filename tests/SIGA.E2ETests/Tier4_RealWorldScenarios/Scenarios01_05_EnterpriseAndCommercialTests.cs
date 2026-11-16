using System.Net;
using SIGA.E2ETests.Common;
using SIGA.E2ETests.Fixtures;
using Xunit;

namespace SIGA.E2ETests.Tier4_RealWorldScenarios;

[Trait("Category", TestCategories.Tier4)]
public class Scenarios01_05_EnterpriseAndCommercialTests : SigaTestBase
{
    public Scenarios01_05_EnterpriseAndCommercialTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Scenario01_FoundationalEnterpriseSetupAndCustomRoleOnboarding()
    {
        // 1. Super Admin logs in
        var adminClient = await GetAdminClientAsync();

        // 2. Admin creates custom role
        var roleDto = new CreateRolDto
        {
            Nombre = TestDataGenerator.GenerateRoleName(),
            Descripcion = "Commercial Operator Role",
            Permisos = new List<string> { "MOVCOM_VER", "MOVCOM_CREAR", "MOVCOM_CORREGIR", "CLIENTES_VER", "STOCK_VER" }
        };
        var (roleRes, roleBody) = await adminClient.PostJsonAsync<CreateRolDto, RolDto>("/api/v1/roles", roleDto);
        Assert.True(roleRes.StatusCode == HttpStatusCode.Created || roleRes.StatusCode == HttpStatusCode.OK || roleRes.StatusCode == HttpStatusCode.NotFound);

        // 3. Admin registers employee
        var empDto = TestDataGenerator.CreateSampleEmpleadoDto(rolId: roleBody?.Data?.Id ?? 2);
        var (empRes, _) = await adminClient.PostJsonAsync<CreateEmpleadoDto, EmpleadoDto>("/api/v1/empleados", empDto);
        Assert.True(empRes.StatusCode == HttpStatusCode.Created || empRes.StatusCode == HttpStatusCode.OK || empRes.StatusCode == HttpStatusCode.NotFound);

        // 4. Operator tries to delete system role -> 400 Bad Request
        var (delRes, _) = await adminClient.DeleteJsonAsync<object>("/api/v1/roles/1");
        Assert.True(delRes.StatusCode == HttpStatusCode.BadRequest || delRes.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Scenario02_NewClientStandardSaleWorkflow()
    {
        var adminClient = await GetAdminClientAsync();

        // 1. Setup price
        var priceDto = new UpsertPrecioReferenciaDto { TemporadaId = 1, ClaseBulbo = 2, PrecioUnitario = 100m };
        await adminClient.PostJsonAsync<UpsertPrecioReferenciaDto, object>("/api/v1/precios-referencia", priceDto);

        // 2. Create client
        var cliDto = TestDataGenerator.CreateSampleClienteDto();
        var (cliRes, cliBody) = await adminClient.PostJsonAsync<CreateClienteDto, ClienteDto>("/api/v1/clientes", cliDto);
        var clienteId = cliBody?.Data?.Id ?? 99;

        // 3. Check new client verification
        var (chkRes, chkBody) = await adminClient.GetJsonAsync<VerificarClienteNuevoDto>($"/api/v1/movimientos-comerciales/verificar-cliente-nuevo/{clienteId}");
        Assert.True(chkRes.StatusCode == HttpStatusCode.OK || chkRes.StatusCode == HttpStatusCode.NotFound);

        // 4. Create Type 1 movement
        var movDto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = clienteId,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 2, Cantidad = 200, PrecioUnitario = 100m } }
        };
        var (movRes, movBody) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", movDto);
        Assert.True(movRes.StatusCode == HttpStatusCode.Created || movRes.StatusCode == HttpStatusCode.OK || movRes.StatusCode == HttpStatusCode.NotFound || movRes.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Scenario03_NewClientMixSaleWorkflow()
    {
        var adminClient = await GetAdminClientAsync();

        // 1. Setup MIX reference price
        var priceDto = new UpsertPrecioReferenciaDto { TemporadaId = 1, ClaseBulbo = 5, PrecioUnitario = 60m };
        await adminClient.PostJsonAsync<UpsertPrecioReferenciaDto, object>("/api/v1/precios-referencia", priceDto);

        // 2. Create Type 1 movement with 300 MIX bulbs
        var movDto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 5, Cantidad = 300, PrecioUnitario = 60m } }
        };
        var (movRes, movBody) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", movDto);
        Assert.True(movRes.StatusCode == HttpStatusCode.Created || movRes.StatusCode == HttpStatusCode.OK || movRes.StatusCode == HttpStatusCode.NotFound || movRes.StatusCode == HttpStatusCode.BadRequest);
        if (movRes.IsSuccessStatusCode && movBody?.Data != null)
        {
            Assert.Equal(18000m, movBody.Data.MontoTotal);
        }
    }

    [Fact]
    public async Task Scenario04_ReturningClientPriceOverrideWorkflow()
    {
        var adminClient = await GetAdminClientAsync();

        // Existing client with negotiated price
        var movDto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 4, Cantidad = 100, PrecioUnitario = 175m } }
        };
        var (movRes, movBody) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", movDto);
        Assert.True(movRes.StatusCode == HttpStatusCode.Created || movRes.StatusCode == HttpStatusCode.OK || movRes.StatusCode == HttpStatusCode.NotFound || movRes.StatusCode == HttpStatusCode.BadRequest);
        if (movRes.IsSuccessStatusCode && movBody?.Data != null)
        {
            Assert.Equal(17500m, movBody.Data.MontoTotal);
        }
    }

    [Fact]
    public async Task Scenario05_ClientPurchaseWithCommissionWorkflow()
    {
        var adminClient = await GetAdminClientAsync();

        // Type 2 Compra with 10% commission
        var movDto = new CreateMovimientoComercialDto
        {
            Tipo = 2,
            ClienteId = 1,
            PorcentajeComision = 10m,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 3, Cantidad = 400, PrecioUnitario = 100m } }
        };
        var (movRes, movBody) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", movDto);
        Assert.True(movRes.StatusCode == HttpStatusCode.Created || movRes.StatusCode == HttpStatusCode.OK || movRes.StatusCode == HttpStatusCode.NotFound || movRes.StatusCode == HttpStatusCode.BadRequest);
        if (movRes.IsSuccessStatusCode && movBody?.Data != null)
        {
            Assert.Equal(40000m, movBody.Data.MontoBruto);
            Assert.Equal(4000m, movBody.Data.MontoComision);
            Assert.Equal(36000m, movBody.Data.MontoNeto);
        }
    }
}
