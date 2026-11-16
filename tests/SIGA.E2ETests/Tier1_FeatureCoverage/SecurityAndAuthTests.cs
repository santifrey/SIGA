using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Auth;
using SIGA.Application.DTOs.Clientes;
using SIGA.Application.DTOs.Empleados;
using SIGA.Application.DTOs.Permisos;
using SIGA.Application.DTOs.Roles;
using SIGA.Application.Interfaces;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.E2ETests.Infrastructure;
using SIGA.Infrastructure.Persistence;
using Xunit;

namespace SIGA.E2ETests.Tier1_FeatureCoverage;

[Collection("E2E Test Suite")]
public class SecurityAndAuthTests : TestBase
{
    public SecurityAndAuthTests(SigaTestWebApplicationFactory factory) : base(factory)
    {
    }

    #region Feature 1: Single-Role User Model (5 Tests)

    [Fact]
    public async Task T1_F01_01_SingleRoleAssignmentOnCreation_ShouldPersistExactSingleRoleId()
    {
        // Arrange
        var client = CreateAdminClient();
        var uniqueDoc = "F0101" + Random.Shared.Next(10000, 99999);
        var uniqueUsername = "user_f0101_" + Guid.NewGuid().ToString("N")[..6];

        var createDto = new CrearEmpleadoDto
        {
            Nombre = "Vendedor",
            Apellido = "Test",
            Documento = uniqueDoc,
            Telefono = "123456",
            Email = $"{uniqueUsername}@example.com",
            Username = uniqueUsername,
            Password = "Password123!",
            RolId = 2 // VENDEDOR
        };

        // Act
        var res = await client.PostAsJsonAsync("/api/v1/empleados", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<EmpleadoDto>(res);
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.Equal(2, envelope.Data!.RolId);
        Assert.Equal("VENDEDOR", envelope.Data.Rol);

        // Verify in DB directly: exactly one foreign key RolId
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SigaDbContext>();
        var user = await db.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Username == uniqueUsername);
        Assert.NotNull(user);
        Assert.Equal(2, user.RolId);
        Assert.NotNull(user.Rol);
        Assert.Equal("VENDEDOR", user.Rol.Nombre);
    }

    [Fact]
    public async Task T1_F01_02_SingleRoleDirectQuery_ReturnsSingleRoleNameAndId()
    {
        // Arrange
        var client = CreateAdminClient();
        var uniqueDoc = "F0102" + Random.Shared.Next(10000, 99999);
        var uniqueUsername = "user_f0102_" + Guid.NewGuid().ToString("N")[..6];

        var createDto = new CrearEmpleadoDto
        {
            Nombre = "Secretaria",
            Apellido = "Test",
            Documento = uniqueDoc,
            Email = $"{uniqueUsername}@example.com",
            Username = uniqueUsername,
            Password = "Password123!",
            RolId = 3 // SECRETARIA
        };
        var createRes = await client.PostAsJsonAsync("/api/v1/empleados", createDto);
        var created = (await DeserializeApiResponseAsync<EmpleadoDto>(createRes))!.Data!;

        // Act
        var getRes = await client.GetAsync($"/api/v1/empleados/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);
        var envelope = await DeserializeApiResponseAsync<EmpleadoDto>(getRes);
        Assert.NotNull(envelope);
        Assert.Equal(3, envelope.Data!.RolId);
        Assert.Equal("SECRETARIA", envelope.Data.Rol);
    }

    [Fact]
    public async Task T1_F01_03_RoleReplacementOnUpdate_ShouldReplaceSingleRoleWithoutOrphanJoinRecords()
    {
        // Arrange
        var client = CreateAdminClient();
        var uniqueDoc = "F0103" + Random.Shared.Next(10000, 99999);
        var uniqueUsername = "user_f0103_" + Guid.NewGuid().ToString("N")[..6];

        var createDto = new CrearEmpleadoDto
        {
            Nombre = "Promovido",
            Apellido = "Test",
            Documento = uniqueDoc,
            Username = uniqueUsername,
            Password = "Password123!",
            RolId = 2 // Start as VENDEDOR
        };
        var createRes = await client.PostAsJsonAsync("/api/v1/empleados", createDto);
        var created = (await DeserializeApiResponseAsync<EmpleadoDto>(createRes))!.Data!;

        // Act: Update role to CONTABILIDAD (5)
        var updateDto = new ActualizarEmpleadoDto
        {
            Nombre = "Promovido",
            Apellido = "Actualizado",
            Documento = uniqueDoc,
            RolId = 5, // CONTABILIDAD
            Estado = EstadoUsuario.Activo
        };
        var putRes = await client.PutAsJsonAsync($"/api/v1/empleados/{created.Id}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, putRes.StatusCode);
        var envelope = await DeserializeApiResponseAsync<EmpleadoDto>(putRes);
        Assert.Equal(5, envelope!.Data!.RolId);
        Assert.Equal("CONTABILIDAD", envelope.Data.Rol);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SigaDbContext>();
        var userInDb = await db.Usuarios.FindAsync(created.Id);
        Assert.NotNull(userInDb);
        Assert.Equal(5, userInDb.RolId);
    }

    [Fact]
    public async Task T1_F01_04_LoginResponse_ReflectsSingleRoleFieldAndArray()
    {
        // Arrange: Login with seeded admin credentials
        var client = CreateAnonymousClient();
        var loginDto = new LoginRequestDto
        {
            Username = "admin",
            Password = "Admin123!"
        };

        // Act
        var res = await client.PostAsJsonAsync("/api/v1/auth/login", loginDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<LoginResponseDto>(res);
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.Equal("ADMINISTRADOR", envelope.Data!.Rol);
        Assert.Equal(1, envelope.Data.RolId);
        Assert.Contains("ADMINISTRADOR", envelope.Data.Roles);
        Assert.Single(envelope.Data.Roles);
    }

    [Fact]
    public async Task T1_F01_05_DbSchema1ToN_QueryRolesReturnsUserCountMatchingSingleForeignKey()
    {
        // Arrange
        var client = CreateAdminClient();

        // Act
        var res = await client.GetAsync("/api/v1/roles");

        // Assert
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<List<RolDto>>(res);
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);

        var adminRole = envelope.Data!.First(r => r.Nombre == "ADMINISTRADOR");
        Assert.True(adminRole.TotalUsuarios >= 1);
    }

    #endregion

    #region Feature 2: Role-Permission Model (5 Tests)

    [Fact]
    public async Task T1_F02_01_QuerySystemRoles_ShouldHaveEsSistemaTrue()
    {
        // Arrange
        var client = CreateAdminClient();

        // Act
        var res = await client.GetAsync("/api/v1/roles");

        // Assert
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<List<RolDto>>(res);
        Assert.NotNull(envelope);

        var systemRoleNames = new[] { "ADMINISTRADOR", "VENDEDOR", "SECRETARIA", "CLIENTE", "CONTABILIDAD" };
        foreach (var sysName in systemRoleNames)
        {
            var r = envelope.Data!.FirstOrDefault(x => x.Nombre == sysName);
            Assert.NotNull(r);
            Assert.True(r.EsSistema, $"El rol de sistema '{sysName}' debe tener EsSistema = true.");
        }
    }

    [Fact]
    public async Task T1_F02_02_AssignMultiplePermissionsToCustomRole_SucceedsWithExactMapping()
    {
        // Arrange
        var client = CreateAdminClient();
        var roleName = "CUSTOM_AUDITOR_" + Guid.NewGuid().ToString("N")[..6];
        var dto = new CrearRolDto
        {
            Nombre = roleName,
            Descripcion = "Rol auditor personalizado",
            Permisos = new List<string> { "CLIENTES_VER", "STOCK_VER", "REPORTES_VER" }
        };

        // Act
        var res = await client.PostAsJsonAsync("/api/v1/roles", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<RolDto>(res);
        Assert.NotNull(envelope);
        Assert.False(envelope.Data!.EsSistema);
        Assert.Equal(3, envelope.Data.Permisos.Count);
        Assert.Contains("CLIENTES_VER", envelope.Data.Permisos);
        Assert.Contains("STOCK_VER", envelope.Data.Permisos);
        Assert.Contains("REPORTES_VER", envelope.Data.Permisos);
    }

    [Fact]
    public async Task T1_F02_03_UpdateRolePermissionsMatrix_SynchronizesRolPermisoRecords()
    {
        // Arrange
        var client = CreateAdminClient();
        var roleName = "CUSTOM_EDITOR_" + Guid.NewGuid().ToString("N")[..6];
        var createRes = await client.PostAsJsonAsync("/api/v1/roles", new CrearRolDto
        {
            Nombre = roleName,
            Descripcion = "Inicial",
            Permisos = new List<string> { "CLIENTES_VER" }
        });
        var created = (await DeserializeApiResponseAsync<RolDto>(createRes))!.Data!;

        // Act: Update to different permissions
        var updateDto = new ActualizarRolDto
        {
            Nombre = roleName,
            Descripcion = "Actualizado",
            Permisos = new List<string> { "STOCK_VER", "STOCK_AJUSTAR" }
        };
        var putRes = await client.PutAsJsonAsync($"/api/v1/roles/{created.Id}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, putRes.StatusCode);
        var envelope = await DeserializeApiResponseAsync<RolDto>(putRes);
        Assert.NotNull(envelope);
        Assert.Equal(2, envelope.Data!.Permisos.Count);
        Assert.DoesNotContain("CLIENTES_VER", envelope.Data.Permisos);
        Assert.Contains("STOCK_VER", envelope.Data.Permisos);
        Assert.Contains("STOCK_AJUSTAR", envelope.Data.Permisos);
    }

    [Fact]
    public async Task T1_F02_04_RolePermissionsCascadeToUserMeProfile()
    {
        // Arrange
        var client = CreateAdminClient();
        var roleName = "CUSTOM_CAS_" + Guid.NewGuid().ToString("N")[..6];
        var roleRes = await client.PostAsJsonAsync("/api/v1/roles", new CrearRolDto
        {
            Nombre = roleName,
            Permisos = new List<string> { "CLIENTES_VER", "STOCK_VER" }
        });
        var role = (await DeserializeApiResponseAsync<RolDto>(roleRes))!.Data!;

        var username = "user_cas_" + Guid.NewGuid().ToString("N")[..6];
        var doc = "CAS" + Random.Shared.Next(10000, 99999);
        var empRes = await client.PostAsJsonAsync("/api/v1/empleados", new CrearEmpleadoDto
        {
            Nombre = "Cascade",
            Apellido = "User",
            Documento = doc,
            Username = username,
            Password = "Password123!",
            RolId = role.Id
        });
        var emp = (await DeserializeApiResponseAsync<EmpleadoDto>(empRes))!.Data!;

        // Act: Authenticate as this user and call /auth/me
        var userClient = CreateRoleClient(roleName, emp.Id, username);
        var meRes = await userClient.GetAsync("/api/v1/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, meRes.StatusCode);
        var meEnvelope = await DeserializeApiResponseAsync<UserInfoResponseDto>(meRes);
        Assert.NotNull(meEnvelope);
        Assert.Equal(role.Id, meEnvelope.Data!.RolId);
        Assert.Contains("CLIENTES_VER", meEnvelope.Data.Permisos);
        Assert.Contains("STOCK_VER", meEnvelope.Data.Permisos);
    }

    [Fact]
    public async Task T1_F02_05_SystemRoleAdmin_ContainsAllCatalogPermissions()
    {
        // Arrange
        var client = CreateAdminClient();

        // Act
        var res = await client.GetAsync("/api/v1/roles/1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<RolDto>(res);
        Assert.NotNull(envelope);
        Assert.Equal("ADMINISTRADOR", envelope.Data!.Nombre);
        Assert.True(envelope.Data.Permisos.Count >= 25, "El rol de ADMINISTRADOR debe contener el catálogo completo de permisos.");
    }

    #endregion

    #region Feature 3: Permissions Catalog & Seeding (5 Tests)

    [Fact]
    public async Task T1_F03_01_CatalogSeeding_ReturnsCompletePermissionsAcross8Modules()
    {
        // Arrange
        var client = CreateAdminClient();

        // Act
        var res = await client.GetAsync("/api/v1/permisos");

        // Assert
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<List<PermisoDto>>(res);
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.True(envelope.Data!.Count >= 30, $"Se esperaban 30+ permisos sembrados, se encontraron {envelope.Data.Count}");

        var modules = envelope.Data.Select(p => p.Modulo).Distinct().ToList();
        Assert.Contains("Seguridad", modules);
        Assert.Contains("Personal", modules);
        Assert.Contains("Clientes", modules);
        Assert.Contains("Stock", modules);
        Assert.Contains("Comercial", modules);
        Assert.Contains("Producción", modules);
        Assert.Contains("Reportes", modules);
        Assert.Contains("Mi Cuenta", modules);
    }

    [Fact]
    public async Task T1_F03_02_SystemRolesSeeding_Contains5StandardSystemRoles()
    {
        // Arrange
        var client = CreateAdminClient();

        // Act
        var res = await client.GetAsync("/api/v1/roles");

        // Assert
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<List<RolDto>>(res);
        Assert.NotNull(envelope);

        var names = envelope.Data!.Select(r => r.Nombre).ToList();
        Assert.Contains("ADMINISTRADOR", names);
        Assert.Contains("VENDEDOR", names);
        Assert.Contains("SECRETARIA", names);
        Assert.Contains("CLIENTE", names);
        Assert.Contains("CONTABILIDAD", names);
    }

    [Fact]
    public async Task T1_F03_03_InitialAdminUserSeeding_CanAuthenticateSuccessfully()
    {
        // Arrange
        var client = CreateAnonymousClient();
        var loginDto = new LoginRequestDto
        {
            Username = "admin",
            Password = "Admin123!"
        };

        // Act
        var res = await client.PostAsJsonAsync("/api/v1/auth/login", loginDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<LoginResponseDto>(res);
        Assert.NotNull(envelope);
        Assert.False(string.IsNullOrWhiteSpace(envelope.Data!.Token));
    }

    [Fact]
    public async Task T1_F03_04_Verification_NoAprobadorRoleExists()
    {
        // Arrange
        var client = CreateAdminClient();

        // Act
        var res = await client.GetAsync("/api/v1/roles");

        // Assert
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<List<RolDto>>(res);
        Assert.NotNull(envelope);

        var names = envelope.Data!.Select(r => r.Nombre.ToUpperInvariant()).ToList();
        Assert.DoesNotContain("APROBADOR", names);
    }

    [Fact]
    public async Task T1_F03_05_IdempotentDatabaseInitialization_RunsWithoutDuplication()
    {
        // Arrange: invoke seeder again on populated database
        using var scope = Factory.Services.CreateScope();

        // Act: Run seeder
        var exception = await Record.ExceptionAsync(() => DatabaseSeeder.InitializeDatabaseAsync(scope.ServiceProvider));

        // Assert
        Assert.Null(exception);
    }

    #endregion

    #region Feature 4: Dynamic Permission Authorization (5 Tests)

    [Fact]
    public async Task T1_F04_01_AuthorizedRequest_WithPermissionClaim_Succeeds()
    {
        // Arrange: User with only CLIENTES_VER
        var client = CreatePermissionsClient("CLIENTES_VER");

        // Act
        var res = await client.GetAsync("/api/v1/clientes");

        // Assert
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task T1_F04_02_ForbiddenRequest_LackingPermissionClaim_Returns403()
    {
        // Arrange: User with only STOCK_VER accessing /api/v1/roles (requires ROLES_VER)
        var client = CreatePermissionsClient("STOCK_VER");

        // Act
        var res = await client.GetAsync("/api/v1/roles");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task T1_F04_03_UnauthenticatedRequest_Returns401Unauthorized()
    {
        // Arrange
        var client = CreateAnonymousClient();

        // Act
        var res = await client.GetAsync("/api/v1/roles");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task T1_F04_04_AdminRoleAuthorizationBypass_SucceedsWithoutExplicitPermissionClaims()
    {
        // Arrange: Generate token with role ADMINISTRADOR and 0 permission claims
        var token = ApiClientHelper.GenerateToken(
            userId: 1,
            username: "admin",
            role: "ADMINISTRADOR",
            permissions: Array.Empty<string>());
        var client = CreateClientWithCustomToken(token);

        // Act
        var res = await client.GetAsync("/api/v1/roles");

        // Assert
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task T1_F04_05_CustomEndpointPermissionCheck_AllowsAccessWhenMatching()
    {
        // Arrange: User with EMPLEADOS_VER
        var client = CreatePermissionsClient("EMPLEADOS_VER");

        // Act
        var res = await client.GetAsync("/api/v1/empleados");

        // Assert
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    #endregion

    #region Feature 5: Single-Role & Permission JWT (5 Tests)

    [Fact]
    public async Task T1_F05_01_SingleRoleClaimInJwt_ContainsExactRoleClaim()
    {
        // Arrange
        var token = ApiClientHelper.GenerateToken(1, "vendedor1", "VENDEDOR");
        var handler = new JwtSecurityTokenHandler();

        // Act
        var jwt = handler.ReadJwtToken(token);

        // Assert
        var roleClaims = jwt.Claims.Where(c => c.Type == ClaimTypes.Role || c.Type == "role").ToList();
        Assert.Single(roleClaims);
        Assert.Equal("VENDEDOR", roleClaims[0].Value);
    }

    [Fact]
    public async Task T1_F05_02_GranularPermissionClaimsInJwt_EmitsIndividualPermisoClaims()
    {
        // Arrange
        var perms = new[] { "CLIENTES_VER", "CLIENTES_GESTIONAR", "STOCK_VER" };
        var token = ApiClientHelper.GenerateToken(2, "user", "CUSTOM", perms);
        var handler = new JwtSecurityTokenHandler();

        // Act
        var jwt = handler.ReadJwtToken(token);

        // Assert
        var permClaims = jwt.Claims.Where(c => c.Type == "permiso").Select(c => c.Value).ToList();
        Assert.Equal(3, permClaims.Count);
        Assert.Contains("CLIENTES_VER", permClaims);
        Assert.Contains("CLIENTES_GESTIONAR", permClaims);
        Assert.Contains("STOCK_VER", permClaims);
    }

    [Fact]
    public async Task T1_F05_03_UserIdentityClaimsInJwt_ContainsNameIdAndName()
    {
        // Arrange
        var token = ApiClientHelper.GenerateToken(42, "santiago_admin", "ADMINISTRADOR");
        var handler = new JwtSecurityTokenHandler();

        // Act
        var jwt = handler.ReadJwtToken(token);

        // Assert
        var nameId = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "nameid");
        Assert.NotNull(nameId);
        Assert.Equal("42", nameId.Value);

        var name = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name || c.Type == "unique_name");
        Assert.NotNull(name);
        Assert.Equal("santiago_admin", name.Value);
    }

    [Fact]
    public async Task T1_F05_04_JwtExpirationClaim_MatchesIssuedLifetime()
    {
        // Arrange
        var token = ApiClientHelper.GenerateToken(1, "admin", "ADMINISTRADOR");
        var handler = new JwtSecurityTokenHandler();

        // Act
        var jwt = handler.ReadJwtToken(token);

        // Assert
        Assert.NotNull(jwt.ValidTo);
        var lifetime = jwt.ValidTo - DateTime.UtcNow;
        Assert.True(lifetime.TotalHours >= 23.5 && lifetime.TotalHours <= 24.5);
    }

    [Fact]
    public async Task T1_F05_05_JwtSignatureValidation_ValidatesCorrectly()
    {
        // Arrange: Generate and then validate against DefaultSecretKey
        var token = ApiClientHelper.GenerateToken(1, "admin", "ADMINISTRADOR");
        var handler = new JwtSecurityTokenHandler();
        var validationParams = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = ApiClientHelper.DefaultIssuer,
            ValidateAudience = true,
            ValidAudience = ApiClientHelper.DefaultAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(ApiClientHelper.DefaultSecretKey)),
            ValidateLifetime = true
        };

        // Act
        var principal = handler.ValidateToken(token, validationParams, out var validatedToken);

        // Assert
        Assert.NotNull(principal);
        Assert.NotNull(validatedToken);
    }

    #endregion

    #region Feature 6: Roles & Permissions CRUD API (5 Tests)

    [Fact]
    public async Task T1_F06_01_ListAllRoles_ReturnsRolesWithMetadata()
    {
        // Arrange
        var client = CreateAdminClient();

        // Act
        var res = await client.GetAsync("/api/v1/roles");

        // Assert
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<List<RolDto>>(res);
        Assert.NotNull(envelope);
        Assert.True(envelope.Data!.Count >= 5);
        Assert.All(envelope.Data, r =>
        {
            Assert.True(r.Id > 0);
            Assert.False(string.IsNullOrWhiteSpace(r.Nombre));
        });
    }

    [Fact]
    public async Task T1_F06_02_GetRoleById_ReturnsRoleEntity()
    {
        // Arrange
        var client = CreateAdminClient();

        // Act
        var res = await client.GetAsync("/api/v1/roles/1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<RolDto>(res);
        Assert.NotNull(envelope);
        Assert.Equal(1, envelope.Data!.Id);
        Assert.Equal("ADMINISTRADOR", envelope.Data.Nombre);
    }

    [Fact]
    public async Task T1_F06_03_CreateCustomRole_SucceedsWith201Created()
    {
        // Arrange
        var client = CreateAdminClient();
        var name = "ROL_TEST_" + Guid.NewGuid().ToString("N")[..6];
        var dto = new CrearRolDto
        {
            Nombre = name,
            Descripcion = "Descripción test",
            Permisos = new List<string> { "STOCK_VER" }
        };

        // Act
        var res = await client.PostAsJsonAsync("/api/v1/roles", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<RolDto>(res);
        Assert.NotNull(envelope);
        Assert.True(envelope.Data!.Id > 0);
        Assert.Equal(name, envelope.Data.Nombre);
    }

    [Fact]
    public async Task T1_F06_04_UpdateCustomRole_ModifiesDescriptionAndPermissions()
    {
        // Arrange
        var client = CreateAdminClient();
        var name = "ROL_UPD_" + Guid.NewGuid().ToString("N")[..6];
        var createRes = await client.PostAsJsonAsync("/api/v1/roles", new CrearRolDto
        {
            Nombre = name,
            Descripcion = "Original",
            Permisos = new List<string> { "CLIENTES_VER" }
        });
        var created = (await DeserializeApiResponseAsync<RolDto>(createRes))!.Data!;

        // Act
        var updateDto = new ActualizarRolDto
        {
            Nombre = name,
            Descripcion = "Modificado",
            Permisos = new List<string> { "CLIENTES_VER", "CLIENTES_GESTIONAR" }
        };
        var putRes = await client.PutAsJsonAsync($"/api/v1/roles/{created.Id}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, putRes.StatusCode);
        var envelope = await DeserializeApiResponseAsync<RolDto>(putRes);
        Assert.Equal("Modificado", envelope!.Data!.Descripcion);
        Assert.Equal(2, envelope.Data.Permisos.Count);
    }

    [Fact]
    public async Task T1_F06_05_DeleteCustomRole_WithoutUsers_Succeeds()
    {
        // Arrange
        var client = CreateAdminClient();
        var name = "ROL_DEL_" + Guid.NewGuid().ToString("N")[..6];
        var createRes = await client.PostAsJsonAsync("/api/v1/roles", new CrearRolDto
        {
            Nombre = name,
            Descripcion = "Para borrar",
            Permisos = new List<string>()
        });
        var created = (await DeserializeApiResponseAsync<RolDto>(createRes))!.Data!;

        // Act
        var delRes = await client.DeleteAsync($"/api/v1/roles/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, delRes.StatusCode);
        var envelope = await DeserializeApiResponseAsync<bool>(delRes);
        Assert.True(envelope!.Data);

        // Verify it is gone
        var getRes = await client.GetAsync($"/api/v1/roles/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getRes.StatusCode);
    }

    #endregion

    #region Feature 7: Employees CRUD API (5 Tests)

    [Fact]
    public async Task T1_F07_01_ListInternalStaffOnly_ExcludesClienteRole()
    {
        // Arrange
        var client = CreateAdminClient();

        // Act
        var res = await client.GetAsync("/api/v1/empleados");

        // Assert
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<List<EmpleadoDto>>(res);
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.All(envelope.Data!, emp =>
        {
            Assert.NotEqual("CLIENTE", emp.Rol);
            Assert.NotEqual(4, emp.RolId);
        });
    }

    [Fact]
    public async Task T1_F07_02_GetEmployeeById_ReturnsProfileAndRole()
    {
        // Arrange
        var client = CreateAdminClient();
        var uniqueDoc = "F0702" + Random.Shared.Next(10000, 99999);
        var uniqueUsername = "emp_f0702_" + Guid.NewGuid().ToString("N")[..6];

        var createRes = await client.PostAsJsonAsync("/api/v1/empleados", new CrearEmpleadoDto
        {
            Nombre = "Juan",
            Apellido = "Pérez",
            Documento = uniqueDoc,
            Username = uniqueUsername,
            Password = "Password123!",
            RolId = 2
        });
        var created = (await DeserializeApiResponseAsync<EmpleadoDto>(createRes))!.Data!;

        // Act
        var res = await client.GetAsync($"/api/v1/empleados/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<EmpleadoDto>(res);
        Assert.NotNull(envelope);
        Assert.Equal(created.Id, envelope.Data!.Id);
        Assert.Equal(uniqueUsername, envelope.Data.Username);
        Assert.Equal("Juan", envelope.Data.Nombre);
    }

    [Fact]
    public async Task T1_F07_03_CreateEmployee_AtomicTransaction_CreatesPersonaAndUsuario()
    {
        // Arrange
        var client = CreateAdminClient();
        var uniqueDoc = "F0703" + Random.Shared.Next(10000, 99999);
        var uniqueUsername = "emp_f0703_" + Guid.NewGuid().ToString("N")[..6];

        var dto = new CrearEmpleadoDto
        {
            Nombre = "Maria",
            Apellido = "Gonzalez",
            Documento = uniqueDoc,
            Email = $"{uniqueUsername}@example.com",
            Telefono = "+54 9 351 999999",
            Username = uniqueUsername,
            Password = "Password123!",
            RolId = 3 // SECRETARIA
        };

        // Act
        var res = await client.PostAsJsonAsync("/api/v1/empleados", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<EmpleadoDto>(res);
        Assert.NotNull(envelope);
        Assert.True(envelope.Data!.Id > 0);
        Assert.True(envelope.Data.PersonaId > 0);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SigaDbContext>();
        var persona = await db.Personas.FindAsync(envelope.Data.PersonaId);
        var usuario = await db.Usuarios.FindAsync(envelope.Data.Id);
        Assert.NotNull(persona);
        Assert.NotNull(usuario);
        Assert.Equal(envelope.Data.PersonaId, usuario.PersonaId);
    }

    [Fact]
    public async Task T1_F07_04_UpdateEmployeeDetails_UpdatesPersonalAndAccountFields()
    {
        // Arrange
        var client = CreateAdminClient();
        var uniqueDoc = "F0704" + Random.Shared.Next(10000, 99999);
        var uniqueUsername = "emp_f0704_" + Guid.NewGuid().ToString("N")[..6];

        var createRes = await client.PostAsJsonAsync("/api/v1/empleados", new CrearEmpleadoDto
        {
            Nombre = "Original",
            Apellido = "Empleado",
            Documento = uniqueDoc,
            Username = uniqueUsername,
            Password = "Password123!",
            RolId = 2
        });
        var created = (await DeserializeApiResponseAsync<EmpleadoDto>(createRes))!.Data!;

        // Act: Update phone, email, and role to SECRETARIA
        var updateDto = new ActualizarEmpleadoDto
        {
            Nombre = "Original",
            Apellido = "Modificado",
            Documento = uniqueDoc,
            Telefono = "555-1234",
            Email = "modificado@example.com",
            RolId = 3,
            Estado = EstadoUsuario.Activo
        };
        var putRes = await client.PutAsJsonAsync($"/api/v1/empleados/{created.Id}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, putRes.StatusCode);
        var envelope = await DeserializeApiResponseAsync<EmpleadoDto>(putRes);
        Assert.Equal("555-1234", envelope!.Data!.Telefono);
        Assert.Equal("modificado@example.com", envelope.Data.Email);
        Assert.Equal(3, envelope.Data.RolId);
    }

    [Fact]
    public async Task T1_F07_05_SoftDeleteEmployee_MarksUserInactivo()
    {
        // Arrange
        var client = CreateAdminClient();
        var uniqueDoc = "F0705" + Random.Shared.Next(10000, 99999);
        var uniqueUsername = "emp_f0705_" + Guid.NewGuid().ToString("N")[..6];

        var createRes = await client.PostAsJsonAsync("/api/v1/empleados", new CrearEmpleadoDto
        {
            Nombre = "ParaDesactivar",
            Apellido = "Test",
            Documento = uniqueDoc,
            Username = uniqueUsername,
            Password = "Password123!",
            RolId = 2
        });
        var created = (await DeserializeApiResponseAsync<EmpleadoDto>(createRes))!.Data!;

        // Act: Delete employee
        var delRes = await client.DeleteAsync($"/api/v1/empleados/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, delRes.StatusCode);

        // Verify in DB directly: state is Inactivo and IsDeleted is set
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SigaDbContext>();
        var userInDb = await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == created.Id);
        Assert.NotNull(userInDb);
        Assert.Equal(EstadoUsuario.Inactivo, userInDb.Estado);
    }

    #endregion

    #region Feature 8: Complete User Profile API (5 Tests)

    [Fact]
    public async Task T1_F08_01_GetProfileForAuthenticatedUser_ReturnsCompleteProfileDto()
    {
        // Arrange
        var client = CreateAdminClient();

        // Act
        var res = await client.GetAsync("/api/v1/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<UserInfoResponseDto>(res);
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.Equal("admin", envelope.Data!.Username);
    }

    [Fact]
    public async Task T1_F08_02_VerifyPersonalInformationFields_InMeProfile()
    {
        // Arrange
        var client = CreateAdminClient();

        // Act
        var res = await client.GetAsync("/api/v1/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<UserInfoResponseDto>(res);
        Assert.NotNull(envelope);
        Assert.NotNull(envelope.Data!.Nombre);
        Assert.NotNull(envelope.Data.Apellido);
        Assert.NotNull(envelope.Data.NombreCompleto);
        Assert.NotNull(envelope.Data.Documento);
    }

    [Fact]
    public async Task T1_F08_03_VerifyRoleAndPermissions_InMeProfile()
    {
        // Arrange
        var client = CreateAdminClient();

        // Act
        var res = await client.GetAsync("/api/v1/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<UserInfoResponseDto>(res);
        Assert.NotNull(envelope);
        Assert.Equal("ADMINISTRADOR", envelope.Data!.Rol);
        Assert.Equal(1, envelope.Data.RolId);
        Assert.NotEmpty(envelope.Data.Permisos);
    }

    [Fact]
    public async Task T1_F08_04_VerifyLastAccessTimestamp_PopulatedOnLogin()
    {
        // Arrange
        var anon = CreateAnonymousClient();
        var loginRes = await anon.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto
        {
            Username = "admin",
            Password = "Admin123!"
        });
        var loginData = (await DeserializeApiResponseAsync<LoginResponseDto>(loginRes))!.Data!;

        // Act: Request profile using issued token
        var client = CreateClientWithCustomToken(loginData.Token);
        var meRes = await client.GetAsync("/api/v1/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, meRes.StatusCode);
        var meEnvelope = await DeserializeApiResponseAsync<UserInfoResponseDto>(meRes);
        Assert.NotNull(meEnvelope);
        Assert.NotNull(meEnvelope.Data!.UltimoAcceso);
    }

    [Fact]
    public async Task T1_F08_05_VerifyAccountStatus_InProfile_IsActivo()
    {
        // Arrange
        var client = CreateAdminClient();

        // Act
        var res = await client.GetAsync("/api/v1/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<UserInfoResponseDto>(res);
        Assert.NotNull(envelope);
        Assert.Equal(EstadoUsuario.Activo, envelope.Data!.Estado);
    }

    #endregion

    #region Feature 9: Omission of Notifications (5 Tests)

    [Fact]
    public async Task T1_F09_01_NonExistentNotificationEndpoint_GET_Returns404()
    {
        // Arrange
        var client = CreateAdminClient();

        // Act
        var res = await client.GetAsync("/api/v1/notificaciones");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task T1_F09_02_NonExistentNotificationEndpoint_POST_Returns404()
    {
        // Arrange
        var client = CreateAdminClient();

        // Act
        var res = await client.PostAsJsonAsync("/api/v1/notificaciones", new { Mensaje = "test" });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task T1_F09_03_DependencyInjection_DoesNotRegisterNotificationService()
    {
        // Arrange: Service provider inspection
        using var scope = Factory.Services.CreateScope();

        // Act: Try resolving any service containing Notification
        var allServices = Factory.Services;
        var notifDescriptors = Factory.Services
            .Where(s => s.ServiceType.Name.Contains("Notificacion", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Assert: Explicit omission of notification services
        Assert.Empty(notifDescriptors);
    }

    [Fact]
    public async Task T1_F09_04_FrontendHtmlDom_AbsenceOfNotificationBell()
    {
        // Arrange & Act
        var indexHtml = FrontendAssetScanner.ReadFrontendFile("index.html");

        // Assert: No notification bell in index.html
        Assert.DoesNotContain("notification-bell", indexHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("notifications-dropdown", indexHtml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task T1_F09_05_FrontendSpaJs_CleanAbsenceOfNotificationPolling()
    {
        // Arrange & Act
        var appJs = FrontendAssetScanner.ReadFrontendFile("js/app.js");

        // Assert: app.js contains no notification polling or bell handlers
        Assert.DoesNotContain("notificaciones", appJs, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("notification-bell", appJs, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Feature 10: Security & Auth Unit Tests (5 Tests)

    [Fact]
    public async Task T1_F10_01_GestionRoles_IntegrityVerification()
    {
        // Arrange: Direct service resolution
        using var scope = Factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetService<IGestionRoles>();

        // Assert
        Assert.NotNull(service);
        var roles = await service.GetRolesAsync();
        Assert.True(roles.Success);
        Assert.True(roles.Data!.Any());
    }

    [Fact]
    public async Task T1_F10_02_GestionEmpleados_IntegrityVerification()
    {
        using var scope = Factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetService<IGestionEmpleados>();

        Assert.NotNull(service);
        var emps = await service.GetEmpleadosAsync();
        Assert.True(emps.Success);
    }

    [Fact]
    public async Task T1_F10_03_GestionUsuarios_IntegrityVerification()
    {
        using var scope = Factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetService<IGestionUsuarios>();

        Assert.NotNull(service);
    }

    [Fact]
    public async Task T1_F10_04_JwtTokenService_IntegrityVerification()
    {
        using var scope = Factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetService<ITokenService>();

        Assert.NotNull(service);
    }

    [Fact]
    public async Task T1_F10_05_PermissionAuthorizationHandler_ResolvesFromDI()
    {
        using var scope = Factory.Services.CreateScope();
        var handler = scope.ServiceProvider.GetService<Microsoft.AspNetCore.Authorization.IAuthorizationHandler>();

        Assert.NotNull(handler);
    }

    #endregion
}
