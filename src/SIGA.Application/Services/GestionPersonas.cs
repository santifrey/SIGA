using SIGA.Application.Common;
using SIGA.Application.DTOs.Personas;
using SIGA.Application.Interfaces;
using SIGA.Domain.Entities;
using SIGA.Domain.Interfaces;

namespace SIGA.Application.Services;

public class GestionPersonas : IGestionPersonas
{
    private readonly IPersonaRepository _personaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GestionPersonas(IPersonaRepository personaRepository, IUnitOfWork unitOfWork)
    {
        _personaRepository = personaRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<PersonaDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var persona = await _personaRepository.GetByIdAsync(id, cancellationToken);
        if (persona == null)
            throw new NotFoundException(nameof(Persona), id);

        return ApiResponse<PersonaDto>.Ok(MapToDto(persona));
    }

    public async Task<ApiResponse<PersonaDto>> GetByDocumentoAsync(string documento, CancellationToken cancellationToken = default)
    {
        var persona = await _personaRepository.GetByDocumentoAsync(documento, cancellationToken);
        if (persona == null)
            throw new NotFoundException($"No se encontró persona con documento '{documento}'.");

        return ApiResponse<PersonaDto>.Ok(MapToDto(persona));
    }

    public async Task<ApiResponse<IEnumerable<PersonaDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var personas = await _personaRepository.GetAllAsync(cancellationToken);
        var dtos = personas.Select(MapToDto);
        return ApiResponse<IEnumerable<PersonaDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<PersonaDto>> CreateAsync(CreatePersonaDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Apellido))
            throw new ValidationException("El nombre y el apellido son obligatorios.");

        if (string.IsNullOrWhiteSpace(dto.Documento))
            throw new ValidationException("El documento es obligatorio.");

        var exists = await _personaRepository.GetByDocumentoAsync(dto.Documento, cancellationToken);
        if (exists != null)
            throw new ValidationException($"Ya existe una persona registrada con el documento '{dto.Documento}'.");

        var persona = new Persona
        {
            Nombre = dto.Nombre.Trim(),
            Apellido = dto.Apellido.Trim(),
            Documento = dto.Documento.Trim(),
            Telefono = dto.Telefono?.Trim(),
            Email = dto.Email?.Trim()
        };

        await _personaRepository.AddAsync(persona, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<PersonaDto>.Created(MapToDto(persona), "Persona creada exitosamente.");
    }

    public async Task<ApiResponse<PersonaDto>> UpdateAsync(int id, UpdatePersonaDto dto, CancellationToken cancellationToken = default)
    {
        var persona = await _personaRepository.GetByIdAsync(id, cancellationToken);
        if (persona == null)
            throw new NotFoundException(nameof(Persona), id);

        if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Apellido))
            throw new ValidationException("El nombre y el apellido son obligatorios.");

        if (string.IsNullOrWhiteSpace(dto.Documento))
            throw new ValidationException("El documento es obligatorio.");

        // Verificar duplicado de documento en otra persona
        var existingDoc = await _personaRepository.GetByDocumentoAsync(dto.Documento, cancellationToken);
        if (existingDoc != null && existingDoc.Id != id)
            throw new ValidationException($"El documento '{dto.Documento}' ya está asociado a otra persona.");

        persona.Nombre = dto.Nombre.Trim();
        persona.Apellido = dto.Apellido.Trim();
        persona.Documento = dto.Documento.Trim();
        persona.Telefono = dto.Telefono?.Trim();
        persona.Email = dto.Email?.Trim();

        await _personaRepository.UpdateAsync(persona, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<PersonaDto>.Ok(MapToDto(persona), "Persona actualizada exitosamente.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var persona = await _personaRepository.GetByIdAsync(id, cancellationToken);
        if (persona == null)
            throw new NotFoundException(nameof(Persona), id);

        await _personaRepository.DeleteAsync(persona, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Persona eliminada exitosamente.");
    }

    private static PersonaDto MapToDto(Persona persona)
    {
        return new PersonaDto
        {
            Id = persona.Id,
            Nombre = persona.Nombre,
            Apellido = persona.Apellido,
            Documento = persona.Documento,
            Telefono = persona.Telefono,
            Email = persona.Email,
            FechaCreacion = persona.FechaCreacion,
            FechaModificacion = persona.FechaModificacion
        };
    }
}
