using SIGA.Domain.Enums;

namespace SIGA.Application.DTOs.Sucursales;

public class SucursalDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Ubicacion { get; set; } = string.Empty;
    public EstadoSucursal Estado { get; set; }
    public decimal CapacidadTotal { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
}

public class CreateSucursalDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Ubicacion { get; set; } = string.Empty;
    public decimal CapacidadTotal { get; set; }
}

public class UpdateSucursalDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Ubicacion { get; set; } = string.Empty;
    public EstadoSucursal Estado { get; set; }
    public decimal CapacidadTotal { get; set; }
}
