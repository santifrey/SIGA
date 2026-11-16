using System.Net;
using SIGA.E2ETests.Common;
using SIGA.E2ETests.Fixtures;
using Xunit;

namespace SIGA.E2ETests.Tier1_FeatureCoverage;

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F06")]
public class F06_RolesPermissionsCrudTests : SigaTestBase
{
    public F06_RolesPermissionsCrudTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F06_01_ListPermisos_ReturnsCatalog()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<List<PermisoDto>>("/api/v1/permisos");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.NotEmpty(body.Data);
    }

    [Fact]
    public async Task TC_F06_02_ListRoles_ReturnsRolesArray()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<List<RolDto>>("/api/v1/roles");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.NotEmpty(body.Data);
    }

    [Fact]
    public async Task TC_F06_03_CreateRole_ReturnsCreated()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateRolDto
        {
            Nombre = TestDataGenerator.GenerateRoleName(),
            Descripcion = "Testing role",
            Permisos = new List<string> { "STOCK_VER" }
        };
        var (response, body) = await adminClient.PostJsonAsync<CreateRolDto, RolDto>("/api/v1/roles", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body?.Data);
    }

    [Fact]
    public async Task TC_F06_04_UpdateRole_ReturnsSuccess()
    {
        var adminClient = await GetAdminClientAsync();
        var createDto = new CreateRolDto
        {
            Nombre = TestDataGenerator.GenerateRoleName(),
            Descripcion = "Initial role to update",
            Permisos = new List<string> { "STOCK_VER" }
        };
        var (createRes, createBody) = await adminClient.PostJsonAsync<CreateRolDto, RolDto>("/api/v1/roles", createDto);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        Assert.NotNull(createBody?.Data);

        var dto = new UpdateRolDto { Descripcion = "New desc", Permisos = new List<string> { "STOCK_VER", "CLIENTES_VER" } };
        var (response, body) = await adminClient.PutJsonAsync<UpdateRolDto, RolDto>($"/api/v1/roles/{createBody.Data.Id}", dto);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.Equal(2, body.Data.Permisos.Count);
    }

    [Fact]
    public async Task TC_F06_05_DeleteUnusedCustomRole_Succeeds()
    {
        var adminClient = await GetAdminClientAsync();
        var createDto = new CreateRolDto
        {
            Nombre = TestDataGenerator.GenerateRoleName(),
            Descripcion = "Role to delete",
            Permisos = new List<string> { "STOCK_VER" }
        };
        var (createRes, createBody) = await adminClient.PostJsonAsync<CreateRolDto, RolDto>("/api/v1/roles", createDto);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        Assert.NotNull(createBody?.Data);

        var (response, _) = await adminClient.DeleteJsonAsync<object>($"/api/v1/roles/{createBody.Data.Id}");
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent);
    }
}

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F07")]
public class F07_EmployeesCrudTests : SigaTestBase
{
    public F07_EmployeesCrudTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F07_01_ListEmployees_ReturnsInternalStaff()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<List<EmpleadoDto>>("/api/v1/empleados");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.NotEmpty(body.Data);
    }

    [Fact]
    public async Task TC_F07_02_CreateEmployeeTransactionally_CreatesPersonaAndUsuario()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = TestDataGenerator.CreateSampleEmpleadoDto(rolId: 2);
        var (response, body) = await adminClient.PostJsonAsync<CreateEmpleadoDto, EmpleadoDto>("/api/v1/empleados", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body?.Data);
    }

    [Fact]
    public async Task TC_F07_03_GetEmployeeById_ReturnsDetails()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<EmpleadoDto>("/api/v1/empleados/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.Equal(1, body.Data.Id);
    }

    [Fact]
    public async Task TC_F07_04_UpdateEmployee_UpdatesProfile()
    {
        var adminClient = await GetAdminClientAsync();
        var createDto = TestDataGenerator.CreateSampleEmpleadoDto(rolId: 2);
        var (createRes, createBody) = await adminClient.PostJsonAsync<CreateEmpleadoDto, EmpleadoDto>("/api/v1/empleados", createDto);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        Assert.NotNull(createBody?.Data);

        var dto = new UpdateEmpleadoDto
        {
            Nombre = "CarlosMod",
            Apellido = "TestMod",
            Email = TestDataGenerator.GenerateEmail(),
            Documento = TestDataGenerator.GenerateDocumento(),
            RolId = 2
        };
        var (response, body) = await adminClient.PutJsonAsync<UpdateEmpleadoDto, EmpleadoDto>($"/api/v1/empleados/{createBody.Data.Id}", dto);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.Equal("CarlosMod", body.Data.Nombre);
    }

    [Fact]
    public async Task TC_F07_05_SoftDeleteEmployee_DeactivatesUser()
    {
        var adminClient = await GetAdminClientAsync();
        var createDto = TestDataGenerator.CreateSampleEmpleadoDto(rolId: 2);
        var (createRes, createBody) = await adminClient.PostJsonAsync<CreateEmpleadoDto, EmpleadoDto>("/api/v1/empleados", createDto);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        Assert.NotNull(createBody?.Data);

        var (response, body) = await adminClient.DeleteJsonAsync<object>($"/api/v1/empleados/{createBody.Data.Id}");
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent);

        // Verify soft-deleted employee state is Inactivo
        var (getRes, getBody) = await adminClient.GetJsonAsync<EmpleadoDto>($"/api/v1/empleados/{createBody.Data.Id}");
        if (getRes.IsSuccessStatusCode && getBody?.Data != null)
        {
            Assert.Equal("Inactivo", getBody.Data.Estado);
        }
    }
}

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F08")]
public class F08_UserProfileApiTests : SigaTestBase
{
    public F08_UserProfileApiTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F08_01_ProfileStructure_ReturnsAllExpectedFields()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<UserProfileDto>("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.True(body.Data.UsuarioId > 0);
        Assert.False(string.IsNullOrWhiteSpace(body.Data.Username));
    }

    [Fact]
    public async Task TC_F08_02_FullNameCombination_ConcatenatesNombreAndApellido()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<UserProfileDto>("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.Contains(body.Data.Nombre, body.Data.NombreCompleto);
    }

    [Fact]
    public async Task TC_F08_03_PermissionsListPopulation_MatchesRolePermissions()
    {
        var client = GetClientWithPermissions("VENDEDOR", "MOVCOM_VER", "MOVCOM_CREAR");
        var (response, body) = await client.GetJsonAsync<UserProfileDto>("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.Contains("MOVCOM_VER", body.Data.Permisos);
    }

    [Fact]
    public async Task TC_F08_04_SingleRoleMatch_MatchesAssignedRole()
    {
        var client = GetClientWithPermissions("SECRETARIA", "CLIENTES_VER");
        var (response, body) = await client.GetJsonAsync<UserProfileDto>("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.Equal("SECRETARIA", body.Data.Rol);
    }

    [Fact]
    public async Task TC_F08_05_LastAccessUpdate_RecordedOnLogin()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<UserProfileDto>("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.NotNull(body.Data.UltimoAcceso);
    }
}

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F09")]
public class F09_OmissionOfNotificationsTests : SigaTestBase
{
    public F09_OmissionOfNotificationsTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F09_01_NotificationsBaseRoute_Returns404NotFound()
    {
        var adminClient = await GetAdminClientAsync();
        var response = await adminClient.GetAsync("/api/v1/notificaciones");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TC_F09_02_PostNotificationsRoute_Returns404NotFound()
    {
        var adminClient = await GetAdminClientAsync();
        var response = await adminClient.PostAsync("/api/v1/notificaciones", new { });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TC_F09_03_UnreadNotificationsRoute_Returns404NotFound()
    {
        var adminClient = await GetAdminClientAsync();
        var response = await adminClient.GetAsync("/api/v1/notificaciones/unread");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TC_F09_04_NoHubWebSocketEndpoint_Returns404NotFound()
    {
        var adminClient = await GetAdminClientAsync();
        var response = await adminClient.GetAsync("/hubs/notifications");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void TC_F09_05_FrontendTopbarAbsence_NoNotificationBellInHtml()
    {
        var indexPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/index.html"));
        if (File.Exists(indexPath))
        {
            var html = File.ReadAllText(indexPath);
            Assert.DoesNotContain("btn-notifications", html);
            Assert.DoesNotContain("notification-bell", html);
        }
    }
}

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F10")]
public class F10_SecurityAuthUnitTestsExecution : SigaTestBase
{
    public F10_SecurityAuthUnitTestsExecution(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F10_01_SingleRoleConstraint_AdminHasSingleRoleInProfile()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<UserProfileDto>("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.Equal(1, body.Data.RolId);
        Assert.Equal("ADMINISTRADOR", body.Data.Rol);
    }

    [Fact]
    public async Task TC_F10_02_DynamicPermissionAuthorizationHandler_BlocksUnauthorizedEndpoint()
    {
        // Client with CLIENTES_VER only, attempting to call ROLES_GESTIONAR endpoint
        var client = GetClientWithPermissions("VENDEDOR", "CLIENTES_VER");
        var roleDto = new CreateRolDto { Nombre = "UnauthorizedRole", Permisos = new List<string> { "STOCK_VER" } };

        var (response, _) = await client.PostJsonAsync<CreateRolDto, RolDto>("/api/v1/roles", roleDto);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task TC_F10_03_SystemRoleDeletionGuard_RejectsDeleteOnEsSistemaRole()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, _) = await adminClient.DeleteJsonAsync<object>("/api/v1/roles/1"); // ADMINISTRADOR (EsSistema=true)

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TC_F10_04_LastAdminDeletionGuard_RejectsSoftDeleteOnOnlyAdmin()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, _) = await adminClient.DeleteJsonAsync<object>("/api/v1/empleados/1"); // Initial admin

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TC_F10_05_JwtTokenAuthentication_RejectsTamperedOrExpiredToken()
    {
        var tamperedClient = CreateNewClient();
        tamperedClient.SetBearerToken("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.invalid.signature");

        var response = await tamperedClient.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
