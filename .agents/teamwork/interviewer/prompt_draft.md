Implement the SIGA Iteration ("Iteración SIGA") covering Commercial Movements, Roles and Permissions, Notifications, Stock Bug Fix, Reference Prices, and Plantations in the SIGA repository.

Working directory: c:\Users\santi\Desktop\u\ies\tesis\SIGA
Integrity mode: [OPEN: Which shortcut restrictions apply?]

## Requirements

### R1. Backend: Security, Platform, Roles, Permissions, and Notifications
- Change user-role mapping so each User has exactly one Role, and each Role has multiple Permisos (Role-Permission many-to-many). Include EsSistema flag for system roles.
- Replace role-based authorization with permission-based authorization policies (e.g. `[RequierePermiso("...")]`) across all controllers; update JWT to include the role claim and permission claims.
- Implement API endpoints for Roles and Permissions, and Employees (Persona + Usuario with non-CLIENTE role; soft delete via Inactivo) according to the API contract.
- Extend `GET /auth/me` to return complete profile details, role, and permissions.
- Implement Notifications module with `INotificacionService` (`NotificarUsuarioAsync`, `NotificarPorPermisoAsync`), polling endpoints, and traceability.

### R2. Backend: Commercial Movements (Movimiento Comercial)
- Unify and evolve Venta into MovimientoComercial with 3 types (VentaACliente, CompraACliente, TraspasoEntreClientes) and 5 states (PendienteRevision, AprobadoAdmin, Facturado, Finalizado, Rechazado).
- Implement non-terminal rejection: rejection requires a reason; creators or users with MOVCOM_CORREGIR can edit and resubmit (Rechazado -> PendienteRevision), re-notifying approvers. Rejection from Facturado voids the invoice.
- Handle MIX bulb items: decomposed equally across Calibres 1, 2, and 3 upon stock validation and finalization; quantity must be a multiple of 3.
- Enforce contract requirement only for VentaACliente when the client is new (no previous Finalizado commercial movements).
- Maintain full traceability in approval history (who registered, transitions, timestamps, actions, notes).
- Relocate reference prices to dedicated controller/service supporting both Class 0..4 and MIX.
- On Finalizado, create confirmed bulb movements impacting stock; validate stock availability from origin on creation and approval.

### R3. Frontend: Navigation Shell, User Profile, Notifications, and Administration
- Reorganize sidebar navigation into 5 collapsible sections (Principal, Movimientos Comerciales, Stock y Bulbos, Producción y Campo, Administración) with chevron indicators, smooth transition, localStorage state persistence, and automatic expansion for the active route.
- Filter sidebar items and add router guards based on user permissions.
- Add notification bell in topbar with unread count badge, 30s polling during active session, and dropdown panel for reading and navigation.
- Implement User Profile view (`#perfil`), Employees management view (`#empleados`), and Roles & Permissions view (`#roles`).
- Replace all trash can icons used for "dar de baja" (deactivation) across the entire application with an inline SVG prohibition circle with diagonal slash (`var(--danger)`).

### R4. Frontend: Commercial Views, Reference Prices, and Bug Fixes
- Fix the #stock bug ("items.find is not a function") by querying real API endpoints for Empresa (`SingletonId = 1`), returning normalized arrays, safely checking `Array.isArray`, and aligning class names with backend.
- Create dedicated Reference Prices view (`#precios-referencia`) under Administration with season selector and MIX price support.
- Create unified Commercial Movements view (`#movimientos-comerciales`, replacing #ventas) supporting registration for all 3 types, dynamic origin/destination fields, MIX breakdown, live stock checks, new client alerts, filtering by type/state/client/dates, detail view with full timeline, and conditional actions (approve, invoice, finalize, reject with reason, edit & resubmit).
- Update Plantations view and backend to use structured numeric selectors for row and lot rather than free-text input.

## Acceptance Criteria

### Security, Platform, and Backend Architecture
- [ ] `dotnet build SIGA.sln` compiles with 0 errors.
- [ ] Database seeder correctly populates initial permissions catalog and roles without creating an "APROBADOR" role.
- [ ] Login returns JWT containing single role claim and permission claims; `/auth/me` returns full profile and permissions.
- [ ] Accessing protected endpoints without the required permission returns 403 Forbidden.
- [ ] CRUD for Roles and Employees operates correctly, preventing deletion of system roles or the last administrator.
- [ ] `INotificacionService.NotificarPorPermisoAsync("MOVCOM_APROBAR", ...)` dispatches notifications to all active users holding the permission.

### Commercial Movements
- [ ] All 3 movement types can be created and follow their specific financial and workflow rules (Type 3 skips invoice; Type 2 computes gross, commission, and net).
- [ ] MIX items validate that quantity is a multiple of 3 and distribute bulb counts equally across Classes 1, 2, and 3 upon validation and finalization.
- [ ] Rejected movements can be edited and resubmitted by the creator or MOVCOM_CORREGIR holders, returning to PendienteRevision and re-notifying approvers. Unauthorized users receive 403.
- [ ] Finalizing a movement generates confirmed MovimientoBulbos and updates stock accurately.
- [ ] Contracts are requested and required only for new clients under VentaACliente.
- [ ] Full traceability history records registration and every subsequent status transition.

### Frontend Interface and Management
- [ ] Sidebar displays 5 collapsible sections, remembers collapsed state across page reloads, and hides unauthorized items.
- [ ] Notification bell badge updates via polling and displays latest notifications in a dropdown panel.
- [ ] Profile, Employees, and Roles & Permissions views function against backend endpoints.
- [ ] No trash can icons remain for "dar de baja" actions anywhere in the frontend.

### Commercial Frontend and Bug Fixes
- [ ] Stock view (#stock) displays stock without JavaScript errors for both Empresa and individual clients.
- [ ] Reference prices view allows viewing and editing prices (including MIX) for authorized users.
- [ ] Commercial movements view handles registration, filtering, detail inspection with timeline, and state transitions for all 3 movement types.
- [ ] Plantaciones view replaces free-text inputs for row and lot with structured selectors and validates on the backend.
- [ ] Existing automated tests (`dotnet test`) pass and updated/new tests cover the modified domain rules.
