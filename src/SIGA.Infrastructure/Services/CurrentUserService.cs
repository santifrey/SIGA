using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SIGA.Application.Interfaces;
using SIGA.Domain.Common;
using SIGA.Domain.Enums;
using SIGA.Infrastructure.Persistence;

namespace SIGA.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SigaDbContext _dbContext;

    private int? _cachedPersonaId;
    private int? _cachedSucursalId;
    private bool _userDataLoaded;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, SigaDbContext dbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public int UsuarioId
    {
        get
        {
            var claim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }

    public string Username => User?.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;

    public bool EsAdministrador
    {
        get
        {
            if (User == null) return false;
            return User.IsInRole("ADMINISTRADOR") ||
                   User.HasClaim(c => (c.Type == ClaimTypes.Role || c.Type == "role") &&
                                      c.Value.Equals("ADMINISTRADOR", StringComparison.OrdinalIgnoreCase));
        }
    }

    public async Task<int?> GetPersonaIdAsync(CancellationToken cancellationToken = default)
    {
        await EnsureUserDataLoadedAsync(cancellationToken);
        return _cachedPersonaId;
    }

    public async Task<int?> GetSucursalIdAsync(CancellationToken cancellationToken = default)
    {
        await EnsureUserDataLoadedAsync(cancellationToken);
        return _cachedSucursalId;
    }

    private async Task EnsureUserDataLoadedAsync(CancellationToken cancellationToken)
    {
        if (_userDataLoaded) return;

        var userId = UsuarioId;
        if (userId > 0)
        {
            var data = await _dbContext.Usuarios
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.PersonaId, SucursalId = u.Persona != null ? u.Persona.SucursalId : (int?)null })
                .FirstOrDefaultAsync(cancellationToken);

            if (data != null)
            {
                _cachedPersonaId = data.PersonaId;
                _cachedSucursalId = data.SucursalId;
            }
        }

        _userDataLoaded = true;
    }

    public Task<AlcancePermiso> ObtenerAlcanceAsync(string permisoBase, CancellationToken cancellationToken = default)
    {
        if (EsAdministrador)
            return Task.FromResult(AlcancePermiso.Todos);

        var permissions = User?.Claims
            .Where(c => c.Type.Equals("permiso", StringComparison.OrdinalIgnoreCase) ||
                        c.Type.Equals("permission", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Value);

        return Task.FromResult(PermisoHierarchy.ObtenerAlcance(permissions ?? Enumerable.Empty<string>(), permisoBase));
    }

    public Task<bool> TienePermisoAsync(string permiso, CancellationToken cancellationToken = default)
    {
        if (EsAdministrador)
            return Task.FromResult(true);

        var permissions = User?.Claims
            .Where(c => c.Type.Equals("permiso", StringComparison.OrdinalIgnoreCase) ||
                        c.Type.Equals("permission", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Value);

        return Task.FromResult(PermisoHierarchy.SatisfaceRequerimiento(permissions ?? Enumerable.Empty<string>(), permiso));
    }
}

