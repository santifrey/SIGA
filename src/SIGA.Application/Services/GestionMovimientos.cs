using SIGA.Application.Common;
using SIGA.Application.DTOs.Movimientos;
using SIGA.Application.Interfaces;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Exceptions;
using SIGA.Domain.Interfaces;

namespace SIGA.Application.Services;

public class GestionMovimientos : IGestionMovimientos
{
    private readonly IMovimientoBulbosRepository _movimientoRepository;
    private readonly IOperacionComercialRepository _operacionRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IRepository<ParticipanteComercial> _participanteRepository;
    private readonly IGestionStock _gestionStock;
    private readonly IUnitOfWork _unitOfWork;

    public GestionMovimientos(
        IMovimientoBulbosRepository movimientoRepository,
        IOperacionComercialRepository operacionRepository,
        IClienteRepository clienteRepository,
        IRepository<ParticipanteComercial> participanteRepository,
        IGestionStock gestionStock,
        IUnitOfWork unitOfWork)
    {
        _movimientoRepository = movimientoRepository;
        _operacionRepository = operacionRepository;
        _clienteRepository = clienteRepository;
        _participanteRepository = participanteRepository;
        _gestionStock = gestionStock;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<MovimientoBulbosDto>> CrearMovimientoAsync(
        CrearMovimientoDto dto, 
        int usuarioCreadorId, 
        CancellationToken cancellationToken = default)
    {
        if (dto.Cantidades == null || !dto.Cantidades.Any())
            throw new ValidationException("Debe ingresar al menos una cantidad de bulbos.");

        if (dto.Cantidades.Any(c => c.Cantidad <= 0))
            throw new ValidationException("Todas las cantidades de bulbos deben ser mayores a cero.");

        if (dto.OrigenId <= 0 || dto.DestinoId <= 0)
            throw new ValidationException("El origen y el destino deben ser identificadores válidos.");

        if (dto.OrigenId == dto.DestinoId)
            throw new OperacionInvalidaException("El participante Origen no puede ser igual al participante Destino.");

        var existeOrigen = await _participanteRepository.ExistsAsync(p => p.Id == dto.OrigenId, cancellationToken);
        if (!existeOrigen)
            throw new NotFoundException($"No se encontró el participante origen con ID {dto.OrigenId}.");

        var existeDestino = await _participanteRepository.ExistsAsync(p => p.Id == dto.DestinoId, cancellationToken);
        if (!existeDestino)
            throw new NotFoundException($"No se encontró el participante destino con ID {dto.DestinoId}.");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            int operacionId;
            if (dto.OperacionComercialId.HasValue && dto.OperacionComercialId.Value > 0)
            {
                var operacionExistente = await _operacionRepository.GetByIdAsync(dto.OperacionComercialId.Value, cancellationToken);
                if (operacionExistente == null)
                    throw new NotFoundException(nameof(OperacionComercial), dto.OperacionComercialId.Value);

                operacionId = operacionExistente.Id;
            }
            else
            {
                var nuevaOperacion = new OperacionComercial
                {
                    Fecha = DateTime.UtcNow,
                    Tipo = dto.TipoOperacion,
                    Estado = EstadoOperacion.Pendiente,
                    UsuarioCreadorId = usuarioCreadorId,
                    Observaciones = dto.Observaciones
                };

                await _operacionRepository.AddAsync(nuevaOperacion, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                operacionId = nuevaOperacion.Id;
            }

            var movimiento = new MovimientoBulbos(
                operacionId, 
                dto.OrigenId, 
                dto.DestinoId, 
                usuarioCreadorId, 
                dto.Observaciones);

            foreach (var cant in dto.Cantidades)
            {
                movimiento.AgregarCantidad(cant.Clase, cant.Cantidad);
            }

            await _movimientoRepository.AddAsync(movimiento, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            var resultado = await _movimientoRepository.GetByIdWithDetailsAsync(movimiento.Id, cancellationToken);
            return ApiResponse<MovimientoBulbosDto>.Created(MapToDto(resultado ?? movimiento), "Movimiento registrado en estado Pendiente.");
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ApiResponse<MovimientoBulbosDto>> ConfirmarMovimientoAsync(
        int movimientoId, 
        int usuarioConfirmadorId, 
        CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var movimiento = await _movimientoRepository.GetByIdWithDetailsAsync(movimientoId, cancellationToken);
            if (movimiento == null)
                throw new NotFoundException(nameof(MovimientoBulbos), movimientoId);

            if (movimiento.Estado != EstadoMovimiento.Pendiente)
                throw new OperacionInvalidaException($"No se puede confirmar un movimiento en estado '{movimiento.Estado}'. Solo se pueden confirmar movimientos en estado 'Pendiente'.");

            // Si el Origen es un Cliente, validar disponibilidad suficiente de stock dentro de la transacción
            var clienteOrigen = await _clienteRepository.GetByIdAsync(movimiento.OrigenId, cancellationToken);
            if (clienteOrigen != null)
            {
                var saldosDisponibles = await _gestionStock.ObtenerSaldoPorClaseAsync(movimiento.OrigenId, cancellationToken);

                foreach (var cant in movimiento.Cantidades)
                {
                    var saldoActual = saldosDisponibles.TryGetValue(cant.ClaseBulbo, out var stock) ? stock : 0;
                    if (saldoActual < cant.Cantidad)
                    {
                        throw new StockInsuficienteException(
                            movimiento.OrigenId, 
                            (int)cant.ClaseBulbo, 
                            cant.Cantidad, 
                            saldoActual);
                    }
                }
            }

            movimiento.Confirmar();
            await _movimientoRepository.UpdateAsync(movimiento, cancellationToken);

            if (movimiento.OperacionComercial != null)
            {
                await _operacionRepository.UpdateAsync(movimiento.OperacionComercial, cancellationToken);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return ApiResponse<MovimientoBulbosDto>.Ok(MapToDto(movimiento), "Movimiento confirmado exitosamente.");
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ApiResponse<MovimientoBulbosDto>> AnularMovimientoAsync(
        int movimientoId, 
        string motivo, 
        int usuarioId, 
        CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var movimiento = await _movimientoRepository.GetByIdWithDetailsAsync(movimientoId, cancellationToken);
            if (movimiento == null)
                throw new NotFoundException(nameof(MovimientoBulbos), movimientoId);

            if (movimiento.Estado == EstadoMovimiento.Anulado)
                throw new OperacionInvalidaException("El movimiento ya se encuentra anulado.");

            // Si el movimiento estaba Confirmado y el Destino es un Cliente, validar que anular la entrada no deje saldo negativo
            if (movimiento.Estado == EstadoMovimiento.Confirmado)
            {
                var clienteDestino = await _clienteRepository.GetByIdAsync(movimiento.DestinoId, cancellationToken);
                if (clienteDestino != null)
                {
                    var saldosDestino = await _gestionStock.ObtenerSaldoPorClaseAsync(movimiento.DestinoId, cancellationToken);

                    foreach (var cant in movimiento.Cantidades)
                    {
                        var saldoActual = saldosDestino.TryGetValue(cant.ClaseBulbo, out var stock) ? stock : 0;
                        if (saldoActual - cant.Cantidad < 0)
                        {
                            throw new OperacionInvalidaException(
                                $"No se puede anular el movimiento: el participante destino quedaría con saldo negativo ({saldoActual - cant.Cantidad}) en la Clase {(int)cant.ClaseBulbo}.");
                        }
                    }
                }
            }

            movimiento.Anular(motivo);
            await _movimientoRepository.UpdateAsync(movimiento, cancellationToken);

            if (movimiento.OperacionComercial != null)
            {
                await _operacionRepository.UpdateAsync(movimiento.OperacionComercial, cancellationToken);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return ApiResponse<MovimientoBulbosDto>.Ok(MapToDto(movimiento), "Movimiento anulado exitosamente.");
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ApiResponse<IEnumerable<MovimientoBulbosDto>>> ListarMovimientosAsync(
        EstadoMovimiento? estado = null, 
        CancellationToken cancellationToken = default)
    {
        var movimientos = await _movimientoRepository.GetAllWithDetailsAsync(estado, cancellationToken);
        var dtos = movimientos.Select(MapToDto);
        return ApiResponse<IEnumerable<MovimientoBulbosDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<MovimientoBulbosDto>> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var movimiento = await _movimientoRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (movimiento == null)
            throw new NotFoundException(nameof(MovimientoBulbos), id);

        return ApiResponse<MovimientoBulbosDto>.Ok(MapToDto(movimiento));
    }

    public async Task<ApiResponse<IEnumerable<MovimientoBulbosDto>>> ObtenerPorClienteAsync(
        int clienteId, 
        CancellationToken cancellationToken = default)
    {
        var movimientos = await _movimientoRepository.GetByParticipanteAsync(clienteId, cancellationToken);
        var dtos = movimientos.Select(MapToDto);
        return ApiResponse<IEnumerable<MovimientoBulbosDto>>.Ok(dtos);
    }

    private static MovimientoBulbosDto MapToDto(MovimientoBulbos m)
    {
        return new MovimientoBulbosDto
        {
            Id = m.Id,
            OperacionComercialId = m.OperacionComercialId,
            Fecha = m.Fecha,
            OrigenId = m.OrigenId,
            OrigenNombre = ObtenerNombreParticipante(m.Origen),
            DestinoId = m.DestinoId,
            DestinoNombre = ObtenerNombreParticipante(m.Destino),
            UsuarioCreadorId = m.UsuarioCreadorId,
            Estado = m.Estado,
            Observaciones = m.Observaciones,
            TotalBulbos = m.CalcularTotal(),
            Cantidades = m.Cantidades.Select(c => new CantidadBulbosDto
            {
                Clase = c.ClaseBulbo,
                Cantidad = c.Cantidad
            }).ToList()
        };
    }

    private static string ObtenerNombreParticipante(ParticipanteComercial? p)
    {
        if (p == null)
            return string.Empty;

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
