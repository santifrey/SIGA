using SIGA.Domain.Common;
using SIGA.Domain.Enums;
using SIGA.Domain.Exceptions;

namespace SIGA.Domain.Entities;

public class Contrato : BaseEntity
{
    public int VentaId { get; set; }
    public virtual Venta Venta { get; set; } = null!;

    public string NumeroContrato { get; set; } = string.Empty;
    public DateTime FechaGeneracion { get; set; } = DateTime.UtcNow;
    public string? ArchivoUrl { get; set; }
    public EstadoContrato Estado { get; set; } = EstadoContrato.Generado;

    public int UsuarioGeneradorId { get; set; }
    public virtual Usuario? UsuarioGenerador { get; set; }

    public Contrato()
    {
    }

    public Contrato(int ventaId, string numeroContrato, int usuarioGeneradorId, string? archivoUrl = null)
    {
        if (string.IsNullOrWhiteSpace(numeroContrato))
            throw new OperacionInvalidaException("El número de contrato es obligatorio.");

        VentaId = ventaId;
        NumeroContrato = numeroContrato.Trim();
        UsuarioGeneradorId = usuarioGeneradorId;
        ArchivoUrl = archivoUrl;
        FechaGeneracion = DateTime.UtcNow;
        Estado = EstadoContrato.Generado;
    }

    public void Firmar()
    {
        if (Estado == EstadoContrato.Anulado)
            throw new OperacionInvalidaException("No se puede firmar un contrato anulado.");

        Estado = EstadoContrato.Firmado;
    }
}
