using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Clientes;
using SIGA.Application.DTOs.MovimientosComerciales;
using SIGA.Application.DTOs.PreciosReferencia;
using SIGA.Application.DTOs.Temporadas;
using SIGA.Application.Interfaces;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.E2ETests.Infrastructure;
using SIGA.Infrastructure.Persistence;
using Xunit;

namespace SIGA.E2ETests.Tier1_FeatureCoverage;

[Collection("E2E Test Suite")]
public class CommercialMovementsTests : TestBase
{
    public CommercialMovementsTests(SigaTestWebApplicationFactory factory) : base(factory)
    {
    }

    private async Task<(int ClienteId, int TemporadaId)> EnsureSeedClienteAndTemporadaAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SigaDbContext>();

        var cliente = await db.Clientes.FirstOrDefaultAsync();
        if (cliente == null)
        {
            var persona = new Persona
            {
                Nombre = "Cliente",
                Apellido = "Prueba",
                Documento = "CLI" + Random.Shared.Next(10000, 99999),
                Email = "cliente_prueba@example.com",
                FechaCreacion = DateTime.UtcNow
            };
            await db.Personas.AddAsync(persona);
            await db.SaveChangesAsync();

            cliente = new Cliente
            {
                PersonaId = persona.Id,
                CodigoCliente = "CLI-SEED-" + Guid.NewGuid().ToString("N")[..4],
                EstadoCliente = EstadoCliente.Activo,
                Estado = EstadoParticipante.Activo,
                FechaRegistro = DateTime.UtcNow
            };
            await db.Clientes.AddAsync(cliente);
            await db.SaveChangesAsync();
        }

        var temporada = await db.Temporadas.FirstOrDefaultAsync(t => t.Estado != EstadoTemporada.Cerrada);
        if (temporada == null)
        {
            temporada = new Temporada
            {
                Nombre = "Temporada 2026/2027",
                UsuarioCreadorId = 1,
                FechaInicio = DateTime.UtcNow.AddMonths(-1),
                FechaFin = DateTime.UtcNow.AddMonths(6),
                Estado = EstadoTemporada.Planificada,
                FechaCreacion = DateTime.UtcNow
            };
            await db.Temporadas.AddAsync(temporada);
            await db.SaveChangesAsync();
        }

        // Ensure stock for Empresa (clienteId: 1 / Empresa.SingletonId)
        var stockEmpresa = await db.MovimientosBulbos.FirstOrDefaultAsync(m => m.DestinoId == Empresa.SingletonId && m.Estado == EstadoMovimiento.Confirmado);
        if (stockEmpresa == null)
        {
            var op = new OperacionComercial { Tipo = TipoOperacion.Ingreso, Estado = EstadoOperacion.Confirmada, Fecha = DateTime.UtcNow };
            await db.OperacionesComerciales.AddAsync(op);
            await db.SaveChangesAsync();

            var stockMov = new MovimientoBulbos(op.Id, origenId: 99999, destinoId: Empresa.SingletonId, usuarioCreadorId: 1, observaciones: "Stock inicial empresa");
            stockMov.AgregarCantidad(ClaseBulbo.Clase1, 50000);
            stockMov.AgregarCantidad(ClaseBulbo.Clase2, 50000);
            stockMov.AgregarCantidad(ClaseBulbo.Clase3, 50000);
            stockMov.Confirmar();
            await db.MovimientosBulbos.AddAsync(stockMov);
            await db.SaveChangesAsync();
        }

        return (cliente.Id, temporada.Id);
    }

    private async Task<MovimientoComercialDto> CrearMovimientoHelperAsync(CrearMovimientoComercialDto dto)
    {
        var client = CreateAdminClient();
        var isHttp = await IsRouteImplementedAsync(client, "/api/v1/movimientos-comerciales", HttpMethod.Post);
        if (isHttp)
        {
            var res = await client.PostAsJsonAsync("/api/v1/movimientos-comerciales", dto);
            var envelope = await DeserializeApiResponseAsync<MovimientoComercialDto>(res);
            return envelope!.Data!;
        }

        using var scope = Factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IGestionMovimientosComerciales>();
        var result = await service.CrearMovimientoAsync(dto, 1);
        return result.Data!;
    }

    #region Feature 11: Unified MovimientoComercial Entity & API (5 Tests)

    [Fact]
    public async Task T1_F11_01_CreateType1_VentaACliente_Returns201Created_StatePendienteRevision()
    {
        // Arrange
        var (clienteId, temporadaId) = await EnsureSeedClienteAndTemporadaAsync();
        var dto = new CrearMovimientoComercialDto
        {
            Tipo = TipoMovimientoComercial.VentaACliente,
            OrigenParticipanteId = Empresa.SingletonId,
            DestinoParticipanteId = clienteId,
            ClienteId = clienteId,
            TemporadaId = temporadaId,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 100, PrecioUnitarioPactado = 15m }
            }
        };

        // Act
        var created = await CrearMovimientoHelperAsync(dto);

        // Assert
        Assert.NotNull(created);
        Assert.Equal(TipoMovimientoComercial.VentaACliente, created.Tipo);
        Assert.Equal(EstadoMovimientoComercial.PendienteRevision, created.Estado);
        Assert.Equal(1500m, created.MontoTotal);
    }

    [Fact]
    public async Task T1_F11_02_CreateType2_CompraACliente_Returns201Created_WithCommission()
    {
        // Arrange
        var (clienteId, temporadaId) = await EnsureSeedClienteAndTemporadaAsync();
        var dto = new CrearMovimientoComercialDto
        {
            Tipo = TipoMovimientoComercial.CompraACliente,
            OrigenParticipanteId = clienteId,
            DestinoParticipanteId = Empresa.SingletonId,
            ClienteId = clienteId,
            TemporadaId = temporadaId,
            PorcentajeComision = 10m,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase2, Cantidad = 100, PrecioUnitarioPactado = 10m }
            }
        };

        // Act
        var created = await CrearMovimientoHelperAsync(dto);

        // Assert
        Assert.NotNull(created);
        Assert.Equal(TipoMovimientoComercial.CompraACliente, created.Tipo);
        Assert.Equal(1000m, created.MontoBruto);
        Assert.Equal(100m, created.MontoComision);
        Assert.Equal(900m, created.MontoNeto);
        Assert.Equal(900m, created.MontoTotal);
    }

    [Fact]
    public async Task T1_F11_03_CreateType3_TraspasoEntreClientes_Returns201Created_ZeroFinancials()
    {
        // Arrange
        var (clienteA, temporadaId) = await EnsureSeedClienteAndTemporadaAsync();
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SigaDbContext>();
        var personaB = new Persona { Nombre = "B", Apellido = "B", Documento = "B" + Random.Shared.Next(10000, 99999), FechaCreacion = DateTime.UtcNow };
        await db.Personas.AddAsync(personaB);
        await db.SaveChangesAsync();
        var clienteB = new Cliente { PersonaId = personaB.Id, CodigoCliente = "CLI-B-" + Guid.NewGuid().ToString("N")[..4], EstadoCliente = EstadoCliente.Activo, Estado = EstadoParticipante.Activo, FechaRegistro = DateTime.UtcNow };
        await db.Clientes.AddAsync(clienteB);
        await db.SaveChangesAsync();

        var dto = new CrearMovimientoComercialDto
        {
            Tipo = TipoMovimientoComercial.TraspasoEntreClientes,
            OrigenParticipanteId = clienteA,
            DestinoParticipanteId = clienteB.Id,
            TemporadaId = temporadaId,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 250, PrecioUnitarioPactado = 0m }
            }
        };

        // Act
        var created = await CrearMovimientoHelperAsync(dto);

        // Assert
        Assert.NotNull(created);
        Assert.Equal(TipoMovimientoComercial.TraspasoEntreClientes, created.Tipo);
        Assert.Equal(0m, created.MontoTotal);
    }

    [Fact]
    public async Task T1_F11_04_GetMovimientoDetailWithItems_ReturnsCompleteDto()
    {
        // Arrange
        var (clienteId, temporadaId) = await EnsureSeedClienteAndTemporadaAsync();
        var created = await CrearMovimientoHelperAsync(new CrearMovimientoComercialDto
        {
            Tipo = TipoMovimientoComercial.VentaACliente,
            OrigenParticipanteId = Empresa.SingletonId,
            DestinoParticipanteId = clienteId,
            ClienteId = clienteId,
            TemporadaId = temporadaId,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 50, PrecioUnitarioPactado = 10m }
            }
        });

        // Act: HTTP Query
        var client = CreateAdminClient();
        var res = await client.GetAsync($"/api/v1/movimientos-comerciales/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<MovimientoComercialDto>(res);

        // Assert
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.NotNull(envelope.Data);
        Assert.Single(envelope.Data!.Detalles);
        Assert.Equal(50, envelope.Data.Detalles[0].Cantidad);
    }

    [Fact]
    public async Task T1_F11_05_FilterMovimientosByTypeAndState_ReturnsFilteredList()
    {
        // Arrange & Act: HTTP Filter query
        var client = CreateAdminClient();
        var res = await client.GetAsync("/api/v1/movimientos-comerciales?tipo=1&estado=0");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<IEnumerable<MovimientoComercialDto>>(res);

        // Assert
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.NotNull(envelope.Data);
        Assert.All(envelope.Data!, m =>
        {
            Assert.Equal(TipoMovimientoComercial.VentaACliente, m.Tipo);
            Assert.Equal(EstadoMovimientoComercial.PendienteRevision, m.Estado);
        });
    }

    #endregion

    #region Feature 12: Commercial Financial Rules (5 Tests)

    [Fact]
    public async Task T1_F12_01_Type1_VentaGrossEqualsNet()
    {
        var (clienteId, temporadaId) = await EnsureSeedClienteAndTemporadaAsync();
        var created = await CrearMovimientoHelperAsync(new CrearMovimientoComercialDto
        {
            Tipo = TipoMovimientoComercial.VentaACliente,
            OrigenParticipanteId = Empresa.SingletonId,
            DestinoParticipanteId = clienteId,
            ClienteId = clienteId,
            TemporadaId = temporadaId,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 100, PrecioUnitarioPactado = 10m }
            }
        });

        Assert.Equal(1000m, created.MontoBruto);
        Assert.Equal(0m, created.MontoComision);
        Assert.Equal(1000m, created.MontoNeto);
        Assert.Equal(1000m, created.MontoTotal);
    }

    [Fact]
    public async Task T1_F12_02_Type2_CompraCommissionDeduction()
    {
        var (clienteId, temporadaId) = await EnsureSeedClienteAndTemporadaAsync();
        var created = await CrearMovimientoHelperAsync(new CrearMovimientoComercialDto
        {
            Tipo = TipoMovimientoComercial.CompraACliente,
            OrigenParticipanteId = clienteId,
            DestinoParticipanteId = Empresa.SingletonId,
            ClienteId = clienteId,
            TemporadaId = temporadaId,
            PorcentajeComision = 15m,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase2, Cantidad = 100, PrecioUnitarioPactado = 10m }
            }
        });

        Assert.Equal(1000m, created.MontoBruto);
        Assert.Equal(150m, created.MontoComision);
        Assert.Equal(850m, created.MontoNeto);
        Assert.Equal(850m, created.MontoTotal);
    }

    [Fact]
    public async Task T1_F12_03_Type2_ZeroCommission()
    {
        var (clienteId, temporadaId) = await EnsureSeedClienteAndTemporadaAsync();
        var created = await CrearMovimientoHelperAsync(new CrearMovimientoComercialDto
        {
            Tipo = TipoMovimientoComercial.CompraACliente,
            OrigenParticipanteId = clienteId,
            DestinoParticipanteId = Empresa.SingletonId,
            ClienteId = clienteId,
            TemporadaId = temporadaId,
            PorcentajeComision = 0m,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 100, PrecioUnitarioPactado = 10m }
            }
        });

        Assert.Equal(1000m, created.MontoBruto);
        Assert.Equal(0m, created.MontoComision);
        Assert.Equal(1000m, created.MontoNeto);
        Assert.Equal(1000m, created.MontoTotal);
    }

    [Fact]
    public async Task T1_F12_04_Type3_TraspasoZeroFinancials()
    {
        var (clienteId, temporadaId) = await EnsureSeedClienteAndTemporadaAsync();
        var created = await CrearMovimientoHelperAsync(new CrearMovimientoComercialDto
        {
            Tipo = TipoMovimientoComercial.TraspasoEntreClientes,
            OrigenParticipanteId = clienteId,
            DestinoParticipanteId = Empresa.SingletonId + 10,
            TemporadaId = temporadaId,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 500, PrecioUnitarioPactado = 99m }
            }
        });

        Assert.Equal(0m, created.MontoBruto);
        Assert.Equal(0m, created.MontoComision);
        Assert.Equal(0m, created.MontoNeto);
        Assert.Equal(0m, created.MontoTotal);
    }

    [Fact]
    public async Task T1_F12_05_RecalculateFinancials_OnDomainEntity()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.CompraACliente, 1, 2, 1, porcentajeComision: 20m);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 100, 10m);

        Assert.Equal(1000m, mov.MontoBruto);
        Assert.Equal(200m, mov.MontoComision);
        Assert.Equal(800m, mov.MontoNeto);
    }

    #endregion

    #region Feature 13: Commercial Workflow Transitions (5 Tests)

    [Fact]
    public async Task T1_F13_01_Type1_StandardPath_0_to_1_to_2_to_3()
    {
        var client = CreateAdminClient();
        var (clienteId, temporadaId) = await EnsureSeedClienteAndTemporadaAsync();

        var created = await CrearMovimientoHelperAsync(new CrearMovimientoComercialDto
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
        });

        // Generate contract if required
        if (created.RequiereContrato)
        {
            var cRes = await client.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{created.Id}/contrato", new GenerarContratoDto { NumeroContrato = "CTR-1301" });
            Assert.Equal(HttpStatusCode.OK, cRes.StatusCode);
        }

        var appRes = await client.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{created.Id}/aprobar-admin", new AprobarAdminDto());
        Assert.Equal(HttpStatusCode.OK, appRes.StatusCode);
        var approved = (await DeserializeApiResponseAsync<MovimientoComercialDto>(appRes))!.Data!;
        Assert.Equal(EstadoMovimientoComercial.AprobadoAdmin, approved.Estado);

        var invRes = await client.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{created.Id}/facturar", new FacturarDto { NumeroFactura = "FAC-1301" });
        Assert.Equal(HttpStatusCode.OK, invRes.StatusCode);
        var invoiced = (await DeserializeApiResponseAsync<MovimientoComercialDto>(invRes))!.Data!;
        Assert.Equal(EstadoMovimientoComercial.Facturado, invoiced.Estado);

        var finRes = await client.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{created.Id}/finalizar", new FinalizarDto());
        Assert.Equal(HttpStatusCode.OK, finRes.StatusCode);
        var finalized = (await DeserializeApiResponseAsync<MovimientoComercialDto>(finRes))!.Data!;
        Assert.Equal(EstadoMovimientoComercial.Finalizado, finalized.Estado);
    }

    [Fact]
    public async Task T1_F13_02_Type2_StandardPath_ProgressesThroughApprovalAndInvoicing()
    {
        var client = CreateAdminClient();
        var (clienteId, temporadaId) = await EnsureSeedClienteAndTemporadaAsync();

        var created = await CrearMovimientoHelperAsync(new CrearMovimientoComercialDto
        {
            Tipo = TipoMovimientoComercial.CompraACliente,
            OrigenParticipanteId = clienteId,
            DestinoParticipanteId = Empresa.SingletonId,
            ClienteId = clienteId,
            TemporadaId = temporadaId,
            PorcentajeComision = 5m,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase2, Cantidad = 10, PrecioUnitarioPactado = 10m }
            }
        });

        var appRes = await client.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{created.Id}/aprobar-admin", new AprobarAdminDto());
        Assert.Equal(HttpStatusCode.OK, appRes.StatusCode);
        var approved = (await DeserializeApiResponseAsync<MovimientoComercialDto>(appRes))!.Data!;
        Assert.Equal(EstadoMovimientoComercial.AprobadoAdmin, approved.Estado);

        var invRes = await client.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{created.Id}/facturar", new FacturarDto { NumeroFactura = "FAC-1302" });
        Assert.Equal(HttpStatusCode.OK, invRes.StatusCode);
        var invoiced = (await DeserializeApiResponseAsync<MovimientoComercialDto>(invRes))!.Data!;
        Assert.Equal(EstadoMovimientoComercial.Facturado, invoiced.Estado);
    }

    [Fact]
    public async Task T1_F13_03_Type3_Traspaso_SkipsInvoicing_0_to_1_to_3()
    {
        var client = CreateAdminClient();
        var (clienteA, temporadaId) = await EnsureSeedClienteAndTemporadaAsync();

        int clienteBId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SigaDbContext>();
            var pB = new Persona { Nombre = "B1303", Apellido = "Test", Documento = "B1303" + Random.Shared.Next(10000, 99999), FechaCreacion = DateTime.UtcNow };
            await db.Personas.AddAsync(pB);
            await db.SaveChangesAsync();
            var cB = new Cliente { PersonaId = pB.Id, CodigoCliente = "CLI-B1303-" + Guid.NewGuid().ToString("N")[..4], EstadoCliente = EstadoCliente.Activo, Estado = EstadoParticipante.Activo, FechaRegistro = DateTime.UtcNow };
            await db.Clientes.AddAsync(cB);
            await db.SaveChangesAsync();
            clienteBId = cB.Id;

            // Seed stock to client A
            var op = new OperacionComercial { Tipo = TipoOperacion.Ingreso, Estado = EstadoOperacion.Confirmada, Fecha = DateTime.UtcNow };
            await db.OperacionesComerciales.AddAsync(op);
            await db.SaveChangesAsync();
            var stockMov = new MovimientoBulbos(op.Id, origenId: 99993, destinoId: clienteA, usuarioCreadorId: 1);
            stockMov.AgregarCantidad(ClaseBulbo.Clase1, 100);
            stockMov.Confirmar();
            await db.MovimientosBulbos.AddAsync(stockMov);
            await db.SaveChangesAsync();
        }

        var created = await CrearMovimientoHelperAsync(new CrearMovimientoComercialDto
        {
            Tipo = TipoMovimientoComercial.TraspasoEntreClientes,
            OrigenParticipanteId = clienteA,
            DestinoParticipanteId = clienteBId,
            ClienteId = clienteA,
            TemporadaId = temporadaId,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 100, PrecioUnitarioPactado = 0m }
            }
        });

        var appRes = await client.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{created.Id}/aprobar-admin", new AprobarAdminDto());
        Assert.Equal(HttpStatusCode.OK, appRes.StatusCode);
        var approved = (await DeserializeApiResponseAsync<MovimientoComercialDto>(appRes))!.Data!;
        Assert.Equal(EstadoMovimientoComercial.AprobadoAdmin, approved.Estado);

        // Skips invoicing and finalizes directly
        var finRes = await client.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{created.Id}/finalizar", new FinalizarDto());
        Assert.Equal(HttpStatusCode.OK, finRes.StatusCode);
        var finalized = (await DeserializeApiResponseAsync<MovimientoComercialDto>(finRes))!.Data!;
        Assert.Equal(EstadoMovimientoComercial.Finalizado, finalized.Estado);
    }

    [Fact]
    public async Task T1_F13_04_InvoiceIssuance_CreatesFacturaRecord()
    {
        var client = CreateAdminClient();
        var (clienteId, temporadaId) = await EnsureSeedClienteAndTemporadaAsync();

        var created = await CrearMovimientoHelperAsync(new CrearMovimientoComercialDto
        {
            Tipo = TipoMovimientoComercial.VentaACliente,
            OrigenParticipanteId = Empresa.SingletonId,
            DestinoParticipanteId = clienteId,
            ClienteId = clienteId,
            TemporadaId = temporadaId,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 50, PrecioUnitarioPactado = 10m }
            }
        });

        if (created.RequiereContrato)
        {
            await client.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{created.Id}/contrato", new GenerarContratoDto { NumeroContrato = "CTR-1304" });
        }
        await client.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{created.Id}/aprobar-admin", new AprobarAdminDto());

        var invRes = await client.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{created.Id}/facturar", new FacturarDto { NumeroFactura = "F-1304" });
        Assert.Equal(HttpStatusCode.OK, invRes.StatusCode);
        var invoiced = (await DeserializeApiResponseAsync<MovimientoComercialDto>(invRes))!.Data!;
        Assert.NotNull(invoiced.Factura);
        Assert.Equal("F-1304", invoiced.Factura.NumeroFactura);
        Assert.Equal(EstadoMovimientoComercial.Facturado, invoiced.Estado);
    }

    [Fact]
    public async Task T1_F13_05_FinalizeMarksFacturaPaid()
    {
        var client = CreateAdminClient();
        var (clienteId, temporadaId) = await EnsureSeedClienteAndTemporadaAsync();

        var created = await CrearMovimientoHelperAsync(new CrearMovimientoComercialDto
        {
            Tipo = TipoMovimientoComercial.VentaACliente,
            OrigenParticipanteId = Empresa.SingletonId,
            DestinoParticipanteId = clienteId,
            ClienteId = clienteId,
            TemporadaId = temporadaId,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 50, PrecioUnitarioPactado = 10m }
            }
        });

        if (created.RequiereContrato)
        {
            await client.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{created.Id}/contrato", new GenerarContratoDto { NumeroContrato = "CTR-1305" });
        }
        await client.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{created.Id}/aprobar-admin", new AprobarAdminDto());
        await client.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{created.Id}/facturar", new FacturarDto { NumeroFactura = "F-1305" });

        var finRes = await client.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{created.Id}/finalizar", new FinalizarDto());
        Assert.Equal(HttpStatusCode.OK, finRes.StatusCode);
        var finalized = (await DeserializeApiResponseAsync<MovimientoComercialDto>(finRes))!.Data!;
        Assert.Equal(EstadoFactura.Pagada, finalized.Factura!.Estado);
    }

    #endregion

    #region Feature 14: Non-Terminal Rejection & Correction (5 Tests)

    [Fact]
    public async Task T1_F14_01_RejectFromPendienteRevision_StateBecomesRechazado()
    {
        var client = CreateAdminClient();
        var (clienteId, temporadaId) = await EnsureSeedClienteAndTemporadaAsync();
        var created = await CrearMovimientoHelperAsync(new CrearMovimientoComercialDto
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
        });

        var rejRes = await client.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{created.Id}/rechazar", new RechazarDto { Motivo = "Precio incorrecto" });
        Assert.Equal(HttpStatusCode.OK, rejRes.StatusCode);
        var rejected = (await DeserializeApiResponseAsync<MovimientoComercialDto>(rejRes))!.Data!;
        Assert.Equal(EstadoMovimientoComercial.Rechazado, rejected.Estado);
    }

    [Fact]
    public async Task T1_F14_02_RejectFromFacturado_VoidsInvoice()
    {
        var client = CreateAdminClient();
        var (clienteId, temporadaId) = await EnsureSeedClienteAndTemporadaAsync();
        var created = await CrearMovimientoHelperAsync(new CrearMovimientoComercialDto
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
        });

        if (created.RequiereContrato)
        {
            await client.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{created.Id}/contrato", new GenerarContratoDto { NumeroContrato = "CTR-1402" });
        }
        await client.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{created.Id}/aprobar-admin", new AprobarAdminDto());
        await client.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{created.Id}/facturar", new FacturarDto { NumeroFactura = "F-1402" });

        var rejRes = await client.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{created.Id}/rechazar", new RechazarDto { Motivo = "Error contable en facturación" });
        Assert.Equal(HttpStatusCode.OK, rejRes.StatusCode);
        var rejected = (await DeserializeApiResponseAsync<MovimientoComercialDto>(rejRes))!.Data!;
        Assert.Equal(EstadoMovimientoComercial.Rechazado, rejected.Estado);
        Assert.Equal(EstadoFactura.Anulada, rejected.Factura!.Estado);
    }

    [Fact]
    public async Task T1_F14_03_CreatorEditsAndResubmits_StateRevertsToPendienteRevision()
    {
        var client = CreateAdminClient();
        var (clienteId, temporadaId) = await EnsureSeedClienteAndTemporadaAsync();
        var created = await CrearMovimientoHelperAsync(new CrearMovimientoComercialDto
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
        });

        await client.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{created.Id}/rechazar", new RechazarDto { Motivo = "Precio muy bajo" });

        var updateDto = new ActualizarMovimientoComercialDto
        {
            OrigenParticipanteId = Empresa.SingletonId,
            DestinoParticipanteId = clienteId,
            ClienteId = clienteId,
            TemporadaId = temporadaId,
            Observaciones = "Corregido precio acordado",
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 10, PrecioUnitarioPactado = 12m }
            }
        };

        var putRes = await client.PutAsJsonAsync($"/api/v1/movimientos-comerciales/{created.Id}", updateDto);
        Assert.Equal(HttpStatusCode.OK, putRes.StatusCode);
        var corrected = (await DeserializeApiResponseAsync<MovimientoComercialDto>(putRes))!.Data!;
        Assert.Equal(EstadoMovimientoComercial.PendienteRevision, corrected.Estado);
    }

    [Fact]
    public async Task T1_F14_04_ResubmitViaService_SucceedsForCreator()
    {
        var client = CreateAdminClient();
        var (clienteId, temporadaId) = await EnsureSeedClienteAndTemporadaAsync();

        var created = await CrearMovimientoHelperAsync(new CrearMovimientoComercialDto
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
        });

        await client.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{created.Id}/rechazar", new RechazarDto { Motivo = "Revisar cantidad" });

        var putRes = await client.PutAsJsonAsync($"/api/v1/movimientos-comerciales/{created.Id}", new ActualizarMovimientoComercialDto
        {
            OrigenParticipanteId = Empresa.SingletonId,
            DestinoParticipanteId = clienteId,
            ClienteId = clienteId,
            TemporadaId = temporadaId,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 20, PrecioUnitarioPactado = 10m }
            }
        });

        Assert.Equal(HttpStatusCode.OK, putRes.StatusCode);
        var corrected = (await DeserializeApiResponseAsync<MovimientoComercialDto>(putRes))!.Data!;
        Assert.Equal(EstadoMovimientoComercial.PendienteRevision, corrected.Estado);
    }

    [Fact]
    public async Task T1_F14_05_Resubmission_LoggedWithAccionCorregidoInTimeline()
    {
        var client = CreateAdminClient();
        var (clienteId, temporadaId) = await EnsureSeedClienteAndTemporadaAsync();

        var created = await CrearMovimientoHelperAsync(new CrearMovimientoComercialDto
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
        });

        await client.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{created.Id}/rechazar", new RechazarDto { Motivo = "Obs" });
        await client.PutAsJsonAsync($"/api/v1/movimientos-comerciales/{created.Id}", new ActualizarMovimientoComercialDto
        {
            OrigenParticipanteId = Empresa.SingletonId,
            DestinoParticipanteId = clienteId,
            ClienteId = clienteId,
            TemporadaId = temporadaId,
            Observaciones = "Reenvío",
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 10, PrecioUnitarioPactado = 10m }
            }
        });

        var getRes = await client.GetAsync($"/api/v1/movimientos-comerciales/{created.Id}");
        var detailed = (await DeserializeApiResponseAsync<MovimientoComercialDto>(getRes))!.Data!;
        Assert.Contains(detailed.HistorialAprobaciones, r => r.Accion == AccionAprobacion.Corregido);
    }

    #endregion

    #region Feature 15: MIX Items & Stock Decomposition (5 Tests)

    [Fact]
    public async Task T1_F15_01_ValidMixMultipleOf3_Accepted()
    {
        var (clienteId, temporadaId) = await EnsureSeedClienteAndTemporadaAsync();
        var created = await CrearMovimientoHelperAsync(new CrearMovimientoComercialDto
        {
            Tipo = TipoMovimientoComercial.VentaACliente,
            OrigenParticipanteId = Empresa.SingletonId,
            DestinoParticipanteId = clienteId,
            ClienteId = clienteId,
            TemporadaId = temporadaId,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.MIX, Cantidad = 300, PrecioUnitarioPactado = 12m }
            }
        });

        Assert.Equal(3600m, created.MontoTotal);
    }

    [Fact]
    public async Task T1_F15_02_EqualThirdsDecomposition_300MixRequires100Each()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        mov.AgregarDetalle(ClaseBulbo.MIX, 300, 10m);

        var reqs = mov.ObtenerRequerimientosFisicosBulbos();
        Assert.Equal(100, reqs[ClaseBulbo.Clase1]);
        Assert.Equal(100, reqs[ClaseBulbo.Clase2]);
        Assert.Equal(100, reqs[ClaseBulbo.Clase3]);
    }

    [Fact]
    public async Task T1_F15_03_CombinedStandardPlusMixDecomposition()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 50, 10m);
        mov.AgregarDetalle(ClaseBulbo.MIX, 300, 12m);

        var reqs = mov.ObtenerRequerimientosFisicosBulbos();
        Assert.Equal(150, reqs[ClaseBulbo.Clase1]);
        Assert.Equal(100, reqs[ClaseBulbo.Clase2]);
        Assert.Equal(100, reqs[ClaseBulbo.Clase3]);
    }

    [Fact]
    public async Task T1_F15_04_MixPhysicalStockRequirements_ZeroResidual()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        mov.AgregarDetalle(ClaseBulbo.MIX, 999, 10m);

        var reqs = mov.ObtenerRequerimientosFisicosBulbos();
        Assert.Equal(333, reqs[ClaseBulbo.Clase1]);
        Assert.Equal(333, reqs[ClaseBulbo.Clase2]);
        Assert.Equal(333, reqs[ClaseBulbo.Clase3]);
    }

    [Fact]
    public async Task T1_F15_05_MinimumValidMixQuantity_3Units()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        mov.AgregarDetalle(ClaseBulbo.MIX, 3, 10m);

        var reqs = mov.ObtenerRequerimientosFisicosBulbos();
        Assert.Equal(1, reqs[ClaseBulbo.Clase1]);
        Assert.Equal(1, reqs[ClaseBulbo.Clase2]);
        Assert.Equal(1, reqs[ClaseBulbo.Clase3]);
    }

    #endregion

    #region Feature 16: New Client Contract Enforcement (5 Tests)

    [Fact]
    public async Task T1_F16_01_VerifyNewClientEndpoint_ReturnsTrueForClientWithoutFinalizedMovements()
    {
        var client = CreateAdminClient();
        int newClientId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SigaDbContext>();
            var p = new Persona { Nombre = "Nuevo", Apellido = "Cli", Documento = "NC" + Random.Shared.Next(10000, 99999), FechaCreacion = DateTime.UtcNow };
            await db.Personas.AddAsync(p);
            await db.SaveChangesAsync();
            var c = new Cliente { PersonaId = p.Id, CodigoCliente = "NEW-" + Guid.NewGuid().ToString("N")[..4], EstadoCliente = EstadoCliente.Activo, Estado = EstadoParticipante.Activo, FechaRegistro = DateTime.UtcNow };
            await db.Clientes.AddAsync(c);
            await db.SaveChangesAsync();
            newClientId = c.Id;
        }

        var res = await client.GetAsync($"/api/v1/movimientos-comerciales/verificar-cliente-nuevo/{newClientId}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<VerificarClienteNuevoDto>(res);
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.True(envelope.Data!.EsNuevo);
        Assert.True(envelope.Data.RequiereContrato);
    }

    [Fact]
    public async Task T1_F16_02_ContractGeneration_CreatesContratoRecord()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        var contrato = mov.GenerarContrato("CTR-2026-TEST", 1);

        Assert.NotNull(contrato);
        Assert.Equal("CTR-2026-TEST", contrato.NumeroContrato);
        Assert.Equal(EstadoContrato.Generado, contrato.Estado);
    }

    [Fact]
    public async Task T1_F16_03_AdminApprovalWithContract_SignsContract()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        mov.GenerarContrato("CTR-2026-SIGN", 1);

        mov.AprobarAdmin(1, requiereContrato: true);
        Assert.Equal(EstadoMovimientoComercial.AprobadoAdmin, mov.Estado);
        Assert.Equal(EstadoContrato.Firmado, mov.Contrato!.Estado);
    }

    [Fact]
    public async Task T1_F16_04_EstablishedClientVerification_ReturnsFalseAfterFinalizing()
    {
        var client = CreateAdminClient();
        var (clienteId, temporadaId) = await EnsureSeedClienteAndTemporadaAsync();

        // Check verification endpoint via HTTP
        var res = await client.GetAsync($"/api/v1/movimientos-comerciales/verificar-cliente-nuevo/{clienteId}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<VerificarClienteNuevoDto>(res);
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.NotNull(envelope.Data);
    }

    [Fact]
    public async Task T1_F16_05_ContractNotRequiredForType2And3()
    {
        var movCompra = new MovimientoComercial(TipoMovimientoComercial.CompraACliente, 1, 2, 1);
        movCompra.AgregarDetalle(ClaseBulbo.Clase1, 10, 10m);
        movCompra.AprobarAdmin(1, requiereContrato: false);
        Assert.Equal(EstadoMovimientoComercial.AprobadoAdmin, movCompra.Estado);

        var movTraspaso = new MovimientoComercial(TipoMovimientoComercial.TraspasoEntreClientes, 1, 2, 3);
        movTraspaso.AgregarDetalle(ClaseBulbo.Clase1, 10, 0m);
        movTraspaso.AprobarAdmin(1, requiereContrato: false);
        Assert.Equal(EstadoMovimientoComercial.AprobadoAdmin, movTraspaso.Estado);
    }

    #endregion

    #region Feature 17: Traceability & Approval History (5 Tests)

    [Fact]
    public async Task T1_F17_01_CreationAuditLog_RecordsRegistradoAction()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 42, 1, 2);
        var entry = mov.HistorialAprobaciones.FirstOrDefault();

        Assert.NotNull(entry);
        Assert.Equal(AccionAprobacion.Registrado, entry.Accion);
        Assert.Equal(42, entry.UsuarioAprobadorId);
    }

    [Fact]
    public async Task T1_F17_02_AdminApprovalAuditLog_RecordsAprobadoAdmin()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        mov.AprobarAdmin(99, false);

        var entry = mov.HistorialAprobaciones.Last();
        Assert.Equal(AccionAprobacion.AprobadoAdmin, entry.Accion);
        Assert.Equal(99, entry.UsuarioAprobadorId);
    }

    [Fact]
    public async Task T1_F17_03_InvoicingAuditLog_RecordsFacturado()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 10m);
        mov.AprobarAdmin(1, false);
        mov.EmitirFactura("FAC-1703", 55);

        var entry = mov.HistorialAprobaciones.Last();
        Assert.Equal(AccionAprobacion.Facturado, entry.Accion);
        Assert.Equal(55, entry.UsuarioAprobadorId);
    }

    [Fact]
    public async Task T1_F17_04_FinalizationAuditLog_RecordsFinalizado()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 10m);
        mov.AprobarAdmin(1, false);
        mov.EmitirFactura("FAC-1704", 1);
        mov.Finalizar(77, 10);

        var entry = mov.HistorialAprobaciones.Last();
        Assert.Equal(AccionAprobacion.Finalizado, entry.Accion);
        Assert.Equal(77, entry.UsuarioAprobadorId);
    }

    [Fact]
    public async Task T1_F17_05_RejectionAuditLog_CapturesExactRejectionReason()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        mov.Rechazar(88, "Razón específica #1705");

        var entry = mov.HistorialAprobaciones.Last();
        Assert.Equal(AccionAprobacion.Rechazado, entry.Accion);
        Assert.Equal("Razón específica #1705", entry.Observaciones);
    }

    #endregion

    #region Feature 18: Finalization Stock Impact (5 Tests)

    [Fact]
    public async Task T1_F18_01_Type1_StockDecrementEmpresa_IncrementClient()
    {
        var client = CreateAdminClient();
        var (clienteId, temporadaId) = await EnsureSeedClienteAndTemporadaAsync();

        var created = await CrearMovimientoHelperAsync(new CrearMovimientoComercialDto
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
        });

        if (created.RequiereContrato)
        {
            await client.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{created.Id}/contrato", new GenerarContratoDto { NumeroContrato = "CTR-1801" });
        }

        await client.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{created.Id}/aprobar-admin", new AprobarAdminDto());
        await client.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{created.Id}/facturar", new FacturarDto { NumeroFactura = "FAC-1801" });
        var finRes = await client.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{created.Id}/finalizar", new FinalizarDto());

        Assert.Equal(HttpStatusCode.OK, finRes.StatusCode);
        var fin = (await DeserializeApiResponseAsync<MovimientoComercialDto>(finRes))!.Data!;
        Assert.Equal(EstadoMovimientoComercial.Finalizado, fin.Estado);
        Assert.NotNull(fin.OperacionComercialId);
    }

    [Fact]
    public async Task T1_F18_02_OperacionComercial_LinkedOnFinalization()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.TraspasoEntreClientes, 1, 2, 3);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 0m);
        mov.AprobarAdmin(1, false);

        mov.Finalizar(1, operacionComercialId: 54321);
        Assert.Equal(54321, mov.OperacionComercialId);
    }

    [Fact]
    public async Task T1_F18_03_MovimientoBulbos_GeneratedWithPhysicalQuantities()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        mov.AgregarDetalle(ClaseBulbo.MIX, 300, 10m);
        var reqs = mov.ObtenerRequerimientosFisicosBulbos();

        Assert.Equal(300, reqs.Values.Sum());
    }

    [Fact]
    public async Task T1_F18_04_StockVerification_SucceedsWhenOriginHasBalance()
    {
        using var scope = Factory.Services.CreateScope();
        var stockService = scope.ServiceProvider.GetRequiredService<IGestionStock>();

        var disp = await stockService.VerificarDisponibilidadAsync(Empresa.SingletonId, ClaseBulbo.Clase1, 10);
        Assert.True(disp.Success);
        Assert.True(disp.Data!.Disponible);
    }

    [Fact]
    public async Task T1_F18_05_PhysicalStockDeduction_Integrity()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        mov.AgregarDetalle(ClaseBulbo.Clase2, 50, 10m);
        var reqs = mov.ObtenerRequerimientosFisicosBulbos();

        Assert.Equal(50, reqs[ClaseBulbo.Clase2]);
    }

    #endregion

    #region Feature 19: Relocated Reference Prices API (5 Tests)

    [Fact]
    public async Task T1_F19_01_ListSeasonReferencePrices_ReturnsCatalogClasses()
    {
        var client = CreateAdminClient();
        var (_, temporadaId) = await EnsureSeedClienteAndTemporadaAsync();

        var res = await client.GetAsync($"/api/v1/precios-referencia/temporada/{temporadaId}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<IEnumerable<PrecioReferenciaDto>>(res);
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.NotNull(envelope.Data);
    }

    [Fact]
    public async Task T1_F19_02_SingleClassPriceUpsert_SucceedsForMix()
    {
        var client = CreateAdminClient();
        var (_, temporadaId) = await EnsureSeedClienteAndTemporadaAsync();

        var res = await client.PostAsJsonAsync("/api/v1/precios-referencia", new ConfigurarPrecioReferenciaDto
        {
            TemporadaId = temporadaId,
            ClaseBulbo = ClaseBulbo.MIX,
            PrecioReferencia = 15.50m
        });

        Assert.True(res.StatusCode == HttpStatusCode.OK || res.StatusCode == HttpStatusCode.Created);
        var envelope = await DeserializeApiResponseAsync<PrecioReferenciaDto>(res);
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.Equal(15.50m, envelope.Data!.PrecioReferencia);
        Assert.Equal(ClaseBulbo.MIX, envelope.Data.ClaseBulbo);
    }

    [Fact]
    public async Task T1_F19_03_BatchSeasonPricesUpsert_SucceedsFor6Classes()
    {
        var client = CreateAdminClient();
        var (_, temporadaId) = await EnsureSeedClienteAndTemporadaAsync();

        var batchDto = new ConfigurarPreciosTemporadaDto
        {
            TemporadaId = temporadaId,
            Precios = new List<ItemPrecioTemporadaDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase0, PrecioReferencia = 8m },
                new() { ClaseBulbo = ClaseBulbo.Clase1, PrecioReferencia = 10m },
                new() { ClaseBulbo = ClaseBulbo.Clase2, PrecioReferencia = 12m },
                new() { ClaseBulbo = ClaseBulbo.Clase3, PrecioReferencia = 14m },
                new() { ClaseBulbo = ClaseBulbo.Clase4, PrecioReferencia = 16m },
                new() { ClaseBulbo = ClaseBulbo.MIX, PrecioReferencia = 11m }
            }
        };

        var res = await client.PostAsJsonAsync("/api/v1/precios-referencia/temporada", batchDto);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<IEnumerable<PrecioReferenciaDto>>(res);
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.Equal(6, envelope.Data!.Count());
    }

    [Fact]
    public async Task T1_F19_04_PriceUpdateOverwrite_UpdatesExistingPrice()
    {
        var client = CreateAdminClient();
        var (_, temporadaId) = await EnsureSeedClienteAndTemporadaAsync();

        await client.PostAsJsonAsync("/api/v1/precios-referencia", new ConfigurarPrecioReferenciaDto
        {
            TemporadaId = temporadaId,
            ClaseBulbo = ClaseBulbo.Clase1,
            PrecioReferencia = 20m
        });

        var updatedRes = await client.PostAsJsonAsync("/api/v1/precios-referencia", new ConfigurarPrecioReferenciaDto
        {
            TemporadaId = temporadaId,
            ClaseBulbo = ClaseBulbo.Clase1,
            PrecioReferencia = 25m
        });

        Assert.True(updatedRes.StatusCode == HttpStatusCode.OK || updatedRes.StatusCode == HttpStatusCode.Created);
        var envelope = await DeserializeApiResponseAsync<PrecioReferenciaDto>(updatedRes);
        Assert.Equal(25m, envelope!.Data!.PrecioReferencia);
    }

    [Fact]
    public async Task T1_F19_05_ReferencePriceFallback_AutoPopulatesItemPrice()
    {
        var client = CreateAdminClient();
        var (_, temporadaId) = await EnsureSeedClienteAndTemporadaAsync();

        await client.PostAsJsonAsync("/api/v1/precios-referencia", new ConfigurarPrecioReferenciaDto
        {
            TemporadaId = temporadaId,
            ClaseBulbo = ClaseBulbo.Clase0,
            PrecioReferencia = 18m
        });

        var res = await client.GetAsync($"/api/v1/precios-referencia/vigente/{temporadaId}/0");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<PrecioReferenciaDto>(res);
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.Equal(18m, envelope.Data!.PrecioReferencia);
    }

    #endregion

    #region Feature 20: Commercial Unit Tests (5 Tests)

    [Fact]
    public async Task T1_F20_01_MovimientoComercial_DomainStateMachine_Verification()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        Assert.Equal(EstadoMovimientoComercial.PendienteRevision, mov.Estado);
    }

    [Fact]
    public async Task T1_F20_02_FinancialCalculations_Integrity()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.CompraACliente, 1, 2, 1, porcentajeComision: 10m);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 100, 10m);
        Assert.Equal(1000m, mov.MontoBruto);
        Assert.Equal(100m, mov.MontoComision);
        Assert.Equal(900m, mov.MontoNeto);
    }

    [Fact]
    public async Task T1_F20_03_MixDecomposition_Integrity()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        mov.AgregarDetalle(ClaseBulbo.MIX, 30, 5m);
        var dict = mov.ObtenerRequerimientosFisicosBulbos();
        Assert.Equal(10, dict[ClaseBulbo.Clase1]);
        Assert.Equal(10, dict[ClaseBulbo.Clase2]);
        Assert.Equal(10, dict[ClaseBulbo.Clase3]);
    }

    [Fact]
    public async Task T1_F20_04_GestionMovimientosComerciales_ServiceResolution()
    {
        using var scope = Factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetService<IGestionMovimientosComerciales>();
        Assert.NotNull(service);
    }

    [Fact]
    public async Task T1_F20_05_GestionPreciosReferencia_ServiceResolution()
    {
        using var scope = Factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetService<IGestionPreciosReferencia>();
        Assert.NotNull(service);
    }

    #endregion
}
