using SIGA.Domain.Enums;

namespace SIGA.Domain.Entities;

public class Cliente : ParticipanteComercial
{
    public Cliente()
    {
        TipoParticipante = TipoParticipante.Cliente;
    }

    public int PersonaId { get; set; }
    public virtual Persona Persona { get; set; } = null!;

    public int? ContratoId { get; set; }
    public virtual Contrato? Contrato { get; set; }

    public string CodigoCliente { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public EstadoCliente EstadoCliente { get; set; } = EstadoCliente.Activo;
}
