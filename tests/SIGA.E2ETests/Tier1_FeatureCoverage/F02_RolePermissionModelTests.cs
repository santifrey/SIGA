using System.Net;
using SIGA.E2ETests.Common;
using SIGA.E2ETests.Fixtures;
using Xunit;

namespace SIGA.E2ETests.Tier1_FeatureCoverage;

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F02")]
public class F02_RolePermissionModelTests : SigaTestBase
{
    public F02_RolePermissionModelTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F02_01_QuerySystemRoles_SystemRolesHaveEsSistemaTrue()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<List<RolDto>>("/api/v1/roles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        var adminRole = body.Data.FirstOrDefault(r => r.Nombre == "ADMINISTRADOR");
        Assert.NotNull(adminRole);
        Assert.True(adminRole.EsSistema);
    }

    [Fact]
    public async Task TC_F02_02_CreateCustomRole_ReturnsCreatedAndEsSistemaFalse()
    {
        var adminClient = await GetAdminClientAsync();
        var roleDto = new CreateRolDto
        {
            Nombre = TestDataGenerator.GenerateRoleName(),
            Descripcion = "Custom supervisor role",
            Permisos = new List<string> { "STOCK_VER", "CLIENTES_VER" }
        };

        var (response, body) = await adminClient.PostJsonAsync<CreateRolDto, RolDto>("/api/v1/roles", roleDto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.False(body.Data.EsSistema);
        Assert.Equal(roleDto.Nombre, body.Data.Nombre);
    }

    [Fact]
    public async Task TC_F02_03_AddPermissionsToCustomRole_UpdatesPermissionsCount()
    {
        var adminClient = await GetAdminClientAsync();
        var createDto = new CreateRolDto
        {
            Nombre = TestDataGenerator.GenerateRoleName(),
            Descripcion = "Initial role",
            Permisos = new List<string> { "STOCK_VER" }
        };
        var (createRes, createBody) = await adminClient.PostJsonAsync<CreateRolDto, RolDto>("/api/v1/roles", createDto);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        Assert.NotNull(createBody?.Data);
        var roleId = createBody.Data.Id;

        var updateDto = new UpdateRolDto
        {
            Descripcion = "Updated description",
            Permisos = new List<string> { "STOCK_VER", "CLIENTES_VER", "FILAS_VER" }
        };

        var (response, body) = await adminClient.PutJsonAsync<UpdateRolDto, RolDto>($"/api/v1/roles/{roleId}", updateDto);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.Equal(3, body.Data.Permisos.Count);
    }

    [Fact]
    public async Task TC_F02_04_RemovePermissionFromCustomRole_DecrementsPermissions()
    {
        var adminClient = await GetAdminClientAsync();
        var createDto = new CreateRolDto
        {
            Nombre = TestDataGenerator.GenerateRoleName(),
            Descripcion = "Role for removal test",
            Permisos = new List<string> { "STOCK_VER", "CLIENTES_VER" }
        };
        var (createRes, createBody) = await adminClient.PostJsonAsync<CreateRolDto, RolDto>("/api/v1/roles", createDto);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        Assert.NotNull(createBody?.Data);
        var roleId = createBody.Data.Id;

        var updateDto = new UpdateRolDto
        {
            Descripcion = "Reduced permissions",
            Permisos = new List<string> { "STOCK_VER" }
        };

        var (response, body) = await adminClient.PutJsonAsync<UpdateRolDto, RolDto>($"/api/v1/roles/{roleId}", updateDto);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.Single(body.Data.Permisos);
    }

    [Fact]
    public async Task TC_F02_05_QueryRoleDetails_ReturnsAssignedPermissionsList()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<RolDto>("/api/v1/roles/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.NotNull(body.Data.Permisos);
        Assert.NotEmpty(body.Data.Permisos);
    }
}
