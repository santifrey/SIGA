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

        if (string.IsNullOrWhiteSpace(dto.IdentificacionFila))
            throw new ValidationException("La identificación de la fila es obligatoria.");

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

            // 5. Obtener o crear Fila
            var fila = await _filaRepository.GetByIdentificacionAsync(
                dto.SucursalId, 
                dto.TemporadaId, 
                dto.IdentificacionFila, 
                cancellationToken);

            if (fila == null)
            {
                fila = new Fila(dto.SucursalId, dto.TemporadaId, dto.IdentificacionFila, dto.CapacidadFila);
                await _filaRepository.AddAsync(fila, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // 6. Obtener o crear Plantacion
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

            // 7. Crear el Lote
            var orden = await _loteRepository.GetNextOrdenAsync(fila.Id, cancellationToken);
            var totalPlantado = dto.Detalles.Sum(d => d.Cantidad);

            var lote = new Lote(fila.Id, dto.ClienteId, dto.TemporadaId, orden)
            {
                CantidadPlantadaTotal = totalPlantado,
                Estado = EstadoLote.Plantado
            };

            await _loteRepository.AddAsync(lote, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 8. Crear Detalles de Plantación
            foreach (var det in dto.Detalles)
            {
                var detalle = new DetallePlantacion(plantacion.Id, lote.Id, det.Clase, det.Cantidad);
                lote.DetallesPlantacion.Add(detalle);
            }

            // Actualizar estado de fila
            fila.Estado = EstadoFila.Plantada;
            await _filaRepository.UpdateAsync(fila, cancellationToken);

            // 9. Registrar movimiento de salida del cliente hacia Empresa (Id: 1) para balance de stock
            var operacion = new OperacionComercial
            {
                Fecha = DateTime.UtcNow,
                Tipo = TipoOperacionComercial.AjusteStock,
                Estado = EstadoOperacion.Confirmada,
                UsuarioCreadorId = usuarioId,
                Observaciones = $"Plantación - Temporada {temporada.Nombre} - Fila {fila.Identificacion} - Lote #{lote.Id}"
            };

            await _operacionRepository.AddAsync(operacion, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var movimiento = new MovimientoBulbos(
                operacion.Id, 
                dto.ClienteId, 
                Empresa.SingletonId, // Destino Empresa Singleton
                usuarioId, 
                $"Salida por plantación en Fila {fila.Identificacion}, Lote #{lote.Id}");

            foreach (var det in dto.Detalles)
            {
                movimiento.AgregarCantidad(det.Clase, det.Cantidad);
            }

            movimiento.Confirmar();

            await _movimientoRepository.AddAsync(movimiento, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            var loteCompleto = await _loteRepository.GetByIdWithDetailsAsync(lote.Id, cancellationToken);
            return ApiResponse<LoteDto>.Created(MapLoteToDto(loteCompleto ?? lote), "Distribución de plantación registrada con éxito.");
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
