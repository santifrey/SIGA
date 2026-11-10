using SIGA.Domain.Common;
using SIGA.Domain.Enums;
using SIGA.Domain.Exceptions;

namespace SIGA.Domain.Entities;

public class Venta : BaseEntity
{
    public int? OperacionComercialId { get; set; }
    public virtual OperacionComercial? OperacionComercial { get; set; }

    public int VendedorId { get; set; }
    public virtual Usuario? Vendedor { get; set; }

    public int OrigenParticipanteId { get; set; } = Empresa.SingletonId; // Por defecto Empresa Singleton
    public virtual ParticipanteComercial? OrigenParticipante { get; set; }

    public int ClienteId { get; set; }
    public virtual Cliente Cliente { get; set; } = null!;

    public int? TemporadaId { get; set; }
    public virtual Temporada? Temporada { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public decimal MontoTotal { get; set; }
    public EstadoVenta Estado { get; set; } = EstadoVenta.Registrada;
    public string? Observaciones { get; set; }

    public virtual ICollection<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();
    public virtual Contrato? Contrato { get; set; }
    public virtual Factura? Factura { get; set; }
    public virtual ICollection<RegistroAprobacion> HistorialAprobaciones { get; set; } = new List<RegistroAprobacion>();

    public Venta()
    {
    }

    public Venta(int vendedorId, int clienteId, int? temporadaId = null, int origenParticipanteId = Empresa.SingletonId, string? observaciones = null)
    {
        if (vendedorId <= 0)
            throw new OperacionInvalidaException("El vendedor es requerido.");
        if (clienteId <= 0)
            throw new OperacionInvalidaException("El cliente comprador es requerido.");
        if (origenParticipanteId <= 0)
            throw new OperacionInvalidaException("El participante vendedor de origen es inválido.");
        if (origenParticipanteId == clienteId)
            throw new OperacionInvalidaException("El participante vendedor y el comprador no pueden ser el mismo.");

        VendedorId = vendedorId;
        ClienteId = clienteId;
        TemporadaId = temporadaId;
        OrigenParticipanteId = origenParticipanteId;
        Observaciones = observaciones;
        Fecha = DateTime.UtcNow;
        Estado = EstadoVenta.Registrada;
        MontoTotal = 0m;
    }

    public void AgregarDetalle(ClaseBulbo clase, int cantidad, decimal precioUnitarioPactado)
    {
        if (Estado != EstadoVenta.Registrada && Estado != EstadoVenta.Rechazada)
            throw new OperacionInvalidaException("Solo se pueden agregar o modificar detalles cuando la venta está en estado Registrada o Rechazada.");

        if (cantidad <= 0)
            throw new OperacionInvalidaException("La cantidad vendida debe ser mayor a cero.");

        if (precioUnitarioPactado <= 0)
            throw new OperacionInvalidaException("El precio unitario pactado debe ser mayor a cero.");

        var existente = Detalles.FirstOrDefault(d => d.ClaseBulbo == clase && !d.IsDeleted);
        if (existente != null)
        {
            existente.Actualizar(cantidad, precioUnitarioPactado);
        }
        else
        {
            Detalles.Add(new DetalleVenta(Id, clase, cantidad, precioUnitarioPactado)
            {
                Venta = this
            });
        }

        RecalcularMontoTotal();
    }

    public void RecalcularMontoTotal()
    {
        MontoTotal = Detalles.Where(d => !d.IsDeleted).Sum(d => d.Subtotal);
    }

    public void EnviarARevision(int usuarioId, string? observaciones = null)
    {
        if (Estado != EstadoVenta.Registrada && Estado != EstadoVenta.Rechazada)
            throw new OperacionInvalidaException($"No se puede enviar a revisión una venta en estado '{Estado}'.");

        if (!Detalles.Any(d => !d.IsDeleted && d.Cantidad > 0))
            throw new OperacionInvalidaException("La venta debe tener al menos un ítem de detalle antes de enviarse a revisión.");

        RecalcularMontoTotal();
        Estado = EstadoVenta.EnRevisionAdministracion;

        HistorialAprobaciones.Add(new RegistroAprobacion(
            Id,
            usuarioId,
            AccionAprobacion.EnviadoARevision,
            observaciones ?? "Venta enviada a revisión de Secretaría / Administración"));
    }

    public Contrato GenerarContrato(string numeroContrato, int usuarioGeneradorId, string? archivoUrl = null)
    {
        if (Estado == EstadoVenta.Anulada)
            throw new OperacionInvalidaException("No se puede generar un contrato para una venta anulada.");

        if (string.IsNullOrWhiteSpace(numeroContrato))
            throw new OperacionInvalidaException("El número de contrato es obligatorio.");

        if (Contrato != null && !Contrato.IsDeleted)
        {
            Contrato.NumeroContrato = numeroContrato.Trim();
            Contrato.ArchivoUrl = archivoUrl;
            Contrato.FechaGeneracion = DateTime.UtcNow;
            return Contrato;
        }

        Contrato = new Contrato(Id, numeroContrato.Trim(), usuarioGeneradorId, archivoUrl)
        {
            Venta = this
        };
        return Contrato;
    }

    public void AprobarAdministracion(int usuarioAprobadorId, string? observaciones = null)
    {
        if (Estado != EstadoVenta.EnRevisionAdministracion)
            throw new OperacionInvalidaException($"La venta debe estar en estado 'EnRevisionAdministracion' para ser aprobada por Administración (estado actual: '{Estado}').");

        if (Contrato == null || Contrato.IsDeleted)
            throw new OperacionInvalidaException("Debe generarse el contrato de la venta antes de la aprobación administrativa.");

        Contrato.Firmar();
        Estado = EstadoVenta.AprobadaAdministracion;

        HistorialAprobaciones.Add(new RegistroAprobacion(
            Id,
            usuarioAprobadorId,
            AccionAprobacion.AprobadoAdministracion,
            observaciones ?? "Contrato y operación aprobados por Secretaría / Administración"));
    }

    public Factura EmitirFactura(string numeroFactura, int usuarioEmisionId, string? archivoUrl = null, string? observaciones = null)
    {
        if (Estado != EstadoVenta.AprobadaAdministracion)
            throw new OperacionInvalidaException($"La venta debe estar en estado 'AprobadaAdministracion' para poder emitir su factura (estado actual: '{Estado}').");

        if (string.IsNullOrWhiteSpace(numeroFactura))
            throw new OperacionInvalidaException("El número de factura es obligatorio.");

        RecalcularMontoTotal();

        Factura = new Factura(Id, numeroFactura.Trim(), MontoTotal, usuarioEmisionId, archivoUrl)
        {
            Venta = this
        };

        Estado = EstadoVenta.Facturada;

        HistorialAprobaciones.Add(new RegistroAprobacion(
            Id,
            usuarioEmisionId,
            AccionAprobacion.Facturado,
            observaciones ?? $"Factura {numeroFactura.Trim()} emitida por Contabilidad"));

        return Factura;
    }

    public void AprobarFinal(int usuarioAprobadorId, int operacionComercialId, string? observaciones = null)
    {
        if (Estado != EstadoVenta.Facturada)
            throw new OperacionInvalidaException($"La venta debe estar en estado 'Facturada' para recibir la aprobación final (estado actual: '{Estado}').");

        if (Contrato == null || Contrato.IsDeleted)
            throw new OperacionInvalidaException("La venta no posee un contrato válido.");

        if (Factura == null || Factura.IsDeleted)
            throw new OperacionInvalidaException("La venta no posee una factura emitida.");

        if (operacionComercialId <= 0)
            throw new OperacionInvalidaException("Se requiere la operación comercial vinculada para completar la venta.");

        OperacionComercialId = operacionComercialId;
        Factura.MarcarPagada();
        Estado = EstadoVenta.Completada;

        HistorialAprobaciones.Add(new RegistroAprobacion(
            Id,
            usuarioAprobadorId,
            AccionAprobacion.AprobacionFinal,
            observaciones ?? "Aprobación final completada y movimiento comercial de bulbos confirmado"));
    }

    public void Rechazar(int usuarioId, string motivo)
    {
        if (Estado == EstadoVenta.Completada || Estado == EstadoVenta.Anulada)
            throw new OperacionInvalidaException($"No se puede rechazar una venta en estado '{Estado}'.");

        if (string.IsNullOrWhiteSpace(motivo))
            throw new OperacionInvalidaException("Debe indicar el motivo del rechazo u observación.");

        Estado = EstadoVenta.Rechazada;

        HistorialAprobaciones.Add(new RegistroAprobacion(
            Id,
            usuarioId,
            AccionAprobacion.Rechazado,
            motivo.Trim()));
    }
}
