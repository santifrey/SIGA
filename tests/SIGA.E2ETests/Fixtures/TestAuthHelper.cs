using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SIGA.E2ETests.Common;

namespace SIGA.E2ETests.Fixtures;

public static class TestAuthHelper
{
    public const string TestSecretKey = "SIGA_E2E_Test_Secret_Key_At_Least_32_Chars_Long!";
    public const string TestIssuer = "SIGA.API";
    public const string TestAudience = "SIGA.Frontend";

    public static string GenerateToken(
        int userId = 1,
        string username = "admin",
        string role = "ADMINISTRADOR",
        IEnumerable<string>? permissions = null,
        TimeSpan? lifetime = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, role),
            new("role", role)
        };

        if (permissions != null)
        {
            foreach (var perm in permissions)
            {
                claims.Add(new Claim("permiso", perm));
            }
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromHours(24));

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static async Task<string?> LoginAndGetTokenAsync(ApiTestClient client, string username = "admin", string password = "Admin123!")
    {
        var (response, body) = await client.PostJsonAsync<LoginRequestDto, LoginResponseDto>(
            "/api/v1/auth/login",
            new LoginRequestDto { Username = username, Password = password });

        if (response.IsSuccessStatusCode && body?.Data != null)
        {
            return body.Data.Token;
        }

        return null;
    }
}
