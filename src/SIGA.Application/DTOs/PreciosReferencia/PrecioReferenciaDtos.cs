using System.ComponentModel.DataAnnotations;
using SIGA.Domain.Enums;

namespace SIGA.Application.DTOs.PreciosReferencia;

public class PrecioReferenciaDto
{
    public int Id { get; set; }
    public int TemporadaId { get; set; }
    public string NombreTemporada { get; set; } = string.Empty;
    public ClaseBulbo ClaseBulbo { get; set; }
    public string NombreClase => ClaseBulbo.ToString();
    public decimal PrecioReferencia { get; set; }
    public DateTime FechaVigencia { get; set; }
}

public class ConfigurarPrecioReferenciaDto
{
    [Range(1, int.MaxValue, ErrorMessage = "El identificador de temporada es requerido.")]
    public int TemporadaId { get; set; }

    [Required(ErrorMessage = "La clase de bulbo es requerida.")]
    public ClaseBulbo ClaseBulbo { get; set; }

    public ClaseBulbo Clase { get => ClaseBulbo; set => ClaseBulbo = value; }

    [Range(0.01, double.MaxValue, ErrorMessage = "El precio de referencia debe ser mayor a cero.")]
    public decimal PrecioReferencia { get; set; }
}

public class ItemPrecioTemporadaDto
{
    [Required]
    public ClaseBulbo ClaseBulbo { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "El precio de referencia debe ser mayor a cero.")]
    public decimal PrecioReferencia { get; set; }
}

public class ConfigurarPreciosTemporadaDto
{
    [Range(1, int.MaxValue, ErrorMessage = "El identificador de temporada es requerido.")]
    public int TemporadaId { get; set; }

    [Required(ErrorMessage = "Debe especificar la lista de precios.")]
    [MinLength(1, ErrorMessage = "Debe incluir al menos un precio de referencia.")]
    public List<ItemPrecioTemporadaDto> Precios { get; set; } = new();
}

public class PrecioClaseDto : PrecioReferenciaDto { }
public class CrearPrecioClaseDto : ConfigurarPrecioReferenciaDto { }
