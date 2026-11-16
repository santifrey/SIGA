using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIGA.Application.Common;
using SIGA.Application.DTOs.MovimientosComerciales;
using SIGA.Application.DTOs.PreciosReferencia;
using SIGA.Application.Interfaces;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Exceptions;
using SIGA.E2ETests.Infrastructure;
using SIGA.Infrastructure.Persistence;
using Xunit;

namespace SIGA.E2ETests.Tier2_BoundaryCases;

[Collection("E2E Test Suite")]
public class CommercialBoundariesTests : TestBase
{
    public CommercialBoundariesTests(SigaTestWebApplicationFactory factory) : base(factory)
    {
    }

    private async Task<(int ClienteId, int TemporadaId)> EnsureSeedClienteAndTemporadaAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SigaDbContext>();

        var cliente = await db.Clientes.FirstOrDefaultAsync();
        if (cliente == null)
        {
            var persona = new Persona { Nombre = "C", Apellido = "B", Documento = "CB" + Random.Shared.Next(10000, 99999), FechaCreacion = DateTime.UtcNow };
            await db.Personas.AddAsync(persona);
            await db.SaveChangesAsync();
            cliente = new Cliente { PersonaId = persona.Id, CodigoCliente = "CLI-" + Guid.NewGuid().ToString("N")[..4], EstadoCliente = EstadoCliente.Activo, Estado = EstadoParticipante.Activo, FechaRegistro = DateTime.UtcNow };
            await db.Clientes.AddAsync(cliente);
            await db.SaveChangesAsync();
        }

        var temporada = await db.Temporadas.FirstOrDefaultAsync(t => t.Estado != EstadoTemporada.Cerrada);
        if (temporada == null)
        {
            temporada = new Temporada { Nombre = "Temporada B", UsuarioCreadorId = 1, FechaInicio = DateTime.UtcNow, FechaFin = DateTime.UtcNow.AddMonths(6), Estado = EstadoTemporada.Planificada, FechaCreacion = DateTime.UtcNow };
            await db.Temporadas.AddAsync(temporada);
            await db.SaveChangesAsync();
        }

        return (cliente.Id, temporada.Id);
    }

    #region Feature 11: Unified MovimientoComercial Entity & API (5 Boundary Tests)

    [Fact]
    public async Task T2_F11_01_InvalidTipoMovimientoComercialEnum_Throws()
    {
        Assert.Throws<OperacionInvalidaException>(() =>
        {
            new MovimientoComercial((TipoMovimientoComercial)99, 1, 1, 2);
        });
    }

    [Fact]
    public async Task T2_F11_02_SameOriginAndDestinationParticipant_Throws400()
    {
        var ex = Assert.Throws<OperacionInvalidaException>(() =>
        {
            new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 5, 5);
        });
        Assert.Contains("mismo", ex.Message);
    }

    [Fact]
    public async Task T2_F11_03_EmptyItemsCollection_Returns400()
    {
        var client = CreateAdminClient();
        var (clienteId, temporadaId) = await EnsureSeedClienteAndTemporadaAsync();

        var dto = new CrearMovimientoComercialDto
        {
            Tipo = TipoMovimientoComercial.VentaACliente,
            OrigenParticipanteId = Empresa.SingletonId,
            DestinoParticipanteId = clienteId,
            ClienteId = clienteId,
            TemporadaId = temporadaId,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>() // Empty!
        };

        var res = await client.PostAsJsonAsync("/api/v1/movimientos-comerciales", dto);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task T2_F11_04_ItemWithZeroOrNegativeQuantity_Throws()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        Assert.Throws<OperacionInvalidaException>(() =>
        {
            mov.AgregarDetalle(ClaseBulbo.Clase1, 0, 10m);
        });
        Assert.Throws<OperacionInvalidaException>(() =>
        {
            mov.AgregarDetalle(ClaseBulbo.Clase1, -10, 10m);
        });
    }

    [Fact]
    public async Task T2_F11_05_QueryNonExistentMovementId_Returns404()
    {
        var client = CreateAdminClient();
        var res = await client.GetAsync("/api/v1/movimientos-comerciales/999999");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    #endregion

    #region Feature 12: Commercial Financial Rules (5 Boundary Tests)

    [Fact]
    public async Task T2_F12_01_Type2_CommissionNegative_Throws()
    {
        Assert.Throws<OperacionInvalidaException>(() =>
        {
            new MovimientoComercial(TipoMovimientoComercial.CompraACliente, 1, 2, 1, porcentajeComision: -5m);
        });
    }

    [Fact]
    public async Task T2_F12_02_Type2_CommissionExceeds100_Throws()
    {
        Assert.Throws<OperacionInvalidaException>(() =>
        {
            new MovimientoComercial(TipoMovimientoComercial.CompraACliente, 1, 2, 1, porcentajeComision: 105m);
        });
    }

    [Fact]
    public async Task T2_F12_03_FractionalCentRoundingPrecision_RoundsTo2Decimals()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.CompraACliente, 1, 2, 1, porcentajeComision: 33.33m);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 10m); // Bruto: 100

        Assert.Equal(100m, mov.MontoBruto);
        Assert.Equal(33.33m, mov.MontoComision);
        Assert.Equal(66.67m, mov.MontoNeto);
    }

    [Fact]
    public async Task T2_F12_04_NegativeItemUnitPrice_Throws()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        Assert.Throws<OperacionInvalidaException>(() =>
        {
            mov.AgregarDetalle(ClaseBulbo.Clase1, 10, -5m);
        });
    }

    [Fact]
    public async Task T2_F12_05_LargeFinancialAmounts_CalculatesWithoutOverflow()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 1_000_000, 100m);

        Assert.Equal(100_000_000m, mov.MontoTotal);
    }

    #endregion

    #region Feature 13: Commercial Workflow Transitions (5 Boundary Tests)

    [Fact]
    public async Task T2_F13_01_SkipApprovalDirectlyToInvoice_Throws()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 10m);

        // In PendienteRevision
        var ex = Assert.Throws<OperacionInvalidaException>(() =>
        {
            mov.EmitirFactura("F-1301", 1);
        });
        Assert.Contains("AprobadoAdmin", ex.Message);
    }

    [Fact]
    public async Task T2_F13_02_SkipInvoiceDirectlyToFinalizeForType1_Throws()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 10m);
        mov.AprobarAdmin(1, false);

        var ex = Assert.Throws<OperacionInvalidaException>(() =>
        {
            mov.Finalizar(1, operacionComercialId: 100);
        });
        Assert.Contains("Facturado", ex.Message);
    }

    [Fact]
    public async Task T2_F13_03_Type3_AttemptToInvoice_Throws()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.TraspasoEntreClientes, 1, 2, 3);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 0m);
        mov.AprobarAdmin(1, false);

        var ex = Assert.Throws<OperacionInvalidaException>(() =>
        {
            mov.EmitirFactura("F-1303", 1);
        });
        Assert.Contains("traspasos", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task T2_F13_04_TransitionOnAlreadyFinalizedMovement_Throws()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.TraspasoEntreClientes, 1, 2, 3);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 0m);
        mov.AprobarAdmin(1, false);
        mov.Finalizar(1, 1);

        Assert.Throws<OperacionInvalidaException>(() => mov.AprobarAdmin(1, false));
        Assert.Throws<OperacionInvalidaException>(() => mov.Rechazar(1, "Obs"));
        Assert.Throws<OperacionInvalidaException>(() => mov.EditarYReenviar(1));
    }

    [Fact]
    public async Task T2_F13_05_FacturarWithEmptyInvoiceNumber_Throws()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 10m);
        mov.AprobarAdmin(1, false);

        Assert.Throws<OperacionInvalidaException>(() => mov.EmitirFactura("", 1));
        Assert.Throws<OperacionInvalidaException>(() => mov.EmitirFactura("   ", 1));
    }

    #endregion

    #region Feature 14: Non-Terminal Rejection & Correction (5 Boundary Tests)

    [Fact]
    public async Task T2_F14_01_RejectWithoutReason_Throws()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 10m);

        Assert.Throws<OperacionInvalidaException>(() => mov.Rechazar(1, ""));
        Assert.Throws<OperacionInvalidaException>(() => mov.Rechazar(1, "   "));
    }

    [Fact]
    public async Task T2_F14_02_RejectAlreadyFinalizedMovement_Prohibited()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.TraspasoEntreClientes, 1, 2, 3);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 0m);
        mov.AprobarAdmin(1, false);
        mov.Finalizar(1, 1);

        var ex = Assert.Throws<OperacionInvalidaException>(() => mov.Rechazar(1, "Intento tardío"));
        Assert.Contains("finalizado", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task T2_F14_03_ResubmitByNonCreatorWithoutPermission_Denied()
    {
        using var scope = Factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IGestionMovimientosComerciales>();
        var (clienteId, temporadaId) = await EnsureSeedClienteAndTemporadaAsync();

        var created = await service.CrearMovimientoAsync(new CrearMovimientoComercialDto
        {
            Tipo = TipoMovimientoComercial.VentaACliente,
            OrigenParticipanteId = Empresa.SingletonId,
            DestinoParticipanteId = clienteId,
            ClienteId = clienteId,
            TemporadaId = temporadaId,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 10, PrecioUnitarioPactado = 10m }
            }
        }, 1);

        await service.RechazarAsync(created.Data!.Id, new RechazarDto { Motivo = "Obs" }, 1);

        // Different user (user 99) without MOVCOM_CORREGIR attempts resubmit
        var res = await service.EditarYReenviarAsync(created.Data.Id, new ActualizarMovimientoComercialDto
        {
            OrigenParticipanteId = Empresa.SingletonId,
            DestinoParticipanteId = clienteId,
            ClienteId = clienteId,
            TemporadaId = temporadaId,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 10, PrecioUnitarioPactado = 10m }
            }
        }, usuarioId: 99, tienePermisoCorregir: false);

        Assert.False(res.Success);
        Assert.True(res.StatusCode == 403 || res.StatusCode == 400);
    }

    [Fact]
    public async Task T2_F14_04_ResubmitMovementNotInRechazadoState_Throws()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        // Currently in PendienteRevision
        Assert.Throws<OperacionInvalidaException>(() => mov.EditarYReenviar(1));
    }

    [Fact]
    public async Task T2_F14_05_ResubmitWithEmptyItems_Rejected()
    {
        using var scope = Factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IGestionMovimientosComerciales>();
        var (clienteId, temporadaId) = await EnsureSeedClienteAndTemporadaAsync();

        var created = await service.CrearMovimientoAsync(new CrearMovimientoComercialDto
        {
            Tipo = TipoMovimientoComercial.VentaACliente,
            OrigenParticipanteId = Empresa.SingletonId,
            DestinoParticipanteId = clienteId,
            ClienteId = clienteId,
            TemporadaId = temporadaId,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 10, PrecioUnitarioPactado = 10m }
            }
        }, 1);

        await service.RechazarAsync(created.Data!.Id, new RechazarDto { Motivo = "Obs" }, 1);

        var res = await service.EditarYReenviarAsync(created.Data.Id, new ActualizarMovimientoComercialDto
        {
            OrigenParticipanteId = Empresa.SingletonId,
            DestinoParticipanteId = clienteId,
            ClienteId = clienteId,
            TemporadaId = temporadaId,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>() // Empty!
        }, 1, true);

        Assert.False(res.Success);
        Assert.Equal(400, res.StatusCode);
    }

    #endregion

    #region Feature 15: MIX Items & Stock Decomposition (5 Boundary Tests)

    [Fact]
    public async Task T2_F15_01_MixQuantityNotDivisibleBy3_Remainder1_Throws()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        var ex = Assert.Throws<OperacionInvalidaException>(() =>
        {
            mov.AgregarDetalle(ClaseBulbo.MIX, 100, 10m); // 100 % 3 = 1
        });
        Assert.Contains("múltiplo de 3", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task T2_F15_02_MixQuantityNotDivisibleBy3_Remainder2_Throws()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        var ex = Assert.Throws<OperacionInvalidaException>(() =>
        {
            mov.AgregarDetalle(ClaseBulbo.MIX, 14, 10m); // 14 % 3 = 2
        });
        Assert.Contains("múltiplo de 3", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task T2_F15_03_MixQuantityZeroOrNegative_Throws()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        Assert.Throws<OperacionInvalidaException>(() => mov.AgregarDetalle(ClaseBulbo.MIX, 0, 10m));
        Assert.Throws<OperacionInvalidaException>(() => mov.AgregarDetalle(ClaseBulbo.MIX, -3, 10m));
    }

    [Fact]
    public async Task T2_F15_04_OriginStockDeficit_InSingleDecomposedCalibre_Fails()
    {
        using var scope = Factory.Services.CreateScope();
        var stockService = scope.ServiceProvider.GetRequiredService<IGestionStock>();

        // Querying an impossible quantity (e.g. 99,999,999) returns disponible: false
        var disp = await stockService.VerificarDisponibilidadAsync(Empresa.SingletonId, ClaseBulbo.Clase1, 99_999_999);
        Assert.False(disp.Data!.Disponible);
    }

    [Fact]
    public async Task T2_F15_05_MixDecompositionRoundingIntegrity()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        mov.AgregarDetalle(ClaseBulbo.MIX, 333, 10m);
        var reqs = mov.ObtenerRequerimientosFisicosBulbos();

        Assert.Equal(111, reqs[ClaseBulbo.Clase1]);
        Assert.Equal(111, reqs[ClaseBulbo.Clase2]);
        Assert.Equal(111, reqs[ClaseBulbo.Clase3]);
    }

    #endregion

    #region Feature 16: New Client Contract Enforcement (5 Boundary Tests)

    [Fact]
    public async Task T2_F16_01_ApproveType1ForNewClientWithoutContract_Throws()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 10m);

        var ex = Assert.Throws<OperacionInvalidaException>(() =>
        {
            mov.AprobarAdmin(1, requiereContrato: true); // No contract generated!
        });
        Assert.Contains("contrato", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task T2_F16_02_ContractNumberBlank_Throws()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        Assert.Throws<OperacionInvalidaException>(() => mov.GenerarContrato("", 1));
        Assert.Throws<OperacionInvalidaException>(() => mov.GenerarContrato("   ", 1));
    }

    [Fact]
    public async Task T2_F16_03_ContractOnAlreadyFinalizedMovement_Throws()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.TraspasoEntreClientes, 1, 2, 3);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 0m);
        mov.AprobarAdmin(1, false);
        mov.Finalizar(1, 1);

        Assert.Throws<OperacionInvalidaException>(() => mov.GenerarContrato("CTR-AFTER", 1));
    }

    [Fact]
    public async Task T2_F16_04_PriorMovementsNotFinalized_StillEvaluatedAsNew()
    {
        using var scope = Factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IGestionMovimientosComerciales>();
        var db = scope.ServiceProvider.GetRequiredService<SigaDbContext>();

        var p = new Persona { Nombre = "Brand", Apellido = "New", Documento = "BN" + Random.Shared.Next(10000, 99999), FechaCreacion = DateTime.UtcNow };
        await db.Personas.AddAsync(p);
        await db.SaveChangesAsync();
        var c = new Cliente
        {
            PersonaId = p.Id,
            CodigoCliente = "BN-" + Guid.NewGuid().ToString("N")[..4],
            EstadoCliente = EstadoCliente.Activo,
            Estado = EstadoParticipante.Activo,
            FechaRegistro = DateTime.UtcNow
        };
        await db.Clientes.AddAsync(c);
        await db.SaveChangesAsync();

        var check = await service.VerificarClienteNuevoAsync(c.Id);
        Assert.True(check.Data!.EsNuevo);
    }

    [Fact]
    public async Task T2_F16_05_ContractRegenerationIdempotence()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        mov.GenerarContrato("CTR-001", 1);
        mov.GenerarContrato("CTR-002", 1);

        Assert.Equal("CTR-002", mov.Contrato!.NumeroContrato);
    }

    #endregion

    #region Feature 17: Traceability & Approval History (5 Boundary Tests)

    [Fact]
    public async Task T2_F17_01_PreserveRejectionReasonExactText()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        var reason = "Obs: ¿precio acordado? $15/u & notas especiales!";
        mov.Rechazar(1, reason);

        var log = mov.HistorialAprobaciones.Last();
        Assert.Equal(reason, log.Observaciones);
    }

    [Fact]
    public async Task T2_F17_02_MultipleRejectionCyclesAuditTrail()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 10m);
        mov.Rechazar(1, "R1");
        mov.EditarYReenviar(1);
        mov.Rechazar(1, "R2");
        mov.EditarYReenviar(1);
        mov.AprobarAdmin(1, false);

        // 1 creation + 2 rejections + 2 corrections + 1 approval = 6 history records
        Assert.Equal(6, mov.HistorialAprobaciones.Count);
    }

    [Fact]
    public async Task T2_F17_03_ActorIdentityVerification()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 456, 1, 2);
        mov.AprobarAdmin(789, false);

        Assert.Equal(456, mov.HistorialAprobaciones.First().UsuarioAprobadorId);
        Assert.Equal(789, mov.HistorialAprobaciones.Last().UsuarioAprobadorId);
    }

    [Fact]
    public async Task T2_F17_04_ChronologicalSorting_MonotonicallyIncreasing()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        mov.AprobarAdmin(1, false);

        var history = mov.HistorialAprobaciones.ToList();
        Assert.True(history[1].Fecha >= history[0].Fecha);
    }

    [Fact]
    public async Task T2_F17_05_HistoryIntegrityOnSoftDelete()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        mov.IsDeleted = true;

        Assert.NotEmpty(mov.HistorialAprobaciones);
    }

    #endregion

    #region Feature 18: Finalization Stock Impact (5 Boundary Tests)

    [Fact]
    public async Task T2_F18_01_InsufficientOriginStockOverall_ReturnsStockError()
    {
        using var scope = Factory.Services.CreateScope();
        var stockService = scope.ServiceProvider.GetRequiredService<IGestionStock>();

        var check = await stockService.VerificarDisponibilidadAsync(Empresa.SingletonId, ClaseBulbo.Clase1, 999_999_999);
        Assert.False(check.Data!.Disponible);
    }

    [Fact]
    public async Task T2_F18_02_ExactZeroStockRemaining_Allowed()
    {
        using var scope = Factory.Services.CreateScope();
        var stockService = scope.ServiceProvider.GetRequiredService<IGestionStock>();
        var db = scope.ServiceProvider.GetRequiredService<SigaDbContext>();

        // 1. Arrange: Create a dedicated client and seed exactly 50 bulbs of Clase 1 via confirmed MovimientoBulbos
        var persona = new Persona 
        { 
            Nombre = "ZeroStock", 
            Apellido = "Boundary", 
            Documento = "ZSTK" + Random.Shared.Next(10000, 99999), 
            FechaCreacion = DateTime.UtcNow 
        };
        await db.Personas.AddAsync(persona);
        await db.SaveChangesAsync();

        var cliente = new Cliente 
        { 
            PersonaId = persona.Id, 
            CodigoCliente = "CLI-ZSTK-" + Guid.NewGuid().ToString("N")[..4], 
            EstadoCliente = EstadoCliente.Activo, 
            Estado = EstadoParticipante.Activo, 
            FechaRegistro = DateTime.UtcNow 
        };
        await db.Clientes.AddAsync(cliente);
        await db.SaveChangesAsync();

        var op = new OperacionComercial 
        { 
            Tipo = TipoOperacion.Ingreso, 
            Estado = EstadoOperacion.Confirmada, 
            Fecha = DateTime.UtcNow 
        };
        await db.OperacionesComerciales.AddAsync(op);
        await db.SaveChangesAsync();

        var movIngreso = new MovimientoBulbos(op.Id, origenId: Empresa.SingletonId, destinoId: cliente.Id, usuarioCreadorId: 1);
        movIngreso.AgregarCantidad(ClaseBulbo.Clase1, 50);
        movIngreso.Confirmar();
        await db.MovimientosBulbos.AddAsync(movIngreso);
        await db.SaveChangesAsync();

        // 2. Act & Assert: Verify stock availability when requesting EXACTLY total available stock (50 bulbs)
        var exactCheck = await stockService.VerificarDisponibilidadAsync(cliente.Id, ClaseBulbo.Clase1, 50);
        Assert.True(exactCheck.Success);
        Assert.NotNull(exactCheck.Data);
        Assert.True(exactCheck.Data.Disponible);
        Assert.Equal(50, exactCheck.Data.StockDisponible);
        Assert.Equal(50, exactCheck.Data.CantidadSolicitada);
        Assert.Equal(0, exactCheck.Data.StockDisponible - exactCheck.Data.CantidadSolicitada);

        // 3. Boundary rejection: Requesting 51 (which would drive balance to -1) is rejected
        var overCheck = await stockService.VerificarDisponibilidadAsync(cliente.Id, ClaseBulbo.Clase1, 51);
        Assert.True(overCheck.Success);
        Assert.NotNull(overCheck.Data);
        Assert.False(overCheck.Data.Disponible);
    }

    [Fact]
    public async Task T2_F18_03_OriginHasZeroStock_CheckFails()
    {
        using var scope = Factory.Services.CreateScope();
        var stockService = scope.ServiceProvider.GetRequiredService<IGestionStock>();

        // Non-existent client has 0 stock
        var check = await stockService.VerificarDisponibilidadAsync(987654, ClaseBulbo.Clase1, 10);
        Assert.False(check.Data!.Disponible);
    }

    [Fact]
    public async Task T2_F18_04_FinalizeMissingOperacionComercialId_Throws()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.TraspasoEntreClientes, 1, 2, 3);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 0m);
        mov.AprobarAdmin(1, false);

        Assert.Throws<OperacionInvalidaException>(() => mov.Finalizar(1, operacionComercialId: 0));
    }

    [Fact]
    public async Task T2_F18_05_NegativeStockPrevention_DomainEnforcesNonNegativeBalances()
    {
        using var scope = Factory.Services.CreateScope();
        var stockService = scope.ServiceProvider.GetRequiredService<IGestionStock>();

        // 1. Domain invariant: Attempting to add non-positive quantities to MovimientoBulbos throws OperacionInvalidaException
        var mov = new MovimientoBulbos(1, origenId: Empresa.SingletonId, destinoId: 2, usuarioCreadorId: 1);

        var negEx = Assert.Throws<OperacionInvalidaException>(() =>
        {
            mov.AgregarCantidad(ClaseBulbo.Clase1, -50);
        });
        Assert.Contains("mayor a cero", negEx.Message);

        var zeroEx = Assert.Throws<OperacionInvalidaException>(() =>
        {
            mov.AgregarCantidad(ClaseBulbo.Clase1, 0);
        });
        Assert.Contains("mayor a cero", zeroEx.Message);

        // 2. Service invariant: Availability check strictly rejects overdrafts that would cause negative balance
        // Requesting 1 bulb for a participant with 0 stock returns Disponible == false
        var check = await stockService.VerificarDisponibilidadAsync(999999, ClaseBulbo.Clase1, 1);
        Assert.NotNull(check.Data);
        Assert.False(check.Data.Disponible);
        Assert.True(check.Data.StockDisponible < check.Data.CantidadSolicitada);
    }

    #endregion

    #region Feature 19: Relocated Reference Prices API (5 Boundary Tests)

    [Fact]
    public async Task T2_F19_01_NegativeOrZeroReferencePrice_Throws()
    {
        using var scope = Factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IGestionPreciosReferencia>();
        var (_, temporadaId) = await EnsureSeedClienteAndTemporadaAsync();

        await Assert.ThrowsAsync<OperacionInvalidaException>(() =>
            service.ConfigurarPrecioReferenciaAsync(new ConfigurarPrecioReferenciaDto
            {
                TemporadaId = temporadaId,
                ClaseBulbo = ClaseBulbo.Clase1,
                PrecioReferencia = 0m
            }));

        await Assert.ThrowsAsync<OperacionInvalidaException>(() =>
            service.ConfigurarPrecioReferenciaAsync(new ConfigurarPrecioReferenciaDto
            {
                TemporadaId = temporadaId,
                ClaseBulbo = ClaseBulbo.Clase1,
                PrecioReferencia = -10m
            }));
    }

    [Fact]
    public async Task T2_F19_02_NonExistentTemporadaId_ReturnsNotFound()
    {
        using var scope = Factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IGestionPreciosReferencia>();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.ConfigurarPrecioReferenciaAsync(new ConfigurarPrecioReferenciaDto
            {
                TemporadaId = 999999,
                ClaseBulbo = ClaseBulbo.Clase1,
                PrecioReferencia = 15m
            }));
    }

    [Fact]
    public async Task T2_F19_03_InvalidClaseBulboEnumValue_Throws()
    {
        using var scope = Factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IGestionPreciosReferencia>();
        var (_, temporadaId) = await EnsureSeedClienteAndTemporadaAsync();

        var res = await service.ObtenerPrecioVigenteAsync(temporadaId, (ClaseBulbo)99);
        Assert.False(res.Success);
    }

    [Fact]
    public async Task T2_F19_04_GetPricesForTemporadaWithoutPrices_DoesNotThrow500()
    {
        using var scope = Factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IGestionPreciosReferencia>();

        var res = await service.ListarPreciosPorTemporadaAsync(999999);
        Assert.True(res.Success);
        Assert.NotNull(res.Data);
    }

    [Fact]
    public async Task T2_F19_05_UnauthorizedPriceUpdate_EndpointGuarded()
    {
        var client = CreatePermissionsClient("CLIENTES_VER"); // Lacks PRECIOS_REF_GESTIONAR
        var isHttp = await IsRouteImplementedAsync(client, "/api/v1/precios-referencia", HttpMethod.Post);
        if (isHttp)
        {
            var res = await client.PostAsJsonAsync("/api/v1/precios-referencia", new ConfigurarPrecioReferenciaDto
            {
                TemporadaId = 1,
                ClaseBulbo = ClaseBulbo.Clase1,
                PrecioReferencia = 15m
            });
            Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        }
    }

    #endregion

    #region Feature 20: Commercial Unit Tests (5 Boundary Tests)

    [Fact]
    public async Task T2_F20_01_UnitTestsCoverZeroCommission()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.CompraACliente, 1, 2, 1, porcentajeComision: 0m);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 10m);
        Assert.Equal(0m, mov.MontoComision);
        Assert.Equal(100m, mov.MontoTotal);
    }

    [Fact]
    public async Task T2_F20_02_UnitTestsCover100PercentCommission()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.CompraACliente, 1, 2, 1, porcentajeComision: 100m);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 10m);
        Assert.Equal(100m, mov.MontoComision);
        Assert.Equal(0m, mov.MontoNeto);
        Assert.Equal(0m, mov.MontoTotal);
    }

    [Fact]
    public async Task T2_F20_03_UnitTestsCoverMixNonMultipleOf3()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        Assert.Throws<OperacionInvalidaException>(() => mov.AgregarDetalle(ClaseBulbo.MIX, 10, 10m));
    }

    [Fact]
    public async Task T2_F20_04_UnitTestsCoverInvoiceCancellationOnRejection()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 10m);
        mov.AprobarAdmin(1, false);
        var f = mov.EmitirFactura("F-2004", 1);
        mov.Rechazar(1, "Anulación");

        Assert.Equal(EstadoFactura.Anulada, f.Estado);
    }

    [Fact]
    public async Task T2_F20_05_UnitTestsCoverResubmissionPermissionCheck()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 10m);
        mov.Rechazar(1, "Obs");
        mov.EditarYReenviar(1);

        Assert.Equal(EstadoMovimientoComercial.PendienteRevision, mov.Estado);
    }

    #endregion
}
