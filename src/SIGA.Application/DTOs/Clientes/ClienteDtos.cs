using SIGA.Domain.Enums;

namespace SIGA.Application.DTOs.Clientes;

public class ClienteDto
{
    public int Id { get; set; }
    public int PersonaId { get; set; }
    public int? ContratoId { get; set; }
    public string? NumeroContrato { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string CodigoCliente { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public EstadoCliente EstadoCliente { get; set; }
    public DateTime FechaRegistro { get; set; }
}

public class CreateClienteDto
{
    public int? PersonaId { get; set; }

    // Si se crea con nueva persona:
    public string? Nombre { get; set; }
    public string? Apellido { get; set; }
    public string? Documento { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }

    public string? CodigoCliente { get; set; } // Si es null, se genera automáticamente
    public int? ContratoId { get; set; }
    public string? Observaciones { get; set; }
    public bool? Activo { get; set; }
}

public class UpdateClienteDto
{
    public string? Nombre { get; set; }
    public string? Apellido { get; set; }
    public string? Documento { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? CodigoCliente { get; set; }
    public int? ContratoId { get; set; }
    public string? Observaciones { get; set; }
    public EstadoCliente? EstadoCliente { get; set; }
    public bool? Activo { get; set; }
}
