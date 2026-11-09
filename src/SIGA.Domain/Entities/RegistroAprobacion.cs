using SIGA.Domain.Common;
using SIGA.Domain.Enums;

namespace SIGA.Domain.Entities;

public class RegistroAprobacion : BaseEntity
{
    public int VentaId { get; set; }
    public virtual Venta Venta { get; set; } = null!;

    public string TipoEntidad { get; set; } = "Venta";
    public int EntidadId { get; set; }

    public int UsuarioAprobadorId { get; set; }
    public virtual Usuario? UsuarioAprobador { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public AccionAprobacion Accion { get; set; }
    public string? Observaciones { get; set; }

    public RegistroAprobacion()
    {
    }

    public RegistroAprobacion(int ventaId, int usuarioAprobadorId, AccionAprobacion accion, string? observaciones = null)
    {
        VentaId = ventaId;
        EntidadId = ventaId;
        TipoEntidad = "Venta";
        UsuarioAprobadorId = usuarioAprobadorId;
        Accion = accion;
        Observaciones = observaciones;
        Fecha = DateTime.UtcNow;
    }
}
