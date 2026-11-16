using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Moq;
using SIGA.API.Authorization;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Empleados;
using SIGA.Application.DTOs.MovimientosComerciales;
using SIGA.Application.DTOs.PreciosReferencia;
using SIGA.Application.DTOs.Roles;
using SIGA.Application.DTOs.Usuarios;
using SIGA.Application.Interfaces;
using SIGA.Application.Services;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Exceptions;
using SIGA.Domain.Interfaces;
using Xunit;

namespace SIGA.UnitTests;

/// <summary>
/// Phase 2: Adversarial Coverage Hardening (Tier 5) Test Suite.
/// Stress-tests commercial movements, financial invariants, security boundaries, and reference prices.
/// </summary>
public class ChallengerHardeningTests
{
    #region 1. Commercial Movements & Financial Invariants

    [Fact]
    public void CompraACliente_Commission_ZeroPercent_LeavesNetEqualToGross()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.CompraACliente, 1, 2, Empresa.SingletonId, 2, porcentajeComision: 0m);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 100, 100m); // Gross: 10,000

        Assert.Equal(10000m, mov.MontoBruto);
        Assert.Equal(0m, mov.PorcentajeComision);
        Assert.Equal(0m, mov.MontoComision);
        Assert.Equal(10000m, mov.MontoNeto);
        Assert.Equal(10000m, mov.MontoTotal);
    }

    [Fact]
    public void CompraACliente_Commission_HundredPercent_ReducesNetToZero()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.CompraACliente, 1, 2, Empresa.SingletonId, 2, porcentajeComision: 100m);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 100, 100m); // Gross: 10,000

        Assert.Equal(10000m, mov.MontoBruto);
        Assert.Equal(100m, mov.PorcentajeComision);
        Assert.Equal(10000m, mov.MontoComision);
        Assert.Equal(0m, mov.MontoNeto);
        Assert.Equal(0m, mov.MontoTotal);
    }

    [Theory]
    [InlineData(12.375, 1555.50, 192.49, 1363.01)]
    [InlineData(33.333, 1000.00, 333.33, 666.67)]
    [InlineData(0.01, 10000.00, 1.00, 9999.00)]
    [InlineData(99.99, 1000.00, 999.90, 0.10)]
    public void CompraACliente_Commission_DecimalFractions_RoundsToTwoCentDecimals(
        decimal comisionPct, decimal precio, decimal comisionEsperada, decimal netoEsperado)
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.CompraACliente, 1, 2, Empresa.SingletonId, 2, porcentajeComision: comisionPct);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 1, precio);

        Assert.Equal(precio, mov.MontoBruto);
        Assert.Equal(comisionEsperada, mov.MontoComision);
        Assert.Equal(netoEsperado, mov.MontoNeto);
        Assert.Equal(netoEsperado, mov.MontoTotal);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-10.0)]
    [InlineData(100.01)]
    [InlineData(150.0)]
    public void CompraACliente_Commission_OutOfBounds_ThrowsOperacionInvalidaException(decimal invalidComision)
    {
        var ex = Assert.Throws<OperacionInvalidaException>(() =>
            new MovimientoComercial(TipoMovimientoComercial.CompraACliente, 1, 2, Empresa.SingletonId, 2, porcentajeComision: invalidComision));

        Assert.Contains("entre 0 y 100", ex.Message);
    }

    [Fact]
    public void VentaACliente_And_Traspaso_CommissionAlwaysForcedToZero()
    {
        var venta = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, Empresa.SingletonId, 2, 2, porcentajeComision: 25m);
        venta.AgregarDetalle(ClaseBulbo.Clase1, 10, 50m);

        Assert.Equal(0m, venta.PorcentajeComision);
        Assert.Equal(0m, venta.MontoComision);
        Assert.Equal(500m, venta.MontoTotal);

        var traspaso = new MovimientoComercial(TipoMovimientoComercial.TraspasoEntreClientes, 1, 2, 3, 2, porcentajeComision: 50m);
        traspaso.AgregarDetalle(ClaseBulbo.Clase1, 10, 50m);

        Assert.Equal(0m, traspaso.PorcentajeComision);
        Assert.Equal(0m, traspaso.MontoComision);
        Assert.Equal(0m, traspaso.MontoTotal);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(7)]
    [InlineData(301)]
    public void MixItems_Remainder1_ThrowsOperacionInvalidaException(int cantidad)
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, Empresa.SingletonId, 2);
        var ex = Assert.Throws<OperacionInvalidaException>(() =>
            mov.AgregarDetalle(ClaseBulbo.MIX, cantidad, 20m));

        Assert.Contains("múltiplo de 3", ex.Message);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(8)]
    [InlineData(302)]
    public void MixItems_Remainder2_ThrowsOperacionInvalidaException(int cantidad)
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, Empresa.SingletonId, 2);
        var ex = Assert.Throws<OperacionInvalidaException>(() =>
            mov.AgregarDetalle(ClaseBulbo.MIX, cantidad, 20m));

        Assert.Contains("múltiplo de 3", ex.Message);
    }

    [Fact]
    public void MixItems_ValidMultipleOf3_DecomposesEquallyAcrossCalibres123()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, Empresa.SingletonId, 2);
        mov.AgregarDetalle(ClaseBulbo.MIX, 333, 25m);
        mov.AgregarDetalle(ClaseBulbo.Clase0, 50, 10m);

        var reqs = mov.ObtenerRequerimientosFisicosBulbos();

        Assert.Equal(50, reqs[ClaseBulbo.Clase0]);
        Assert.Equal(111, reqs[ClaseBulbo.Clase1]);
        Assert.Equal(111, reqs[ClaseBulbo.Clase2]);
        Assert.Equal(111, reqs[ClaseBulbo.Clase3]);
        Assert.Equal(0, reqs[ClaseBulbo.Clase4]);
        Assert.False(reqs.ContainsKey(ClaseBulbo.MIX)); // MIX is not a physical inventory bucket
    }

    [Fact]
    public async Task Finalizar_WhenSingleDecomposedClassDeficitExists_FailsWithValidationException()
    {
        var movRepoMock = new Mock<IMovimientoComercialRepository>();
        var precioRepoMock = new Mock<IPrecioClaseRepository>();
        var clienteRepoMock = new Mock<IClienteRepository>();
        var tempRepoMock = new Mock<ITemporadaRepository>();
        var partRepoMock = new Mock<IRepository<ParticipanteComercial>>();
        var gestionStockMock = new Mock<IGestionStock>();
        var operacionRepoMock = new Mock<IOperacionComercialRepository>();
        var movBulbosRepoMock = new Mock<IMovimientoBulbosRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();

        var service = new GestionMovimientosComerciales(
            movRepoMock.Object,
            precioRepoMock.Object,
            clienteRepoMock.Object,
            tempRepoMock.Object,
            partRepoMock.Object,
            gestionStockMock.Object,
            operacionRepoMock.Object,
            movBulbosRepoMock.Object,
            unitOfWorkMock.Object);

        // 300 MIX requires 100 Cal1, 100 Cal2, 100 Cal3
        var mov = new MovimientoComercial(TipoMovimientoComercial.TraspasoEntreClientes, 1, 2, 3) { Id = 77 };
        mov.AgregarDetalle(ClaseBulbo.MIX, 300, 0m);
        mov.AprobarAdmin(1, false);

        movRepoMock.Setup(r => r.GetByIdWithDetailsAsync(77, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);

        // Calibre 1 and 3 are sufficient, but Calibre 2 has only 99 bulbs (1 short)
        gestionStockMock.Setup(s => s.ObtenerSaldoPorClaseAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<ClaseBulbo, int>
            {
                [ClaseBulbo.Clase1] = 200,
                [ClaseBulbo.Clase2] = 99, // Deficit!
                [ClaseBulbo.Clase3] = 200
            });

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.FinalizarAsync(77, new FinalizarDto(), 1));

        Assert.Contains("Stock insuficiente", ex.Message);
        Assert.Contains("Clase2", ex.Message);
        Assert.Contains("Disponible: 99", ex.Message);
        Assert.Contains("Requerido: 100", ex.Message);
    }

    [Fact]
    public void StateTransition_BypassingAprobacion_ThrowsOperacionInvalidaException()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, Empresa.SingletonId, 2);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 50m);

        // In PendienteRevision (0), cannot invoice
        var exFact = Assert.Throws<OperacionInvalidaException>(() => mov.EmitirFactura("F01", 1));
        Assert.Contains("AprobadoAdmin", exFact.Message);

        // In PendienteRevision (0), cannot finalize
        var exFin = Assert.Throws<OperacionInvalidaException>(() => mov.Finalizar(1, 10));
        Assert.Contains("Facturado", exFin.Message);
    }

    [Fact]
    public void StateTransition_BypassingFactura_OnVentaOrCompra_ThrowsOperacionInvalidaException()
    {
        var venta = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, Empresa.SingletonId, 2);
        venta.AgregarDetalle(ClaseBulbo.Clase1, 10, 50m);
        venta.AprobarAdmin(1, false);

        // Approved but not invoiced
        var exVenta = Assert.Throws<OperacionInvalidaException>(() => venta.Finalizar(1, 10));
        Assert.Contains("Facturado", exVenta.Message);

        var compra = new MovimientoComercial(TipoMovimientoComercial.CompraACliente, 1, 2, Empresa.SingletonId, 2);
        compra.AgregarDetalle(ClaseBulbo.Clase1, 10, 50m);
        compra.AprobarAdmin(1, false);

        var exCompra = Assert.Throws<OperacionInvalidaException>(() => compra.Finalizar(1, 10));
        Assert.Contains("Facturado", exCompra.Message);
    }

    [Fact]
    public void StateTransition_InvoicingTraspaso_Prohibited()
    {
        var traspaso = new MovimientoComercial(TipoMovimientoComercial.TraspasoEntreClientes, 1, 2, 3);
        traspaso.AgregarDetalle(ClaseBulbo.Clase1, 10, 0m);
        traspaso.AprobarAdmin(1, false);

        var ex = Assert.Throws<OperacionInvalidaException>(() => traspaso.EmitirFactura("F-TRASP", 1));
        Assert.Contains("no generan facturas comerciales", ex.Message);
    }

    [Fact]
    public void StateTransition_RejectingFacturado_VoidsInvoice()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, Empresa.SingletonId, 2);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 50m);
        mov.AprobarAdmin(1, false);
        var factura = mov.EmitirFactura("FAC-TEST-001", 1);

        Assert.Equal(EstadoMovimientoComercial.Facturado, mov.Estado);
        Assert.Equal(EstadoFactura.Emitida, factura.Estado);

        mov.Rechazar(1, "Precios incorrectos pactados");

        Assert.Equal(EstadoMovimientoComercial.Rechazado, mov.Estado);
        Assert.Equal(EstadoFactura.Anulada, factura.Estado);
    }

    [Fact]
    public void StateTransition_EditingMovementNotInRechazado_ThrowsOperacionInvalidaException()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, Empresa.SingletonId, 2);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 50m);

        // In PendienteRevision
        Assert.Throws<OperacionInvalidaException>(() => mov.EditarYReenviar(1));

        mov.AprobarAdmin(1, false);
        // In AprobadoAdmin
        Assert.Throws<OperacionInvalidaException>(() => mov.EditarYReenviar(1));
    }

    [Fact]
    public void StateTransition_RejectingFinalizedMovement_ThrowsOperacionInvalidaException()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.TraspasoEntreClientes, 1, 2, 3);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 0m);
        mov.AprobarAdmin(1, false);
        mov.Finalizar(1, 555);

        var ex = Assert.Throws<OperacionInvalidaException>(() => mov.Rechazar(1, "Tarde"));
        Assert.Contains("No se puede rechazar un movimiento comercial finalizado", ex.Message);
    }

    [Fact]
    public async Task ContractRequirement_ClientWithPreviousRejectedSalesOnly_StillRequiresContract()
    {
        var movRepoMock = new Mock<IMovimientoComercialRepository>();
        var service = new GestionMovimientosComerciales(
            movRepoMock.Object,
            Mock.Of<IPrecioClaseRepository>(),
            Mock.Of<IClienteRepository>(),
            Mock.Of<ITemporadaRepository>(),
            Mock.Of<IRepository<ParticipanteComercial>>(),
            Mock.Of<IGestionStock>(),
            Mock.Of<IOperacionComercialRepository>(),
            Mock.Of<IMovimientoBulbosRepository>(),
            Mock.Of<IUnitOfWork>());

        // Client 45 has previous rejected or pending movements, but ZERO finalized
        movRepoMock.Setup(r => r.HasFinalizedMovementsForClientAsync(45, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var check = await service.VerificarClienteNuevoAsync(45);
        Assert.True(check.Success);
        Assert.True(check.Data!.EsNuevo);
        Assert.True(check.Data.RequiereContrato);

        // Attempting to approve Venta for this client without contract must throw
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, Empresa.SingletonId, 45, 45) { Id = 100 };
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 50m);

        movRepoMock.Setup(r => r.GetByIdWithDetailsAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);

        var ex = await Assert.ThrowsAsync<OperacionInvalidaException>(() =>
            service.AprobarAdminAsync(100, new AprobarAdminDto(), 1));

        Assert.Contains("El cliente es nuevo", ex.Message);
        Assert.Contains("contrato", ex.Message);
    }

    [Fact]
    public async Task ContractRequirement_ClientWithPreviousFinalizedMovement_DoesNotRequireContract()
    {
        var movRepoMock = new Mock<IMovimientoComercialRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();

        var service = new GestionMovimientosComerciales(
            movRepoMock.Object,
            Mock.Of<IPrecioClaseRepository>(),
            Mock.Of<IClienteRepository>(),
            Mock.Of<ITemporadaRepository>(),
            Mock.Of<IRepository<ParticipanteComercial>>(),
            Mock.Of<IGestionStock>(),
            Mock.Of<IOperacionComercialRepository>(),
            Mock.Of<IMovimientoBulbosRepository>(),
            unitOfWorkMock.Object);

        // Client 45 has at least 1 finalized movement
        movRepoMock.Setup(r => r.HasFinalizedMovementsForClientAsync(45, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var check = await service.VerificarClienteNuevoAsync(45);
        Assert.True(check.Success);
        Assert.False(check.Data!.EsNuevo);
        Assert.False(check.Data.RequiereContrato);

        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, Empresa.SingletonId, 45, 45) { Id = 101 };
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 50m);

        movRepoMock.Setup(r => r.GetByIdWithDetailsAsync(101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);

        // Approval succeeds without any contract
        var res = await service.AprobarAdminAsync(101, new AprobarAdminDto(), 1);
        Assert.True(res.Success);
        Assert.Equal(EstadoMovimientoComercial.AprobadoAdmin, mov.Estado);
        Assert.Null(mov.Contrato);
    }

    [Fact]
    public async Task Resubmission_CreatorCanResubmit_UnauthorizedUserReceivesForbidden403()
    {
        var movRepoMock = new Mock<IMovimientoComercialRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();

        var service = new GestionMovimientosComerciales(
            movRepoMock.Object,
            Mock.Of<IPrecioClaseRepository>(),
            Mock.Of<IClienteRepository>(),
            Mock.Of<ITemporadaRepository>(),
            Mock.Of<IRepository<ParticipanteComercial>>(),
            Mock.Of<IGestionStock>(),
            Mock.Of<IOperacionComercialRepository>(),
            Mock.Of<IMovimientoBulbosRepository>(),
            unitOfWorkMock.Object);

        // Movement registered by user 10, currently Rejected
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 10, Empresa.SingletonId, 2) { Id = 200 };
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 50m);
        mov.Rechazar(1, "Precios observados");

        movRepoMock.Setup(r => r.GetByIdWithDetailsAsync(200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);

        var updateDto = new ActualizarMovimientoComercialDto
        {
            OrigenParticipanteId = Empresa.SingletonId,
            DestinoParticipanteId = 2,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 10, PrecioUnitarioPactado = 55m }
            }
        };

        // 1. Non-creator (user 99) without permission is denied with 403 Forbidden
        var forbiddenEx = await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.EditarYReenviarAsync(200, updateDto, usuarioId: 99, tienePermisoCorregir: false));
        Assert.Equal(403, forbiddenEx.StatusCode);

        // 2. Original creator (user 10) without MOVCOM_CORREGIR is allowed
        var creatorRes = await service.EditarYReenviarAsync(200, updateDto, usuarioId: 10, tienePermisoCorregir: false);
        Assert.True(creatorRes.Success);
        Assert.Equal(EstadoMovimientoComercial.PendienteRevision, mov.Estado);

        // Reject again for second test branch
        mov.Rechazar(1, "Nueva observacion");

        // 3. Non-creator (user 99) WITH MOVCOM_CORREGIR is allowed
        var authorizedRes = await service.EditarYReenviarAsync(200, updateDto, usuarioId: 99, tienePermisoCorregir: true);
        Assert.True(authorizedRes.Success);
        Assert.Equal(EstadoMovimientoComercial.PendienteRevision, mov.Estado);
    }

    [Fact]
    public async Task Rejection_EmptyOrWhitespaceReason_ThrowsValidationException()
    {
        var movRepoMock = new Mock<IMovimientoComercialRepository>();
        var service = new GestionMovimientosComerciales(
            movRepoMock.Object,
            Mock.Of<IPrecioClaseRepository>(),
            Mock.Of<IClienteRepository>(),
            Mock.Of<ITemporadaRepository>(),
            Mock.Of<IRepository<ParticipanteComercial>>(),
            Mock.Of<IGestionStock>(),
            Mock.Of<IOperacionComercialRepository>(),
            Mock.Of<IMovimientoBulbosRepository>(),
            Mock.Of<IUnitOfWork>());

        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, Empresa.SingletonId, 2) { Id = 300 };
        movRepoMock.Setup(r => r.GetByIdWithDetailsAsync(300, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);

        var exEmpty = await Assert.ThrowsAsync<ValidationException>(() =>
            service.RechazarAsync(300, new RechazarDto { Motivo = "" }, 1));
        Assert.Contains("motivo", exEmpty.Message);

        var exWhitespace = await Assert.ThrowsAsync<ValidationException>(() =>
            service.RechazarAsync(300, new RechazarDto { Motivo = "    " }, 1));
        Assert.Contains("motivo", exWhitespace.Message);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-15.0)]
    [InlineData(100.01)]
    [InlineData(125.0)]
    public async Task EditarYReenviar_CommissionOutOfBounds_ThrowsReglaNegocioException(decimal invalidComision)
    {
        var movRepoMock = new Mock<IMovimientoComercialRepository>();
        var service = new GestionMovimientosComerciales(
            movRepoMock.Object,
            Mock.Of<IPrecioClaseRepository>(),
            Mock.Of<IClienteRepository>(),
            Mock.Of<ITemporadaRepository>(),
            Mock.Of<IRepository<ParticipanteComercial>>(),
            Mock.Of<IGestionStock>(),
            Mock.Of<IOperacionComercialRepository>(),
            Mock.Of<IMovimientoBulbosRepository>(),
            Mock.Of<IUnitOfWork>());

        var mov = new MovimientoComercial(TipoMovimientoComercial.CompraACliente, 1, 2, Empresa.SingletonId, 2, porcentajeComision: 10m) { Id = 350 };
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 50m);
        mov.Rechazar(1, "Precios incorrectos");

        movRepoMock.Setup(r => r.GetByIdWithDetailsAsync(350, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);

        var updateDto = new ActualizarMovimientoComercialDto
        {
            OrigenParticipanteId = 2,
            DestinoParticipanteId = Empresa.SingletonId,
            ClienteId = 2,
            PorcentajeComision = invalidComision,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 10, PrecioUnitarioPactado = 50m }
            }
        };

        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            service.EditarYReenviarAsync(350, updateDto, usuarioId: 1, tienePermisoCorregir: true));

        Assert.Contains("entre 0 y 100", ex.Message);
    }

    [Fact]
    public async Task FacturarAsync_ReInvoicingVoidedInvoice_UpdatesExistingInvoiceCleanly()
    {
        var movRepoMock = new Mock<IMovimientoComercialRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();

        var service = new GestionMovimientosComerciales(
            movRepoMock.Object,
            Mock.Of<IPrecioClaseRepository>(),
            Mock.Of<IClienteRepository>(),
            Mock.Of<ITemporadaRepository>(),
            Mock.Of<IRepository<ParticipanteComercial>>(),
            Mock.Of<IGestionStock>(),
            Mock.Of<IOperacionComercialRepository>(),
            Mock.Of<IMovimientoBulbosRepository>(),
            unitOfWorkMock.Object);

        // 1. Initial creation, approval and invoicing
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, Empresa.SingletonId, 2) { Id = 360 };
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 50m); // MontoTotal = 500
        mov.AprobarAdmin(1, false);
        var originalFactura = mov.EmitirFactura("FAC-001", 1);
        Assert.Equal(EstadoFactura.Emitida, originalFactura.Estado);

        // 2. Movement rejected -> invoice is voided
        mov.Rechazar(1, "Error en detalle");
        Assert.Equal(EstadoMovimientoComercial.Rechazado, mov.Estado);
        Assert.Equal(EstadoFactura.Anulada, mov.Factura!.Estado);

        // 3. Movement re-approved
        mov.EditarYReenviar(1);
        mov.AprobarAdmin(1, false);

        movRepoMock.Setup(r => r.GetByIdWithDetailsAsync(360, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);

        // 4. Re-invoicing via service
        var res = await service.FacturarAsync(360, new FacturarDto { NumeroFactura = "FAC-002-FIXED" }, usuarioId: 2);

        Assert.True(res.Success);
        Assert.Equal(EstadoMovimientoComercial.Facturado, mov.Estado);
        Assert.Equal("FAC-002-FIXED", mov.Factura.NumeroFactura);
        Assert.Equal(EstadoFactura.Emitida, mov.Factura.Estado);
        // Verify invoice instance is reused, preventing EF Core duplicate key collision
        Assert.Same(originalFactura, mov.Factura);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region 2. Security & Authorization Boundaries

    [Theory]
    [InlineData("ADMINISTRADOR")]
    [InlineData("VENDEDOR")]
    [InlineData("CLIENTE")]
    [InlineData("SECRETARIA")]
    [InlineData("CONTABILIDAD")]
    public async Task DeleteRol_SystemRole_ThrowsValidationException(string systemRoleName)
    {
        var rolRepoMock = new Mock<IRolRepository>();
        var usuarioRepoMock = new Mock<IUsuarioRepository>();
        var service = new GestionRoles(rolRepoMock.Object, Mock.Of<IRepository<Permiso>>(), usuarioRepoMock.Object, Mock.Of<IUnitOfWork>());

        var rol = new Rol { Id = 1, Nombre = systemRoleName, EsSistema = true };
        rolRepoMock.Setup(r => r.GetByIdWithPermisosAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rol);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.DeleteRolAsync(1));
        Assert.Contains("No se puede eliminar un rol protegido del sistema", ex.Message);
    }

    [Fact]
    public async Task UpdateRol_RenamingSystemRole_ThrowsValidationException()
    {
        var rolRepoMock = new Mock<IRolRepository>();
        var service = new GestionRoles(rolRepoMock.Object, Mock.Of<IRepository<Permiso>>(), Mock.Of<IUsuarioRepository>(), Mock.Of<IUnitOfWork>());

        var rol = new Rol { Id = 1, Nombre = "ADMINISTRADOR", EsSistema = true };
        rolRepoMock.Setup(r => r.GetByIdWithPermisosAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rol);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateRolAsync(1, new ActualizarRolDto { Nombre = "SUPER_ADMIN" }));

        Assert.Contains("No se permite modificar el nombre de un rol de sistema", ex.Message);
    }

    [Theory]
    [InlineData("administrador")]
    [InlineData("Vendedor")]
    [InlineData("cliente")]
    public async Task CreateRol_WithReservedSystemName_ThrowsValidationException(string reservedName)
    {
        var rolRepoMock = new Mock<IRolRepository>();
        var service = new GestionRoles(rolRepoMock.Object, Mock.Of<IRepository<Permiso>>(), Mock.Of<IUsuarioRepository>(), Mock.Of<IUnitOfWork>());

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateRolAsync(new CrearRolDto { Nombre = reservedName }));

        Assert.Contains("reservado", ex.Message);
    }

    [Fact]
    public async Task DeleteRol_CustomRoleWithActiveUsers_ThrowsValidationException()
    {
        var rolRepoMock = new Mock<IRolRepository>();
        var usuarioRepoMock = new Mock<IUsuarioRepository>();
        var service = new GestionRoles(rolRepoMock.Object, Mock.Of<IRepository<Permiso>>(), usuarioRepoMock.Object, Mock.Of<IUnitOfWork>());

        var customRol = new Rol { Id = 20, Nombre = "OPERARIO", EsSistema = false };
        rolRepoMock.Setup(r => r.GetByIdWithPermisosAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customRol);

        usuarioRepoMock.Setup(u => u.ExistsAsync(It.IsAny<Expression<Func<Usuario, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.DeleteRolAsync(20));
        Assert.Contains("tiene usuarios asignados", ex.Message);
    }

    [Fact]
    public async Task DeleteRol_CustomRoleWithOnlySoftDeletedUsers_Succeeds()
    {
        var rolRepoMock = new Mock<IRolRepository>();
        var usuarioRepoMock = new Mock<IUsuarioRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var service = new GestionRoles(rolRepoMock.Object, Mock.Of<IRepository<Permiso>>(), usuarioRepoMock.Object, unitOfWorkMock.Object);

        var customRol = new Rol { Id = 21, Nombre = "PASANTE", EsSistema = false };
        rolRepoMock.Setup(r => r.GetByIdWithPermisosAsync(21, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customRol);

        // No ACTIVE users (only soft deleted users would evaluate false for !u.IsDeleted)
        usuarioRepoMock.Setup(u => u.ExistsAsync(It.IsAny<Expression<Func<Usuario, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var res = await service.DeleteRolAsync(21);
        Assert.True(res.Success);
        rolRepoMock.Verify(r => r.DeleteAsync(customRol, It.IsAny<CancellationToken>()), Times.Once);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteEmpleado_SoleActiveAdmin_ThrowsValidationException()
    {
        var usuarioRepoMock = new Mock<IUsuarioRepository>();
        var service = new GestionEmpleados(
            usuarioRepoMock.Object,
            Mock.Of<IPersonaRepository>(),
            Mock.Of<IRolRepository>(),
            Mock.Of<IPasswordHasher>(),
            Mock.Of<IUnitOfWork>());

        var admin = new Usuario
        {
            Id = 1,
            Username = "admin",
            RolId = 1,
            Rol = new Rol { Id = 1, Nombre = "ADMINISTRADOR" },
            Estado = EstadoUsuario.Activo
        };

        usuarioRepoMock.Setup(u => u.GetByIdWithRolesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);

        usuarioRepoMock.Setup(u => u.CountAsync(It.IsAny<Expression<Func<Usuario, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1); // Exactly 1 active admin

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.DeleteEmpleadoAsync(1));
        Assert.Equal("No se puede desactivar el único administrador activo.", ex.Message);
    }

    [Fact]
    public async Task UpdateEmpleado_DemotingSoleActiveAdmin_ThrowsValidationException()
    {
        var usuarioRepoMock = new Mock<IUsuarioRepository>();
        var rolRepoMock = new Mock<IRolRepository>();
        var service = new GestionEmpleados(
            usuarioRepoMock.Object,
            Mock.Of<IPersonaRepository>(),
            rolRepoMock.Object,
            Mock.Of<IPasswordHasher>(),
            Mock.Of<IUnitOfWork>());

        var admin = new Usuario
        {
            Id = 1,
            Username = "admin",
            RolId = 1,
            Rol = new Rol { Id = 1, Nombre = "ADMINISTRADOR" },
            Estado = EstadoUsuario.Activo,
            Persona = new Persona { Id = 1, Documento = "000" }
        };

        usuarioRepoMock.Setup(u => u.GetByIdWithRolesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);

        var vendedorRol = new Rol { Id = 2, Nombre = "VENDEDOR" };
        rolRepoMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vendedorRol);

        usuarioRepoMock.Setup(u => u.CountAsync(It.IsAny<Expression<Func<Usuario, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateEmpleadoAsync(1, new ActualizarEmpleadoDto
            {
                Nombre = "Admin",
                Apellido = "SIGA",
                Documento = "000",
                RolId = 2, // Demote to VENDEDOR
                Estado = EstadoUsuario.Activo
            }));

        Assert.Equal("No se puede desactivar el único administrador activo.", ex.Message);
    }

    [Fact]
    public async Task PermissionAuthorizationHandler_UserWithoutPermissions_FailsRequirement()
    {
        var handler = new PermissionAuthorizationHandler();

        // User authenticated with custom role "AUDITOR" and ZERO permissions
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "88"),
            new Claim(ClaimTypes.Name, "auditor"),
            new Claim(ClaimTypes.Role, "AUDITOR")
        }, "TestAuth");

        var principal = new ClaimsPrincipal(identity);
        var requirement = new PermissionRequirement("MOVCOM_CREAR");
        var context = new AuthorizationHandlerContext(new[] { requirement }, principal, null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task GestionUsuarios_DeleteAsync_SoleActiveAdmin_ThrowsReglaNegocioException()
    {
        var usuarioRepoMock = new Mock<IUsuarioRepository>();
        var service = new GestionUsuarios(
            usuarioRepoMock.Object,
            Mock.Of<IPersonaRepository>(),
            Mock.Of<IRolRepository>(),
            Mock.Of<IPasswordHasher>(),
            Mock.Of<ITokenService>(),
            Mock.Of<IUnitOfWork>());

        var admin = new Usuario
        {
            Id = 1,
            Username = "admin",
            RolId = 1,
            Rol = new Rol { Id = 1, Nombre = "ADMINISTRADOR" },
            Estado = EstadoUsuario.Activo
        };

        usuarioRepoMock.Setup(u => u.GetByIdWithRolesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);

        usuarioRepoMock.Setup(u => u.CountAsync(It.IsAny<Expression<Func<Usuario, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1); // Sole active admin

        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => service.DeleteAsync(1));
        Assert.Contains("No se puede eliminar el único administrador activo", ex.Message);
    }

    [Fact]
    public async Task GestionUsuarios_CambiarEstadoAsync_SoleActiveAdmin_ThrowsReglaNegocioException()
    {
        var usuarioRepoMock = new Mock<IUsuarioRepository>();
        var service = new GestionUsuarios(
            usuarioRepoMock.Object,
            Mock.Of<IPersonaRepository>(),
            Mock.Of<IRolRepository>(),
            Mock.Of<IPasswordHasher>(),
            Mock.Of<ITokenService>(),
            Mock.Of<IUnitOfWork>());

        var admin = new Usuario
        {
            Id = 1,
            Username = "admin",
            RolId = 1,
            Rol = new Rol { Id = 1, Nombre = "ADMINISTRADOR" },
            Estado = EstadoUsuario.Activo
        };

        usuarioRepoMock.Setup(u => u.GetByIdWithRolesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);

        usuarioRepoMock.Setup(u => u.CountAsync(It.IsAny<Expression<Func<Usuario, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1); // Sole active admin

        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            service.CambiarEstadoAsync(1, EstadoUsuario.Inactivo));
        Assert.Contains("No se puede desactivar el único administrador activo", ex.Message);
    }

    [Fact]
    public async Task PermissionAuthorizationHandler_UnauthenticatedUser_FailsRequirement()
    {
        var handler = new PermissionAuthorizationHandler();
        var principal = new ClaimsPrincipal(new ClaimsIdentity()); // IsAuthenticated == false

        var requirement = new PermissionRequirement("MOVCOM_CREAR");
        var context = new AuthorizationHandlerContext(new[] { requirement }, principal, null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    #endregion

    #region 3. Reference Prices

    [Fact]
    public async Task ConfigurarPrecioReferencia_PositiveBoundary_SucceedsForMixAndNumberedClasses()
    {
        var precioRepoMock = new Mock<IPrecioClaseRepository>();
        var tempRepoMock = new Mock<ITemporadaRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();

        var service = new GestionPreciosReferencia(precioRepoMock.Object, tempRepoMock.Object, unitOfWorkMock.Object);

        var temporada = new Temporada("Temporada 2026", 1) { Id = 10 };
        tempRepoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(temporada);

        // Minimum positive price boundary (1 cent)
        var dto = new ConfigurarPrecioReferenciaDto
        {
            TemporadaId = 10,
            ClaseBulbo = ClaseBulbo.MIX,
            PrecioReferencia = 0.01m
        };

        var res = await service.ConfigurarPrecioReferenciaAsync(dto);

        Assert.True(res.Success);
        Assert.Equal(0.01m, res.Data!.PrecioReferencia);
        Assert.Equal(ClaseBulbo.MIX, res.Data.ClaseBulbo);
        precioRepoMock.Verify(r => r.AddAsync(It.IsAny<PrecioClase>(), It.IsAny<CancellationToken>()), Times.Once);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100.0)]
    public async Task ConfigurarPrecioReferencia_ZeroOrNegative_ThrowsOperacionInvalidaException(decimal invalidPrice)
    {
        var service = new GestionPreciosReferencia(
            Mock.Of<IPrecioClaseRepository>(),
            Mock.Of<ITemporadaRepository>(),
            Mock.Of<IUnitOfWork>());

        var ex = await Assert.ThrowsAsync<OperacionInvalidaException>(() =>
            service.ConfigurarPrecioReferenciaAsync(new ConfigurarPrecioReferenciaDto
            {
                TemporadaId = 1,
                ClaseBulbo = ClaseBulbo.Clase1,
                PrecioReferencia = invalidPrice
            }));

        Assert.Contains("mayor a cero", ex.Message);
    }

    [Fact]
    public async Task ListarPreciosPorTemporada_AlwaysReturnsExactly6CatalogClasses()
    {
        var precioRepoMock = new Mock<IPrecioClaseRepository>();
        var tempRepoMock = new Mock<ITemporadaRepository>();
        var service = new GestionPreciosReferencia(precioRepoMock.Object, tempRepoMock.Object, Mock.Of<IUnitOfWork>());

        var temporada = new Temporada("Temporada 2026", 1) { Id = 5 };
        tempRepoMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(temporada);

        // Only Clase2 is configured with 75.00
        var existing = new List<PrecioClase>
        {
            new PrecioClase(5, ClaseBulbo.Clase2, 75.00m) { Id = 1 }
        };
        precioRepoMock.Setup(r => r.GetByTemporadaAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var res = await service.ListarPreciosPorTemporadaAsync(5);

        Assert.True(res.Success);
        Assert.NotNull(res.Data);
        var list = res.Data.ToList();
        Assert.Equal(6, list.Count);

        var c2 = list.First(p => p.ClaseBulbo == ClaseBulbo.Clase2);
        Assert.Equal(75.00m, c2.PrecioReferencia);

        var mix = list.First(p => p.ClaseBulbo == ClaseBulbo.MIX);
        Assert.Equal(0m, mix.PrecioReferencia); // Default unconfigured is 0
    }

    [Fact]
    public async Task ObtenerPrecioVigente_UnconfiguredClass_ReturnsZeroPriceWithoutThrowing()
    {
        var precioRepoMock = new Mock<IPrecioClaseRepository>();
        var tempRepoMock = new Mock<ITemporadaRepository>();
        var service = new GestionPreciosReferencia(precioRepoMock.Object, tempRepoMock.Object, Mock.Of<IUnitOfWork>());

        var temporada = new Temporada("Temporada 2026", 1) { Id = 5 };
        tempRepoMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(temporada);

        precioRepoMock.Setup(r => r.GetByTemporadaAndClaseAsync(5, ClaseBulbo.Clase4, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PrecioClase?)null);

        var res = await service.ObtenerPrecioVigenteAsync(5, ClaseBulbo.Clase4);

        Assert.True(res.Success);
        Assert.Equal(0m, res.Data!.PrecioReferencia);
        Assert.Equal(ClaseBulbo.Clase4, res.Data.ClaseBulbo);
    }

    [Fact]
    public async Task ConfigurarPrecioReferencia_InvalidClaseBulboEnum_ThrowsValidationException()
    {
        var service = new GestionPreciosReferencia(
            Mock.Of<IPrecioClaseRepository>(),
            Mock.Of<ITemporadaRepository>(),
            Mock.Of<IUnitOfWork>());

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.ConfigurarPrecioReferenciaAsync(new ConfigurarPrecioReferenciaDto
            {
                TemporadaId = 1,
                ClaseBulbo = (ClaseBulbo)999,
                PrecioReferencia = 15m
            }));

        Assert.Contains("no es válida", ex.Message);
    }

    [Fact]
    public async Task ObtenerPrecioVigente_InvalidClaseBulboEnum_ReturnsFailure()
    {
        var service = new GestionPreciosReferencia(
            Mock.Of<IPrecioClaseRepository>(),
            Mock.Of<ITemporadaRepository>(),
            Mock.Of<IUnitOfWork>());

        var res = await service.ObtenerPrecioVigenteAsync(1, (ClaseBulbo)999);
        Assert.False(res.Success);
        Assert.Contains("no es válida", res.Message);
    }

    #endregion
}
