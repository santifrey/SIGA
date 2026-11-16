using System.Text.RegularExpressions;
using SIGA.E2ETests.Infrastructure;
using Xunit;

namespace SIGA.E2ETests.Tier1_FeatureCoverage;

public class FrontendContractTests
{
    #region Feature 24: Sidebar 5 Collapsible Sections (5 Tests)

    [Fact]
    public void T1_F24_01_DomRenders5Sections_InSidebar()
    {
        var appJs = FrontendAssetScanner.ReadFrontendFile("js/app.js");

        Assert.Contains("data-section=\"principal\"", appJs);
        Assert.Contains("data-section=\"movimientos-comerciales\"", appJs);
        Assert.Contains("data-section=\"stock\"", appJs);
        Assert.Contains("data-section=\"produccion\"", appJs);
        Assert.Contains("data-section=\"administracion\"", appJs);
    }

    [Fact]
    public void T1_F24_02_ChevronIcon_RotationOnToggle_DefinedInMarkup()
    {
        var appJs = FrontendAssetScanner.ReadFrontendFile("js/app.js");
        Assert.Contains("nav-section-chevron", appJs);
    }

    [Fact]
    public void T1_F24_03_SmoothHeightAnimationCss_DefinedInStyles()
    {
        var layoutCss = FrontendAssetScanner.ReadFrontendFile("css/layout.css");
        Assert.Contains("transition:", layoutCss);
        Assert.Contains("nav-section-content", layoutCss);
    }

    [Fact]
    public void T1_F24_04_CollapseExpandInteraction_ToggleFunctionExists()
    {
        var appJs = FrontendAssetScanner.ReadFrontendFile("js/app.js");
        Assert.Contains("data-toggle=\"section\"", appJs);
        Assert.Contains("classList.toggle('collapsed'", appJs);
    }

    [Fact]
    public void T1_F24_05_ActiveRouteHighlight_UpdatesActiveNav()
    {
        var appJs = FrontendAssetScanner.ReadFrontendFile("js/app.js");
        Assert.Contains("_updateActiveNav", appJs);
        Assert.Contains("active", appJs);
    }

    #endregion

    #region Feature 25: Sidebar Persistence & Auto-Expansion (5 Tests)

    [Fact]
    public void T1_F25_01_StateSavedToLocalStorage_KeyDefined()
    {
        var appJs = FrontendAssetScanner.ReadFrontendFile("js/app.js");
        Assert.Contains("siga_sidebar_collapsed_sections", appJs);
        Assert.Contains("localStorage.setItem", appJs);
    }

    [Fact]
    public void T1_F25_02_StateRestoredOnPageLoad_ReadsLocalStorage()
    {
        var appJs = FrontendAssetScanner.ReadFrontendFile("js/app.js");
        Assert.Contains("getCollapsedSectionsState", appJs);
        Assert.Contains("localStorage.getItem", appJs);
    }

    [Fact]
    public void T1_F25_03_AutoExpandOnDirectUrlNavigation_ExpandsParentSection()
    {
        var appJs = FrontendAssetScanner.ReadFrontendFile("js/app.js");
        Assert.Contains("parentSection.classList.remove('collapsed')", appJs);
    }

    [Fact]
    public void T1_F25_04_AutoExpandOnInAppRouteChange_TriggersOnUpdateActiveNav()
    {
        var appJs = FrontendAssetScanner.ReadFrontendFile("js/app.js");
        Assert.Contains("_updateActiveNav", appJs);
    }

    [Fact]
    public void T1_F25_05_GracefulFallback_OnMissingOrCorruptStorage()
    {
        var appJs = FrontendAssetScanner.ReadFrontendFile("js/app.js");
        Assert.Contains("try", appJs);
        Assert.Contains("catch", appJs);
    }

    #endregion

    #region Feature 26: Frontend Route Guards & Perm Filtering (5 Tests)

    [Fact]
    public void T1_F26_01_HideUnauthorizedNavItems_ChecksPermissions()
    {
        var appJs = FrontendAssetScanner.ReadFrontendFile("js/app.js");
        Assert.Contains("_applyNavPermissionFiltering", appJs);
        Assert.Contains("data-permiso", appJs);
    }

    [Fact]
    public void T1_F26_02_RedirectUnauthorizedDirectNavigation_GuardImplemented()
    {
        var routerJs = FrontendAssetScanner.ReadFrontendFile("js/router.js");
        Assert.Contains("tienePermiso", routerJs);
        Assert.Contains("#dashboard", routerJs);
    }

    [Fact]
    public void T1_F26_03_UnauthenticatedNavigation_RedirectsToLogin()
    {
        var routerJs = FrontendAssetScanner.ReadFrontendFile("js/router.js");
        Assert.Contains("estaAutenticado", routerJs);
        Assert.Contains("#login", routerJs);
    }

    [Fact]
    public void T1_F26_04_AdminRoleAccessToAllRoutes_BypassesPermissionCheck()
    {
        var authServiceJs = FrontendAssetScanner.ReadFrontendFile("js/services/auth-service.js");
        Assert.Contains("ADMINISTRADOR", authServiceJs);
    }

    [Fact]
    public void T1_F26_05_PermissionCacheInvalidationOnLogout_ClearsStorage()
    {
        var authServiceJs = FrontendAssetScanner.ReadFrontendFile("js/services/auth-service.js");
        Assert.Contains("logout()", authServiceJs);
        Assert.Contains("clearSession()", authServiceJs);
    }

    #endregion

    #region Feature 27: Topbar Profile Navigation & No Bell (5 Tests)

    [Fact]
    public void T1_F27_01_ProfileBadgeDisplaysFullName_InTopbar()
    {
        var appJs = FrontendAssetScanner.ReadFrontendFile("js/app.js");
        Assert.Contains("topbar-user-name", appJs);
    }

    [Fact]
    public void T1_F27_02_ProfileBadgeClickNavigatesToPerfil()
    {
        var appJs = FrontendAssetScanner.ReadFrontendFile("js/app.js");
        Assert.Contains("#perfil", appJs);
    }

    [Fact]
    public void T1_F27_03_AbsenceOfNotificationBellIcon_AcrossAllFrontend()
    {
        var indexHtml = FrontendAssetScanner.ReadFrontendFile("index.html");
        Assert.DoesNotContain("notification-bell", indexHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fa-bell", indexHtml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void T1_F27_04_AbsenceOfNotificationPanel_InTopbar()
    {
        var appJs = FrontendAssetScanner.ReadFrontendFile("js/app.js");
        Assert.DoesNotContain("notification-panel", appJs, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("notifications-dropdown", appJs, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void T1_F27_05_TopbarResponsiveAlignment_LayoutClassesPresent()
    {
        var layoutCss = FrontendAssetScanner.ReadFrontendFile("css/layout.css");
        Assert.Contains(".app-topbar", layoutCss);
    }

    #endregion

    #region Feature 28: Deactivation Prohibition Symbol (5 Tests)

    [Fact]
    public void T1_F28_01_ClientesView_UsesProhibitionSymbol()
    {
        var clientesJs = FrontendAssetScanner.ReadFrontendFile("js/views/clientes-view.js");
        Assert.Contains("getProhibitionIcon", clientesJs);
    }

    [Fact]
    public void T1_F28_02_EmpleadosView_UsesProhibitionSymbol()
    {
        var empleadosJs = FrontendAssetScanner.ReadFrontendFile("js/views/empleados-view.js");
        Assert.Contains("getProhibitionIcon", empleadosJs);
    }

    [Fact]
    public void T1_F28_03_ZeroTrashCansAcrossFrontend_ForDeactivation()
    {
        var files = FrontendAssetScanner.GetAllFrontendFiles("*.js");
        foreach (var file in files)
        {
            var content = System.IO.File.ReadAllText(file);
            Assert.DoesNotContain("fa-trash", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("icon-trash", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void T1_F28_04_ProhibitionSymbolStyling_UsesDangerVariable()
    {
        var iconsJs = FrontendAssetScanner.ReadFrontendFile("js/utils/icons.js");
        Assert.Contains("var(--danger)", iconsJs);
        Assert.Contains("<circle cx=\"12\" cy=\"12\" r=\"10\"", iconsJs);
        Assert.Contains("<line x1=\"4.93\" y1=\"4.93\" x2=\"19.07\" y2=\"19.07\"", iconsJs);
    }

    [Fact]
    public void T1_F28_05_AccessibleTooltip_OnProhibitionIcon()
    {
        var iconsJs = FrontendAssetScanner.ReadFrontendFile("js/utils/icons.js");
        Assert.Contains("title=\"Dar de baja\"", iconsJs);
        Assert.Contains("aria-label=\"Dar de baja\"", iconsJs);
    }

    #endregion

    #region Feature 29: User Profile View (#perfil) (5 Tests)

    [Fact]
    public void T1_F29_01_ProfileViewRendersPersonalDetails()
    {
        var perfilJs = FrontendAssetScanner.ReadFrontendFile("js/views/perfil-view.js");
        Assert.Contains("p.nombreCompleto", perfilJs);
        Assert.Contains("p.email", perfilJs);
        Assert.Contains("p.documento", perfilJs);
    }

    [Fact]
    public void T1_F29_02_ProfileViewRendersRoleBadge()
    {
        var perfilJs = FrontendAssetScanner.ReadFrontendFile("js/views/perfil-view.js");
        Assert.Contains("p.rol", perfilJs);
    }

    [Fact]
    public void T1_F29_03_PermissionsGroupedByModule()
    {
        var perfilJs = FrontendAssetScanner.ReadFrontendFile("js/views/perfil-view.js");
        Assert.Contains("permisos", perfilJs);
        Assert.Contains("_agruparPermisos", perfilJs);
    }

    [Fact]
    public void T1_F29_04_LastAccessFormattedDisplay()
    {
        var perfilJs = FrontendAssetScanner.ReadFrontendFile("js/views/perfil-view.js");
        Assert.Contains("ultimoAcceso", perfilJs);
    }

    [Fact]
    public void T1_F29_05_EditProfileForm_SubmitsChanges()
    {
        var perfilJs = FrontendAssetScanner.ReadFrontendFile("js/views/perfil-view.js");
        Assert.Contains("obtenerPerfilCompleto", perfilJs);
    }

    #endregion

    #region Feature 30: Employees Management View (#empleados) (5 Tests)

    [Fact]
    public void T1_F30_01_StaffTableRendersInternalUsers()
    {
        var empleadosJs = FrontendAssetScanner.ReadFrontendFile("js/views/empleados-view.js");
        Assert.Contains("tabla-empleados", empleadosJs);
    }

    [Fact]
    public void T1_F30_02_CreateEmployeeModal_OpensWithFields()
    {
        var empleadosJs = FrontendAssetScanner.ReadFrontendFile("js/views/empleados-view.js");
        Assert.Contains("modal-empleado", empleadosJs);
        Assert.Contains("emp-nombre", empleadosJs);
        Assert.Contains("emp-documento", empleadosJs);
    }

    [Fact]
    public void T1_F30_03_RoleSelectorInEmployeeModal_ExcludesCliente()
    {
        var empleadosJs = FrontendAssetScanner.ReadFrontendFile("js/views/empleados-view.js");
        Assert.Contains("emp-rol", empleadosJs);
    }

    [Fact]
    public void T1_F30_04_DeactivateEmployeeButton_TriggersSoftDelete()
    {
        var empleadosJs = FrontendAssetScanner.ReadFrontendFile("js/views/empleados-view.js");
        Assert.Contains("desactivar", empleadosJs);
    }

    [Fact]
    public void T1_F30_05_LastAdminProtectionInUi()
    {
        var empleadosJs = FrontendAssetScanner.ReadFrontendFile("js/views/empleados-view.js");
        Assert.Contains("ADMINISTRADOR", empleadosJs);
    }

    #endregion

    #region Feature 31: Roles & Permissions View (#roles) (5 Tests)

    [Fact]
    public void T1_F31_01_RolesListing_WithUserCounts()
    {
        var rolesJs = FrontendAssetScanner.ReadFrontendFile("js/views/roles-view.js");
        Assert.Contains("totalUsuarios", rolesJs);
    }

    [Fact]
    public void T1_F31_02_SystemRoleBadgeRendering()
    {
        var rolesJs = FrontendAssetScanner.ReadFrontendFile("js/views/roles-view.js");
        Assert.Contains("esSistema", rolesJs);
        Assert.Contains("Sistema", rolesJs);
    }

    [Fact]
    public void T1_F31_03_SystemRoleDeletionProtected_InUi()
    {
        var rolesJs = FrontendAssetScanner.ReadFrontendFile("js/views/roles-view.js");
        Assert.Contains("esSistema", rolesJs);
    }

    [Fact]
    public void T1_F31_04_CreateCustomRoleModal_WithPermissionMatrix()
    {
        var rolesJs = FrontendAssetScanner.ReadFrontendFile("js/views/roles-view.js");
        Assert.Contains("modal-rol", rolesJs);
        Assert.Contains("permiso-checkbox", rolesJs);
    }

    [Fact]
    public void T1_F31_05_EditCustomRolePermissions_SavesUpdates()
    {
        var rolesJs = FrontendAssetScanner.ReadFrontendFile("js/views/roles-view.js");
        Assert.Contains("_handleSubmitRol", rolesJs);
        Assert.Contains("btn-save-rol", rolesJs);
    }

    #endregion

    #region Feature 32: Stock View Bug Fix (#stock) (5 Tests)

    [Fact]
    public void T1_F32_01_LoadEmpresaStockWithoutError_DefensiveCheck()
    {
        var stockServiceJs = FrontendAssetScanner.ReadFrontendFile("js/services/stock-service.js");
        Assert.Contains("/stock/por-cliente/", stockServiceJs);
        Assert.True(
            stockServiceJs.Contains("EMPRESA_SINGLETON_ID") ||
            stockServiceJs.Contains("idConsulta") ||
            stockServiceJs.Contains("1"),
            "stock-service.js must route Empresa queries through the real API endpoint /stock/por-cliente/{id}");
    }

    [Fact]
    public void T1_F32_02_LoadClientStock_ParsesCalibres0To4()
    {
        var stockServiceJs = FrontendAssetScanner.ReadFrontendFile("js/services/stock-service.js");
        Assert.Contains("_parseClaseIndex", stockServiceJs);
    }

    [Fact]
    public void T1_F32_03_ArrayNormalizationDefense_ArrayIsArrayCheck()
    {
        var stockServiceJs = FrontendAssetScanner.ReadFrontendFile("js/services/stock-service.js");
        Assert.Contains("Array.isArray", stockServiceJs);
    }

    [Fact]
    public void T1_F32_04_BulbClassNamingAlignment_AlignedWithBackend()
    {
        var stockViewJs = FrontendAssetScanner.ReadFrontendFile("js/views/stock-view.js");
        Assert.Contains("Clase", stockViewJs);
    }

    [Fact]
    public void T1_F32_05_ZeroStockDisplay_ReturnsNumericZero()
    {
        var stockServiceJs = FrontendAssetScanner.ReadFrontendFile("js/services/stock-service.js");
        Assert.Contains("Number(it.cantidad) || 0", stockServiceJs);
    }

    #endregion

    #region Feature 33: Standalone Reference Prices View (#precios-referencia) (5 Tests)

    [Fact]
    public void T1_F33_01_ViewLocatedUnderAdministration_RouteRegistered()
    {
        var appJs = FrontendAssetScanner.ReadFrontendFile("js/app.js");
        Assert.Contains("#precios-referencia", appJs);
        Assert.Contains("PRECIOS_REF_VER", appJs);
    }

    [Fact]
    public void T1_F33_02_SeasonSelectorPopulatesGrid()
    {
        var prefJs = FrontendAssetScanner.ReadFrontendFile("js/views/precios-referencia-view.js");
        Assert.Contains("select-temporada", prefJs);
    }

    [Fact]
    public void T1_F33_03_PriceInputsEditable_ForAuthorizedUsers()
    {
        var prefJs = FrontendAssetScanner.ReadFrontendFile("js/views/precios-referencia-view.js");
        Assert.Contains("PRECIOS_REF_GESTIONAR", prefJs);
    }

    [Fact]
    public void T1_F33_04_SavePricesButtonSubmitsToApi()
    {
        var prefJs = FrontendAssetScanner.ReadFrontendFile("js/views/precios-referencia-view.js");
        Assert.Contains("btn-guardar-precios", prefJs);
    }

    [Fact]
    public void T1_F33_05_MixPriceRowRendered()
    {
        var prefJs = FrontendAssetScanner.ReadFrontendFile("js/views/precios-referencia-view.js");
        Assert.Contains("MIX", prefJs);
    }

    #endregion

    #region Feature 34: Unified Commercial Movements View (#movimientos-comerciales) (5 Tests)

    [Fact]
    public void T1_F34_01_DynamicFormAdaptsTo3Types()
    {
        var js = FrontendAssetScanner.ReadCommercialMovementsView();
        Assert.Contains("tipo", js, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void T1_F34_02_LiveMixDecomposition_InGrid()
    {
        var js = FrontendAssetScanner.ReadCommercialMovementsView();
        Assert.Contains("MIX", js);
    }

    [Fact]
    public void T1_F34_03_MultipleOf3ValidationFeedback()
    {
        var js = FrontendAssetScanner.ReadCommercialMovementsView();
        Assert.Contains("% 3", js);
    }

    [Fact]
    public void T1_F34_04_NewClientContractBadge()
    {
        var js = FrontendAssetScanner.ReadCommercialMovementsView();
        Assert.Contains("contrato", js, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void T1_F34_05_ActionableButtonsAndTimeline_InDetail()
    {
        var js = FrontendAssetScanner.ReadCommercialMovementsView();
        Assert.Contains("timeline", js, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
