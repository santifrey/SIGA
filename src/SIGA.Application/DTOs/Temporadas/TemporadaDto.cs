using System.Text.Json;
using System.Text.Json.Serialization;
using SIGA.Domain.Enums;

namespace SIGA.Application.DTOs.Temporadas;

public class TemporadaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public EstadoTemporada Estado { get; set; }
    public string NombreEstado => Estado.ToString();
    public decimal PorcentajeComisionCompra { get; set; }
    public int UsuarioCreadorId { get; set; }
    public int TotalFilas { get; set; }
    public int TotalLotes { get; set; }
    public int TotalPlantaciones { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class CreateTemporadaDto
{
    public string Nombre { get; set; } = string.Empty;
    public DateTime? FechaInicio { get; set; }
    public decimal? PorcentajeComisionCompra { get; set; }
}

public class ActualizarComisionTemporadaDto
{
    public decimal PorcentajeComisionCompra { get; set; }
}

public class CambiarEstadoTemporadaDto
{
    [JsonConverter(typeof(EstadoTemporadaJsonConverter))]
    public EstadoTemporada NuevoEstado { get; set; }
}

public class EstadoTemporadaJsonConverter : JsonConverter<EstadoTemporada>
{
    public override EstadoTemporada Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var intVal))
        {
            if (Enum.IsDefined(typeof(EstadoTemporada), intVal))
                return (EstadoTemporada)intVal;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (!string.IsNullOrWhiteSpace(str))
            {
                var norm = str.Trim().ToLowerInvariant()
                    .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u")
                    .Replace(" ", "");

                return norm switch
                {
                    "planificada" => EstadoTemporada.Planificada,
                    "plantacion" => EstadoTemporada.Plantacion,
                    "encrecimiento" => EstadoTemporada.EnCrecimiento,
                    "cosecha" => EstadoTemporada.Cosecha,
                    "comercializacion" => EstadoTemporada.Comercializacion,
                    "cerrada" => EstadoTemporada.Cerrada,
                    _ => Enum.TryParse<EstadoTemporada>(str, true, out var parsed) 
                        ? parsed 
                        : throw new JsonException($"Valor de estado de temporada inválido: '{str}'.")
                };
            }
        }

        throw new JsonException("No se pudo convertir el valor a EstadoTemporada.");
    }

    public override void Write(Utf8JsonWriter writer, EstadoTemporada value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
