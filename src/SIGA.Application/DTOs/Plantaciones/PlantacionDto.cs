using System.Text.Json.Serialization;
using SIGA.Application.DTOs.Lotes;
using SIGA.Domain.Enums;

namespace SIGA.Application.DTOs.Plantaciones;

public class PlantacionDto
{
    public int Id { get; set; }
    public int TemporadaId { get; set; }
    public string NombreTemporada { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public EstadoPlantacion Estado { get; set; }
    public string NombreEstado => Estado.ToString();
    public int UsuarioCreadorId { get; set; }
    public string? Observaciones { get; set; }
    public int TotalBulbos { get; set; }
    public List<DetallePlantacionDto> Detalles { get; set; } = new();
    public DateTime FechaCreacion { get; set; }
}

public class CreatePlantacionDto
{
    public int TemporadaId { get; set; }
    public string? Observaciones { get; set; }
}

public class DetalleDistribucionDto
{
    public ClaseBulbo Clase { get; set; }
    public int Cantidad { get; set; }
}

public class RegistrarDistribucionDto
{
    public int? PlantacionId { get; set; }
    public int TemporadaId { get; set; }
    public int SucursalId { get; set; }

    /// <summary>
    /// ID de fila existente si fue seleccionada del selector estructurado.
    /// </summary>
    public int? FilaId { get; set; }

    /// <summary>
    /// Número entero de fila para creación o identificación estructurada.
    /// </summary>
    public int? NumeroFila { get; set; }

    /// <summary>
    /// Identificación de fila (ej. "12", "F-01").
    /// </summary>
    public string IdentificacionFila { get; set; } = string.Empty;

    /// <summary>
    /// Capacidad total de la fila en unidades de bulbos.
    /// </summary>
    public int CapacidadFila { get; set; } = 0;

    /// <summary>
    /// Número o consecutivo de orden del lote dentro de la fila.
    /// Si es nulo o 0, el backend asigna automáticamente el siguiente disponible.
    /// </summary>
    public int? NumeroLote { get; set; }

    /// <summary>
    /// Alias de NumeroLote
    /// </summary>
    [JsonPropertyName("orden")]
    public int? Orden
    {
        get => NumeroLote;
        set
        {
            if (value.HasValue && !NumeroLote.HasValue)
                NumeroLote = value;
        }
    }

    public int ClienteId { get; set; }
    public List<DetalleDistribucionDto> Detalles { get; set; } = new();

    // Compatibilidad retroactiva con nombres de propiedades frontend
    [JsonPropertyName("filaIdentificacion")]
    public string? FilaIdentificacionAlias
    {
        get => IdentificacionFila;
        set
        {
            if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(IdentificacionFila))
                IdentificacionFila = value;
        }
    }

    [JsonPropertyName("cantidades")]
    public List<DetalleDistribucionDto>? CantidadesAlias
    {
        get => Detalles;
        set
        {
            if (value != null && (Detalles == null || !Detalles.Any()))
                Detalles = value;
        }
    }
}
