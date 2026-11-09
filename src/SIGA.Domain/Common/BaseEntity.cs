namespace SIGA.Domain.Common;

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? FechaEliminacion { get; set; }
}

public abstract class BaseEntity : ISoftDeletable
{
    public int Id { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? FechaEliminacion { get; set; }
}
