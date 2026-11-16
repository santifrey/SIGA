using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Filas;
using SIGA.Application.DTOs.Lotes;
using SIGA.Application.DTOs.Plantaciones;
using SIGA.Application.Interfaces;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.E2ETests.Infrastructure;
using SIGA.Infrastructure.Persistence;
using Xunit;

namespace SIGA.E2ETests.Tier1_FeatureCoverage;

[Collection("E2E Test Suite")]
public class PlantationsTests : TestBase
{
    public PlantationsTests(SigaTestWebApplicationFactory factory) : base(factory)
    {
    }

    private async Task<(int SucursalId, int TemporadaId, int ClienteId)> EnsureContextAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SigaDbContext>();

        var sucursal = await db.Sucursales.FirstOrDefaultAsync(s => s.Estado == EstadoSucursal.Activo);
        if (sucursal == null)
        {
            sucursal = new Sucursal
            {
                Nombre = "Sucursal Central",
                Ubicacion = "Córdoba",
                CapacidadTotal = 50000m,
                Estado = EstadoSucursal.Activo,
                FechaCreacion = DateTime.UtcNow
            };
            await db.Sucursales.AddAsync(sucursal);
            await db.SaveChangesAsync();
        }

        var temporada = await db.Temporadas.FirstOrDefaultAsync(t => t.Estado == EstadoTemporada.Planificada || t.Estado == EstadoTemporada.Plantacion);
        if (temporada == null)
        {
            temporada = new Temporada
            {
                Nombre = "Temporada 2026/2027",
                UsuarioCreadorId = 1,
                FechaInicio = DateTime.UtcNow,
                FechaFin = DateTime.UtcNow.AddMonths(6),
                Estado = EstadoTemporada.Planificada,
                FechaCreacion = DateTime.UtcNow
            };
            await db.Temporadas.AddAsync(temporada);
            await db.SaveChangesAsync();
        }

        var cliente = await db.Clientes.FirstOrDefaultAsync();
        if (cliente == null)
        {
            var persona = new Persona
            {
                Nombre = "Productor",
                Apellido = "Campo",
                Documento = "PROD" + Random.Shared.Next(10000, 99999),
                FechaCreacion = DateTime.UtcNow
            };
            await db.Personas.AddAsync(persona);
            await db.SaveChangesAsync();

            cliente = new Cliente
            {
                PersonaId = persona.Id,
                CodigoCliente = "CLI-PROD-" + Guid.NewGuid().ToString("N")[..4],
                EstadoCliente = EstadoCliente.Activo,
                Estado = EstadoParticipante.Activo,
                FechaRegistro = DateTime.UtcNow
            };
            await db.Clientes.AddAsync(cliente);
            await db.SaveChangesAsync();
        }

        // Ensure available bulb stock for client to plant
        var stock = await db.MovimientosBulbos.FirstOrDefaultAsync(m => m.DestinoId == cliente.Id && m.Estado == EstadoMovimiento.Confirmado);
        if (stock == null)
        {
            var op = new OperacionComercial { Tipo = TipoOperacion.Ingreso, Estado = EstadoOperacion.Confirmada, Fecha = DateTime.UtcNow };
            await db.OperacionesComerciales.AddAsync(op);
            await db.SaveChangesAsync();

            var stockMov = new MovimientoBulbos(op.Id, origenId: Empresa.SingletonId, destinoId: cliente.Id, usuarioCreadorId: 1, observaciones: "Stock inicial de prueba para siembra");
            stockMov.AgregarCantidad(ClaseBulbo.Clase1, 25000);
            stockMov.Confirmar();
            await db.MovimientosBulbos.AddAsync(stockMov);
            await db.SaveChangesAsync();
        }

        return (sucursal.Id, temporada.Id, cliente.Id);
    }

    #region Feature 21: Structured Row & Lot Backend (5 Tests)

    [Fact]
    public async Task T1_F21_01_QueryRowsByBranchAndSeason_ReturnsIntegerMetadata()
    {
        // Arrange
        var client = CreateAdminClient();
        var (sucursalId, temporadaId, _) = await EnsureContextAsync();

        // Create a row with integer NumeroFila
        var createFilaDto = new CreateFilaDto
        {
            SucursalId = sucursalId,
            TemporadaId = temporadaId,
            NumeroFila = 15,
            Identificacion = "15",
            Capacidad = 10000
        };
        await client.PostAsJsonAsync("/api/v1/filas", createFilaDto);

        // Act
        var res = await client.GetAsync($"/api/v1/filas/por-sucursal/{sucursalId}/temporada/{temporadaId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<List<FilaDto>>(res);
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.Contains(envelope.Data!, f => f.NumeroFila == 15 && f.Capacidad == 10000);
    }

    [Fact]
    public async Task T1_F21_02_PlantingDistribution_WithExistingRow_Returns201Created()
    {
        // Arrange
        var client = CreateAdminClient();
        var (sucursalId, temporadaId, clienteId) = await EnsureContextAsync();

        // Create a row
        var filaRes = await client.PostAsJsonAsync("/api/v1/filas", new CreateFilaDto
        {
            SucursalId = sucursalId,
            TemporadaId = temporadaId,
            NumeroFila = 21,
            Identificacion = "Fila 21",
            Capacidad = 8000
        });
        var fila = (await DeserializeApiResponseAsync<FilaDto>(filaRes))!.Data!;

        // Act: Distribute lot with FilaId and integer NumeroLote = 1
        var distDto = new RegistrarDistribucionDto
        {
            SucursalId = sucursalId,
            TemporadaId = temporadaId,
            FilaId = fila.Id,
            NumeroFila = 21,
            IdentificacionFila = "21",
            CapacidadFila = 8000,
            NumeroLote = 1,
            ClienteId = clienteId,
            Detalles = new List<DetalleDistribucionDto>
            {
                new() { Clase = ClaseBulbo.Clase1, Cantidad = 1000 }
            }
        };

        var res = await client.PostAsJsonAsync("/api/v1/plantaciones/distribucion", distDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<LoteDto>(res);
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.Equal(1, envelope.Data!.Orden);
        Assert.Equal(fila.Id, envelope.Data.FilaId);
    }

    [Fact]
    public async Task T1_F21_03_PlantingDistribution_WithNewRowCreation_CreatesFilaAndLote()
    {
        // Arrange
        var client = CreateAdminClient();
        var (sucursalId, temporadaId, clienteId) = await EnsureContextAsync();
        var newRowNum = Random.Shared.Next(100, 999);

        var distDto = new RegistrarDistribucionDto
        {
            SucursalId = sucursalId,
            TemporadaId = temporadaId,
            FilaId = null, // Trigger inline creation
            NumeroFila = newRowNum,
            IdentificacionFila = newRowNum.ToString(),
            CapacidadFila = 5000,
            NumeroLote = 1,
            ClienteId = clienteId,
            Detalles = new List<DetalleDistribucionDto>
            {
                new() { Clase = ClaseBulbo.Clase1, Cantidad = 500 }
            }
        };

        // Act
        var res = await client.PostAsJsonAsync("/api/v1/plantaciones/distribucion", distDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<LoteDto>(res);
        Assert.NotNull(envelope);
        Assert.True(envelope.Data!.FilaId > 0);
    }

    [Fact]
    public async Task T1_F21_04_AutoIncrementLotOrder_AssignsNextSequentialInteger()
    {
        // Arrange
        var client = CreateAdminClient();
        var (sucursalId, temporadaId, clienteId) = await EnsureContextAsync();

        // Create row
        var filaRes = await client.PostAsJsonAsync("/api/v1/filas", new CreateFilaDto
        {
            SucursalId = sucursalId,
            TemporadaId = temporadaId,
            NumeroFila = 33,
            Identificacion = "33",
            Capacidad = 10000
        });
        var fila = (await DeserializeApiResponseAsync<FilaDto>(filaRes))!.Data!;

        // First lot: order 1
        await client.PostAsJsonAsync("/api/v1/plantaciones/distribucion", new RegistrarDistribucionDto
        {
            SucursalId = sucursalId,
            TemporadaId = temporadaId,
            FilaId = fila.Id,
            NumeroLote = 1,
            ClienteId = clienteId,
            Detalles = new List<DetalleDistribucionDto> { new() { Clase = ClaseBulbo.Clase1, Cantidad = 100 } }
        });

        // Act: Second lot with NumeroLote = null (auto-suggest)
        var res = await client.PostAsJsonAsync("/api/v1/plantaciones/distribucion", new RegistrarDistribucionDto
        {
            SucursalId = sucursalId,
            TemporadaId = temporadaId,
            FilaId = fila.Id,
            NumeroLote = null,
            ClienteId = clienteId,
            Detalles = new List<DetalleDistribucionDto> { new() { Clase = ClaseBulbo.Clase1, Cantidad = 100 } }
        });

        // Assert
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<LoteDto>(res);
        Assert.Equal(2, envelope!.Data!.Orden);
    }

    [Fact]
    public async Task T1_F21_05_RowCapacityEnforcement_DecrementsAvailableCapacity()
    {
        // Arrange
        var client = CreateAdminClient();
        var (sucursalId, temporadaId, clienteId) = await EnsureContextAsync();

        var filaRes = await client.PostAsJsonAsync("/api/v1/filas", new CreateFilaDto
        {
            SucursalId = sucursalId,
            TemporadaId = temporadaId,
            NumeroFila = 44,
            Identificacion = "44",
            Capacidad = 3000
        });
        var fila = (await DeserializeApiResponseAsync<FilaDto>(filaRes))!.Data!;

        // Act: Plant 1000 bulbs
        await client.PostAsJsonAsync("/api/v1/plantaciones/distribucion", new RegistrarDistribucionDto
        {
            SucursalId = sucursalId,
            TemporadaId = temporadaId,
            FilaId = fila.Id,
            NumeroLote = 1,
            ClienteId = clienteId,
            Detalles = new List<DetalleDistribucionDto> { new() { Clase = ClaseBulbo.Clase1, Cantidad = 1000 } }
        });

        // Query row again
        var getRes = await client.GetAsync($"/api/v1/filas/{fila.Id}");
        var updatedFila = (await DeserializeApiResponseAsync<FilaDto>(getRes))!.Data!;

        // Assert
        Assert.Equal(1000, updatedFila.OcupacionActual);
        Assert.Equal(2000, updatedFila.CapacidadDisponible);
    }

    #endregion

    #region Feature 22: Structured Row & Lot Frontend Contract (5 Tests)

    [Fact]
    public async Task T1_F22_01_DynamicRowDropdown_InPlantacionesView()
    {
        var js = FrontendAssetScanner.ReadFrontendFile("js/views/plantaciones-view.js");
        var serviceJs = FrontendAssetScanner.ReadFrontendFile("js/services/plantaciones-service.js");
        Assert.Contains("select-fila", js);
        Assert.Contains("/filas/por-sucursal", serviceJs);
    }

    [Fact]
    public async Task T1_F22_02_SelectNuevaFila_DisplaysNumberAndCapacityInputs()
    {
        var js = FrontendAssetScanner.ReadFrontendFile("js/views/plantaciones-view.js");
        Assert.Contains("btn-toggle-nueva-fila", js);
        Assert.Contains("input-nueva-fila-num", js);
        Assert.Contains("input-nueva-fila-cap", js);
    }

    [Fact]
    public async Task T1_F22_03_IntegerInputTypes_InDom()
    {
        var js = FrontendAssetScanner.ReadFrontendFile("js/views/plantaciones-view.js");
        Assert.Contains("type=\"number\"", js);
        Assert.Contains("input-lote-orden", js);
    }

    [Fact]
    public async Task T1_F22_04_AutoSuggestNextLotOrder_Implementation()
    {
        var js = FrontendAssetScanner.ReadFrontendFile("js/views/plantaciones-view.js");
        Assert.Contains("nextOrden", js);
    }

    [Fact]
    public async Task T1_F22_05_PayloadFormatsIntegers_NumericConversion()
    {
        var js = FrontendAssetScanner.ReadFrontendFile("js/views/plantaciones-view.js");
        Assert.Contains("parseInt", js);
        Assert.Contains("numeroFila", js);
        Assert.Contains("numeroLote", js);
    }

    #endregion

    #region Feature 23: Plantations Unit Tests (5 Tests)

    [Fact]
    public async Task T1_F23_01_GestionPlantaciones_ResolvesFromDI()
    {
        using var scope = Factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetService<IGestionPlantaciones>();
        Assert.NotNull(service);
    }

    [Fact]
    public async Task T1_F23_02_GestionFilas_ResolvesFromDI()
    {
        using var scope = Factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetService<IGestionFilas>();
        Assert.NotNull(service);
    }

    [Fact]
    public async Task T1_F23_03_GestionLotes_ResolvesFromDI()
    {
        using var scope = Factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetService<IGestionLotes>();
        Assert.NotNull(service);
    }

    [Fact]
    public async Task T1_F23_04_FilaEntity_CapacidadDisponible_Calculation()
    {
        var fila = new Fila(1, 1, 10, 5000);
        Assert.Equal(5000, fila.ObtenerCapacidadDisponible());
    }

    [Fact]
    public async Task T1_F23_05_FilaEntity_OverflowPrevention()
    {
        var fila = new Fila(1, 1, 10, 100);
        Assert.True(fila.TieneCapacidadDisponible(100));
        Assert.False(fila.TieneCapacidadDisponible(101));
    }

    #endregion
}
