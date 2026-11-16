using SIGA.Application.Common;
using SIGA.Application.DTOs.Lotes;
using SIGA.Application.DTOs.Plantaciones;
using SIGA.Application.Interfaces;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Interfaces;

namespace SIGA.Application.Services;

public class GestionPlantaciones : IGestionPlantaciones
{
    private readonly ITemporadaRepository _temporadaRepository;
    private readonly IFilaRepository _filaRepository;
    private readonly ILoteRepository _loteRepository;
    private readonly IPlantacionRepository _plantacionRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly ISucursalRepository _sucursalRepository;
    private readonly IGestionStock _gestionStock;
    private readonly IOperacionComercialRepository _operacionRepository;
    private readonly IMovimientoBulbosRepository _movimientoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GestionPlantaciones(
        ITemporadaRepository temporadaRepository,
        IFilaRepository filaRepository,
        ILoteRepository loteRepository,
        IPlantacionRepository plantacionRepository,
        IClienteRepository clienteRepository,
        ISucursalRepository sucursalRepository,
        IGestionStock gestionStock,
        IOperacionComercialRepository operacionRepository,
        IMovimientoBulbosRepository movimientoRepository,
        IUnitOfWork unitOfWork)
    {
        _temporadaRepository = temporadaRepository;
        _filaRepository = filaRepository;
        _loteRepository = loteRepository;
        _plantacionRepository = plantacionRepository;
        _clienteRepository = clienteRepository;
        _sucursalRepository = sucursalRepository;
        _gestionStock = gestionStock;
        _operacionRepository = operacionRepository;
        _movimientoRepository = movimientoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<PlantacionDto>> CrearPlantacionAsync(
        CreatePlantacionDto dto, 
        int usuarioId, 
        CancellationToken cancellationToken = default)
    {
        var temporada = await _temporadaRepository.GetByIdAsync(dto.TemporadaId, cancellationToken);
        if (temporada == null)
            throw new NotFoundException(nameof(Temporada), dto.TemporadaId);

        if (temporada.Estado == EstadoTemporada.Cerrada)
            throw new ValidationException("No se puede crear una plantación para una temporada cerrada.");

        var plantacion = new Plantacion(dto.TemporadaId, usuarioId, dto.Observaciones)
        {
            Estado = EstadoPlantacion.EnProceso
        };

        if (temporada.Estado == EstadoTemporada.Planificada)
        {
            temporada.IniciarPlantacion();
            await _temporadaRepository.UpdateAsync(temporada, cancellationToken);
        }

        await _plantacionRepository.AddAsync(plantacion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var creada = await _plantacionRepository.GetByIdWithDetailsAsync(plantacion.Id, cancellationToken);
        return ApiResponse<PlantacionDto>.Created(MapToDto(creada ?? plantacion), "Plantación creada exitosamente.");
    }

    public async Task<ApiResponse<LoteDto>> RegistrarDistribucionAsync(
        RegistrarDistribucionDto dto, 
        int usuarioId, 
        CancellationToken cancellationToken = default)
    {
        if (dto.Detalles == null || !dto.Detalles.Any())
            throw new ValidationException("Debe ingresar al menos una clase y cantidad de bulbos a plantar.");

        if (dto.Detalles.Any(d => d.Cantidad <= 0))
            throw new ValidationException("Todas las cantidades de bulbos a plantar deben ser mayores a cero.");

        if (dto.CapacidadFila < 0)
            throw new ValidationException("La capacidad de la fila no puede ser negativa.");

        if (dto.NumeroFila.HasValue && dto.NumeroFila.Value <= 0)
            throw new ValidationException("El número de fila debe ser un entero mayor a cero.");

        if (dto.NumeroLote.HasValue && dto.NumeroLote.Value <= 0)
            throw new ValidationException("El orden/número de lote debe ser un entero mayor a cero.");

        bool hasFilaId = dto.FilaId.HasValue && dto.FilaId.Value > 0;
        bool hasNumeroFila = dto.NumeroFila.HasValue && dto.NumeroFila.Value > 0;
        bool hasIdentificacion = !string.IsNullOrWhiteSpace(dto.IdentificacionFila);

        if (!hasFilaId && !hasNumeroFila && !hasIdentificacion)
            throw new ValidationException("Debe especificar la fila mediante FilaId, NumeroFila o IdentificacionFila.");

        if (string.IsNullOrWhiteSpace(dto.IdentificacionFila) && hasNumeroFila)
            dto.IdentificacionFila = dto.NumeroFila!.Value.ToString();

        // 1. Validar Cliente
        var cliente = await _clienteRepository.GetByIdWithPersonaAsync(dto.ClienteId, cancellationToken);
        if (cliente == null)
            throw new NotFoundException(nameof(Cliente), dto.ClienteId);

        // 2. Validar Temporada
        var temporada = await _temporadaRepository.GetByIdAsync(dto.TemporadaId, cancellationToken);
        if (temporada == null)
            throw new NotFoundException(nameof(Temporada), dto.TemporadaId);

        if (temporada.Estado == EstadoTemporada.Cerrada)
            throw new ValidationException("No se puede registrar distribución en una temporada cerrada.");

        // 3. Validar Sucursal
        var sucursal = await _sucursalRepository.GetByIdAsync(dto.SucursalId, cancellationToken);
        if (sucursal == null)
            throw new NotFoundException(nameof(Sucursal), dto.SucursalId);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // 4. Validar disponibilidad de stock del cliente para cada clase dentro de la transacción
            foreach (var det in dto.Detalles)
            {
                var dispResult = await _gestionStock.VerificarDisponibilidadAsync(dto.ClienteId, det.Clase, det.Cantidad, cancellationToken);
                if (dispResult == null || !dispResult.Success || dispResult.Data == null || !dispResult.Data.Disponible)
                {
                    var disp = dispResult?.Data?.StockDisponible ?? 0;
                    throw new ValidationException(
                        $"Stock insuficiente para el cliente {dto.ClienteId} en {det.Clase}. Disponible: {disp}, Requerido: {det.Cantidad}.");
                }
            }

            // 5. Obtener o crear Fila estructurada
            Fila? fila = null;

            if (hasFilaId)
            {
                fila = await _filaRepository.GetByIdWithLotesAsync(dto.FilaId!.Value, cancellationToken);
                if (fila == null)
                    throw new NotFoundException(nameof(Fila), dto.FilaId.Value);

                if (fila.SucursalId != dto.SucursalId || fila.TemporadaId != dto.TemporadaId)
                    throw new ValidationException("La fila seleccionada no corresponde a la sucursal o temporada indicada.");
            }
            else if (hasNumeroFila)
            {
                var numStr = dto.NumeroFila!.Value.ToString();
                fila = await _filaRepository.GetByNumeroAsync(dto.SucursalId, dto.TemporadaId, dto.NumeroFila.Value, cancellationToken);
                if (fila == null)
                {
                    fila = await _filaRepository.GetByIdentificacionAsync(dto.SucursalId, dto.TemporadaId, numStr, cancellationToken);
                }

                if (fila == null)
                {
                    fila = new Fila(dto.SucursalId, dto.TemporadaId, dto.NumeroFila.Value, dto.CapacidadFila);
                    await _filaRepository.AddAsync(fila, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }
            else
            {
                var ident = dto.IdentificacionFila.Trim();
                fila = await _filaRepository.GetByIdentificacionAsync(dto.SucursalId, dto.TemporadaId, ident, cancellationToken);
                if (fila == null)
                {
                    fila = new Fila(dto.SucursalId, dto.TemporadaId, ident, dto.CapacidadFila);
                    await _filaRepository.AddAsync(fila, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }

            // 6. Validar Capacidad de la Fila
            var totalPlantado = dto.Detalles.Sum(d => d.Cantidad);
            var ocupacionActual = fila.Lotes?.Where(l => !l.IsDeleted).Sum(l => l.CantidadPlantadaTotal) ?? 0;

            if (fila.Capacidad > 0 && (ocupacionActual + totalPlantado > fila.Capacidad))
            {
                var disponible = Math.Max(0, fila.Capacidad - ocupacionActual);
                throw new ValidationException(
                    $"La cantidad total de bulbos ({ocupacionActual + totalPlantado}) supera la capacidad máxima de la fila ({fila.Capacidad}). Capacidad disponible: {disponible}.");
            }

            // 7. Resolver y Validar Orden / Número de Lote
            int orden;
            if (dto.NumeroLote.HasValue && dto.NumeroLote.Value > 0)
            {
                orden = dto.NumeroLote.Value;
                var existeOrden = (fila.Lotes != null && fila.Lotes.Any(l => !l.IsDeleted && l.Orden == orden)) ||
                                  await _loteRepository.ExisteOrdenEnFilaAsync(fila.Id, orden, cancellationToken);
                if (existeOrden)
                {
                    throw new ValidationException($"El orden de lote {orden} ya se encuentra ocupado en la fila '{fila.Identificacion}'.");
                }
            }
            else
            {
                orden = await _loteRepository.GetNextOrdenAsync(fila.Id, cancellationToken);
            }

            // 8. Obtener o crear Plantacion
            Plantacion? plantacion = null;
            if (dto.PlantacionId.HasValue && dto.PlantacionId.Value > 0)
            {
                plantacion = await _plantacionRepository.GetByIdAsync(dto.PlantacionId.Value, cancellationToken);
            }

            if (plantacion == null)
            {
                plantacion = await _plantacionRepository.GetByTemporadaIdAsync(dto.TemporadaId, cancellationToken);
            }

            if (plantacion == null)
            {
                plantacion = new Plantacion(dto.TemporadaId, usuarioId, $"Plantación automática - Temporada {temporada.Nombre}")
                {
                    Estado = EstadoPlantacion.EnProceso
                };

                if (temporada.Estado == EstadoTemporada.Planificada)
                {
                    temporada.IniciarPlantacion();
                    await _temporadaRepository.UpdateAsync(temporada, cancellationToken);
                }

                await _plantacionRepository.AddAsync(plantacion, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // 9. Crear el Lote
            var lote = new Lote(fila.Id, dto.ClienteId, dto.TemporadaId, orden)
            {
                CantidadPlantadaTotal = totalPlantado,
                Estado = EstadoLote.Plantado
            };

            await _loteRepository.AddAsync(lote, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 10. Crear Detalles de Plantación
            foreach (var det in dto.Detalles)
            {
                var detalle = new DetallePlantacion(plantacion.Id, lote.Id, det.Clase, det.Cantidad);
                lote.DetallesPlantacion.Add(detalle);
            }

            // Actualizar estado de fila
            fila.Estado = EstadoFila.Plantada;
            await _filaRepository.UpdateAsync(fila, cancellationToken);

            // 11. Registrar movimiento de salida del cliente hacia Empresa (Id: 1) para balance de stock
            var operacion = new OperacionComercial
            {
                Fecha = DateTime.UtcNow,
                Tipo = TipoOperacionComercial.AjusteStock,
                Estado = EstadoOperacion.Confirmada,
                UsuarioCreadorId = usuarioId,
                Observaciones = $"Plantación - Temporada {temporada.Nombre} - Fila {fila.Identificacion} - Lote #{lote.Id} (Orden {orden})"
            };

            await _operacionRepository.AddAsync(operacion, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            int destinoId = dto.ClienteId == Empresa.SingletonId
                ? (dto.SucursalId != Empresa.SingletonId && dto.SucursalId > 0 ? dto.SucursalId : 99998)
                : Empresa.SingletonId;

            var movimiento = new MovimientoBulbos(
                operacion.Id, 
                dto.ClienteId, 
                destinoId, 
                usuarioId, 
                $"Salida por plantación en Fila {fila.Identificacion}, Lote #{lote.Id} (Orden {orden})");

            foreach (var det in dto.Detalles)
            {
                movimiento.AgregarCantidad(det.Clase, det.Cantidad);
            }

            movimiento.Confirmar();

            await _movimientoRepository.AddAsync(movimiento, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            var loteCompleto = await _loteRepository.GetByIdWithDetailsAsync(lote.Id, cancellationToken);
            var loteParaDto = loteCompleto ?? lote;
            loteParaDto.Fila ??= fila;
            return ApiResponse<LoteDto>.Created(MapLoteToDto(loteParaDto), "Distribución de plantación registrada con éxito.");
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ApiResponse<PlantacionDto>> ObtenerPorTemporadaAsync(
        int temporadaId, 
        CancellationToken cancellationToken = default)
    {
        var plantacion = await _plantacionRepository.GetByTemporadaIdAsync(temporadaId, cancellationToken);
        if (plantacion == null)
            throw new NotFoundException($"No existe plantación para la temporada con ID {temporadaId}.");

        return ApiResponse<PlantacionDto>.Ok(MapToDto(plantacion));
    }

    private static PlantacionDto MapToDto(Plantacion p)
    {
        return new PlantacionDto
        {
            Id = p.Id,
            TemporadaId = p.TemporadaId,
            NombreTemporada = p.Temporada?.Nombre ?? string.Empty,
            FechaInicio = p.FechaInicio,
            FechaFin = p.FechaFin,
            Estado = p.Estado,
            UsuarioCreadorId = p.UsuarioCreadorId,
            Observaciones = p.Observaciones,
            TotalBulbos = p.DetallesPlantacion?.Sum(d => d.Cantidad) ?? 0,
            Detalles = p.DetallesPlantacion?.Select(d => new DetallePlantacionDto
            {
                Id = d.Id,
                PlantacionId = d.PlantacionId,
                LoteId = d.LoteId,
                ClaseBulbo = d.ClaseBulbo,
                Cantidad = d.Cantidad
            }).ToList() ?? new List<DetallePlantacionDto>(),
            FechaCreacion = p.FechaCreacion
        };
    }

    private static LoteDto MapLoteToDto(Lote l)
    {
        return new LoteDto
        {
            Id = l.Id,
            FilaId = l.FilaId,
            IdentificacionFila = l.Fila?.Identificacion ?? string.Empty,
            SucursalId = l.Fila?.SucursalId ?? 0,
            NombreSucursal = l.Fila?.Sucursal?.Nombre ?? string.Empty,
            ClienteId = l.ClienteId,
            CodigoCliente = l.Cliente?.CodigoCliente ?? string.Empty,
            NombreCliente = l.Cliente?.Persona != null ? l.Cliente.Persona.NombreCompleto : string.Empty,
            TemporadaId = l.TemporadaId,
            NombreTemporada = l.Temporada?.Nombre ?? string.Empty,
            Orden = l.Orden,
            CantidadPlantadaTotal = l.CantidadPlantadaTotal,
            Estado = l.Estado,
            Detalles = l.DetallesPlantacion?.Select(d => new DetallePlantacionDto
            {
                Id = d.Id,
                PlantacionId = d.PlantacionId,
                LoteId = d.LoteId,
                ClaseBulbo = d.ClaseBulbo,
                Cantidad = d.Cantidad
            }).ToList() ?? new List<DetallePlantacionDto>(),
            FechaCreacion = l.FechaCreacion
        };
    }
}
