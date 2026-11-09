using SIGA.Application.DTOs.Movimientos;
using SIGA.Application.DTOs.Stock;
using SIGA.Application.DTOs.Transferencias;
using SIGA.Application.DTOs.Ventas;
using SIGA.Domain.Enums;

namespace SIGA.Application.DTOs.Reportes;

public class DerechoLoteClienteDto
{
    public int LoteId { get; set; }
    public int TemporadaId { get; set; }
    public string NombreTemporada { get; set; } = string.Empty;
    public int SucursalId { get; set; }
    public string NombreSucursal { get; set; } = string.Empty;
    public string IdentificacionFila { get; set; } = string.Empty;
    public int Orden { get; set; }
    public EstadoLote EstadoLote { get; set; }
    public string NombreEstadoLote => EstadoLote.ToString();
    public int CantidadPlantadaTotal { get; set; }
    public bool AdquiridoPorTransferencia { get; set; }
    public int TitularOriginalId { get; set; }
    public string NombreTitularOriginal { get; set; } = string.Empty;
}

public class ResumenProductivoTemporadaClienteDto
{
    public int TemporadaId { get; set; }
    public string NombreTemporada { get; set; } = string.Empty;
    public EstadoTemporada EstadoTemporada { get; set; }
    public string NombreEstadoTemporada => EstadoTemporada.ToString();
    public int CantidadLotesPlantados { get; set; }
    public int BulbosPlantados { get; set; }
    public int CantidadLotesCosechados { get; set; }
    public int BulbosCosechados { get; set; }
    public int DiferenciaNeta => BulbosCosechados - BulbosPlantados;
    public decimal PorcentajeRendimiento => BulbosPlantados > 0
        ? Math.Round((decimal)BulbosCosechados / BulbosPlantados * 100m, 2)
        : 0m;
}

public class ReporteIntegralClienteDto
{
    public int ClienteId { get; set; }
    public string CodigoCliente { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string? Email { get; set; }
    public EstadoCliente EstadoCliente { get; set; }
    public string NombreEstadoCliente => EstadoCliente.ToString();

    public int TotalBulbosEnStock { get; set; }
    public List<StockPorClaseDto> StockDisponiblePorClase { get; set; } = new();

    public int TotalLotesVigentes { get; set; }
    public int TotalBulbosPlantadosEnLotesVigentes { get; set; }
    public List<DerechoLoteClienteDto> DerechosSobreLotesPlantados { get; set; } = new();

    public List<ResumenProductivoTemporadaClienteDto> HistorialProductivoPorTemporada { get; set; } = new();
    public List<MovimientoBulbosDto> HistorialMovimientos { get; set; } = new();
    public List<TransferenciaLoteDto> HistorialTransferencias { get; set; } = new();
    public List<VentaDto> HistorialComprasVentas { get; set; } = new();
}

public class ComparativaClaseBulboDto
{
    public ClaseBulbo ClaseBulbo { get; set; }
    public string NombreClase => ClaseBulbo.ToString();
    public int CantidadPlantada { get; set; }
    public int CantidadCosechada { get; set; }
    public int DiferenciaNeta => CantidadCosechada - CantidadPlantada;
    public decimal PorcentajeRendimiento => CantidadPlantada > 0
        ? Math.Round((decimal)CantidadCosechada / CantidadPlantada * 100m, 2)
        : 0m;
}

public class RendimientoSucursalDto
{
    public int SucursalId { get; set; }
    public string NombreSucursal { get; set; } = string.Empty;
    public string EstadoCosecha { get; set; } = "Sin Iniciar";
    public int CantidadLotes { get; set; }
    public int TotalPlantado { get; set; }
    public int TotalCosechado { get; set; }
    public int DiferenciaNeta => TotalCosechado - TotalPlantado;
    public decimal PorcentajeRendimiento => TotalPlantado > 0
        ? Math.Round((decimal)TotalCosechado / TotalPlantado * 100m, 2)
        : 0m;
    public List<ComparativaClaseBulboDto> ComparativaPorClase { get; set; } = new();
}

public class TrazabilidadLoteDto
{
    public int LoteId { get; set; }
    public int SucursalId { get; set; }
    public string NombreSucursal { get; set; } = string.Empty;
    public string IdentificacionFila { get; set; } = string.Empty;
    public int Orden { get; set; }
    public int TitularOriginalId { get; set; }
    public string NombreTitularOriginal { get; set; } = string.Empty;
    public int TitularVigenteId { get; set; }
    public string NombreTitularVigente { get; set; } = string.Empty;
    public bool FueTransferido => TitularOriginalId != TitularVigenteId;
    public int CantidadPlantada { get; set; }
    public int CantidadCosechada { get; set; }
    public int DiferenciaNeta => CantidadCosechada - CantidadPlantada;
    public EstadoLote EstadoLote { get; set; }
    public string NombreEstadoLote => EstadoLote.ToString();
}

public class ReporteRendimientoTemporadaDto
{
    public int TemporadaId { get; set; }
    public string NombreTemporada { get; set; } = string.Empty;
    public EstadoTemporada EstadoTemporada { get; set; }
    public string NombreEstadoTemporada => EstadoTemporada.ToString();
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }

    public int TotalPlantadoTemporada { get; set; }
    public int TotalCosechadoTemporada { get; set; }
    public int DiferenciaNetaTemporada => TotalCosechadoTemporada - TotalPlantadoTemporada;
    public decimal PorcentajeRendimientoGlobal => TotalPlantadoTemporada > 0
        ? Math.Round((decimal)TotalCosechadoTemporada / TotalPlantadoTemporada * 100m, 2)
        : 0m;

    public List<ComparativaClaseBulboDto> ComparativaPorClase { get; set; } = new();
    public List<RendimientoSucursalDto> DesglosePorSucursal { get; set; } = new();
    public List<TrazabilidadLoteDto> TrazabilidadPorLote { get; set; } = new();
}

public class ResumenEjecutivoDto
{
    public int TotalClientesActivos { get; set; }
    public int TotalSucursalesActivas { get; set; }
    public int? TemporadaActivaId { get; set; }
    public string? TemporadaActivaNombre { get; set; }
    public string? TemporadaActivaEstado { get; set; }
    public int TotalLotesTemporadaActiva { get; set; }
    public int TotalBulbosPlantadosTemporadaActiva { get; set; }
    public int TotalBulbosCosechadosTemporadaActiva { get; set; }
    public int TotalVentasRegistradas { get; set; }
    public int TotalVentasCompletadas { get; set; }
    public decimal MontoTotalVentasCompletadas { get; set; }
    public decimal MontoTotalVentasEnCircuito { get; set; }
}
