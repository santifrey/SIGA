using SIGA.Domain.Enums;

namespace SIGA.Application.DTOs.Filas;

public class FilaDto
{
    public int Id { get; set; }
    public int SucursalId { get; set; }
    public string NombreSucursal { get; set; } = string.Empty;
    public int TemporadaId { get; set; }
    public string NombreTemporada { get; set; } = string.Empty;
    public string Identificacion { get; set; } = string.Empty;
    public int Capacidad { get; set; }
    public EstadoFila Estado { get; set; }
    public string NombreEstado => Estado.ToString();
    public int TotalLotes { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class CreateFilaDto
{
    public int SucursalId { get; set; }
    public int TemporadaId { get; set; }
    public string Identificacion { get; set; } = string.Empty;
    public int Capacidad { get; set; } = 0;
}
