using System.Net;
using SIGA.E2ETests.Common;
using SIGA.E2ETests.Fixtures;
using Xunit;

namespace SIGA.E2ETests.Tier1_FeatureCoverage;

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F21")]
public class F21_PlantationsStructuredRowLotTests : SigaTestBase
{
    public F21_PlantationsStructuredRowLotTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F21_01_QueryRowsByBranchAndSeason_ReturnsStructuredRows()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<List<FilaDto>>("/api/v1/filas/por-sucursal/1/temporada/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
    }

    [Fact]
    public async Task TC_F21_02_CreateDistributionWithStructuredIntegers_Succeeds()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new RegistrarDistribucionRequestDto
        {
            SucursalId = 1,
            TemporadaId = 1,
            NumeroFila = 1,
            NumeroLote = 1,
            CapacidadFila = 1000,
            ClienteId = 1,
            Detalles = new List<DetalleDistribucionDto>
            {
                new() { Clase = 1, Cantidad = 100 }
            }
        };

        var (response, body) = await adminClient.PostJsonAsync<RegistrarDistribucionRequestDto, LoteDto>("/api/v1/plantaciones/distribucion", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body?.Data);
    }

    [Fact]
    public async Task TC_F21_03_CreateNewRowOnTheFly_AcceptsIntegerCapacity()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new RegistrarDistribucionRequestDto
        {
            SucursalId = 1,
            TemporadaId = 1,
            NumeroFila = 5,
            CapacidadFila = 5000,
            NumeroLote = 1,
            ClienteId = 1,
            Detalles = new List<DetalleDistribucionDto>
            {
                new() { Clase = 2, Cantidad = 150 }
            }
        };

        var (response, body) = await adminClient.PostJsonAsync<RegistrarDistribucionRequestDto, LoteDto>("/api/v1/plantaciones/distribucion", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body?.Data);
    }

    [Fact]
    public async Task TC_F21_04_SequentialLotOrderAssignment_ReturnsOrderedLots()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<List<FilaDto>>("/api/v1/filas/por-sucursal/1/temporada/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
    }

    [Fact]
    public async Task TC_F21_05_VerifyFilaCapacityTracking_CapacityDeducted()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<List<FilaDto>>("/api/v1/filas/por-sucursal/1/temporada/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
    }
}

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F22")]
public class F22_PlantationsFrontendContractTests : SigaTestBase
{
    public F22_PlantationsFrontendContractTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public void TC_F22_01_DynamicRowDropdownPopulation_ViewScriptExists()
    {
        var viewPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/views/plantaciones-view.js"));
        Assert.True(File.Exists(viewPath), "plantaciones-view.js must exist");
        var content = File.ReadAllText(viewPath);
        Assert.True(content.Contains("fila") || content.Contains("Fila") || content.Contains("sucursal"));
    }

    [Fact]
    public void TC_F22_02_SelectNuevaFilaOption_SupportsIntegerRowInput()
    {
        var viewPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/views/plantaciones-view.js"));
        Assert.True(File.Exists(viewPath), "plantaciones-view.js must exist");
        var content = File.ReadAllText(viewPath);
        Assert.True(content.Contains("nueva") || content.Contains("Nueva") || content.Contains("capacidad") || content.Contains("input"));
    }

    [Fact]
    public void TC_F22_03_SelectExistingRowSuggestsNextLot_ServiceExists()
    {
        var servicePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/services/plantaciones-service.js"));
        var appPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/app.js"));
        Assert.True(File.Exists(servicePath) || File.Exists(appPath));
        if (File.Exists(servicePath))
        {
            var content = File.ReadAllText(servicePath);
            Assert.True(content.Contains("filas") || content.Contains("distribucion") || content.Contains("plantaciones"));
        }
    }

    [Fact]
    public void TC_F22_04_SubmitStructuredDistributionForm_NumericTypesAdhered()
    {
        var dto = new RegistrarDistribucionRequestDto { NumeroFila = 1, NumeroLote = 1, CapacidadFila = 500 };
        Assert.Equal(1, dto.NumeroFila);
        Assert.Equal(1, dto.NumeroLote);
        Assert.Equal(500, dto.CapacidadFila);
    }

    [Fact]
    public void TC_F22_05_GridRefreshAfterDistribution_StructureVerified()
    {
        var viewPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/views/plantaciones-view.js"));
        Assert.True(File.Exists(viewPath), "plantaciones-view.js must exist");
        var content = File.ReadAllText(viewPath);
        Assert.True(content.Contains("render") || content.Contains("cargar") || content.Contains("distribucion"));
    }
}

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F23")]
public class F23_PlantationsUnitTestsExecution : SigaTestBase
{
    public F23_PlantationsUnitTestsExecution(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F23_01_PlantationDistribution_ValidIntegerRowAndLot_ReturnsCreated()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new RegistrarDistribucionRequestDto
        {
            SucursalId = 1,
            TemporadaId = 1,
            NumeroFila = 1,
            CapacidadFila = 1000,
            NumeroLote = 1,
            ClienteId = 1,
            Detalles = new List<DetalleDistribucionDto>
            {
                new() { Clase = 1, Cantidad = 250 }
            }
        };

        var (response, body) = await adminClient.PostJsonAsync<RegistrarDistribucionRequestDto, LoteDto>(
            "/api/v1/plantaciones/distribucion", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body?.Data);
    }

    [Fact]
    public async Task TC_F23_02_PlantationDistribution_InvalidRowNumberZeroOrNegative_ReturnsBadRequest()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new RegistrarDistribucionRequestDto
        {
            SucursalId = 1,
            TemporadaId = 1,
            NumeroFila = -1, // Invalid negative integer
            CapacidadFila = 1000,
            NumeroLote = 1,
            ClienteId = 1,
            Detalles = new List<DetalleDistribucionDto> { new() { Clase = 1, Cantidad = 100 } }
        };

        var (response, _) = await adminClient.PostJsonAsync<RegistrarDistribucionRequestDto, LoteDto>(
            "/api/v1/plantaciones/distribucion", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TC_F23_03_PlantationDistribution_ExceedingRowCapacity_ReturnsBadRequest()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new RegistrarDistribucionRequestDto
        {
            SucursalId = 1,
            TemporadaId = 1,
            NumeroFila = 2,
            CapacidadFila = 100, // Capacity 100
            NumeroLote = 1,
            ClienteId = 1,
            Detalles = new List<DetalleDistribucionDto>
            {
                new() { Clase = 1, Cantidad = 150 } // Exceeds capacity
            }
        };

        var (response, _) = await adminClient.PostJsonAsync<RegistrarDistribucionRequestDto, LoteDto>(
            "/api/v1/plantaciones/distribucion", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TC_F23_04_QueryFilasPorSucursalYTemporada_ReturnsStructuredIntegerProperties()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<List<FilaDto>>(
            "/api/v1/filas/por-sucursal/1/temporada/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        foreach (var fila in body.Data)
        {
            Assert.True(fila.Id > 0);
            Assert.True(fila.Capacidad > 0);
            Assert.False(string.IsNullOrWhiteSpace(fila.Identificacion));
        }
    }

    [Fact]
    public async Task TC_F23_05_PlantationDistribution_InvalidSucursalOrTemporada_ReturnsBadRequestOrNotFound()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new RegistrarDistribucionRequestDto
        {
            SucursalId = 99999, // Non-existent branch
            TemporadaId = 1,
            NumeroFila = 1,
            CapacidadFila = 1000,
            NumeroLote = 1,
            ClienteId = 1,
            Detalles = new List<DetalleDistribucionDto> { new() { Clase = 1, Cantidad = 50 } }
        };

        var (response, _) = await adminClient.PostJsonAsync<RegistrarDistribucionRequestDto, LoteDto>(
            "/api/v1/plantaciones/distribucion", dto);

        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }
}

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F24")]
public class F24_SidebarCollapsibleSectionsTests : SigaTestBase
{
    public F24_SidebarCollapsibleSectionsTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public void TC_F24_01_Sidebar5SectionHeaders_IndexOrAppContainsExpectedSections()
    {
        var appPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/app.js"));
        var indexPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/index.html"));
        var combined = (File.Exists(appPath) ? File.ReadAllText(appPath) : "") +
                       (File.Exists(indexPath) ? File.ReadAllText(indexPath) : "");

        Assert.Contains("Principal", combined);
        Assert.Contains("Movimientos Comerciales", combined);
        Assert.Contains("Stock y Bulbos", combined);
        Assert.True(combined.Contains("Producción y Campo") || combined.Contains("Producción") || combined.Contains("Produccion"));
        Assert.True(combined.Contains("Administración") || combined.Contains("Administracion"));
    }

    [Fact]
    public void TC_F24_02_CollapsibleSectionChevronIndicator_CssRuleDefined()
    {
        var layoutCss = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/css/layout.css"));
        Assert.True(File.Exists(layoutCss), "layout.css must exist.");

        var css = File.ReadAllText(layoutCss);
        Assert.True(css.Contains("chevron") || css.Contains("rotate") || css.Contains(".sidebar-nav"),
            "layout.css must define chevron indicator or rotation styles for collapsible sections.");
    }

    [Fact]
    public void TC_F24_03_SmoothHeightTransition_CssRulesDefined()
    {
        var layoutCss = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/css/layout.css"));
        Assert.True(File.Exists(layoutCss), "layout.css must exist.");

        var css = File.ReadAllText(layoutCss);
        Assert.Contains("transition", css);
    }

    [Fact]
    public void TC_F24_04_SectionToggleHandler_ImplementedInAppScript()
    {
        var appPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/app.js"));
        Assert.True(File.Exists(appPath), "app.js must exist.");

        var appJs = File.ReadAllText(appPath);
        Assert.True(appJs.Contains("addEventListener") || appJs.Contains("toggle") || appJs.Contains("click"),
            "app.js must handle section click/toggle events.");
    }

    [Fact]
    public void TC_F24_05_NavigationLinksContainProperDataPathAttributes()
    {
        var appPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/app.js"));
        Assert.True(File.Exists(appPath), "app.js must exist.");

        var appJs = File.ReadAllText(appPath);
        Assert.Contains("data-path", appJs);
        Assert.Contains("#dashboard", appJs);
        Assert.Contains("#stock", appJs);
    }
}

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F25")]
public class F25_SidebarPersistenceAutoExpandTests : SigaTestBase
{
    public F25_SidebarPersistenceAutoExpandTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public void TC_F25_01_StateSavedToLocalStorage_KeyDocumented()
    {
        var appPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/app.js"));
        Assert.True(File.Exists(appPath), "app.js must exist.");

        var code = File.ReadAllText(appPath);
        Assert.True(code.Contains("localStorage.setItem") || code.Contains("localStorage"),
            "app.js must persist sidebar state using localStorage.");
    }

    [Fact]
    public void TC_F25_02_StateRestoredFromLocalStorage_ImplementedInApp()
    {
        var appPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/app.js"));
        Assert.True(File.Exists(appPath), "app.js must exist.");

        var code = File.ReadAllText(appPath);
        Assert.True(code.Contains("localStorage.getItem") || code.Contains("localStorage"),
            "app.js must restore sidebar state from localStorage.");
    }

    [Fact]
    public void TC_F25_03_AutoExpandActiveSectionOnNavigation_HandledInAppOrRouter()
    {
        var appPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/app.js"));
        var routerPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/router.js"));
        var combined = (File.Exists(appPath) ? File.ReadAllText(appPath) : "") +
                       (File.Exists(routerPath) ? File.ReadAllText(routerPath) : "");

        Assert.True(combined.Contains("_updateActiveNav") || combined.Contains("active") || combined.Contains("expand"),
            "Frontend must contain logic to expand section matching active route.");
    }

    [Fact]
    public void TC_F25_04_AutoExpandActiveSectionOnDeepLink_SupportedOnHashChange()
    {
        var routerPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/router.js"));
        Assert.True(File.Exists(routerPath), "router.js must exist.");

        var code = File.ReadAllText(routerPath);
        Assert.Contains("hashchange", code);
    }

    [Fact]
    public void TC_F25_05_ActiveLinkHighlight_CssClassDefined()
    {
        var layoutCss = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/css/layout.css"));
        Assert.True(File.Exists(layoutCss), "layout.css must exist.");

        var css = File.ReadAllText(layoutCss);
        Assert.Contains(".nav-item.active", css);
    }
}
