using SIGA.Domain.Common;
using SIGA.Domain.Enums;

namespace SIGA.Domain.Entities;

public class Sucursal : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Ubicacion { get; set; } = string.Empty;
    public EstadoSucursal Estado { get; set; } = EstadoSucursal.Activo;
    public decimal CapacidadTotal { get; set; }
}
