using Moq;
using SIGA.Application.Common;
using SIGA.Application.DTOs.MovimientosComerciales;
using SIGA.Application.Interfaces;
using SIGA.Application.Services;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Exceptions;
using SIGA.Domain.Interfaces;
using Xunit;

namespace SIGA.UnitTests;

public class MovimientosComercialesAdversarialTests
{
    private readonly Mock<IMovimientoComercialRepository> _movimientoRepoMock;
    private readonly Mock<IPrecioClaseRepository> _precioClaseRepoMock;
    private readonly Mock<IClienteRepository> _clienteRepoMock;
    private readonly Mock<ITemporadaRepository> _temporadaRepoMock;
    private readonly Mock<IRepository<ParticipanteComercial>> _participanteRepoMock;
    private readonly Mock<IGestionStock> _gestionStockMock;
    private readonly Mock<IOperacionComercialRepository> _operacionRepoMock;
    private readonly Mock<IMovimientoBulbosRepository> _movimientoBulbosRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly GestionMovimientosComerciales _gestion;

    public MovimientosComercialesAdversarialTests()
    {
        _movimientoRepoMock = new Mock<IMovimientoComercialRepository>();
        _precioClaseRepoMock = new Mock<IPrecioClaseRepository>();
        _clienteRepoMock = new Mock<IClienteRepository>();
        _temporadaRepoMock = new Mock<ITemporadaRepository>();
        _participanteRepoMock = new Mock<IRepository<ParticipanteComercial>>();
        _gestionStockMock = new Mock<IGestionStock>();
        _operacionRepoMock = new Mock<IOperacionComercialRepository>();
        _movimientoBulbosRepoMock = new Mock<IMovimientoBulbosRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _gestion = new GestionMovimientosComerciales(
            _movimientoRepoMock.Object,
            _precioClaseRepoMock.Object,
            _clienteRepoMock.Object,
            _temporadaRepoMock.Object,
            _participanteRepoMock.Object,
            _gestionStockMock.Object,
            _operacionRepoMock.Object,
            _movimientoBulbosRepoMock.Object,
            _unitOfWorkMock.Object);
    }

    #region 1. Unauthorized Resubmission & HTTP 403 Forbidden

    [Fact]
    public async Task Resubmission_UnauthorizedNonCreatorWithoutPermiso_ThrowsForbiddenException_WithStatusCode403()
    {
        // Arrange
        const int creatorId = 100;
        const int unauthorizedUserId = 999;

        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, creatorId, Empresa.SingletonId, 2, 2)
        {
            Id = 501
        };
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 50m);
        mov.Rechazar(200, "Corregir precio");

        _movimientoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(501, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);

        var dto = new ActualizarMovimientoComercialDto
        {
            OrigenParticipanteId = Empresa.SingletonId,
            DestinoParticipanteId = 2,
            ClienteId = 2,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 10, PrecioUnitarioPactado = 55m }
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            _gestion.EditarYReenviarAsync(501, dto, unauthorizedUserId, tienePermisoCorregir: false));

        Assert.Equal(403, ex.StatusCode);
        Assert.Contains("No tiene permisos para corregir", ex.Message);
        Assert.Equal(EstadoMovimientoComercial.Rechazado, mov.Estado); // State unchanged
    }

    [Fact]
    public async Task Resubmission_Creator_Succeeds_AndPreservesUsuarioRegistroIdAndFechaRegistro()
    {
        // Arrange
        const int creatorId = 100;
        var originalFechaRegistro = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, creatorId, Empresa.SingletonId, 2, 2)
        {
            Id = 502,
            FechaRegistro = originalFechaRegistro
        };
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 50m);
        mov.Rechazar(200, "Corregir cantidad");

        _movimientoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(502, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);

        var dto = new ActualizarMovimientoComercialDto
        {
            OrigenParticipanteId = Empresa.SingletonId,
            DestinoParticipanteId = 2,
            ClienteId = 2,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 25, PrecioUnitarioPactado = 50m }
            }
        };

        // Act
        var res = await _gestion.EditarYReenviarAsync(502, dto, creatorId, tienePermisoCorregir: false);

        // Assert
        Assert.True(res.Success);
        Assert.Equal(EstadoMovimientoComercial.PendienteRevision, mov.Estado);
        Assert.Equal(creatorId, mov.UsuarioRegistroId); // Preserved
        Assert.Equal(originalFechaRegistro, mov.FechaRegistro); // Preserved
        Assert.Equal(1250m, mov.MontoTotal);

        var lastLog = mov.HistorialAprobaciones.Last();
        Assert.Equal(AccionAprobacion.Corregido, lastLog.Accion);
        Assert.Equal(creatorId, lastLog.UsuarioAprobadorId);
    }

    [Fact]
    public async Task Resubmission_NonCreatorWithMovComCorregir_Succeeds_AndPreservesUsuarioRegistroIdAndFechaRegistro()
    {
        // Arrange
        const int creatorId = 100;
        const int privilegedEditorId = 300;
        var originalFechaRegistro = new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);

        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, creatorId, Empresa.SingletonId, 2, 2)
        {
            Id = 503,
            FechaRegistro = originalFechaRegistro
        };
        mov.AgregarDetalle(ClaseBulbo.Clase2, 20, 60m);
        mov.Rechazar(200, "Corregir destinatario");

        _movimientoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(503, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);

        var dto = new ActualizarMovimientoComercialDto
        {
            OrigenParticipanteId = Empresa.SingletonId,
            DestinoParticipanteId = 3,
            ClienteId = 3,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase2, Cantidad = 20, PrecioUnitarioPactado = 60m }
            }
        };

        // Act: Non-creator with MOVCOM_CORREGIR
        var res = await _gestion.EditarYReenviarAsync(503, dto, privilegedEditorId, tienePermisoCorregir: true);

        // Assert
        Assert.True(res.Success);
        Assert.Equal(EstadoMovimientoComercial.PendienteRevision, mov.Estado);
        Assert.Equal(creatorId, mov.UsuarioRegistroId); // Creator remains 100
        Assert.Equal(originalFechaRegistro, mov.FechaRegistro); // Registration timestamp intact
        Assert.Equal(3, mov.DestinoParticipanteId);

        var lastLog = mov.HistorialAprobaciones.Last();
        Assert.Equal(AccionAprobacion.Corregido, lastLog.Accion);
        Assert.Equal(privilegedEditorId, lastLog.UsuarioAprobadorId);
    }

    [Theory]
    [InlineData(EstadoMovimientoComercial.PendienteRevision)]
    [InlineData(EstadoMovimientoComercial.AprobadoAdmin)]
    [InlineData(EstadoMovimientoComercial.Facturado)]
    [InlineData(EstadoMovimientoComercial.Finalizado)]
    public async Task Resubmission_FromNonRejectedState_ThrowsOperacionInvalidaException(EstadoMovimientoComercial estadoInvalido)
    {
        // Arrange
        const int creatorId = 100;
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, creatorId, Empresa.SingletonId, 2, 2)
        {
            Id = 504,
            Estado = estadoInvalido
        };
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 50m);

        _movimientoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(504, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);

        var dto = new ActualizarMovimientoComercialDto
        {
            OrigenParticipanteId = Empresa.SingletonId,
            DestinoParticipanteId = 2,
            ClienteId = 2,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 15, PrecioUnitarioPactado = 50m }
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<OperacionInvalidaException>(() =>
            _gestion.EditarYReenviarAsync(504, dto, creatorId, tienePermisoCorregir: false));

        Assert.Contains("Solo se pueden modificar y reenviar movimientos en estado Rechazado", ex.Message);
    }

    #endregion

    #region 2. Rejection & Invoice Voiding

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Rejection_WithNullOrWhitespaceReason_ThrowsValidationException(string? motivoInvalido)
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, Empresa.SingletonId, 2, 2) { Id = 601 };

        _movimientoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(601, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);

        var dto = new RechazarDto { Motivo = motivoInvalido! };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _gestion.RechazarAsync(601, dto, 1));

        Assert.Contains("motivo del rechazo", ex.Message);
        Assert.Equal(EstadoMovimientoComercial.PendienteRevision, mov.Estado);
    }

    [Theory]
    [InlineData(EstadoMovimientoComercial.PendienteRevision)]
    [InlineData(EstadoMovimientoComercial.AprobadoAdmin)]
    public async Task Rejection_FromPendienteRevisionOrAprobadoAdmin_TransitionsToRechazado(EstadoMovimientoComercial estadoInicial)
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, Empresa.SingletonId, 2, 2)
        {
            Id = 602,
            Estado = estadoInicial
        };

        _movimientoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(602, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);

        var res = await _gestion.RechazarAsync(602, new RechazarDto { Motivo = "Observación comercial" }, 42);

        Assert.True(res.Success);
        Assert.Equal(EstadoMovimientoComercial.Rechazado, mov.Estado);
        var lastLog = mov.HistorialAprobaciones.Last();
        Assert.Equal(AccionAprobacion.Rechazado, lastLog.Accion);
        Assert.Equal(42, lastLog.UsuarioAprobadorId);
        Assert.Equal("Observación comercial", lastLog.Observaciones);
    }

    [Fact]
    public async Task Rejection_FromFacturado_TransitionsToRechazado_AndVoidsInvoice()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, Empresa.SingletonId, 2, 2) { Id = 603 };
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 50m);
        mov.AprobarAdmin(1, false);
        var factura = mov.EmitirFactura("FAC-2026-999", 1);

        Assert.Equal(EstadoMovimientoComercial.Facturado, mov.Estado);
        Assert.Equal(EstadoFactura.Emitida, factura.Estado);

        _movimientoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(603, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);

        var res = await _gestion.RechazarAsync(603, new RechazarDto { Motivo = "Facturación errónea" }, 50);

        Assert.True(res.Success);
        Assert.Equal(EstadoMovimientoComercial.Rechazado, mov.Estado);
        Assert.Equal(EstadoFactura.Anulada, factura.Estado); // Invoice voided!
    }

    [Fact]
    public async Task Rejection_FromFinalizado_ThrowsOperacionInvalidaException()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.TraspasoEntreClientes, 1, 2, 3) { Id = 604 };
        mov.AprobarAdmin(1, false);
        mov.Finalizar(1, 999);

        Assert.Equal(EstadoMovimientoComercial.Finalizado, mov.Estado);

        _movimientoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(604, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);

        var ex = await Assert.ThrowsAsync<OperacionInvalidaException>(() =>
            _gestion.RechazarAsync(604, new RechazarDto { Motivo = "Intento rechazar finalizado" }, 1));

        Assert.Contains("No se puede rechazar", ex.Message);
        Assert.Equal(EstadoMovimientoComercial.Finalizado, mov.Estado);
    }

    #endregion

    #region 3. New Client Contract Enforcement

    [Fact]
    public async Task ContractEnforcement_VentaACliente_NewClient_WithoutContract_ThrowsOperacionInvalidaException()
    {
        const int newClientId = 77;
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, Empresa.SingletonId, newClientId, newClientId) { Id = 701 };
        mov.AgregarDetalle(ClaseBulbo.Clase1, 50, 40m);

        _movimientoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(701, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);
        // 0 finalized movements -> New Client
        _movimientoRepoMock.Setup(r => r.HasFinalizedMovementsForClientAsync(newClientId, 701, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<OperacionInvalidaException>(() =>
            _gestion.AprobarAdminAsync(701, new AprobarAdminDto(), 1));

        Assert.Contains("El cliente es nuevo", ex.Message);
        Assert.Contains("contrato", ex.Message);
        Assert.Equal(EstadoMovimientoComercial.PendienteRevision, mov.Estado);
    }

    [Fact]
    public async Task ContractEnforcement_VentaACliente_NewClient_WithContract_SucceedsAndSignsContract()
    {
        const int newClientId = 78;
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, Empresa.SingletonId, newClientId, newClientId) { Id = 702 };
        mov.AgregarDetalle(ClaseBulbo.Clase1, 50, 40m);
        mov.GenerarContrato("CTR-2026-702", 1);

        _movimientoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(702, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);
        _movimientoRepoMock.Setup(r => r.HasFinalizedMovementsForClientAsync(newClientId, 702, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var res = await _gestion.AprobarAdminAsync(702, new AprobarAdminDto(), 1);

        Assert.True(res.Success);
        Assert.Equal(EstadoMovimientoComercial.AprobadoAdmin, mov.Estado);
        Assert.NotNull(mov.Contrato);
        Assert.Equal(EstadoContrato.Firmado, mov.Contrato.Estado);
    }

    [Fact]
    public async Task ContractEnforcement_VentaACliente_ExistingClient_SucceedsWithoutContract()
    {
        const int existingClientId = 79;
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, Empresa.SingletonId, existingClientId, existingClientId) { Id = 703 };
        mov.AgregarDetalle(ClaseBulbo.Clase1, 50, 40m);

        _movimientoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(703, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);
        // >= 1 finalized movements -> Existing Client
        _movimientoRepoMock.Setup(r => r.HasFinalizedMovementsForClientAsync(existingClientId, 703, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var res = await _gestion.AprobarAdminAsync(703, new AprobarAdminDto(), 1);

        Assert.True(res.Success);
        Assert.Equal(EstadoMovimientoComercial.AprobadoAdmin, mov.Estado);
        Assert.Null(mov.Contrato);
    }

    [Theory]
    [InlineData(TipoMovimientoComercial.CompraACliente)]
    [InlineData(TipoMovimientoComercial.TraspasoEntreClientes)]
    public async Task ContractEnforcement_NonVentaTypes_NeverRequireContract_EvenForNewClients(TipoMovimientoComercial nonVentaType)
    {
        const int newClientId = 80;
        var mov = new MovimientoComercial(nonVentaType, 1, newClientId, Empresa.SingletonId, newClientId) { Id = 704 };
        mov.AgregarDetalle(ClaseBulbo.Clase2, 100, 30m);

        _movimientoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(704, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);
        // Client has 0 finalized movements
        _movimientoRepoMock.Setup(r => r.HasFinalizedMovementsForClientAsync(newClientId, 704, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var res = await _gestion.AprobarAdminAsync(704, new AprobarAdminDto(), 1);

        Assert.True(res.Success);
        Assert.Equal(EstadoMovimientoComercial.AprobadoAdmin, mov.Estado);
        Assert.Null(mov.Contrato);
    }

    [Fact]
    public async Task VerificarClienteNuevo_ReturnsAccurateContractRequirement()
    {
        _movimientoRepoMock.Setup(r => r.HasFinalizedMovementsForClientAsync(101, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // New
        _movimientoRepoMock.Setup(r => r.HasFinalizedMovementsForClientAsync(202, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // Existing

        var resNew = await _gestion.VerificarClienteNuevoAsync(101);
        Assert.True(resNew.Success);
        Assert.True(resNew.Data!.EsNuevo);
        Assert.True(resNew.Data.RequiereContrato);

        var resExisting = await _gestion.VerificarClienteNuevoAsync(202);
        Assert.True(resExisting.Success);
        Assert.False(resExisting.Data!.EsNuevo);
        Assert.False(resExisting.Data.RequiereContrato);
    }

    #endregion

    #region 4. Full Traceability & Invariants

    [Fact]
    public async Task WorkflowTraceability_FullLifecycle_PreservesAuditFields_AndLogsEveryAction()
    {
        const int creatorId = 11;
        const int adminId = 22;
        const int invoiceUserId = 33;
        const int finalizerId = 44;
        var creationTime = new DateTime(2026, 3, 1, 8, 30, 0, DateTimeKind.Utc);

        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, creatorId, Empresa.SingletonId, 5, 5)
        {
            Id = 801,
            FechaRegistro = creationTime
        };
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 50m);

        _movimientoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(801, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);
        _movimientoRepoMock.Setup(r => r.HasFinalizedMovementsForClientAsync(5, 801, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _gestionStockMock.Setup(s => s.ObtenerSaldoPorClaseAsync(Empresa.SingletonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<ClaseBulbo, int> { [ClaseBulbo.Clase1] = 100 });

        // 1. Initial State: PendienteRevision
        Assert.Equal(EstadoMovimientoComercial.PendienteRevision, mov.Estado);
        Assert.Single(mov.HistorialAprobaciones);
        Assert.Equal(AccionAprobacion.Registrado, mov.HistorialAprobaciones.First().Accion);
        Assert.Equal(creatorId, mov.HistorialAprobaciones.First().UsuarioAprobadorId);

        // 2. Reject: Rechazar
        await _gestion.RechazarAsync(801, new RechazarDto { Motivo = "Ajustar cantidades" }, adminId);
        Assert.Equal(EstadoMovimientoComercial.Rechazado, mov.Estado);
        Assert.Equal(2, mov.HistorialAprobaciones.Count);
        Assert.Equal(AccionAprobacion.Rechazado, mov.HistorialAprobaciones.Last().Accion);

        // 3. Resubmit: EditarYReenviar
        var dtoUpdate = new ActualizarMovimientoComercialDto
        {
            OrigenParticipanteId = Empresa.SingletonId,
            DestinoParticipanteId = 5,
            ClienteId = 5,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 20, PrecioUnitarioPactado = 50m }
            }
        };
        await _gestion.EditarYReenviarAsync(801, dtoUpdate, creatorId, tienePermisoCorregir: false);
        Assert.Equal(EstadoMovimientoComercial.PendienteRevision, mov.Estado);
        Assert.Equal(3, mov.HistorialAprobaciones.Count);
        Assert.Equal(AccionAprobacion.Corregido, mov.HistorialAprobaciones.Last().Accion);
        Assert.Equal(creatorId, mov.UsuarioRegistroId); // Preserved!
        Assert.Equal(creationTime, mov.FechaRegistro); // Preserved!

        // 4. Approve: AprobarAdmin
        await _gestion.AprobarAdminAsync(801, new AprobarAdminDto { Observaciones = "Aprobado reenvio" }, adminId);
        Assert.Equal(EstadoMovimientoComercial.AprobadoAdmin, mov.Estado);
        Assert.Equal(4, mov.HistorialAprobaciones.Count);
        Assert.Equal(AccionAprobacion.AprobadoAdmin, mov.HistorialAprobaciones.Last().Accion);

        // 5. Invoice: Facturar
        await _gestion.FacturarAsync(801, new FacturarDto { NumeroFactura = "FAC-801-01" }, invoiceUserId);
        Assert.Equal(EstadoMovimientoComercial.Facturado, mov.Estado);
        Assert.Equal(5, mov.HistorialAprobaciones.Count);
        Assert.Equal(AccionAprobacion.Facturado, mov.HistorialAprobaciones.Last().Accion);

        // 6. Finalize: Finalizar
        await _gestion.FinalizarAsync(801, new FinalizarDto { Observaciones = "Cierre total" }, finalizerId);
        Assert.Equal(EstadoMovimientoComercial.Finalizado, mov.Estado);
        Assert.Equal(6, mov.HistorialAprobaciones.Count);
        Assert.Equal(AccionAprobacion.Finalizado, mov.HistorialAprobaciones.Last().Accion);

        // Invariant verification across full lifecycle
        Assert.Equal(creatorId, mov.UsuarioRegistroId);
        Assert.Equal(creationTime, mov.FechaRegistro);
        Assert.Equal(EstadoFactura.Pagada, mov.Factura!.Estado);
    }

    [Fact]
    public async Task Resubmission_WithMixClass_NonMultipleOf3_ThrowsValidationException()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, Empresa.SingletonId, 2, 2) { Id = 802 };
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 50m);
        mov.Rechazar(1, "Rechazo previo");

        _movimientoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(802, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);

        var dto = new ActualizarMovimientoComercialDto
        {
            OrigenParticipanteId = Empresa.SingletonId,
            DestinoParticipanteId = 2,
            ClienteId = 2,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.MIX, Cantidad = 17, PrecioUnitarioPactado = 50m } // 17 not multiple of 3!
            }
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _gestion.EditarYReenviarAsync(802, dto, 1, tienePermisoCorregir: false));

        Assert.Contains("múltiplo de 3", ex.Message);
        Assert.Equal(EstadoMovimientoComercial.Rechazado, mov.Estado);
    }

    #endregion
}
