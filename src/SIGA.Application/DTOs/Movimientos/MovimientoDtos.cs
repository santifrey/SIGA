using System.ComponentModel.DataAnnotations;
using SIGA.Domain.Enums;

namespace SIGA.Application.DTOs.Movimientos;

public class CantidadBulbosDto
{
    [Required]
    public ClaseBulbo Clase { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero.")]
    public int Cantidad { get; set; }
}

public class CrearMovimientoDto
{
    public int? OperacionComercialId { get; set; }
    public TipoOperacionComercial TipoOperacion { get; set; } = TipoOperacionComercial.CompraVentaBulbos;

    [Range(1, int.MaxValue, ErrorMessage = "El origen es requerido.")]
    public int OrigenId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "El destino es requerido.")]
    public int DestinoId { get; set; }

    [StringLength(1000)]
    public string? Observaciones { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Debe ingresar al menos una cantidad de bulbos.")]
    public List<CantidadBulbosDto> Cantidades { get; set; } = new();
}

public class MovimientoBulbosDto
{
    public int Id { get; set; }
    public int OperacionComercialId { get; set; }
    public DateTime Fecha { get; set; }
    public int OrigenId { get; set; }
    public string OrigenNombre { get; set; } = string.Empty;
    public int DestinoId { get; set; }
    public string DestinoNombre { get; set; } = string.Empty;
    public int UsuarioCreadorId { get; set; }
    public EstadoMovimiento Estado { get; set; }
    public string? Observaciones { get; set; }
    public int TotalBulbos { get; set; }
    public List<CantidadBulbosDto> Cantidades { get; set; } = new();
}

public class AnularMovimientoDto
{
    public string Motivo { get; set; } = string.Empty;
}
