using SIGA.Application.Common;
using SIGA.Application.DTOs.MovimientosComerciales;
using SIGA.Application.Interfaces;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Exceptions;
using SIGA.Domain.Interfaces;

namespace SIGA.Application.Services;

public class GestionMovimientosComerciales : IGestionMovimientosComerciales
{
    private readonly IMovimientoComercialRepository _movimientoRepository;
    private readonly IPrecioClaseRepository _precioClaseRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly ITemporadaRepository _temporadaRepository;
    private readonly IRepository<ParticipanteComercial> _participanteRepository;
    private readonly IGestionStock _gestionStock;
    private readonly IMovimientoBulbosRepository _movimientoBulbosRepository;
    private readonly ISucursalRepository? _sucursalRepository;
    private readonly ICurrentUserService? _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public GestionMovimientosComerciales(
        IMovimientoComercialRepository movimientoRepository,
        IPrecioClaseRepository precioClaseRepository,
        IClienteRepository clienteRepository,
        ITemporadaRepository temporadaRepository,
        IRepository<ParticipanteComercial> participanteRepository,
        IGestionStock gestionStock,
        IMovimientoBulbosRepository movimientoBulbosRepository,
        IUnitOfWork unitOfWork,
        ISucursalRepository? sucursalRepository = null,
        ICurrentUserService? currentUserService = null)
    {
        _movimientoRepository = movimientoRepository;
        _precioClaseRepository = precioClaseRepository;
        _clienteRepository = clienteRepository;
        _temporadaRepository = temporadaRepository;
        _participanteRepository = participanteRepository;
        _gestionStock = gestionStock;
        _movimientoBulbosRepository = movimientoBulbosRepository;
        _unitOfWork = unitOfWork;
        _sucursalRepository = sucursalRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<MovimientoComercialDto>> CrearMovimientoAsync(
        CrearMovimientoComercialDto dto,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        if (dto.Detalles == null || !dto.Detalles.Any())
            throw new ValidationException("Debe incluir al menos un ítem de detalle.");

        if (dto.OrigenParticipanteId <= 0)
            throw new ValidationException("El participante origen es requerido.");
        if (dto.DestinoParticipanteId <= 0)
            throw new ValidationException("El participante destino es requerido.");
        if (dto.OrigenParticipanteId == dto.DestinoParticipanteId)
            throw new ValidationException("El participante origen y destino no pueden ser el mismo.");

        if (dto.Tipo == TipoMovimientoComercial.CompraACliente && (dto.PorcentajeComision < 0 || dto.PorcentajeComision > 100))
            throw new ValidationException("El porcentaje de comisión debe estar entre 0 y 100.");

        foreach (var item in dto.Detalles)
        {
            if (item.Cantidad <= 0)
                throw new ValidationException("La cantidad debe ser mayor a cero.");

            if (item.ClaseBulbo == ClaseBulbo.MIX && item.Cantidad % 3 != 0)
                throw new ValidationException($"La cantidad para ítems MIX debe ser un múltiplo de 3 (recibido: {item.Cantidad}).");
        }

        await ValidarParticipanteActivoAsync(dto.OrigenParticipanteId, "origen", cancellationToken);
        await ValidarParticipanteActivoAsync(dto.DestinoParticipanteId, "destino", cancellationToken);

        if (_currentUserService != null)
        {
            var alcanceCrear = await _currentUserService.ObtenerAlcanceAsync("MOVCOM_CREAR", cancellationToken);
            if (alcanceCrear == AlcancePermiso.Ninguno)
                throw new ForbiddenException("No posee permisos para registrar movimientos comerciales.");

            if (alcanceCrear == AlcancePermiso.Sucursal)
            {
                var userSucursalId = await _currentUserService.GetSucursalIdAsync(cancellationToken);
                if (userSucursalId.HasValue && dto.OrigenParticipanteId != userSucursalId.Value && dto.DestinoParticipanteId != userSucursalId.Value)
                    throw new ForbiddenException("Solo posee permisos para registrar operaciones comerciales que involucren a su propia sucursal.");
            }
        }

        // Participant linkage defaults per type
        int? clienteId = dto.ClienteId;
        if (!clienteId.HasValue)
        {
            if (dto.Tipo == TipoMovimientoComercial.VentaACliente)
                clienteId = dto.DestinoParticipanteId;
            else if (dto.Tipo == TipoMovimientoComercial.CompraACliente)
                clienteId = dto.OrigenParticipanteId;
            else
            {
                // En traspasos, verificar si alguno de los participantes es un cliente
                var clienteOrigen = await _clienteRepository.GetByIdAsync(dto.OrigenParticipanteId, cancellationToken);
                if (clienteOrigen != null)
                {
                    clienteId = dto.OrigenParticipanteId;
                }
                else
                {
                    var clienteDestino = await _clienteRepository.GetByIdAsync(dto.DestinoParticipanteId, cancellationToken);
                    clienteId = clienteDestino?.Id;
                }
            }
        }

        var movimiento = new MovimientoComercial(
            dto.Tipo,
            usuarioId,
            dto.OrigenParticipanteId,
            dto.DestinoParticipanteId,
            clienteId,
            dto.TemporadaId,
            dto.PorcentajeComision,
            dto.Observaciones);

        foreach (var item in dto.Detalles)
        {
            decimal precioPactado = 0m;
            if (dto.Tipo == TipoMovimientoComercial.TraspasoEntreClientes)
            {
                precioPactado = 0m;
            }
            else
            {
                if (item.PrecioUnitarioPactado.HasValue && item.PrecioUnitarioPactado.Value > 0)
                {
                    precioPactado = item.PrecioUnitarioPactado.Value;
                }
                else if (dto.TemporadaId.HasValue)
                {
                    var refPrice = await _precioClaseRepository.GetByTemporadaAndClaseAsync(dto.TemporadaId.Value, item.ClaseBulbo, cancellationToken);
                    if (refPrice != null && refPrice.PrecioReferencia > 0)
                    {
                        precioPactado = refPrice.PrecioReferencia;
                    }
                    else
                    {
                        throw new ValidationException($"Debe especificar un precio pactado mayor a cero para {item.ClaseBulbo} o configurar su precio de referencia en la temporada.");
                    }
                }
                else
                {
                    throw new ValidationException($"Debe especificar un precio pactado mayor a cero para {item.ClaseBulbo} o especificar una temporada con precios configurados.");
                }
            }

            movimiento.AgregarDetalle(item.ClaseBulbo, item.Cantidad, precioPactado);
        }

        movimiento.RecalcularFinancieros();

        await _movimientoRepository.AddAsync(movimiento, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        bool requiereContrato = false;
        if (movimiento.Tipo == TipoMovimientoComercial.VentaACliente && clienteId.HasValue)
        {
            bool hasFinalized = await _movimientoRepository.HasFinalizedMovementsForClientAsync(clienteId.Value, movimiento.Id, cancellationToken);
            requiereContrato = !hasFinalized;

            // La sucursal del cliente debe ser por defecto la que haga la venta inicial obligatoria para generar el contrato
            if (requiereContrato && _sucursalRepository != null)
            {
                var cliente = await _clienteRepository.GetByIdWithPersonaAsync(clienteId.Value, cancellationToken);
                if (cliente?.Persona != null && !cliente.Persona.SucursalId.HasValue)
                {
                    var sucursalOrigen = await _sucursalRepository.GetByParticipanteComercialIdAsync(movimiento.OrigenParticipanteId, cancellationToken);
                    if (sucursalOrigen != null)
                    {
                        cliente.Persona.SucursalId = sucursalOrigen.Id;
                        await _clienteRepository.UpdateAsync(cliente, cancellationToken);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }
                }
            }
        }

        return ApiResponse<MovimientoComercialDto>.Created(MapToDto(movimiento, requiereContrato), "Movimiento comercial registrado exitosamente.");
    }

    public async Task<ApiResponse<MovimientoComercialDto>> AprobarAdminAsync(
        int id,
        AprobarAdminDto dto,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var mov = await _movimientoRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (mov == null)
            throw new NotFoundException(nameof(MovimientoComercial), id);

        if (mov.Estado != EstadoMovimientoComercial.PendienteRevision)
            throw new OperacionInvalidaException($"El movimiento comercial debe estar en estado PendienteRevision para ser aprobado (estado actual: '{mov.Estado}').");

        bool requiereContrato = false;
        if (mov.Tipo == TipoMovimientoComercial.VentaACliente)
        {
            int clienteId = mov.ClienteId ?? mov.DestinoParticipanteId;
            bool hasFinalized = await _movimientoRepository.HasFinalizedMovementsForClientAsync(clienteId, mov.Id, cancellationToken);
            requiereContrato = !hasFinalized;

            if (requiereContrato && (mov.Contrato == null || mov.Contrato.IsDeleted))
            {
                throw new OperacionInvalidaException("El cliente es nuevo y no registra operaciones previas finalizadas. Se requiere la generación de un contrato antes de la aprobación administrativa.");
            }
        }

        mov.AprobarAdmin(usuarioId, requiereContrato, dto?.Observaciones);

        await _movimientoRepository.UpdateAsync(mov, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await VincularContratoAClienteSiAplicaAsync(mov, cancellationToken);

        return ApiResponse<MovimientoComercialDto>.Ok(MapToDto(mov, requiereContrato), "Movimiento comercial aprobado por Administración.");
    }

    public async Task<ApiResponse<MovimientoComercialDto>> GenerarContratoAsync(
        int id,
        GenerarContratoDto dto,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var mov = await _movimientoRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (mov == null)
            throw new NotFoundException(nameof(MovimientoComercial), id);

        if (mov.Estado == EstadoMovimientoComercial.Finalizado)
            throw new OperacionInvalidaException("No se puede generar contrato para un movimiento ya finalizado.");

        if (string.IsNullOrWhiteSpace(dto.NumeroContrato))
            throw new ValidationException("El número de contrato es obligatorio.");

        mov.GenerarContrato(dto.NumeroContrato, usuarioId, dto.ArchivoUrl);

        await _movimientoRepository.UpdateAsync(mov, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await VincularContratoAClienteSiAplicaAsync(mov, cancellationToken);

        return ApiResponse<MovimientoComercialDto>.Ok(MapToDto(mov), "Contrato comercial generado exitosamente.");
    }

    public async Task<ApiResponse<MovimientoComercialDto>> FacturarAsync(
        int id,
        FacturarDto dto,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var mov = await _movimientoRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (mov == null)
            throw new NotFoundException(nameof(MovimientoComercial), id);

        if (mov.Tipo == TipoMovimientoComercial.TraspasoEntreClientes)
            throw new OperacionInvalidaException("Los traspasos entre clientes no generan facturas comerciales.");

        if (mov.Estado != EstadoMovimientoComercial.AprobadoAdmin && mov.Estado != EstadoMovimientoComercial.Rechazado)
            throw new OperacionInvalidaException($"El movimiento comercial debe estar en estado AprobadoAdmin o Rechazado para emitir factura (estado actual: '{mov.Estado}').");

        if (string.IsNullOrWhiteSpace(dto.NumeroFactura))
            throw new ValidationException("El número de factura es obligatorio.");

        if (mov.Factura != null)
        {
            // Re-invoicing an edited/resubmitted movement updates the existing voided invoice cleanly without unique index collisions
            mov.RecalcularFinancieros();
            mov.Factura.NumeroFactura = dto.NumeroFactura.Trim();
            mov.Factura.MontoTotal = mov.MontoTotal;
            mov.Factura.UsuarioEmisionId = usuarioId;
            mov.Factura.ArchivoUrl = dto.ArchivoUrl;
            mov.Factura.FechaEmision = DateTime.UtcNow;
            mov.Factura.Estado = EstadoFactura.Emitida;
            mov.Factura.IsDeleted = false;

            mov.Estado = EstadoMovimientoComercial.Facturado;
            mov.HistorialAprobaciones.Add(new RegistroAprobacion(
                mov.Id,
                usuarioId,
                AccionAprobacion.Facturado,
                dto.Observaciones ?? $"Factura {dto.NumeroFactura.Trim()} emitida"));
        }
        else
        {
            mov.EmitirFactura(dto.NumeroFactura, usuarioId, dto.ArchivoUrl, dto.Observaciones);
        }

        await _movimientoRepository.UpdateAsync(mov, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<MovimientoComercialDto>.Ok(MapToDto(mov), "Factura emitida exitosamente.");
    }

    public async Task<ApiResponse<MovimientoComercialDto>> FinalizarAsync(
        int id,
        FinalizarDto dto,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var mov = await _movimientoRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (mov == null)
            throw new NotFoundException(nameof(MovimientoComercial), id);

        if (mov.Tipo == TipoMovimientoComercial.TraspasoEntreClientes)
        {
            if (mov.Estado != EstadoMovimientoComercial.AprobadoAdmin)
                throw new OperacionInvalidaException($"Los traspasos deben estar en AprobadoAdmin para ser finalizados (estado actual: '{mov.Estado}').");
        }
        else
        {
            if (mov.Estado != EstadoMovimientoComercial.Facturado)
                throw new OperacionInvalidaException($"Las ventas y compras deben estar en estado Facturado para ser finalizadas (estado actual: '{mov.Estado}').");
        }

        // Validate origin stock availability with MIX decomposition
        var requerimientosFisicos = mov.ObtenerRequerimientosFisicosBulbos();
        var stockDisponible = await _gestionStock.ObtenerSaldoPorClaseAsync(mov.OrigenParticipanteId, cancellationToken);

        foreach (var (clase, requerido) in requerimientosFisicos.Where(r => r.Value > 0))
        {
            int disponible = stockDisponible.TryGetValue(clase, out var saldo) ? saldo : 0;
            if (disponible < requerido)
            {
                throw new ValidationException(
                    $"Stock insuficiente en el participante origen #{mov.OrigenParticipanteId} para {clase}. Disponible: {disponible}, Requerido: {requerido}.");
            }
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var movBulbos = new MovimientoBulbos(
                mov.OrigenParticipanteId,
                mov.DestinoParticipanteId,
                usuarioId,
                $"Entrega comercial #{mov.Id} ({mov.Tipo}) finalizada",
                movimientoComercialId: mov.Id);

            foreach (var (clase, cantidad) in requerimientosFisicos.Where(r => r.Value > 0))
            {
                movBulbos.AgregarCantidad(clase, cantidad);
            }
            movBulbos.Confirmar();
            await _movimientoBulbosRepository.AddAsync(movBulbos, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            mov.Finalizar(usuarioId, dto?.Observaciones);
            await _movimientoRepository.UpdateAsync(mov, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await VincularContratoAClienteSiAplicaAsync(mov, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return ApiResponse<MovimientoComercialDto>.Ok(MapToDto(mov), "Movimiento comercial finalizado exitosamente.");
    }

    public async Task<ApiResponse<MovimientoComercialDto>> RechazarAsync(
        int id,
        RechazarDto dto,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var mov = await _movimientoRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (mov == null)
            throw new NotFoundException(nameof(MovimientoComercial), id);

        if (mov.Estado == EstadoMovimientoComercial.Rechazado)
            throw new OperacionInvalidaException("El movimiento ya se encuentra en estado Rechazado.");

        if (string.IsNullOrWhiteSpace(dto.Motivo))
            throw new ValidationException("Debe indicar el motivo del rechazo u observación.");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (mov.Estado == EstadoMovimientoComercial.Finalizado)
            {
                var bulbosList = await _movimientoBulbosRepository.GetByMovimientoComercialIdAsync(mov.Id, cancellationToken);
                if (bulbosList != null)
                {
                    foreach (var item in bulbosList)
                    {
                        var entity = await _movimientoBulbosRepository.GetByIdAsync(item.Id, cancellationToken);
                        if (entity != null && entity.Estado == EstadoMovimiento.Confirmado)
                        {
                            entity.Anular($"Anulación por cancelación de operación comercial #{mov.Id}: {dto.Motivo.Trim()}");
                            await _movimientoBulbosRepository.UpdateAsync(entity, cancellationToken);
                        }
                    }
                }
            }

            if (mov.Contrato != null)
            {
                int? clienteId = mov.ClienteId ?? (mov.Tipo == TipoMovimientoComercial.VentaACliente ? mov.DestinoParticipanteId : null);
                if (clienteId.HasValue)
                {
                    var cliente = await _clienteRepository.GetByIdAsync(clienteId.Value, cancellationToken);
                    if (cliente != null && cliente.ContratoId == mov.Contrato.Id)
                    {
                        cliente.ContratoId = null;
                        await _clienteRepository.UpdateAsync(cliente, cancellationToken);
                    }
                }
            }

            mov.Rechazar(usuarioId, dto.Motivo);

            await _movimientoRepository.UpdateAsync(mov, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return ApiResponse<MovimientoComercialDto>.Ok(MapToDto(mov), "Movimiento comercial rechazado.");
    }

    public async Task<ApiResponse<MovimientoComercialDto>> EditarYReenviarAsync(
        int id,
        ActualizarMovimientoComercialDto dto,
        int usuarioId,
        bool tienePermisoCorregir,
        CancellationToken cancellationToken = default)
    {
        var mov = await _movimientoRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (mov == null)
            throw new NotFoundException(nameof(MovimientoComercial), id);

        if (mov.Estado != EstadoMovimientoComercial.Rechazado)
            throw new OperacionInvalidaException($"Solo se pueden modificar y reenviar movimientos en estado Rechazado (estado actual: '{mov.Estado}').");

        if (mov.UsuarioRegistroId != usuarioId && !tienePermisoCorregir)
            throw new ForbiddenException("No tiene permisos para corregir este movimiento comercial. Solo el creador o usuarios con permiso MOVCOM_CORREGIR pueden editarlo.");

        if (dto.Detalles == null || !dto.Detalles.Any())
            throw new ValidationException("Debe incluir al menos un ítem de detalle.");

        if (dto.PorcentajeComision < 0 || dto.PorcentajeComision > 100)
            throw new ReglaNegocioException("El porcentaje de comisión debe estar entre 0 y 100.");

        foreach (var item in dto.Detalles)
        {
            if (item.Cantidad <= 0)
                throw new ValidationException("La cantidad debe ser mayor a cero.");

            if (item.ClaseBulbo == ClaseBulbo.MIX && item.Cantidad % 3 != 0)
                throw new ValidationException($"La cantidad para ítems MIX debe ser un múltiplo de 3 (recibido: {item.Cantidad}).");
        }

        // Update participants
        await ValidarParticipanteActivoAsync(dto.OrigenParticipanteId, "origen", cancellationToken);
        await ValidarParticipanteActivoAsync(dto.DestinoParticipanteId, "destino", cancellationToken);

        if (_currentUserService != null)
        {
            var alcanceCrear = await _currentUserService.ObtenerAlcanceAsync("MOVCOM_CREAR", cancellationToken);
            if (alcanceCrear == AlcancePermiso.Ninguno)
                throw new ForbiddenException("No posee permisos para registrar movimientos comerciales.");

            if (alcanceCrear == AlcancePermiso.Sucursal)
            {
                var userSucursalId = await _currentUserService.GetSucursalIdAsync(cancellationToken);
                if (userSucursalId.HasValue && dto.OrigenParticipanteId != userSucursalId.Value && dto.DestinoParticipanteId != userSucursalId.Value)
                    throw new ForbiddenException("Solo posee permisos para registrar operaciones comerciales que involucren a su propia sucursal.");
            }
        }

        mov.OrigenParticipanteId = dto.OrigenParticipanteId;
        mov.DestinoParticipanteId = dto.DestinoParticipanteId;
        mov.ClienteId = dto.ClienteId;
        mov.TemporadaId = dto.TemporadaId;
        mov.PorcentajeComision = mov.Tipo == TipoMovimientoComercial.CompraACliente ? dto.PorcentajeComision : 0m;
        mov.Observaciones = dto.Observaciones;

        // Clear existing details
        foreach (var det in mov.Detalles)
        {
            det.IsDeleted = true;
        }

        foreach (var item in dto.Detalles)
        {
            decimal precioPactado = 0m;
            if (mov.Tipo != TipoMovimientoComercial.TraspasoEntreClientes)
            {
                if (item.PrecioUnitarioPactado.HasValue && item.PrecioUnitarioPactado.Value > 0)
                {
                    precioPactado = item.PrecioUnitarioPactado.Value;
                }
                else if (dto.TemporadaId.HasValue)
                {
                    var refPrice = await _precioClaseRepository.GetByTemporadaAndClaseAsync(dto.TemporadaId.Value, item.ClaseBulbo, cancellationToken);
                    if (refPrice != null && refPrice.PrecioReferencia > 0)
                    {
                        precioPactado = refPrice.PrecioReferencia;
                    }
                    else
                    {
                        throw new ValidationException($"Debe especificar un precio pactado mayor a cero para {item.ClaseBulbo} o configurar su precio de referencia en la temporada.");
                    }
                }
                else
                {
                    throw new ValidationException($"Debe especificar un precio pactado mayor a cero para {item.ClaseBulbo} o especificar una temporada con precios configurados.");
                }
            }

            mov.AgregarDetalle(item.ClaseBulbo, item.Cantidad, precioPactado);
        }

        mov.EditarYReenviar(usuarioId, dto.Observaciones);

        await _movimientoRepository.UpdateAsync(mov, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<MovimientoComercialDto>.Ok(MapToDto(mov), "Movimiento comercial corregido y reenviado a revisión.");
    }

    public async Task<ApiResponse<VerificarClienteNuevoDto>> VerificarClienteNuevoAsync(
        int clienteId,
        int? excludeMovimientoId = null,
        CancellationToken cancellationToken = default)
    {
        bool hasFinalized = await _movimientoRepository.HasFinalizedMovementsForClientAsync(clienteId, excludeMovimientoId, cancellationToken);
        return ApiResponse<VerificarClienteNuevoDto>.Ok(new VerificarClienteNuevoDto
        {
            EsNuevo = !hasFinalized,
            RequiereContrato = !hasFinalized
        });
    }

    public async Task<ApiResponse<IEnumerable<MovimientoComercialDto>>> ListarAsync(
        FiltroMovimientosComercialesDto filtro,
        CancellationToken cancellationToken = default)
    {
        int? usuarioRegistroId = filtro?.UsuarioRegistroId;
        int? sucursalId = filtro?.SucursalId;

        if (_currentUserService != null)
        {
            var alcanceVer = await _currentUserService.ObtenerAlcanceAsync("MOVCOM_VER", cancellationToken);
            if (alcanceVer == AlcancePermiso.Ninguno)
            {
                return ApiResponse<IEnumerable<MovimientoComercialDto>>.Ok(Enumerable.Empty<MovimientoComercialDto>());
            }
            if (alcanceVer == AlcancePermiso.Propio)
            {
                usuarioRegistroId = _currentUserService.UsuarioId;
            }
            else if (alcanceVer == AlcancePermiso.Sucursal)
            {
                sucursalId = await _currentUserService.GetSucursalIdAsync(cancellationToken);
            }
        }

        var movimientos = await _movimientoRepository.GetByFiltersAsync(
            filtro?.Tipo,
            filtro?.Estado,
            filtro?.ClienteId,
            filtro?.TemporadaId,
            filtro?.FechaDesde,
            filtro?.FechaHasta,
            usuarioRegistroId,
            sucursalId,
            cancellationToken);

        var dtos = movimientos.Select(m => MapToDto(m));
        return ApiResponse<IEnumerable<MovimientoComercialDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<IEnumerable<MovimientoComercialDto>>> ListarPorClienteAsync(
        int clienteId,
        CancellationToken cancellationToken = default)
    {
        var movimientos = await _movimientoRepository.GetByFiltersAsync(
            clienteId: clienteId,
            cancellationToken: cancellationToken);

        var dtos = movimientos.Select(m => MapToDto(m));
        return ApiResponse<IEnumerable<MovimientoComercialDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<MovimientoComercialDto>> ObtenerPorIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var mov = await _movimientoRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (mov == null)
            throw new NotFoundException(nameof(MovimientoComercial), id);

        bool requiereContrato = false;
        if (mov.Tipo == TipoMovimientoComercial.VentaACliente && mov.ClienteId.HasValue)
        {
            bool hasFinalized = await _movimientoRepository.HasFinalizedMovementsForClientAsync(mov.ClienteId.Value, mov.Id, cancellationToken);
            requiereContrato = !hasFinalized;
        }

        return ApiResponse<MovimientoComercialDto>.Ok(MapToDto(mov, requiereContrato));
    }

    private static MovimientoComercialDto MapToDto(MovimientoComercial mov, bool requiereContrato = false)
    {
        string codigoCliente = mov.Cliente?.CodigoCliente ?? string.Empty;
        string nombreCliente = mov.Cliente?.Persona != null ? mov.Cliente.Persona.NombreCompleto : codigoCliente;

        string nombreOrigen = mov.OrigenParticipante is Sucursal sOrig ? sOrig.Nombre :
            mov.OrigenParticipante is Cliente cOrig ? (cOrig.Persona != null ? cOrig.Persona.NombreCompleto : (mov.ClienteId == mov.OrigenParticipanteId && !string.IsNullOrWhiteSpace(nombreCliente) ? nombreCliente : cOrig.CodigoCliente)) :
            (mov.ClienteId == mov.OrigenParticipanteId && !string.IsNullOrWhiteSpace(nombreCliente) ? nombreCliente : $"Participante #{mov.OrigenParticipanteId}");

        string nombreDestino = mov.DestinoParticipante is Sucursal sDest ? sDest.Nombre :
            mov.DestinoParticipante is Cliente cDest ? (cDest.Persona != null ? cDest.Persona.NombreCompleto : (mov.ClienteId == mov.DestinoParticipanteId && !string.IsNullOrWhiteSpace(nombreCliente) ? nombreCliente : cDest.CodigoCliente)) :
            (mov.ClienteId == mov.DestinoParticipanteId && !string.IsNullOrWhiteSpace(nombreCliente) ? nombreCliente : $"Participante #{mov.DestinoParticipanteId}");

        string nombreUsuarioRegistro = mov.UsuarioRegistro?.Persona != null
            ? mov.UsuarioRegistro.Persona.NombreCompleto
            : (mov.UsuarioRegistro?.Username ?? $"Usuario #{mov.UsuarioRegistroId}");

        var dto = new MovimientoComercialDto
        {
            Id = mov.Id,
            Tipo = mov.Tipo,
            Estado = mov.Estado,
            OrigenParticipanteId = mov.OrigenParticipanteId,
            NombreOrigen = nombreOrigen,
            DestinoParticipanteId = mov.DestinoParticipanteId,
            NombreDestino = nombreDestino,
            ClienteId = mov.ClienteId,
            CodigoCliente = codigoCliente,
            NombreCliente = nombreCliente,
            TemporadaId = mov.TemporadaId,
            NombreTemporada = mov.Temporada?.Nombre,
            Fecha = mov.Fecha,
            FechaRegistro = mov.FechaRegistro,
            UsuarioRegistroId = mov.UsuarioRegistroId,
            NombreUsuarioRegistro = nombreUsuarioRegistro,
            MontoBruto = mov.MontoBruto,
            PorcentajeComision = mov.PorcentajeComision,
            MontoComision = mov.MontoComision,
            MontoNeto = mov.MontoNeto,
            MontoTotal = mov.MontoTotal,
            TotalBulbos = mov.Detalles.Where(d => !d.IsDeleted).Sum(d => d.Cantidad),
            Observaciones = mov.Observaciones,
            RequiereContrato = requiereContrato,
            FechaCreacion = mov.FechaRegistro,
            Detalles = mov.Detalles.Where(d => !d.IsDeleted).Select(d => new DetalleMovimientoComercialDto
            {
                Id = d.Id,
                MovimientoComercialId = d.MovimientoComercialId,
                ClaseBulbo = d.ClaseBulbo,
                Cantidad = d.Cantidad,
                PrecioUnitarioPactado = d.PrecioUnitarioPactado,
                Subtotal = d.Subtotal
            }).ToList(),
            HistorialAprobaciones = mov.HistorialAprobaciones.Select(r => new RegistroAprobacionMovimientoDto
            {
                Id = r.Id,
                MovimientoComercialId = r.MovimientoComercialId,
                UsuarioAprobadorId = r.UsuarioAprobadorId,
                NombreUsuarioAprobador = r.UsuarioAprobador?.Persona != null
                    ? r.UsuarioAprobador.Persona.NombreCompleto
                    : (r.UsuarioAprobador?.Username ?? $"Usuario #{r.UsuarioAprobadorId}"),
                Fecha = r.Fecha,
                Accion = r.Accion,
                Observaciones = r.Observaciones
            }).OrderBy(r => r.Fecha).ThenBy(r => r.Id).ToList()
        };

        if (mov.Contrato != null && !mov.Contrato.IsDeleted)
        {
            dto.Contrato = new ContratoMovimientoDto
            {
                Id = mov.Contrato.Id,
                MovimientoComercialId = mov.Contrato.MovimientoComercialId,
                NumeroContrato = mov.Contrato.NumeroContrato,
                FechaGeneracion = mov.Contrato.FechaGeneracion,
                ArchivoUrl = mov.Contrato.ArchivoUrl,
                Estado = mov.Contrato.Estado,
                UsuarioGeneradorId = mov.Contrato.UsuarioGeneradorId
            };
        }

        if (mov.Factura != null && !mov.Factura.IsDeleted)
        {
            dto.Factura = new FacturaMovimientoDto
            {
                Id = mov.Factura.Id,
                MovimientoComercialId = mov.Factura.MovimientoComercialId,
                NumeroFactura = mov.Factura.NumeroFactura,
                FechaEmision = mov.Factura.FechaEmision,
                MontoTotal = mov.Factura.MontoTotal,
                ArchivoUrl = mov.Factura.ArchivoUrl,
                Estado = mov.Factura.Estado,
                UsuarioEmisionId = mov.Factura.UsuarioEmisionId
            };
        }

        return dto;
    }

    private async Task ValidarParticipanteActivoAsync(int participanteId, string rol, CancellationToken cancellationToken)
    {
        var participante = await _participanteRepository.GetByIdAsync(participanteId, cancellationToken);
        if (participante == null || participante.IsDeleted)
            throw new ValidationException($"El participante {rol} #{participanteId} no existe o fue eliminado.");

        if (participante.Estado == EstadoParticipante.Inactivo)
            throw new ReglaNegocioException($"El participante {rol} se encuentra inactivo.");

        if (participante is Cliente cliente)
        {
            if (cliente.EstadoCliente == EstadoCliente.Inactivo)
                throw new ReglaNegocioException($"El cliente {rol} se encuentra inactivo.");
        }
        else if (participante is Sucursal sucursal)
        {
            if (!sucursal.Activo)
                throw new ReglaNegocioException($"La sucursal {rol} '{sucursal.Nombre}' se encuentra inactiva.");
        }
    }

    private async Task VincularContratoAClienteSiAplicaAsync(MovimientoComercial mov, CancellationToken cancellationToken)
    {
        if (mov.Contrato == null || mov.Contrato.IsDeleted) return;

        int? clienteId = mov.ClienteId ?? (mov.Tipo == TipoMovimientoComercial.VentaACliente ? mov.DestinoParticipanteId : null);
        if (!clienteId.HasValue) return;

        var cliente = await _clienteRepository.GetByIdAsync(clienteId.Value, cancellationToken);
        if (cliente != null)
        {
            if (cliente.ContratoId == null || cliente.ContratoId <= 0 || mov.Estado == EstadoMovimientoComercial.Finalizado)
            {
                cliente.ContratoId = mov.Contrato.Id;
                await _clienteRepository.UpdateAsync(cliente, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
