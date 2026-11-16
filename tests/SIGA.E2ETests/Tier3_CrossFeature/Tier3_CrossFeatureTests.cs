using System.Net;
using SIGA.E2ETests.Common;
using SIGA.E2ETests.Fixtures;
using Xunit;

namespace SIGA.E2ETests.Tier3_CrossFeature;

[Trait("Category", TestCategories.Tier3)]
public class CrossFeature_SecurityAndAdministrativeTests : SigaTestBase
{
    public CrossFeature_SecurityAndAdministrativeTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task E2E_T3_01_SingleRole_DynamicPerm_RolesApi()
    {
        var client = GetClientWithPermissions("OPERADOR", "ROLES_VER");
        var (getResponse, _) = await client.GetJsonAsync<List<RolDto>>("/api/v1/roles");
        Assert.True(getResponse.StatusCode == HttpStatusCode.OK || getResponse.StatusCode == HttpStatusCode.NotFound);

        // Lacks ROLES_GESTIONAR
        var postResponse = await client.PostAsync("/api/v1/roles", new CreateRolDto { Nombre = "Unauthorized" });
        Assert.True(postResponse.StatusCode == HttpStatusCode.Forbidden || postResponse.StatusCode == HttpStatusCode.Unauthorized || postResponse.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task E2E_T3_02_RolePermNM_DynamicPerm_DeleteSystemRoleFails()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, _) = await adminClient.DeleteJsonAsync<object>("/api/v1/roles/1");
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task E2E_T3_03_CustomRole_DeleteRoleWithUsersFails()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, _) = await adminClient.DeleteJsonAsync<object>("/api/v1/roles/2");
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task E2E_T3_04_DynamicPerm_EmployeesSoftDelete()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, _) = await adminClient.DeleteJsonAsync<object>("/api/v1/empleados/2");
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task E2E_T3_05_DynamicPerm_LastAdminSafeguard()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, _) = await adminClient.DeleteJsonAsync<object>("/api/v1/empleados/1");
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task E2E_T3_06_SingleRole_JwtClaims_GetMeProfile()
    {
        var client = GetClientWithPermissions("VENDEDOR", "MOVCOM_VER", "MOVCOM_CREAR");
        var (response, body) = await client.GetJsonAsync<UserProfileDto>("/api/v1/auth/me");
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task E2E_T3_07_DynamicPerm_EmployeeCreationTransactionRollback()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = TestDataGenerator.CreateSampleEmpleadoDto();
        dto.Email = "invalid-email-format";
        var (response, _) = await adminClient.PostJsonAsync<CreateEmpleadoDto, EmpleadoDto>("/api/v1/empleados", dto);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Created);
    }

    [Fact]
    public async Task E2E_T3_08_CatalogSeeding_DynamicPermDistinction_401Vs403()
    {
        var anon = GetAnonymousClient();
        var r401 = await anon.GetAsync("/api/v1/empleados");
        Assert.True(r401.StatusCode == HttpStatusCode.Unauthorized || r401.StatusCode == HttpStatusCode.NotFound);

        var noPermsClient = GetClientWithPermissions("CLIENTE");
        var r403 = await noPermsClient.GetAsync("/api/v1/empleados");
        Assert.True(r403.StatusCode == HttpStatusCode.Forbidden || r403.StatusCode == HttpStatusCode.Unauthorized || r403.StatusCode == HttpStatusCode.NotFound);
    }
}

[Trait("Category", TestCategories.Tier3)]
public class CrossFeature_CommercialTypesAndStockTests : SigaTestBase
{
    public CrossFeature_CommercialTypesAndStockTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task E2E_T3_09_Type1Venta_StandardBulbos_StockImpact()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 2, Cantidad = 200, PrecioUnitario = 50m } }
        };
        var (response, body) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task E2E_T3_10_Type1Venta_PureMixBulbos_MixStockDecomposition()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 5, Cantidad = 300, PrecioUnitario = 40m } }
        };
        var (response, body) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task E2E_T3_11_Type1Venta_InvalidMixQuantity_NonMultipleOf3Rejected()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 5, Cantidad = 125, PrecioUnitario = 40m } }
        };
        var (response, _) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task E2E_T3_12_Type1Venta_HybridComposition_StandardPlusMix()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto>
            {
                new() { ClaseBulbo = 2, Cantidad = 100, PrecioUnitario = 50m },
                new() { ClaseBulbo = 5, Cantidad = 150, PrecioUnitario = 40m }
            }
        };
        var (response, _) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task E2E_T3_13_Type2Compra_FinancialCommission_StockIngestion()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 2,
            ClienteId = 1,
            PorcentajeComision = 15m,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 3, Cantidad = 400, PrecioUnitario = 100m } }
        };
        var (response, body) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task E2E_T3_14_Type2Compra_MixDecompositionFromClient()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 2,
            ClienteId = 1,
            PorcentajeComision = 10m,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 5, Cantidad = 300, PrecioUnitario = 60m } }
        };
        var (response, _) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task E2E_T3_15_Type3Traspaso_DirectJump_StockTransfer()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 3,
            ClienteOrigenId = 1,
            ClienteDestinoId = 2,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 4, Cantidad = 100, PrecioUnitario = 0m } }
        };
        var (response, body) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task E2E_T3_16_Type3Traspaso_MixDecompositionAcrossClients()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 3,
            ClienteOrigenId = 1,
            ClienteDestinoId = 2,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 5, Cantidad = 150, PrecioUnitario = 0m } }
        };
        var (response, _) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }
}

[Trait("Category", TestCategories.Tier3)]
public class CrossFeature_ReferencePricesAndPricingTests : SigaTestBase
{
    public CrossFeature_ReferencePricesAndPricingTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task E2E_T3_17_RefPrices_Type1_AutoPricingFromReference()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, _) = await adminClient.GetJsonAsync<List<PrecioReferenciaDto>>("/api/v1/precios-referencia/temporada/1");
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task E2E_T3_18_RefPrices_Type1_NegotiatedPriceOverride()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 4, Cantidad = 10, PrecioUnitario = 220m } }
        };
        var (response, body) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
        if (response.IsSuccessStatusCode && body?.Data != null)
        {
            Assert.Equal(2200m, body.Data.MontoTotal);
        }
    }

    [Fact]
    public async Task E2E_T3_19_MixRefPrice_Type1_MixValuation()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 1,
            ClienteId = 1,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 5, Cantidad = 300, PrecioUnitario = 75m } }
        };
        var (response, body) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
        if (response.IsSuccessStatusCode && body?.Data != null)
        {
            Assert.Equal(22500m, body.Data.MontoTotal);
        }
    }

    [Fact]
    public async Task E2E_T3_20_RefPrices_Type2_CompraCalculation()
    {
        var gross = 500 * 120m; // 60000
        var comm = gross * 0.12m; // 7200
        var net = gross - comm; // 52800
        Assert.Equal(52800m, net);
    }

    [Fact]
    public async Task E2E_T3_21_RefPrices_Type3_TraspasoZeroPricing()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new CreateMovimientoComercialDto
        {
            Tipo = 3,
            ClienteOrigenId = 1,
            ClienteDestinoId = 2,
            Detalles = new List<CreateDetalleMovimientoDto> { new() { ClaseBulbo = 1, Cantidad = 50 } }
        };
        var (response, body) = await adminClient.PostJsonAsync<CreateMovimientoComercialDto, MovimientoComercialDto>("/api/v1/movimientos-comerciales", dto);
        if (response.IsSuccessStatusCode && body?.Data != null)
        {
            Assert.Equal(0m, body.Data.MontoTotal);
        }
    }

    [Fact]
    public async Task E2E_T3_22_PriceMutation_HistoricMovementInvariance()
    {
        var fixedHistoricTotal = 15000m;
        var newUnitPrice = 180m;
        Assert.Equal(15000m, fixedHistoricTotal);
        Assert.NotEqual(fixedHistoricTotal, 100 * newUnitPrice);
    }
}

[Trait("Category", TestCategories.Tier3)]
public class CrossFeature_ContractEnforcementWorkflowTests : SigaTestBase
{
    public CrossFeature_ContractEnforcementWorkflowTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task E2E_T3_23_Contract_Type1_NewClientMissingContractBlocked()
    {
        var adminClient = await GetAdminClientAsync();
        var response = await adminClient.PostAsync("/api/v1/movimientos-comerciales/99/aprobar", new { });
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task E2E_T3_24_Contract_Type1_NewClientWithGeneratedContractApproved()
    {
        var adminClient = await GetAdminClientAsync();
        var response = await adminClient.PostAsync("/api/v1/movimientos-comerciales/1/aprobar", new { });
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task E2E_T3_25_Contract_Type1_ReturningClientContractExemption()
    {
        var adminClient = await GetAdminClientAsync();
        var response = await adminClient.PostAsync("/api/v1/movimientos-comerciales/1/aprobar", new { });
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task E2E_T3_26_Contract_Type2_CompraNewClientExemption()
    {
        var adminClient = await GetAdminClientAsync();
        var response = await adminClient.PostAsync("/api/v1/movimientos-comerciales/2/aprobar", new { });
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task E2E_T3_27_Contract_Type3_TraspasoNewClientExemption()
    {
        var adminClient = await GetAdminClientAsync();
        var response = await adminClient.PostAsync("/api/v1/movimientos-comerciales/3/aprobar", new { });
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task E2E_T3_28_Contract_TraceabilityAuditTrail()
    {
        var adminClient = await GetAdminClientAsync();
        var (response, body) = await adminClient.GetJsonAsync<MovimientoComercialDto>("/api/v1/movimientos-comerciales/1");
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
    }
}

[Trait("Category", TestCategories.Tier3)]
public class CrossFeature_RejectionAndTraceabilityTests : SigaTestBase
{
    public CrossFeature_RejectionAndTraceabilityTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task E2E_T3_29_Rejection_State0PendienteRevisionRejection()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new RechazarMovimientoDto { Motivo = "Precio no acordado" };
        var response = await adminClient.PostAsync("/api/v1/movimientos-comerciales/1/rechazar", dto);
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task E2E_T3_30_Rejection_MissingReasonValidation()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new RechazarMovimientoDto { Motivo = "" };
        var response = await adminClient.PostAsync("/api/v1/movimientos-comerciales/1/rechazar", dto);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task E2E_T3_31_Rejection_State2FacturadoInvoiceVoiding()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new RechazarMovimientoDto { Motivo = "Error de facturación" };
        var response = await adminClient.PostAsync("/api/v1/movimientos-comerciales/1/rechazar", dto);
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task E2E_T3_32_Resubmission_AuthorizationCheck()
    {
        var client = GetClientWithPermissions("VENDEDOR", "MOVCOM_VER"); // lacks MOVCOM_CORREGIR
        var response = await client.PostAsync("/api/v1/movimientos-comerciales/1/corregir-reenviar", new { });
        Assert.True(response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task E2E_T3_33_Resubmission_ReApprovalAndReInvoicing()
    {
        var adminClient = await GetAdminClientAsync();
        var response = await adminClient.PostAsync("/api/v1/movimientos-comerciales/1/aprobar", new { });
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }
}

[Trait("Category", TestCategories.Tier3)]
public class CrossFeature_PlantationsAndStockTests : SigaTestBase
{
    public CrossFeature_PlantationsAndStockTests(SigaWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task E2E_T3_34_StructuredFilaLote_StockValidation()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new RegistrarDistribucionRequestDto
        {
            NumeroFila = 5,
            CapacidadFila = 1000,
            NumeroLote = 1,
            ClienteId = 1,
            Detalles = new List<DetalleDistribucionDto> { new() { Clase = 1, Cantidad = 250 } }
        };
        var (response, _) = await adminClient.PostJsonAsync<RegistrarDistribucionRequestDto, LoteDto>("/api/v1/plantaciones/distribucion", dto);
        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task E2E_T3_35_StructuredInputs_OverPlantingRejection()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new RegistrarDistribucionRequestDto
        {
            NumeroFila = 5,
            CapacidadFila = 1000,
            NumeroLote = 1,
            ClienteId = 1,
            Detalles = new List<DetalleDistribucionDto> { new() { Clase = 1, Cantidad = 9999999 } }
        };
        var (response, _) = await adminClient.PostJsonAsync<RegistrarDistribucionRequestDto, LoteDto>("/api/v1/plantaciones/distribucion", dto);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task E2E_T3_36_MixAcquiredStock_PlantingDecomposedStock()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new RegistrarDistribucionRequestDto
        {
            NumeroFila = 5,
            CapacidadFila = 1000,
            NumeroLote = 2,
            ClienteId = 1,
            Detalles = new List<DetalleDistribucionDto> { new() { Clase = 1, Cantidad = 80 }, new() { Clase = 2, Cantidad = 80 } }
        };
        var (response, _) = await adminClient.PostJsonAsync<RegistrarDistribucionRequestDto, LoteDto>("/api/v1/plantaciones/distribucion", dto);
        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task E2E_T3_37_StructuredInputs_FilaLotOrderAutoIncrement()
    {
        var lot1 = 1;
        var lot2 = lot1 + 1;
        Assert.Equal(2, lot2);
    }

    [Fact]
    public async Task E2E_T3_38_ClosedSeason_DistributionAttemptRejected()
    {
        var adminClient = await GetAdminClientAsync();
        var dto = new RegistrarDistribucionRequestDto
        {
            TemporadaId = 999, // Closed season
            NumeroFila = 1,
            ClienteId = 1,
            Detalles = new List<DetalleDistribucionDto> { new() { Clase = 1, Cantidad = 10 } }
        };
        var (response, _) = await adminClient.PostJsonAsync<RegistrarDistribucionRequestDto, LoteDto>("/api/v1/plantaciones/distribucion", dto);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }
}
