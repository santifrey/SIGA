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
    private readonly IUnitOfWork _unitOfWork;

    public GestionClientes(
        IClienteRepository clienteRepository,
        IPersonaRepository personaRepository,
        IUnitOfWork unitOfWork)
    {
        _clienteRepository = clienteRepository;
        _personaRepository = personaRepository;
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
        Persona? persona;
        if (dto.PersonaId.HasValue && dto.PersonaId.Value > 0)
        {
            persona = await _personaRepository.GetByIdAsync(dto.PersonaId.Value, cancellationToken);
            if (persona == null)
                throw new NotFoundException(nameof(Persona), dto.PersonaId.Value);

            var existingCliente = await _clienteRepository.GetByPersonaIdAsync(persona.Id, cancellationToken);
            if (existingCliente != null)
                throw new ValidationException($"La persona '{persona.NombreCompleto}' ya está registrada como cliente.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Apellido) || string.IsNullOrWhiteSpace(dto.Documento))
                throw new ValidationException("Los datos de la persona (Nombre, Apellido, Documento) son requeridos.");

            var existingDoc = await _personaRepository.GetByDocumentoAsync(dto.Documento.Trim(), cancellationToken);
            if (existingDoc != null)
            {
                persona = existingDoc;
                var existingCliente = await _clienteRepository.GetByPersonaIdAsync(persona.Id, cancellationToken);
                if (existingCliente != null)
                    throw new ValidationException($"La persona con documento '{dto.Documento}' ya está registrada como cliente.");
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

        string codigoCliente = dto.CodigoCliente?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(codigoCliente))
        {
            codigoCliente = $"CLI-{DateTime.UtcNow:yyMM}-{persona.Id:D4}";
        }

        var existingWithCode = await _clienteRepository.GetByCodigoWithPersonaAsync(codigoCliente, cancellationToken);
        if (existingWithCode != null)
            throw new ValidationException($"El código de cliente '{codigoCliente}' ya está registrado.");

        var cliente = new Cliente
        {
            PersonaId = persona.Id,
            CodigoCliente = codigoCliente,
            Observaciones = dto.Observaciones?.Trim(),
            EstadoCliente = EstadoCliente.Activo,
            Estado = EstadoParticipante.Activo
        };

        await _clienteRepository.AddAsync(cliente, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await _clienteRepository.GetByIdWithPersonaAsync(cliente.Id, cancellationToken);
        return ApiResponse<ClienteDto>.Created(MapToDto(created ?? cliente), "Cliente creado exitosamente.");
    }

    public async Task<ApiResponse<ClienteDto>> UpdateAsync(int id, UpdateClienteDto dto, CancellationToken cancellationToken = default)
    {
        var cliente = await _clienteRepository.GetByIdWithPersonaAsync(id, cancellationToken);
        if (cliente == null)
            throw new NotFoundException(nameof(Cliente), id);

        cliente.Observaciones = dto.Observaciones?.Trim();
        cliente.EstadoCliente = dto.EstadoCliente;
        cliente.Estado = dto.EstadoCliente == EstadoCliente.Activo ? EstadoParticipante.Activo : EstadoParticipante.Inactivo;

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
