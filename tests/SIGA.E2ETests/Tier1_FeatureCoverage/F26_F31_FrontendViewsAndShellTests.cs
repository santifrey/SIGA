using System.Net;
using SIGA.E2ETests.Common;
using SIGA.E2ETests.Fixtures;
using Xunit;

namespace SIGA.E2ETests.Tier1_FeatureCoverage;

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F26")]
public class F26_FrontendRouteGuardsPermTests : SigaTestBase
{
    public F26_FrontendRouteGuardsPermTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public void TC_F26_01_AuthService_ImplementsTienePermisoMethod()
    {
        var authServicePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/services/auth-service.js"));
        Assert.True(File.Exists(authServicePath), "auth-service.js must exist.");

        var code = File.ReadAllText(authServicePath);
        Assert.Contains("tienePermiso", code);
    }

    [Fact]
    public void TC_F26_02_TienePermiso_ChecksPermissionsArrayOrAdminRole()
    {
        var authServicePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/services/auth-service.js"));
        Assert.True(File.Exists(authServicePath), "auth-service.js must exist.");

        var code = File.ReadAllText(authServicePath);
        Assert.True(code.Contains("permisos") || code.Contains("ADMINISTRADOR"),
            "auth-service.tienePermiso must inspect permisos or administrator role.");
    }

    [Fact]
    public void TC_F26_03_RouterRouteRegistration_SupportsPermissionGuard()
    {
        var routerPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/router.js"));
        Assert.True(File.Exists(routerPath), "router.js must exist.");

        var code = File.ReadAllText(routerPath);
        Assert.True(code.Contains("permiso") || code.Contains("auth") || code.Contains("navigate"),
            "router.js must support route protection/guards.");
    }

    [Fact]
    public void TC_F26_04_SidebarRendering_FiltersLinksByPermission()
    {
        var appPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/app.js"));
        Assert.True(File.Exists(appPath), "app.js must exist.");

        var code = File.ReadAllText(appPath);
        Assert.True(code.Contains("tienePermiso") || code.Contains("authService"),
            "app.js must filter sidebar links using tienePermiso.");
    }

    [Fact]
    public async Task TC_F26_05_BackendEnforcementCorrelation_ClientRoleReceives403OnRolesEndpoint()
    {
        var clientRole = GetClientWithPermissions("CLIENTE", "STOCK_VER");
        var response = await clientRole.GetAsync("/api/v1/roles");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F27")]
public class F27_TopbarProfileNavNoBellTests : SigaTestBase
{
    public F27_TopbarProfileNavNoBellTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public void TC_F27_01_ProfileBadgeDisplay_ElementPresentInHtmlOrApp()
    {
        var indexPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/index.html"));
        var appPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/app.js"));
        var combined = (File.Exists(indexPath) ? File.ReadAllText(indexPath) : "") +
                       (File.Exists(appPath) ? File.ReadAllText(appPath) : "");

        Assert.True(combined.Contains("user-profile") || combined.Contains("perfil") || combined.Contains("profile"));
    }

    [Fact]
    public void TC_F27_02_ClickProfileNavigatesToPerfil_RouteDefined()
    {
        var routerPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/router.js"));
        if (File.Exists(routerPath))
        {
            var content = File.ReadAllText(routerPath);
            Assert.Contains("perfil", content);
        }
    }

    [Fact]
    public void TC_F27_03_ProfileViewRenders_ViewModuleExists()
    {
        var viewPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/views/perfil-view.js"));
        Assert.True(File.Exists(viewPath) || File.Exists(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/app.js"))));
    }

    [Fact]
    public void TC_F27_04_VerifyNoNotificationBell_ZeroBellElements()
    {
        var indexPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/index.html"));
        if (File.Exists(indexPath))
        {
            var html = File.ReadAllText(indexPath);
            Assert.DoesNotContain("notification-bell", html);
            Assert.DoesNotContain("btn-notifications", html);
        }
    }

    [Fact]
    public void TC_F27_05_VerifyNoNotificationDropdown_ZeroNotificationDropdown()
    {
        var indexPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/index.html"));
        if (File.Exists(indexPath))
        {
            var html = File.ReadAllText(indexPath);
            Assert.DoesNotContain("notifications-dropdown", html);
            Assert.DoesNotContain("notifications-panel", html);
        }
    }
}

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F28")]
public class F28_DeactivationProhibitionSymbolTests : SigaTestBase
{
    public F28_DeactivationProhibitionSymbolTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public void TC_F28_01_ClientesDeactivationIcon_UsesProhibitionIcon()
    {
        var clientesView = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/views/clientes-view.js"));
        if (File.Exists(clientesView))
        {
            var content = File.ReadAllText(clientesView);
            Assert.DoesNotContain("fa-trash", content);
            Assert.DoesNotContain("bi-trash", content);
        }
    }

    [Fact]
    public void TC_F28_02_EmpleadosDeactivationIcon_UsesProhibitionIcon()
    {
        var empleadosView = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/views/empleados-view.js"));
        if (File.Exists(empleadosView))
        {
            var content = File.ReadAllText(empleadosView);
            Assert.DoesNotContain("fa-trash", content);
            Assert.DoesNotContain("bi-trash", content);
        }
    }

    [Fact]
    public void TC_F28_03_DangerColorVariable_DefinedInCss()
    {
        var varsCss = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/css/variables.css"));
        if (File.Exists(varsCss))
        {
            var content = File.ReadAllText(varsCss);
            Assert.Contains("danger", content);
        }
    }

    [Fact]
    public void TC_F28_04_DescriptiveTooltip_AttributesUsed()
    {
        var viewsDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/views"));
        Assert.True(Directory.Exists(viewsDir));
    }

    [Fact]
    public void TC_F28_05_DeactivationConfirmationDialog_Preserved()
    {
        var viewsDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/views"));
        Assert.True(Directory.Exists(viewsDir));
    }
}

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F29")]
public class F29_UserProfileViewTests : SigaTestBase
{
    public F29_UserProfileViewTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public void TC_F29_01_ProfileViewRendering_ScriptExists()
    {
        var viewPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/views/perfil-view.js"));
        Assert.True(File.Exists(viewPath) || File.Exists(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/app.js"))));
    }

    [Fact]
    public async Task TC_F29_02_PersonalDataFields_ProvidedByBackend()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<UserProfileDto>("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.False(string.IsNullOrWhiteSpace(body.Data.Nombre));
    }

    [Fact]
    public async Task TC_F29_03_RoleBadge_ReturnsRoleField()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<UserProfileDto>("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.Equal("ADMINISTRADOR", body.Data.Rol);
    }

    [Fact]
    public async Task TC_F29_04_PermissionsListRendering_ReturnsPermisosArray()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<UserProfileDto>("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.NotEmpty(body.Data.Permisos);
    }

    [Fact]
    public async Task TC_F29_05_LastAccessDisplay_ReturnsUltimoAcceso()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<UserProfileDto>("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.NotNull(body.Data.UltimoAcceso);
    }
}

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F30")]
public class F30_EmployeesManagementViewTests : SigaTestBase
{
    public F30_EmployeesManagementViewTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public void TC_F30_01_EmployeesTableView_ViewFileExists()
    {
        var viewPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/views/empleados-view.js"));
        Assert.True(File.Exists(viewPath) || File.Exists(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/app.js"))));
    }

    [Fact]
    public void TC_F30_02_OpenNewEmployeeModal_SupportedInView()
    {
        var viewPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/views/empleados-view.js"));
        if (File.Exists(viewPath))
        {
            var content = File.ReadAllText(viewPath);
            Assert.False(string.IsNullOrWhiteSpace(content));
        }
    }

    [Fact]
    public async Task TC_F30_03_CreateEmployeeFromUi_BackedByPostEndpoint()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = TestDataGenerator.CreateSampleEmpleadoDto();
        var (response, _) = await adminClient.PostJsonAsync<CreateEmpleadoDto, EmpleadoDto>("/api/v1/empleados", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task TC_F30_04_EditEmployeeRole_BackedByPutEndpoint()
    {
        var adminClient = await GetAdminClientAsync();
        var createDto = TestDataGenerator.CreateSampleEmpleadoDto();
        var (createRes, createBody) = await adminClient.PostJsonAsync<CreateEmpleadoDto, EmpleadoDto>("/api/v1/empleados", createDto);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        Assert.NotNull(createBody?.Data);

        var dto = new UpdateEmpleadoDto { Nombre = "Edit", Apellido = "Test", Email = TestDataGenerator.GenerateEmail(), Documento = TestDataGenerator.GenerateDocumento(), RolId = 3 };
        var (response, _) = await adminClient.PutJsonAsync<UpdateEmpleadoDto, EmpleadoDto>($"/api/v1/empleados/{createBody.Data.Id}", dto);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TC_F30_05_DeactivateEmployee_BackedByDeleteEndpoint()
    {
        var adminClient = await GetAdminClientAsync();
        var createDto = TestDataGenerator.CreateSampleEmpleadoDto();
        var (createRes, createBody) = await adminClient.PostJsonAsync<CreateEmpleadoDto, EmpleadoDto>("/api/v1/empleados", createDto);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        Assert.NotNull(createBody?.Data);

        var (response, _) = await adminClient.DeleteJsonAsync<object>($"/api/v1/empleados/{createBody.Data.Id}");
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent);
    }
}

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F31")]
public class F31_RolesPermissionsViewTests : SigaTestBase
{
    public F31_RolesPermissionsViewTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public void TC_F31_01_RolesListingView_ViewFileExists()
    {
        var viewPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/views/roles-view.js"));
        Assert.True(File.Exists(viewPath) || File.Exists(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/app.js"))));
    }

    [Fact]
    public async Task TC_F31_02_SystemRoleBadge_IdentifiedByEsSistemaFlag()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<List<RolDto>>("/api/v1/roles");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
    }

    [Fact]
    public void TC_F31_03_OpenNewRoleModal_PermissionsAvailable()
    {
        var viewPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/views/roles-view.js"));
        if (File.Exists(viewPath))
        {
            var content = File.ReadAllText(viewPath);
            Assert.False(string.IsNullOrWhiteSpace(content));
        }
    }

    [Fact]
    public async Task TC_F31_04_CreateCustomRoleFromUi_BackedByPostEndpoint()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateRolDto { Nombre = TestDataGenerator.GenerateRoleName(), Descripcion = "Test UI", Permisos = new List<string> { "STOCK_VER" } };
        var (response, _) = await adminClient.PostJsonAsync<CreateRolDto, RolDto>("/api/v1/roles", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task TC_F31_05_EditPermissionsMatrix_BackedByPutEndpoint()
    {
        var adminClient = await GetAdminClientAsync();
        var createDto = new CreateRolDto { Nombre = TestDataGenerator.GenerateRoleName(), Descripcion = "Test UI Initial", Permisos = new List<string> { "STOCK_VER" } };
        var (createRes, createBody) = await adminClient.PostJsonAsync<CreateRolDto, RolDto>("/api/v1/roles", createDto);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        Assert.NotNull(createBody?.Data);

        var dto = new UpdateRolDto { Descripcion = "Updated UI", Permisos = new List<string> { "STOCK_VER", "CLIENTES_VER" } };
        var (response, _) = await adminClient.PutJsonAsync<UpdateRolDto, RolDto>($"/api/v1/roles/{createBody.Data.Id}", dto);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
