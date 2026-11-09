using SIGA.Application.DTOs.Lotes;
using SIGA.Domain.Enums;

namespace SIGA.Application.DTOs.Plantaciones;

public class PlantacionDto
{
    public int Id { get; set; }
    public int TemporadaId { get; set; }
    public string NombreTemporada { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public EstadoPlantacion Estado { get; set; }
    public string NombreEstado => Estado.ToString();
    public int UsuarioCreadorId { get; set; }
    public string? Observaciones { get; set; }
    public int TotalBulbos { get; set; }
    public List<DetallePlantacionDto> Detalles { get; set; } = new();
    public DateTime FechaCreacion { get; set; }
}

public class CreatePlantacionDto
{
    public int TemporadaId { get; set; }
    public string? Observaciones { get; set; }
}

public class DetalleDistribucionDto
{
    public ClaseBulbo Clase { get; set; }
    public int Cantidad { get; set; }
}

public class RegistrarDistribucionDto
{
    public int? PlantacionId { get; set; }
    public int TemporadaId { get; set; }
    public int SucursalId { get; set; }
    public string IdentificacionFila { get; set; } = string.Empty;
    public int CapacidadFila { get; set; } = 0;
    public int ClienteId { get; set; }
    public List<DetalleDistribucionDto> Detalles { get; set; } = new();
}
