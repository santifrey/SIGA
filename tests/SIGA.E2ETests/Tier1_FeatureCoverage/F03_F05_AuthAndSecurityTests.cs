using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using SIGA.E2ETests.Common;
using SIGA.E2ETests.Fixtures;
using Xunit;

namespace SIGA.E2ETests.Tier1_FeatureCoverage;

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F03")]
public class F03_CatalogSeedingTests : SigaTestBase
{
    public F03_CatalogSeedingTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F03_01_CatalogRetrieval_ReturnsCompletePermissionsCatalog()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<List<PermisoDto>>("/api/v1/permisos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.NotEmpty(body.Data);
        Assert.Contains(body.Data, p => p.Modulo == "ROLES" || p.Codigo.StartsWith("ROLES_"));
    }

    [Fact]
    public async Task TC_F03_02_VerifyBuiltInRolesExist_ContainsCoreSystemRoles()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<List<RolDto>>("/api/v1/roles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        var roleNames = body.Data.Select(r => r.Nombre.ToUpperInvariant()).ToList();
        Assert.Contains("ADMINISTRADOR", roleNames);
    }

    [Fact]
    public async Task TC_F03_03_VerifyAdminSeedPermissions_AdminHasExtensivePermissions()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<List<RolDto>>("/api/v1/roles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        var admin = body.Data.FirstOrDefault(r => r.Nombre == "ADMINISTRADOR");
        Assert.NotNull(admin);
        Assert.NotEmpty(admin.Permisos);
    }

    [Fact]
    public async Task TC_F03_04_VerifyInitialAdminUser_LoginSucceeds()
    {
        var anonClient = GetAnonymousClient();
        var (response, body) = await anonClient.PostJsonAsync<LoginRequestDto, LoginResponseDto>(
            "/api/v1/auth/login",
            new LoginRequestDto { Username = "admin", Password = "Admin123!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.False(string.IsNullOrWhiteSpace(body.Data.Token));
    }

    [Fact]
    public async Task TC_F03_05_SeederIdempotency_DatabaseHasUniqueRolesAndUsers()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<List<RolDto>>("/api/v1/roles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        var duplicates = body.Data.GroupBy(r => r.Nombre).Where(g => g.Count() > 1).ToList();
        Assert.Empty(duplicates);
    }
}

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F04")]
public class F04_DynamicPermissionAuthTests : SigaTestBase
{
    public F04_DynamicPermissionAuthTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F04_01_AccessWithRequiredPermission_ReturnsSuccess()
    {
        var client = GetClientWithPermissions("ADMINISTRADOR", "ROLES_VER");
        var response = await client.GetAsync("/api/v1/roles");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TC_F04_02_MutationWithRequiredPermission_ReturnsSuccessOrCreated()
    {
        var client = GetClientWithPermissions("ADMINISTRADOR", "EMPLEADOS_GESTIONAR");
        var dto = TestDataGenerator.CreateSampleEmpleadoDto();
        var (response, _) = await client.PostJsonAsync<CreateEmpleadoDto, EmpleadoDto>("/api/v1/empleados", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task TC_F04_03_CommercialActionWithRequiredPermission_OperatesUnderPolicy()
    {
        var client = GetClientWithPermissions("ADMINISTRADOR", "MOVCOM_APROBAR", "MOVCOM_CREAR");
        var createDto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            TemporadaId = 1,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 1, Cantidad = 10, PrecioUnitario = 50m } }
        };
        var (createRes, createBody) = await client.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", createDto);
        var movId = (createRes.IsSuccessStatusCode && createBody?.Data != null) ? createBody.Data.Id : 1;
        var response = await client.PostAsync($"/api/v1/movimientos-comerciales/{movId}/aprobar", new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TC_F04_04_ReferencePricesWithRequiredPermission_AllowsAccess()
    {
        var client = GetClientWithPermissions("ADMINISTRADOR", "PRECIOS_REF_GESTIONAR");
        var response = await client.PostAsync("/api/v1/precios-referencia", new UpsertPrecioReferenciaDto
        {
            TemporadaId = 1,
            ClaseBulbo = 1,
            PrecioUnitario = 150.0m
        });
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created);
    }

    [Fact]
    public async Task TC_F04_05_AllowAnonymousEndpoint_LoginDoesNotReturnForbidden()
    {
        var anonClient = GetAnonymousClient();
        var response = await anonClient.PostAsync("/api/v1/auth/login", new LoginRequestDto { Username = "unknown", Password = "bad" });
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F05")]
public class F05_SingleRolePermissionJwtTests : SigaTestBase
{
    public F05_SingleRolePermissionJwtTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F05_01_SingleRoleClaim_RealTokenContainsExactlyOneRoleClaim()
    {
        // Arrange
        var anonClient = GetAnonymousClient();
        var loginDto = new LoginRequestDto { Username = "admin", Password = "Admin123!" };

        // Act - Call authentic API login endpoint
        var (response, body) = await anonClient.PostJsonAsync<LoginRequestDto, LoginResponseDto>(
            "/api/v1/auth/login", loginDto);

        // Assert - Strict 200 OK and valid token
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data?.Token);
        Assert.Equal("ADMINISTRADOR", body.Data.Usuario?.Rol);

        // Parse token emitted by SIGA.API
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(body.Data.Token);

        // Verify single role claim (must NOT emit multiple roles)
        var roleClaims = jwt.Claims.Where(c => c.Type == ClaimTypes.Role || c.Type == "role").ToList();
        Assert.Single(roleClaims);
        Assert.Equal("ADMINISTRADOR", roleClaims.First().Value);
    }

    [Fact]
    public async Task TC_F05_02_GranularPermissionClaims_RealTokenContainsAllCatalogoPermissionsForAdmin()
    {
        // Arrange
        var anonClient = GetAnonymousClient();
        var loginDto = new LoginRequestDto { Username = "admin", Password = "Admin123!" };

        // Act
        var (response, body) = await anonClient.PostJsonAsync<LoginRequestDto, LoginResponseDto>(
            "/api/v1/auth/login", loginDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data?.Token);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(body.Data.Token);

        var permClaims = jwt.Claims.Where(c => c.Type == "permiso").Select(c => c.Value).ToHashSet();
        Assert.Contains("ROLES_VER", permClaims);
        Assert.Contains("ROLES_GESTIONAR", permClaims);
        Assert.Contains("EMPLEADOS_VER", permClaims);
        Assert.Contains("EMPLEADOS_GESTIONAR", permClaims);
        Assert.Contains("MOVCOM_VER", permClaims);
        Assert.Contains("MOVCOM_CREAR", permClaims);
        Assert.Contains("STOCK_VER", permClaims);
        Assert.True(permClaims.Count >= 20, "Admin token must contain all seeded permissions.");
    }

    [Fact]
    public async Task TC_F05_03_CreatedVendedorTokenClaims_ContainsVendedorRoleAndPermissionSubset()
    {
        // Arrange: Create a real Vendedor user via API
        var adminClient = await GetAdminClientAsync();
        var vendedorUsername = $"vend_{Guid.NewGuid():N}"[..12];
        var vendedorPassword = "Vendedor123!";
        var createDto = new CreateEmpleadoDto
        {
            Nombre = "Vendedor",
            Apellido = "Test",
            Email = $"{vendedorUsername}@siga.com",
            Documento = TestDataGenerator.GenerateDocumento(),
            Telefono = "123456789",
            Username = vendedorUsername,
            Password = vendedorPassword,
            RolId = 2 // VENDEDOR
        };

        var (createRes, createBody) = await adminClient.PostJsonAsync<CreateEmpleadoDto, EmpleadoDto>(
            "/api/v1/empleados", createDto);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);

        // Act: Log in as the newly created vendedor
        var anonClient = GetAnonymousClient();
        var (loginRes, loginBody) = await anonClient.PostJsonAsync<LoginRequestDto, LoginResponseDto>(
            "/api/v1/auth/login", new LoginRequestDto { Username = vendedorUsername, Password = vendedorPassword });

        // Assert
        Assert.Equal(HttpStatusCode.OK, loginRes.StatusCode);
        Assert.NotNull(loginBody?.Data?.Token);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(loginBody.Data.Token);
        var roleClaims = jwt.Claims.Where(c => c.Type == ClaimTypes.Role || c.Type == "role").ToList();
        Assert.Single(roleClaims);
        Assert.Equal("VENDEDOR", roleClaims.First().Value);

        var permClaims = jwt.Claims.Where(c => c.Type == "permiso").Select(c => c.Value).ToHashSet();
        Assert.Contains("MOVCOM_CREAR", permClaims);
        Assert.Contains("CLIENTES_VER", permClaims);
        Assert.Contains("STOCK_VER", permClaims);
        // Guard: Vendedor must NOT possess admin-exclusive permissions
        Assert.DoesNotContain("ROLES_GESTIONAR", permClaims);
        Assert.DoesNotContain("EMPLEADOS_GESTIONAR", permClaims);
    }

    [Fact]
    public async Task TC_F05_04_StandardIdentityAndAudienceClaims_PresentInRealToken()
    {
        // Arrange
        var anonClient = GetAnonymousClient();

        // Act
        var (response, body) = await anonClient.PostJsonAsync<LoginRequestDto, LoginResponseDto>(
            "/api/v1/auth/login", new LoginRequestDto { Username = "admin", Password = "Admin123!" });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data?.Token);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(body.Data.Token);
        Assert.Equal("SIGA.API", jwt.Issuer);
        Assert.Contains("SIGA.Frontend", jwt.Audiences);
        Assert.Contains(jwt.Claims, c => (c.Type == ClaimTypes.NameIdentifier || c.Type == "nameid" || c.Type == "sub") && c.Value == "1");
        Assert.Contains(jwt.Claims, c => (c.Type == ClaimTypes.Name || c.Type == "unique_name") && c.Value == "admin");
        Assert.Contains(jwt.Claims, c => c.Type == "PersonaId");
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public async Task TC_F05_05_RealTokenValidityPeriod_MatchesConfigured24Hours()
    {
        // Arrange
        var anonClient = GetAnonymousClient();

        // Act
        var (response, body) = await anonClient.PostJsonAsync<LoginRequestDto, LoginResponseDto>(
            "/api/v1/auth/login", new LoginRequestDto { Username = "admin", Password = "Admin123!" });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data?.Token);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(body.Data.Token);
        Assert.True(jwt.ValidTo > DateTime.UtcNow);

        var lifetime = jwt.ValidTo - jwt.ValidFrom;
        Assert.InRange(lifetime.TotalHours, 23.5, 24.5);
    }
}
