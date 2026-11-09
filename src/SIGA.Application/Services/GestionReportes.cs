using SIGA.Application.Common;
using SIGA.Application.DTOs.Movimientos;
using SIGA.Application.DTOs.Reportes;
using SIGA.Application.DTOs.Stock;
using SIGA.Application.DTOs.Transferencias;
using SIGA.Application.DTOs.Ventas;
using SIGA.Application.Interfaces;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Interfaces;

namespace SIGA.Application.Services;

public class GestionReportes : IGestionReportes
{
    private readonly IClienteRepository _clienteRepository;
    private readonly ISucursalRepository _sucursalRepository;
    private readonly ITemporadaRepository _temporadaRepository;
    private readonly ILoteRepository _loteRepository;
    private readonly ICosechaRepository _cosechaRepository;
    private readonly ITransferenciaLoteRepository _transferenciaRepository;
    private readonly IVentaRepository _ventaRepository;
    private readonly IGestionStock _gestionStock;
    private readonly IGestionMovimientos _gestionMovimientos;
    private readonly IGestionVentas _gestionVentas;

    private static readonly ClaseBulbo[] TodasLasClases =
    {
        ClaseBulbo.Clase0,
        ClaseBulbo.Clase1,
        ClaseBulbo.Clase2,
        ClaseBulbo.Clase3,
        ClaseBulbo.Clase4
    };

    public GestionReportes(
        IClienteRepository clienteRepository,
        ISucursalRepository sucursalRepository,
        ITemporadaRepository temporadaRepository,
        ILoteRepository loteRepository,
        ICosechaRepository cosechaRepository,
        ITransferenciaLoteRepository transferenciaRepository,
        IVentaRepository ventaRepository,
        IGestionStock gestionStock,
        IGestionMovimientos gestionMovimientos,
        IGestionVentas gestionVentas)
    {
        _clienteRepository = clienteRepository;
        _sucursalRepository = sucursalRepository;
        _temporadaRepository = temporadaRepository;
        _loteRepository = loteRepository;
        _cosechaRepository = cosechaRepository;
        _transferenciaRepository = transferenciaRepository;
        _ventaRepository = ventaRepository;
        _gestionStock = gestionStock;
        _gestionMovimientos = gestionMovimientos;
        _gestionVentas = gestionVentas;
    }

    public async Task<ApiResponse<ReporteIntegralClienteDto>> ObtenerEstadoIntegralClienteAsync(
        int clienteId,
        CancellationToken cancellationToken = default)
    {
        var cliente = await _clienteRepository.GetByIdWithPersonaAsync(clienteId, cancellationToken);
        if (cliente == null)
            throw new NotFoundException(nameof(Cliente), clienteId);

        // 1. Stock disponible actual por clase
        var stockResp = await _gestionStock.ConsultarStockPorClienteAsync(clienteId, cancellationToken);
        var stockPorClase = stockResp.Data?.StockPorClase ?? new List<StockPorClaseDto>();
        var totalStock = stockResp.Data?.TotalBulbos ?? 0;

        // 2. Consultar temporadas, lotes y cosechas para resolver derechos vigentes e historial productivo
        var temporadas = await _temporadaRepository.GetAllAsync(cancellationToken);
        var derechosLotes = new List<DerechoLoteClienteDto>();
        var historialProductivo = new List<ResumenProductivoTemporadaClienteDto>();

        foreach (var temp in temporadas.OrderByDescending(t => t.FechaInicio))
        {
            var lotesTemp = await _loteRepository.GetByTemporadaAsync(temp.Id, cancellationToken);
            var cosechasTemp = await _cosechaRepository.GetByTemporadaAsync(temp.Id, cancellationToken);

            // Lotes donde el cliente es el titular vigente hoy
            var lotesVigentesCliente = lotesTemp
                .Where(l => l.ResolverTitularVigenteId() == clienteId)
                .ToList();

            foreach (var lote in lotesVigentesCliente)
            {
                derechosLotes.Add(new DerechoLoteClienteDto
                {
                    LoteId = lote.Id,
                    TemporadaId = temp.Id,
                    NombreTemporada = temp.Nombre,
                    SucursalId = lote.Fila?.SucursalId ?? 0,
                    NombreSucursal = lote.Fila?.Sucursal?.Nombre ?? string.Empty,
                    IdentificacionFila = lote.Fila?.Identificacion ?? string.Empty,
                    Orden = lote.Orden,
                    EstadoLote = lote.Estado,
                    CantidadPlantadaTotal = lote.CantidadPlantadaTotal,
                    AdquiridoPorTransferencia = lote.ClienteId != clienteId,
                    TitularOriginalId = lote.ClienteId,
                    NombreTitularOriginal = lote.Cliente?.Persona != null
                        ? lote.Cliente.Persona.NombreCompleto
                        : (lote.Cliente?.CodigoCliente ?? $"Cliente #{lote.ClienteId}")
                });
            }

            // Resumen productivo de la temporada para este cliente
            var lotesPlantadosOriginalmente = lotesTemp.Where(l => l.ClienteId == clienteId).ToList();
            var bulbosPlantados = lotesPlantadosOriginalmente.Sum(l => l.CantidadPlantadaTotal);

            var idsLotesVigentesCliente = lotesVigentesCliente.Select(l => l.Id).ToHashSet();
            var bulbosCosechados = cosechasTemp
                .SelectMany(c => c.DetallesCosecha)
                .Where(d => !d.IsDeleted && idsLotesVigentesCliente.Contains(d.LoteId))
                .Sum(d => d.Cantidad);

            if (lotesPlantadosOriginalmente.Any() || lotesVigentesCliente.Any() || bulbosCosechados > 0)
            {
                historialProductivo.Add(new ResumenProductivoTemporadaClienteDto
                {
                    TemporadaId = temp.Id,
                    NombreTemporada = temp.Nombre,
                    EstadoTemporada = temp.Estado,
                    CantidadLotesPlantados = lotesPlantadosOriginalmente.Count,
                    BulbosPlantados = bulbosPlantados,
                    CantidadLotesCosechados = lotesVigentesCliente.Count(l => l.Estado == EstadoLote.Cosechado),
                    BulbosCosechados = bulbosCosechados
                });
            }
        }

        // 3. Historial de movimientos comerciales y de stock
        var movsResp = await _gestionMovimientos.ObtenerPorClienteAsync(clienteId, cancellationToken);
        var historialMovimientos = movsResp.Data?.ToList() ?? new List<MovimientoBulbosDto>();

        // 4. Historial de transferencias donde el cliente fue cedente o cesionario
        var todasTransferencias = await _transferenciaRepository.GetAllWithDetailsAsync(null, cancellationToken);
        var transferenciasEntidad = todasTransferencias
            .Where(t => t.TitularAnteriorId == clienteId || t.NuevoTitularId == clienteId)
            .OrderByDescending(t => t.Fecha)
            .ToList();

        var historialTransferencias = transferenciasEntidad.Select(t => new TransferenciaLoteDto
        {
            Id = t.Id,
            LoteId = t.LoteId,
            IdentificacionFila = t.Lote?.Fila?.Identificacion ?? string.Empty,
            NombreSucursal = t.Lote?.Fila?.Sucursal?.Nombre ?? string.Empty,
            TitularAnteriorId = t.TitularAnteriorId,
            NombreTitularAnterior = FormatearParticipante(t.TitularAnterior, t.TitularAnteriorId),
            NuevoTitularId = t.NuevoTitularId,
            NombreNuevoTitular = FormatearParticipante(t.NuevoTitular, t.NuevoTitularId),
            Fecha = t.Fecha,
            Estado = t.Estado,
            Observaciones = t.Observaciones
        }).ToList();

        // 5. Historial de compras / ventas del cliente
        var ventasResp = await _gestionVentas.ListarPorClienteAsync(clienteId, cancellationToken);
        var historialVentas = ventasResp.Data?.ToList() ?? new List<VentaDto>();

        var reporte = new ReporteIntegralClienteDto
        {
            ClienteId = cliente.Id,
            CodigoCliente = cliente.CodigoCliente,
            NombreCompleto = cliente.Persona != null ? cliente.Persona.NombreCompleto : cliente.CodigoCliente,
            Documento = cliente.Persona?.Documento ?? string.Empty,
            Email = cliente.Persona?.Email,
            EstadoCliente = cliente.EstadoCliente,
            TotalBulbosEnStock = totalStock,
            StockDisponiblePorClase = stockPorClase,
            TotalLotesVigentes = derechosLotes.Count,
            TotalBulbosPlantadosEnLotesVigentes = derechosLotes.Sum(d => d.CantidadPlantadaTotal),
            DerechosSobreLotesPlantados = derechosLotes,
            HistorialProductivoPorTemporada = historialProductivo,
            HistorialMovimientos = historialMovimientos,
            HistorialTransferencias = historialTransferencias,
            HistorialComprasVentas = historialVentas
        };

        return ApiResponse<ReporteIntegralClienteDto>.Ok(reporte, "Reporte integral del cliente generado exitosamente.");
    }

    public async Task<ApiResponse<ReporteRendimientoTemporadaDto>> ObtenerRendimientoTemporadaAsync(
        int temporadaId,
        CancellationToken cancellationToken = default)
    {
        var temporada = await _temporadaRepository.GetByIdAsync(temporadaId, cancellationToken);
        if (temporada == null)
            throw new NotFoundException(nameof(Temporada), temporadaId);

        var lotes = await _loteRepository.GetByTemporadaAsync(temporadaId, cancellationToken);
        var cosechas = await _cosechaRepository.GetByTemporadaAsync(temporadaId, cancellationToken);
        var sucursales = await _sucursalRepository.GetAllAsync(cancellationToken);
        var mapaSucursales = sucursales.ToDictionary(s => s.Id, s => s.Nombre);

        // 1. Comparativa global por Clase (Clase 0 a Clase 4)
        var todosDetallesPlantacion = lotes
            .SelectMany(l => l.DetallesPlantacion ?? Enumerable.Empty<DetallePlantacion>())
            .Where(d => !d.IsDeleted)
            .ToList();

        var todosDetallesCosecha = cosechas
            .SelectMany(c => c.DetallesCosecha ?? Enumerable.Empty<DetalleCosecha>())
            .Where(d => !d.IsDeleted)
            .ToList();

        var comparativaGlobalClases = TodasLasClases.Select(clase => new ComparativaClaseBulboDto
        {
            ClaseBulbo = clase,
            CantidadPlantada = todosDetallesPlantacion.Where(d => d.ClaseBulbo == clase).Sum(d => d.Cantidad),
            CantidadCosechada = todosDetallesCosecha.Where(d => d.ClaseBulbo == clase).Sum(d => d.Cantidad)
        }).ToList();

        // 2. Desglose por Sucursal
        var idsSucursalesInvolucradas = lotes
            .Where(l => l.Fila != null)
            .Select(l => l.Fila.SucursalId)
            .Union(cosechas.Select(c => c.SucursalId))
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        var desgloseSucursales = new List<RendimientoSucursalDto>();
        foreach (var sucId in idsSucursalesInvolucradas)
        {
            var lotesSucursal = lotes.Where(l => l.Fila != null && l.Fila.SucursalId == sucId).ToList();
            var cosechaSucursal = cosechas.FirstOrDefault(c => c.SucursalId == sucId);

            var detallesPlantSuc = lotesSucursal
                .SelectMany(l => l.DetallesPlantacion ?? Enumerable.Empty<DetallePlantacion>())
                .Where(d => !d.IsDeleted)
                .ToList();

            var detallesCosSuc = cosechaSucursal?.DetallesCosecha?
                .Where(d => !d.IsDeleted)
                .ToList() ?? new List<DetalleCosecha>();

            var nombreSuc = cosechaSucursal?.Sucursal?.Nombre
                ?? (mapaSucursales.TryGetValue(sucId, out var n) ? n : $"Sucursal #{sucId}");

            desgloseSucursales.Add(new RendimientoSucursalDto
            {
                SucursalId = sucId,
                NombreSucursal = nombreSuc,
                EstadoCosecha = cosechaSucursal != null ? cosechaSucursal.Estado.ToString() : "Sin Iniciar",
                CantidadLotes = lotesSucursal.Count,
                TotalPlantado = lotesSucursal.Sum(l => l.CantidadPlantadaTotal),
                TotalCosechado = detallesCosSuc.Sum(d => d.Cantidad),
                ComparativaPorClase = TodasLasClases.Select(clase => new ComparativaClaseBulboDto
                {
                    ClaseBulbo = clase,
                    CantidadPlantada = detallesPlantSuc.Where(d => d.ClaseBulbo == clase).Sum(d => d.Cantidad),
                    CantidadCosechada = detallesCosSuc.Where(d => d.ClaseBulbo == clase).Sum(d => d.Cantidad)
                }).ToList()
            });
        }

        // 3. Trazabilidad por Lote
        var trazabilidadLotes = new List<TrazabilidadLoteDto>();
        foreach (var lote in lotes)
        {
            var titularOriginalId = lote.ClienteId;
            var titularVigenteId = lote.ResolverTitularVigenteId();

            var nombreOriginal = lote.Cliente?.Persona != null
                ? lote.Cliente.Persona.NombreCompleto
                : (lote.Cliente?.CodigoCliente ?? $"Cliente #{titularOriginalId}");

            string nombreVigente = nombreOriginal;
            if (titularVigenteId != titularOriginalId)
            {
                var ultimaTransf = lote.Transferencias?
                    .Where(t => t.Estado == EstadoTransferencia.Confirmada && !t.IsDeleted)
                    .OrderByDescending(t => t.Fecha)
                    .ThenByDescending(t => t.Id)
                    .FirstOrDefault();

                nombreVigente = FormatearParticipante(ultimaTransf?.NuevoTitular, titularVigenteId);
            }

            var cosechadoLote = todosDetallesCosecha
                .Where(d => d.LoteId == lote.Id)
                .Sum(d => d.Cantidad);

            var sucId = lote.Fila?.SucursalId ?? 0;
            var nombreSuc = lote.Fila?.Sucursal?.Nombre
                ?? (mapaSucursales.TryGetValue(sucId, out var ns) ? ns : $"Sucursal #{sucId}");

            trazabilidadLotes.Add(new TrazabilidadLoteDto
            {
                LoteId = lote.Id,
                SucursalId = sucId,
                NombreSucursal = nombreSuc,
                IdentificacionFila = lote.Fila?.Identificacion ?? string.Empty,
                Orden = lote.Orden,
                TitularOriginalId = titularOriginalId,
                NombreTitularOriginal = nombreOriginal,
                TitularVigenteId = titularVigenteId,
                NombreTitularVigente = nombreVigente,
                CantidadPlantada = lote.CantidadPlantadaTotal,
                CantidadCosechada = cosechadoLote,
                EstadoLote = lote.Estado
            });
        }

        var dto = new ReporteRendimientoTemporadaDto
        {
            TemporadaId = temporada.Id,
            NombreTemporada = temporada.Nombre,
            EstadoTemporada = temporada.Estado,
            FechaInicio = temporada.FechaInicio,
            FechaFin = temporada.FechaFin,
            TotalPlantadoTemporada = lotes.Sum(l => l.CantidadPlantadaTotal),
            TotalCosechadoTemporada = todosDetallesCosecha.Sum(d => d.Cantidad),
            ComparativaPorClase = comparativaGlobalClases,
            DesglosePorSucursal = desgloseSucursales,
            TrazabilidadPorLote = trazabilidadLotes
        };

        return ApiResponse<ReporteRendimientoTemporadaDto>.Ok(dto, "Reporte de rendimiento productivo generado exitosamente.");
    }

    public async Task<ApiResponse<ResumenEjecutivoDto>> ObtenerResumenEjecutivoAsync(
        CancellationToken cancellationToken = default)
    {
        var clientes = await _clienteRepository.GetAllWithPersonaAsync(null, cancellationToken);
        var sucursales = await _sucursalRepository.GetAllAsync(cancellationToken);
        var temporadaActiva = await _temporadaRepository.GetActivaAsync(cancellationToken);
        var ventas = await _ventaRepository.GetAllWithDetailsAsync(cancellationToken);

        int totalLotesActiva = 0;
        int totalPlantadoActiva = 0;
        int totalCosechadoActiva = 0;

        if (temporadaActiva != null)
        {
            var lotesActiva = await _loteRepository.GetByTemporadaAsync(temporadaActiva.Id, cancellationToken);
            var cosechasActiva = await _cosechaRepository.GetByTemporadaAsync(temporadaActiva.Id, cancellationToken);

            totalLotesActiva = lotesActiva.Count;
            totalPlantadoActiva = lotesActiva.Sum(l => l.CantidadPlantadaTotal);
            totalCosechadoActiva = cosechasActiva
                .SelectMany(c => c.DetallesCosecha)
                .Where(d => !d.IsDeleted)
                .Sum(d => d.Cantidad);
        }

        var ventasCompletadas = ventas.Where(v => v.Estado == EstadoVenta.Completada).ToList();
        var ventasEnCircuito = ventas.Where(v => v.Estado != EstadoVenta.Completada && v.Estado != EstadoVenta.Anulada && v.Estado != EstadoVenta.Rechazada).ToList();

        var resumen = new ResumenEjecutivoDto
        {
            TotalClientesActivos = clientes.Count(c => c.EstadoCliente == EstadoCliente.Activo),
            TotalSucursalesActivas = sucursales.Count(s => s.Estado == EstadoSucursal.Activo),
            TemporadaActivaId = temporadaActiva?.Id,
            TemporadaActivaNombre = temporadaActiva?.Nombre,
            TemporadaActivaEstado = temporadaActiva?.Estado.ToString(),
            TotalLotesTemporadaActiva = totalLotesActiva,
            TotalBulbosPlantadosTemporadaActiva = totalPlantadoActiva,
            TotalBulbosCosechadosTemporadaActiva = totalCosechadoActiva,
            TotalVentasRegistradas = ventas.Count,
            TotalVentasCompletadas = ventasCompletadas.Count,
            MontoTotalVentasCompletadas = ventasCompletadas.Sum(v => v.MontoTotal),
            MontoTotalVentasEnCircuito = ventasEnCircuito.Sum(v => v.MontoTotal)
        };

        return ApiResponse<ResumenEjecutivoDto>.Ok(resumen, "Resumen ejecutivo obtenido exitosamente.");
    }

    private static string FormatearParticipante(ParticipanteComercial? p, int fallbackId)
    {
        if (p is Cliente c)
        {
            return c.Persona != null ? c.Persona.NombreCompleto : c.CodigoCliente;
        }
        if (p is Empresa e)
        {
            return e.RazonSocial;
        }
        return fallbackId == 1 ? "Azafrán Mediterráneo S.A." : $"Participante #{fallbackId}";
    }
}
