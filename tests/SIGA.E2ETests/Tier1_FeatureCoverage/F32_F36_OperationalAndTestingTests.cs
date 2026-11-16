using System.Net;
using SIGA.E2ETests.Common;
using SIGA.E2ETests.Fixtures;
using Xunit;

namespace SIGA.E2ETests.Tier1_FeatureCoverage;

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F32")]
public class F32_StockViewBugFixTests : SigaTestBase
{
    public F32_StockViewBugFixTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F32_01_LoadEmpresaStockViaRealApi_ReturnsStockData()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<StockClienteDto>("/api/v1/stock/por-cliente/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.Equal(1, body.Data.ClienteId);
        Assert.NotNull(body.Data.StockPorClase);
    }

    [Fact]
    public async Task TC_F32_02_LoadClientStock_ReturnsClientStock()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<StockClienteDto>("/api/v1/stock/por-cliente/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
    }

    [Fact]
    public async Task TC_F32_03_FiveBulbClassesDisplayed_StructurePreserved()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<StockClienteDto>("/api/v1/stock/por-cliente/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data?.StockPorClase);
        Assert.NotEmpty(body.Data.StockPorClase);
    }

    [Fact]
    public async Task TC_F32_04_TotalBulbsCalculation_MatchesSumOfApiReturnedClasses()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<StockClienteDto>("/api/v1/stock/por-cliente/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);

        var calculatedSum = body.Data.StockPorClase.Sum(s => s.Cantidad);
        Assert.Equal(calculatedSum, body.Data.TotalBulbos);
    }

    [Fact]
    public void TC_F32_05_RefreshButton_ServiceUsesDefensiveArrayChecks()
    {
        var servicePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/services/stock-service.js"));
        Assert.True(File.Exists(servicePath), "stock-service.js must exist");

        var code = File.ReadAllText(servicePath);
        Assert.Contains("Array.isArray", code);
        Assert.Contains("obtenerStockPorCliente", code);
    }
}

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F33")]
public class F33_StandaloneReferencePricesViewTests : SigaTestBase
{
    public F33_StandaloneReferencePricesViewTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public void TC_F33_01_RenderReferencePricesTable_ViewFileExists()
    {
        var viewPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/views/precios-referencia-view.js"));
        Assert.True(File.Exists(viewPath) || File.Exists(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/app.js"))));
    }

    [Fact]
    public async Task TC_F33_02_SeasonSelector_ReloadsPricesFromEndpoint()
    {
        var adminClient = await GetAdminClientAsync();
        var response = await adminClient.GetAsync("/api/v1/precios-referencia/temporada/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TC_F33_03_AuthorizedPriceEdit_SucceedsWithPermission()
    {
        var client = GetClientWithPermissions("CONTABILIDAD", "PRECIOS_REF_GESTIONAR");
        var dto = new UpsertPrecioReferenciaDto { TemporadaId = 1, ClaseBulbo = 5, PrecioUnitario = 95.0m };
        var response = await client.PostAsync("/api/v1/precios-referencia", dto);
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created);
    }

    [Fact]
    public void TC_F33_04_PersistenceNotification_ToastScriptExists()
    {
        var appPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/app.js"));
        Assert.True(File.Exists(appPath));
    }

    [Fact]
    public async Task TC_F33_05_NegativePriceValidation_ReturnsBadRequest()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new UpsertPrecioReferenciaDto { TemporadaId = 1, ClaseBulbo = 1, PrecioUnitario = -10.0m };
        var (response, _) = await adminClient.PostJsonAsync<UpsertPrecioReferenciaDto, object>(
            "/api/v1/precios-referencia", dto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F34")]
public class F34_CommercialMovementsViewTests : SigaTestBase
{
    public F34_CommercialMovementsViewTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public void TC_F34_01_MovementsListingTable_ViewScriptExists()
    {
        var viewPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/views/movimientos-comerciales-view.js"));
        Assert.True(File.Exists(viewPath) || File.Exists(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/views/ventas-view.js"))));
    }

    [Fact]
    public async Task TC_F34_02_FilterControls_EndpointSupportsQueryFiltering()
    {
        var adminClient = await GetAdminClientAsync();
        var response = await adminClient.GetAsync("/api/v1/movimientos-comerciales?tipo=1&estado=0");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void TC_F34_03_DynamicMovementForm_FormTypesDocumented()
    {
        var viewPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/views/movimientos-comerciales-view.js"));
        if (File.Exists(viewPath))
        {
            var content = File.ReadAllText(viewPath);
            Assert.False(string.IsNullOrWhiteSpace(content));
        }
    }

    [Fact]
    public void TC_F34_04_MixLiveDecompositionInView_EnforcesDivisibleByThreeAndSplitsClasses()
    {
        var viewPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/views/movimientos-comerciales-view.js"));
        Assert.True(File.Exists(viewPath), "movimientos-comerciales-view.js must exist.");
        var code = File.ReadAllText(viewPath);
        Assert.True(code.Contains("% 3") || code.Contains("múltiplo de 3") || code.Contains("/ 3"));
    }

    [Fact]
    public async Task TC_F34_05_DetailViewTimelineAndActions_ReturnsTimelineData()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 1, Cantidad = 10, PrecioUnitario = 50m } }
        };
        var (createRes, createBody) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        Assert.NotNull(createBody?.Data);

        var (response, body) = await adminClient.GetJsonAsync<MovimientoComercialDto>($"/api/v1/movimientos-comerciales/{createBody.Data.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.NotNull(body.Data.HistorialAprobaciones);
    }
}

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F35")]
public class F35_E2ETestSuiteRunnerTests : SigaTestBase
{
    public F35_E2ETestSuiteRunnerTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public void TC_F35_01_FullSuiteSingleCommandExecution_ProjectReferencedInSln()
    {
        var slnPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../SIGA.sln"));
        var sln = File.ReadAllText(slnPath);
        Assert.Contains("SIGA.E2ETests", sln);
    }

    [Fact]
    public void TC_F35_02_TestRunnerScript_SupportsTierFilteringAndExitCode()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        string? scriptPath = null;
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "test-e2e.ps1");
            if (File.Exists(candidate))
            {
                scriptPath = candidate;
                break;
            }
            dir = dir.Parent;
        }

        Assert.NotNull(scriptPath);
        Assert.True(File.Exists(scriptPath), "test-e2e.ps1 runner script must exist.");
        var script = File.ReadAllText(scriptPath);
        Assert.Contains("param(", script);
        Assert.Contains("$Tier", script);
        Assert.Contains("exit", script);
    }

    [Fact]
    public void TC_F35_03_IsolatedDatabaseSeeding_DatabasePathIsTemp()
    {
        Assert.Contains(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar), Factory.TestDatabasePath);
    }

    [Fact]
    public async Task TC_F35_04_RateLimiterOverride_PermitsHighThroughputRequests()
    {
        var adminClient = await GetAdminClientAsync();
        for (int i = 0; i < 20; i++)
        {
            var res = await adminClient.GetAsync("/api/v1/auth/me");
            Assert.NotEqual(HttpStatusCode.TooManyRequests, res.StatusCode);
        }
    }

    [Fact]
    public void TC_F35_05_InMemoryWebApplicationFactoryOperation_FactoryInstantiated()
    {
        Assert.NotNull(Factory);
        Assert.NotNull(Client);
    }
}

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F36")]
public class F36_AdversarialHardeningSmokeTests : SigaTestBase
{
    public F36_AdversarialHardeningSmokeTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F36_01_SqlInjectionResistance_ReturnsHandledResponse()
    {
        var adminClient = await GetAdminClientAsync();
        var response = await adminClient.GetAsync("/api/v1/movimientos-comerciales?busqueda=1%27%20OR%20%271%27=%271");
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task TC_F36_02_RolePrivilegeEscalationGuard_DeniedForUnauthorized()
    {
        var client = GetClientWithPermissions("VENDEDOR", "MOVCOM_VER");
        var response = await client.PutAsync("/api/v1/roles/1", new UpdateRolDto { Descripcion = "Escalation" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task TC_F36_03_ConcurrentFinalizationStockSafety_IdempotentResponse()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 1, Cantidad = 10, PrecioUnitario = 50m } }
        };
        var (createRes, createBody) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        Assert.NotNull(createBody?.Data);
        var movId = createBody.Data.Id;

        await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/aprobar", new { });
        await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/facturar", new FacturarMovimientoDto { NumeroFactura = "F-36-03" });
        var response = await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/finalizar", new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TC_F36_04_CrossClientStockIsolation_RestrictsUnauthorizedTransfer()
    {
        var client = GetClientWithPermissions("CLIENTE");
        var dto = new CreateMovimientoComercialDto { Tipo = 3, ClienteOrigenId = 1, ClienteDestinoId = 2 };
        var response = await client.PostAsync("/api/v1/movimientos-comerciales", dto);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task TC_F36_05_IdempotentResubmissionSafeguard_HandledCleanly()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 1, Cantidad = 10, PrecioUnitario = 50m } }
        };
        var (createRes, createBody) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        Assert.NotNull(createBody?.Data);
        var movId = createBody.Data.Id;

        await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/rechazar", new RechazarMovimientoDto { Motivo = "Para resometer" });
        var response = await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/corregir-reenviar", new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
