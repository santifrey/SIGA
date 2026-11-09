using SIGA.Application.Common;
using SIGA.Application.DTOs.Cosechas;
using SIGA.Application.Interfaces;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Interfaces;

namespace SIGA.Application.Services;

public class GestionCosechas : IGestionCosechas
{
    private readonly ICosechaRepository _cosechaRepository;
    private readonly ITemporadaRepository _temporadaRepository;
    private readonly ISucursalRepository _sucursalRepository;
    private readonly ILoteRepository _loteRepository;
    private readonly IFilaRepository _filaRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IRepository<ParticipanteComercial> _participanteRepository;
    private readonly IOperacionComercialRepository _operacionRepository;
    private readonly IMovimientoBulbosRepository _movimientoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GestionCosechas(
        ICosechaRepository cosechaRepository,
        ITemporadaRepository temporadaRepository,
        ISucursalRepository sucursalRepository,
        ILoteRepository loteRepository,
        IFilaRepository filaRepository,
        IClienteRepository clienteRepository,
        IRepository<ParticipanteComercial> participanteRepository,
        IOperacionComercialRepository operacionRepository,
        IMovimientoBulbosRepository movimientoRepository,
        IUnitOfWork unitOfWork)
    {
        _cosechaRepository = cosechaRepository;
        _temporadaRepository = temporadaRepository;
        _sucursalRepository = sucursalRepository;
        _loteRepository = loteRepository;
        _filaRepository = filaRepository;
        _clienteRepository = clienteRepository;
        _participanteRepository = participanteRepository;
        _operacionRepository = operacionRepository;
        _movimientoRepository = movimientoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<CosechaDto>> CrearCosechaAsync(
        CreateCosechaDto dto,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var temporada = await _temporadaRepository.GetByIdAsync(dto.TemporadaId, cancellationToken);
        if (temporada == null)
            throw new NotFoundException(nameof(Temporada), dto.TemporadaId);

        if (temporada.Estado == EstadoTemporada.Cerrada)
            throw new ValidationException("No se puede registrar una cosecha en una temporada cerrada.");

        var sucursal = await _sucursalRepository.GetByIdAsync(dto.SucursalId, cancellationToken);
        if (sucursal == null)
            throw new NotFoundException(nameof(Sucursal), dto.SucursalId);

        // Regla DP-7: Una sola cosecha por sucursal en cada temporada
        var existente = await _cosechaRepository.GetByTemporadaAndSucursalAsync(dto.TemporadaId, dto.SucursalId, cancellationToken);
        if (existente != null)
            throw new ValidationException($"Ya existe una cosecha registrada para la sucursal '{sucursal.Nombre}' en la temporada '{temporada.Nombre}'.");

        if (temporada.Estado == EstadoTemporada.EnCrecimiento || temporada.Estado == EstadoTemporada.Plantacion)
        {
            temporada.IniciarCosecha();
            await _temporadaRepository.UpdateAsync(temporada, cancellationToken);
        }

        var cosecha = new Cosecha(dto.TemporadaId, dto.SucursalId, usuarioId, dto.Observaciones);
        await _cosechaRepository.AddAsync(cosecha, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var creada = await _cosechaRepository.GetByIdWithDetailsAsync(cosecha.Id, cancellationToken);
        var dtoResult = await MapToDtoAsync(creada ?? cosecha, cancellationToken);
        return ApiResponse<CosechaDto>.Created(dtoResult, "Cosecha iniciada exitosamente para la sucursal.");
    }

    public async Task<ApiResponse<CosechaDto>> RegistrarDetalleCosechaAsync(
        int cosechaId,
        RegistrarDetalleCosechaDto dto,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var cosecha = await _cosechaRepository.GetByIdWithDetailsAsync(cosechaId, cancellationToken);
        if (cosecha == null)
            throw new NotFoundException(nameof(Cosecha), cosechaId);

        if (cosecha.Estado == EstadoCosecha.Cerrada)
            throw new ValidationException("No se pueden registrar ni modificar cantidades en una cosecha cerrada.");

        if (dto.Cantidades == null || !dto.Cantidades.Any())
            throw new ValidationException("Debe especificar al menos una clase y cantidad cosechada para el lote.");

        if (dto.Cantidades.Any(c => c.Cantidad < 0))
            throw new ValidationException("Las cantidades cosechadas no pueden ser negativas.");

        var lote = await _loteRepository.GetByIdWithDetailsAsync(dto.LoteId, cancellationToken);
        if (lote == null)
            throw new NotFoundException(nameof(Lote), dto.LoteId);

        if (lote.TemporadaId != cosecha.TemporadaId)
            throw new ValidationException($"El lote #{lote.Id} no pertenece a la temporada de esta cosecha.");

        if (lote.Fila != null && lote.Fila.SucursalId != cosecha.SucursalId)
            throw new ValidationException($"El lote #{lote.Id} pertenece a otra sucursal y no puede registrarse en esta cosecha.");

        foreach (var item in dto.Cantidades)
        {
            cosecha.RegistrarDetalle(dto.LoteId, item.ClaseBulbo, item.Cantidad);
        }

        await _cosechaRepository.UpdateAsync(cosecha, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var actualizada = await _cosechaRepository.GetByIdWithDetailsAsync(cosechaId, cancellationToken);
        var dtoResult = await MapToDtoAsync(actualizada ?? cosecha, cancellationToken);
        return ApiResponse<CosechaDto>.Ok(dtoResult, "Cantidades cosechadas del lote registradas exitosamente.");
    }

    public async Task<ApiResponse<CosechaDto>> CerrarCosechaAsync(
        int cosechaId,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var cosecha = await _cosechaRepository.GetByIdWithDetailsAsync(cosechaId, cancellationToken);
        if (cosecha == null)
            throw new NotFoundException(nameof(Cosecha), cosechaId);

        if (cosecha.Estado == EstadoCosecha.Cerrada)
            throw new ValidationException("La cosecha de esta sucursal ya se encuentra cerrada.");

        var temporada = await _temporadaRepository.GetByIdAsync(cosecha.TemporadaId, cancellationToken);
        if (temporada == null)
            throw new NotFoundException(nameof(Temporada), cosecha.TemporadaId);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // 1. Cerrar la cosecha de la sucursal
            cosecha.Cerrar();
            await _cosechaRepository.UpdateAsync(cosecha, cancellationToken);

            // 2. Actualizar estado de Lotes y Filas de esta sucursal en la temporada
            var lotesTemporada = await _loteRepository.GetByTemporadaAsync(cosecha.TemporadaId, cancellationToken);
            var lotesSucursal = lotesTemporada
                .Where(l => l.Fila != null && l.Fila.SucursalId == cosecha.SucursalId)
                .ToList();

            foreach (var lote in lotesSucursal)
            {
                lote.Estado = EstadoLote.Cosechado;
                await _loteRepository.UpdateAsync(lote, cancellationToken);
            }

            var filasSucursal = await _filaRepository.GetBySucursalAndTemporadaAsync(
                cosecha.SucursalId,
                cosecha.TemporadaId,
                cancellationToken);

            foreach (var fila in filasSucursal)
            {
                fila.Estado = EstadoFila.Cosechada;
                await _filaRepository.UpdateAsync(fila, cancellationToken);
            }

            // 3. Agrupar cantidades cosechadas por Titular Vigente del Lote y acreditar su stock
            var detallesPositivos = cosecha.DetallesCosecha
                .Where(d => !d.IsDeleted && d.Cantidad > 0)
                .ToList();

            // Mapa de LoteId -> Lote con Transferencias cargadas
            var mapaLotes = lotesTemporada.ToDictionary(l => l.Id, l => l);

            var gruposPorTitular = detallesPositivos
                .GroupBy(d =>
                {
                    if (d.Lote != null)
                        return d.Lote.ResolverTitularVigenteId();
                    if (mapaLotes.TryGetValue(d.LoteId, out var loteRef))
                        return loteRef.ResolverTitularVigenteId();
                    return 0;
                })
                .Where(g => g.Key > 0)
                .ToList();

            foreach (var grupoTitular in gruposPorTitular)
            {
                var titularVigenteId = grupoTitular.Key;

                // Si el titular vigente es un Cliente (Id != 1), generamos operación de IngresoCosecha y movimiento confirmado (Empresa 1 -> TitularVigente)
                if (titularVigenteId != 1)
                {
                    var operacion = new OperacionComercial
                    {
                        Fecha = DateTime.UtcNow,
                        Tipo = TipoOperacionComercial.IngresoCosecha,
                        Estado = EstadoOperacion.Confirmada,
                        UsuarioCreadorId = usuarioId,
                        Observaciones = $"Ingreso por Cosecha #{cosecha.Id} - Temporada {temporada.Nombre} - Sucursal #{cosecha.SucursalId}"
                    };

                    await _operacionRepository.AddAsync(operacion, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    var movimiento = new MovimientoBulbos(
                        operacion.Id,
                        1, // Origen: Producción / Empresa Singleton
                        titularVigenteId, // Destino: Titular Vigente del Lote
                        usuarioId,
                        $"Acreditación de cosecha #{cosecha.Id} (Temporada {temporada.Nombre})");

                    var cantidadesPorClase = grupoTitular
                        .GroupBy(d => d.ClaseBulbo)
                        .Select(g => new { Clase = g.Key, Total = g.Sum(x => x.Cantidad) })
                        .Where(x => x.Total > 0);

                    foreach (var item in cantidadesPorClase)
                    {
                        movimiento.AgregarCantidad(item.Clase, item.Total);
                    }

                    movimiento.Confirmar();
                    await _movimientoRepository.AddAsync(movimiento, cancellationToken);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 4. Regla DP-7: Verificar si todas las sucursales con lotes plantados en la temporada ya cerraron su cosecha
            var sucursalesConLotes = lotesTemporada
                .Where(l => l.Fila != null)
                .Select(l => l.Fila.SucursalId)
                .Distinct()
                .ToList();

            if (!sucursalesConLotes.Contains(cosecha.SucursalId))
            {
                sucursalesConLotes.Add(cosecha.SucursalId);
            }

            var cosechasTemporada = await _cosechaRepository.GetByTemporadaAsync(cosecha.TemporadaId, cancellationToken);
            var sucursalesCosechadas = cosechasTemporada
                .Where(c => c.Estado == EstadoCosecha.Cerrada || c.Id == cosecha.Id)
                .Select(c => c.SucursalId)
                .Distinct()
                .ToHashSet();

            var todasLasSucursalesCosechadas = sucursalesConLotes.All(sucId => sucursalesCosechadas.Contains(sucId));
            if (todasLasSucursalesCosechadas)
            {
                if (temporada.Estado == EstadoTemporada.Plantacion || temporada.Estado == EstadoTemporada.EnCrecimiento)
                {
                    temporada.IniciarCosecha();
                }

                if (temporada.Estado == EstadoTemporada.Cosecha)
                {
                    temporada.HabilitarComercializacion();
                    await _temporadaRepository.UpdateAsync(temporada, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            var cosechaCerrada = await _cosechaRepository.GetByIdWithDetailsAsync(cosechaId, cancellationToken);
            var dtoResult = await MapToDtoAsync(cosechaCerrada ?? cosecha, cancellationToken);
            return ApiResponse<CosechaDto>.Ok(dtoResult, "Cosecha cerrada exitosamente y tenencias acreditadas a los titulares vigentes.");
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ApiResponse<IEnumerable<CosechaDto>>> ListarPorTemporadaAsync(
        int temporadaId,
        CancellationToken cancellationToken = default)
    {
        var temporada = await _temporadaRepository.GetByIdAsync(temporadaId, cancellationToken);
        if (temporada == null)
            throw new NotFoundException(nameof(Temporada), temporadaId);

        var cosechas = await _cosechaRepository.GetByTemporadaAsync(temporadaId, cancellationToken);
        var dtos = new List<CosechaDto>();
        foreach (var c in cosechas)
        {
            dtos.Add(await MapToDtoAsync(c, cancellationToken));
        }

        return ApiResponse<IEnumerable<CosechaDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<CosechaDto>> ObtenerPorIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var cosecha = await _cosechaRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (cosecha == null)
            throw new NotFoundException(nameof(Cosecha), id);

        var dto = await MapToDtoAsync(cosecha, cancellationToken);
        return ApiResponse<CosechaDto>.Ok(dto);
    }

    private async Task<CosechaDto> MapToDtoAsync(Cosecha c, CancellationToken cancellationToken)
    {
        var detallesDto = new List<DetalleCosechaDto>();

        if (c.DetallesCosecha != null)
        {
            foreach (var d in c.DetallesCosecha.Where(x => !x.IsDeleted))
            {
                var lote = d.Lote ?? await _loteRepository.GetByIdWithDetailsAsync(d.LoteId, cancellationToken);
                var titularOriginalId = lote?.ClienteId ?? 0;
                var titularVigenteId = lote?.ResolverTitularVigenteId() ?? titularOriginalId;

                var nombreOriginal = await ResolverNombreParticipanteAsync(titularOriginalId, cancellationToken);
                var nombreVigente = titularVigenteId == titularOriginalId
                    ? nombreOriginal
                    : await ResolverNombreParticipanteAsync(titularVigenteId, cancellationToken);

                detallesDto.Add(new DetalleCosechaDto
                {
                    Id = d.Id,
                    CosechaId = d.CosechaId,
                    LoteId = d.LoteId,
                    IdentificacionFila = lote?.Fila?.Identificacion ?? string.Empty,
                    TitularOriginalId = titularOriginalId,
                    NombreTitularOriginal = nombreOriginal,
                    TitularVigenteId = titularVigenteId,
                    NombreTitularVigente = nombreVigente,
                    ClaseBulbo = d.ClaseBulbo,
                    Cantidad = d.Cantidad
                });
            }
        }

        return new CosechaDto
        {
            Id = c.Id,
            TemporadaId = c.TemporadaId,
            NombreTemporada = c.Temporada?.Nombre ?? string.Empty,
            SucursalId = c.SucursalId,
            NombreSucursal = c.Sucursal?.Nombre ?? string.Empty,
            FechaInicio = c.FechaInicio,
            FechaFin = c.FechaFin,
            Estado = c.Estado,
            UsuarioCreadorId = c.UsuarioCreadorId,
            Observaciones = c.Observaciones,
            TotalCosechado = c.CalcularTotalCosechado(),
            Detalles = detallesDto,
            FechaCreacion = c.FechaCreacion
        };
    }

    private async Task<string> ResolverNombreParticipanteAsync(int participanteId, CancellationToken cancellationToken)
    {
        if (participanteId <= 0)
            return string.Empty;

        var cliente = await _clienteRepository.GetByIdWithPersonaAsync(participanteId, cancellationToken);
        if (cliente != null)
        {
            return cliente.Persona != null ? cliente.Persona.NombreCompleto : cliente.CodigoCliente;
        }

        var participante = await _participanteRepository.GetByIdAsync(participanteId, cancellationToken);
        if (participante is Empresa empresa)
        {
            return empresa.RazonSocial;
        }

        return $"Participante #{participanteId}";
    }
}
