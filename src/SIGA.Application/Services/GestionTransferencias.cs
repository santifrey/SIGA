using SIGA.Application.Common;
using SIGA.Application.DTOs.Transferencias;
using SIGA.Application.Interfaces;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Exceptions;
using SIGA.Domain.Interfaces;

namespace SIGA.Application.Services;

public class GestionTransferencias : IGestionTransferencias
{
    private readonly ITransferenciaLoteRepository _transferenciaRepo;
    private readonly ILoteRepository _loteRepository;
    private readonly IRepository<ParticipanteComercial> _participanteRepo;
    private readonly IUnitOfWork _unitOfWork;

    public GestionTransferencias(
        ITransferenciaLoteRepository transferenciaRepo,
        ILoteRepository loteRepository,
        IRepository<ParticipanteComercial> participanteRepo,
        IUnitOfWork unitOfWork)
    {
        _transferenciaRepo = transferenciaRepo;
        _loteRepository = loteRepository;
        _participanteRepo = participanteRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<TransferenciaLoteDto>> TransferirLoteAsync(
        TransferirLoteDto dto, 
        int usuarioId, 
        CancellationToken cancellationToken = default)
    {
        if (dto.LoteId <= 0)
            throw new ValidationException("El identificador del lote es inválido.");

        if (dto.NuevoTitularId <= 0)
            throw new ValidationException("El nuevo titular es requerido.");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var lote = await _loteRepository.GetByIdWithDetailsAsync(dto.LoteId, cancellationToken);
            if (lote == null)
                throw new NotFoundException(nameof(Lote), dto.LoteId);

            if (lote.Temporada != null && lote.Temporada.Estado == EstadoTemporada.Cerrada)
                throw new OperacionInvalidaException("No se pueden transferir lotes de una temporada cerrada.");

            if (lote.Estado == EstadoLote.Cosechado || lote.Estado == EstadoLote.Cerrado)
                throw new OperacionInvalidaException("El lote ya se encuentra cosechado o cerrado. Solo se pueden transferir lotes plantados o en crecimiento.");

            var hasPending = await _transferenciaRepo.HasPendingTransferAsync(dto.LoteId, cancellationToken);
            if (hasPending)
                throw new OperacionInvalidaException("El lote ya posee una transferencia pendiente de confirmación.");

            var titularActualId = lote.ResolverTitularVigenteId();
            if (dto.NuevoTitularId == titularActualId)
                throw new OperacionInvalidaException("El nuevo titular no puede ser el mismo que el titular actual del lote.");

            var existeNuevoTitular = await _participanteRepo.ExistsAsync(p => p.Id == dto.NuevoTitularId, cancellationToken);
            if (!existeNuevoTitular)
                throw new NotFoundException("Participante", dto.NuevoTitularId);

            var transferencia = new TransferenciaLote(
                dto.LoteId, 
                titularActualId, 
                dto.NuevoTitularId, 
                usuarioId, 
                dto.Observaciones);

            // Se confirma la transferencia del lote completo de forma inmediata para efectivizar los derechos productivos
            transferencia.Confirmar();

            await _transferenciaRepo.AddAsync(transferencia, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            var creada = await _transferenciaRepo.GetByIdWithDetailsAsync(transferencia.Id, cancellationToken);
            return ApiResponse<TransferenciaLoteDto>.Created(MapToDto(creada ?? transferencia), "Transferencia de lote completo realizada con éxito.");
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ApiResponse<TransferenciaLoteDto>> ConfirmarTransferenciaAsync(
        int transferenciaId, 
        int usuarioId, 
        CancellationToken cancellationToken = default)
    {
        var transferencia = await _transferenciaRepo.GetByIdWithDetailsAsync(transferenciaId, cancellationToken);
        if (transferencia == null)
            throw new NotFoundException(nameof(TransferenciaLote), transferenciaId);

        if (transferencia.Estado != EstadoTransferencia.Pendiente)
            throw new OperacionInvalidaException($"La transferencia no se encuentra en estado Pendiente (estado actual: {transferencia.Estado}).");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            transferencia.Confirmar();
            await _transferenciaRepo.UpdateAsync(transferencia, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return ApiResponse<TransferenciaLoteDto>.Ok(MapToDto(transferencia), "Transferencia confirmada exitosamente.");
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ApiResponse<TransferenciaLoteDto>> AnularTransferenciaAsync(
        int transferenciaId, 
        string motivo, 
        int usuarioId, 
        CancellationToken cancellationToken = default)
    {
        var transferencia = await _transferenciaRepo.GetByIdWithDetailsAsync(transferenciaId, cancellationToken);
        if (transferencia == null)
            throw new NotFoundException(nameof(TransferenciaLote), transferenciaId);

        if (transferencia.Estado == EstadoTransferencia.Anulada)
            throw new OperacionInvalidaException("La transferencia ya se encuentra anulada.");

        if (transferencia.Lote != null && (transferencia.Lote.Estado == EstadoLote.Cosechado || transferencia.Lote.Estado == EstadoLote.Cerrado))
            throw new OperacionInvalidaException("No se puede anular una transferencia de un lote que ya fue cosechado o cerrado.");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            transferencia.Anular(motivo);
            await _transferenciaRepo.UpdateAsync(transferencia, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return ApiResponse<TransferenciaLoteDto>.Ok(MapToDto(transferencia), "Transferencia anulada exitosamente.");
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ApiResponse<HistorialTitularidadDto>> ObtenerHistorialTitularidadAsync(
        int loteId, 
        CancellationToken cancellationToken = default)
    {
        var lote = await _loteRepository.GetByIdWithDetailsAsync(loteId, cancellationToken);
        if (lote == null)
            throw new NotFoundException(nameof(Lote), loteId);

        var transferencias = await _transferenciaRepo.GetByLoteIdAsync(loteId, cancellationToken);
        var transferenciasDtos = transferencias.Select(MapToDto).ToList();

        var titularVigenteId = lote.ResolverTitularVigenteId();
        string nombreTitularVigente = ObtenerNombreParticipante(
            transferencias.FirstOrDefault(t => t.Estado == EstadoTransferencia.Confirmada)?.NuevoTitular ?? lote.Cliente);

        var dto = new HistorialTitularidadDto
        {
            LoteId = lote.Id,
            IdentificacionFila = lote.Fila?.Identificacion ?? string.Empty,
            NombreSucursal = lote.Fila?.Sucursal?.Nombre ?? string.Empty,
            TitularOriginalId = lote.ClienteId,
            NombreTitularOriginal = ObtenerNombreParticipante(lote.Cliente),
            TitularVigenteId = titularVigenteId,
            NombreTitularVigente = nombreTitularVigente,
            CantidadPlantadaTotal = lote.CantidadPlantadaTotal,
            Transferencias = transferenciasDtos
        };

        return ApiResponse<HistorialTitularidadDto>.Ok(dto);
    }

    public async Task<ApiResponse<IEnumerable<TransferenciaLoteDto>>> ListarTransferenciasAsync(
        int? loteId = null, 
        EstadoTransferencia? estado = null, 
        CancellationToken cancellationToken = default)
    {
        IEnumerable<TransferenciaLote> list;
        if (loteId.HasValue && loteId.Value > 0)
        {
            list = await _transferenciaRepo.GetByLoteIdAsync(loteId.Value, cancellationToken);
            if (estado.HasValue)
            {
                list = list.Where(t => t.Estado == estado.Value);
            }
        }
        else
        {
            list = await _transferenciaRepo.GetAllWithDetailsAsync(estado, cancellationToken);
        }

        var dtos = list.Select(MapToDto);
        return ApiResponse<IEnumerable<TransferenciaLoteDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<TransferenciaLoteDto>> ObtenerPorIdAsync(
        int id, 
        CancellationToken cancellationToken = default)
    {
        var transferencia = await _transferenciaRepo.GetByIdWithDetailsAsync(id, cancellationToken);
        if (transferencia == null)
            throw new NotFoundException(nameof(TransferenciaLote), id);

        return ApiResponse<TransferenciaLoteDto>.Ok(MapToDto(transferencia));
    }

    private static TransferenciaLoteDto MapToDto(TransferenciaLote t)
    {
        return new TransferenciaLoteDto
        {
            Id = t.Id,
            LoteId = t.LoteId,
            IdentificacionFila = t.Lote?.Fila?.Identificacion ?? string.Empty,
            NombreSucursal = t.Lote?.Fila?.Sucursal?.Nombre ?? string.Empty,
            TitularAnteriorId = t.TitularAnteriorId,
            NombreTitularAnterior = ObtenerNombreParticipante(t.TitularAnterior),
            NuevoTitularId = t.NuevoTitularId,
            NombreNuevoTitular = ObtenerNombreParticipante(t.NuevoTitular),
            Fecha = t.Fecha,
            Estado = t.Estado,
            Observaciones = t.Observaciones
        };
    }

    private static string ObtenerNombreParticipante(ParticipanteComercial? p)
    {
        if (p == null) return string.Empty;

        if (p is Cliente c)
        {
            var nombre = c.Persona != null ? c.Persona.NombreCompleto : c.CodigoCliente;
            return $"{nombre} ({c.CodigoCliente})".Trim();
        }

        if (p is Empresa e)
        {
            return e.RazonSocial;
        }

        return $"Participante #{p.Id}";
    }
}
