using SIGA.Domain.Common;
using SIGA.Domain.Enums;
using SIGA.Domain.Exceptions;

namespace SIGA.Domain.Entities;

public class MovimientoBulbos : BaseEntity
{
    public int OperacionComercialId { get; set; }
    public virtual OperacionComercial OperacionComercial { get; set; } = null!;

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public int OrigenId { get; set; }
    public virtual ParticipanteComercial Origen { get; set; } = null!;

    public int DestinoId { get; set; }
    public virtual ParticipanteComercial Destino { get; set; } = null!;

    public int UsuarioCreadorId { get; set; }
    public virtual Usuario? UsuarioCreador { get; set; }

    public EstadoMovimiento Estado { get; set; } = EstadoMovimiento.Pendiente;
    public string? Observaciones { get; set; }

    public virtual ICollection<CantidadBulbos> Cantidades { get; set; } = new List<CantidadBulbos>();

    public MovimientoBulbos()
    {
    }

    public MovimientoBulbos(
        int operacionComercialId, 
        int origenId, 
        int destinoId, 
        int usuarioCreadorId, 
        string? observaciones = null)
    {
        ValidarOrigenYDestino(origenId, destinoId);

        OperacionComercialId = operacionComercialId;
        OrigenId = origenId;
        DestinoId = destinoId;
        UsuarioCreadorId = usuarioCreadorId;
        Observaciones = observaciones;
        Estado = EstadoMovimiento.Pendiente;
        Fecha = DateTime.UtcNow;
    }

    public void AgregarCantidad(ClaseBulbo clase, int cantidad)
    {
        if (Estado != EstadoMovimiento.Pendiente)
            throw new OperacionInvalidaException("No se pueden modificar cantidades en un movimiento que no esté en estado Pendiente.");

        if (cantidad <= 0)
            throw new OperacionInvalidaException("La cantidad debe ser mayor a cero.");

        var existente = Cantidades.FirstOrDefault(c => c.ClaseBulbo == clase);
        if (existente != null)
        {
            existente.Cantidad += cantidad;
        }
        else
        {
            Cantidades.Add(new CantidadBulbos
            {
                ClaseBulbo = clase,
                Cantidad = cantidad,
                MovimientoBulbos = this
            });
        }
    }

    public void Confirmar()
    {
        if (Estado != EstadoMovimiento.Pendiente)
            throw new OperacionInvalidaException($"No se puede confirmar un movimiento en estado '{Estado}'. Solo se pueden confirmar movimientos en estado 'Pendiente'.");

        ValidarOrigenYDestino(OrigenId, DestinoId);

        if (!Cantidades.Any() || CalcularTotal() <= 0)
            throw new OperacionInvalidaException("El movimiento debe contener al menos una cantidad de bulbos mayor a cero para ser confirmado.");

        Estado = EstadoMovimiento.Confirmado;

        if (OperacionComercial != null)
        {
            // Si la operación comercial tiene todos sus movimientos confirmados, pasa a confirmada
            if (OperacionComercial.Movimientos.All(m => m.Estado == EstadoMovimiento.Confirmado))
            {
                OperacionComercial.Estado = EstadoOperacion.Confirmada;
            }
        }
    }

    public void Anular(string motivo)
    {
        if (Estado == EstadoMovimiento.Anulado)
            throw new OperacionInvalidaException("El movimiento ya se encuentra anulado.");

        Estado = EstadoMovimiento.Anulado;

        var prefix = string.IsNullOrWhiteSpace(motivo) ? "[ANULADO]" : $"[ANULADO]: {motivo}";
        Observaciones = string.IsNullOrWhiteSpace(Observaciones) 
            ? prefix 
            : $"{Observaciones} | {prefix}";

        if (OperacionComercial != null)
        {
            if (OperacionComercial.Movimientos.All(m => m.Estado == EstadoMovimiento.Anulado))
            {
                OperacionComercial.Estado = EstadoOperacion.Anulada;
            }
        }
    }

    public int CalcularTotal()
    {
        return Cantidades.Sum(c => c.Cantidad);
    }

    private static void ValidarOrigenYDestino(int origenId, int destinoId)
    {
        if (origenId <= 0 || destinoId <= 0)
            throw new OperacionInvalidaException("El origen y el destino deben ser identificadores válidos.");

        if (origenId == destinoId)
            throw new OperacionInvalidaException("El participante Origen no puede ser igual al participante Destino.");
    }
}
