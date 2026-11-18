using SIGA.Domain.Common;
using SIGA.Domain.Enums;

namespace SIGA.Domain.Entities;

public class RegistroAprobacion : BaseEntity
{
    public int MovimientoComercialId { get; set; }
    public virtual MovimientoComercial? MovimientoComercial { get; set; }


    public string TipoEntidad { get; set; } = "MovimientoComercial";
    public int EntidadId { get; set; }

    public int UsuarioAprobadorId { get; set; }
    public virtual Usuario? UsuarioAprobador { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public AccionAprobacion Accion { get; set; }
    public string? Observaciones { get; set; }

    public RegistroAprobacion()
    {
    }

    public RegistroAprobacion(int movimientoComercialId, int usuarioAprobadorId, AccionAprobacion accion, string? observaciones = null)
    {
        MovimientoComercialId = movimientoComercialId;
        EntidadId = movimientoComercialId;
        TipoEntidad = "MovimientoComercial";
        UsuarioAprobadorId = usuarioAprobadorId;
        Accion = accion;
        Observaciones = observaciones;
        Fecha = DateTime.UtcNow;
    }
}
