using SIGA.Domain.Common;
using SIGA.Domain.Enums;
using SIGA.Domain.Exceptions;

namespace SIGA.Domain.Entities;

public class MovimientoComercial : BaseEntity
{
    public TipoMovimientoComercial Tipo { get; set; } = TipoMovimientoComercial.VentaACliente;
    public EstadoMovimientoComercial Estado { get; set; } = EstadoMovimientoComercial.PendienteRevision;

    public int OrigenParticipanteId { get; set; }
    public virtual ParticipanteComercial? OrigenParticipante { get; set; }

    public int DestinoParticipanteId { get; set; }
    public virtual ParticipanteComercial? DestinoParticipante { get; set; }

    public int? ClienteId { get; set; }
    public virtual Cliente? Cliente { get; set; }

    public int? TemporadaId { get; set; }
    public virtual Temporada? Temporada { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public int UsuarioRegistroId { get; set; }
    public virtual Usuario? UsuarioRegistro { get; set; }

    // Financial breakdown
    public decimal MontoBruto { get; set; } = 0m;
    public decimal PorcentajeComision { get; set; } = 0m;
    public decimal MontoComision { get; set; } = 0m;
    public decimal MontoNeto { get; set; } = 0m;
    public decimal MontoTotal { get; set; } = 0m;

    public string? Observaciones { get; set; }

    public virtual ICollection<MovimientoBulbos> MovimientosBulbos { get; set; } = new List<MovimientoBulbos>();

    public virtual ICollection<DetalleMovimientoComercial> Detalles { get; set; } = new List<DetalleMovimientoComercial>();
    public virtual Contrato? Contrato { get; set; }
    public virtual Factura? Factura { get; set; }
    public virtual ICollection<RegistroAprobacion> HistorialAprobaciones { get; set; } = new List<RegistroAprobacion>();

    public MovimientoComercial()
    {
    }

    public MovimientoComercial(
        TipoMovimientoComercial tipo,
        int usuarioRegistroId,
        int origenParticipanteId,
        int destinoParticipanteId,
        int? clienteId = null,
        int? temporadaId = null,
        decimal porcentajeComision = 0m,
        string? observaciones = null)
    {
        if (usuarioRegistroId <= 0)
            throw new OperacionInvalidaException("El usuario de registro es requerido.");
        if (origenParticipanteId <= 0)
            throw new OperacionInvalidaException("El participante origen es requerido.");
        if (destinoParticipanteId <= 0)
            throw new OperacionInvalidaException("El participante destino es requerido.");
        if (origenParticipanteId == destinoParticipanteId)
            throw new OperacionInvalidaException("El participante origen y destino no pueden ser el mismo.");
        if (tipo == TipoMovimientoComercial.CompraACliente && (porcentajeComision < 0 || porcentajeComision > 100))
            throw new OperacionInvalidaException("El porcentaje de comisión debe situarse entre 0 y 100.");

        Tipo = tipo;
        UsuarioRegistroId = usuarioRegistroId;
        OrigenParticipanteId = origenParticipanteId;
        DestinoParticipanteId = destinoParticipanteId;
        ClienteId = clienteId;
        TemporadaId = temporadaId;
        PorcentajeComision = tipo == TipoMovimientoComercial.CompraACliente ? porcentajeComision : 0m;
        Observaciones = observaciones;
        Fecha = DateTime.UtcNow;
        FechaRegistro = DateTime.UtcNow;
        Estado = EstadoMovimientoComercial.PendienteRevision;

        HistorialAprobaciones.Add(new RegistroAprobacion(
            Id,
            usuarioRegistroId,
            AccionAprobacion.Registrado,
            "Movimiento comercial registrado en estado PendienteRevision"));
    }

    public void AgregarDetalle(ClaseBulbo clase, int cantidad, decimal precioUnitarioPactado)
    {
        if (Estado != EstadoMovimientoComercial.PendienteRevision && Estado != EstadoMovimientoComercial.Rechazado)
            throw new OperacionInvalidaException("Solo se pueden agregar o modificar detalles cuando el movimiento está en PendienteRevision o Rechazado.");

        if (Tipo == TipoMovimientoComercial.TraspasoEntreClientes)
        {
            precioUnitarioPactado = 0m;
        }

        var existente = Detalles.FirstOrDefault(d => d.ClaseBulbo == clase && !d.IsDeleted);
        if (existente != null)
        {
            existente.Actualizar(cantidad, precioUnitarioPactado);
        }
        else
        {
            Detalles.Add(new DetalleMovimientoComercial(Id, clase, cantidad, precioUnitarioPactado)
            {
                MovimientoComercial = this
            });
        }

        RecalcularFinancieros();
    }

    public void RecalcularFinancieros()
    {
        var activos = Detalles.Where(d => !d.IsDeleted).ToList();

        switch (Tipo)
        {
            case TipoMovimientoComercial.VentaACliente:
                MontoBruto = activos.Sum(d => d.Subtotal);
                PorcentajeComision = 0m;
                MontoComision = 0m;
                MontoNeto = MontoBruto;
                MontoTotal = MontoBruto;
                break;

            case TipoMovimientoComercial.CompraACliente:
                MontoBruto = activos.Sum(d => d.Subtotal);
                MontoComision = Math.Round(MontoBruto * (PorcentajeComision / 100m), 2);
                MontoNeto = MontoBruto - MontoComision;
                MontoTotal = MontoNeto;
                break;

            case TipoMovimientoComercial.TraspasoEntreClientes:
                MontoBruto = 0m;
                PorcentajeComision = 0m;
                MontoComision = 0m;
                MontoNeto = 0m;
                MontoTotal = 0m;
                break;
        }
    }

    public Contrato GenerarContrato(string numeroContrato, int usuarioGeneradorId, string? archivoUrl = null)
    {
        if (Estado == EstadoMovimientoComercial.Finalizado)
            throw new OperacionInvalidaException("No se puede generar contrato para un movimiento ya finalizado.");

        if (string.IsNullOrWhiteSpace(numeroContrato))
            throw new OperacionInvalidaException("El número de contrato es obligatorio.");

        if (Contrato != null && !Contrato.IsDeleted)
        {
            Contrato.NumeroContrato = numeroContrato.Trim();
            Contrato.ArchivoUrl = archivoUrl;
            Contrato.FechaGeneracion = DateTime.UtcNow;
            Contrato.Estado = EstadoContrato.Generado;
        }
        else
        {
            Contrato = new Contrato(Id, numeroContrato.Trim(), usuarioGeneradorId, archivoUrl)
            {
                MovimientoComercial = this
            };
        }

        if (Estado == EstadoMovimientoComercial.Rechazado)
        {
            Estado = EstadoMovimientoComercial.PendienteRevision;
            HistorialAprobaciones.Add(new RegistroAprobacion(
                Id,
                usuarioGeneradorId,
                AccionAprobacion.Registrado,
                $"Contrato {numeroContrato.Trim()} corregido/regenerado tras rechazo (Reenviado a revisión)"));
        }

        return Contrato;
    }

    public void AprobarAdmin(int usuarioAprobadorId, bool requiereContrato, string? observaciones = null)
    {
        if (Estado != EstadoMovimientoComercial.PendienteRevision)
            throw new OperacionInvalidaException($"El movimiento comercial debe estar en estado PendienteRevision para ser aprobado (estado actual: '{Estado}').");

        if (requiereContrato)
        {
            if (Contrato == null || Contrato.IsDeleted)
                throw new OperacionInvalidaException("El cliente es nuevo y no registra operaciones previas finalizadas. Se requiere la generación de un contrato antes de la aprobación administrativa.");

            Contrato.Firmar();
        }

        Estado = EstadoMovimientoComercial.AprobadoAdmin;
        HistorialAprobaciones.Add(new RegistroAprobacion(
            Id,
            usuarioAprobadorId,
            AccionAprobacion.AprobadoAdmin,
            observaciones ?? "Movimiento aprobado por Administración"));
    }

    public Factura EmitirFactura(string numeroFactura, int usuarioEmisionId, string? archivoUrl = null, string? observaciones = null)
    {
        if (Estado != EstadoMovimientoComercial.AprobadoAdmin && Estado != EstadoMovimientoComercial.Rechazado)
            throw new OperacionInvalidaException($"El movimiento comercial debe estar en estado AprobadoAdmin o Rechazado para emitir factura (estado actual: '{Estado}').");

        if (Tipo == TipoMovimientoComercial.TraspasoEntreClientes)
            throw new OperacionInvalidaException("Los traspasos entre clientes no generan facturas comerciales.");

        if (string.IsNullOrWhiteSpace(numeroFactura))
            throw new OperacionInvalidaException("El número de factura es obligatorio.");

        RecalcularFinancieros();

        if (Factura != null)
        {
            Factura.NumeroFactura = numeroFactura.Trim();
            Factura.MontoTotal = MontoTotal;
            Factura.UsuarioEmisionId = usuarioEmisionId;
            Factura.ArchivoUrl = archivoUrl;
            Factura.FechaEmision = DateTime.UtcNow;
            Factura.Estado = EstadoFactura.Emitida;
            Factura.IsDeleted = false;
        }
        else
        {
            Factura = new Factura(Id, numeroFactura.Trim(), MontoTotal, usuarioEmisionId, archivoUrl)
            {
                MovimientoComercial = this
            };
        }

        Estado = EstadoMovimientoComercial.Facturado;
        HistorialAprobaciones.Add(new RegistroAprobacion(
            Id,
            usuarioEmisionId,
            AccionAprobacion.Facturado,
            observaciones ?? $"Factura {numeroFactura.Trim()} emitida"));

        return Factura;
    }

    public void Finalizar(int usuarioFinalizadorId, string? observaciones = null)
    {
        if (Tipo == TipoMovimientoComercial.TraspasoEntreClientes)
        {
            if (Estado != EstadoMovimientoComercial.AprobadoAdmin)
                throw new OperacionInvalidaException($"Los traspasos deben estar en AprobadoAdmin para ser finalizados (estado actual: '{Estado}').");
        }
        else
        {
            if (Estado != EstadoMovimientoComercial.Facturado)
                throw new OperacionInvalidaException($"Las ventas y compras deben estar en estado Facturado para ser finalizadas (estado actual: '{Estado}').");
        }

        if (Factura != null && !Factura.IsDeleted)
        {
            Factura.MarcarPagada();
        }

        Estado = EstadoMovimientoComercial.Finalizado;
        HistorialAprobaciones.Add(new RegistroAprobacion(
            Id,
            usuarioFinalizadorId,
            AccionAprobacion.Finalizado,
            observaciones ?? "Movimiento comercial finalizado e inventario físico impactado"));
    }

    public void Rechazar(int usuarioId, string motivo)
    {
        if (Estado == EstadoMovimientoComercial.Finalizado)
            throw new OperacionInvalidaException("No se puede rechazar un movimiento comercial finalizado.");

        if (string.IsNullOrWhiteSpace(motivo))
            throw new OperacionInvalidaException("Debe indicar el motivo del rechazo u observación.");

        string notaAuditoria = motivo.Trim();
        if (Factura != null)
        {
            Factura.Anular();
            notaAuditoria = $"{motivo.Trim()} (Factura #{Factura.NumeroFactura} anulada)";
        }

        Estado = EstadoMovimientoComercial.Rechazado;
        HistorialAprobaciones.Add(new RegistroAprobacion(
            Id,
            usuarioId,
            AccionAprobacion.Rechazado,
            notaAuditoria));
    }

    public void EditarYReenviar(int usuarioId, string? observaciones = null)
    {
        if (Estado != EstadoMovimientoComercial.Rechazado)
            throw new OperacionInvalidaException($"Solo se pueden modificar y reenviar movimientos en estado Rechazado (estado actual: '{Estado}').");

        RecalcularFinancieros();
        Estado = EstadoMovimientoComercial.PendienteRevision;

        HistorialAprobaciones.Add(new RegistroAprobacion(
            Id,
            usuarioId,
            AccionAprobacion.Corregido,
            observaciones ?? "Movimiento comercial corregido y reenviado a revisión"));
    }

    public Dictionary<ClaseBulbo, int> ObtenerRequerimientosFisicosBulbos()
    {
        var dict = new Dictionary<ClaseBulbo, int>
        {
            [ClaseBulbo.Clase0] = 0,
            [ClaseBulbo.Clase1] = 0,
            [ClaseBulbo.Clase2] = 0,
            [ClaseBulbo.Clase3] = 0,
            [ClaseBulbo.Clase4] = 0
        };

        foreach (var det in Detalles.Where(d => !d.IsDeleted && d.Cantidad > 0))
        {
            if (det.ClaseBulbo == ClaseBulbo.MIX)
            {
                int tercio = det.Cantidad / 3;
                dict[ClaseBulbo.Clase1] += tercio;
                dict[ClaseBulbo.Clase2] += tercio;
                dict[ClaseBulbo.Clase3] += tercio;
            }
            else if (dict.ContainsKey(det.ClaseBulbo))
            {
                dict[det.ClaseBulbo] += det.Cantidad;
            }
        }

        return dict;
    }
}
