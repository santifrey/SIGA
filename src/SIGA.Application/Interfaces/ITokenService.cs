using System.Security.Claims;
using SIGA.Domain.Entities;

namespace SIGA.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(Usuario usuario, IEnumerable<string> roles);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
