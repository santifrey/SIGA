using System.Security.Claims;
using SIGA.Domain.Entities;

namespace SIGA.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(Usuario usuario, IEnumerable<string>? permisos = null);
    string GenerateToken(Usuario usuario, string? rol, IEnumerable<string>? permisos);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
