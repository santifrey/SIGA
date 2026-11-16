Implement the SIGA Iteration ("Iteración SIGA") covering Commercial Movements, Roles and Permissions, Stock Bug Fix, Reference Prices, and Plantations in the SIGA repository. Do not implement any notification features or services at this time.

Working directory: c:\Users\santi\Desktop\u\ies\tesis\SIGA
Integrity mode: development

## Verification Resources
- Test suite: `tests/SIGA.UnitTests` contains automated tests (`dotnet test`) verifying domain entities, services, and authorization.
- Database seeding: `DatabaseSeeder.cs` handles schema and initial data via `EnsureCreatedAsync`.

## Requirements

### R1. Backend: Security, Platform, Roles, and Permissions
- Change user-role relationship so each User has exactly one Role (foreign key `Usuario.RolId`), while each Role has multiple Permisos (Role-Permission many-to-many via `RolPermiso`). Add an `EsSistema` flag to Role (preventing deletion of system roles like `ADMINISTRADOR`). Update seed data with the permissions catalog and seed roles; do not create an "APROBADOR" role.
- Implement permission-based authorization policies (e.g. `[RequierePermiso("...")]`) backed by an authorization policy provider and handler; replace role-based attributes in all controllers. Include single role claim and individual permission claims in JWT.
- Implement API endpoints according to the agreed API contract for Roles and Permissions (`/roles`, `/permisos`) and Employees (`/empleados`, managing Persona and non-CLIENTE Usuario in a transaction, with soft delete via Inactivo state).
- Extend `GET /auth/me` to return complete user profile data, including person details, role, permissions, and last access date.
- Explicit non-goal: Do not implement notifications or notification services/endpoints (`INotificacionService`, `/notificaciones`).

### R2. Backend: Commercial Movements (Movimiento Comercial)
- Unify and evolve `Venta` into `MovimientoComercial` with route `/movimientos-comerciales`, supporting 3 types (1 = VentaACliente, 2 = CompraACliente, 3 = TraspasoEntreClientes) and 5 states (0 = PendienteRevision, 1 = AprobadoAdmin, 2 = Facturado, 3 = Finalizado, 4 = Rechazado).
- Rejection is non-terminal: requires a reason; movements in `Rechazado` can be edited and resubmitted back to `PendienteRevision` by the creator or users with `MOVCOM_CORREGIR`. Rejection from `Facturado` voids the invoice.
- Financial calculation rules: Type 1 calculates total from item unit prices; Type 2 includes items, commission percentage, gross amount, commission amount, and net amount; Type 3 contains no financial amounts and skips the `Facturado` step directly from `AprobadoAdmin` to `Finalizado`.
- MIX items handling: quantity must be a multiple of 3, represented by a single unit price per bulb, and decomposed into equal parts (quantity / 3) across Calibre 1, 2, and 3 for origin stock availability validation and confirmed movement generation.
- Contract enforcement: contract is generated and required only when movement type is VentaACliente and the client is new (has no prior finalized commercial movements).
- Traceability: capture `UsuarioRegistroId` and `FechaRegistro` on creation; log every transition in approval history (`RegistroAprobacion`) recording user, date, action, and notes.
- On `Finalizado`, generate confirmed `MovimientoBulbos` impacting stock. Relocate reference prices to a dedicated controller and service (`/precios-referencia`) supporting both Class 0..4 and MIX prices.

### R3. Frontend: Layout Shell, Profile, and Administration
- Reorganize sidebar navigation into 5 collapsible sections (Principal, Movimientos Comerciales, Stock y Bulbos, Producción y Campo, Administración) with chevron indicators, smooth height animation, localStorage open/close persistence, and automatic expansion of the section containing the active route.
- Filter navigation items and enforce route guards based on user permissions (`auth-service.tienePermiso(codigo)`).
- Implement views for User Profile (`#perfil`), Employees management (`#empleados`), and Roles & Permissions (`#roles`).
- Replace all trash can icons used for "dar de baja" / deactivation across all views with an inline SVG prohibition symbol (red circle with diagonal slash, `var(--danger)`).
- Explicit non-goal: Do not implement the notification bell or notifications panel in the topbar.

### R4. Frontend: Commercial Views, Reference Prices, and Bug Fixes
- Fix the bug in #stock ("items.find is not a function") in `views/stock-view.js` and `services/stock-service.js` by querying real API data for Empresa (`SingletonId = 1`), normalizing returned arrays, defensively checking `Array.isArray`, and aligning bulb class naming with backend enums.
- Extract reference prices into a standalone view under Administration (`#precios-referencia`), allowing season selection and editing of Class 0..4 and MIX prices for users with `PRECIOS_REF_GESTIONAR`.
- Build unified Commercial Movements view (`#movimientos-comerciales`, replacing #ventas) supporting:
  - Form adaptable to the 3 movement types with dynamic participant fields.
  - Item grid with Class 0..4 and MIX rows, displaying live decomposition for MIX and validating multiples of 3.
  - Live origin stock availability warnings.
  - Visual notification/badge if a client is new and requires a contract.
  - Filterable listing by type, state, client, season, and date range.
  - Detail view with complete transition timeline and actionable buttons according to state and permissions (Approve, Invoice, Finalize, Reject with required reason, Edit & Resubmit).

### R5. Plantations: Structured Row and Lot Inputs
- Replace free-text inputs for row identification in `views/plantaciones-view.js` with structured numeric selectors:
  - Row selector populated from branch and season (`/filas/por-sucursal/{id}/temporada/{id}`), with an option to create a new row specifying integer number and capacity.
  - Lot selection using integer number, suggesting the next available order within the row.
- Update backend DTOs and entity validations (`RegistrarDistribucionDto`, `Fila`, `GestionPlantaciones`) to handle integer row and lot numbers.

## Acceptance Criteria

### Security and Backend Architecture
- [ ] `dotnet build SIGA.sln` completes with 0 errors.
- [ ] Existing tests in `tests/SIGA.UnitTests` pass and new/updated unit tests verify the single-role/many-permissions model and permission authorization handler.
- [ ] Initial database seed successfully loads permissions catalog, system roles, and initial user without an "APROBADOR" role.
- [ ] Login returns JWT with single role claim and permission claims; `GET /auth/me` returns complete profile details.
- [ ] Endpoints protected by `[RequierePermiso(...)]` return 403 Forbidden when invoked without the required permission.
- [ ] CRUD for Roles and Employees operates correctly; deleting a system role or the last administrator is prevented.
- [ ] Notification endpoints and `INotificacionService` are omitted.

### Commercial Movements
- [ ] All 3 movement types can be created and progress through their defined state workflows (Type 3 transitions directly from AprobadoAdmin to Finalizado without invoicing; Type 2 calculates gross, commission, and net amounts).
- [ ] MIX items reject non-multiples of 3 and distribute bulb counts equally across Classes 1, 2, and 3 upon stock validation and stock movement generation.
- [ ] Movements in `Rechazado` can be modified and resubmitted to `PendienteRevision` by the creator or users with `MOVCOM_CORREGIR`. Unauthorized users receive 403.
- [ ] Moving to `Finalizado` generates confirmed `MovimientoBulbos` and correctly updates inventory stock.
- [ ] Contract is required and requested exclusively for new clients under VentaACliente.
- [ ] Detail endpoint returns complete traceability history covering registration and all state transitions with timestamps and user names.
- [ ] Reference prices endpoints operate under `/precios-referencia` and persist prices for Class 0..4 and MIX.

### Layout Shell and Administration Views
- [ ] Sidebar renders 5 collapsible sections, persists collapsed/expanded state in localStorage, and hides items lacking permission.
- [ ] Topbar renders without the notification bell; existing profile and layout elements remain functional.
- [ ] Views for `#perfil`, `#empleados`, and `#roles` function properly against backend endpoints.
- [ ] Zero trash can icons remain for "dar de baja" actions anywhere in the frontend.

### Commercial Frontend and Operational Fixes
- [ ] Stock view (`#stock`) renders without console errors or `items.find is not a function` exceptions for both Empresa and client selections.
- [ ] Standalone Reference Prices view permits editing Class 0..4 and MIX prices when authorized.
- [ ] Commercial Movements view allows registration of all 3 types, displays live stock availability and MIX decomposition, and shows the full timeline in detail view.
- [ ] Plantations view replaces free-text inputs with structured numeric selectors for row and lot, supported by backend validation.
- [ ] Browser console remains free of unhandled JavaScript errors during navigation and standard flows.
