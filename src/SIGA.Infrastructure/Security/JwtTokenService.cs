using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SIGA.Application.Interfaces;
using SIGA.Domain.Entities;

namespace SIGA.Infrastructure.Security;

public class JwtTokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(Usuario usuario, IEnumerable<string>? permisos = null)
    {
        var rolNombre = usuario.Rol?.Nombre;
        return GenerateToken(usuario, rolNombre, permisos);
    }

    public string GenerateToken(Usuario usuario, string? rol, IEnumerable<string>? permisos)
    {
        var secretKey = GetValidatedSecretKey();
        var issuer = _configuration["JwtSettings:Issuer"] ?? "SIGA.API";
        var audience = _configuration["JwtSettings:Audience"] ?? "SIGA.Frontend";
        var expirationHoursStr = _configuration["JwtSettings:ExpirationHours"] ?? "24";

        if (!double.TryParse(expirationHoursStr, out var expirationHours))
        {
            expirationHours = 24;
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Username),
            new("PersonaId", usuario.PersonaId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (usuario.Persona != null)
        {
            if (!string.IsNullOrEmpty(usuario.Persona.Email))
                claims.Add(new Claim(ClaimTypes.Email, usuario.Persona.Email));

            claims.Add(new Claim("NombreCompleto", usuario.Persona.NombreCompleto));
        }

        // 1. Emitir claim de rol único
        var effectiveRole = !string.IsNullOrWhiteSpace(rol) ? rol : usuario.Rol?.Nombre;
        if (!string.IsNullOrWhiteSpace(effectiveRole))
        {
            claims.Add(new Claim(ClaimTypes.Role, effectiveRole));
        }

        // 2. Emitir claims individuales de permiso
        if (permisos != null)
        {
            foreach (var permiso in permisos.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                claims.Add(new Claim("permiso", permiso));
            }
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(expirationHours),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var secretKey = GetValidatedSecretKey();

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Token inválido");
        }

        return principal;
    }

    private string GetValidatedSecretKey()
    {
        var secretKey = _configuration["JwtSettings:SecretKey"]
            ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY");

        if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < 32)
        {
            throw new InvalidOperationException(
                "La clave secreta de JWT ('JwtSettings:SecretKey' o variable de entorno 'JwtSettings__SecretKey') no está configurada o tiene menos de 32 caracteres.");
        }

        return secretKey;
    }
}
