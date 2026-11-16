namespace SIGA.Domain.Entities;

public class Permiso
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Modulo { get; set; } = string.Empty;

    // Propiedad de navegación
    public virtual ICollection<RolPermiso> RolPermisos { get; set; } = new List<RolPermiso>();
}
