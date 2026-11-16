using SIGA.Application.Common;
using SIGA.Application.DTOs.Clientes;
using SIGA.Application.Interfaces;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Interfaces;

namespace SIGA.Application.Services;

public class GestionClientes : IGestionClientes
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IPersonaRepository _personaRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IRolRepository _rolRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public GestionClientes(
        IClienteRepository clienteRepository,
        IPersonaRepository personaRepository,
        IUsuarioRepository usuarioRepository,
        IRolRepository rolRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _clienteRepository = clienteRepository;
        _personaRepository = personaRepository;
        _usuarioRepository = usuarioRepository;
        _rolRepository = rolRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<ClienteDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var cliente = await _clienteRepository.GetByIdWithPersonaAsync(id, cancellationToken);
        if (cliente == null)
            throw new NotFoundException(nameof(Cliente), id);

        return ApiResponse<ClienteDto>.Ok(MapToDto(cliente));
    }

    public async Task<ApiResponse<ClienteDto>> GetByCodigoAsync(string codigo, CancellationToken cancellationToken = default)
    {
        var cliente = await _clienteRepository.GetByCodigoWithPersonaAsync(codigo, cancellationToken);
        if (cliente == null)
            throw new NotFoundException($"No se encontró cliente con código '{codigo}'.");

        return ApiResponse<ClienteDto>.Ok(MapToDto(cliente));
    }

    public async Task<ApiResponse<IEnumerable<ClienteDto>>> GetAllAsync(string? busqueda = null, CancellationToken cancellationToken = default)
    {
        var clientes = await _clienteRepository.GetAllWithPersonaAsync(busqueda, cancellationToken);
        var dtos = clientes.Select(MapToDto);
        return ApiResponse<IEnumerable<ClienteDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<ClienteDto>> CreateAsync(CreateClienteDto dto, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            Persona? persona;
            if (dto.PersonaId.HasValue && dto.PersonaId.Value > 0)
            {
                persona = await _personaRepository.GetByIdAsync(dto.PersonaId.Value, cancellationToken);
                if (persona == null)
                    throw new NotFoundException(nameof(Persona), dto.PersonaId.Value);

                var existingCliente = await _clienteRepository.GetByPersonaIdAsync(persona.Id, cancellationToken);
                if (existingCliente != null)
                    throw new ValidationException($"La persona '{persona.NombreCompleto}' ya está registrada como cliente.");

                if (string.IsNullOrWhiteSpace(persona.Email) && !string.IsNullOrWhiteSpace(dto.Email))
                {
                    persona.Email = dto.Email.Trim();
                    await _personaRepository.UpdateAsync(persona, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Apellido) || string.IsNullOrWhiteSpace(dto.Documento))
                    throw new ValidationException("Los datos de la persona (Nombre, Apellido, Documento) son requeridos.");

                if (string.IsNullOrWhiteSpace(dto.Email))
                    throw new ValidationException("El correo electrónico es requerido para generar la cuenta de usuario del cliente.");

                var existingDoc = await _personaRepository.GetByDocumentoAsync(dto.Documento.Trim(), cancellationToken);
                if (existingDoc != null)
                {
                    persona = existingDoc;
                    var existingCliente = await _clienteRepository.GetByPersonaIdAsync(persona.Id, cancellationToken);
                    if (existingCliente != null)
                        throw new ValidationException($"La persona con documento '{dto.Documento}' ya está registrada como cliente.");

                    if (string.IsNullOrWhiteSpace(persona.Email) && !string.IsNullOrWhiteSpace(dto.Email))
                    {
                        persona.Email = dto.Email.Trim();
                        await _personaRepository.UpdateAsync(persona, cancellationToken);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }
                }
                else
                {
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
            }

            if (string.IsNullOrWhiteSpace(persona.Email))
            {
                throw new ValidationException("El correo electrónico es requerido para generar el usuario del cliente.");
            }

            string codigoCliente = dto.CodigoCliente?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(codigoCliente))
            {
                codigoCliente = $"CLI-{DateTime.UtcNow:yyMM}-{persona.Id:D4}";
            }

            var existingWithCode = await _clienteRepository.GetByCodigoWithPersonaAsync(codigoCliente, cancellationToken);
            if (existingWithCode != null)
                throw new ValidationException($"El código de cliente '{codigoCliente}' ya está registrado.");

            var estadoInicial = dto.Activo == false ? EstadoCliente.Inactivo : EstadoCliente.Activo;
            var cliente = new Cliente
            {
                PersonaId = persona.Id,
                CodigoCliente = codigoCliente,
                Observaciones = dto.Observaciones?.Trim(),
                EstadoCliente = estadoInicial,
                Estado = estadoInicial == EstadoCliente.Activo ? EstadoParticipante.Activo : EstadoParticipante.Inactivo
            };

            await _clienteRepository.AddAsync(cliente, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Generar usuario para el cliente: Username = Email, Password = DNI
            var username = persona.Email.Trim().ToLowerInvariant();
            var rawPassword = persona.Documento.Trim();

            var existingUsers = await _usuarioRepository.FindAsync(u => u.PersonaId == persona.Id, cancellationToken);
            if (!existingUsers.Any())
            {
                var userWithSameUsername = await _usuarioRepository.GetByUsernameWithRolesAsync(username, cancellationToken);
                if (userWithSameUsername != null)
                {
                    throw new ValidationException($"El correo electrónico '{username}' ya está en uso como nombre de usuario de otra cuenta.");
                }

                var rolCliente = await _rolRepository.GetByNombreAsync("CLIENTE", cancellationToken)
                    ?? await _rolRepository.GetByIdAsync(4, cancellationToken);

                if (rolCliente == null)
                {
                    throw new InvalidOperationException("El rol 'CLIENTE' no está configurado en el sistema.");
                }

                var usuario = new Usuario
                {
                    PersonaId = persona.Id,
                    Persona = persona,
                    Username = username,
                    PasswordHash = _passwordHasher.HashPassword(rawPassword),
                    Estado = EstadoUsuario.Activo,
                    Activado = false,
                    RolId = rolCliente.Id,
                    Rol = rolCliente,
                    FechaCreacion = DateTime.UtcNow
                };

                await _usuarioRepository.AddAsync(usuario, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            var created = await _clienteRepository.GetByIdWithPersonaAsync(cliente.Id, cancellationToken);
            return ApiResponse<ClienteDto>.Created(MapToDto(created ?? cliente), "Cliente creado exitosamente con cuenta de usuario generada.");
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ApiResponse<ClienteDto>> UpdateAsync(int id, UpdateClienteDto dto, CancellationToken cancellationToken = default)
    {
        var cliente = await _clienteRepository.GetByIdWithPersonaAsync(id, cancellationToken);
        if (cliente == null)
            throw new NotFoundException(nameof(Cliente), id);

        if (cliente.Persona != null)
        {
            if (!string.IsNullOrWhiteSpace(dto.Nombre))
                cliente.Persona.Nombre = dto.Nombre.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Apellido))
                cliente.Persona.Apellido = dto.Apellido.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Documento))
                cliente.Persona.Documento = dto.Documento.Trim();
            if (dto.Telefono != null)
                cliente.Persona.Telefono = dto.Telefono.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var newEmail = dto.Email.Trim();
                if (!string.Equals(cliente.Persona.Email, newEmail, StringComparison.OrdinalIgnoreCase))
                {
                    cliente.Persona.Email = newEmail;
                    var users = await _usuarioRepository.FindAsync(u => u.PersonaId == cliente.Persona.Id, cancellationToken);
                    var user = users.FirstOrDefault();
                    if (user != null)
                    {
                        user.Username = newEmail.ToLowerInvariant();
                        await _usuarioRepository.UpdateAsync(user, cancellationToken);
                    }
                }
            }

            await _personaRepository.UpdateAsync(cliente.Persona, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(dto.CodigoCliente))
        {
            cliente.CodigoCliente = dto.CodigoCliente.Trim();
        }

        if (dto.Observaciones != null)
        {
            cliente.Observaciones = dto.Observaciones.Trim();
        }

        if (dto.EstadoCliente.HasValue)
        {
            cliente.EstadoCliente = dto.EstadoCliente.Value;
        }
        else if (dto.Activo.HasValue)
        {
            cliente.EstadoCliente = dto.Activo.Value ? EstadoCliente.Activo : EstadoCliente.Inactivo;
        }

        cliente.Estado = cliente.EstadoCliente == EstadoCliente.Activo ? EstadoParticipante.Activo : EstadoParticipante.Inactivo;

        await _clienteRepository.UpdateAsync(cliente, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<ClienteDto>.Ok(MapToDto(cliente), "Cliente actualizado exitosamente.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var cliente = await _clienteRepository.GetByIdAsync(id, cancellationToken);
        if (cliente == null)
            throw new NotFoundException(nameof(Cliente), id);

        await _clienteRepository.DeleteAsync(cliente, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Cliente eliminado exitosamente.");
    }

    private static ClienteDto MapToDto(Cliente cliente)
    {
        return new ClienteDto
        {
            Id = cliente.Id,
            PersonaId = cliente.PersonaId,
            Nombre = cliente.Persona?.Nombre ?? string.Empty,
            Apellido = cliente.Persona?.Apellido ?? string.Empty,
            NombreCompleto = cliente.Persona != null ? cliente.Persona.NombreCompleto : string.Empty,
            Documento = cliente.Persona?.Documento ?? string.Empty,
            Telefono = cliente.Persona?.Telefono,
            Email = cliente.Persona?.Email,
            CodigoCliente = cliente.CodigoCliente,
            Observaciones = cliente.Observaciones,
            EstadoCliente = cliente.EstadoCliente,
            FechaRegistro = cliente.FechaRegistro
        };
    }
}
