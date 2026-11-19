using SIGA.Domain.Common;

namespace SIGA.Domain.Entities;

public class Persona : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Email { get; set; }

    public string NombreCompleto => $"{Nombre} {Apellido}".Trim();

    // Vinculación a Sucursal (opcional)
    public int? SucursalId { get; set; }
    public virtual Sucursal? Sucursal { get; set; }

    // Propiedades de navegación
    public virtual Usuario? Usuario { get; set; }
    public virtual Cliente? Cliente { get; set; }
}
