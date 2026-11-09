using SIGA.Domain.Enums;

namespace SIGA.Application.DTOs.Stock;

public class StockPorClaseDto
{
    public ClaseBulbo Clase { get; set; }
    public string NombreClase { get; set; } = string.Empty;
    public int Cantidad { get; set; }
}

public class StockClienteDto
{
    public int ClienteId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public string CodigoCliente { get; set; } = string.Empty;
    public List<StockPorClaseDto> StockPorClase { get; set; } = new();
    public int TotalBulbos { get; set; }
}

public class VerificarDisponibilidadDto
{
    public int ClienteId { get; set; }
    public ClaseBulbo Clase { get; set; }
    public int Cantidad { get; set; }
}

public class DisponibilidadStockResponseDto
{
    public int ClienteId { get; set; }
    public ClaseBulbo Clase { get; set; }
    public int CantidadSolicitada { get; set; }
    public int StockDisponible { get; set; }
    public bool Disponible { get; set; }
}
