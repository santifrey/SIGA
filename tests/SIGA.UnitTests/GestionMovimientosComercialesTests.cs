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

public class GestionMovimientosComercialesTests
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
    private readonly GestionMovimientosComerciales _gestionMovimientos;

    public GestionMovimientosComercialesTests()
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

        _gestionMovimientos = new GestionMovimientosComerciales(
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

    [Fact]
    public async Task CrearMovimiento_Tipo1_VentaACliente_CalculatesTotalFromItems()
    {
        // Arrange
        _movimientoRepoMock.Setup(r => r.HasFinalizedMovementsForClientAsync(2, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var dto = new CrearMovimientoComercialDto
        {
            Tipo = TipoMovimientoComercial.VentaACliente,
            OrigenParticipanteId = Empresa.SingletonId,
            DestinoParticipanteId = 2,
            ClienteId = 2,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 100, PrecioUnitarioPactado = 50m },
                new() { ClaseBulbo = ClaseBulbo.Clase2, Cantidad = 200, PrecioUnitarioPactado = 75m }
            }
        };

        // Act
        var res = await _gestionMovimientos.CrearMovimientoAsync(dto, 1);

        // Assert
        Assert.True(res.Success);
        Assert.Equal(201, res.StatusCode);
        Assert.Equal(20000m, res.Data!.MontoTotal); // 100*50 + 200*75 = 5000 + 15000 = 20000
        Assert.Equal(20000m, res.Data.MontoBruto);
        Assert.Equal(0m, res.Data.MontoComision);
        Assert.Equal(20000m, res.Data.MontoNeto);
        Assert.Equal(EstadoMovimientoComercial.PendienteRevision, res.Data.Estado);
        _movimientoRepoMock.Verify(r => r.AddAsync(It.IsAny<MovimientoComercial>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CrearMovimiento_Tipo2_CompraACliente_CalculatesGrossCommissionAndNet()
    {
        // Arrange
        var dto = new CrearMovimientoComercialDto
        {
            Tipo = TipoMovimientoComercial.CompraACliente,
            OrigenParticipanteId = 2, // Cliente
            DestinoParticipanteId = Empresa.SingletonId, // Empresa
            ClienteId = 2,
            PorcentajeComision = 15m,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase3, Cantidad = 1000, PrecioUnitarioPactado = 100m } // 100,000 bruto
            }
        };

        // Act
        var res = await _gestionMovimientos.CrearMovimientoAsync(dto, 1);

        // Assert
        Assert.True(res.Success);
        Assert.Equal(100000m, res.Data!.MontoBruto);
        Assert.Equal(15m, res.Data.PorcentajeComision);
        Assert.Equal(15000m, res.Data.MontoComision);
        Assert.Equal(85000m, res.Data.MontoNeto);
        Assert.Equal(85000m, res.Data.MontoTotal);
    }

    [Fact]
    public async Task CrearMovimiento_Tipo3_TraspasoEntreClientes_HasZeroFinancials()
    {
        // Arrange
        var dto = new CrearMovimientoComercialDto
        {
            Tipo = TipoMovimientoComercial.TraspasoEntreClientes,
            OrigenParticipanteId = 2,
            DestinoParticipanteId = 3,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 300, PrecioUnitarioPactado = 80m } // Should be ignored to 0
            }
        };

        // Act
        var res = await _gestionMovimientos.CrearMovimientoAsync(dto, 1);

        // Assert
        Assert.True(res.Success);
        Assert.Equal(0m, res.Data!.MontoBruto);
        Assert.Equal(0m, res.Data.MontoComision);
        Assert.Equal(0m, res.Data.MontoNeto);
        Assert.Equal(0m, res.Data.MontoTotal);
        Assert.Equal(0m, res.Data.Detalles.First().PrecioUnitarioPactado);
        Assert.Equal(0m, res.Data.Detalles.First().Subtotal);
    }

    [Fact]
    public async Task CrearMovimiento_SameOriginAndDestination_ThrowsValidationException()
    {
        var dto = new CrearMovimientoComercialDto
        {
            Tipo = TipoMovimientoComercial.TraspasoEntreClientes,
            OrigenParticipanteId = 2,
            DestinoParticipanteId = 2,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 100 }
            }
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _gestionMovimientos.CrearMovimientoAsync(dto, 1));
        Assert.Contains("no pueden ser el mismo", ex.Message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(10)]
    [InlineData(100)]
    public async Task CrearMovimiento_WithMixClass_NonMultipleOf3_ThrowsValidationException(int cantidadInvalida)
    {
        var dto = new CrearMovimientoComercialDto
        {
            Tipo = TipoMovimientoComercial.VentaACliente,
            OrigenParticipanteId = Empresa.SingletonId,
            DestinoParticipanteId = 2,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.MIX, Cantidad = cantidadInvalida, PrecioUnitarioPactado = 50m }
            }
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _gestionMovimientos.CrearMovimientoAsync(dto, 1));
        Assert.Contains("múltiplo de 3", ex.Message);
    }

    [Fact]
    public async Task CrearMovimiento_WithMixClass_MultipleOf3_Succeeds()
    {
        // Arrange
        var dto = new CrearMovimientoComercialDto
        {
            Tipo = TipoMovimientoComercial.VentaACliente,
            OrigenParticipanteId = Empresa.SingletonId,
            DestinoParticipanteId = 2,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.MIX, Cantidad = 300, PrecioUnitarioPactado = 50m }
            }
        };

        // Act
        var res = await _gestionMovimientos.CrearMovimientoAsync(dto, 1);

        // Assert
        Assert.True(res.Success);
        Assert.Equal(15000m, res.Data!.MontoTotal);
        Assert.Single(res.Data.Detalles);
        Assert.Equal(ClaseBulbo.MIX, res.Data.Detalles.First().ClaseBulbo);
        Assert.Equal(300, res.Data.Detalles.First().Cantidad);
        Assert.Equal(15000m, res.Data.Detalles.First().Subtotal);
    }

    [Fact]
    public async Task CircuitoWorkflow_Tipos1Y2_Transitions_0_to_1_to_2_to_3()
    {
        // Arrange: Movimiento Tipo 1
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, Empresa.SingletonId, 2, 2) { Id = 10 };
        mov.AgregarDetalle(ClaseBulbo.Clase1, 100, 50m); // MontoTotal = 5000

        _movimientoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);
        _movimientoRepoMock.Setup(r => r.HasFinalizedMovementsForClientAsync(2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // Existing client, no contract needed

        _gestionStockMock.Setup(s => s.ObtenerSaldoPorClaseAsync(Empresa.SingletonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<ClaseBulbo, int> { [ClaseBulbo.Clase1] = 500 });

        // Step 1: AprobarAdmin (0 -> 1)
        var aproRes = await _gestionMovimientos.AprobarAdminAsync(10, new AprobarAdminDto { Observaciones = "Aprobado por admin" }, 1);
        Assert.True(aproRes.Success);
        Assert.Equal(EstadoMovimientoComercial.AprobadoAdmin, mov.Estado);

        // Step 2: Facturar (1 -> 2)
        var factRes = await _gestionMovimientos.FacturarAsync(10, new FacturarDto { NumeroFactura = "F-0001-00000001" }, 1);
        Assert.True(factRes.Success);
        Assert.Equal(EstadoMovimientoComercial.Facturado, mov.Estado);
        Assert.NotNull(mov.Factura);
        Assert.Equal("F-0001-00000001", mov.Factura.NumeroFactura);

        // Step 3: Finalizar (2 -> 3)
        var finRes = await _gestionMovimientos.FinalizarAsync(10, new FinalizarDto { Observaciones = "Finalizado" }, 1);
        Assert.True(finRes.Success);
        Assert.Equal(EstadoMovimientoComercial.Finalizado, mov.Estado);
        Assert.Equal(EstadoFactura.Pagada, mov.Factura.Estado);
        _movimientoBulbosRepoMock.Verify(m => m.AddAsync(It.IsAny<MovimientoBulbos>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CircuitoWorkflow_Tipo3_DirectTransition_0_to_1_to_3_SkipsFacturado()
    {
        // Arrange: Movimiento Tipo 3 (Traspaso)
        var mov = new MovimientoComercial(TipoMovimientoComercial.TraspasoEntreClientes, 1, 2, 3, 2) { Id = 20 };
        mov.AgregarDetalle(ClaseBulbo.Clase2, 150, 0m);

        _movimientoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);

        _gestionStockMock.Setup(s => s.ObtenerSaldoPorClaseAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<ClaseBulbo, int> { [ClaseBulbo.Clase2] = 200 });

        // Step 1: AprobarAdmin (0 -> 1)
        var aproRes = await _gestionMovimientos.AprobarAdminAsync(20, new AprobarAdminDto(), 1);
        Assert.True(aproRes.Success);
        Assert.Equal(EstadoMovimientoComercial.AprobadoAdmin, mov.Estado);

        // Step 2: Direct transition Finalizar (1 -> 3), skipping Facturado
        var finRes = await _gestionMovimientos.FinalizarAsync(20, new FinalizarDto(), 1);
        Assert.True(finRes.Success);
        Assert.Equal(EstadoMovimientoComercial.Finalizado, mov.Estado);
        Assert.Null(mov.Factura); // No invoice created
    }

    [Fact]
    public async Task Workflow_Tipo3_AttemptingFacturar_ThrowsOperacionInvalidaException()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.TraspasoEntreClientes, 1, 2, 3) { Id = 21 };
        mov.AprobarAdmin(1, false);

        _movimientoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(21, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);

        var ex = await Assert.ThrowsAsync<OperacionInvalidaException>(() =>
            _gestionMovimientos.FacturarAsync(21, new FacturarDto { NumeroFactura = "F-001" }, 1));
        Assert.Contains("no generan facturas", ex.Message);
    }

    [Fact]
    public async Task AprobarAdmin_Tipo1_NewClientWithoutContract_ThrowsOperacionInvalidaException()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, Empresa.SingletonId, 5, 5) { Id = 30 };
        mov.AgregarDetalle(ClaseBulbo.Clase1, 100, 50m);

        _movimientoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);
        // Client is NEW (has no finalized movements)
        _movimientoRepoMock.Setup(r => r.HasFinalizedMovementsForClientAsync(5, 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<OperacionInvalidaException>(() =>
            _gestionMovimientos.AprobarAdminAsync(30, new AprobarAdminDto(), 1));
        Assert.Contains("El cliente es nuevo", ex.Message);
        Assert.Contains("contrato", ex.Message);
    }

    [Fact]
    public async Task AprobarAdmin_Tipo1_NewClientWithContract_SucceedsAndSignsContract()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, Empresa.SingletonId, 5, 5) { Id = 31 };
        mov.AgregarDetalle(ClaseBulbo.Clase1, 100, 50m);
        mov.GenerarContrato("CTR-2026-001", 1);

        _movimientoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(31, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);
        _movimientoRepoMock.Setup(r => r.HasFinalizedMovementsForClientAsync(5, 31, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var res = await _gestionMovimientos.AprobarAdminAsync(31, new AprobarAdminDto(), 1);

        Assert.True(res.Success);
        Assert.Equal(EstadoMovimientoComercial.AprobadoAdmin, mov.Estado);
        Assert.NotNull(mov.Contrato);
        Assert.Equal(EstadoContrato.Firmado, mov.Contrato.Estado);
    }

    [Fact]
    public async Task AprobarAdmin_Tipo1_ExistingClientWithoutContract_Succeeds()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, Empresa.SingletonId, 5, 5) { Id = 32 };
        mov.AgregarDetalle(ClaseBulbo.Clase1, 100, 50m);

        _movimientoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(32, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);
        // Existing client
        _movimientoRepoMock.Setup(r => r.HasFinalizedMovementsForClientAsync(5, 32, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var res = await _gestionMovimientos.AprobarAdminAsync(32, new AprobarAdminDto(), 1);

        Assert.True(res.Success);
        Assert.Equal(EstadoMovimientoComercial.AprobadoAdmin, mov.Estado);
        Assert.Null(mov.Contrato);
    }

    [Theory]
    [InlineData(TipoMovimientoComercial.CompraACliente)]
    [InlineData(TipoMovimientoComercial.TraspasoEntreClientes)]
    public async Task AprobarAdmin_Tipo2And3_NewClient_DoesNotRequireContract(TipoMovimientoComercial tipo)
    {
        var mov = new MovimientoComercial(tipo, 1, 2, 3, 2) { Id = 33 };
        mov.AgregarDetalle(ClaseBulbo.Clase1, 100, 50m);

        _movimientoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(33, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);
        _movimientoRepoMock.Setup(r => r.HasFinalizedMovementsForClientAsync(2, 33, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var res = await _gestionMovimientos.AprobarAdminAsync(33, new AprobarAdminDto(), 1);

        Assert.True(res.Success);
        Assert.Equal(EstadoMovimientoComercial.AprobadoAdmin, mov.Estado);
    }

    [Fact]
    public async Task Rechazar_FromPendienteRevisionOrAprobadoAdmin_SetsRechazadoWithReason()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, Empresa.SingletonId, 2, 2) { Id = 40 };

        _movimientoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(40, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);

        var res = await _gestionMovimientos.RechazarAsync(40, new RechazarDto { Motivo = "Documentación incompleta" }, 1);

        Assert.True(res.Success);
        Assert.Equal(EstadoMovimientoComercial.Rechazado, mov.Estado);
        var lastAction = mov.HistorialAprobaciones.Last();
        Assert.Equal(AccionAprobacion.Rechazado, lastAction.Accion);
        Assert.Equal("Documentación incompleta", lastAction.Observaciones);
    }

    [Fact]
    public async Task Rechazar_FromFacturado_SetsRechazadoAndVoidsInvoice()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, Empresa.SingletonId, 2, 2) { Id = 41 };
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 50m);
        mov.AprobarAdmin(1, false);
        mov.EmitirFactura("FAC-001", 1);

        Assert.Equal(EstadoMovimientoComercial.Facturado, mov.Estado);
        Assert.Equal(EstadoFactura.Emitida, mov.Factura!.Estado);

        _movimientoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(41, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);

        var res = await _gestionMovimientos.RechazarAsync(41, new RechazarDto { Motivo = "Error en precios de factura" }, 1);

        Assert.True(res.Success);
        Assert.Equal(EstadoMovimientoComercial.Rechazado, mov.Estado);
        Assert.Equal(EstadoFactura.Anulada, mov.Factura.Estado); // Invoice voided!
    }

    [Fact]
    public async Task Rechazar_WithEmptyReason_ThrowsValidationException()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, Empresa.SingletonId, 2, 2) { Id = 42 };

        _movimientoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _gestionMovimientos.RechazarAsync(42, new RechazarDto { Motivo = "   " }, 1));
        Assert.Contains("motivo del rechazo", ex.Message);
    }

    [Fact]
    public async Task Rechazar_FromFinalizado_ThrowsOperacionInvalidaException()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.TraspasoEntreClientes, 1, 2, 3) { Id = 43 };
        mov.AprobarAdmin(1, false);
        mov.Finalizar(1, 100);

        _movimientoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(43, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);

        var ex = await Assert.ThrowsAsync<OperacionInvalidaException>(() =>
            _gestionMovimientos.RechazarAsync(43, new RechazarDto { Motivo = "Cancelar" }, 1));
        Assert.Contains("No se puede rechazar", ex.Message);
    }

    [Fact]
    public async Task EditarYReenviar_ByCreator_ResubmitsToPendienteRevision()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, Empresa.SingletonId, 2, 2) { Id = 50 };
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 50m);
        mov.Rechazar(1, "Corregir cantidad");

        _movimientoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);

        var dto = new ActualizarMovimientoComercialDto
        {
            OrigenParticipanteId = Empresa.SingletonId,
            DestinoParticipanteId = 2,
            ClienteId = 2,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 20, PrecioUnitarioPactado = 50m }
            }
        };

        // User 1 is the creator (UsuarioRegistroId == 1)
        var res = await _gestionMovimientos.EditarYReenviarAsync(50, dto, 1, tienePermisoCorregir: false);

        Assert.True(res.Success);
        Assert.Equal(EstadoMovimientoComercial.PendienteRevision, mov.Estado);
        Assert.Equal(1000m, mov.MontoTotal);
        var lastAction = mov.HistorialAprobaciones.Last();
        Assert.Equal(AccionAprobacion.Corregido, lastAction.Accion);
    }

    [Fact]
    public async Task EditarYReenviar_ByNonCreatorWithMovComCorregir_Succeeds()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, Empresa.SingletonId, 2, 2) { Id = 51 };
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 50m);
        mov.Rechazar(1, "Corregir precio");

        _movimientoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(51, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);

        var dto = new ActualizarMovimientoComercialDto
        {
            OrigenParticipanteId = Empresa.SingletonId,
            DestinoParticipanteId = 2,
            ClienteId = 2,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 10, PrecioUnitarioPactado = 60m }
            }
        };

        // User 99 is NOT creator, but has MOVCOM_CORREGIR
        var res = await _gestionMovimientos.EditarYReenviarAsync(51, dto, 99, tienePermisoCorregir: true);

        Assert.True(res.Success);
        Assert.Equal(EstadoMovimientoComercial.PendienteRevision, mov.Estado);
        Assert.Equal(600m, mov.MontoTotal);
    }

    [Fact]
    public async Task EditarYReenviar_ByUnauthorizedUser_ThrowsForbiddenException_Returns403()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, Empresa.SingletonId, 2, 2) { Id = 52 };
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 50m);
        mov.Rechazar(1, "Rechazado");

        _movimientoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(52, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);

        var dto = new ActualizarMovimientoComercialDto
        {
            OrigenParticipanteId = Empresa.SingletonId,
            DestinoParticipanteId = 2,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 10, PrecioUnitarioPactado = 50m }
            }
        };

        // User 99 is NOT creator, and has NO permission
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            _gestionMovimientos.EditarYReenviarAsync(52, dto, 99, tienePermisoCorregir: false));

        Assert.Equal(403, ex.StatusCode);
        Assert.Contains("No tiene permisos para corregir", ex.Message);
    }

    [Fact]
    public async Task Finalizar_WithMixItems_DecomposesIntoEqualCalibres123_AndValidatesStock()
    {
        // Arrange: Movimiento con 300 ítems MIX
        var mov = new MovimientoComercial(TipoMovimientoComercial.TraspasoEntreClientes, 1, 2, 3) { Id = 60 };
        mov.AgregarDetalle(ClaseBulbo.MIX, 300, 0m); // 300 MIX = 100 Cal1, 100 Cal2, 100 Cal3
        mov.AprobarAdmin(1, false);

        _movimientoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(60, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);

        // Origen stock has exactly 100 each for Cal1, Cal2, Cal3
        _gestionStockMock.Setup(s => s.ObtenerSaldoPorClaseAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<ClaseBulbo, int>
            {
                [ClaseBulbo.Clase1] = 100,
                [ClaseBulbo.Clase2] = 100,
                [ClaseBulbo.Clase3] = 100
            });

        MovimientoBulbos? confirmedMovement = null;
        _movimientoBulbosRepoMock.Setup(m => m.AddAsync(It.IsAny<MovimientoBulbos>(), It.IsAny<CancellationToken>()))
            .Callback<MovimientoBulbos, CancellationToken>((mb, _) => confirmedMovement = mb);

        // Act
        var res = await _gestionMovimientos.FinalizarAsync(60, new FinalizarDto(), 1);

        // Assert
        Assert.True(res.Success);
        Assert.Equal(EstadoMovimientoComercial.Finalizado, mov.Estado);
        Assert.NotNull(confirmedMovement);
        Assert.Equal(EstadoMovimiento.Confirmado, confirmedMovement.Estado);

        // Check decomposed quantities
        var c1 = confirmedMovement.Cantidades.FirstOrDefault(c => c.ClaseBulbo == ClaseBulbo.Clase1);
        var c2 = confirmedMovement.Cantidades.FirstOrDefault(c => c.ClaseBulbo == ClaseBulbo.Clase2);
        var c3 = confirmedMovement.Cantidades.FirstOrDefault(c => c.ClaseBulbo == ClaseBulbo.Clase3);
        var mix = confirmedMovement.Cantidades.FirstOrDefault(c => c.ClaseBulbo == ClaseBulbo.MIX);

        Assert.NotNull(c1);
        Assert.Equal(100, c1.Cantidad);
        Assert.NotNull(c2);
        Assert.Equal(100, c2.Cantidad);
        Assert.NotNull(c3);
        Assert.Equal(100, c3.Cantidad);
        Assert.Null(mix); // Physical stock does not store MIX
    }

    [Fact]
    public async Task Finalizar_WhenStockIsInsufficientForDecomposedMix_ThrowsValidationException()
    {
        // Arrange: 300 MIX items (requires 100 Cal1, 100 Cal2, 100 Cal3)
        var mov = new MovimientoComercial(TipoMovimientoComercial.TraspasoEntreClientes, 1, 2, 3) { Id = 61 };
        mov.AgregarDetalle(ClaseBulbo.MIX, 300, 0m);
        mov.AprobarAdmin(1, false);

        _movimientoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(61, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);

        // Origin stock has only 50 Cal2
        _gestionStockMock.Setup(s => s.ObtenerSaldoPorClaseAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<ClaseBulbo, int>
            {
                [ClaseBulbo.Clase1] = 100,
                [ClaseBulbo.Clase2] = 50, // Insufficient!
                [ClaseBulbo.Clase3] = 100
            });

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _gestionMovimientos.FinalizarAsync(61, new FinalizarDto(), 1));

        Assert.Contains("Stock insuficiente", ex.Message);
        Assert.Contains("Clase2", ex.Message);
    }

    [Fact]
    public async Task VerificarClienteNuevo_ReturnsCorrectContractRequirement()
    {
        _movimientoRepoMock.Setup(r => r.HasFinalizedMovementsForClientAsync(10, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // New client
        _movimientoRepoMock.Setup(r => r.HasFinalizedMovementsForClientAsync(20, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // Existing client

        var resNew = await _gestionMovimientos.VerificarClienteNuevoAsync(10);
        Assert.True(resNew.Success);
        Assert.True(resNew.Data!.EsNuevo);
        Assert.True(resNew.Data.RequiereContrato);

        var resExisting = await _gestionMovimientos.VerificarClienteNuevoAsync(20);
        Assert.True(resExisting.Success);
        Assert.False(resExisting.Data!.EsNuevo);
        Assert.False(resExisting.Data.RequiereContrato);
    }
}
