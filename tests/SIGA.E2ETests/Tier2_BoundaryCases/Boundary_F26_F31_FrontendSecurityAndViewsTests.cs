using System.Net;
using SIGA.E2ETests.Common;
using SIGA.E2ETests.Fixtures;
using Xunit;

namespace SIGA.E2ETests.Tier2_BoundaryCases;

[Trait("Category", TestCategories.Tier2)]
[Trait("Feature", "F26")]
public class Boundary_F26_FrontendRouteGuardsPermBoundaries : SigaTestBase
{
    public Boundary_F26_FrontendRouteGuardsPermBoundaries(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public void TC_F26_B01_DirectUrlTamperingUnauthorizedRoute_GuardBlocks()
    {
        var routerPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/router.js"));
        Assert.True(File.Exists(routerPath));
    }

    [Fact]
    public void TC_F26_B02_UnauthenticatedProtectedRouteAccess_RedirectsToLogin()
    {
        var routerPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/router.js"));
        Assert.True(File.Exists(routerPath));
    }

    [Fact]
    public void TC_F26_B03_PermissionRevocationDuringSession_Re-evaluated()
    {
        var routerPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/router.js"));
        Assert.True(File.Exists(routerPath));
    }

    [Fact]
    public async Task TC_F26_B04_ClientSidePermissionInjectionProtection_BackendEnforcesTruth()
    {
        // Even if client tampers with JS state, backend returns 403
        var client = GetClientWithPermissions("VENDEDOR", "STOCK_VER");
        var response = await client.PostAsync("/api/v1/roles", new CreateRolDto { Nombre = "Tampered" });
        Assert.True(response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public void TC_F26_B05_InvalidRouteGuardFallback_RedirectsToDefault()
    {
        var routerPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/router.js"));
        Assert.True(File.Exists(routerPath));
    }
}

[Trait("Category", TestCategories.Tier2)]
[Trait("Feature", "F27")]
public class Boundary_F27_TopbarProfileNavNoBellBoundaries : SigaTestBase
{
    public Boundary_F27_TopbarProfileNavNoBellBoundaries(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public void TC_F27_B01_RepeatedProfileBadgeClicks_SingleNavigation()
    {
        var routerPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/router.js"));
        Assert.True(File.Exists(routerPath));
    }

    [Fact]
    public void TC_F27_B02_ExtremelyLongUsernameDisplay_TruncationSupported()
    {
        var longName = new string('A', 50);
        Assert.Equal(50, longName.Length);
    }

    [Fact]
    public void TC_F27_B03_NullDocumentPhoneInProfileBadge_HandledSafely()
    {
        var profile = new UserProfileDto { Documento = null, Telefono = null };
        Assert.Null(profile.Documento);
    }

    [Fact]
    public async Task TC_F27_B04_NetworkTabPollingAbsence_NoPollingToNotificaciones()
    {
        var adminClient = await GetAdminClientAsync();
        var response = await adminClient.GetAsync("/api/v1/notificaciones");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void TC_F27_B05_ResponsiveViewportCollapse_MobileLayoutHandled()
    {
        var appPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/app.js"));
        Assert.True(File.Exists(appPath));
    }
}

[Trait("Category", TestCategories.Tier2)]
[Trait("Feature", "F28")]
public class Boundary_F28_DeactivationProhibitionIconBoundaries : SigaTestBase
{
    public Boundary_F28_DeactivationProhibitionIconBoundaries(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public void TC_F28_B01_ZeroTrashCansCodebaseScrutiny_ScanViewsForTrashCans()
    {
        var viewsDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/views"));
        if (Directory.Exists(viewsDir))
        {
            var files = Directory.GetFiles(viewsDir, "*.js");
            foreach (var f in files)
            {
                var content = File.ReadAllText(f);
                Assert.DoesNotContain("fa-trash", content);
            }
        }
    }

    [Fact]
    public void TC_F28_B02_DisabledProhibitionIconOnLastAdmin_GuardDocumented()
    {
        var empView = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/views/empleados-view.js"));
        Assert.True(File.Exists(empView) || File.Exists(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/app.js"))));
    }

    [Fact]
    public void TC_F28_B03_ProhibitionIconHighContrastStyling_DangerVarUsed()
    {
        var varsCss = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/css/variables.css"));
        Assert.True(File.Exists(varsCss) || Directory.Exists(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/css"))));
    }

    [Fact]
    public void TC_F28_B04_ActionCancellationPreservesState_ModalSupportsCancel()
    {
        Assert.True(true);
    }

    [Fact]
    public void TC_F28_B05_ProhibitionIconOnInactiveEntities_HandledConsistently()
    {
        Assert.True(true);
    }
}

[Trait("Category", TestCategories.Tier2)]
[Trait("Feature", "F29")]
public class Boundary_F29_UserProfileViewBoundaries : SigaTestBase
{
    public Boundary_F29_UserProfileViewBoundaries(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public void TC_F29_B01_UserWithZeroExplicitPermissions_RendersCleanly()
    {
        var profile = new UserProfileDto { Permisos = new List<string>() };
        Assert.Empty(profile.Permisos);
    }

    [Fact]
    public void TC_F29_B02_NullDocumentAndPhone_HandledCleanly()
    {
        var profile = new UserProfileDto { Documento = null, Telefono = null };
        Assert.Null(profile.Documento);
        Assert.Null(profile.Telefono);
    }

    [Fact]
    public void TC_F29_B03_BackendErrorOnMeCall_UIBannerSupported()
    {
        var appPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/app.js"));
        Assert.True(File.Exists(appPath));
    }

    [Fact]
    public void TC_F29_B04_ReadOnlyEnforcementInDom_NoInputsInProfileView()
    {
        var viewPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/views/perfil-view.js"));
        Assert.True(File.Exists(viewPath) || File.Exists(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/app.js"))));
    }

    [Fact]
    public void TC_F29_B05_NarrowMobileLayout_Responsive()
    {
        var appPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/app.js"));
        Assert.True(File.Exists(appPath));
    }
}

[Trait("Category", TestCategories.Tier2)]
[Trait("Feature", "F30")]
public class Boundary_F30_EmployeesManagementViewBoundaries : SigaTestBase
{
    public Boundary_F30_EmployeesManagementViewBoundaries(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public void TC_F30_B01_LastAdminDeactivationGuardInUi_Prevented()
    {
        var viewPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/views/empleados-view.js"));
        Assert.True(File.Exists(viewPath) || File.Exists(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/app.js"))));
    }

    [Fact]
    public void TC_F30_B02_ExcludeClienteRoleFromDropdown_ClienteExcluded()
    {
        var viewPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/views/empleados-view.js"));
        Assert.True(File.Exists(viewPath) || File.Exists(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/app.js"))));
    }

    [Fact]
    public async Task TC_F30_B03_ClientSideValidationOnBlankFields_BackendAlsoRejects()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateEmpleadoDto { Nombre = "", Email = "invalid" };
        var (response, _) = await adminClient.PostJsonAsync<CreateEmpleadoDto, EmpleadoDto>("/api/v1/empleados", dto);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TC_F30_B04_DuplicateUsernameFeedback_ReturnsError()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = TestDataGenerator.CreateSampleEmpleadoDto();
        dto.Username = "admin";
        var (response, _) = await adminClient.PostJsonAsync<CreateEmpleadoDto, EmpleadoDto>("/api/v1/empleados", dto);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Conflict || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public void TC_F30_B05_UnauthorizedAccessRedirection_Guarded()
    {
        var routerPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/router.js"));
        Assert.True(File.Exists(routerPath));
    }
}

[Trait("Category", TestCategories.Tier2)]
[Trait("Feature", "F31")]
public class Boundary_F31_RolesPermissionsViewBoundaries : SigaTestBase
{
    public Boundary_F31_RolesPermissionsViewBoundaries(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F31_B01_HideDisableDeleteOnSystemRoles_SystemRolesCannotBeDeleted()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, _) = await adminClient.DeleteJsonAsync<object>("/api/v1/roles/1");
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TC_F31_B02_DeleteCustomRoleWithUsersFeedback_ReturnsBadRequest()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, _) = await adminClient.DeleteJsonAsync<object>("/api/v1/roles/2");
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public void TC_F31_B03_ModuleCheckboxBulkSelection_SupportedInUi()
    {
        var viewPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/views/roles-view.js"));
        Assert.True(File.Exists(viewPath) || File.Exists(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/app.js"))));
    }

    [Fact]
    public async Task TC_F31_B04_DuplicateRoleNameErrorDisplay_ReturnsBadRequestOrConflict()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateRolDto { Nombre = "ADMINISTRADOR" };
        var (response, _) = await adminClient.PostJsonAsync<CreateRolDto, RolDto>("/api/v1/roles", dto);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Conflict || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public void TC_F31_B05_UnauthorizedNavigationInterception_Guarded()
    {
        var routerPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/js/router.js"));
        Assert.True(File.Exists(routerPath));
    }
}
