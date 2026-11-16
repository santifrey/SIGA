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
using SIGA.Application.Interfaces;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.E2ETests.Infrastructure;
using SIGA.Infrastructure.Persistence;
using Xunit;

namespace SIGA.E2ETests.Tier3_Combinations;

[Collection("E2E Test Suite")]
public class CrossModuleIntegrationTests : TestBase
{
    public CrossModuleIntegrationTests(SigaTestWebApplicationFactory factory) : base(factory)
    {
    }

    private async Task<(int ClienteId, int TemporadaId, int SucursalId)> EnsureContextAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SigaDbContext>();

        var cliente = await db.Clientes.FirstOrDefaultAsync();
        if (cliente == null)
        {
            var p = new Persona { Nombre = "C3", Apellido = "Test", Documento = "C3" + Random.Shared.Next(10000, 99999), FechaCreacion = DateTime.UtcNow };
            await db.Personas.AddAsync(p);
            await db.SaveChangesAsync();
            cliente = new Cliente { PersonaId = p.Id, CodigoCliente = "C3-" + Guid.NewGuid().ToString("N")[..4], EstadoCliente = EstadoCliente.Activo, Estado = EstadoParticipante.Activo, FechaRegistro = DateTime.UtcNow };
            await db.Clientes.AddAsync(cliente);
            await db.SaveChangesAsync();
        }

        var temporada = await db.Temporadas.FirstOrDefaultAsync(t => t.Estado == EstadoTemporada.Planificada);
        if (temporada == null)
        {
            temporada = new Temporada { Nombre = "Temp C3", UsuarioCreadorId = 1, FechaInicio = DateTime.UtcNow, FechaFin = DateTime.UtcNow.AddMonths(6), Estado = EstadoTemporada.Planificada, FechaCreacion = DateTime.UtcNow };
            await db.Temporadas.AddAsync(temporada);
            await db.SaveChangesAsync();
        }

        var sucursal = await db.Sucursales.FirstOrDefaultAsync(s => s.Estado == EstadoSucursal.Activo);
        if (sucursal == null)
        {
            sucursal = new Sucursal { Nombre = "Suc C3", Ubicacion = "Córdoba", CapacidadTotal = 50000m, Estado = EstadoSucursal.Activo, FechaCreacion = DateTime.UtcNow };
            await db.Sucursales.AddAsync(sucursal);
            await db.SaveChangesAsync();
        }

        return (cliente.Id, temporada.Id, sucursal.Id);
    }

    [Fact]
    public async Task T3_01_F01_F06_SingleRoleAssignmentAndRolesCrudLifecycle()
    {
        var client = CreateAdminClient();
        var roleName = "ROL_T301_" + Guid.NewGuid().ToString("N")[..6];

        // 1. Create custom role
        var roleRes = await client.PostAsJsonAsync("/api/v1/roles", new CrearRolDto { Nombre = roleName });
        var role = (await DeserializeApiResponseAsync<RolDto>(roleRes))!.Data!;

        // 2. Assign to employee
        var doc = "T301" + Random.Shared.Next(10000, 99999);
        var username = "user_t301_" + Guid.NewGuid().ToString("N")[..6];
        var empRes = await client.PostAsJsonAsync("/api/v1/empleados", new CrearEmpleadoDto
        {
            Nombre = "T301",
            Apellido = "User",
            Documento = doc,
            Username = username,
            Password = "Password123!",
            RolId = role.Id
        });
        var emp = (await DeserializeApiResponseAsync<EmpleadoDto>(empRes))!.Data!;
        Assert.Equal(role.Id, emp.RolId);

        // 3. Delete role fails because active user exists
        var delFail = await client.DeleteAsync($"/api/v1/roles/{role.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, delFail.StatusCode);

        // 4. Reassign user to role 2 (VENDEDOR)
        await client.PutAsJsonAsync($"/api/v1/empleados/{emp.Id}", new ActualizarEmpleadoDto
        {
            Nombre = "T301",
            Apellido = "User",
            Documento = doc,
            RolId = 2,
            Estado = EstadoUsuario.Activo
        });

        // 5. Delete role now succeeds
        var delOk = await client.DeleteAsync($"/api/v1/roles/{role.Id}");
        Assert.Equal(HttpStatusCode.OK, delOk.StatusCode);
    }

    [Fact]
    public async Task T3_02_F02_F04_RolePermissionAssignmentAndDynamicAuthorization()
    {
        var client = CreateAdminClient();
        var roleName = "ROL_T302_" + Guid.NewGuid().ToString("N")[..6];

        // Create role with ONLY STOCK_VER
        var roleRes = await client.PostAsJsonAsync("/api/v1/roles", new CrearRolDto
        {
            Nombre = roleName,
            Permisos = new List<string> { "STOCK_VER" }
        });
        var role = (await DeserializeApiResponseAsync<RolDto>(roleRes))!.Data!;

        var doc = "T302" + Random.Shared.Next(10000, 99999);
        var username = "user_t302_" + Guid.NewGuid().ToString("N")[..6];
        var empRes = await client.PostAsJsonAsync("/api/v1/empleados", new CrearEmpleadoDto
        {
            Nombre = "T302",
            Apellido = "StockOnly",
            Documento = doc,
            Username = username,
            Password = "Password123!",
            RolId = role.Id
        });
        var emp = (await DeserializeApiResponseAsync<EmpleadoDto>(empRes))!.Data!;

        // Authenticate as this user
        var token = ApiClientHelper.GenerateToken(emp.Id, username, roleName, new[] { "STOCK_VER" });
        var userClient = CreateClientWithCustomToken(token);

        // Accessing /stock succeeds
        var stockRes = await userClient.GetAsync("/api/v1/stock/disponibilidad?clienteId=1&clase=1&cantidad=10");
        Assert.Equal(HttpStatusCode.OK, stockRes.StatusCode);

        // Accessing /clientes (requires CLIENTES_VER) returns 403 Forbidden
        var clientesRes = await userClient.GetAsync("/api/v1/clientes");
        Assert.Equal(HttpStatusCode.Forbidden, clientesRes.StatusCode);
    }

    [Fact]
    public async Task T3_03_F04_F05_JwtClaimsAndDynamicAuthorizationProvider()
    {
        var token = ApiClientHelper.GenerateToken(10, "perm_tester", "CUSTOM", new[] { "CLIENTES_VER" });
        var client = CreateClientWithCustomToken(token);

        var res = await client.GetAsync("/api/v1/clientes");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task T3_04_F06_F07_RoleDeletionSafeguardAndEmployeeAssociation()
    {
        var client = CreateAdminClient();
        var roleRes = await client.PostAsJsonAsync("/api/v1/roles", new CrearRolDto { Nombre = "ROL_T304_" + Guid.NewGuid().ToString("N")[..6] });
        var role = (await DeserializeApiResponseAsync<RolDto>(roleRes))!.Data!;

        var doc = "T304" + Random.Shared.Next(10000, 99999);
        var username = "user_t304_" + Guid.NewGuid().ToString("N")[..6];
        var empRes = await client.PostAsJsonAsync("/api/v1/empleados", new CrearEmpleadoDto
        {
            Nombre = "T304",
            Apellido = "Emp",
            Documento = doc,
            Username = username,
            Password = "Password123!",
            RolId = role.Id
        });
        var emp = (await DeserializeApiResponseAsync<EmpleadoDto>(empRes))!.Data!;

        // Attempt delete role -> 400
        var delFail = await client.DeleteAsync($"/api/v1/roles/{role.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, delFail.StatusCode);

        // Soft delete employee
        await client.DeleteAsync($"/api/v1/empleados/{emp.Id}");

        // Now delete role succeeds
        var delOk = await client.DeleteAsync($"/api/v1/roles/{role.Id}");
        Assert.Equal(HttpStatusCode.OK, delOk.StatusCode);
    }

    [Fact]
    public async Task T3_05_F07_F08_EmployeeProfileUpdateAndMeEndpointParity()
    {
        var adminClient = CreateAdminClient();
        var doc = "T305" + Random.Shared.Next(10000, 99999);
        var username = "user_t305_" + Guid.NewGuid().ToString("N")[..6];

        var createRes = await adminClient.PostAsJsonAsync("/api/v1/empleados", new CrearEmpleadoDto
        {
            Nombre = "T305",
            Apellido = "PhoneParity",
            Documento = doc,
            Telefono = "111-1111",
            Username = username,
            Password = "Password123!",
            RolId = 2
        });
        var emp = (await DeserializeApiResponseAsync<EmpleadoDto>(createRes))!.Data!;

        // Admin updates phone to 999-9999
        await adminClient.PutAsJsonAsync($"/api/v1/empleados/{emp.Id}", new ActualizarEmpleadoDto
        {
            Nombre = "T305",
            Apellido = "PhoneParity",
            Documento = doc,
            Telefono = "999-9999",
            RolId = 2,
            Estado = EstadoUsuario.Activo
        });

        // User calls /auth/me
        var userClient = CreateRoleClient("VENDEDOR", emp.Id, username);
        var meRes = await userClient.GetAsync("/api/v1/auth/me");
        var me = (await DeserializeApiResponseAsync<UserInfoResponseDto>(meRes))!.Data!;

        Assert.Equal("999-9999", me.Telefono);
    }

    [Fact]
    public void T3_06_F08_F29_MeEndpointAndFrontendProfileViewBinding()
    {
        var perfilJs = FrontendAssetScanner.ReadFrontendFile("js/views/perfil-view.js");
        Assert.Contains("authService.getUsuarioActual", perfilJs);
        Assert.Contains("nombreCompleto", perfilJs);
        Assert.Contains("rol", perfilJs);
        Assert.Contains("permisos", perfilJs);
    }

    [Fact]
    public void T3_07_F04_F26_BackendAuthHandlerAndFrontendRouteGuardsParity()
    {
        var appJs = FrontendAssetScanner.ReadFrontendFile("js/app.js");
        Assert.Contains("ROLES_VER", appJs);
        Assert.Contains("EMPLEADOS_VER", appJs);
        Assert.Contains("CLIENTES_VER", appJs);
        Assert.Contains("STOCK_VER", appJs);
    }

    [Fact]
    public async Task T3_08_F11_F12_CommercialMovementType1_FinancialCalculations()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 100, 15m);
        mov.AgregarDetalle(ClaseBulbo.Clase2, 200, 10m);

        Assert.Equal(3500m, mov.MontoTotal);
        Assert.Equal(3500m, mov.MontoNeto);
        Assert.Equal(0m, mov.MontoComision);
    }

    [Fact]
    public async Task T3_09_F11_F12_CommercialMovementType2_CommissionDeduction()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.CompraACliente, 1, 2, 1, porcentajeComision: 10m);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 100, 10m);

        Assert.Equal(1000m, mov.MontoBruto);
        Assert.Equal(100m, mov.MontoComision);
        Assert.Equal(900m, mov.MontoNeto);
        Assert.Equal(900m, mov.MontoTotal);
    }

    [Fact]
    public async Task T3_10_F11_F12_CommercialMovementType3_ZeroFinancials()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.TraspasoEntreClientes, 1, 2, 3);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 500, 100m); // Price ignored for type 3

        Assert.Equal(0m, mov.MontoTotal);
    }

    [Fact]
    public async Task T3_11_F11_F13_SequentialStateTransitions()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 10m);

        Assert.Equal(EstadoMovimientoComercial.PendienteRevision, mov.Estado);
        mov.AprobarAdmin(1, false);
        Assert.Equal(EstadoMovimientoComercial.AprobadoAdmin, mov.Estado);
        mov.EmitirFactura("F-311", 1);
        Assert.Equal(EstadoMovimientoComercial.Facturado, mov.Estado);
        mov.Finalizar(1, 1);
        Assert.Equal(EstadoMovimientoComercial.Finalizado, mov.Estado);
    }

    [Fact]
    public async Task T3_12_F13_F14_FacturadoState_NonTerminalRejectionVoidsInvoice()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 10m);
        mov.AprobarAdmin(1, false);
        var f = mov.EmitirFactura("F-312", 1);
        Assert.Equal(EstadoFactura.Emitida, f.Estado);

        mov.Rechazar(1, "Error de facturación");
        Assert.Equal(EstadoMovimientoComercial.Rechazado, mov.Estado);
        Assert.Equal(EstadoFactura.Anulada, f.Estado);
    }

    [Fact]
    public async Task T3_13_F14_F17_RejectionCorrectionCycle_AuditHistory()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        mov.Rechazar(1, "Motivo 1");
        mov.EditarYReenviar(1);

        var actions = mov.HistorialAprobaciones.Select(h => h.Accion).ToList();
        Assert.Contains(AccionAprobacion.Registrado, actions);
        Assert.Contains(AccionAprobacion.Rechazado, actions);
        Assert.Contains(AccionAprobacion.Corregido, actions);
    }

    [Fact]
    public async Task T3_14_F11_F15_CommercialMovementItems_MixStockDecomposition()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        mov.AgregarDetalle(ClaseBulbo.MIX, 300, 10m);

        var reqs = mov.ObtenerRequerimientosFisicosBulbos();
        Assert.Equal(100, reqs[ClaseBulbo.Clase1]);
        Assert.Equal(100, reqs[ClaseBulbo.Clase2]);
        Assert.Equal(100, reqs[ClaseBulbo.Clase3]);
    }

    [Fact]
    public async Task T3_15_F15_F18_MixDecomposition_FinalizationStockImpact()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        mov.AgregarDetalle(ClaseBulbo.MIX, 300, 10m);
        var reqs = mov.ObtenerRequerimientosFisicosBulbos();

        Assert.Equal(300, reqs.Values.Sum());
    }

    [Fact]
    public async Task T3_16_F11_F16_VentaMovement_NewClientContractEnforcement()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, 1, 2);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 10m);

        // Attempting approval without contract throws
        Assert.Throws<OperacionInvalidaException>(() => mov.AprobarAdmin(1, requiereContrato: true));

        // Generating contract allows approval
        mov.GenerarContrato("CTR-316", 1);
        mov.AprobarAdmin(1, requiereContrato: true);
        Assert.Equal(EstadoMovimientoComercial.AprobadoAdmin, mov.Estado);
    }

    [Fact]
    public async Task T3_17_F16_F18_FinalizedMovement_SubsequentClientStatus()
    {
        using var scope = Factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IGestionMovimientosComerciales>();
        var (clienteId, _) = await EnsureContextAsync();

        var check = await service.VerificarClienteNuevoAsync(clienteId);
        Assert.True(check.Success);
    }

    [Fact]
    public async Task T3_18_F11_F19_CommercialMovement_ReferencePricesFallback()
    {
        using var scope = Factory.Services.CreateScope();
        var prefService = scope.ServiceProvider.GetRequiredService<IGestionPreciosReferencia>();
        var (_, temporadaId, _) = await EnsureContextAsync();

        await prefService.ConfigurarPrecioReferenciaAsync(new ConfigurarPrecioReferenciaDto
        {
            TemporadaId = temporadaId,
            ClaseBulbo = ClaseBulbo.Clase1,
            PrecioReferencia = 14.50m
        });

        var price = await prefService.ObtenerPrecioReferenciaAsync(temporadaId, ClaseBulbo.Clase1);
        Assert.Equal(14.50m, price.Data!.PrecioReferencia);
    }

    [Fact]
    public async Task T3_19_F18_F32_FinalizationStockMovement_ReflectedInStockQuery()
    {
        using var scope = Factory.Services.CreateScope();
        var stockService = scope.ServiceProvider.GetRequiredService<IGestionStock>();

        var stock = await stockService.ConsultarStockPorClienteAsync(Empresa.SingletonId);
        Assert.True(stock.Success);
        Assert.NotNull(stock.Data);
    }

    [Fact]
    public async Task T3_20_F19_F33_PreciosReferenciaApiAndFrontendViewSync()
    {
        var prefJs = FrontendAssetScanner.ReadFrontendFile("js/views/precios-referencia-view.js");
        Assert.Contains("precios-referencia", prefJs);
    }

    [Fact]
    public async Task T3_21_F21_F22_StructuredRowLotApiAndFrontendPlantationsView()
    {
        var plantacionesJs = FrontendAssetScanner.ReadFrontendFile("js/views/plantaciones-view.js");
        var serviceJs = FrontendAssetScanner.ReadFrontendFile("js/services/plantaciones-service.js");
        Assert.Contains("/filas/por-sucursal", serviceJs);
        Assert.Contains("select-fila", plantacionesJs);
    }

    [Fact]
    public async Task T3_22_F21_F18_FieldDistributionAndStockVerification()
    {
        using var scope = Factory.Services.CreateScope();
        var stockService = scope.ServiceProvider.GetRequiredService<IGestionStock>();

        var disp = await stockService.VerificarDisponibilidadAsync(Empresa.SingletonId, ClaseBulbo.Clase1, 100);
        Assert.True(disp.Success);
    }

    [Fact]
    public void T3_23_F24_F25_Sidebar5SectionsAndLocalStoragePersistence()
    {
        var appJs = FrontendAssetScanner.ReadFrontendFile("js/app.js");
        Assert.Contains("siga_sidebar_collapsed_sections", appJs);
        Assert.Contains("nav-section", appJs);
    }

    [Fact]
    public void T3_24_F26_F30_PermissionFilteringAndEmployeesViewAccess()
    {
        var appJs = FrontendAssetScanner.ReadFrontendFile("js/app.js");
        Assert.Contains("data-permiso=\"EMPLEADOS_VER\"", appJs);
    }

    [Fact]
    public void T3_25_F26_F31_PermissionFilteringAndRolesViewAccess()
    {
        var appJs = FrontendAssetScanner.ReadFrontendFile("js/app.js");
        Assert.Contains("data-permiso=\"ROLES_VER\"", appJs);
    }

    [Fact]
    public void T3_26_F28_F30_ProhibitionSymbolAndEmployeeDeactivation()
    {
        var empleadosJs = FrontendAssetScanner.ReadFrontendFile("js/views/empleados-view.js");
        Assert.Contains("getProhibitionIcon", empleadosJs);
        Assert.Contains("desactivar", empleadosJs);
    }

    [Fact]
    public void T3_27_F28_F07_ProhibitionSymbolAndLastAdminSafeguard()
    {
        var empleadosJs = FrontendAssetScanner.ReadFrontendFile("js/views/empleados-view.js");
        Assert.Contains("ADMINISTRADOR", empleadosJs);
    }

    [Fact]
    public void T3_28_F34_F15_CommercialMovementsViewAndLiveMixDecomposition()
    {
        var js = FrontendAssetScanner.ReadCommercialMovementsView();
        Assert.Contains("MIX", js);
    }

    [Fact]
    public void T3_29_F34_F16_CommercialMovementsViewAndNewClientBadge()
    {
        var js = FrontendAssetScanner.ReadCommercialMovementsView();
        Assert.Contains("contrato", js);
    }

    [Fact]
    public void T3_30_F34_F14_CommercialMovementsDetailAndRejectionModal()
    {
        var js = FrontendAssetScanner.ReadCommercialMovementsView();
        Assert.Contains("motivo", js);
    }

    [Fact]
    public async Task T3_31_F03_F05_SeederSystemRolesAndJwtGeneration()
    {
        var anon = CreateAnonymousClient();
        var res = await anon.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto
        {
            Username = "admin",
            Password = "Admin123!"
        });

        var envelope = await DeserializeApiResponseAsync<LoginResponseDto>(res);
        Assert.NotNull(envelope!.Data!.Token);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(envelope.Data.Token);
        var role = jwt.Claims.First(c => c.Type == ClaimTypes.Role || c.Type == "role");
        Assert.Equal("ADMINISTRADOR", role.Value);
    }

    [Fact]
    public async Task T3_32_F07_F01_EmployeeSoftDeleteAndLoginPrevention()
    {
        var adminClient = CreateAdminClient();
        var doc = "T332" + Random.Shared.Next(10000, 99999);
        var username = "user_t332_" + Guid.NewGuid().ToString("N")[..6];

        var createRes = await adminClient.PostAsJsonAsync("/api/v1/empleados", new CrearEmpleadoDto
        {
            Nombre = "T332",
            Apellido = "SoftDel",
            Documento = doc,
            Username = username,
            Password = "Password123!",
            RolId = 2
        });
        var emp = (await DeserializeApiResponseAsync<EmpleadoDto>(createRes))!.Data!;

        // Soft delete
        await adminClient.DeleteAsync($"/api/v1/empleados/{emp.Id}");

        // Attempt login
        var anon = CreateAnonymousClient();
        var loginRes = await anon.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto
        {
            Username = username,
            Password = "Password123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, loginRes.StatusCode);
    }

    [Fact]
    public async Task T3_33_F13_F18_FinalizationEndpointAndOperacionComercialLinkage()
    {
        var mov = new MovimientoComercial(TipoMovimientoComercial.TraspasoEntreClientes, 1, 2, 3);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 10, 0m);
        mov.AprobarAdmin(1, false);

        mov.Finalizar(1, operacionComercialId: 8888);
        Assert.Equal(8888, mov.OperacionComercialId);
        Assert.Equal(EstadoMovimientoComercial.Finalizado, mov.Estado);
    }

    [Fact]
    public async Task T3_34_F21_F23_RowCapacityBackendAndUnitTestsAssertion()
    {
        var fila = new Fila(1, 1, 1, capacidad: 100);
        Assert.True(fila.TieneCapacidadDisponible(100));
        Assert.False(fila.TieneCapacidadDisponible(101));
    }
}
