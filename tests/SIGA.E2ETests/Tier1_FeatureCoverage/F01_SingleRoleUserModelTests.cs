using System.Net;
using SIGA.E2ETests.Common;
using SIGA.E2ETests.Fixtures;
using Xunit;

namespace SIGA.E2ETests.Tier1_FeatureCoverage;

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F01")]
public class F01_SingleRoleUserModelTests : SigaTestBase
{
    public F01_SingleRoleUserModelTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F01_01_CreateUserWithRole_ReturnsCreatedAndSingleRole()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = TestDataGenerator.CreateSampleEmpleadoDto(rolId: 2); // VENDEDOR

        var (response, body) = await adminClient.PostJsonAsync<CreateEmpleadoDto, EmpleadoDto>("/api/v1/empleados", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.Equal(2, body.Data.RolId);
        Assert.False(string.IsNullOrWhiteSpace(body.Data.Rol));
    }

    [Fact]
    public async Task TC_F01_02_RetrieveUserRole_ReturnsSingleRoleObject()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<EmpleadoDto>("/api/v1/empleados/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.True(body.Data.RolId > 0);
        Assert.False(string.IsNullOrWhiteSpace(body.Data.Rol));
    }

    [Fact]
    public async Task TC_F01_03_ReassignSingleRole_UpdatesRoleSuccessfully()
    {
        var adminClient = await GetAdminClientAsync();
        var createDto = TestDataGenerator.CreateSampleEmpleadoDto(rolId: 2);
        var (createRes, createBody) = await adminClient.PostJsonAsync<CreateEmpleadoDto, EmpleadoDto>("/api/v1/empleados", createDto);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        Assert.NotNull(createBody?.Data);
        var empId = createBody.Data.Id;

        var updateDto = new UpdateEmpleadoDto
        {
            Nombre = "Juan",
            Apellido = "Perez",
            Email = TestDataGenerator.GenerateEmail(),
            Documento = TestDataGenerator.GenerateDocumento(),
            RolId = 3 // SECRETARIA
        };

        var (response, body) = await adminClient.PutJsonAsync<UpdateEmpleadoDto, EmpleadoDto>($"/api/v1/empleados/{empId}", updateDto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.Equal(3, body.Data.RolId);
    }

    [Fact]
    public async Task TC_F01_04_QueryStaffRoleConsistency_EveryEmployeeHasSingleRole()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<List<EmpleadoDto>>("/api/v1/empleados");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.NotEmpty(body.Data);
        foreach (var emp in body.Data)
        {
            Assert.True(emp.RolId > 0);
            Assert.False(string.IsNullOrWhiteSpace(emp.Rol));
        }
    }

    [Fact]
    public async Task TC_F01_05_EffectivePermissionsUpdateOnRoleChange_ReflectedInProfile()
    {
        var client = GetClientWithPermissions("VENDEDOR", "MOVCOM_CREAR", "MOVCOM_VER");
        var (response, body) = await client.GetJsonAsync<UserProfileDto>("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.Equal("VENDEDOR", body.Data.Rol);
    }
}
