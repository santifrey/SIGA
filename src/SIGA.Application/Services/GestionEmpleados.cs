using SIGA.Application.Common;
using SIGA.Application.DTOs.Empleados;
using SIGA.Application.Interfaces;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Interfaces;

namespace SIGA.Application.Services;

public class GestionEmpleados : IGestionEmpleados
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPersonaRepository _personaRepository;
    private readonly IRolRepository _rolRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public GestionEmpleados(
        IUsuarioRepository usuarioRepository,
        IPersonaRepository personaRepository,
        IRolRepository rolRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _usuarioRepository = usuarioRepository;
        _personaRepository = personaRepository;
        _rolRepository = rolRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<List<EmpleadoDto>>> GetEmpleadosAsync(CancellationToken cancellationToken = default)
    {
        var usuarios = await _usuarioRepository.GetAllWithRolesAndPersonaAsync(cancellationToken);

        // Filtrar personal interno (excluir rol CLIENTE y usuarios eliminados)
        var empleados = usuarios
            .Where(u => !u.IsDeleted && u.Rol != null && !string.Equals(u.Rol.Nombre, "CLIENTE", StringComparison.OrdinalIgnoreCase))
            .Select(MapToEmpleadoDto)
            .ToList();

        return ApiResponse<List<EmpleadoDto>>.Ok(empleados);
    }

    public async Task<ApiResponse<EmpleadoDto>> GetEmpleadoByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.GetByIdWithRolesAsync(id, cancellationToken);
        if (usuario == null || usuario.IsDeleted || string.Equals(usuario.Rol?.Nombre, "CLIENTE", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotFoundException("Empleado", id);
        }

        return ApiResponse<EmpleadoDto>.Ok(MapToEmpleadoDto(usuario));
    }

    public async Task<ApiResponse<EmpleadoDto>> CreateEmpleadoAsync(CrearEmpleadoDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Apellido))
            throw new ValidationException("El nombre y apellido son requeridos.");

        if (string.IsNullOrWhiteSpace(dto.Documento))
            throw new ValidationException("El número de documento es requerido.");

        if (string.IsNullOrWhiteSpace(dto.Email) && string.IsNullOrWhiteSpace(dto.Username))
            throw new ValidationException("El correo electrónico es requerido para generar el usuario del empleado.");

        // Username = Email (o Username si fue explícitamente provisto)
        var username = !string.IsNullOrWhiteSpace(dto.Username)
            ? dto.Username.Trim()
            : dto.Email!.Trim().ToLowerInvariant();

        // Password = DNI / Documento (o Password si fue explícitamente provista)
        var rawPassword = !string.IsNullOrWhiteSpace(dto.Password)
            ? dto.Password.Trim()
            : dto.Documento.Trim();

        if (rawPassword.Length < 4)
            throw new ValidationException("La contraseña / documento debe tener al menos 4 caracteres.");

        var rol = await _rolRepository.GetByIdAsync(dto.RolId, cancellationToken);
        if (rol == null)
            throw new NotFoundException(nameof(Rol), dto.RolId);

        if (string.Equals(rol.Nombre, "CLIENTE", StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("No se puede asignar el rol de CLIENTE a un empleado del sistema.");

        var existingUser = await _usuarioRepository.GetByUsernameWithRolesAsync(username, cancellationToken);
        if (existingUser != null)
            throw new ValidationException($"El nombre de usuario/correo '{username}' ya está en uso.");

        var existingPersona = await _personaRepository.GetByDocumentoAsync(dto.Documento.Trim(), cancellationToken);
        if (existingPersona != null)
            throw new ValidationException($"Ya existe una persona registrada con el documento '{dto.Documento}'.");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var empleado = new Empleado
            {
                Nombre = dto.Nombre.Trim(),
                Apellido = dto.Apellido.Trim(),
                Documento = dto.Documento.Trim(),
                Legajo = dto.Legajo?.Trim() ?? string.Empty,
                Posicion = dto.Posicion?.Trim() ?? string.Empty,
                Telefono = dto.Telefono?.Trim(),
                Email = dto.Email?.Trim(),
                FechaCreacion = DateTime.UtcNow
            };
            await _personaRepository.AddAsync(empleado, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var passwordHash = _passwordHasher.HashPassword(rawPassword);
            var usuario = new Usuario
            {
                PersonaId = empleado.Id,
                Persona = empleado,
                Username = username,
                PasswordHash = passwordHash,
                Estado = EstadoUsuario.Activo,
                Activado = false,
                RolId = rol.Id,
                Rol = rol,
                FechaCreacion = DateTime.UtcNow
            };
            await _usuarioRepository.AddAsync(usuario, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            var created = await _usuarioRepository.GetByIdWithRolesAsync(usuario.Id, cancellationToken);
            return ApiResponse<EmpleadoDto>.Created(MapToEmpleadoDto(created ?? usuario), "Empleado creado exitosamente.");
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ApiResponse<EmpleadoDto>> UpdateEmpleadoAsync(int id, ActualizarEmpleadoDto dto, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.GetByIdWithRolesAsync(id, cancellationToken);
        if (usuario == null || usuario.IsDeleted || string.Equals(usuario.Rol?.Nombre, "CLIENTE", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotFoundException("Empleado", id);
        }

        var newRol = await _rolRepository.GetByIdAsync(dto.RolId, cancellationToken);
        if (newRol == null)
            throw new NotFoundException(nameof(Rol), dto.RolId);

        if (string.Equals(newRol.Nombre, "CLIENTE", StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("No se puede asignar el rol de CLIENTE a un empleado del sistema.");

        // Salvaguarda: No degradar o inactivar al único administrador activo
        if (usuario.Rol?.Nombre == "ADMINISTRADOR" && usuario.Estado == EstadoUsuario.Activo)
        {
            bool rolCambiado = (newRol.Nombre != "ADMINISTRADOR");
            bool estadoInactivo = (dto.Estado != EstadoUsuario.Activo);

            if (rolCambiado || estadoInactivo)
            {
                var activeAdmins = await _usuarioRepository.CountAsync(
                    u => u.Rol.Nombre == "ADMINISTRADOR" && u.Estado == EstadoUsuario.Activo && !u.IsDeleted,
                    cancellationToken);

                if (activeAdmins <= 1)
                {
                    throw new ValidationException("No se puede desactivar el único administrador activo.");
                }
            }
        }

        // Validar documento único si cambió
        if (usuario.Persona != null && !string.Equals(usuario.Persona.Documento, dto.Documento?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            var existingDoc = await _personaRepository.GetByDocumentoAsync(dto.Documento!.Trim(), cancellationToken);
            if (existingDoc != null && existingDoc.Id != usuario.PersonaId)
                throw new ValidationException($"Ya existe una persona registrada con el documento '{dto.Documento}'.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (usuario.Persona != null)
            {
                usuario.Persona.Nombre = dto.Nombre.Trim();
                usuario.Persona.Apellido = dto.Apellido.Trim();
                usuario.Persona.Documento = dto.Documento.Trim();
                usuario.Persona.Telefono = dto.Telefono?.Trim();
                usuario.Persona.Email = dto.Email?.Trim();

                if (usuario.Persona is Empleado emp)
                {
                    if (dto.Legajo != null) emp.Legajo = dto.Legajo.Trim();
                    if (dto.Posicion != null) emp.Posicion = dto.Posicion.Trim();
                }

                await _personaRepository.UpdateAsync(usuario.Persona, cancellationToken);
            }

            usuario.RolId = newRol.Id;
            usuario.Rol = newRol;
            usuario.Estado = dto.Estado;

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                if (dto.Password.Length < 6)
                    throw new ValidationException("La contraseña debe tener al menos 6 caracteres.");

                usuario.PasswordHash = _passwordHasher.HashPassword(dto.Password);
            }

            await _usuarioRepository.UpdateAsync(usuario, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            var updated = await _usuarioRepository.GetByIdWithRolesAsync(id, cancellationToken);
            return ApiResponse<EmpleadoDto>.Ok(MapToEmpleadoDto(updated ?? usuario), "Empleado actualizado exitosamente.");
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ApiResponse<bool>> DeleteEmpleadoAsync(int id, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.GetByIdWithRolesAsync(id, cancellationToken);
        if (usuario == null || usuario.IsDeleted || string.Equals(usuario.Rol?.Nombre, "CLIENTE", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotFoundException("Empleado", id);
        }

        // Salvaguarda: No permitir desactivar al único administrador activo
        if (usuario.Rol?.Nombre == "ADMINISTRADOR" && usuario.Estado == EstadoUsuario.Activo)
        {
            var activeAdmins = await _usuarioRepository.CountAsync(
                u => u.Rol.Nombre == "ADMINISTRADOR" && u.Estado == EstadoUsuario.Activo && !u.IsDeleted,
                cancellationToken);

            if (activeAdmins <= 1)
            {
                throw new ValidationException("No se puede desactivar el único administrador activo.");
            }
        }

        // Soft delete estableciendo Estado = Inactivo
        usuario.Estado = EstadoUsuario.Inactivo;
        await _usuarioRepository.UpdateAsync(usuario, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Empleado desactivado exitosamente.");
    }

    private static EmpleadoDto MapToEmpleadoDto(Usuario usuario)
    {
        var empleado = usuario.Persona as Empleado;

        return new EmpleadoDto
        {
            Id = usuario.Id,
            PersonaId = usuario.PersonaId,
            Username = usuario.Username,
            Nombre = usuario.Persona?.Nombre ?? string.Empty,
            Apellido = usuario.Persona?.Apellido ?? string.Empty,
            NombreCompleto = usuario.Persona?.NombreCompleto ?? usuario.Username,
            Documento = usuario.Persona?.Documento ?? string.Empty,
            Legajo = empleado?.Legajo,
            Posicion = empleado?.Posicion,
            Telefono = usuario.Persona?.Telefono,
            Email = usuario.Persona?.Email,
            RolId = usuario.RolId,
            Rol = usuario.Rol?.Nombre ?? string.Empty,
            Estado = usuario.Estado,
            Activado = usuario.Activado,
            UltimoAcceso = usuario.UltimoAcceso,
            FechaCreacion = usuario.FechaCreacion
        };
    }
}
