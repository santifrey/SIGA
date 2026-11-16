using SIGA.Application.Common;
using SIGA.Application.DTOs.Auth;
using SIGA.Application.DTOs.Usuarios;
using SIGA.Application.Interfaces;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Interfaces;

namespace SIGA.Application.Services;

public class GestionUsuarios : IGestionUsuarios
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPersonaRepository _personaRepository;
    private readonly IRolRepository _rolRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;

    public GestionUsuarios(
        IUsuarioRepository usuarioRepository,
        IPersonaRepository personaRepository,
        IRolRepository rolRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IUnitOfWork unitOfWork)
    {
        _usuarioRepository = usuarioRepository;
        _personaRepository = personaRepository;
        _rolRepository = rolRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            throw new ValidationException("El nombre de usuario y la contraseña son requeridos.");

        var usuario = await _usuarioRepository.GetByUsernameWithRolesAsync(request.Username.Trim(), cancellationToken);
        if (usuario == null)
            throw new UnauthorizedException("Credenciales inválidas.");

        if (usuario.Estado != EstadoUsuario.Activo)
            throw new UnauthorizedException("El usuario se encuentra inactivo o bloqueado.");

        var isPasswordValid = _passwordHasher.VerifyPassword(request.Password, usuario.PasswordHash);
        if (!isPasswordValid)
            throw new UnauthorizedException("Credenciales inválidas.");

        usuario.UltimoAcceso = DateTime.UtcNow;
        await _usuarioRepository.UpdateAsync(usuario, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var rolNombre = usuario.Rol?.Nombre ?? string.Empty;
        var permisos = usuario.Rol?.RolPermisos?
            .Where(rp => rp.Permiso != null)
            .Select(rp => rp.Permiso.Codigo)
            .Distinct()
            .ToList() ?? new List<string>();

        var token = _tokenService.GenerateToken(usuario, rolNombre, permisos);

        var response = new LoginResponseDto
        {
            Token = token,
            Expiration = DateTime.UtcNow.AddHours(24),
            UsuarioId = usuario.Id,
            PersonaId = usuario.PersonaId,
            Username = usuario.Username,
            NombreCompleto = usuario.Persona != null ? usuario.Persona.NombreCompleto : usuario.Username,
            Email = usuario.Persona?.Email ?? string.Empty,
            RolId = usuario.RolId,
            Rol = rolNombre,
            Permisos = permisos,
            Roles = string.IsNullOrEmpty(rolNombre) ? new List<string>() : new List<string> { rolNombre }
        };

        return ApiResponse<LoginResponseDto>.Ok(response, "Inicio de sesión exitoso.");
    }

    public async Task<ApiResponse<UserInfoResponseDto>> GetMeAsync(int usuarioId, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.GetByIdWithRolesAsync(usuarioId, cancellationToken);
        if (usuario == null)
            throw new NotFoundException("Usuario no encontrado.");

        var rolNombre = usuario.Rol?.Nombre ?? string.Empty;
        var permisos = usuario.Rol?.RolPermisos?
            .Where(rp => rp.Permiso != null)
            .Select(rp => rp.Permiso.Codigo)
            .Distinct()
            .ToList() ?? new List<string>();

        var response = new UserInfoResponseDto
        {
            UsuarioId = usuario.Id,
            PersonaId = usuario.PersonaId,
            Username = usuario.Username,
            Nombre = usuario.Persona?.Nombre ?? string.Empty,
            Apellido = usuario.Persona?.Apellido ?? string.Empty,
            NombreCompleto = usuario.Persona?.NombreCompleto ?? usuario.Username,
            Email = usuario.Persona?.Email ?? string.Empty,
            Documento = usuario.Persona?.Documento ?? string.Empty,
            Telefono = usuario.Persona?.Telefono,
            RolId = usuario.RolId,
            Rol = rolNombre,
            Permisos = permisos,
            UltimoAcceso = usuario.UltimoAcceso,
            Estado = usuario.Estado,
            Roles = string.IsNullOrEmpty(rolNombre) ? new List<string>() : new List<string> { rolNombre }
        };

        return ApiResponse<UserInfoResponseDto>.Ok(response);
    }

    public async Task<ApiResponse<UsuarioDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.GetByIdWithRolesAsync(id, cancellationToken);
        if (usuario == null)
            throw new NotFoundException(nameof(Usuario), id);

        return ApiResponse<UsuarioDto>.Ok(MapToDto(usuario));
    }

    public async Task<ApiResponse<IEnumerable<UsuarioDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var usuarios = await _usuarioRepository.GetAllWithRolesAndPersonaAsync(cancellationToken);
        var dtos = usuarios.Select(MapToDto);
        return ApiResponse<IEnumerable<UsuarioDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<UsuarioDto>> CreateAsync(CreateUsuarioDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Username))
            throw new ValidationException("El nombre de usuario es obligatorio.");

        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
            throw new ValidationException("La contraseña debe tener al menos 6 caracteres.");

        var existingUser = await _usuarioRepository.GetByUsernameWithRolesAsync(dto.Username.Trim(), cancellationToken);
        if (existingUser != null)
            throw new ValidationException($"El nombre de usuario '{dto.Username}' ya está en uso.");

        Persona? persona;
        if (dto.PersonaId.HasValue && dto.PersonaId.Value > 0)
        {
            persona = await _personaRepository.GetByIdAsync(dto.PersonaId.Value, cancellationToken);
            if (persona == null)
                throw new NotFoundException(nameof(Persona), dto.PersonaId.Value);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Apellido) || string.IsNullOrWhiteSpace(dto.Documento))
                throw new ValidationException("Los datos de la persona (Nombre, Apellido, Documento) son requeridos si no se especifica un PersonaId existente.");

            var existingDoc = await _personaRepository.GetByDocumentoAsync(dto.Documento.Trim(), cancellationToken);
            if (existingDoc != null)
                throw new ValidationException($"Ya existe una persona registrada con el documento '{dto.Documento}'.");

            persona = new Persona
            {
                Nombre = dto.Nombre.Trim(),
                Apellido = dto.Apellido.Trim(),
                Documento = dto.Documento.Trim(),
                Telefono = dto.Telefono?.Trim(),
                Email = dto.Email?.Trim()
            };

            await _personaRepository.AddAsync(persona, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var passwordHash = _passwordHasher.HashPassword(dto.Password);

        var usuario = new Usuario
        {
            PersonaId = persona.Id,
            Username = dto.Username.Trim(),
            PasswordHash = passwordHash,
            Estado = EstadoUsuario.Activo
        };

        int targetRolId = dto.RolId > 0 ? dto.RolId : (dto.RolIds != null && dto.RolIds.Any() ? dto.RolIds.First() : 0);
        if (targetRolId > 0)
        {
            var rol = await _rolRepository.GetByIdAsync(targetRolId, cancellationToken);
            if (rol != null)
            {
                usuario.RolId = rol.Id;
                usuario.Rol = rol;
            }
        }

        await _usuarioRepository.AddAsync(usuario, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await _usuarioRepository.GetByIdWithRolesAsync(usuario.Id, cancellationToken);
        return ApiResponse<UsuarioDto>.Created(MapToDto(created ?? usuario), "Usuario creado exitosamente.");
    }

    public async Task<ApiResponse<UsuarioDto>> UpdateAsync(int id, UpdateUsuarioDto dto, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.GetByIdWithRolesAsync(id, cancellationToken);
        if (usuario == null)
            throw new NotFoundException(nameof(Usuario), id);

        if (usuario.Rol?.Nombre == "ADMINISTRADOR" && usuario.Estado == EstadoUsuario.Activo && dto.Estado != EstadoUsuario.Activo)
        {
            var activeAdmins = await _usuarioRepository.CountAsync(
                u => u.Rol.Nombre == "ADMINISTRADOR" && u.Estado == EstadoUsuario.Activo && !u.IsDeleted,
                cancellationToken);

            if (activeAdmins <= 1)
            {
                throw new ReglaNegocioException("No se puede desactivar el único administrador activo.");
            }
        }

        usuario.Estado = dto.Estado;

        int newRolId = dto.RolId > 0 ? dto.RolId : (dto.RolIds != null && dto.RolIds.Any() ? dto.RolIds.First() : 0);
        if (newRolId > 0)
        {
            var rol = await _rolRepository.GetByIdAsync(newRolId, cancellationToken);
            if (rol != null)
            {
                usuario.RolId = rol.Id;
                usuario.Rol = rol;
            }
        }

        await _usuarioRepository.UpdateAsync(usuario, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await _usuarioRepository.GetByIdWithRolesAsync(id, cancellationToken);
        return ApiResponse<UsuarioDto>.Ok(MapToDto(updated ?? usuario), "Usuario actualizado exitosamente.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.GetByIdWithRolesAsync(id, cancellationToken);
        if (usuario == null)
            throw new NotFoundException(nameof(Usuario), id);

        if (usuario.Rol?.Nombre == "ADMINISTRADOR" && usuario.Estado == EstadoUsuario.Activo)
        {
            var activeAdmins = await _usuarioRepository.CountAsync(
                u => u.Rol.Nombre == "ADMINISTRADOR" && u.Estado == EstadoUsuario.Activo && !u.IsDeleted,
                cancellationToken);

            if (activeAdmins <= 1)
            {
                throw new ReglaNegocioException("No se puede eliminar el único administrador activo.");
            }
        }

        await _usuarioRepository.DeleteAsync(usuario, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Usuario eliminado exitosamente.");
    }

    public async Task<ApiResponse<bool>> CambiarEstadoAsync(int id, EstadoUsuario nuevoEstado, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.GetByIdWithRolesAsync(id, cancellationToken);
        if (usuario == null)
            throw new NotFoundException(nameof(Usuario), id);

        if (usuario.Rol?.Nombre == "ADMINISTRADOR" && usuario.Estado == EstadoUsuario.Activo && nuevoEstado != EstadoUsuario.Activo)
        {
            var activeAdmins = await _usuarioRepository.CountAsync(
                u => u.Rol.Nombre == "ADMINISTRADOR" && u.Estado == EstadoUsuario.Activo && !u.IsDeleted,
                cancellationToken);

            if (activeAdmins <= 1)
            {
                throw new ReglaNegocioException("No se puede desactivar el único administrador activo.");
            }
        }

        usuario.Estado = nuevoEstado;
        await _usuarioRepository.UpdateAsync(usuario, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, $"Estado de usuario actualizado a {nuevoEstado}.");
    }

    public async Task<ApiResponse<bool>> AsignarRolesAsync(int id, List<int> rolIds, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.GetByIdWithRolesAsync(id, cancellationToken);
        if (usuario == null)
            throw new NotFoundException(nameof(Usuario), id);

        if (rolIds != null && rolIds.Any())
        {
            var rol = await _rolRepository.GetByIdAsync(rolIds.First(), cancellationToken);
            if (rol != null)
            {
                usuario.RolId = rol.Id;
                usuario.Rol = rol;
            }
        }

        await _usuarioRepository.UpdateAsync(usuario, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Roles asignados exitosamente.");
    }

    public async Task<ApiResponse<bool>> CambiarPasswordAsync(int id, CambiarPasswordDto dto, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(id, cancellationToken);
        if (usuario == null)
            throw new NotFoundException(nameof(Usuario), id);

        if (!_passwordHasher.VerifyPassword(dto.PasswordActual, usuario.PasswordHash))
            throw new ValidationException("La contraseña actual es incorrecta.");

        if (string.IsNullOrWhiteSpace(dto.PasswordNueva) || dto.PasswordNueva.Length < 6)
            throw new ValidationException("La nueva contraseña debe tener al menos 6 caracteres.");

        usuario.PasswordHash = _passwordHasher.HashPassword(dto.PasswordNueva);
        await _usuarioRepository.UpdateAsync(usuario, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Contraseña actualizada exitosamente.");
    }

    private static UsuarioDto MapToDto(Usuario usuario)
    {
        var rolNombre = usuario.Rol?.Nombre ?? string.Empty;
        return new UsuarioDto
        {
            Id = usuario.Id,
            PersonaId = usuario.PersonaId,
            Username = usuario.Username,
            NombreCompleto = usuario.Persona != null ? usuario.Persona.NombreCompleto : string.Empty,
            Documento = usuario.Persona?.Documento ?? string.Empty,
            Email = usuario.Persona?.Email,
            RolId = usuario.RolId,
            Rol = rolNombre,
            Estado = usuario.Estado,
            FechaCreacion = usuario.FechaCreacion,
            UltimoAcceso = usuario.UltimoAcceso,
            Roles = string.IsNullOrEmpty(rolNombre) ? new List<string>() : new List<string> { rolNombre }
        };
    }
}
