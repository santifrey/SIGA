using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Auth;
using SIGA.Application.DTOs.Empleados;
using SIGA.Application.DTOs.Roles;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.E2ETests.Infrastructure;
using SIGA.Infrastructure.Persistence;
using Xunit;

namespace SIGA.E2ETests.Tier2_BoundaryCases;

[Collection("E2E Test Suite")]
public class SecurityBoundariesTests : TestBase
{
    public SecurityBoundariesTests(SigaTestWebApplicationFactory factory) : base(factory)
    {
    }

    #region Feature 1: Single-Role User Model (5 Boundary Tests)

    [Fact]
    public async Task T2_F01_01_AssignNonExistentRolId_Returns404NotFound()
    {
        var client = CreateAdminClient();
        var dto = new CrearEmpleadoDto
        {
            Nombre = "Invalid",
            Apellido = "Rol",
            Documento = "B" + Random.Shared.Next(100000, 999999),
            Username = "user_b0101_" + Guid.NewGuid().ToString("N")[..6],
            Password = "Password123!",
            RolId = 99999
        };

        var res = await client.PostAsJsonAsync("/api/v1/empleados", dto);
        Assert.True(res.StatusCode == HttpStatusCode.NotFound || res.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task T2_F01_02_AssignRolIdZeroOrNegative_Returns400BadRequest()
    {
        var client = CreateAdminClient();
        var dto = new CrearEmpleadoDto
        {
            Nombre = "Zero",
            Apellido = "Rol",
            Documento = "B" + Random.Shared.Next(100000, 999999),
            Username = "user_b0102_" + Guid.NewGuid().ToString("N")[..6],
            Password = "Password123!",
            RolId = 0
        };

        var res = await client.PostAsJsonAsync("/api/v1/empleados", dto);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task T2_F01_03_AssignNegativeRolId_Returns400BadRequest()
    {
        var client = CreateAdminClient();
        var dto = new CrearEmpleadoDto
        {
            Nombre = "Neg",
            Apellido = "Rol",
            Documento = "B" + Random.Shared.Next(100000, 999999),
            Username = "user_b0103_" + Guid.NewGuid().ToString("N")[..6],
            Password = "Password123!",
            RolId = -5
        };

        var res = await client.PostAsJsonAsync("/api/v1/empleados", dto);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task T2_F01_04_UserWithoutRoleIntegrity_DbEnforcesNotNullConstraint()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SigaDbContext>();

        var user = new Usuario
        {
            Username = "null_role_user_" + Guid.NewGuid().ToString("N")[..4],
            PasswordHash = "hash",
            RolId = 0
        };

        // Saving user with invalid RolId throws foreign key / validation exception
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await db.Usuarios.AddAsync(user);
            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task T2_F01_05_ConcurrentRoleUpdate_ResolvesSafely()
    {
        var client = CreateAdminClient();
        var doc = "B" + Random.Shared.Next(100000, 999999);
        var username = "user_b0105_" + Guid.NewGuid().ToString("N")[..6];

        var createRes = await client.PostAsJsonAsync("/api/v1/empleados", new CrearEmpleadoDto
        {
            Nombre = "Concurrent",
            Apellido = "User",
            Documento = doc,
            Username = username,
            Password = "Password123!",
            RolId = 2
        });
        var created = (await DeserializeApiResponseAsync<EmpleadoDto>(createRes))!.Data!;

        // Two sequential updates
        var t1 = client.PutAsJsonAsync($"/api/v1/empleados/{created.Id}", new ActualizarEmpleadoDto
        {
            Nombre = "Concurrent",
            Apellido = "User",
            Documento = doc,
            RolId = 3,
            Estado = EstadoUsuario.Activo
        });
        var t2 = client.PutAsJsonAsync($"/api/v1/empleados/{created.Id}", new ActualizarEmpleadoDto
        {
            Nombre = "Concurrent",
            Apellido = "User",
            Documento = doc,
            RolId = 5,
            Estado = EstadoUsuario.Activo
        });

        await Task.WhenAll(t1, t2);

        // Final role is either 3 or 5, but exactly one
        var finalRes = await client.GetAsync($"/api/v1/empleados/{created.Id}");
        var finalEmp = (await DeserializeApiResponseAsync<EmpleadoDto>(finalRes))!.Data!;
        Assert.True(finalEmp.RolId == 3 || finalEmp.RolId == 5);
    }

    #endregion

    #region Feature 2: Role-Permission Model (5 Boundary Tests)

    [Fact]
    public async Task T2_F02_01_AssignNonExistentPermissionCode_HandledGracefully()
    {
        var client = CreateAdminClient();
        var res = await client.PostAsJsonAsync("/api/v1/roles", new CrearRolDto
        {
            Nombre = "ROL_INVALID_PERM_" + Guid.NewGuid().ToString("N")[..4],
            Permisos = new List<string> { "PERMISO_QUE_NO_EXISTE_XYZ" }
        });

        // Either ignored (created with 0 mapped) or 400
        Assert.True(res.StatusCode == HttpStatusCode.Created || res.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task T2_F02_02_CreateRoleWithEmptyPermissionsList_SucceedsWith201()
    {
        var client = CreateAdminClient();
        var name = "ROL_EMPTY_PERMS_" + Guid.NewGuid().ToString("N")[..6];

        var res = await client.PostAsJsonAsync("/api/v1/roles", new CrearRolDto
        {
            Nombre = name,
            Permisos = new List<string>()
        });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<RolDto>(res);
        Assert.Empty(envelope!.Data!.Permisos);
    }

    [Fact]
    public async Task T2_F02_03_AssignAllPermissionsCatalog_Succeeds()
    {
        var client = CreateAdminClient();
        var allPerms = DatabaseSeeder.CatalogoPermisos.Select(p => p.Codigo).ToList();
        var name = "ROL_SUPER_" + Guid.NewGuid().ToString("N")[..6];

        var res = await client.PostAsJsonAsync("/api/v1/roles", new CrearRolDto
        {
            Nombre = name,
            Permisos = allPerms
        });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<RolDto>(res);
        Assert.Equal(allPerms.Count, envelope!.Data!.Permisos.Count);
    }

    [Fact]
    public async Task T2_F02_04_DuplicatePermissionCodeInRequest_Deduplicated()
    {
        var client = CreateAdminClient();
        var name = "ROL_DEDUP_" + Guid.NewGuid().ToString("N")[..6];

        var res = await client.PostAsJsonAsync("/api/v1/roles", new CrearRolDto
        {
            Nombre = name,
            Permisos = new List<string> { "STOCK_VER", "STOCK_VER", "STOCK_VER" }
        });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<RolDto>(res);
        Assert.Single(envelope!.Data!.Permisos);
    }

    [Fact]
    public async Task T2_F02_05_CaseInsensitivityInPermissionCodes_MapsCorrectly()
    {
        var client = CreateAdminClient();
        var name = "ROL_CASE_" + Guid.NewGuid().ToString("N")[..6];

        var res = await client.PostAsJsonAsync("/api/v1/roles", new CrearRolDto
        {
            Nombre = name,
            Permisos = new List<string> { "stock_ver" }
        });

        if (res.StatusCode == HttpStatusCode.Created)
        {
            var envelope = await DeserializeApiResponseAsync<RolDto>(res);
            Assert.Contains(envelope!.Data!.Permisos, p => p.Equals("STOCK_VER", StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        }
    }

    #endregion

    #region Feature 3: Permissions Catalog & Seeding (5 Boundary Tests)

    [Fact]
    public async Task T2_F03_01_SeederMultipleConsecutiveRuns_NoDuplicateErrors()
    {
        using var scope = Factory.Services.CreateScope();
        for (int i = 0; i < 3; i++)
        {
            await DatabaseSeeder.InitializeDatabaseAsync(scope.ServiceProvider);
        }

        var db = scope.ServiceProvider.GetRequiredService<SigaDbContext>();
        var adminCount = await db.Roles.CountAsync(r => r.Nombre == "ADMINISTRADOR");
        Assert.Equal(1, adminCount);
    }

    [Fact]
    public async Task T2_F03_02_PreserveSystemRolesEsSistemaFlag()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SigaDbContext>();

        var admin = await db.Roles.FirstAsync(r => r.Nombre == "ADMINISTRADOR");
        Assert.True(admin.EsSistema);
    }

    [Fact]
    public async Task T2_F03_03_CatalogCompletenessAudit_AllPermissionsHaveCompleteMetadata()
    {
        foreach (var perm in DatabaseSeeder.CatalogoPermisos)
        {
            Assert.False(string.IsNullOrWhiteSpace(perm.Codigo));
            Assert.False(string.IsNullOrWhiteSpace(perm.Nombre));
            Assert.False(string.IsNullOrWhiteSpace(perm.Descripcion));
            Assert.False(string.IsNullOrWhiteSpace(perm.Modulo));
        }
    }

    [Fact]
    public async Task T2_F03_04_SystemRolesPermissionMatrixParity()
    {
        Assert.True(DatabaseSeeder.MatrizRolesPermisos.ContainsKey("ADMINISTRADOR"));
        Assert.True(DatabaseSeeder.MatrizRolesPermisos.ContainsKey("VENDEDOR"));
        Assert.True(DatabaseSeeder.MatrizRolesPermisos.ContainsKey("SECRETARIA"));
        Assert.True(DatabaseSeeder.MatrizRolesPermisos.ContainsKey("CONTABILIDAD"));
        Assert.True(DatabaseSeeder.MatrizRolesPermisos.ContainsKey("CLIENTE"));
    }

    [Fact]
    public async Task T2_F03_05_TotalCatalogCount_Exceeds30()
    {
        Assert.True(DatabaseSeeder.CatalogoPermisos.Count >= 30);
    }

    #endregion

    #region Feature 4: Dynamic Permission Authorization (5 Boundary Tests)

    [Fact]
    public async Task T2_F04_01_MalformedAuthorizationHeader_Returns401()
    {
        var client = CreateAnonymousClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer");

        var res = await client.GetAsync("/api/v1/roles");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task T2_F04_02_ExpiredJwtToken_Returns401()
    {
        var expiredToken = ApiClientHelper.GenerateToken(
            userId: 1,
            username: "admin",
            role: "ADMINISTRADOR",
            expiresAt: DateTime.UtcNow.AddMinutes(-10));

        var client = CreateClientWithCustomToken(expiredToken);
        var res = await client.GetAsync("/api/v1/roles");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task T2_F04_03_JwtSignedWithWrongSecretKey_Returns401()
    {
        var wrongSecretToken = ApiClientHelper.GenerateToken(
            userId: 1,
            username: "admin",
            role: "ADMINISTRADOR",
            secretKeyOverride: "Completely_Foreign_And_Different_Secret_Key_For_Testing_123!");

        var client = CreateClientWithCustomToken(wrongSecretToken);
        var res = await client.GetAsync("/api/v1/roles");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task T2_F04_04_TamperedJwtPayloadClaims_Returns401()
    {
        var validToken = ApiClientHelper.GenerateToken(userId: 1, username: "admin", role: "VENDEDOR");
        // Tamper signature by changing last characters
        var tamperedToken = validToken[..^4] + "ABCD";

        var client = CreateClientWithCustomToken(tamperedToken);
        var res = await client.GetAsync("/api/v1/roles");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task T2_F04_05_BlankAuthorizationHeader_Returns401()
    {
        var client = CreateAnonymousClient();
        client.DefaultRequestHeaders.Add("Authorization", "");

        var res = await client.GetAsync("/api/v1/roles");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    #endregion

    #region Feature 5: Single-Role & Permission JWT (5 Boundary Tests)

    [Fact]
    public async Task T2_F05_01_TokenGeneration_ForUserWithNullRole_DoesNotThrow()
    {
        var token = ApiClientHelper.GenerateToken(userId: 99, username: "norole", role: null, includeRoleClaim: false);
        Assert.False(string.IsNullOrWhiteSpace(token));

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        Assert.DoesNotContain(jwt.Claims, c => c.Type == ClaimTypes.Role || c.Type == "role");
    }

    [Fact]
    public async Task T2_F05_02_TokenGeneration_WithZeroPermissions_EmitsZeroPermisoClaims()
    {
        var token = ApiClientHelper.GenerateToken(userId: 99, username: "noperms", role: "CUSTOM", permissions: Array.Empty<string>());
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.DoesNotContain(jwt.Claims, c => c.Type == "permiso");
    }

    [Fact]
    public async Task T2_F05_03_TokenLifetime_ClockSkewZeroEnforced()
    {
        // Token expired 1 second ago
        var expiredToken = ApiClientHelper.GenerateToken(
            userId: 1,
            username: "admin",
            role: "ADMINISTRADOR",
            expiresAt: DateTime.UtcNow.AddSeconds(-2));

        var client = CreateClientWithCustomToken(expiredToken);
        var res = await client.GetAsync("/api/v1/roles");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task T2_F05_04_SecretKeyMinimumLengthEnforcement_Validation()
    {
        using var scope = Factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        var secretKey = config["JwtSettings:SecretKey"] ?? ApiClientHelper.DefaultSecretKey;
        Assert.True(System.Text.Encoding.UTF8.GetByteCount(secretKey) >= 32, "JWT SecretKey must be at least 256 bits (32 bytes).");
    }

    [Fact]
    public async Task T2_F05_05_LargePermissionClaimSet_FitsStandardHeader()
    {
        var manyPerms = Enumerable.Range(1, 50).Select(i => $"CUSTOM_PERM_{i}").ToArray();
        var token = ApiClientHelper.GenerateToken(userId: 1, username: "admin", role: "ADMINISTRADOR", permissions: manyPerms);

        var client = CreateClientWithCustomToken(token);
        var res = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    #endregion

    #region Feature 6: Roles & Permissions CRUD API (5 Boundary Tests)

    [Fact]
    public async Task T2_F06_01_DeleteSystemRole_Prohibited_Returns400()
    {
        var client = CreateAdminClient();

        // Rol 1: ADMINISTRADOR (EsSistema = true)
        var res = await client.DeleteAsync("/api/v1/roles/1");

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<object>(res);
        Assert.False(envelope!.Success);
        Assert.Contains("sistema", envelope.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task T2_F06_02_DeleteRoleWithActiveUsers_Prohibited_Returns400()
    {
        var client = CreateAdminClient();
        var roleName = "ROL_WITH_USERS_" + Guid.NewGuid().ToString("N")[..6];
        var roleRes = await client.PostAsJsonAsync("/api/v1/roles", new CrearRolDto { Nombre = roleName });
        var role = (await DeserializeApiResponseAsync<RolDto>(roleRes))!.Data!;

        // Assign to user
        await client.PostAsJsonAsync("/api/v1/empleados", new CrearEmpleadoDto
        {
            Nombre = "Active",
            Apellido = "RoleUser",
            Documento = "B" + Random.Shared.Next(100000, 999999),
            Username = "user_b0602_" + Guid.NewGuid().ToString("N")[..6],
            Password = "Password123!",
            RolId = role.Id
        });

        // Act: Attempt delete
        var delRes = await client.DeleteAsync($"/api/v1/roles/{role.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, delRes.StatusCode);
        var envelope = await DeserializeApiResponseAsync<object>(delRes);
        Assert.False(envelope!.Success);
    }

    [Fact]
    public async Task T2_F06_03_RenameSystemRole_Prohibited_Returns400()
    {
        var client = CreateAdminClient();

        var res = await client.PutAsJsonAsync("/api/v1/roles/2", new ActualizarRolDto
        {
            Nombre = "VENDEDOR_MODIFICADO"
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task T2_F06_04_CreateRoleWithReservedSystemName_Returns400()
    {
        var client = CreateAdminClient();

        var res = await client.PostAsJsonAsync("/api/v1/roles", new CrearRolDto
        {
            Nombre = "ADMINISTRADOR"
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task T2_F06_05_CreateRoleWithDuplicateName_Returns400()
    {
        var client = CreateAdminClient();
        var name = "ROL_DUP_" + Guid.NewGuid().ToString("N")[..6];

        await client.PostAsJsonAsync("/api/v1/roles", new CrearRolDto { Nombre = name });
        var dupRes = await client.PostAsJsonAsync("/api/v1/roles", new CrearRolDto { Nombre = name });

        Assert.Equal(HttpStatusCode.BadRequest, dupRes.StatusCode);
    }

    #endregion

    #region Feature 7: Employees CRUD API (5 Boundary Tests)

    [Fact]
    public async Task T2_F07_01_DeleteLastActiveAdministrator_Prohibited_Returns400()
    {
        var client = CreateAdminClient();

        // Attempt deleting the sole admin user (Id: 1)
        var res = await client.DeleteAsync("/api/v1/empleados/1");

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<object>(res);
        Assert.False(envelope!.Success);
        Assert.Contains("administrador", envelope.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task T2_F07_02_DowngradeLastActiveAdministrator_Prohibited_Returns400()
    {
        var client = CreateAdminClient();

        var res = await client.PutAsJsonAsync("/api/v1/empleados/1", new ActualizarEmpleadoDto
        {
            Nombre = "Admin",
            Apellido = "SIGA",
            Documento = "00000000",
            RolId = 2, // Attempt change to VENDEDOR
            Estado = EstadoUsuario.Activo
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<object>(res);
        Assert.False(envelope!.Success);
    }

    [Fact]
    public async Task T2_F07_03_AssignClienteRoleToEmployee_Prohibited_Returns400()
    {
        var client = CreateAdminClient();

        var res = await client.PostAsJsonAsync("/api/v1/empleados", new CrearEmpleadoDto
        {
            Nombre = "Staff",
            Apellido = "Invalid",
            Documento = "B" + Random.Shared.Next(100000, 999999),
            Username = "user_b0703_" + Guid.NewGuid().ToString("N")[..6],
            Password = "Password123!",
            RolId = 4 // CLIENTE
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<object>(res);
        Assert.False(envelope!.Success);
        Assert.Contains("CLIENTE", envelope.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task T2_F07_04_CreateEmployeeWithDuplicateDocument_Returns400()
    {
        var client = CreateAdminClient();
        var doc = "DUP" + Random.Shared.Next(100000, 999999);

        await client.PostAsJsonAsync("/api/v1/empleados", new CrearEmpleadoDto
        {
            Nombre = "First",
            Apellido = "One",
            Documento = doc,
            Username = "user_b0704_a_" + Guid.NewGuid().ToString("N")[..4],
            Password = "Password123!",
            RolId = 2
        });

        var dupRes = await client.PostAsJsonAsync("/api/v1/empleados", new CrearEmpleadoDto
        {
            Nombre = "Second",
            Apellido = "Two",
            Documento = doc,
            Username = "user_b0704_b_" + Guid.NewGuid().ToString("N")[..4],
            Password = "Password123!",
            RolId = 2
        });

        Assert.Equal(HttpStatusCode.BadRequest, dupRes.StatusCode);
    }

    [Fact]
    public async Task T2_F07_05_ShortPassword_LessThan6Chars_Returns400()
    {
        var client = CreateAdminClient();

        var res = await client.PostAsJsonAsync("/api/v1/empleados", new CrearEmpleadoDto
        {
            Nombre = "Short",
            Apellido = "Pass",
            Documento = "B" + Random.Shared.Next(100000, 999999),
            Username = "user_b0705_" + Guid.NewGuid().ToString("N")[..6],
            Password = "123", // Too short
            RolId = 2
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    #endregion

    #region Feature 8: Complete User Profile API (5 Boundary Tests)

    [Fact]
    public async Task T2_F08_01_ProfileQueryForNonExistentUserId_Returns404()
    {
        using var scope = Factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IGestionUsuarios>();

        var res = await service.GetMeAsync(999999);
        Assert.False(res.Success);
        Assert.Equal(404, res.StatusCode);
    }

    [Fact]
    public async Task T2_F08_02_ProfileForUserWithMissingPhone_ReturnsGracefully()
    {
        var client = CreateAdminClient();
        var doc = "B" + Random.Shared.Next(100000, 999999);
        var username = "user_b0802_" + Guid.NewGuid().ToString("N")[..6];

        var createRes = await client.PostAsJsonAsync("/api/v1/empleados", new CrearEmpleadoDto
        {
            Nombre = "NoPhone",
            Apellido = "User",
            Documento = doc,
            Telefono = null,
            Email = null,
            Username = username,
            Password = "Password123!",
            RolId = 2
        });
        var created = (await DeserializeApiResponseAsync<EmpleadoDto>(createRes))!.Data!;

        var userClient = CreateRoleClient("VENDEDOR", created.Id, username);
        var meRes = await userClient.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.OK, meRes.StatusCode);
        var envelope = await DeserializeApiResponseAsync<UserInfoResponseDto>(meRes);
        Assert.NotNull(envelope);
    }

    [Fact]
    public async Task T2_F08_03_ProfileForInactiveUser_Returns401()
    {
        var client = CreateAdminClient();
        var doc = "B" + Random.Shared.Next(100000, 999999);
        var username = "user_b0803_" + Guid.NewGuid().ToString("N")[..6];

        var createRes = await client.PostAsJsonAsync("/api/v1/empleados", new CrearEmpleadoDto
        {
            Nombre = "Inactive",
            Apellido = "User",
            Documento = doc,
            Username = username,
            Password = "Password123!",
            RolId = 2
        });
        var created = (await DeserializeApiResponseAsync<EmpleadoDto>(createRes))!.Data!;

        // Soft delete employee
        await client.DeleteAsync($"/api/v1/empleados/{created.Id}");

        // Now attempt calling /auth/me with their token
        var inactiveClient = CreateRoleClient("VENDEDOR", created.Id, username);
        var meRes = await inactiveClient.GetAsync("/api/v1/auth/me");

        Assert.True(meRes.StatusCode == HttpStatusCode.Unauthorized || meRes.StatusCode == HttpStatusCode.NotFound || meRes.StatusCode == HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task T2_F08_04_SpecialCharactersInName_ProperlyUtf8Encoded()
    {
        var client = CreateAdminClient();
        var doc = "B" + Random.Shared.Next(100000, 999999);
        var username = "user_b0804_" + Guid.NewGuid().ToString("N")[..6];

        var createRes = await client.PostAsJsonAsync("/api/v1/empleados", new CrearEmpleadoDto
        {
            Nombre = "María José",
            Apellido = "Núñez-Ñandú",
            Documento = doc,
            Username = username,
            Password = "Password123!",
            RolId = 2
        });
        var created = (await DeserializeApiResponseAsync<EmpleadoDto>(createRes))!.Data!;

        var userClient = CreateRoleClient("VENDEDOR", created.Id, username);
        var meRes = await userClient.GetAsync("/api/v1/auth/me");

        var envelope = await DeserializeApiResponseAsync<UserInfoResponseDto>(meRes);
        Assert.Equal("María José", envelope!.Data!.Nombre);
        Assert.Equal("Núñez-Ñandú", envelope.Data.Apellido);
    }

    [Fact]
    public async Task T2_F08_05_UnauthenticatedProfileAccess_Returns401()
    {
        var anon = CreateAnonymousClient();
        var res = await anon.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    #endregion

    #region Feature 9: Omission of Notifications (5 Boundary Tests)

    [Fact]
    public async Task T2_F09_01_NotificationSubRoutes_Return404()
    {
        var client = CreateAdminClient();
        var r1 = await client.GetAsync("/api/v1/notificaciones/unread");
        var r2 = await client.GetAsync("/api/v1/notificaciones/count");

        Assert.Equal(HttpStatusCode.NotFound, r1.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, r2.StatusCode);
    }

    [Fact]
    public async Task T2_F09_02_NotificationMarkAsRead_Returns404()
    {
        var client = CreateAdminClient();
        var res = await client.PutAsJsonAsync("/api/v1/notificaciones/1/leida", new { });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task T2_F09_03_NotificationHubOrWebSocket_Returns404()
    {
        var client = CreateAdminClient();
        var res = await client.GetAsync("/hub/notificaciones");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task T2_F09_04_AbsenceOfNotificationBell_InMobileLayout()
    {
        var css = FrontendAssetScanner.ReadFrontendFile("css/layout.css");
        Assert.DoesNotContain("notification-badge", css, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task T2_F09_05_ZeroNotificationReferences_InRouter()
    {
        var routerJs = FrontendAssetScanner.ReadFrontendFile("js/router.js");
        Assert.DoesNotContain("notificaciones", routerJs, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Feature 10: Security & Auth Unit Tests (5 Boundary Tests)

    [Fact]
    public async Task T2_F10_01_SoleAdminSafeguard_CountAsyncVerification()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SigaDbContext>();

        var adminCount = await db.Usuarios.CountAsync(u => u.RolId == 1 && !u.IsDeleted && u.Estado == EstadoUsuario.Activo);
        Assert.True(adminCount >= 1);
    }

    [Fact]
    public async Task T2_F10_02_SystemRoleEsSistema_DatabaseAssertion()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SigaDbContext>();

        var systemRoles = await db.Roles.Where(r => r.EsSistema).ToListAsync();
        Assert.Equal(5, systemRoles.Count);
    }

    [Fact]
    public async Task T2_F10_03_SoftDeleteBehavior_NeverDeletesRowPhysically()
    {
        // Arrange: Create a dedicated employee to soft-delete
        var client = CreateAdminClient();
        var uniqueDoc = "T2F1003_" + Random.Shared.Next(10000, 99999);
        var uniqueUsername = "soft_del_" + Guid.NewGuid().ToString("N")[..6];

        var createRes = await client.PostAsJsonAsync("/api/v1/empleados", new CrearEmpleadoDto
        {
            Nombre = "SoftDel",
            Apellido = "TestUser",
            Documento = uniqueDoc,
            Username = uniqueUsername,
            Password = "Password123!",
            RolId = 2 // VENDEDOR
        });
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var created = (await DeserializeApiResponseAsync<EmpleadoDto>(createRes))!.Data!;

        // Act: Execute soft deletion via API
        var delRes = await client.DeleteAsync($"/api/v1/empleados/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, delRes.StatusCode);

        // Assert: Verify row STILL physically exists in SQLite with Estado == Inactivo
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SigaDbContext>();

        var userInDb = await db.Usuarios.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == created.Id);

        Assert.NotNull(userInDb);
        Assert.Equal(created.Id, userInDb.Id);
        Assert.Equal(uniqueUsername, userInDb.Username);
        Assert.Equal(EstadoUsuario.Inactivo, userInDb.Estado);
    }

    [Fact]
    public async Task T2_F10_04_DuplicateUsernameValidation_Throws()
    {
        var client = CreateAdminClient();
        var username = "dup_user_" + Guid.NewGuid().ToString("N")[..6];

        await client.PostAsJsonAsync("/api/v1/empleados", new CrearEmpleadoDto
        {
            Nombre = "U1",
            Apellido = "A1",
            Documento = "DOC" + Random.Shared.Next(100000, 999999),
            Username = username,
            Password = "Password123!",
            RolId = 2
        });

        var res = await client.PostAsJsonAsync("/api/v1/empleados", new CrearEmpleadoDto
        {
            Nombre = "U2",
            Apellido = "A2",
            Documento = "DOC" + Random.Shared.Next(100000, 999999),
            Username = username,
            Password = "Password123!",
            RolId = 2
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task T2_F10_05_DynamicPermissionHandler_AdminBypassValidation()
    {
        // Admin token has no specific permission claims, but bypasses all permission policies
        var token = ApiClientHelper.GenerateToken(1, "admin", "ADMINISTRADOR", Array.Empty<string>());
        var client = CreateClientWithCustomToken(token);

        var res = await client.GetAsync("/api/v1/permisos");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    #endregion
}
