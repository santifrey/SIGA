using SIGA.Domain.Enums;

namespace SIGA.Application.DTOs.Lotes;

public class DetallePlantacionDto
{
    public int Id { get; set; }
    public int PlantacionId { get; set; }
    public int LoteId { get; set; }
    public ClaseBulbo ClaseBulbo { get; set; }
    public string NombreClase => ClaseBulbo.ToString();
    public int Cantidad { get; set; }
}

public class LoteDto
{
    public int Id { get; set; }
    public int FilaId { get; set; }
    public string IdentificacionFila { get; set; } = string.Empty;
    public int SucursalId { get; set; }
    public string NombreSucursal { get; set; } = string.Empty;
    public int ClienteId { get; set; }
    public string CodigoCliente { get; set; } = string.Empty;
    public string NombreCliente { get; set; } = string.Empty;
    public int TitularVigenteId { get; set; }
    public int TemporadaId { get; set; }
    public string NombreTemporada { get; set; } = string.Empty;
    public int Orden { get; set; }
    public int CantidadPlantadaTotal { get; set; }
    public EstadoLote Estado { get; set; }
    public string NombreEstado => Estado.ToString();
    public List<DetallePlantacionDto> Detalles { get; set; } = new();
    public DateTime FechaCreacion { get; set; }
}
