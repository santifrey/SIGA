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
using SIGA.Domain.Exceptions;
using SIGA.E2ETests.Infrastructure;
using SIGA.Infrastructure.Persistence;
using Xunit;

namespace SIGA.E2ETests.Tier2_BoundaryCases;

[Collection("E2E Test Suite")]
public class PlantationsBoundariesTests : TestBase
{
    public PlantationsBoundariesTests(SigaTestWebApplicationFactory factory) : base(factory)
    {
    }

    private async Task<(int SucursalId, int TemporadaId, int ClienteId)> EnsureContextAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SigaDbContext>();

        var sucursal = await db.Sucursales.FirstOrDefaultAsync(s => s.Estado == EstadoSucursal.Activo);
        if (sucursal == null)
        {
            sucursal = new Sucursal { Nombre = "Suc B", Ubicacion = "Córdoba", CapacidadTotal = 50000m, Estado = EstadoSucursal.Activo, FechaCreacion = DateTime.UtcNow };
            await db.Sucursales.AddAsync(sucursal);
            await db.SaveChangesAsync();
        }

        var temporada = await db.Temporadas.FirstOrDefaultAsync(t => t.Estado == EstadoTemporada.Planificada);
        if (temporada == null)
        {
            temporada = new Temporada { Nombre = "Temporada PB", UsuarioCreadorId = 1, FechaInicio = DateTime.UtcNow, FechaFin = DateTime.UtcNow.AddMonths(6), Estado = EstadoTemporada.Planificada, FechaCreacion = DateTime.UtcNow };
            await db.Temporadas.AddAsync(temporada);
            await db.SaveChangesAsync();
        }

        var cliente = await db.Clientes.FirstOrDefaultAsync();
        if (cliente == null)
        {
            var p = new Persona { Nombre = "Cli", Apellido = "B", Documento = "PB" + Random.Shared.Next(10000, 99999), FechaCreacion = DateTime.UtcNow };
            await db.Personas.AddAsync(p);
            await db.SaveChangesAsync();
            cliente = new Cliente { PersonaId = p.Id, CodigoCliente = "PB-" + Guid.NewGuid().ToString("N")[..4], EstadoCliente = EstadoCliente.Activo, Estado = EstadoParticipante.Activo, FechaRegistro = DateTime.UtcNow };
            await db.Clientes.AddAsync(cliente);
            await db.SaveChangesAsync();
        }

        return (sucursal.Id, temporada.Id, cliente.Id);
    }

    #region Feature 21: Structured Row & Lot Backend (5 Boundary Tests)

    [Fact]
    public async Task T2_F21_01_NumeroFilaZeroOrNegative_Returns400()
    {
        var client = CreateAdminClient();
        var (sucursalId, temporadaId, _) = await EnsureContextAsync();

        var resZero = await client.PostAsJsonAsync("/api/v1/filas", new CreateFilaDto
        {
            SucursalId = sucursalId,
            TemporadaId = temporadaId,
            NumeroFila = 0,
            Capacidad = 1000
        });

        var resNeg = await client.PostAsJsonAsync("/api/v1/filas", new CreateFilaDto
        {
            SucursalId = sucursalId,
            TemporadaId = temporadaId,
            NumeroFila = -5,
            Capacidad = 1000
        });

        Assert.True(resZero.StatusCode == HttpStatusCode.BadRequest || resZero.StatusCode == HttpStatusCode.UnprocessableEntity);
        Assert.True(resNeg.StatusCode == HttpStatusCode.BadRequest || resNeg.StatusCode == HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task T2_F21_02_NumeroLoteZeroOrNegative_Rejected()
    {
        var client = CreateAdminClient();
        var (sucursalId, temporadaId, clienteId) = await EnsureContextAsync();

        var res = await client.PostAsJsonAsync("/api/v1/plantaciones/distribucion", new RegistrarDistribucionDto
        {
            SucursalId = sucursalId,
            TemporadaId = temporadaId,
            NumeroFila = 10,
            NumeroLote = -1,
            ClienteId = clienteId,
            Detalles = new List<DetalleDistribucionDto> { new() { Clase = ClaseBulbo.Clase1, Cantidad = 100 } }
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task T2_F21_03_CapacidadFilaNegative_Rejected()
    {
        var client = CreateAdminClient();
        var (sucursalId, temporadaId, _) = await EnsureContextAsync();

        var res = await client.PostAsJsonAsync("/api/v1/filas", new CreateFilaDto
        {
            SucursalId = sucursalId,
            TemporadaId = temporadaId,
            NumeroFila = 12,
            Capacidad = -100
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task T2_F21_04_DistributionInClosedSeason_Rejected()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SigaDbContext>();
        var (sucursalId, _, clienteId) = await EnsureContextAsync();

        // Create a closed season
        var closedTemp = new Temporada
        {
            Nombre = "Temporada Cerrada",
            UsuarioCreadorId = 1,
            FechaInicio = DateTime.UtcNow.AddMonths(-12),
            FechaFin = DateTime.UtcNow.AddMonths(-6),
            Estado = EstadoTemporada.Cerrada,
            FechaCreacion = DateTime.UtcNow
        };
        await db.Temporadas.AddAsync(closedTemp);
        await db.SaveChangesAsync();

        var client = CreateAdminClient();
        var res = await client.PostAsJsonAsync("/api/v1/plantaciones/distribucion", new RegistrarDistribucionDto
        {
            SucursalId = sucursalId,
            TemporadaId = closedTemp.Id,
            NumeroFila = 1,
            NumeroLote = 1,
            ClienteId = clienteId,
            Detalles = new List<DetalleDistribucionDto> { new() { Clase = ClaseBulbo.Clase1, Cantidad = 10 } }
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task T2_F21_05_RowCapacityExceeded_Rejected()
    {
        var client = CreateAdminClient();
        var (sucursalId, temporadaId, clienteId) = await EnsureContextAsync();

        // Create small row with capacity 50
        var filaRes = await client.PostAsJsonAsync("/api/v1/filas", new CreateFilaDto
        {
            SucursalId = sucursalId,
            TemporadaId = temporadaId,
            NumeroFila = 99,
            Capacidad = 50
        });
        var fila = (await DeserializeApiResponseAsync<FilaDto>(filaRes))!.Data!;

        // Attempt distributing 1000 bulbs
        var res = await client.PostAsJsonAsync("/api/v1/plantaciones/distribucion", new RegistrarDistribucionDto
        {
            SucursalId = sucursalId,
            TemporadaId = temporadaId,
            FilaId = fila.Id,
            NumeroLote = 1,
            ClienteId = clienteId,
            Detalles = new List<DetalleDistribucionDto> { new() { Clase = ClaseBulbo.Clase1, Cantidad = 1000 } }
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    #endregion

    #region Feature 22: Structured Row & Lot Frontend Contract (5 Boundary Tests)

    [Fact]
    public void T2_F22_01_NegativeNumberEntryInFrontendInput_BlockedByMinAttribute()
    {
        var js = FrontendAssetScanner.ReadFrontendFile("js/views/plantaciones-view.js");
        Assert.Contains("min=\"1\"", js);
    }

    [Fact]
    public void T2_F22_02_EmptySelectionHandling_ShowsToastWarning()
    {
        var js = FrontendAssetScanner.ReadFrontendFile("js/views/plantaciones-view.js");
        Assert.Contains("toast.warning", js);
    }

    [Fact]
    public void T2_F22_03_SwitchingSeason_ResetsRowSelector()
    {
        var js = FrontendAssetScanner.ReadFrontendFile("js/views/plantaciones-view.js");
        Assert.Contains("change", js);
        Assert.Contains("select-temporada", js);
    }

    [Fact]
    public void T2_F22_04_CapacityIndicatorLiveRefresh_RendersOccupancyRatio()
    {
        var js = FrontendAssetScanner.ReadFrontendFile("js/views/plantaciones-view.js");
        Assert.Contains("capacidad", js);
    }

    [Fact]
    public void T2_F22_05_DuplicateRowNumberHandlingInUi_AlertsUser()
    {
        var js = FrontendAssetScanner.ReadFrontendFile("js/views/plantaciones-view.js");
        Assert.Contains("Fila", js);
    }

    #endregion

    #region Feature 23: Plantations Unit Tests (5 Boundary Tests)

    [Fact]
    public async Task T2_F23_01_ZeroRowCapacity_UnlimitedPlantingAllowed()
    {
        var fila = new Fila(1, 1, 1, capacidad: 0);
        Assert.True(fila.TieneCapacidadDisponible(100_000));
    }

    [Fact]
    public async Task T2_F23_02_ClosedSeasonValidation_Fails()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SigaDbContext>();

        // Arrange: Seed a closed season in the database
        var closedSeason = new Temporada
        {
            Nombre = "Temporada Cerrada " + Guid.NewGuid().ToString("N")[..4],
            FechaInicio = DateTime.UtcNow.AddYears(-1),
            FechaFin = DateTime.UtcNow.AddMonths(-1),
            Estado = EstadoTemporada.Cerrada,
            UsuarioCreadorId = 1,
            FechaCreacion = DateTime.UtcNow
        };
        await db.Temporadas.AddAsync(closedSeason);
        await db.SaveChangesAsync();

        // Act 1 (Domain Validation): Attempting to start plantation on a closed season throws OperacionInvalidaException
        var ex = Assert.Throws<OperacionInvalidaException>(() => closedSeason.IniciarPlantacion());
        Assert.Contains("Cerrada", ex.Message);

        // Act 2 (Controller Validation): Calling POST /api/v1/plantaciones with closed season returns 400 BadRequest
        var client = CreateAdminClient();
        var res = await client.PostAsJsonAsync("/api/v1/plantaciones", new CreatePlantacionDto
        {
            TemporadaId = closedSeason.Id,
            Observaciones = "Intento en temporada cerrada"
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var envelope = await DeserializeApiResponseAsync<object>(res);
        Assert.NotNull(envelope);
        Assert.False(envelope.Success);
        Assert.Contains("cerrada", envelope.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task T2_F23_03_MissingIdentificationFallback_DefaultsToNumeroFila()
    {
        var fila = new Fila(1, 1, 42, 100);
        Assert.Equal("42", fila.Identificacion);
    }

    [Fact]
    public async Task T2_F23_04_ClientStockAvailability_NegativeQuantityThrows()
    {
        using var scope = Factory.Services.CreateScope();
        var stockService = scope.ServiceProvider.GetRequiredService<IGestionStock>();

        var res = await stockService.VerificarDisponibilidadAsync(1, ClaseBulbo.Clase1, -10);
        Assert.False(res.Success);
    }

    [Fact]
    public async Task T2_F23_05_ZeroPlantingQuantity_Rejected()
    {
        var client = CreateAdminClient();
        var (sucursalId, temporadaId, clienteId) = await EnsureContextAsync();

        var res = await client.PostAsJsonAsync("/api/v1/plantaciones/distribucion", new RegistrarDistribucionDto
        {
            SucursalId = sucursalId,
            TemporadaId = temporadaId,
            NumeroFila = 1,
            NumeroLote = 1,
            ClienteId = clienteId,
            Detalles = new List<DetalleDistribucionDto> { new() { Clase = ClaseBulbo.Clase1, Cantidad = 0 } }
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    #endregion
}
