using SIGA.Domain.Enums;

namespace SIGA.Domain.Entities;

public class Empresa : ParticipanteComercial
{
    public const int SingletonId = 1;

    public Empresa()
    {
        TipoParticipante = TipoParticipante.Empresa;
    }

    public string RazonSocial { get; set; } = string.Empty;
    public string CUIT { get; set; } = string.Empty;
    public string? DireccionFiscal { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public bool EsCasaMatriz { get; set; } = false;
}
