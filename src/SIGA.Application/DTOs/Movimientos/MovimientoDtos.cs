using SIGA.Domain.Enums;

namespace SIGA.Application.DTOs.Movimientos;

public class CantidadBulbosDto
{
    public ClaseBulbo Clase { get; set; }
    public int Cantidad { get; set; }
}

public class CrearMovimientoDto
{
    public int? OperacionComercialId { get; set; }
    public TipoOperacionComercial TipoOperacion { get; set; } = TipoOperacionComercial.CompraVentaBulbos;
    public int OrigenId { get; set; }
    public int DestinoId { get; set; }
    public string? Observaciones { get; set; }
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
