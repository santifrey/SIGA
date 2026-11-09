using SIGA.Domain.Common;
using SIGA.Domain.Enums;
using SIGA.Domain.Exceptions;

namespace SIGA.Domain.Entities;

public class TransferenciaLote : BaseEntity
{
    public int LoteId { get; set; }
    public virtual Lote Lote { get; set; } = null!;

    public int TitularAnteriorId { get; set; }
    public virtual ParticipanteComercial TitularAnterior { get; set; } = null!;

    public int NuevoTitularId { get; set; }
    public virtual ParticipanteComercial NuevoTitular { get; set; } = null!;

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public int UsuarioCreadorId { get; set; }
    public virtual Usuario? UsuarioCreador { get; set; }

    public EstadoTransferencia Estado { get; set; } = EstadoTransferencia.Pendiente;
    public string? Observaciones { get; set; }

    public int? OperacionComercialId { get; set; }
    public virtual OperacionComercial? OperacionComercial { get; set; }

    public TransferenciaLote()
    {
    }

    public TransferenciaLote(
        int loteId, 
        int titularAnteriorId, 
        int nuevoTitularId, 
        int usuarioCreadorId, 
        string? observaciones = null,
        int? operacionComercialId = null)
    {
        if (loteId <= 0)
            throw new OperacionInvalidaException("El lote a transferir es inválido.");

        if (titularAnteriorId <= 0 || nuevoTitularId <= 0)
            throw new OperacionInvalidaException("Los titulares anterior y nuevo deben ser identificadores válidos.");

        if (titularAnteriorId == nuevoTitularId)
            throw new OperacionInvalidaException("El titular anterior no puede ser igual al nuevo titular.");

        LoteId = loteId;
        TitularAnteriorId = titularAnteriorId;
        NuevoTitularId = nuevoTitularId;
        UsuarioCreadorId = usuarioCreadorId;
        Observaciones = observaciones;
        OperacionComercialId = operacionComercialId;
        Estado = EstadoTransferencia.Pendiente;
        Fecha = DateTime.UtcNow;
    }

    public void Confirmar()
    {
        if (Estado != EstadoTransferencia.Pendiente)
            throw new OperacionInvalidaException($"No se puede confirmar una transferencia en estado '{Estado}'. Solo se pueden confirmar transferencias en estado 'Pendiente'.");

        Estado = EstadoTransferencia.Confirmada;
    }

    public void Anular(string motivo)
    {
        if (Estado == EstadoTransferencia.Anulada)
            throw new OperacionInvalidaException("La transferencia ya se encuentra anulada.");

        Estado = EstadoTransferencia.Anulada;
        Observaciones = string.IsNullOrWhiteSpace(Observaciones) 
            ? $"[Anulado]: {motivo}" 
            : $"{Observaciones} | [Anulado]: {motivo}";
    }
}
