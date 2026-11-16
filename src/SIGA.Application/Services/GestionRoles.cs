using SIGA.Application.Common;
using SIGA.Application.DTOs.Roles;
using SIGA.Application.Interfaces;
using SIGA.Domain.Entities;
using SIGA.Domain.Interfaces;

namespace SIGA.Application.Services;

public class GestionRoles : IGestionRoles
{
    private static readonly HashSet<string> RolesSistemaNombres = new(StringComparer.OrdinalIgnoreCase)
    {
        "ADMINISTRADOR",
        "VENDEDOR",
        "SECRETARIA",
        "CLIENTE",
        "CONTABILIDAD"
    };

    private readonly IRolRepository _rolRepository;
    private readonly IRepository<Permiso> _permisoRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GestionRoles(
        IRolRepository rolRepository,
        IRepository<Permiso> permisoRepository,
        IUsuarioRepository usuarioRepository,
        IUnitOfWork unitOfWork)
    {
        _rolRepository = rolRepository;
        _permisoRepository = permisoRepository;
        _usuarioRepository = usuarioRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<List<RolDto>>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _rolRepository.GetAllWithPermisosAsync(cancellationToken);
        var dtos = roles.Select(MapToDto).ToList();
        return ApiResponse<List<RolDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<RolDto>> GetRolByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var rol = await _rolRepository.GetByIdWithPermisosAsync(id, cancellationToken);
        if (rol == null)
            throw new NotFoundException(nameof(Rol), id);

        return ApiResponse<RolDto>.Ok(MapToDto(rol));
    }

    public async Task<ApiResponse<RolDto>> CreateRolAsync(CrearRolDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            throw new ValidationException("El nombre del rol es requerido.");

        var nombreNorm = dto.Nombre.Trim().ToUpperInvariant();

        if (RolesSistemaNombres.Contains(nombreNorm))
            throw new ValidationException($"No se puede crear un rol con el nombre reservado '{nombreNorm}'.");

        var existing = await _rolRepository.GetByNombreAsync(nombreNorm, cancellationToken);
        if (existing != null)
            throw new ValidationException($"Ya existe un rol con el nombre '{nombreNorm}'.");

        var rol = new Rol
        {
            Nombre = nombreNorm,
            Descripcion = dto.Descripcion?.Trim(),
            EsSistema = false
        };

        if (dto.Permisos != null && dto.Permisos.Any())
        {
            var permisosDisponibles = await _permisoRepository.GetAllAsync(cancellationToken);
            var permDict = permisosDisponibles.ToDictionary(p => p.Codigo, StringComparer.OrdinalIgnoreCase);

            foreach (var permCode in dto.Permisos)
            {
                if (permDict.TryGetValue(permCode.Trim(), out var permEntity))
                {
                    rol.RolPermisos.Add(new RolPermiso
                    {
                        Rol = rol,
                        PermisoId = permEntity.Id,
                        Permiso = permEntity
                    });
                }
            }
        }

        await _rolRepository.AddAsync(rol, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await _rolRepository.GetByIdWithPermisosAsync(rol.Id, cancellationToken);
        return ApiResponse<RolDto>.Created(MapToDto(created ?? rol), "Rol creado exitosamente.");
    }

    public async Task<ApiResponse<RolDto>> UpdateRolAsync(int id, ActualizarRolDto dto, CancellationToken cancellationToken = default)
    {
        var rol = await _rolRepository.GetByIdWithPermisosAsync(id, cancellationToken);
        if (rol == null)
            throw new NotFoundException(nameof(Rol), id);

        if (string.IsNullOrWhiteSpace(dto.Nombre))
            throw new ValidationException("El nombre del rol es requerido.");

        var nuevoNombre = dto.Nombre.Trim().ToUpperInvariant();

        // Salvaguarda 1: No se puede modificar el nombre de un rol de sistema
        if (rol.EsSistema)
        {
            if (!string.Equals(rol.Nombre, nuevoNombre, StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException("No se permite modificar el nombre de un rol de sistema.");
            }
        }
        else
        {
            if (!string.Equals(rol.Nombre, nuevoNombre, StringComparison.OrdinalIgnoreCase))
            {
                if (RolesSistemaNombres.Contains(nuevoNombre))
                    throw new ValidationException($"No se puede asignar el nombre reservado '{nuevoNombre}'.");

                var existing = await _rolRepository.GetByNombreAsync(nuevoNombre, cancellationToken);
                if (existing != null && existing.Id != id)
                    throw new ValidationException($"Ya existe un rol con el nombre '{nuevoNombre}'.");

                rol.Nombre = nuevoNombre;
            }
        }

        rol.Descripcion = dto.Descripcion?.Trim();

        // Sincronizar permisos
        rol.RolPermisos.Clear();
        if (dto.Permisos != null && dto.Permisos.Any())
        {
            var permisosDisponibles = await _permisoRepository.GetAllAsync(cancellationToken);
            var permDict = permisosDisponibles.ToDictionary(p => p.Codigo, StringComparer.OrdinalIgnoreCase);

            foreach (var permCode in dto.Permisos)
            {
                if (permDict.TryGetValue(permCode.Trim(), out var permEntity))
                {
                    rol.RolPermisos.Add(new RolPermiso
                    {
                        RolId = rol.Id,
                        Rol = rol,
                        PermisoId = permEntity.Id,
                        Permiso = permEntity
                    });
                }
            }
        }

        await _rolRepository.UpdateAsync(rol, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await _rolRepository.GetByIdWithPermisosAsync(id, cancellationToken);
        return ApiResponse<RolDto>.Ok(MapToDto(updated ?? rol), "Rol actualizado exitosamente.");
    }

    public async Task<ApiResponse<bool>> DeleteRolAsync(int id, CancellationToken cancellationToken = default)
    {
        var rol = await _rolRepository.GetByIdWithPermisosAsync(id, cancellationToken);
        if (rol == null)
            throw new NotFoundException(nameof(Rol), id);

        // Salvaguarda 1: Prohibido eliminar roles de sistema (EsSistema == true)
        if (rol.EsSistema)
        {
            throw new ValidationException("No se puede eliminar un rol protegido del sistema.");
        }

        // Salvaguarda 2: Prohibido eliminar rol con usuarios activos asignados
        var hasActiveUsers = await _usuarioRepository.ExistsAsync(u => u.RolId == id && !u.IsDeleted, cancellationToken);
        if (hasActiveUsers)
        {
            throw new ValidationException("No se puede eliminar el rol porque tiene usuarios asignados. Reasigne los usuarios antes de eliminar el rol.");
        }

        await _rolRepository.DeleteAsync(rol, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Rol eliminado exitosamente.");
    }

    private static RolDto MapToDto(Rol rol)
    {
        return new RolDto
        {
            Id = rol.Id,
            Nombre = rol.Nombre,
            Descripcion = rol.Descripcion,
            EsSistema = rol.EsSistema,
            TotalUsuarios = rol.Usuarios?.Count(u => !u.IsDeleted) ?? 0,
            Permisos = rol.RolPermisos?
                .Where(rp => rp.Permiso != null)
                .Select(rp => rp.Permiso.Codigo)
                .OrderBy(c => c)
                .ToList() ?? new List<string>()
        };
    }
}
