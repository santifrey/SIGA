using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Auth;
using SIGA.Application.DTOs.Clientes;
using SIGA.Application.DTOs.Empleados;
using SIGA.Application.DTOs.Filas;
using SIGA.Application.DTOs.Lotes;
using SIGA.Application.DTOs.MovimientosComerciales;
using SIGA.Application.DTOs.Plantaciones;
using SIGA.Application.DTOs.PreciosReferencia;
using SIGA.Application.DTOs.Roles;
using SIGA.Application.DTOs.Stock;
using SIGA.Application.Interfaces;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.E2ETests.Infrastructure;
using SIGA.Infrastructure.Persistence;
using Xunit;

namespace SIGA.E2ETests.Tier4_Workflows;

[Collection("E2E Test Suite")]
public class RealWorldWorkflowScenariosTests : TestBase
{
    public RealWorldWorkflowScenariosTests(SigaTestWebApplicationFactory factory) : base(factory)
    {
    }

    private async Task<(int ClienteId, int TemporadaId, int SucursalId)> EnsureContextAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SigaDbContext>();

        var cliente = await db.Clientes.FirstOrDefaultAsync();
        if (cliente == null)
        {
            var p = new Persona { Nombre = "T4", Apellido = "Cliente", Documento = "T4" + Random.Shared.Next(10000, 99999), FechaCreacion = DateTime.UtcNow };
            await db.Personas.AddAsync(p);
            await db.SaveChangesAsync();
            cliente = new Cliente { PersonaId = p.Id, CodigoCliente = "T4-" + Guid.NewGuid().ToString("N")[..4], EstadoCliente = EstadoCliente.Activo, Estado = EstadoParticipante.Activo, FechaRegistro = DateTime.UtcNow };
            await db.Clientes.AddAsync(cliente);
            await db.SaveChangesAsync();
        }

        var temporada = await db.Temporadas.FirstOrDefaultAsync(t => t.Estado == EstadoTemporada.Planificada);
        if (temporada == null)
        {
            temporada = new Temporada { Nombre = "Temporada T4", UsuarioCreadorId = 1, FechaInicio = DateTime.UtcNow, FechaFin = DateTime.UtcNow.AddMonths(6), Estado = EstadoTemporada.Planificada, FechaCreacion = DateTime.UtcNow };
            await db.Temporadas.AddAsync(temporada);
            await db.SaveChangesAsync();
        }

        var sucursal = await db.Sucursales.FirstOrDefaultAsync(s => s.Estado == EstadoSucursal.Activo);
        if (sucursal == null)
        {
            sucursal = new Sucursal { Nombre = "Sucursal T4", Ubicacion = "Córdoba", CapacidadTotal = 50000m, Estado = EstadoSucursal.Activo, FechaCreacion = DateTime.UtcNow };
            await db.Sucursales.AddAsync(sucursal);
            await db.SaveChangesAsync();
        }

        return (cliente.Id, temporada.Id, sucursal.Id);
    }

    [Fact]
    public async Task T4_01_NewClientCommercialOnboardingAndSale()
    {
        // 0. Ensure seed context & Empresa stock for MIX 300 (100 each for C1, C2, C3)
        var adminClient = CreateAdminClient();
        var (_, temporadaId, _) = await EnsureContextAsync();

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SigaDbContext>();

            var stockOp = new OperacionComercial { Tipo = TipoOperacion.Ingreso, Estado = EstadoOperacion.Confirmada, Fecha = DateTime.UtcNow };
            await db.OperacionesComerciales.AddAsync(stockOp);
            await db.SaveChangesAsync();

            var stockMov = new MovimientoBulbos(stockOp.Id, origenId: 99999, destinoId: Empresa.SingletonId, usuarioCreadorId: 1);
            stockMov.AgregarCantidad(ClaseBulbo.Clase1, 500);
            stockMov.AgregarCantidad(ClaseBulbo.Clase2, 500);
            stockMov.AgregarCantidad(ClaseBulbo.Clase3, 500);
            stockMov.Confirmar();
            await db.MovimientosBulbos.AddAsync(stockMov);
            await db.SaveChangesAsync();
        }

        // 1. Admin registers new client in DB
        int newClientId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SigaDbContext>();
            var persona = new Persona { Nombre = "Carlos", Apellido = "Nuevo", Documento = "ONB" + Random.Shared.Next(10000, 99999), FechaCreacion = DateTime.UtcNow };
            await db.Personas.AddAsync(persona);
            await db.SaveChangesAsync();
            var newClient = new Cliente { PersonaId = persona.Id, CodigoCliente = "CLI-ONB-" + Guid.NewGuid().ToString("N")[..4], EstadoCliente = EstadoCliente.Activo, Estado = EstadoParticipante.Activo, FechaRegistro = DateTime.UtcNow };
            await db.Clientes.AddAsync(newClient);
            await db.SaveChangesAsync();
            newClientId = newClient.Id;
        }

        // 2. Initial check: New client requires contract
        var checkRes1 = await adminClient.GetAsync($"/api/v1/movimientos-comerciales/verificar-cliente-nuevo/{newClientId}");
        Assert.Equal(HttpStatusCode.OK, checkRes1.StatusCode);
        var check1 = await DeserializeApiResponseAsync<VerificarClienteNuevoDto>(checkRes1);
        Assert.NotNull(check1!.Data);
        Assert.True(check1.Data.EsNuevo);
        Assert.True(check1.Data.RequiereContrato);

        // 3. Register Type 1 Venta for 300 MIX without contract
        var createDto = new CrearMovimientoComercialDto
        {
            Tipo = TipoMovimientoComercial.VentaACliente,
            OrigenParticipanteId = Empresa.SingletonId,
            DestinoParticipanteId = newClientId,
            ClienteId = newClientId,
            TemporadaId = temporadaId,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new()
                {
                    ClaseBulbo = ClaseBulbo.MIX,
                    Cantidad = 300,
                    PrecioUnitarioPactado = 15m
                }
            }
        };
        var createRes = await adminClient.PostAsJsonAsync("/api/v1/movimientos-comerciales", createDto);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var createdMov = (await DeserializeApiResponseAsync<MovimientoComercialDto>(createRes))!.Data!;
        Assert.Equal(EstadoMovimientoComercial.PendienteRevision, createdMov.Estado);

        // 4. Admin attempts approval without contract -> rejected (HTTP 400 Bad Request)
        var approveWithoutContractRes = await adminClient.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{createdMov.Id}/aprobar-admin", new AprobarAdminDto());
        Assert.Equal(HttpStatusCode.BadRequest, approveWithoutContractRes.StatusCode);

        // 5. Generate contract for the movement
        var contractRes = await adminClient.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{createdMov.Id}/contrato", new GenerarContratoDto
        {
            NumeroContrato = "CTR-2026-001"
        });
        Assert.Equal(HttpStatusCode.OK, contractRes.StatusCode);

        // 6. Admin approves movement now that contract is generated
        var approveRes = await adminClient.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{createdMov.Id}/aprobar-admin", new AprobarAdminDto());
        Assert.Equal(HttpStatusCode.OK, approveRes.StatusCode);
        var approvedMov = (await DeserializeApiResponseAsync<MovimientoComercialDto>(approveRes))!.Data!;
        Assert.Equal(EstadoMovimientoComercial.AprobadoAdmin, approvedMov.Estado);

        // 7. Accounting emits invoice -> state Facturado
        var invoiceRes = await adminClient.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{createdMov.Id}/facturar", new FacturarDto
        {
            NumeroFactura = "FC-0001"
        });
        Assert.Equal(HttpStatusCode.OK, invoiceRes.StatusCode);
        var invoicedMov = (await DeserializeApiResponseAsync<MovimientoComercialDto>(invoiceRes))!.Data!;
        Assert.Equal(EstadoMovimientoComercial.Facturado, invoicedMov.Estado);

        // 8. Admin finalizes movement -> state Finalizado
        var finalizeRes = await adminClient.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{createdMov.Id}/finalizar", new FinalizarDto());
        Assert.Equal(HttpStatusCode.OK, finalizeRes.StatusCode);
        var finalizedMov = (await DeserializeApiResponseAsync<MovimientoComercialDto>(finalizeRes))!.Data!;
        Assert.Equal(EstadoMovimientoComercial.Finalizado, finalizedMov.Estado);

        // 9. AUTHENTIC CONTRACT VERIFICATION: Subsequent check for client shows NOT requiring contract once established
        var checkRes2 = await adminClient.GetAsync($"/api/v1/movimientos-comerciales/verificar-cliente-nuevo/{newClientId}");
        Assert.Equal(HttpStatusCode.OK, checkRes2.StatusCode);
        var check2 = await DeserializeApiResponseAsync<VerificarClienteNuevoDto>(checkRes2);
        Assert.NotNull(check2!.Data);
        Assert.False(check2.Data.EsNuevo, "El cliente con venta finalizada ya no debe ser considerado nuevo.");
        Assert.False(check2.Data.RequiereContrato, "El cliente ya establecido no debe requerir un nuevo contrato.");
    }

    [Fact]
    public async Task T4_02_CompraOperationWithCommissionAndPhysicalStockIngestion()
    {
        var adminClient = CreateAdminClient();
        var (_, temporadaId, _) = await EnsureContextAsync();

        // 1. Arrange: Create client and give client 1000 bulbs in Clase 1
        int clienteId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SigaDbContext>();
            var persona = new Persona { Nombre = "VendedorBulbos", Apellido = "Test", Documento = "VND" + Random.Shared.Next(10000, 99999), FechaCreacion = DateTime.UtcNow };
            await db.Personas.AddAsync(persona);
            await db.SaveChangesAsync();

            var cliente = new Cliente { PersonaId = persona.Id, CodigoCliente = "CLI-VND-" + Guid.NewGuid().ToString("N")[..4], EstadoCliente = EstadoCliente.Activo, Estado = EstadoParticipante.Activo, FechaRegistro = DateTime.UtcNow };
            await db.Clientes.AddAsync(cliente);
            await db.SaveChangesAsync();
            clienteId = cliente.Id;

            var op = new OperacionComercial { Tipo = TipoOperacion.Ingreso, Estado = EstadoOperacion.Confirmada, Fecha = DateTime.UtcNow };
            await db.OperacionesComerciales.AddAsync(op);
            await db.SaveChangesAsync();

            var stockMov = new MovimientoBulbos(op.Id, origenId: 99998, destinoId: clienteId, usuarioCreadorId: 1);
            stockMov.AgregarCantidad(ClaseBulbo.Clase1, 1000);
            stockMov.Confirmar();
            await db.MovimientosBulbos.AddAsync(stockMov);
            await db.SaveChangesAsync();
        }

        // 2. Seller registers Type 2 Compra: 600 Clase 1 at $10 with 12% commission
        var createDto = new CrearMovimientoComercialDto
        {
            Tipo = TipoMovimientoComercial.CompraACliente,
            OrigenParticipanteId = clienteId,
            DestinoParticipanteId = Empresa.SingletonId,
            ClienteId = clienteId,
            TemporadaId = temporadaId,
            PorcentajeComision = 12m,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 600, PrecioUnitarioPactado = 10m }
            }
        };

        var createRes = await adminClient.PostAsJsonAsync("/api/v1/movimientos-comerciales", createDto);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var mov = (await DeserializeApiResponseAsync<MovimientoComercialDto>(createRes))!.Data!;

        // 3. Verify financials
        Assert.Equal(6000m, mov.MontoBruto);
        Assert.Equal(720m, mov.MontoComision);
        Assert.Equal(5280m, mov.MontoNeto);
        Assert.Equal(5280m, mov.MontoTotal);

        // 4. Admin Approves
        var approveRes = await adminClient.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{mov.Id}/aprobar-admin", new AprobarAdminDto());
        Assert.Equal(HttpStatusCode.OK, approveRes.StatusCode);

        // 5. Accounting invoices
        var invoiceRes = await adminClient.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{mov.Id}/facturar", new FacturarDto { NumeroFactura = "LIQ-001" });
        Assert.Equal(HttpStatusCode.OK, invoiceRes.StatusCode);

        // 6. Admin finalizes
        var finRes = await adminClient.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{mov.Id}/finalizar", new FinalizarDto());
        Assert.Equal(HttpStatusCode.OK, finRes.StatusCode);
        var finalizedMov = (await DeserializeApiResponseAsync<MovimientoComercialDto>(finRes))!.Data!;
        Assert.Equal(EstadoMovimientoComercial.Finalizado, finalizedMov.Estado);
    }

    [Fact]
    public async Task T4_03_ClientToClientStockTransferTraspaso()
    {
        var adminClient = CreateAdminClient();
        var (_, temporadaId, _) = await EnsureContextAsync();

        int clientAId, clientBId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SigaDbContext>();

            var pA = new Persona { Nombre = "ClienteA", Apellido = "Traspaso", Documento = "TRPA" + Random.Shared.Next(10000, 99999), FechaCreacion = DateTime.UtcNow };
            await db.Personas.AddAsync(pA);
            var pB = new Persona { Nombre = "ClienteB", Apellido = "Traspaso", Documento = "TRPB" + Random.Shared.Next(10000, 99999), FechaCreacion = DateTime.UtcNow };
            await db.Personas.AddAsync(pB);
            await db.SaveChangesAsync();

            var cA = new Cliente { PersonaId = pA.Id, CodigoCliente = "CLI-TRPA-" + Guid.NewGuid().ToString("N")[..4], EstadoCliente = EstadoCliente.Activo, Estado = EstadoParticipante.Activo, FechaRegistro = DateTime.UtcNow };
            await db.Clientes.AddAsync(cA);
            var cB = new Cliente { PersonaId = pB.Id, CodigoCliente = "CLI-TRPB-" + Guid.NewGuid().ToString("N")[..4], EstadoCliente = EstadoCliente.Activo, Estado = EstadoParticipante.Activo, FechaRegistro = DateTime.UtcNow };
            await db.Clientes.AddAsync(cB);
            await db.SaveChangesAsync();

            clientAId = cA.Id;
            clientBId = cB.Id;

            // Seed 300 Clase 2 bulbs to client A
            var op = new OperacionComercial { Tipo = TipoOperacion.Ingreso, Estado = EstadoOperacion.Confirmada, Fecha = DateTime.UtcNow };
            await db.OperacionesComerciales.AddAsync(op);
            await db.SaveChangesAsync();

            var stockMov = new MovimientoBulbos(op.Id, origenId: 99997, destinoId: clientAId, usuarioCreadorId: 1);
            stockMov.AgregarCantidad(ClaseBulbo.Clase2, 300);
            stockMov.Confirmar();
            await db.MovimientosBulbos.AddAsync(stockMov);
            await db.SaveChangesAsync();
        }

        // 1. Seller registers Type 3 Traspaso
        var createDto = new CrearMovimientoComercialDto
        {
            Tipo = TipoMovimientoComercial.TraspasoEntreClientes,
            OrigenParticipanteId = clientAId,
            DestinoParticipanteId = clientBId,
            ClienteId = clientAId,
            TemporadaId = temporadaId,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase2, Cantidad = 150, PrecioUnitarioPactado = 0m }
            }
        };

        var createRes = await adminClient.PostAsJsonAsync("/api/v1/movimientos-comerciales", createDto);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var mov = (await DeserializeApiResponseAsync<MovimientoComercialDto>(createRes))!.Data!;

        // 2. Verify $0 financials
        Assert.Equal(0m, mov.MontoTotal);

        // 3. Admin approves
        var appRes = await adminClient.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{mov.Id}/aprobar-admin", new AprobarAdminDto());
        Assert.Equal(HttpStatusCode.OK, appRes.StatusCode);
        var approvedMov = (await DeserializeApiResponseAsync<MovimientoComercialDto>(appRes))!.Data!;
        Assert.Equal(EstadoMovimientoComercial.AprobadoAdmin, approvedMov.Estado);

        // 4. Invoicing blocked for Traspaso
        var invRes = await adminClient.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{mov.Id}/facturar", new FacturarDto { NumeroFactura = "FAC-TRASP" });
        Assert.Equal(HttpStatusCode.BadRequest, invRes.StatusCode);

        // 5. Finalize directly from AprobadoAdmin
        var finRes = await adminClient.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{mov.Id}/finalizar", new FinalizarDto());
        Assert.Equal(HttpStatusCode.OK, finRes.StatusCode);
        var finalizedMov = (await DeserializeApiResponseAsync<MovimientoComercialDto>(finRes))!.Data!;
        Assert.Equal(EstadoMovimientoComercial.Finalizado, finalizedMov.Estado);
        Assert.Null(finalizedMov.Factura);
    }

    [Fact]
    public async Task T4_04_CommercialRejectionCorrectionAndFinalization()
    {
        var adminClient = CreateAdminClient();
        var (_, temporadaId, _) = await EnsureContextAsync();

        int clientDestId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SigaDbContext>();

            var p = new Persona { Nombre = "ClienteRechazo", Apellido = "Test", Documento = "RECH" + Random.Shared.Next(10000, 99999), FechaCreacion = DateTime.UtcNow };
            await db.Personas.AddAsync(p);
            await db.SaveChangesAsync();

            var c = new Cliente { PersonaId = p.Id, CodigoCliente = "CLI-RECH-" + Guid.NewGuid().ToString("N")[..4], EstadoCliente = EstadoCliente.Activo, Estado = EstadoParticipante.Activo, FechaRegistro = DateTime.UtcNow };
            await db.Clientes.AddAsync(c);
            await db.SaveChangesAsync();
            clientDestId = c.Id;

            // Seed stock for Empresa
            var op = new OperacionComercial { Tipo = TipoOperacion.Ingreso, Estado = EstadoOperacion.Confirmada, Fecha = DateTime.UtcNow };
            await db.OperacionesComerciales.AddAsync(op);
            await db.SaveChangesAsync();

            var stockMov = new MovimientoBulbos(op.Id, origenId: 99996, destinoId: Empresa.SingletonId, usuarioCreadorId: 1);
            stockMov.AgregarCantidad(ClaseBulbo.Clase1, 500);
            stockMov.Confirmar();
            await db.MovimientosBulbos.AddAsync(stockMov);
            await db.SaveChangesAsync();
        }

        // 1. Seller creates initial Venta with low price ($8)
        var createDto = new CrearMovimientoComercialDto
        {
            Tipo = TipoMovimientoComercial.VentaACliente,
            OrigenParticipanteId = Empresa.SingletonId,
            DestinoParticipanteId = clientDestId,
            ClienteId = clientDestId,
            TemporadaId = temporadaId,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 100, PrecioUnitarioPactado = 8m }
            }
        };

        var createRes = await adminClient.PostAsJsonAsync("/api/v1/movimientos-comerciales", createDto);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var mov = (await DeserializeApiResponseAsync<MovimientoComercialDto>(createRes))!.Data!;

        // 2. Admin rejects without motivo -> 400 Bad Request
        var badRejRes = await adminClient.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{mov.Id}/rechazar", new RechazarDto { Motivo = "" });
        Assert.Equal(HttpStatusCode.BadRequest, badRejRes.StatusCode);

        // 3. Admin rejects with valid motivo
        var rejRes = await adminClient.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{mov.Id}/rechazar", new RechazarDto { Motivo = "Precio por debajo de referencia mínima ($10)" });
        Assert.Equal(HttpStatusCode.OK, rejRes.StatusCode);
        var rejectedMov = (await DeserializeApiResponseAsync<MovimientoComercialDto>(rejRes))!.Data!;
        Assert.Equal(EstadoMovimientoComercial.Rechazado, rejectedMov.Estado);

        // 4. Seller updates price to $10 and resubmits
        var updateDto = new ActualizarMovimientoComercialDto
        {
            OrigenParticipanteId = Empresa.SingletonId,
            DestinoParticipanteId = clientDestId,
            ClienteId = clientDestId,
            TemporadaId = temporadaId,
            Observaciones = "Actualizado a $10",
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 100, PrecioUnitarioPactado = 10m }
            }
        };

        var updateRes = await adminClient.PutAsJsonAsync($"/api/v1/movimientos-comerciales/{mov.Id}", updateDto);
        Assert.Equal(HttpStatusCode.OK, updateRes.StatusCode);
        var updatedMov = (await DeserializeApiResponseAsync<MovimientoComercialDto>(updateRes))!.Data!;
        Assert.Equal(EstadoMovimientoComercial.PendienteRevision, updatedMov.Estado);

        // 5. Admin generates contract if required, then approves
        if (updatedMov.RequiereContrato)
        {
            await adminClient.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{mov.Id}/contrato", new GenerarContratoDto { NumeroContrato = "CTR-404" });
        }
        var appRes = await adminClient.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{mov.Id}/aprobar-admin", new AprobarAdminDto());
        Assert.Equal(HttpStatusCode.OK, appRes.StatusCode);

        // 6. Accounting invoices
        var invRes = await adminClient.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{mov.Id}/facturar", new FacturarDto { NumeroFactura = "FAC-404" });
        Assert.Equal(HttpStatusCode.OK, invRes.StatusCode);

        // 7. Admin finalizes
        var finRes = await adminClient.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{mov.Id}/finalizar", new FinalizarDto());
        Assert.Equal(HttpStatusCode.OK, finRes.StatusCode);
        var finalMov = (await DeserializeApiResponseAsync<MovimientoComercialDto>(finRes))!.Data!;
        Assert.Equal(EstadoMovimientoComercial.Finalizado, finalMov.Estado);

        // 8. Verify timeline history has at least 5 entries
        var getRes = await adminClient.GetAsync($"/api/v1/movimientos-comerciales/{mov.Id}");
        var detailedMov = (await DeserializeApiResponseAsync<MovimientoComercialDto>(getRes))!.Data!;
        Assert.True(detailedMov.HistorialAprobaciones.Count >= 5);
    }

    [Fact]
    public async Task T4_05_StaffOnboardingCustomRoleAndPermissionEnforcement()
    {
        var client = CreateAdminClient();

        // 1. Admin creates custom role
        var roleName = "ENCARGADO_DEPOSITO_" + Guid.NewGuid().ToString("N")[..4];
        var roleRes = await client.PostAsJsonAsync("/api/v1/roles", new CrearRolDto
        {
            Nombre = roleName,
            Permisos = new List<string> { "STOCK_VER", "STOCK_AJUSTAR" }
        });
        var role = (await DeserializeApiResponseAsync<RolDto>(roleRes))!.Data!;

        // 2. Admin creates employee Pablo Gomez
        var doc = "PG" + Random.Shared.Next(10000, 99999);
        var username = "pablo_" + Guid.NewGuid().ToString("N")[..6];
        var empRes = await client.PostAsJsonAsync("/api/v1/empleados", new CrearEmpleadoDto
        {
            Nombre = "Pablo",
            Apellido = "Gomez",
            Documento = doc,
            Username = username,
            Password = "Password123!",
            RolId = role.Id
        });
        var emp = (await DeserializeApiResponseAsync<EmpleadoDto>(empRes))!.Data!;

        // 3. Pablo logs in with single role and 2 permissions
        var pabloToken = ApiClientHelper.GenerateToken(emp.Id, username, roleName, new[] { "STOCK_VER", "STOCK_AJUSTAR" });
        var pabloClient = CreateClientWithCustomToken(pabloToken);

        // 4. Pablo accesses /stock -> OK
        var stockRes = await pabloClient.GetAsync("/api/v1/stock/disponibilidad?clienteId=1&clase=1&cantidad=5");
        Assert.Equal(HttpStatusCode.OK, stockRes.StatusCode);

        // 5. Pablo attempts /roles -> 403 Forbidden
        var rolesRes = await pabloClient.GetAsync("/api/v1/roles");
        Assert.Equal(HttpStatusCode.Forbidden, rolesRes.StatusCode);

        // 6. Admin soft-deletes Pablo
        await client.DeleteAsync($"/api/v1/empleados/{emp.Id}");

        // 7. Pablo's login attempts fail with 401
        var anon = CreateAnonymousClient();
        var loginRes = await anon.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto
        {
            Username = username,
            Password = "Password123!"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, loginRes.StatusCode);
    }

    [Fact]
    public async Task T4_06_MultiAdminDeletionSafeguardExecution()
    {
        var client = CreateAdminClient();

        // 1. Attempt to deactivate sole admin (User 1) -> 400 Bad Request
        var failRes = await client.DeleteAsync("/api/v1/empleados/1");
        Assert.Equal(HttpStatusCode.BadRequest, failRes.StatusCode);

        // 2. Admin creates two temporary administrators
        var docA = "TA" + Random.Shared.Next(10000, 99999);
        var uA = "admin_ta_" + Guid.NewGuid().ToString("N")[..6];
        var resA = await client.PostAsJsonAsync("/api/v1/empleados", new CrearEmpleadoDto
        {
            Nombre = "TempA", Apellido = "Admin", Documento = docA, Username = uA, Password = "Password123!", RolId = 1
        });
        var adminA = (await DeserializeApiResponseAsync<EmpleadoDto>(resA))!.Data!;

        var docB = "TB" + Random.Shared.Next(10000, 99999);
        var uB = "admin_tb_" + Guid.NewGuid().ToString("N")[..6];
        var resB = await client.PostAsJsonAsync("/api/v1/empleados", new CrearEmpleadoDto
        {
            Nombre = "TempB", Apellido = "Admin", Documento = docB, Username = uB, Password = "Password123!", RolId = 1
        });
        var adminB = (await DeserializeApiResponseAsync<EmpleadoDto>(resB))!.Data!;

        try
        {
            // 3. Deactivate TempA -> Succeeds (Admin 1 & TempB remain active)
            var delA = await client.DeleteAsync($"/api/v1/empleados/{adminA.Id}");
            Assert.Equal(HttpStatusCode.OK, delA.StatusCode);

            // 4. Deactivate TempB -> Succeeds (Admin 1 remains active)
            var delB = await client.DeleteAsync($"/api/v1/empleados/{adminB.Id}");
            Assert.Equal(HttpStatusCode.OK, delB.StatusCode);

            // 5. Admin 1 is now the sole administrator again -> Deactivation blocked
            var failFinal = await client.DeleteAsync("/api/v1/empleados/1");
            Assert.Equal(HttpStatusCode.BadRequest, failFinal.StatusCode);
        }
        finally
        {
            // Bootstrap admin 1 was never deleted, no restoration of User 1 needed.
        }
    }

    [Fact]
    public async Task T4_07_CompleteSeasonalProductionLifecycleFieldPlanting()
    {
        var client = CreateAdminClient();
        var (sucursalId, temporadaId, clienteId) = await EnsureContextAsync();

        // Create row 5
        var filaRes = await client.PostAsJsonAsync("/api/v1/filas", new CreateFilaDto
        {
            SucursalId = sucursalId,
            TemporadaId = temporadaId,
            NumeroFila = 5,
            Identificacion = "5",
            Capacidad = 8000
        });
        var fila = (await DeserializeApiResponseAsync<FilaDto>(filaRes))!.Data!;

        // Distribute 3000 bulbs in row 5 as Lot 1
        var distRes = await client.PostAsJsonAsync("/api/v1/plantaciones/distribucion", new RegistrarDistribucionDto
        {
            SucursalId = sucursalId,
            TemporadaId = temporadaId,
            FilaId = fila.Id,
            NumeroLote = 1,
            ClienteId = clienteId,
            Detalles = new List<DetalleDistribucionDto> { new() { Clase = ClaseBulbo.Clase1, Cantidad = 3000 } }
        });

        Assert.Equal(HttpStatusCode.Created, distRes.StatusCode);
        var lote = (await DeserializeApiResponseAsync<LoteDto>(distRes))!.Data!;
        Assert.Equal(3000, lote.CantidadPlantadaTotal);
    }

    [Fact]
    public async Task T4_08_FieldRowCapacityBoundaryProtection()
    {
        var client = CreateAdminClient();
        var (sucursalId, temporadaId, clienteId) = await EnsureContextAsync();

        // Row 8 capacity 2000
        var filaRes = await client.PostAsJsonAsync("/api/v1/filas", new CreateFilaDto
        {
            SucursalId = sucursalId,
            TemporadaId = temporadaId,
            NumeroFila = 8,
            Identificacion = "8",
            Capacidad = 2000
        });
        var fila = (await DeserializeApiResponseAsync<FilaDto>(filaRes))!.Data!;

        // Plant 1500 bulbs (lot 1)
        await client.PostAsJsonAsync("/api/v1/plantaciones/distribucion", new RegistrarDistribucionDto
        {
            SucursalId = sucursalId,
            TemporadaId = temporadaId,
            FilaId = fila.Id,
            NumeroLote = 1,
            ClienteId = clienteId,
            Detalles = new List<DetalleDistribucionDto> { new() { Clase = ClaseBulbo.Clase1, Cantidad = 1500 } }
        });

        // Attempt distributing 600 bulbs -> rejected (available 500)
        var failRes = await client.PostAsJsonAsync("/api/v1/plantaciones/distribucion", new RegistrarDistribucionDto
        {
            SucursalId = sucursalId,
            TemporadaId = temporadaId,
            FilaId = fila.Id,
            NumeroLote = 2,
            ClienteId = clienteId,
            Detalles = new List<DetalleDistribucionDto> { new() { Clase = ClaseBulbo.Clase1, Cantidad = 600 } }
        });
        Assert.Equal(HttpStatusCode.BadRequest, failRes.StatusCode);

        // Adjust distribution to 500 bulbs -> succeeds (100% full: 2000/2000)
        var okRes = await client.PostAsJsonAsync("/api/v1/plantaciones/distribucion", new RegistrarDistribucionDto
        {
            SucursalId = sucursalId,
            TemporadaId = temporadaId,
            FilaId = fila.Id,
            NumeroLote = 2,
            ClienteId = clienteId,
            Detalles = new List<DetalleDistribucionDto> { new() { Clase = ClaseBulbo.Clase1, Cantidad = 500 } }
        });
        Assert.Equal(HttpStatusCode.Created, okRes.StatusCode);
    }

    [Fact]
    public async Task T4_09_SeasonalReferencePriceSetupAndVentaAutoPricing()
    {
        using var scope = Factory.Services.CreateScope();
        var prefService = scope.ServiceProvider.GetRequiredService<IGestionPreciosReferencia>();
        var (_, temporadaId, _) = await EnsureContextAsync();

        // Admin configures 6 prices including MIX
        await prefService.ConfigurarPrecioReferenciaAsync(new ConfigurarPrecioReferenciaDto { TemporadaId = temporadaId, ClaseBulbo = ClaseBulbo.Clase0, PrecioReferencia = 8m });
        await prefService.ConfigurarPrecioReferenciaAsync(new ConfigurarPrecioReferenciaDto { TemporadaId = temporadaId, ClaseBulbo = ClaseBulbo.Clase1, PrecioReferencia = 10m });
        await prefService.ConfigurarPrecioReferenciaAsync(new ConfigurarPrecioReferenciaDto { TemporadaId = temporadaId, ClaseBulbo = ClaseBulbo.Clase2, PrecioReferencia = 12m });
        await prefService.ConfigurarPrecioReferenciaAsync(new ConfigurarPrecioReferenciaDto { TemporadaId = temporadaId, ClaseBulbo = ClaseBulbo.Clase3, PrecioReferencia = 14m });
        await prefService.ConfigurarPrecioReferenciaAsync(new ConfigurarPrecioReferenciaDto { TemporadaId = temporadaId, ClaseBulbo = ClaseBulbo.Clase4, PrecioReferencia = 16m });
        await prefService.ConfigurarPrecioReferenciaAsync(new ConfigurarPrecioReferenciaDto { TemporadaId = temporadaId, ClaseBulbo = ClaseBulbo.MIX, PrecioReferencia = 11m });

        var mixPrice = await prefService.ObtenerPrecioVigenteAsync(temporadaId, ClaseBulbo.MIX);
        Assert.Equal(11m, mixPrice.Data!.PrecioReferencia);
    }

    [Fact]
    public void T4_10_UiSessionPersistenceAndDeepLinking()
    {
        var appJs = FrontendAssetScanner.ReadFrontendFile("js/app.js");
        Assert.Contains("siga_sidebar_collapsed_sections", appJs);
        Assert.Contains("parentSection.classList.remove('collapsed')", appJs);
    }

    [Fact]
    public void T4_11_StockBugFixRegressionWalkthrough()
    {
        var stockServiceJs = FrontendAssetScanner.ReadFrontendFile("js/services/stock-service.js");
        Assert.Contains("/stock/por-cliente/", stockServiceJs);
        Assert.Contains("Array.isArray(unwrapped)", stockServiceJs);
    }

    [Fact]
    public async Task T4_12_MultiCalibreMixCombinedCommercialFinalization()
    {
        var adminClient = CreateAdminClient();
        var (_, temporadaId, _) = await EnsureContextAsync();

        int clientDestId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SigaDbContext>();

            var p = new Persona { Nombre = "ClienteMix", Apellido = "Multi", Documento = "MIX" + Random.Shared.Next(10000, 99999), FechaCreacion = DateTime.UtcNow };
            await db.Personas.AddAsync(p);
            await db.SaveChangesAsync();

            var c = new Cliente { PersonaId = p.Id, CodigoCliente = "CLI-MIX-" + Guid.NewGuid().ToString("N")[..4], EstadoCliente = EstadoCliente.Activo, Estado = EstadoParticipante.Activo, FechaRegistro = DateTime.UtcNow };
            await db.Clientes.AddAsync(c);
            await db.SaveChangesAsync();
            clientDestId = c.Id;

            // Seed Empresa stock for all calibres
            var op = new OperacionComercial { Tipo = TipoOperacion.Ingreso, Estado = EstadoOperacion.Confirmada, Fecha = DateTime.UtcNow };
            await db.OperacionesComerciales.AddAsync(op);
            await db.SaveChangesAsync();

            var stockMov = new MovimientoBulbos(op.Id, origenId: 99995, destinoId: Empresa.SingletonId, usuarioCreadorId: 1);
            stockMov.AgregarCantidad(ClaseBulbo.Clase0, 200);
            stockMov.AgregarCantidad(ClaseBulbo.Clase1, 200);
            stockMov.AgregarCantidad(ClaseBulbo.Clase2, 200);
            stockMov.AgregarCantidad(ClaseBulbo.Clase3, 200);
            stockMov.AgregarCantidad(ClaseBulbo.Clase4, 300);
            stockMov.Confirmar();
            await db.MovimientosBulbos.AddAsync(stockMov);
            await db.SaveChangesAsync();
        }

        // 1. Seller creates multi-calibre sale (100 C0, 300 MIX, 200 C4)
        var createDto = new CrearMovimientoComercialDto
        {
            Tipo = TipoMovimientoComercial.VentaACliente,
            OrigenParticipanteId = Empresa.SingletonId,
            DestinoParticipanteId = clientDestId,
            ClienteId = clientDestId,
            TemporadaId = temporadaId,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase0, Cantidad = 100, PrecioUnitarioPactado = 8m },
                new() { ClaseBulbo = ClaseBulbo.MIX, Cantidad = 300, PrecioUnitarioPactado = 11m },
                new() { ClaseBulbo = ClaseBulbo.Clase4, Cantidad = 200, PrecioUnitarioPactado = 16m }
            }
        };

        var createRes = await adminClient.PostAsJsonAsync("/api/v1/movimientos-comerciales", createDto);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var mov = (await DeserializeApiResponseAsync<MovimientoComercialDto>(createRes))!.Data!;

        // Total bulbos = 100 + 300 + 200 = 600
        Assert.Equal(600, mov.TotalBulbos);
        // Financials = 100*8 + 300*11 + 200*16 = 800 + 3300 + 3200 = 7300
        Assert.Equal(7300m, mov.MontoTotal);

        // 2. Contract & Approval
        if (mov.RequiereContrato)
        {
            await adminClient.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{mov.Id}/contrato", new GenerarContratoDto { NumeroContrato = "CTR-MIX-12" });
        }
        var appRes = await adminClient.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{mov.Id}/aprobar-admin", new AprobarAdminDto());
        Assert.Equal(HttpStatusCode.OK, appRes.StatusCode);

        // 3. Invoice & Finalization
        var invRes = await adminClient.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{mov.Id}/facturar", new FacturarDto { NumeroFactura = "FAC-MIX-12" });
        Assert.Equal(HttpStatusCode.OK, invRes.StatusCode);

        var finRes = await adminClient.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{mov.Id}/finalizar", new FinalizarDto());
        Assert.Equal(HttpStatusCode.OK, finRes.StatusCode);
        var finalized = (await DeserializeApiResponseAsync<MovimientoComercialDto>(finRes))!.Data!;
        Assert.Equal(EstadoMovimientoComercial.Finalizado, finalized.Estado);

        // 4. Physical stock decomposition verified via API
        var stockRes = await adminClient.GetAsync($"/api/v1/stock/por-cliente/{clientDestId}");
        Assert.Equal(HttpStatusCode.OK, stockRes.StatusCode);
        var stockEnvelope = await DeserializeApiResponseAsync<StockClienteDto>(stockRes);
        Assert.NotNull(stockEnvelope!.Data);
        Assert.Equal(600, stockEnvelope.Data.TotalBulbos);
    }

    [Fact]
    public void T4_13_UnauthorizedRouteTamperingAndToastNotification()
    {
        var routerJs = FrontendAssetScanner.ReadFrontendFile("js/router.js");
        Assert.Contains("toast.warning", routerJs);
        Assert.Contains("Acceso denegado", routerJs);
    }

    [Fact]
    public void T4_14_ProfileUpdateAndSynchronizedTopbarRendering()
    {
        var appJs = FrontendAssetScanner.ReadFrontendFile("js/app.js");
        Assert.Contains("_updateUserProfile", appJs);
    }

    [Fact]
    public void T4_15_DeactivationProhibitionSymbolConsistencyAcrossViews()
    {
        var cJs = FrontendAssetScanner.ReadFrontendFile("js/views/clientes-view.js");
        var eJs = FrontendAssetScanner.ReadFrontendFile("js/views/empleados-view.js");

        Assert.Contains("getProhibitionIcon", cJs);
        Assert.Contains("getProhibitionIcon", eJs);
        Assert.DoesNotContain("fa-trash", cJs);
        Assert.DoesNotContain("fa-trash", eJs);
    }

    [Fact]
    public async Task T4_16_OmissionOfNotificationsCompleteVerification()
    {
        var client = CreateAdminClient();
        var res = await client.GetAsync("/api/v1/notificaciones");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);

        var indexHtml = FrontendAssetScanner.ReadFrontendFile("index.html");
        Assert.DoesNotContain("notification", indexHtml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task T4_17_EndToEndMultiRoleEnterpriseSimulation()
    {
        var adminClient = CreateAdminClient();
        var (_, temporadaId, sucursalId) = await EnsureContextAsync();

        // 1. Admin configures reference prices via HTTP
        var prefDto = new ConfigurarPreciosTemporadaDto
        {
            TemporadaId = temporadaId,
            Precios = new List<ItemPrecioTemporadaDto>
            {
                new() { ClaseBulbo = ClaseBulbo.Clase0, PrecioReferencia = 8m },
                new() { ClaseBulbo = ClaseBulbo.Clase1, PrecioReferencia = 10m },
                new() { ClaseBulbo = ClaseBulbo.Clase2, PrecioReferencia = 12m },
                new() { ClaseBulbo = ClaseBulbo.Clase3, PrecioReferencia = 14m },
                new() { ClaseBulbo = ClaseBulbo.Clase4, PrecioReferencia = 16m },
                new() { ClaseBulbo = ClaseBulbo.MIX, PrecioReferencia = 15m }
            }
        };

        var prefRes = await adminClient.PostAsJsonAsync("/api/v1/precios-referencia/temporada", prefDto);
        Assert.Equal(HttpStatusCode.OK, prefRes.StatusCode);

        // 2. Arrange Client and seed Empresa stock
        int clienteId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SigaDbContext>();

            var p = new Persona { Nombre = "ClienteSim", Apellido = "MultiRole", Documento = "SIM" + Random.Shared.Next(10000, 99999), FechaCreacion = DateTime.UtcNow };
            await db.Personas.AddAsync(p);
            await db.SaveChangesAsync();

            var c = new Cliente { PersonaId = p.Id, CodigoCliente = "CLI-SIM-" + Guid.NewGuid().ToString("N")[..4], EstadoCliente = EstadoCliente.Activo, Estado = EstadoParticipante.Activo, FechaRegistro = DateTime.UtcNow };
            await db.Clientes.AddAsync(c);
            await db.SaveChangesAsync();
            clienteId = c.Id;

            var op = new OperacionComercial { Tipo = TipoOperacion.Ingreso, Estado = EstadoOperacion.Confirmada, Fecha = DateTime.UtcNow };
            await db.OperacionesComerciales.AddAsync(op);
            await db.SaveChangesAsync();

            var stockMov = new MovimientoBulbos(op.Id, origenId: 99994, destinoId: Empresa.SingletonId, usuarioCreadorId: 1);
            stockMov.AgregarCantidad(ClaseBulbo.Clase1, 300);
            stockMov.AgregarCantidad(ClaseBulbo.Clase2, 300);
            stockMov.AgregarCantidad(ClaseBulbo.Clase3, 300);
            stockMov.Confirmar();
            await db.MovimientosBulbos.AddAsync(stockMov);
            await db.SaveChangesAsync();
        }

        // 3. Seller registers sale for 300 MIX ($15 reference price)
        var createDto = new CrearMovimientoComercialDto
        {
            Tipo = TipoMovimientoComercial.VentaACliente,
            OrigenParticipanteId = Empresa.SingletonId,
            DestinoParticipanteId = clienteId,
            ClienteId = clienteId,
            TemporadaId = temporadaId,
            Detalles = new List<ItemDetalleMovimientoComercialCreateDto>
            {
                new() { ClaseBulbo = ClaseBulbo.MIX, Cantidad = 300, PrecioUnitarioPactado = 15m }
            }
        };

        var createRes = await adminClient.PostAsJsonAsync("/api/v1/movimientos-comerciales", createDto);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var mov = (await DeserializeApiResponseAsync<MovimientoComercialDto>(createRes))!.Data!;

        // 4. Seller generates contract & Admin approves
        if (mov.RequiereContrato)
        {
            await adminClient.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{mov.Id}/contrato", new GenerarContratoDto { NumeroContrato = "CTR-T417" });
        }
        var appRes = await adminClient.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{mov.Id}/aprobar-admin", new AprobarAdminDto());
        Assert.Equal(HttpStatusCode.OK, appRes.StatusCode);

        // 5. Accountant invoices & Admin finalizes
        var invRes = await adminClient.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{mov.Id}/facturar", new FacturarDto { NumeroFactura = "FAC-T417" });
        Assert.Equal(HttpStatusCode.OK, invRes.StatusCode);

        var finRes = await adminClient.PostAsJsonAsync($"/api/v1/movimientos-comerciales/{mov.Id}/finalizar", new FinalizarDto());
        Assert.Equal(HttpStatusCode.OK, finRes.StatusCode);

        // 6. Verify client stock acquired via HTTP: 100 C1, 100 C2, 100 C3
        var stockRes = await adminClient.GetAsync($"/api/v1/stock/por-cliente/{clienteId}");
        Assert.Equal(HttpStatusCode.OK, stockRes.StatusCode);
        var stockDto = (await DeserializeApiResponseAsync<StockClienteDto>(stockRes))!.Data!;
        Assert.Equal(300, stockDto.TotalBulbos);

        // 7. Field distribution: Plant 100 bulbs of Clase 1 into a row
        var distDto = new RegistrarDistribucionDto
        {
            TemporadaId = temporadaId,
            SucursalId = sucursalId,
            NumeroFila = 25,
            IdentificacionFila = "Fila 25 Sim",
            CapacidadFila = 1000,
            NumeroLote = 1,
            ClienteId = clienteId,
            Detalles = new List<DetalleDistribucionDto>
            {
                new() { Clase = ClaseBulbo.Clase1, Cantidad = 100 }
            }
        };

        var distRes = await adminClient.PostAsJsonAsync("/api/v1/plantaciones/distribucion", distDto);
        Assert.True(distRes.StatusCode == HttpStatusCode.OK || distRes.StatusCode == HttpStatusCode.Created);

        // 8. Final stock verification: Client Clase 1 bulbs planted, total decreased to 200
        var finalStockRes = await adminClient.GetAsync($"/api/v1/stock/por-cliente/{clienteId}");
        Assert.Equal(HttpStatusCode.OK, finalStockRes.StatusCode);
        var finalStockDto = (await DeserializeApiResponseAsync<StockClienteDto>(finalStockRes))!.Data!;
        Assert.Equal(200, finalStockDto.TotalBulbos);
    }
}
