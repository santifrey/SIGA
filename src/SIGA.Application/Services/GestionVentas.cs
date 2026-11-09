using SIGA.Application.Common;
using SIGA.Application.DTOs.Ventas;
using SIGA.Application.Interfaces;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Interfaces;

namespace SIGA.Application.Services;

public class GestionVentas : IGestionVentas
{
    private readonly IVentaRepository _ventaRepository;
    private readonly IPrecioClaseRepository _precioClaseRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly ITemporadaRepository _temporadaRepository;
    private readonly IGestionStock _gestionStock;
    private readonly IOperacionComercialRepository _operacionRepository;
    private readonly IMovimientoBulbosRepository _movimientoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GestionVentas(
        IVentaRepository ventaRepository,
        IPrecioClaseRepository precioClaseRepository,
        IClienteRepository clienteRepository,
        ITemporadaRepository temporadaRepository,
        IGestionStock gestionStock,
        IOperacionComercialRepository operacionRepository,
        IMovimientoBulbosRepository movimientoRepository,
        IUnitOfWork unitOfWork)
    {
        _ventaRepository = ventaRepository;
        _precioClaseRepository = precioClaseRepository;
        _clienteRepository = clienteRepository;
        _temporadaRepository = temporadaRepository;
        _gestionStock = gestionStock;
        _operacionRepository = operacionRepository;
        _movimientoRepository = movimientoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<PrecioClaseDto>> ConfigurarPrecioClaseAsync(
        UpsertPrecioClaseDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.PrecioReferencia <= 0)
            throw new ValidationException("El precio de referencia debe ser mayor a cero.");

        var temporada = await _temporadaRepository.GetByIdAsync(dto.TemporadaId, cancellationToken);
        if (temporada == null)
            throw new NotFoundException(nameof(Temporada), dto.TemporadaId);

        var existente = await _precioClaseRepository.GetByTemporadaAndClaseAsync(dto.TemporadaId, dto.ClaseBulbo, cancellationToken);
        if (existente != null)
        {
            existente.ActualizarPrecio(dto.PrecioReferencia);
            await _precioClaseRepository.UpdateAsync(existente, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse<PrecioClaseDto>.Ok(MapPrecioToDto(existente, temporada.Nombre), "Precio de referencia actualizado exitosamente.");
        }

        var nuevo = new PrecioClase(dto.TemporadaId, dto.ClaseBulbo, dto.PrecioReferencia);
        await _precioClaseRepository.AddAsync(nuevo, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<PrecioClaseDto>.Created(MapPrecioToDto(nuevo, temporada.Nombre), "Precio de referencia registrado exitosamente.");
    }

    public async Task<ApiResponse<IEnumerable<PrecioClaseDto>>> ListarPreciosPorTemporadaAsync(
        int temporadaId,
        CancellationToken cancellationToken = default)
    {
        var temporada = await _temporadaRepository.GetByIdAsync(temporadaId, cancellationToken);
        if (temporada == null)
            throw new NotFoundException(nameof(Temporada), temporadaId);

        var precios = await _precioClaseRepository.GetByTemporadaAsync(temporadaId, cancellationToken);
        var dtos = precios.Select(p => MapPrecioToDto(p, temporada.Nombre));
        return ApiResponse<IEnumerable<PrecioClaseDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<VentaDto>> CrearVentaAsync(
        CreateVentaDto dto,
        int vendedorUsuarioId,
        CancellationToken cancellationToken = default)
    {
        if (dto.Detalles == null || !dto.Detalles.Any())
            throw new ValidationException("Debe incluir al menos un detalle de clase y cantidad de bulbos en la venta.");

        if (dto.Detalles.Any(d => d.Cantidad <= 0))
            throw new ValidationException("Todas las cantidades de bulbos en la venta deben ser mayores a cero.");

        var cliente = await _clienteRepository.GetByIdWithPersonaAsync(dto.ClienteId, cancellationToken);
        if (cliente == null)
            throw new NotFoundException(nameof(Cliente), dto.ClienteId);

        if (dto.TemporadaId.HasValue && dto.TemporadaId.Value > 0)
        {
            var temporada = await _temporadaRepository.GetByIdAsync(dto.TemporadaId.Value, cancellationToken);
            if (temporada == null)
                throw new NotFoundException(nameof(Temporada), dto.TemporadaId.Value);
        }

        var origenId = dto.OrigenParticipanteId > 0 ? dto.OrigenParticipanteId : 1;

        var venta = new Venta(
            vendedorUsuarioId,
            dto.ClienteId,
            dto.TemporadaId,
            origenId,
            dto.Observaciones);

        // Regla DP-4: Resolver precio pactado o tomar precio de referencia por clase de la temporada
        foreach (var item in dto.Detalles)
        {
            decimal precioPactado = item.PrecioUnitarioPactado ?? 0m;
            if (precioPactado <= 0 && dto.TemporadaId.HasValue && dto.TemporadaId.Value > 0)
            {
                var precioRef = await _precioClaseRepository.GetByTemporadaAndClaseAsync(
                    dto.TemporadaId.Value,
                    item.ClaseBulbo,
                    cancellationToken);

                if (precioRef != null)
                {
                    precioPactado = precioRef.PrecioReferencia;
                }
            }

            if (precioPactado <= 0)
            {
                throw new ValidationException(
                    $"Debe especificar un precio unitario pactado mayor a cero para {item.ClaseBulbo} o configurar su precio de referencia en la temporada.");
            }

            venta.AgregarDetalle(item.ClaseBulbo, item.Cantidad, precioPactado);
        }

        await _ventaRepository.AddAsync(venta, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var creada = await _ventaRepository.GetByIdWithDetailsAsync(venta.Id, cancellationToken);
        return ApiResponse<VentaDto>.Created(MapToDto(creada ?? venta), "Venta registrada exitosamente.");
    }

    public async Task<ApiResponse<VentaDto>> EnviarARevisionAsync(
        int ventaId,
        AccionCircuitoVentaDto dto,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var venta = await _ventaRepository.GetByIdWithDetailsAsync(ventaId, cancellationToken);
        if (venta == null)
            throw new NotFoundException(nameof(Venta), ventaId);

        venta.EnviarARevision(usuarioId, dto.Observaciones);

        await _ventaRepository.UpdateAsync(venta, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var actualizada = await _ventaRepository.GetByIdWithDetailsAsync(ventaId, cancellationToken);
        return ApiResponse<VentaDto>.Ok(MapToDto(actualizada ?? venta), "Venta enviada a revisión de Secretaría / Administración.");
    }

    public async Task<ApiResponse<VentaDto>> GenerarContratoAsync(
        int ventaId,
        GenerarContratoDto dto,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var venta = await _ventaRepository.GetByIdWithDetailsAsync(ventaId, cancellationToken);
        if (venta == null)
            throw new NotFoundException(nameof(Venta), ventaId);

        if (string.IsNullOrWhiteSpace(dto.NumeroContrato))
            throw new ValidationException("El número de contrato es obligatorio.");

        venta.GenerarContrato(dto.NumeroContrato, usuarioId, dto.ArchivoUrl);

        await _ventaRepository.UpdateAsync(venta, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var actualizada = await _ventaRepository.GetByIdWithDetailsAsync(ventaId, cancellationToken);
        return ApiResponse<VentaDto>.Ok(MapToDto(actualizada ?? venta), "Contrato generado exitosamente.");
    }

    public async Task<ApiResponse<VentaDto>> AprobarAdministracionAsync(
        int ventaId,
        AccionCircuitoVentaDto dto,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var venta = await _ventaRepository.GetByIdWithDetailsAsync(ventaId, cancellationToken);
        if (venta == null)
            throw new NotFoundException(nameof(Venta), ventaId);

        venta.AprobarAdministracion(usuarioId, dto.Observaciones);

        await _ventaRepository.UpdateAsync(venta, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var actualizada = await _ventaRepository.GetByIdWithDetailsAsync(ventaId, cancellationToken);
        return ApiResponse<VentaDto>.Ok(MapToDto(actualizada ?? venta), "Venta y contrato aprobados por Secretaría / Administración.");
    }

    public async Task<ApiResponse<VentaDto>> EmitirFacturaAsync(
        int ventaId,
        EmitirFacturaDto dto,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var venta = await _ventaRepository.GetByIdWithDetailsAsync(ventaId, cancellationToken);
        if (venta == null)
            throw new NotFoundException(nameof(Venta), ventaId);

        if (string.IsNullOrWhiteSpace(dto.NumeroFactura))
            throw new ValidationException("El número de factura es obligatorio.");

        venta.EmitirFactura(dto.NumeroFactura, usuarioId, dto.ArchivoUrl, dto.Observaciones);

        await _ventaRepository.UpdateAsync(venta, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var actualizada = await _ventaRepository.GetByIdWithDetailsAsync(ventaId, cancellationToken);
        return ApiResponse<VentaDto>.Ok(MapToDto(actualizada ?? venta), "Factura emitida exitosamente por Contabilidad.");
    }

    public async Task<ApiResponse<VentaDto>> AprobarFinalAsync(
        int ventaId,
        AccionCircuitoVentaDto dto,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var venta = await _ventaRepository.GetByIdWithDetailsAsync(ventaId, cancellationToken);
        if (venta == null)
            throw new NotFoundException(nameof(Venta), ventaId);

        // Si el vendedor de los bulbos es un Cliente (OrigenParticipanteId != 1), validamos su stock disponible
        if (venta.OrigenParticipanteId != 1)
        {
            foreach (var det in venta.Detalles.Where(d => !d.IsDeleted))
            {
                var disp = await _gestionStock.VerificarDisponibilidadAsync(
                    venta.OrigenParticipanteId,
                    det.ClaseBulbo,
                    det.Cantidad,
                    cancellationToken);

                if (disp == null || !disp.Success || disp.Data == null || !disp.Data.Disponible)
                {
                    var stockActual = disp?.Data?.StockDisponible ?? 0;
                    throw new ValidationException(
                        $"Stock insuficiente en el participante vendedor #{venta.OrigenParticipanteId} para {det.ClaseBulbo}. Disponible: {stockActual}, Requerido: {det.Cantidad}.");
                }
            }
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Generar Operación Comercial y Movimiento de Bulbos confirmado hacia el Cliente comprador
            var operacion = new OperacionComercial
            {
                Fecha = DateTime.UtcNow,
                Tipo = TipoOperacionComercial.CompraVentaBulbos,
                Estado = EstadoOperacion.Confirmada,
                UsuarioCreadorId = usuarioId,
                Observaciones = $"Venta #{venta.Id} - Contrato {venta.Contrato?.NumeroContrato} - Factura {venta.Factura?.NumeroFactura}"
            };

            await _operacionRepository.AddAsync(operacion, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var movimiento = new MovimientoBulbos(
                operacion.Id,
                venta.OrigenParticipanteId,
                venta.ClienteId,
                usuarioId,
                $"Entrega comercial por Venta #{venta.Id} aprobada finalmente");

            foreach (var det in venta.Detalles.Where(d => !d.IsDeleted && d.Cantidad > 0))
            {
                movimiento.AgregarCantidad(det.ClaseBulbo, det.Cantidad);
            }

            movimiento.Confirmar();
            await _movimientoRepository.AddAsync(movimiento, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Completar el circuito de la venta y vincular la operación comercial
            venta.AprobarFinal(usuarioId, operacion.Id, dto.Observaciones);
            await _ventaRepository.UpdateAsync(venta, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            var completada = await _ventaRepository.GetByIdWithDetailsAsync(ventaId, cancellationToken);
            return ApiResponse<VentaDto>.Ok(
                MapToDto(completada ?? venta),
                "Venta aprobada finalmente. Se confirmó el movimiento comercial y se acreditó el stock al cliente.");
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ApiResponse<VentaDto>> RechazarVentaAsync(
        int ventaId,
        AccionCircuitoVentaDto dto,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var venta = await _ventaRepository.GetByIdWithDetailsAsync(ventaId, cancellationToken);
        if (venta == null)
            throw new NotFoundException(nameof(Venta), ventaId);

        if (string.IsNullOrWhiteSpace(dto.Observaciones))
            throw new ValidationException("Debe indicar el motivo del rechazo u observación.");

        venta.Rechazar(usuarioId, dto.Observaciones);

        await _ventaRepository.UpdateAsync(venta, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var actualizada = await _ventaRepository.GetByIdWithDetailsAsync(ventaId, cancellationToken);
        return ApiResponse<VentaDto>.Ok(MapToDto(actualizada ?? venta), "Venta rechazada/observada.");
    }

    public async Task<ApiResponse<IEnumerable<VentaDto>>> ListarTodasAsync(CancellationToken cancellationToken = default)
    {
        var ventas = await _ventaRepository.GetAllWithDetailsAsync(cancellationToken);
        return ApiResponse<IEnumerable<VentaDto>>.Ok(ventas.Select(MapToDto));
    }

    public async Task<ApiResponse<IEnumerable<VentaDto>>> ListarPorClienteAsync(int clienteId, CancellationToken cancellationToken = default)
    {
        var cliente = await _clienteRepository.GetByIdAsync(clienteId, cancellationToken);
        if (cliente == null)
            throw new NotFoundException(nameof(Cliente), clienteId);

        var ventas = await _ventaRepository.GetByClienteAsync(clienteId, cancellationToken);
        return ApiResponse<IEnumerable<VentaDto>>.Ok(ventas.Select(MapToDto));
    }

    public async Task<ApiResponse<VentaDto>> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var venta = await _ventaRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (venta == null)
            throw new NotFoundException(nameof(Venta), id);

        return ApiResponse<VentaDto>.Ok(MapToDto(venta));
    }

    private static PrecioClaseDto MapPrecioToDto(PrecioClase p, string nombreTemporada)
    {
        return new PrecioClaseDto
        {
            Id = p.Id,
            TemporadaId = p.TemporadaId,
            NombreTemporada = p.Temporada?.Nombre ?? nombreTemporada,
            ClaseBulbo = p.ClaseBulbo,
            PrecioReferencia = p.PrecioReferencia,
            FechaVigencia = p.FechaVigencia
        };
    }

    private static VentaDto MapToDto(Venta v)
    {
        var detalles = v.Detalles?
            .Where(d => !d.IsDeleted)
            .Select(d => new DetalleVentaDto
            {
                Id = d.Id,
                VentaId = d.VentaId,
                ClaseBulbo = d.ClaseBulbo,
                Cantidad = d.Cantidad,
                PrecioUnitarioPactado = d.PrecioUnitarioPactado,
                Subtotal = d.Subtotal
            })
            .ToList() ?? new List<DetalleVentaDto>();

        ContratoDto? contratoDto = null;
        if (v.Contrato != null && !v.Contrato.IsDeleted)
        {
            contratoDto = new ContratoDto
            {
                Id = v.Contrato.Id,
                VentaId = v.Contrato.VentaId,
                NumeroContrato = v.Contrato.NumeroContrato,
                FechaGeneracion = v.Contrato.FechaGeneracion,
                ArchivoUrl = v.Contrato.ArchivoUrl,
                Estado = v.Contrato.Estado,
                UsuarioGeneradorId = v.Contrato.UsuarioGeneradorId
            };
        }

        FacturaDto? facturaDto = null;
        if (v.Factura != null && !v.Factura.IsDeleted)
        {
            facturaDto = new FacturaDto
            {
                Id = v.Factura.Id,
                VentaId = v.Factura.VentaId,
                NumeroFactura = v.Factura.NumeroFactura,
                FechaEmision = v.Factura.FechaEmision,
                MontoTotal = v.Factura.MontoTotal,
                ArchivoUrl = v.Factura.ArchivoUrl,
                Estado = v.Factura.Estado,
                UsuarioEmisionId = v.Factura.UsuarioEmisionId
            };
        }

        var aprobaciones = v.HistorialAprobaciones?
            .Where(r => !r.IsDeleted)
            .OrderBy(r => r.Fecha)
            .ThenBy(r => r.Id)
            .Select(r => new RegistroAprobacionDto
            {
                Id = r.Id,
                VentaId = r.VentaId,
                UsuarioAprobadorId = r.UsuarioAprobadorId,
                NombreUsuarioAprobador = r.UsuarioAprobador?.Username ?? $"Usuario #{r.UsuarioAprobadorId}",
                Fecha = r.Fecha,
                Accion = r.Accion,
                Observaciones = r.Observaciones
            })
            .ToList() ?? new List<RegistroAprobacionDto>();

        return new VentaDto
        {
            Id = v.Id,
            OperacionComercialId = v.OperacionComercialId,
            VendedorId = v.VendedorId,
            NombreVendedor = v.Vendedor?.Persona != null
                ? v.Vendedor.Persona.NombreCompleto
                : (v.Vendedor?.Username ?? $"Vendedor #{v.VendedorId}"),
            OrigenParticipanteId = v.OrigenParticipanteId,
            ClienteId = v.ClienteId,
            CodigoCliente = v.Cliente?.CodigoCliente ?? string.Empty,
            NombreCliente = v.Cliente?.Persona != null
                ? v.Cliente.Persona.NombreCompleto
                : (v.Cliente?.CodigoCliente ?? $"Cliente #{v.ClienteId}"),
            TemporadaId = v.TemporadaId,
            NombreTemporada = v.Temporada?.Nombre,
            Fecha = v.Fecha,
            MontoTotal = v.MontoTotal,
            TotalBulbos = detalles.Sum(d => d.Cantidad),
            Estado = v.Estado,
            Observaciones = v.Observaciones,
            Detalles = detalles,
            Contrato = contratoDto,
            Factura = facturaDto,
            HistorialAprobaciones = aprobaciones,
            FechaCreacion = v.FechaCreacion
        };
    }
}
