using SIGA.Application.Common;
using SIGA.Application.DTOs.Permisos;
using SIGA.Application.Interfaces;
using SIGA.Domain.Entities;
using SIGA.Domain.Interfaces;

namespace SIGA.Application.Services;

public class GestionPermisos : IGestionPermisos
{
    private readonly IRepository<Permiso> _permisoRepository;

    public GestionPermisos(IRepository<Permiso> permisoRepository)
    {
        _permisoRepository = permisoRepository;
    }

    public async Task<ApiResponse<List<PermisoDto>>> GetAllPermisosAsync(CancellationToken cancellationToken = default)
    {
        var permisos = await _permisoRepository.GetAllAsync(cancellationToken);
        var dtos = permisos
            .OrderBy(p => p.Modulo)
            .ThenBy(p => p.Codigo)
            .Select(p => new PermisoDto
            {
                Id = p.Id,
                Codigo = p.Codigo,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                Modulo = p.Modulo
            })
            .ToList();

        return ApiResponse<List<PermisoDto>>.Ok(dtos);
    }
}
