using System.ComponentModel.DataAnnotations;
using SIGA.Domain.Enums;

namespace SIGA.Application.DTOs.Cosechas;

public class DetalleCosechaDto
{
    public int Id { get; set; }
    public int CosechaId { get; set; }
    public int LoteId { get; set; }
    public string IdentificacionFila { get; set; } = string.Empty;
    public int TitularOriginalId { get; set; }
    public string NombreTitularOriginal { get; set; } = string.Empty;
    public int TitularVigenteId { get; set; }
    public string NombreTitularVigente { get; set; } = string.Empty;
    public ClaseBulbo ClaseBulbo { get; set; }
    public string NombreClase => ClaseBulbo.ToString();
    public int Cantidad { get; set; }
}

public class CosechaDto
{
    public int Id { get; set; }
    public int TemporadaId { get; set; }
    public string NombreTemporada { get; set; } = string.Empty;
    public int SucursalId { get; set; }
    public string NombreSucursal { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public EstadoCosecha Estado { get; set; }
    public string NombreEstado => Estado.ToString();
    public int UsuarioCreadorId { get; set; }
    public string? Observaciones { get; set; }
    public int TotalCosechado { get; set; }
    public List<DetalleCosechaDto> Detalles { get; set; } = new();
    public DateTime FechaCreacion { get; set; }
}

public class CreateCosechaDto
{
    [Range(1, int.MaxValue, ErrorMessage = "El identificador de temporada es requerido.")]
    public int TemporadaId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "El identificador de sucursal es requerido.")]
    public int SucursalId { get; set; }

    [StringLength(1000)]
    public string? Observaciones { get; set; }
}

public class ItemDetalleCosechaDto
{
    [Required]
    public ClaseBulbo ClaseBulbo { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "La cantidad cosechada no puede ser negativa.")]
    public int Cantidad { get; set; }
}

public class RegistrarDetalleCosechaDto
{
    [Range(1, int.MaxValue, ErrorMessage = "El identificador del lote es requerido.")]
    public int LoteId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Debe incluir al menos una clase de bulbo cosechada.")]
    public List<ItemDetalleCosechaDto> Cantidades { get; set; } = new();
}
