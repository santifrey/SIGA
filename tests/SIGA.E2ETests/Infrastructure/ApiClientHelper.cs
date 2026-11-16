using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;
using SIGA.Infrastructure.Persistence;

namespace SIGA.E2ETests.Infrastructure;

public static class ApiClientHelper
{
    public const string DefaultSecretKey = "SIGA_Local_Development_Only_Secret_Key_2026_Azafran_Mediterraneo!";
    public const string DefaultIssuer = "SIGA.API";
    public const string DefaultAudience = "SIGA.Frontend";

    public static string GenerateToken(
        int userId = 1,
        string username = "admin",
        string? role = "ADMINISTRADOR",
        IEnumerable<string>? permissions = null,
        DateTime? expiresAt = null,
        string? secretKeyOverride = null,
        bool includeRoleClaim = true)
    {
        var secret = secretKeyOverride ?? DefaultSecretKey;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (includeRoleClaim && !string.IsNullOrWhiteSpace(role))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        if (permissions != null)
        {
            foreach (var perm in permissions)
            {
                if (!string.IsNullOrWhiteSpace(perm))
                {
                    claims.Add(new Claim("permiso", perm));
                }
            }
        }
        else if (role != null && DatabaseSeeder.MatrizRolesPermisos.TryGetValue(role, out var defaultPerms))
        {
            foreach (var perm in defaultPerms)
            {
                claims.Add(new Claim("permiso", perm));
            }
        }

        var expires = expiresAt ?? DateTime.UtcNow.AddHours(24);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = DefaultIssuer,
            Audience = DefaultAudience,
            SigningCredentials = creds
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }

    public static HttpClient CreateAdminClient(WebApplicationFactory<Program> factory)
    {
        var token = GenerateToken(
            userId: 1,
            username: "admin",
            role: "ADMINISTRADOR",
            permissions: DatabaseSeeder.CatalogoPermisos.Select(p => p.Codigo));

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static HttpClient CreateRoleClient(WebApplicationFactory<Program> factory, string role, int userId = 2, string username = "testuser")
    {
        var perms = DatabaseSeeder.MatrizRolesPermisos.TryGetValue(role, out var p) ? p : Array.Empty<string>();
        var token = GenerateToken(userId: userId, username: username, role: role, permissions: perms);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static HttpClient CreatePermissionsClient(WebApplicationFactory<Program> factory, params string[] permissions)
    {
        var token = GenerateToken(userId: 10, username: "custom_perm_user", role: "CUSTOM_ROLE", permissions: permissions);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static HttpClient CreateAnonymousClient(WebApplicationFactory<Program> factory)
    {
        return factory.CreateClient();
    }

    public static HttpClient CreateClientWithCustomToken(WebApplicationFactory<Program> factory, string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
