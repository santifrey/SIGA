using SIGA.Domain.Enums;

namespace SIGA.Application.DTOs.Transferencias;

public class TransferirLoteDto
{
    public int LoteId { get; set; }
    public int NuevoTitularId { get; set; }
    public string? Observaciones { get; set; }
}

public class TransferenciaLoteDto
{
    public int Id { get; set; }
    public int LoteId { get; set; }
    public string IdentificacionFila { get; set; } = string.Empty;
    public string NombreSucursal { get; set; } = string.Empty;
    public int TitularAnteriorId { get; set; }
    public string NombreTitularAnterior { get; set; } = string.Empty;
    public int NuevoTitularId { get; set; }
    public string NombreNuevoTitular { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public EstadoTransferencia Estado { get; set; }
    public string NombreEstado => Estado.ToString();
    public string? Observaciones { get; set; }
}

public class HistorialTitularidadDto
{
    public int LoteId { get; set; }
    public string IdentificacionFila { get; set; } = string.Empty;
    public string NombreSucursal { get; set; } = string.Empty;
    public int TitularOriginalId { get; set; }
    public string NombreTitularOriginal { get; set; } = string.Empty;
    public int TitularVigenteId { get; set; }
    public string NombreTitularVigente { get; set; } = string.Empty;
    public int CantidadPlantadaTotal { get; set; }
    public List<TransferenciaLoteDto> Transferencias { get; set; } = new();
}

public class AnularTransferenciaDto
{
    public string Motivo { get; set; } = string.Empty;
}
