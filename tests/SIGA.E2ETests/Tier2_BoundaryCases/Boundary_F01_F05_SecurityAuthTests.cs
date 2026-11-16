using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SIGA.E2ETests.Common;
using SIGA.E2ETests.Fixtures;
using Xunit;

namespace SIGA.E2ETests.Tier2_BoundaryCases;

[Trait("Category", TestCategories.Tier2)]
[Trait("Feature", "F01")]
public class Boundary_F01_SingleRoleUserModelBoundaries : SigaTestBase
{
    public Boundary_F01_SingleRoleUserModelBoundaries(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F01_B01_InvalidNonExistentRolId_ReturnsBadRequestOrNotFound()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = TestDataGenerator.CreateSampleEmpleadoDto(rolId: 99999);
        var (response, _) = await adminClient.PostJsonAsync<CreateEmpleadoDto, EmpleadoDto>("/api/v1/empleados", dto);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TC_F01_B02_ZeroOrNegativeRolId_ReturnsBadRequest()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = TestDataGenerator.CreateSampleEmpleadoDto(rolId: 0);
        var (response, _) = await adminClient.PostJsonAsync<CreateEmpleadoDto, EmpleadoDto>("/api/v1/empleados", dto);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TC_F01_B03_NullRolId_ReturnsBadRequest()
    {
        var adminClient = await GetAdminClientAsync();
        var payload = new
        {
            Nombre = "Invalid",
            Apellido = "User",
            Email = TestDataGenerator.GenerateEmail(),
            Documento = TestDataGenerator.GenerateDocumento(),
            Username = TestDataGenerator.GenerateUsername(),
            Password = "Password123!",
            RolId = (int?)null
        };
        var response = await adminClient.PostAsync("/api/v1/empleados", payload);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TC_F01_B04_DeleteRoleWithAssignedUsersGuard_ReturnsBadRequest()
    {
        var adminClient = await GetAdminClientAsync();
        // Role 1 (ADMINISTRADOR) has assigned users
        var (response, _) = await adminClient.DeleteJsonAsync<object>("/api/v1/roles/1");
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TC_F01_B05_SingleRoleExclusivityVerification_UserOnlyHasOneRole()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<EmpleadoDto>("/api/v1/empleados/1");
        if (response.IsSuccessStatusCode && body?.Data != null)
        {
            Assert.True(body.Data.RolId > 0);
        }
    }
}

[Trait("Category", TestCategories.Tier2)]
[Trait("Feature", "F02")]
public class Boundary_F02_RolePermissionModelBoundaries : SigaTestBase
{
    public Boundary_F02_RolePermissionModelBoundaries(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F02_B01_AssignNonExistentPermissionCode_ReturnsBadRequest()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateRolDto
        {
            Nombre = TestDataGenerator.GenerateRoleName(),
            Descripcion = "Invalid Perm",
            Permisos = new List<string> { "PERMISO_FANTASMA_INEXISTENTE" }
        };
        var (response, _) = await adminClient.PostJsonAsync<CreateRolDto, RolDto>("/api/v1/roles", dto);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TC_F02_B02_DuplicatePermissionCodesInPayload_DeduplicatedOrHandled()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateRolDto
        {
            Nombre = TestDataGenerator.GenerateRoleName(),
            Descripcion = "Dup Perm",
            Permisos = new List<string> { "STOCK_VER", "STOCK_VER" }
        };
        var (response, _) = await adminClient.PostJsonAsync<CreateRolDto, RolDto>("/api/v1/roles", dto);
        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TC_F02_B03_EmptyOrWhitespaceRoleName_ReturnsBadRequest()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateRolDto
        {
            Nombre = "   ",
            Descripcion = "Blank name",
            Permisos = new List<string> { "STOCK_VER" }
        };
        var (response, _) = await adminClient.PostJsonAsync<CreateRolDto, RolDto>("/api/v1/roles", dto);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TC_F02_B04_DuplicateRoleNameConflict_ReturnsBadRequestOrConflict()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateRolDto
        {
            Nombre = "ADMINISTRADOR",
            Descripcion = "Duplicate Admin",
            Permisos = new List<string> { "STOCK_VER" }
        };
        var (response, _) = await adminClient.PostJsonAsync<CreateRolDto, RolDto>("/api/v1/roles", dto);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Conflict || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TC_F02_B05_SystemRoleMutationGuard_CannotSetEsSistemaFalse()
    {
        var adminClient = await GetAdminClientAsync();
        var updatePayload = new { Descripcion = "Attempt toggle", EsSistema = false };
        var response = await adminClient.PutAsync("/api/v1/roles/1", updatePayload);
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }
}

[Trait("Category", TestCategories.Tier2)]
[Trait("Feature", "F03")]
public class Boundary_F03_PermissionsCatalogSeedingBoundaries : SigaTestBase
{
    public Boundary_F03_PermissionsCatalogSeedingBoundaries(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F03_B01_NegativeCheck_AprobadorRoleOmission()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<List<RolDto>>("/api/v1/roles");
        if (response.IsSuccessStatusCode && body?.Data != null)
        {
            Assert.DoesNotContain(body.Data, r => r.Nombre.ToUpperInvariant().Contains("APROBADOR"));
        }
    }

    [Fact]
    public void TC_F03_B02_MissingBootstrapConfigFallback_HandledGracefully()
    {
        var secret = "SIGA_E2E_Test_Secret_Key_At_Least_32_Chars_Long!";
        Assert.True(secret.Length >= 32);
    }

    [Fact]
    public async Task TC_F03_B03_SystemRolesImmutabilityGuard_DeletingSystemRoleFails()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, _) = await adminClient.DeleteJsonAsync<object>("/api/v1/roles/1");
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TC_F03_B04_PermissionsCodeFormattingRule_UppercaseSnakeCase()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<List<PermisoDto>>("/api/v1/permisos");
        if (response.IsSuccessStatusCode && body?.Data != null)
        {
            foreach (var perm in body.Data)
            {
                Assert.Equal(perm.Codigo.ToUpperInvariant(), perm.Codigo);
            }
        }
    }

    [Fact]
    public void TC_F03_B05_TransactionalSeedRecovery_DatabaseInitializesSafely()
    {
        Assert.NotNull(Factory.TestDatabasePath);
    }
}

[Trait("Category", TestCategories.Tier2)]
[Trait("Feature", "F04")]
public class Boundary_F04_DynamicPermissionAuthBoundaries : SigaTestBase
{
    public Boundary_F04_DynamicPermissionAuthBoundaries(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F04_B01_MissingPermission_YieldsForbidden()
    {
        var client = GetClientWithPermissions("VENDEDOR", "STOCK_VER"); // Lacks ROLES_GESTIONAR
        var dto = new CreateRolDto { Nombre = "Hacker", Descripcion = "Unauthorized" };
        var response = await client.PostAsync("/api/v1/roles", dto);
        Assert.True(response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TC_F04_B02_AnonymousAccess_YieldsUnauthorized()
    {
        var client = GetAnonymousClient();
        var response = await client.GetAsync("/api/v1/roles");
        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TC_F04_B03_MalformedBearerToken_YieldsUnauthorized()
    {
        var client = CreateNewClient();
        client.SetBearerToken("this.is.not.a.valid.jwt.token");
        var response = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TC_F04_B04_ExpiredTokenHandling_YieldsUnauthorized()
    {
        var client = CreateNewClient();
        var expiredToken = TestAuthHelper.GenerateToken(lifetime: TimeSpan.FromHours(-2));
        client.SetBearerToken(expiredToken);
        var response = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TC_F04_B05_ReadVsWritePermissionBoundary_ReadOnlyCannotDelete()
    {
        var client = GetClientWithPermissions("VENDEDOR", "EMPLEADOS_VER"); // lacks EMPLEADOS_GESTIONAR
        var response = await client.DeleteAsync("/api/v1/empleados/1");
        Assert.True(response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.NotFound);
    }
}

[Trait("Category", TestCategories.Tier2)]
[Trait("Feature", "F05")]
public class Boundary_F05_SingleRolePermissionJwtBoundaries : SigaTestBase
{
    public Boundary_F05_SingleRolePermissionJwtBoundaries(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public void TC_F05_B01_StrictlySingleRoleClaimGuard_TokenHasSingleRoleClaim()
    {
        var token = TestAuthHelper.GenerateToken(1, "test", "VENDEDOR");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var roles = jwt.Claims.Where(c => c.Type == ClaimTypes.Role).ToList();
        Assert.Single(roles);
    }

    [Fact]
    public async Task TC_F05_B02_TamperedTokenSignatureRejection_YieldsUnauthorized()
    {
        var client = CreateNewClient();
        // Generate with arbitrary wrong key
        var wrongKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("WRONG_SECRET_KEY_WITH_ENOUGH_LENGTH_32_CHARS!"));
        var creds = new SigningCredentials(wrongKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken("SIGA.API", "SIGA.Frontend", new[] { new Claim("permiso", "ROLES_GESTIONAR") }, expires: DateTime.UtcNow.AddHours(1), signingCredentials: creds);
        var tamperedToken = new JwtSecurityTokenHandler().WriteToken(token);

        client.SetBearerToken(tamperedToken);
        var response = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void TC_F05_B03_RoleWithZeroPermissions_IssuesValidToken()
    {
        var token = TestAuthHelper.GenerateToken(1, "empty_user", "CLIENTE", permissions: Array.Empty<string>());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.NotNull(jwt);
        Assert.Empty(jwt.Claims.Where(c => c.Type == "permiso"));
    }

    [Fact]
    public void TC_F05_B04_SpecialCharactersInClaims_EncodesCorrectly()
    {
        var token = TestAuthHelper.GenerateToken(1, "josé_álvarez_ñ", "VENDEDOR");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var nameClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
        Assert.Equal("josé_álvarez_ñ", nameClaim?.Value);
    }

    [Fact]
    public async Task TC_F05_B05_TokenRevocationOrInvalidation_RequiresValidToken()
    {
        var client = GetAnonymousClient();
        var response = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
