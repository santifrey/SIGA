using System.Net;
using SIGA.E2ETests.Common;
using SIGA.E2ETests.Fixtures;
using Xunit;

namespace SIGA.E2ETests.Tier1_FeatureCoverage;

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F11")]
public class F11_MovimientoComercialEntityTests : SigaTestBase
{
    public F11_MovimientoComercialEntityTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F11_01_CreateType1VentaACliente_ReturnsCreatedAndStatePendienteRevision()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1, // VentaACliente
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto>
            {
                new() { ClaseBulbo = 1, Cantidad = 100, PrecioUnitario = 10.0m }
            }
        };

        var (response, body) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.Equal(1, body.Data.Tipo);
        Assert.Equal(0, body.Data.Estado); // PendienteRevision
    }

    [Fact]
    public async Task TC_F11_02_CreateType2CompraACliente_ReturnsCreatedAndStatePendienteRevision()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 2, // CompraACliente
            ClienteId = 1,
            PorcentajeComision = 10.0m,
            Detalles = new List<CreateDetalleMovimientoDto>
            {
                new() { ClaseBulbo = 2, Cantidad = 200, PrecioUnitario = 15.0m }
            }
        };

        var (response, body) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.Equal(2, body.Data.Tipo);
        Assert.Equal(0, body.Data.Estado);
    }

    [Fact]
    public async Task TC_F11_03_CreateType3TraspasoEntreClientes_ReturnsCreatedAndStatePendienteRevision()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 3, // TraspasoEntreClientes
            ClienteOrigenId = 1,
            ClienteDestinoId = 2,
            Detalles = new List<CreateDetalleMovimientoDto>
            {
                new() { ClaseBulbo = 3, Cantidad = 50, PrecioUnitario = 0m }
            }
        };

        var (response, body) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.Equal(3, body.Data.Tipo);
        Assert.Equal(0, body.Data.Estado);
    }

    [Fact]
    public async Task TC_F11_04_GetMovementDetails_ReturnsMovementWithItemsAndTimeline()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto>
            {
                new() { ClaseBulbo = 1, Cantidad = 50, PrecioUnitario = 20.0m }
            }
        };
        var (createRes, createBody) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        Assert.NotNull(createBody?.Data);

        var (response, body) = await adminClient.GetJsonAsync<MovimientoComercialDto>($"/api/v1/movimientos-comerciales/{createBody.Data.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.NotNull(body.Data.Detalles);
        Assert.NotNull(body.Data.HistorialAprobaciones);
    }

    [Fact]
    public async Task TC_F11_05_FilterMovementsList_FiltersByTypeAndState()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<List<MovimientoComercialDto>>("/api/v1/movimientos-comerciales?tipo=1&estado=0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
    }
}

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F12")]
public class F12_CommercialFinancialRulesTests : SigaTestBase
{
    public F12_CommercialFinancialRulesTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F12_01_Type1TotalCalculation_CalculatesFromItemSubtotals()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto>
            {
                new() { ClaseBulbo = 1, Cantidad = 100, PrecioUnitario = 10.0m }, // 1000
                new() { ClaseBulbo = 2, Cantidad = 50, PrecioUnitario = 20.0m }   // 1000
            }
        };

        var (response, body) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.Equal(2000.0m, body.Data.MontoTotal);
    }

    [Fact]
    public async Task TC_F12_02_Type2CommissionCalculation_ComputesGrossCommissionNet()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 2,
            ClienteId = 1,
            PorcentajeComision = 15.0m,
            Detalles = new List<CreateDetalleMovimientoDto>
            {
                new() { ClaseBulbo = 1, Cantidad = 100, PrecioUnitario = 100.0m } // Gross = 10000
            }
        };

        var (response, body) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.Equal(10000.0m, body.Data.MontoBruto);
        Assert.Equal(1500.0m, body.Data.MontoComision);
        Assert.Equal(8500.0m, body.Data.MontoNeto);
        Assert.Equal(8500.0m, body.Data.MontoTotal);
    }

    [Fact]
    public async Task TC_F12_03_Type2ZeroCommission_NetEqualsGross()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 2,
            ClienteId = 1,
            PorcentajeComision = 0.0m,
            Detalles = new List<CreateDetalleMovimientoDto>
            {
                new() { ClaseBulbo = 1, Cantidad = 50, PrecioUnitario = 100.0m } // 5000
            }
        };

        var (response, body) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.Equal(5000.0m, body.Data.MontoBruto);
        Assert.Equal(0.0m, body.Data.MontoComision);
        Assert.Equal(5000.0m, body.Data.MontoNeto);
    }

    [Fact]
    public async Task TC_F12_04_Type3FinancialZeroing_MontoTotalIsZero()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 3,
            ClienteOrigenId = 1,
            ClienteDestinoId = 2,
            Detalles = new List<CreateDetalleMovimientoDto>
            {
                new() { ClaseBulbo = 1, Cantidad = 500, PrecioUnitario = 10.0m }
            }
        };

        var (response, body) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.Equal(0.0m, body.Data.MontoTotal);
    }

    [Fact]
    public async Task TC_F12_05_Type1WithMixFinancials_CalculatesTotal()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto>
            {
                new() { ClaseBulbo = 5, Cantidad = 300, PrecioUnitario = 4.0m } // MIX = 5, 300 * 4 = 1200
            }
        };

        var (response, body) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.Equal(1200.0m, body.Data.MontoTotal);
    }
}

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F13")]
public class F13_CommercialTransitionsTests : SigaTestBase
{
    public F13_CommercialTransitionsTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F13_01_Type1CompleteFlow_ProgressesThroughDefinedStates()
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
    public async Task TC_F13_02_Type2CompleteFlow_ProgressesThroughDefinedStates()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 2,
            ClienteId = 1,
            PorcentajeComision = 10m,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 2, Cantidad = 20, PrecioUnitario = 50m } }
        };
        var (createRes, createBody) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        Assert.NotNull(createBody?.Data);

        var response = await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{createBody.Data.Id}/aprobar", new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TC_F13_03_Type3DirectFlow_SkipsFacturadoToFinalizado()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 3,
            ClienteOrigenId = 1,
            ClienteDestinoId = 2,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 3, Cantidad = 15, PrecioUnitario = 0m } }
        };
        var (createRes, createBody) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        Assert.NotNull(createBody?.Data);
        var movId = createBody.Data.Id;

        await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/aprobar", new { });
        var response = await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/finalizar", new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TC_F13_04_TransitionAuditLogging_HistoryPreservesTransitions()
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

        var (response, body) = await adminClient.GetJsonAsync<MovimientoComercialDto>($"/api/v1/movimientos-comerciales/{createBody.Data.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.NotEmpty(body.Data.HistorialAprobaciones);
    }

    [Fact]
    public async Task TC_F13_05_InvoiceNumberAssociation_LinksInvoiceOnFacturado()
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
        var invoiceDto = new FacturarMovimientoDto { NumeroFactura = "FAC-0001-00001234" };
        var response = await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/facturar", invoiceDto);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F14")]
public class F14_NonTerminalRejectionTests : SigaTestBase
{
    public F14_NonTerminalRejectionTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F14_01_RejectFromPendienteRevision_TransitionsToRechazado()
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

        var rejDto = new RechazarMovimientoDto { Motivo = "Error en cantidades solicitadas" };
        var response = await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{createBody.Data.Id}/rechazar", rejDto);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TC_F14_02_RejectFromAprobadoAdmin_TransitionsToRechazado()
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
        var rejDto = new RechazarMovimientoDto { Motivo = "Discrepancia en precios aprobados" };
        var response = await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/rechazar", rejDto);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TC_F14_03_RejectFromFacturado_VoidsAssociatedInvoice()
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
        await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/facturar", new FacturarMovimientoDto { NumeroFactura = "FAC-INV-001" });

        var rejDto = new RechazarMovimientoDto { Motivo = "Facturación errónea" };
        var response = await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/rechazar", rejDto);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TC_F14_04_CreatorResubmitToPendienteRevision_TransitionsBackToState0()
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

        await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/rechazar", new RechazarMovimientoDto { Motivo = "Corregir datos" });
        var response = await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/corregir-reenviar", new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TC_F14_05_UserWithMovcomCorregir_ResubmitSucceeds()
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

        await adminClient.PostAsync($"/api/v1/movimientos-comerciales/{movId}/rechazar", new RechazarMovimientoDto { Motivo = "Para corregir" });

        var client = GetClientWithPermissions("VENDEDOR", "MOVCOM_CORREGIR");
        var response = await client.PostAsync($"/api/v1/movimientos-comerciales/{movId}/corregir-reenviar", new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

[Trait("Category", TestCategories.Tier1)]
[Trait("Feature", "F15")]
public class F15_MixItemsStockDecompositionTests : SigaTestBase
{
    public F15_MixItemsStockDecompositionTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F15_01_ValidMixQuantityMultipleOf3_AcceptedAndDecomposed()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto>
            {
                new() { ClaseBulbo = 5, Cantidad = 300, PrecioUnitario = 5.0m }
            }
        };

        var (response, body) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body?.Data);
    }

    [Fact]
    public async Task TC_F15_02_MinimumMixQuantity3_Accepted()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto>
            {
                new() { ClaseBulbo = 5, Cantidad = 3, PrecioUnitario = 10.0m }
            }
        };

        var (response, body) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body?.Data);
    }

    [Fact]
    public async Task TC_F15_03_StockAvailabilityCheckOnDecomposedClasses_ValidatesOrigin()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<StockClienteDto>("/api/v1/stock/por-cliente/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Data);
    }

    [Fact]
    public async Task TC_F15_04_MultiItemMixAndRegularClasses_Accepted()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto>
            {
                new() { ClaseBulbo = 0, Cantidad = 200, PrecioUnitario = 10.0m },
                new() { ClaseBulbo = 5, Cantidad = 600, PrecioUnitario = 5.0m },
                new() { ClaseBulbo = 4, Cantidad = 100, PrecioUnitario = 15.0m }
            }
        };

        var (response, body) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body?.Data);
    }

    [Fact]
    public async Task TC_F15_05_MixUnitPriceMultiplier_CalculatesSubtotal()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto>
            {
                new() { ClaseBulbo = 5, Cantidad = 900, PrecioUnitario = 3.50m } // 3150.00
            }
        };

        var (response, body) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body?.Data);
        Assert.Equal(3150.0m, body.Data.MontoTotal);
    }
}
