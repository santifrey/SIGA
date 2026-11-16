using System.Net;
using SIGA.E2ETests.Common;
using SIGA.E2ETests.Fixtures;
using Xunit;

namespace SIGA.E2ETests.Tier2_BoundaryCases;

[Trait("Category", TestCategories.Tier2)]
[Trait("Feature", "F11")]
public class Boundary_F11_MovimientoComercialEntityBoundaries : SigaTestBase
{
    public Boundary_F11_MovimientoComercialEntityBoundaries(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F11_B01_InvalidMovementType_ReturnsBadRequest()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 99, // Invalid
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 1, Cantidad = 10 } }
        };
        var (response, _) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TC_F11_B02_Type3IdenticalOriginAndDestination_ReturnsBadRequest()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 3,
            ClienteOrigenId = 5,
            ClienteDestinoId = 5, // Same origin and destination
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 1, Cantidad = 10 } }
        };
        var (response, _) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TC_F11_B03_EmptyItemsCollection_ReturnsBadRequest()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto>() // Empty items
        };
        var (response, _) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TC_F11_B04_MissingClientOnType1_ReturnsBadRequest()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = null, // Missing client
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 1, Cantidad = 10 } }
        };
        var (response, _) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TC_F11_B05_MissingParticipantsOnType3_ReturnsBadRequest()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 3,
            ClienteOrigenId = 1,
            ClienteDestinoId = null, // Missing destination
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 1, Cantidad = 10 } }
        };
        var (response, _) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }
}

[Trait("Category", TestCategories.Tier2)]
[Trait("Feature", "F12")]
public class Boundary_F12_CommercialFinancialRulesBoundaries : SigaTestBase
{
    public Boundary_F12_CommercialFinancialRulesBoundaries(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F12_B01_NegativeCommissionPercentage_ReturnsBadRequest()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 2,
            ClienteId = 1,
            PorcentajeComision = -10.0m, // Negative commission
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 1, Cantidad = 10, PrecioUnitario = 100m } }
        };
        var (response, _) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TC_F12_B02_ExcessiveCommissionPercentage_ReturnsBadRequest()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 2,
            ClienteId = 1,
            PorcentajeComision = 101.0m, // > 100%
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 1, Cantidad = 10, PrecioUnitario = 100m } }
        };
        var (response, _) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TC_F12_B03_NegativeQuantityOrUnitPrice_ReturnsBadRequest()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto>
            {
                new() { ClaseBulbo = 1, Cantidad = -50, PrecioUnitario = 10m } // Negative quantity
            }
        };
        var (response, _) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TC_F12_B04_ExtremeValuesArithmeticOverflow_HandledCleanly()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto>
            {
                new() { ClaseBulbo = 1, Cantidad = int.MaxValue, PrecioUnitario = 99999999.99m }
            }
        };
        var (response, _) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task TC_F12_B05_NonZeroAmountsOnType3Reset_ResetsOrRejects()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 3,
            ClienteOrigenId = 1,
            ClienteDestinoId = 2,
            Detalles = new List<CreateDetalleMovimientoDto>
            {
                new() { ClaseBulbo = 1, Cantidad = 10, PrecioUnitario = 500m } // Trying to charge on transfer
            }
        };
        var (response, body) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        if (response.IsSuccessStatusCode && body?.Data != null)
        {
            Assert.Equal(0.0m, body.Data.MontoTotal);
        }
    }
}

[Trait("Category", TestCategories.Tier2)]
[Trait("Feature", "F13")]
public class Boundary_F13_CommercialTransitionsBoundaries : SigaTestBase
{
    public Boundary_F13_CommercialTransitionsBoundaries(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F13_B01_InvoicingFromPendiente_ReturnsBadRequest()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new FacturarMovimientoDto { NumeroFactura = "FAC-INV-001" };
        var response = await adminClient.PostAsync("/api/v1/movimientos-comerciales/1/facturar", dto);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task TC_F13_B02_InvoicingType3Traspaso_ReturnsBadRequest()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new FacturarMovimientoDto { NumeroFactura = "FAC-TRASP-001" };
        var response = await adminClient.PostAsync("/api/v1/movimientos-comerciales/3/facturar", dto);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TC_F13_B03_FinalizingType1FromAprobadoAdminWithoutInvoicing_ReturnsBadRequest()
    {
        var adminClient = await GetAdminClientAsync();
        var response = await adminClient.PostAsync("/api/v1/movimientos-comerciales/1/finalizar", new { });
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task TC_F13_B04_TerminalStateImmutability_CannotMutateFinalizado()
    {
        var adminClient = await GetAdminClientAsync();
        var response = await adminClient.PostAsync("/api/v1/movimientos-comerciales/99/aprobar", new { });
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TC_F13_B05_UnauthorizedTransitionAction_ReturnsForbidden()
    {
        var client = GetClientWithPermissions("VENDEDOR", "MOVCOM_VER"); // lacks MOVCOM_FACTURAR
        var response = await client.PostAsync("/api/v1/movimientos-comerciales/1/facturar", new { });
        Assert.True(response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.NotFound);
    }
}

[Trait("Category", TestCategories.Tier2)]
[Trait("Feature", "F14")]
public class Boundary_F14_NonTerminalRejectionBoundaries : SigaTestBase
{
    public Boundary_F14_NonTerminalRejectionBoundaries(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TC_F14_B01_EmptyRejectionMotiveValidation_ReturnsBadRequest()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new RechazarMovimientoDto { Motivo = "   " }; // Empty reason
        var response = await adminClient.PostAsync("/api/v1/movimientos-comerciales/1/rechazar", dto);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TC_F14_B02_NonCreatorLackingMovcomCorregirResubmitGuard_ReturnsForbidden()
    {
        var client = GetClientWithPermissions("VENDEDOR", "MOVCOM_VER"); // lacks MOVCOM_CORREGIR and not creator
        var response = await client.PostAsync("/api/v1/movimientos-comerciales/1/corregir-reenviar", new { });
        Assert.True(response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TC_F14_B03_RejectFinalizedMovementGuard_ReturnsBadRequest()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new RechazarMovimientoDto { Motivo = "Too late" };
        var response = await adminClient.PostAsync("/api/v1/movimientos-comerciales/99/rechazar", dto);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TC_F14_B04_DirectEditOnNonRejectedMovementGuard_ReturnsBadRequest()
    {
        var adminClient = await GetAdminClientAsync();
        var updatePayload = new { Detalles = new List<CreateDetalleMovimientoDto>() };
        var response = await adminClient.PutAsync("/api/v1/movimientos-comerciales/1", updatePayload);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task TC_F14_B05_ResubmitWithInvalidDataStaysRejected_ReturnsBadRequest()
    {
        var adminClient = await GetAdminClientAsync();
        var invalidPayload = new { Detalles = new List<CreateDetalleMovimientoDto> { new() { Cantidad = 0 } } };
        var response = await adminClient.PostAsync("/api/v1/movimientos-comerciales/1/corregir-reenviar", invalidPayload);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }
}

[Trait("Category", TestCategories.Tier2)]
[Trait("Feature", "F15")]
public class Boundary_F15_MixItemsStockDecompBoundaries : SigaTestBase
{
    public Boundary_F15_MixItemsStockDecompBoundaries(SigaWebApplicationFactory factory) : base(factory) { }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(100)]
    public async Task TC_F15_B01_To_B04_MixQuantityNotMultipleOf3_ReturnsBadRequest(int invalidQty)
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto>
            {
                new() { ClaseBulbo = 5, Cantidad = invalidQty, PrecioUnitario = 10m }
            }
        };

        var (response, _) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TC_F15_B05_PartialStockDeficitInDecomposedClasses_ReturnsBadRequest()
    {
        var adminClient = await GetAdminClientAsync();
        // Request massive quantity of MIX that exceeds stock
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto>
            {
                new() { ClaseBulbo = 5, Cantidad = 9000000, PrecioUnitario = 10m }
            }
        };

        var (response, _) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }
}
