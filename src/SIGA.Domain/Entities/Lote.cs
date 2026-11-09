using SIGA.Domain.Common;
using SIGA.Domain.Enums;
using SIGA.Domain.Exceptions;

namespace SIGA.Domain.Entities;

public class Lote : BaseEntity
{
    public int FilaId { get; set; }
    public virtual Fila Fila { get; set; } = null!;

    public int ClienteId { get; set; }
    public virtual Cliente Cliente { get; set; } = null!;

    public int TemporadaId { get; set; }
    public virtual Temporada Temporada { get; set; } = null!;

    public int Orden { get; set; }
    public int CantidadPlantadaTotal { get; set; }
    public EstadoLote Estado { get; set; } = EstadoLote.Asignado;

    public virtual ICollection<DetallePlantacion> DetallesPlantacion { get; set; } = new List<DetallePlantacion>();
    public virtual ICollection<TransferenciaLote> Transferencias { get; set; } = new List<TransferenciaLote>();
    public virtual ICollection<DetalleCosecha> DetallesCosecha { get; set; } = new List<DetalleCosecha>();

    public int ResolverTitularVigenteId()
    {
        var ultimaConfirmada = Transferencias
            .Where(t => t.Estado == EstadoTransferencia.Confirmada && !t.IsDeleted)
            .OrderByDescending(t => t.Fecha)
            .ThenByDescending(t => t.Id)
            .FirstOrDefault();

        return ultimaConfirmada != null ? ultimaConfirmada.NuevoTitularId : ClienteId;
    }

    public Lote()
    {
    }

    public Lote(int filaId, int clienteId, int temporadaId, int orden)
    {
        if (filaId <= 0)
            throw new OperacionInvalidaException("La fila especificada es inválida.");
        if (clienteId <= 0)
            throw new OperacionInvalidaException("El cliente titular es requerido para el lote.");
        if (temporadaId <= 0)
            throw new OperacionInvalidaException("La temporada es requerida para el lote.");
        if (orden < 0)
            throw new OperacionInvalidaException("El orden del lote en la fila debe ser mayor o igual a cero.");

        FilaId = filaId;
        ClienteId = clienteId;
        TemporadaId = temporadaId;
        Orden = orden;
        Estado = EstadoLote.Asignado;
        CantidadPlantadaTotal = 0;
    }
}
