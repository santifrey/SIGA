using SIGA.Domain.Enums;

namespace SIGA.Domain.Entities;

public class Sucursal : ParticipanteComercial
{
    public Sucursal()
    {
        TipoParticipante = TipoParticipante.Sucursal;
    }

    public string Nombre { get; set; } = string.Empty;
    public string Ubicacion { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public decimal CapacidadTotal { get; set; }
    public bool EsCasaMatriz { get; set; } = false;

    // Datos fiscales / sede
    public string? RazonSocial { get; set; }
    public string? CUIT { get; set; }
    public string? DireccionFiscal { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
}
