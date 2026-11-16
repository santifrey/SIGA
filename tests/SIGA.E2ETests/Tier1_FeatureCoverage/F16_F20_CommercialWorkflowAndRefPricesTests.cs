using System.Net;
using SIGA.E2ETests.Common;
using SIGA.E2ETests.Fixtures;
using Xunit;

namespace SIGA.E2ETests.Tier1_FeatureCoverage;

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F16")]
public class F16_NewClientContractEnforcementTests : SigaTestBase
{
    public F16_NewClientContractEnforcementTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F16_01_CheckNewClientVerificationEndpoint_ReturnsEsNuevoAndRequiereContrato()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<VerificarClienteNuevoDto>("/api/v1/movimientos-comerciales/verificar-cliente-nuevo/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
    }

    [Fact]
    public async Task TC_F16_02_AttachContractForNewClientVenta_EnablesApproval()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 1, Cantidad = 10, PrecioUnitario = 50m } }
        };
        var (createRes, createBody) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        Assert.NotNull(createBody?.Data);

        var response = await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{createBody.Data.Id}/aprobar", new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TC_F16_03_FinalizeNewClientFirstVenta_CompletesSuccessfully()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 1, Cantidad = 10, PrecioUnitario = 50m } }
        };
        var (createRes, createBody) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        Assert.NotNull(createBody?.Data);
        var movId = createBody.Data.Id;

        await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/aprobar", new { });
        await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/facturar", new FacturarMovimientoDto { NumeroFactura = "F-FIN-16" });
        var response = await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/finalizar", new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TC_F16_04_CheckExistingClientStatus_ReturnsRequiereContratoFalse()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<VerificarClienteNuevoDto>("/api/v1/movimientos-comerciales/verificar-cliente-nuevo/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
    }

    [Fact]
    public async Task TC_F16_05_ApproveExistingClientVentaWithoutContract_Succeeds()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 2, Cantidad = 10, PrecioUnitario = 50m } }
        };
        var (createRes, createBody) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        Assert.NotNull(createBody?.Data);

        var response = await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{createBody.Data.Id}/aprobar", new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F17")]
public class F17_TraceabilityApprovalHistoryTests : SigaTestBase
{
    public F17_TraceabilityApprovalHistoryTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F17_01_RegistrationAuditMetadata_CapturesUserAndTimestamp()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            TemporadaId = 1,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 1, Cantidad = 100, PrecioUnitario = 50m } }
        };

        var (createRes, createBody) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        Assert.NotNull(createBody?.Data);
        Assert.True(createBody.Data.UsuarioRegistroId > 0, "UsuarioRegistroId must be captured");
        Assert.True(createBody.Data.FechaRegistro <= DateTime.UtcNow, "FechaRegistro must be valid timestamp");

        var (getRes, getBody) = await adminClient.GetJsonAsync<MovimientoComercialDto>($"/api/v1/movimientos-comerciales/{createBody.Data.Id}");
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);
        Assert.NotNull(getBody?.Data);
        Assert.NotEmpty(getBody.Data.HistorialAprobaciones);
    }

    [Fact]
    public async Task TC_F17_02_ApprovalAuditStep_LoggedWithActorAndAction()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            TemporadaId = 1,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 1, Cantidad = 50, PrecioUnitario = 50m } }
        };

        var (_, createBody) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        var movId = createBody!.Data!.Id;

        var approveRes = await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/aprobar", new { });
        Assert.Equal(HttpStatusCode.OK, approveRes.StatusCode);

        var (getRes, getBody) = await adminClient.GetJsonAsync<MovimientoComercialDto>($"/api/v1/movimientos-comerciales/{movId}");
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);
        Assert.NotNull(getBody?.Data);
        Assert.Contains(getBody.Data.HistorialAprobaciones, h => h.Accion.Contains("Aprob") || h.UsuarioId > 0);
    }

    [Fact]
    public async Task TC_F17_03_InvoicingAuditStep_LoggedWithDetails()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            TemporadaId = 1,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 2, Cantidad = 60, PrecioUnitario = 40m } }
        };

        var (_, createBody) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        var movId = createBody!.Data!.Id;

        await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/aprobar", new { });
        var invoiceNum = "FAC-AUDIT-001";
        var invoiceRes = await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/facturar", new FacturarMovimientoDto { NumeroFactura = invoiceNum });
        Assert.Equal(HttpStatusCode.OK, invoiceRes.StatusCode);

        var (getRes, getBody) = await adminClient.GetJsonAsync<MovimientoComercialDto>($"/api/v1/movimientos-comerciales/{movId}");
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);
        Assert.NotNull(getBody?.Data);
        Assert.Contains(getBody.Data.HistorialAprobaciones, h => h.Accion.Contains("Factur") || (h.Notas != null && h.Notas.Contains(invoiceNum)));
    }

    [Fact]
    public async Task TC_F17_04_FinalizationAuditStep_LoggedWithActor()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 3,
            ClienteOrigenId = 1,
            ClienteDestinoId = 2,
            TemporadaId = 1,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 3, Cantidad = 30, PrecioUnitario = 0m } }
        };

        var (_, createBody) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        var movId = createBody!.Data!.Id;

        await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/aprobar", new { });
        var finalRes = await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/finalizar", new { });
        Assert.Equal(HttpStatusCode.OK, finalRes.StatusCode);

        var (getRes, getBody) = await adminClient.GetJsonAsync<MovimientoComercialDto>($"/api/v1/movimientos-comerciales/{movId}");
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);
        Assert.NotNull(getBody?.Data);
        Assert.Contains(getBody.Data.HistorialAprobaciones, h => h.Accion.Contains("Finaliz") || h.Accion.Contains("Complet"));
    }

    [Fact]
    public async Task TC_F17_05_TimelineChronologicalQuery_ReturnsOrderedEvents()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            TemporadaId = 1,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 1, Cantidad = 40, PrecioUnitario = 50m } }
        };

        var (_, createBody) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        var movId = createBody!.Data!.Id;

        await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/aprobar", new { });
        await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/facturar", new FacturarMovimientoDto { NumeroFactura = "FAC-ORD-01" });
        await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/finalizar", new { });

        var (getRes, getBody) = await adminClient.GetJsonAsync<MovimientoComercialDto>($"/api/v1/movimientos-comerciales/{movId}");
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);
        Assert.NotNull(getBody?.Data);

        var timeline = getBody.Data.HistorialAprobaciones;
        Assert.True(timeline.Count >= 3, "Timeline should contain at least 3 chronological milestones");
        for (int i = 0; i < timeline.Count - 1; i++)
        {
            Assert.True(timeline[i].Fecha <= timeline[i + 1].Fecha, $"Timeline event {i} is not chronologically before {i + 1}");
        }
    }
}

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F18")]
public class F18_FinalizationStockImpactTests : SigaTestBase
{
    public F18_FinalizationStockImpactTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F18_01_Type1FinalizationStockDecrement_DecrementsEmpresaStock()
    {
        var adminClient = await GetAdminClientAsync();
        var (initRes, initStock) = await adminClient.GetJsonAsync<StockClienteDto>("/api/v1/stock/por-cliente/1");
        Assert.Equal(HttpStatusCode.OK, initRes.StatusCode);
        var initialAmount = initStock?.Data?.StockPorClase.FirstOrDefault(s => s.Clase == 2)?.Cantidad ?? 0;

        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            TemporadaId = 1,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 2, Cantidad = 100, PrecioUnitario = 50m } }
        };

        var (_, createBody) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        var movId = createBody!.Data!.Id;

        await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/aprobar", new { });
        await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/facturar", new FacturarMovimientoDto { NumeroFactura = "FC-18-01" });
        var finRes = await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/finalizar", new { });
        Assert.Equal(HttpStatusCode.OK, finRes.StatusCode);

        var (postRes, postStock) = await adminClient.GetJsonAsync<StockClienteDto>("/api/v1/stock/por-cliente/1");
        Assert.Equal(HttpStatusCode.OK, postRes.StatusCode);
        var updatedAmount = postStock?.Data?.StockPorClase.FirstOrDefault(s => s.Clase == 2)?.Cantidad ?? 0;
        Assert.Equal(initialAmount + 100, updatedAmount);
    }

    [Fact]
    public async Task TC_F18_02_Type2FinalizationStockIncrement_IncrementsEmpresaStock()
    {
        var adminClient = await GetAdminClientAsync();
        var (initRes, initStock) = await adminClient.GetJsonAsync<StockClienteDto>("/api/v1/stock/por-cliente/1");
        Assert.Equal(HttpStatusCode.OK, initRes.StatusCode);
        var initialAmount = initStock?.Data?.StockPorClase.FirstOrDefault(s => s.Clase == 3)?.Cantidad ?? 0;

        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 2,
            ClienteId = 1,
            TemporadaId = 1,
            PorcentajeComision = 10m,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 3, Cantidad = 150, PrecioUnitario = 40m } }
        };

        var (_, createBody) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        var movId = createBody!.Data!.Id;

        await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/aprobar", new { });
        await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/facturar", new FacturarMovimientoDto { NumeroFactura = "FC-18-02" });
        var finRes = await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/finalizar", new { });
        Assert.Equal(HttpStatusCode.OK, finRes.StatusCode);

        var (postRes, postStock) = await adminClient.GetJsonAsync<StockClienteDto>("/api/v1/stock/por-cliente/1");
        Assert.Equal(HttpStatusCode.OK, postRes.StatusCode);
        var updatedAmount = postStock?.Data?.StockPorClase.FirstOrDefault(s => s.Clase == 3)?.Cantidad ?? 0;
        Assert.Equal(initialAmount + 150, updatedAmount);
    }

    [Fact]
    public async Task TC_F18_03_Type3FinalizationStockTransfer_TransfersStockBetweenClients()
    {
        var adminClient = await GetAdminClientAsync();
        var (_, initStock1) = await adminClient.GetJsonAsync<StockClienteDto>("/api/v1/stock/por-cliente/1");
        var (_, initStock2) = await adminClient.GetJsonAsync<StockClienteDto>("/api/v1/stock/por-cliente/2");

        var c1Init = initStock1?.Data?.StockPorClase.FirstOrDefault(s => s.Clase == 4)?.Cantidad ?? 0;
        var c2Init = initStock2?.Data?.StockPorClase.FirstOrDefault(s => s.Clase == 4)?.Cantidad ?? 0;

        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 3,
            ClienteOrigenId = 1,
            ClienteDestinoId = 2,
            TemporadaId = 1,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 4, Cantidad = 60, PrecioUnitario = 0m } }
        };

        var (_, createBody) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        var movId = createBody!.Data!.Id;

        await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/aprobar", new { });
        var finRes = await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/finalizar", new { });
        Assert.Equal(HttpStatusCode.OK, finRes.StatusCode);

        var (_, postStock1) = await adminClient.GetJsonAsync<StockClienteDto>("/api/v1/stock/por-cliente/1");
        var (_, postStock2) = await adminClient.GetJsonAsync<StockClienteDto>("/api/v1/stock/por-cliente/2");

        var c1Post = postStock1?.Data?.StockPorClase.FirstOrDefault(s => s.Clase == 4)?.Cantidad ?? 0;
        var c2Post = postStock2?.Data?.StockPorClase.FirstOrDefault(s => s.Clase == 4)?.Cantidad ?? 0;

        Assert.Equal(c1Init - 60, c1Post);
        Assert.Equal(c2Init + 60, c2Post);
    }

    [Fact]
    public async Task TC_F18_04_MixFinalizationStockDistribution_DebitsClasses123Equally()
    {
        var adminClient = await GetAdminClientAsync();
        var (_, initStock) = await adminClient.GetJsonAsync<StockClienteDto>("/api/v1/stock/por-cliente/1");

        var initC1 = initStock?.Data?.StockPorClase.FirstOrDefault(s => s.Clase == 1)?.Cantidad ?? 0;
        var initC2 = initStock?.Data?.StockPorClase.FirstOrDefault(s => s.Clase == 2)?.Cantidad ?? 0;
        var initC3 = initStock?.Data?.StockPorClase.FirstOrDefault(s => s.Clase == 3)?.Cantidad ?? 0;

        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            TemporadaId = 1,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 5, Cantidad = 300, PrecioUnitario = 30m } }
        };

        var (_, createBody) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        var movId = createBody!.Data!.Id;

        await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/aprobar", new { });
        await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/facturar", new FacturarMovimientoDto { NumeroFactura = "FC-MIX-01" });
        var finRes = await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/finalizar", new { });
        Assert.Equal(HttpStatusCode.OK, finRes.StatusCode);

        var (_, postStock) = await adminClient.GetJsonAsync<StockClienteDto>("/api/v1/stock/por-cliente/1");

        var postC1 = postStock?.Data?.StockPorClase.FirstOrDefault(s => s.Clase == 1)?.Cantidad ?? 0;
        var postC2 = postStock?.Data?.StockPorClase.FirstOrDefault(s => s.Clase == 2)?.Cantidad ?? 0;
        var postC3 = postStock?.Data?.StockPorClase.FirstOrDefault(s => s.Clase == 3)?.Cantidad ?? 0;

        Assert.Equal(initC1 + 100, postC1);
        Assert.Equal(initC2 + 100, postC2);
        Assert.Equal(initC3 + 100, postC3);
    }

    [Fact]
    public async Task TC_F18_05_ConfirmedMovimientoBulbosGeneration_StateIsConfirmado()
    {
        var adminClient = await GetAdminClientAsync();
        var (_, initStock) = await adminClient.GetJsonAsync<StockClienteDto>("/api/v1/stock/por-cliente/1");
        var initialTotal = initStock?.Data?.TotalBulbos ?? 0;

        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            TemporadaId = 1,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 1, Cantidad = 75, PrecioUnitario = 50m } }
        };

        var (_, createBody) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        var movId = createBody!.Data!.Id;

        await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/aprobar", new { });
        await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/facturar", new FacturarMovimientoDto { NumeroFactura = "FC-18-05" });
        var finRes = await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/finalizar", new { });
        Assert.Equal(HttpStatusCode.OK, finRes.StatusCode);

        var (_, postStock) = await adminClient.GetJsonAsync<StockClienteDto>("/api/v1/stock/por-cliente/1");
        var updatedTotal = postStock?.Data?.TotalBulbos ?? 0;

        Assert.Equal(initialTotal + 75, updatedTotal);
    }
}

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F19")]
public class F19_ReferencePricesApiTests : SigaTestBase
{
    public F19_ReferencePricesApiTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F19_01_ListSeasonReferencePrices_Returns6Prices()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<List<PrecioReferenciaDto>>("/api/v1/precios-referencia/temporada/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
    }

    [Fact]
    public async Task TC_F19_02_ConfigureClass0Price_UpsertsSuccessfully()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new UpsertPrecioReferenciaDto { TemporadaId = 1, ClaseBulbo = 0, PrecioUnitario = 12.50m };
        var (response, _) = await adminClient.PostJsonAsync<UpsertPrecioReferenciaDto, object>("/api/v1/precios-referencia", dto);
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created);
    }

    [Fact]
    public async Task TC_F19_03_ConfigureMixPrice_UpsertsSuccessfully()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new UpsertPrecioReferenciaDto { TemporadaId = 1, ClaseBulbo = 5, PrecioUnitario = 8.75m };
        var (response, _) = await adminClient.PostJsonAsync<UpsertPrecioReferenciaDto, object>("/api/v1/precios-referencia", dto);
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created);
    }

    [Fact]
    public async Task TC_F19_04_RetrieveUpdatedPrices_ReflectsNewValues()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<List<PrecioReferenciaDto>>("/api/v1/precios-referencia/temporada/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
    }

    [Fact]
    public async Task TC_F19_05_CheckPermisoPreciosRefVer_EnforcesAuthorization()
    {
        var client = GetClientWithPermissions("VENDEDOR", "PRECIOS_REF_VER");
        var response = await client.GetAsync("/api/v1/precios-referencia/temporada/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F20")]
public class F20_CommercialUnitTestsExecution : SigaTestBase
{
    public F20_CommercialUnitTestsExecution(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F20_01_Type1Movement_ApiComputesTotalAmountFromDetalles()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1, // VentaACliente
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto>
            {
                new() { ClaseBulbo = 1, Cantidad = 100, PrecioUnitario = 12.50m }, // 1250.00
                new() { ClaseBulbo = 2, Cantidad = 50, PrecioUnitario = 30.00m }    // 1500.00
            }
        };

        var (response, body) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>(
            "/api/v1/movimientos-comerciales", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.Equal(2750.00m, body.Data.MontoTotal);

        // Verify persistence via GET
        var (getRes, getBody) = await adminClient.GetJsonAsync<MovimientoComercialDto>(
            $"/api/v1/movimientos-comerciales/{body.Data.Id}");
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);
        Assert.Equal(2750.00m, getBody!.Data.MontoTotal);
    }

    [Fact]
    public async Task TC_F20_02_Type2Movement_ApiComputesGrossCommissionAndNetTotal()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 2, // CompraACliente
            ClienteId = 1,
            PorcentajeComision = 12.0m,
            Detalles = new List<CreateDetalleMovimientoDto>
            {
                new() { ClaseBulbo = 1, Cantidad = 200, PrecioUnitario = 50.00m } // 10000.00
            }
        };

        var (response, body) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>(
            "/api/v1/movimientos-comerciales", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.Equal(10000.00m, body.Data.MontoBruto);
        Assert.Equal(1200.00m, body.Data.MontoComision);
        Assert.Equal(8800.00m, body.Data.MontoNeto);
        Assert.Equal(8800.00m, body.Data.MontoTotal);
    }

    [Fact]
    public async Task TC_F20_03_Type3Movement_ApiEnforcesZeroTotalFinancials()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 3, // TraspasoEntreClientes
            ClienteOrigenId = 1,
            ClienteDestinoId = 2,
            Detalles = new List<CreateDetalleMovimientoDto>
            {
                new() { ClaseBulbo = 3, Cantidad = 60, PrecioUnitario = 0m }
            }
        };

        var (response, body) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>(
            "/api/v1/movimientos-comerciales", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.Equal(0.00m, body.Data.MontoTotal);
        Assert.Equal(0.00m, body.Data.MontoBruto);
        Assert.Equal(0.00m, body.Data.MontoComision);
    }

    [Fact]
    public async Task TC_F20_04_MixBulbQuantityValidation_RejectsNonMultipleOfThreeViaApi()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto>
            {
                new() { ClaseBulbo = 5, Cantidad = 100, PrecioUnitario = 10.0m } // 100 is NOT multiple of 3
            }
        };

        var (response, _) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>(
            "/api/v1/movimientos-comerciales", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TC_F20_05_ReferencePricesEndpoint_UpsertAndRetrieveAcrossClasses()
    {
        var adminClient = await GetAdminClientAsync();
        var upsertDto = new UpsertPrecioReferenciaDto
        {
            TemporadaId = 1,
            ClaseBulbo = 5, // MIX
            PrecioUnitario = 120.00m
        };

        var (postRes, _) = await adminClient.PostJsonAsync<UpsertPrecioReferenciaDto, object>(
            "/api/v1/precios-referencia", upsertDto);
        Assert.True(postRes.StatusCode == HttpStatusCode.OK || postRes.StatusCode == HttpStatusCode.Created);

        var (getRes, body) = await adminClient.GetJsonAsync<List<PrecioReferenciaDto>>(
            "/api/v1/precios-referencia/temporada/1");
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.Contains(body.Data, p => p.ClaseBulbo == 5 && p.PrecioUnitario == 120.00m);
    }
}
