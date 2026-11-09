using SIGA.Domain.Enums;

namespace SIGA.Application.DTOs.Temporadas;

public class TemporadaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public EstadoTemporada Estado { get; set; }
    public string NombreEstado => Estado.ToString();
    public int UsuarioCreadorId { get; set; }
    public int TotalFilas { get; set; }
    public int TotalLotes { get; set; }
    public int TotalPlantaciones { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class CreateTemporadaDto
{
    public string Nombre { get; set; } = string.Empty;
    public DateTime? FechaInicio { get; set; }
}

public class CambiarEstadoTemporadaDto
{
    public EstadoTemporada NuevoEstado { get; set; }
}
