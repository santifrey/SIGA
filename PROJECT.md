# Project: SIGA Iteration ("Iteración SIGA")

## Architecture
SIGA is a modular agricultural enterprise system composed of:
- **Backend API**: ASP.NET Core 7 Web API (`src/SIGA.API`), Clean Architecture layered with Application (`src/SIGA.Application`), Domain (`src/SIGA.Domain`), and Infrastructure (`src/SIGA.Infrastructure`).
- **Persistence**: Entity Framework Core with MySQL (`siga_db`), repository pattern, and unit of work.
- **Frontend SPA**: Vanilla ES6 modules (`frontend/js/`), CSS custom properties design system (`frontend/css/`), hash-based client-side router (`frontend/js/router.js`).
- **Security**: JWT authentication with single-role claim and granular permission claims, dynamic policy-based authorization (`[RequierePermiso("...")]`).

### System Interaction Diagram
```
[Frontend SPA]
  ├── Shell: 5 Collapsible Sections + Permission Filtering
  ├── Auth & Profile: Token storage, Permissions, /auth/me (#perfil)
  ├── Administration: #empleados, #roles, #precios-referencia, #clientes, #sucursales
  ├── Commercial Movements: #movimientos-comerciales (Types 1..3, MIX decomp, timeline)
  ├── Stock & Bulbos: #stock (Empresa & Clients real API), #movimientos
  └── Production & Field: #plantaciones (Structured row/lot selectors), #cosechas, #temporadas
        │ (REST API / JWT)
        ▼
[Backend Controllers]
  ├── AuthController (/auth/me, /auth/login)
  ├── RolesController (/roles) & PermisosController (/permisos)
  ├── EmpleadosController (/empleados)
  ├── MovimientosComercialesController (/movimientos-comerciales)
  ├── PreciosReferenciaController (/precios-referencia)
  ├── StockController (/stock/por-cliente/{id})
  ├── FilasController & PlantacionesController (/filas, /plantaciones)
  └── Existing Domain Controllers (Clientes, Sucursales, Temporadas, etc.)
        │
        ▼
[Domain & Persistence]
  ├── Single-Role Usuario (Usuario.RolId) & Many-to-Many RolPermiso
  ├── MovimientoComercial (Types 1..3, States 0..4, MIX 1/3 decomposition)
  ├── PrecioClase (Classes 0..4, MIX)
  ├── Fila & Lote (Structured integer row/lot order)
  └── Confirmed MovimientoBulbos (Origin -> Destino physical stock impact)
```

---

## Feature Inventory

Every feature from the Survey phase is mapped to an implementation milestone below:

| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | Single-Role User Model | Change `Usuario`-`Rol` to 1:N via foreign key `Usuario.RolId`, removing `UsuarioRol`. | M1 | R1 Survey |
| 2 | Role-Permission Model | Create `Permiso` and `RolPermiso` entities for N:M relationship; add `EsSistema` flag to `Rol`. | M1 | R1 Survey |
| 3 | Permissions Catalog & Seeding | Seed permissions catalog, system roles (`EsSistema=true`), initial admin user; omit "APROBADOR" role. | M1 | R1 Survey |
| 4 | Dynamic Permission Authorization | Implement `[RequierePermiso("...")]` attribute, policy provider, and authorization handler; replace role attributes across controllers. | M1 | R1 Survey |
| 5 | Single-Role & Permission JWT | Emit single role claim and individual permission claims in JWT token generation. | M1 | R1 Survey |
| 6 | Roles & Permissions CRUD API | Implement `/roles` and `/permisos` endpoints; protect system roles from deletion. | M1 | R1 Survey |
| 7 | Employees CRUD API | Implement `/empleados` managing Persona and non-CLIENTE Usuario in a transaction; soft delete via `Inactivo`; prevent deleting last admin. | M1 | R1 Survey |
| 8 | Complete User Profile API | Extend `GET /auth/me` to return personal details, role, permissions list, and last access date. | M1 | R1 Survey |
| 9 | Omission of Notifications | Explicit non-goal: omit `INotificacionService` and `/notificaciones`. | M1 | R1 Survey |
| 10 | Security & Auth Unit Tests | Update/add tests in `tests/SIGA.UnitTests` for single-role, permissions handler, system roles, and employees. | M1 | R1 Survey |
| 11 | Unified MovimientoComercial Entity | Evolve `Venta` into `MovimientoComercial` with route `/movimientos-comerciales`, supporting 3 types and 5 states. | M2 | R2 Survey |
| 12 | Commercial Financial Rules | Type 1 calculates total from items; Type 2 calculates gross, commission %, commission $, and net; Type 3 has 0 financials. | M2 | R2 Survey |
| 13 | Commercial Workflow Transitions | Types 1 & 2 progress 0->1->2->3; Type 3 skips Facturado directly 0->1->3. | M2 | R2 Survey |
| 14 | Non-Terminal Rejection & Correction | Rejection requires reason; rejection from Facturado voids invoice; rejected movements editable/resubmittable to 0 by creator or `MOVCOM_CORREGIR`. | M2 | R2 Survey |
| 15 | MIX Items & Stock Decomposition | `ClaseBulbo.MIX = 5`; quantity must be multiple of 3; decomposes into 1/3 each across Classes 1, 2, 3 for stock verification and stock movement. | M2 | R2 Survey |
| 16 | New Client Contract Enforcement | Contract required and enforced during admin approval exclusively for Type 1 (`VentaACliente`) when client has 0 finalized movements. | M2 | R2 Survey |
| 17 | Traceability & Approval History | Capture `UsuarioRegistroId`, `FechaRegistro`, and log all transitions in `RegistroAprobacion`. | M2 | R2 Survey |
| 18 | Finalization Stock Impact | On `Finalizado`, generate confirmed `MovimientoBulbos` with decomposed quantities updating physical stock. | M2 | R2 Survey |
| 19 | Relocated Reference Prices API | Dedicated `/precios-referencia` controller and `GestionPreciosReferencia` supporting Classes 0..4 and MIX. | M2 | R2 Survey |
| 20 | Commercial Unit Tests | Comprehensive unit tests in `tests/SIGA.UnitTests` for commercial movements, transitions, rejection, MIX, contracts, and ref prices. | M2 | R2 Survey |
| 21 | Structured Row & Lot Backend | Update `RegistrarDistribucionDto`, `Fila`, `GestionPlantaciones` for integer row and lot numbers. | M3 | R5 Survey |
| 22 | Structured Row & Lot Frontend | Replace free-text inputs in `plantaciones-view.js` with dynamic row selector (`/filas/por-sucursal/{id}/temporada/{id}`), "+ Nueva Fila" option, and integer lot order. | M3 | R5 Survey |
| 23 | Plantations Unit Tests | Verify integer row and lot handling in `GestionPlantacionesTests`. | M3 | R5 Survey |
| 24 | Sidebar 5 Collapsible Sections | Reorganize sidebar into Principal, Movimientos Comerciales, Stock y Bulbos, Producción y Campo, Administración with chevrons and animation. | M4 | R3 Survey |
| 25 | Sidebar Persistence & Auto-Expansion | Save section open/collapsed state in localStorage; auto-expand section containing active route. | M4 | R3 Survey |
| 26 | Frontend Route Guards & Perm Filtering | Filter navigation items and enforce route guards based on `auth-service.tienePermiso(codigo)`. | M4 | R3 Survey |
| 27 | Topbar Profile Navigation & No Bell | Click `#user-profile-badge` navigates to `#perfil`; verify topbar has NO notification bell or panel. | M4 | R3 Survey |
| 28 | Deactivation Prohibition Symbol | Replace all trash can icons across all frontend views (`clientes-view.js`, etc.) with inline SVG prohibition symbol (red circle with slash). | M4 | R3 Survey |
| 29 | User Profile View (#perfil) | Implement `#perfil` view rendering user details, personal info, role, permissions list, and last access from `/auth/me`. | M4 | R3 Survey |
| 30 | Employees Management View (#empleados) | Implement `#empleados` view managing Persona + non-CLIENTE Usuario, role selector, soft delete with prohibition icon, last admin safeguard. | M4 | R3 Survey |
| 31 | Roles & Permissions View (#roles) | Implement `#roles` view managing roles and permissions, system role badge & deletion protection, permission matrix. | M4 | R3 Survey |
| 32 | Stock View Bug Fix (#stock) | Fix "items.find is not a function" in `stock-view.js` and `stock-service.js` by querying real API for Empresa (`SingletonId = 1`), normalizing arrays, and checking `Array.isArray`. | M5 | R4 Survey |
| 33 | Standalone Reference Prices View | Implement `#precios-referencia` view under Admin for Classes 0..4 and MIX, guarded by `PRECIOS_REF_GESTIONAR`. | M5 | R4 Survey |
| 34 | Unified Commercial Movements View | Implement `#movimientos-comerciales` (replacing `#ventas`): 3 types, MIX live decomposition & multiple of 3 validation, live stock warnings, new client badge, filters, timeline, and actionable buttons. | M5 | R4 Survey |
| 35 | E2E Requirement-Driven Test Suite | Automated opaque-box E2E test harness covering Tiers 1-4 across all features. | E2E | Project Plan |
| 36 | Adversarial Hardening (Tier 5) | White-box adversarial testing for edge cases and boundary stress testing. | Final | Project Plan |

---

## Milestones

| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| E2E | E2E Test Suite Creation | Design test harness, runner, and test cases covering Tiers 1-4 for all features; output `TEST_READY.md`. | none (runs in parallel) | IN_PROGRESS |
| M1 | Backend Security, Platform, Roles & Permissions | Features 1–10: `Usuario.RolId`, `Permiso`, `RolPermiso`, `[RequierePermiso]`, JWT claims, `/roles`, `/permisos`, `/empleados`, `/auth/me`, Seeder, unit tests. | none | DONE |
| M2 | Backend Commercial Movements & Ref Prices | Features 11–20: `MovimientoComercial` 3 types, 5 states, financial rules, MIX decomposition, contracts, traceability, `/precios-referencia`, unit tests. | M1 | IN_PROGRESS |
| M3 | Plantations Structured Row & Lot Inputs | Features 21–23: Backend DTOs & validations, frontend `plantaciones-view.js` dynamic row selector, "+ Nueva Fila", and lot order. | none | IN_PROGRESS |
| M4 | Frontend Layout Shell, Profile & Admin Views | Features 24–31: Sidebar 5 collapsible sections, localStorage persistence, permission filtering, prohibition icons, `#perfil`, `#empleados`, `#roles`. | M1 | IN_PROGRESS |
| M5 | Frontend Commercial Views, Ref Prices & Stock Fix | Features 32–34: `#stock` bug fix with real Empresa API, standalone `#precios-referencia`, and unified `#movimientos-comerciales` view. | M2, M4 | PLANNED |
| Final | Final E2E Test Verification & Hardening | Phase 1: Pass 100% of E2E tests (Tiers 1-4). Phase 2: Adversarial coverage hardening (Tier 5). | E2E, M1..M5 | PLANNED |

---

## Interface Contracts

### 1. Security & Auth (`M1 ↔ M4, M5`)
- `POST /api/v1/auth/login`: Returns `{ token: string, usuario: { id, username, rol: string, roles: string[] } }`. Token contains `ClaimTypes.Role` (single role) and `permiso` claims.
- `GET /api/v1/auth/me`: Returns:
  ```json
  {
    "usuarioId": 1,
    "personaId": 1,
    "username": "admin",
    "nombre": "Administrador",
    "apellido": "SIGA",
    "nombreCompleto": "Administrador SIGA",
    "email": "admin@azafranmediterraneo.com",
    "documento": "00000000",
    "telefono": "123456789",
    "rolId": 1,
    "rol": "ADMINISTRADOR",
    "permisos": ["ROLES_VER", "ROLES_GESTIONAR", "EMPLEADOS_VER", ...],
    "ultimoAcceso": "2026-11-11T02:00:00Z",
    "estado": "Activo"
  }
  ```
- `GET /api/v1/roles`: Returns list of `{ id, nombre, descripcion, esSistema, totalUsuarios, permisos: string[] }`.
- `DELETE /api/v1/roles/{id}`: Returns 400 Bad Request if `esSistema == true` or if active users exist.
- `GET /api/v1/permisos`: Returns list of `{ id, codigo, nombre, descripcion, modulo }`.
- `GET /api/v1/empleados`: Returns list of internal staff (role != `CLIENTE`).
- `POST /api/v1/empleados`: Creates `Persona` and `Usuario` in a single transaction.
- `DELETE /api/v1/empleados/{id}`: Soft delete setting `Usuario.Estado = Inactivo`. Rejects with 400 if target is the last active `ADMINISTRADOR`.

### 2. Commercial Movements & Reference Prices (`M2 ↔ M5`)
- Route: `/api/v1/movimientos-comerciales`
- `TipoMovimientoComercial`: `1` (VentaACliente), `2` (CompraACliente), `3` (TraspasoEntreClientes).
- `EstadoMovimientoComercial`: `0` (PendienteRevision), `1` (AprobadoAdmin), `2` (Facturado), `3` (Finalizado), `4` (Rechazado).
- Financial rules:
  - Type 1: `MontoTotal = sum(detalles)`.
  - Type 2: `MontoBruto = sum(detalles)`, `MontoComision = MontoBruto * (PorcentajeComision / 100)`, `MontoNeto = MontoBruto - MontoComision`, `MontoTotal = MontoNeto`.
  - Type 3: `MontoTotal = 0`. Direct transition from `AprobadoAdmin` (1) to `Finalizado` (3).
- MIX Validation: `ClaseBulbo.MIX = 5`. `Cantidad % 3 == 0`. Equal decomposition: `Cantidad / 3` across Calibres 1, 2, and 3.
- Contract Rule: `GET /api/v1/movimientos-comerciales/verificar-cliente-nuevo/{clienteId}` -> `{ esNuevo: bool, requiereContrato: bool }`. Returns `true` only for Type 1 when client has 0 finalized movements.
- Reference Prices:
  - `GET /api/v1/precios-referencia/temporada/{temporadaId}` -> list of 6 prices (Clase 0..4 + MIX).
  - `POST /api/v1/precios-referencia` -> upsert price.

### 3. Plantations Structured Inputs (`M3`)
- `GET /api/v1/filas/por-sucursal/{sucursalId}/temporada/{temporadaId}` -> returns list of existing rows `{ id, identificacion, capacidad, totalLotes }`.
- `POST /api/v1/plantaciones/distribucion`: `RegistrarDistribucionDto` accepting integer row/lot properties:
  `{ sucursalId, temporadaId, filaId?, numeroFila?, identificacionFila, capacidadFila, numeroLote?, clienteId, detalles: [...] }`.

---

## Code Layout

### Backend (`src/`)
- `SIGA.Domain/Entities/`: `Usuario.cs`, `Rol.cs`, `Permiso.cs`, `RolPermiso.cs`, `MovimientoComercial.cs`, `DetalleMovimientoComercial.cs`, `PrecioClase.cs`, `Fila.cs`, `Lote.cs`.
- `SIGA.Domain/Enums/`: `Enums.cs` (`TipoMovimientoComercial`, `EstadoMovimientoComercial`, `ClaseBulbo`, `AccionAprobacion`).
- `SIGA.Application/DTOs/`: `Auth/`, `Roles/`, `Empleados/`, `MovimientosComerciales/`, `PreciosReferencia/`, `Plantaciones/`.
- `SIGA.Application/Interfaces/`: `IGestionUsuarios.cs`, `IGestionRoles.cs`, `IGestionEmpleados.cs`, `IGestionMovimientosComerciales.cs`, `IGestionPreciosReferencia.cs`, `IGestionPlantaciones.cs`.
- `SIGA.Application/Services/`: `GestionUsuarios.cs`, `GestionRoles.cs`, `GestionEmpleados.cs`, `GestionMovimientosComerciales.cs`, `GestionPreciosReferencia.cs`, `GestionPlantaciones.cs`.
- `SIGA.Infrastructure/Persistence/`: `SigaDbContext.cs`, `DatabaseSeeder.cs`, Configurations/ (`UsuarioConfiguration.cs`, `RolConfiguration.cs`, `PermisoConfiguration.cs`, `RolPermisoConfiguration.cs`, `MovimientoComercialConfiguration.cs`, etc.).
- `SIGA.Infrastructure/Security/`: `JwtTokenService.cs`.
- `SIGA.API/Authorization/`: `RequierePermisoAttribute.cs`, `PermissionPolicyProvider.cs`, `PermissionAuthorizationHandler.cs`.
- `SIGA.API/Controllers/`: `AuthController.cs`, `RolesController.cs`, `PermisosController.cs`, `EmpleadosController.cs`, `MovimientosComercialesController.cs`, `PreciosReferenciaController.cs`, `StockController.cs`, `FilasController.cs`, `PlantacionesController.cs`.

### Frontend (`frontend/`)
- `frontend/css/`: `layout.css` (collapsible sections, animations), `variables.css`.
- `frontend/js/`: `app.js` (5 collapsible sections, topbar profile, permission filtering), `router.js` (permission guards).
- `frontend/js/services/`: `auth-service.js`, `roles-service.js`, `empleados-service.js`, `movimientos-comerciales-service.js`, `precios-referencia-service.js`, `stock-service.js`, `plantaciones-service.js`.
- `frontend/js/views/`: `perfil-view.js`, `empleados-view.js`, `roles-view.js`, `movimientos-comerciales-view.js`, `precios-referencia-view.js`, `stock-view.js`, `plantaciones-view.js`, `clientes-view.js`.

### Tests (`tests/`)
- `tests/SIGA.UnitTests/`: `GestionUsuariosTests.cs`, `JwtTokenServiceTests.cs`, `AuthorizationHandlerTests.cs`, `GestionRolesTests.cs`, `GestionEmpleadosTests.cs`, `GestionMovimientosComercialesTests.cs`, `GestionPreciosReferenciaTests.cs`, `GestionStockTests.cs`, `GestionPlantacionesTests.cs`.
