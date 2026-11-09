using SIGA.Domain.Common;
using SIGA.Domain.Enums;
using SIGA.Domain.Exceptions;

namespace SIGA.Domain.Entities;

public class Cosecha : BaseEntity
{
    public int TemporadaId { get; set; }
    public virtual Temporada Temporada { get; set; } = null!;

    public int SucursalId { get; set; }
    public virtual Sucursal Sucursal { get; set; } = null!;

    public DateTime FechaInicio { get; set; } = DateTime.UtcNow;
    public DateTime? FechaFin { get; set; }
    public EstadoCosecha Estado { get; set; } = EstadoCosecha.EnProceso;

    public int UsuarioCreadorId { get; set; }
    public virtual Usuario? UsuarioCreador { get; set; }

    public string? Observaciones { get; set; }

    public virtual ICollection<DetalleCosecha> DetallesCosecha { get; set; } = new List<DetalleCosecha>();

    public Cosecha()
    {
    }

    public Cosecha(int temporadaId, int sucursalId, int usuarioCreadorId, string? observaciones = null)
    {
        if (temporadaId <= 0)
            throw new OperacionInvalidaException("La temporada es requerida para registrar la cosecha.");
        if (sucursalId <= 0)
            throw new OperacionInvalidaException("La sucursal es requerida para registrar la cosecha.");
        if (usuarioCreadorId <= 0)
            throw new OperacionInvalidaException("El usuario creador es requerido.");

        TemporadaId = temporadaId;
        SucursalId = sucursalId;
        UsuarioCreadorId = usuarioCreadorId;
        Observaciones = observaciones;
        FechaInicio = DateTime.UtcNow;
        Estado = EstadoCosecha.EnProceso;
    }

    public void RegistrarDetalle(int loteId, ClaseBulbo clase, int cantidad)
    {
        if (Estado == EstadoCosecha.Cerrada)
            throw new OperacionInvalidaException("No se pueden registrar ni modificar cantidades en una cosecha cerrada.");

        if (loteId <= 0)
            throw new OperacionInvalidaException("El lote especificado es inválido.");

        if (cantidad < 0)
            throw new OperacionInvalidaException("La cantidad cosechada no puede ser negativa.");

        var existente = DetallesCosecha.FirstOrDefault(d => d.LoteId == loteId && d.ClaseBulbo == clase && !d.IsDeleted);
        if (existente != null)
        {
            existente.Cantidad = cantidad;
        }
        else
        {
            DetallesCosecha.Add(new DetalleCosecha(Id, loteId, clase, cantidad)
            {
                Cosecha = this
            });
        }
    }

    public void Cerrar()
    {
        if (Estado == EstadoCosecha.Cerrada)
            throw new OperacionInvalidaException("La cosecha de esta sucursal ya se encuentra cerrada.");

        Estado = EstadoCosecha.Cerrada;
        FechaFin = DateTime.UtcNow;
    }

    public int CalcularTotalCosechado()
    {
        return DetallesCosecha.Where(d => !d.IsDeleted).Sum(d => d.Cantidad);
    }
}
