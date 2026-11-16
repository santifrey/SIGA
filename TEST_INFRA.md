# SIGA Test Infrastructure & Architecture Specification (`TEST_INFRA.md`)

## 1. Test Philosophy & Core Principles

The End-to-End (E2E) testing suite for SIGA ("Iteración SIGA") is designed to enforce opaque-box, requirement-driven verification across the entire application stack:
1. **Opaque-Box Boundary Evaluation**: Tests interact with the system strictly across external HTTP network interfaces (`HttpClient` interacting with `WebApplicationFactory<Program>`) and validate observable outcomes (HTTP status codes, standard `ApiResponse<T>` envelopes, persisted database states, and frontend module contracts). No internal business logic, database queries, or domain handlers are mocked.
2. **Deterministic & Isolated Execution**: Every test executes against an isolated SQLite test database environment seeded with system roles, catalog permissions, and bootstrap administrative credentials via `DatabaseSeeder.InitializeDatabaseAsync`. Parallel execution is serialized to avoid SQLite file locking.
3. **Requirement-Driven Verification**: Test cases are strictly derived from `PROJECT.md`, `ORIGINAL_REQUEST.md`, and `.agents/teamwork/e2e_spec_miner_1/report.md`. No test is designed around incidental implementation details.
4. **4-Tier Testing Methodology**:
   - **Tier 1 (Feature Coverage)**: Happy path tests asserting primary behaviors for each of the 34 features in isolation (>= 5 tests per feature = 170 tests).
   - **Tier 2 (Boundary & Corner Cases)**: Negative inputs, boundary constraints, invalid states, malformed tokens, role protection, and error statuses (400, 401, 403, 404, 409) (>= 5 tests per feature = 170 tests).
   - **Tier 3 (Cross-Feature Combinations)**: Pairwise module interactions across Security, Commercial Movements, Stock, and Field Plantations (34 tests).
   - **Tier 4 (Real-World Application Scenarios)**: Multi-step enterprise business workflows traversing multiple actors, lifecycles, and state transitions (17 tests).

---

## 2. Feature Inventory Mapping (Features 1 to 34)

| # | Feature Name | Tier 1 (Coverage) | Tier 2 (Boundaries) | Tier 3 (Combinations) | Tier 4 (Workflows) |
|---|--------------|-------------------|---------------------|-----------------------|--------------------|
| 1 | Single-Role User Model | T1-F01 (1..5) | T2-F01 (1..5) | T3-01, T3-32 | T4-05, T4-06 |
| 2 | Role-Permission Model | T1-F02 (1..5) | T2-F02 (1..5) | T3-02 | T4-05 |
| 3 | Permissions Catalog & Seeding | T1-F03 (1..5) | T2-F03 (1..5) | T3-31 | T4-05, T4-17 |
| 4 | Dynamic Permission Authorization | T1-F04 (1..5) | T2-F04 (1..5) | T3-02, T3-03, T3-07 | T4-05, T4-13 |
| 5 | Single-Role & Permission JWT | T1-F05 (1..5) | T2-F05 (1..5) | T3-03, T3-31 | T4-05 |
| 6 | Roles & Permissions CRUD API | T1-F06 (1..5) | T2-F06 (1..5) | T3-01, T3-04 | T4-05 |
| 7 | Employees CRUD API | T1-F07 (1..5) | T2-F07 (1..5) | T3-04, T3-05, T3-27, T3-32 | T4-05, T4-06 |
| 8 | Complete User Profile API | T1-F08 (1..5) | T2-F08 (1..5) | T3-05, T3-06 | T4-14 |
| 9 | Omission of Notifications | T1-F09 (1..5) | T2-F09 (1..5) | — | T4-16 |
| 10 | Security & Auth Unit Tests | T1-F10 (1..5) | T2-F10 (1..5) | — | — |
| 11 | Unified MovimientoComercial Entity | T1-F11 (1..5) | T2-F11 (1..5) | T3-08..T3-11, T3-14, T3-16, T3-18 | T4-01..T4-04, T4-12, T4-17 |
| 12 | Commercial Financial Rules | T1-F12 (1..5) | T2-F12 (1..5) | T3-08..T3-10 | T4-01..T4-03 |
| 13 | Commercial Workflow Transitions | T1-F13 (1..5) | T2-F13 (1..5) | T3-11, T3-12, T3-33 | T4-01..T4-04 |
| 14 | Non-Terminal Rejection & Correction | T1-F14 (1..5) | T2-F14 (1..5) | T3-12, T3-13, T3-30 | T4-04 |
| 15 | MIX Items & Stock Decomposition | T1-F15 (1..5) | T2-F15 (1..5) | T3-14, T3-15, T3-28 | T4-01, T4-12, T4-17 |
| 16 | New Client Contract Enforcement | T1-F16 (1..5) | T2-F16 (1..5) | T3-16, T3-17, T3-29 | T4-01, T4-17 |
| 17 | Traceability & Approval History | T1-F17 (1..5) | T2-F17 (1..5) | T3-13, T3-30 | T4-04 |
| 18 | Finalization Stock Impact | T1-F18 (1..5) | T2-F18 (1..5) | T3-15, T3-17, T3-19, T3-22, T3-33 | T4-01..T4-03, T4-12, T4-17 |
| 19 | Relocated Reference Prices API | T1-F19 (1..5) | T2-F19 (1..5) | T3-18, T3-20 | T4-09, T4-17 |
| 20 | Commercial Unit Tests | T1-F20 (1..5) | T2-F20 (1..5) | — | — |
| 21 | Structured Row & Lot Backend | T1-F21 (1..5) | T2-F21 (1..5) | T3-21, T3-22, T3-34 | T4-07, T4-08, T4-17 |
| 22 | Structured Row & Lot Frontend | T1-F22 (1..5) | T2-F22 (1..5) | T3-21 | T4-07, T4-08 |
| 23 | Plantations Unit Tests | T1-F23 (1..5) | T2-F23 (1..5) | T3-34 | — |
| 24 | Sidebar 5 Collapsible Sections | T1-F24 (1..5) | T2-F24 (1..5) | T3-23 | T4-10 |
| 25 | Sidebar Persistence & Auto-Expansion | T1-F25 (1..5) | T2-F25 (1..5) | T3-23 | T4-10 |
| 26 | Frontend Route Guards & Perm Filtering | T1-F26 (1..5) | T2-F26 (1..5) | T3-07, T3-24, T3-25 | T4-13 |
| 27 | Topbar Profile Navigation & No Bell | T1-F27 (1..5) | T2-F27 (1..5) | T3-06 | T4-14, T4-16 |
| 28 | Deactivation Prohibition Symbol | T1-F28 (1..5) | T2-F28 (1..5) | T3-26, T3-27 | T4-15 |
| 29 | User Profile View (#perfil) | T1-F29 (1..5) | T2-F29 (1..5) | T3-06 | T4-14 |
| 30 | Employees Management View (#empleados) | T1-F30 (1..5) | T2-F30 (1..5) | T3-24, T3-26 | T4-05, T4-15 |
| 31 | Roles & Permissions View (#roles) | T1-F31 (1..5) | T2-F31 (1..5) | T3-25 | T4-05 |
| 32 | Stock View Bug Fix (#stock) | T1-F32 (1..5) | T2-F32 (1..5) | T3-19 | T4-11 |
| 33 | Standalone Reference Prices View | T1-F33 (1..5) | T2-F33 (1..5) | T3-20 | T4-09 |
| 34 | Unified Commercial Movements View | T1-F34 (1..5) | T2-F34 (1..5) | T3-28..T3-30 | T4-01, T4-04 |

**Total Test Coverage:** 170 (Tier 1) + 170 (Tier 2) + 34 (Tier 3) + 17 (Tier 4) = **391 Specified Tests**.

---

## 3. Test Architecture & Hosting Infrastructure

### 3.1 Custom WebApplicationFactory (`SigaTestWebApplicationFactory`)
- **Base Class**: `WebApplicationFactory<Program>` (leveraging `public partial class Program { }` in `SIGA.API`).
- **Database Isolation**: Reconfigures EF Core DbContext to target an isolated SQLite database file (`siga_e2e_tests.db` or run-specific test database).
- **Automated Seeding**: Executes `DatabaseSeeder.InitializeDatabaseAsync(app.Services)` during test host startup to ensure catalog permissions, system roles (`ADMINISTRADOR`, `VENDEDOR`, `SECRETARIA`, `CLIENTE`, `CONTABILIDAD`), and initial administrative credentials (`admin / Admin123!`) are present.
- **Rate Limit Relaxation**: Overrides `LoginPolicy` rate limiting configuration with a high permit limit (100,000 permits) to eliminate false 429 Too Many Requests errors during rapid automated test execution.

### 3.2 Authentication & Client Fixture (`ApiClientHelper`)
Provides pre-authenticated `HttpClient` instances with cryptographically valid JWT tokens:
- `GetClientAsAdmin()`: Bearer token containing `ADMINISTRADOR` role claim and full administrative permissions.
- `GetClientAsRole(string roleName)`: Generates valid Bearer token for system roles (`VENDEDOR`, `SECRETARIA`, `CONTABILIDAD`, `CLIENTE`) matching `MatrizRolesPermisos`.
- `GetClientWithPermissions(params string[] permissions)`: Generates scoped token with arbitrary permission claims for granular policy verification.
- `GetAnonymousClient()`: Unauthenticated client for 401 Unauthorized assertions.
- `GenerateCustomToken(...)`: Low-level generator supporting expired tokens, wrong HMAC secret keys, tampered payloads, and null claims for boundary testing.

### 3.3 Test Base Class (`TestBase`)
Implements `IClassFixture<SigaTestWebApplicationFactory>`:
- Provides strongly-typed response deserialization for `ApiResponse<T>`.
- Standardizes JSON serialization options matching `Program.cs` (`PropertyNameCaseInsensitive = true`, `JsonStringEnumConverter`).
- Utility assertions: `EnsureSuccessStatusCode`, `AssertErrorResponse`, and response unwrap helpers.

---

## 4. Directory Layout

```
tests/SIGA.E2ETests/
├── Infrastructure/
│   ├── SigaTestWebApplicationFactory.cs   # Custom in-memory TestServer & SQLite config
│   ├── ApiClientHelper.cs                  # JWT generator and pre-authenticated HttpClients
│   ├── TestBase.cs                         # Fixture base class and response assertions
│   └── FrontendAssetScanner.cs             # Frontend source code & DOM asset inspector
├── Tier1_FeatureCoverage/
│   ├── SecurityAndAuthTests.cs             # Features 1-10 (Single-Role, Permissions, Empleados, /auth/me)
│   ├── CommercialMovementsTests.cs         # Features 11-20 (MovimientoComercial, MIX, Financials, Ref Prices)
│   ├── PlantationsTests.cs                 # Features 21-23 (Filas, Lotes, Integer Order)
│   └── FrontendContractTests.cs            # Features 24-34 (Sidebar, Route Guards, Icons, Profile, Stock)
├── Tier2_BoundaryCases/
│   ├── SecurityBoundariesTests.cs          # Features 1-10 Boundaries (Expired tokens, Last admin, System roles)
│   ├── CommercialBoundariesTests.cs        # Features 11-20 Boundaries (MIX modulo 3, Rejection reason, State gates)
│   ├── PlantationsBoundariesTests.cs       # Features 21-23 Boundaries (Row capacity, Closed seasons, Negatives)
│   └── FrontendBoundariesTests.cs          # Features 24-34 Boundaries (Invalid routes, Corrupted storage, Icons)
├── Tier3_Combinations/
│   └── CrossModuleIntegrationTests.cs      # Pairwise cross-module combinations (T3-01 to T3-34)
├── Tier4_Workflows/
│   └── RealWorldWorkflowScenariosTests.cs  # Multi-step enterprise business scenarios (T4-01 to T4-17)
└── SIGA.E2ETests.csproj                    # Test project configuration
```

---

## 5. Execution Instructions

### 5.1 Build Command
```powershell
dotnet build tests/SIGA.E2ETests/SIGA.E2ETests.csproj -c Release
```

### 5.2 Test Run Command
```powershell
dotnet test tests/SIGA.E2ETests/SIGA.E2ETests.csproj --logger "console;verbosity=normal"
```

### 5.3 Filtered Execution by Tier
```powershell
# Run Tier 1 Feature Coverage
dotnet test tests/SIGA.E2ETests/SIGA.E2ETests.csproj --filter "FullyQualifiedName~Tier1"

# Run Tier 2 Boundary Cases
dotnet test tests/SIGA.E2ETests/SIGA.E2ETests.csproj --filter "FullyQualifiedName~Tier2"

# Run Tier 3 Combinations
dotnet test tests/SIGA.E2ETests/SIGA.E2ETests.csproj --filter "FullyQualifiedName~Tier3"

# Run Tier 4 Enterprise Workflows
dotnet test tests/SIGA.E2ETests/SIGA.E2ETests.csproj --filter "FullyQualifiedName~Tier4"
```

---

## 6. Real-World Application Scenarios (Tier 4 Summary)

1. **T4-01 (New Client Commercial Onboarding & Sale)**: Client creation -> Type 1 Venta 300 MIX -> Contract required -> Contract generated -> Approved -> Invoiced -> Finalized -> Physical stock decremented -> Next sale requires no contract.
2. **T4-02 (Compra Operation with Commission & Physical Stock Ingestion)**: Client brings harvest -> Type 2 Compra with 12% commission -> Gross $6000, Comm $720, Net $5280 -> Approved -> Invoiced -> Finalized -> Empresa stock incremented.
3. **T4-03 (Client-to-Client Stock Transfer Traspaso)**: Type 3 Traspaso -> $0 financials -> Skips invoice -> Finalized directly -> Stock moved between clients.
4. **T4-04 (Commercial Rejection, Correction, and Eventual Finalization)**: Sale rejected for low price -> Reason required -> Resubmitted to Revision -> Approved -> Invoiced -> Finalized -> 5-step timeline audit.
5. **T4-05 (Staff Onboarding, Custom Role Assignment, and Permission Enforcement)**: Custom role created -> User assigned -> User logs in -> Accesses permitted routes -> Denied unassigned routes -> Soft deleted -> Login denied.
6. **T4-06 (Multi-Admin Deletion Safeguard Execution)**: Attempt deactivating sole admin blocked -> Second admin created -> First admin deactivated -> Attempt deactivating sole remaining admin blocked.
7. **T4-07 (Complete Seasonal Production Lifecycle: Field Planting)**: Season created -> Row created -> Bulbs purchased -> Bulbs distributed to field lot -> Client stock decremented -> Row capacity updated.
8. **T4-08 (Field Row Capacity Boundary Protection)**: Row capacity 2000, current 1500 -> Planting 600 rejected -> Planting 500 succeeds (100% full).
9. **T4-09 (Seasonal Reference Price Setup & Venta Auto-Pricing)**: Admin configures season prices including MIX -> Sale created without prices -> Reference prices inherited automatically.
10. **T4-10 (UI Session Persistence & Deep Linking Walkthrough)**: Collapse section -> LocalStorage saved -> Deep link directly to inner view -> Target section auto-expands.
11. **T4-11 (Stock Bug Fix Regression Walkthrough)**: Stock view queries Empresa (`SingletonId = 1`) -> Array normalization handles response -> No `items.find` errors.
12. **T4-12 (Multi-Calibre MIX Combined Commercial Finalization)**: Multi-calibre sale (C0 + 300 MIX + C4) -> Verified and finalized -> Accurate physical stock deductions across all calibres.
13. **T4-13 (Unauthorized Route Tampering & Toast Notification)**: Tampering browser hash URL to `#roles` without permission -> Route guard blocks instantiation -> Redirects to `#dashboard` with toast warning.
14. **T4-14 (Profile Update and Synchronized Topbar Rendering)**: User updates name in `#perfil` -> Topbar badge immediately synchronizes.
15. **T4-15 (Deactivation Prohibition Symbol Consistency Across Views)**: Verification across Clientes, Empleados, and Roles that deactivation actions use prohibition SVG and zero trash icons.
16. **T4-16 (Omission of Notifications Complete Verification)**: System-wide audit verifying total absence of notification endpoints, bells, panels, and background requests.
17. **T4-17 (End-to-End Multi-Role Enterprise Simulation)**: Full cross-role journey: Admin configures prices, Seller registers client & sale, Admin approves, Accountant invoices, Admin finalizes, Manager distributes to field row.

---

## 7. Quality & Verification Standards

- **Zero Facade Implementations**: Tests perform genuine HTTP requests and real assertions.
- **Fail-Safe Contract Compatibility**: When running tests while parallel milestones (M2, M5) are actively developing controllers, tests verify route availability and report pending backend controllers with clear actionable diagnostics.
- **Non-Polluting Test Run**: Test databases and temporary resources are initialized cleanly and do not corrupt development databases (`siga_dev.db`).
