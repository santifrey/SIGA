namespace SIGA.Application.DTOs.Sucursales;

public class SucursalDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Ubicacion { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public decimal CapacidadTotal { get; set; }
    public int? ParticipanteComercialId { get; set; }
    public string? ParticipanteRazonSocial { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
}

public class CreateSucursalDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Ubicacion { get; set; } = string.Empty;
    public decimal CapacidadTotal { get; set; }
    public bool Activo { get; set; } = true;
}

public class UpdateSucursalDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Ubicacion { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public decimal CapacidadTotal { get; set; }
}
