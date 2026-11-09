using SIGA.Domain.Common;
using SIGA.Domain.Enums;

namespace SIGA.Domain.Entities;

public class OperacionComercial : BaseEntity
{
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public TipoOperacionComercial Tipo { get; set; } = TipoOperacionComercial.CompraVentaBulbos;
    public EstadoOperacion Estado { get; set; } = EstadoOperacion.Pendiente;
    public int UsuarioCreadorId { get; set; }
    public virtual Usuario? UsuarioCreador { get; set; }
    public string? Observaciones { get; set; }

    public virtual ICollection<MovimientoBulbos> Movimientos { get; set; } = new List<MovimientoBulbos>();
}
