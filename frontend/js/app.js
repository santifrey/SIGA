/**
 * SIGA - js/app.js
 * Orquestador principal de la Single Page Application (SPA)
 * Implementa Shell de 5 Secciones Colapsables, Animaciones, Persistencia Local,
 * Auto-expansión, Route Guards, Filtrado de Navegación por Permisos y Navegación de Perfil (Features 24-27).
 */

import { Router } from './router.js';
import { apiClient } from './utils/api-client.js';
import { authService } from './services/auth-service.js';
import { toast } from './utils/toast.js';
import { escapeHtml } from './utils/escape-html.js';

import { LoginView } from './views/login-view.js';
import { DashboardView } from './views/dashboard-view.js';
import { ClientesView } from './views/clientes-view.js';
import { SucursalesView } from './views/sucursales-view.js';
import { StockView } from './views/stock-view.js';
import { HistorialMovimientosView } from './views/historial-movimientos-view.js';
import { AjustesStockView } from './views/ajustes-stock-view.js';
import { MovimientosView } from './views/movimientos-view.js';
import { TemporadasView } from './views/temporadas-view.js';
import { PlantacionesView } from './views/plantaciones-view.js';
import { TransferenciasView } from './views/transferencias-view.js';
import { CosechasView } from './views/cosechas-view.js';
import { MovimientosComercialesView } from './views/movimientos-comerciales-view.js';
import { ReportesView } from './views/reportes-view.js';

// Nuevas Vistas (Milestone 4)
import { PerfilView } from './views/perfil-view.js?v=2.0';
import { EmpleadosView } from './views/empleados-view.js';
import { RolesView } from './views/roles-view.js';
import { PreciosReferenciaView } from './views/precios-referencia-view.js';

const STORAGE_KEY_COLLAPSED_SECTIONS = 'siga_sidebar_collapsed_sections';

function getCollapsedSectionsState() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY_COLLAPSED_SECTIONS);
    return raw ? JSON.parse(raw) : {};
  } catch {
    return {};
  }
}

function saveCollapsedSectionState(sectionId, isCollapsed) {
  try {
    const state = getCollapsedSectionsState();
    state[sectionId] = isCollapsed;
    localStorage.setItem(STORAGE_KEY_COLLAPSED_SECTIONS, JSON.stringify(state));
  } catch (err) {
    console.warn('[Sidebar] Error al guardar estado de sección:', err);
  }
}

class App {
  constructor() {
    this.appRoot = document.getElementById('app');
    this.router = new Router();

    // Vistas existentes
    this.loginView = new LoginView(this.router);
    this.dashboardView = new DashboardView(this.router);
    this.clientesView = new ClientesView(this.router);
    this.sucursalesView = new SucursalesView(this.router);
    this.stockView = new StockView(this.router);
    this.historialMovimientosView = new HistorialMovimientosView(this.router);
    this.ajustesStockView = new AjustesStockView(this.router);
    this.movimientosView = this.historialMovimientosView;
    this.temporadasView = new TemporadasView(this.router);
    this.plantacionesView = new PlantacionesView(this.router);
    this.transferenciasView = new TransferenciasView(this.router);
    this.cosechasView = new CosechasView(this.router);
    this.movimientosComercialesView = new MovimientosComercialesView(this.router);
    this.reportesView = new ReportesView(this.router);

    // Nuevas vistas M4
    this.perfilView = new PerfilView(this.router);
    this.empleadosView = new EmpleadosView(this.router);
    this.rolesView = new RolesView(this.router);
    this.preciosReferenciaView = new PreciosReferenciaView(this.router);

    this.isSidebarCollapsed = false;
    this.isMobileOpen = false;
  }

  init() {
    // Interceptor 401: cuando una petición no autorizada llega, redirigir a #login
    apiClient.setOnUnauthorized(() => {
      toast.warning('Sesión expirada. Por favor identifíquese nuevamente.');
      this.router.navigate('#login');
    });

    // Registrar rutas de la SPA con sus respectivos permisos requeridos (Feature 26)
    this.router.register('#login', this.loginView, false, 'Iniciar Sesión', null);
    this.router.register('#dashboard', this.dashboardView, true, 'Panel de Control', null);
    this.router.register('#reportes', this.reportesView, true, 'Reportes Ejecutivos', 'REPORTES_VER');

    // Movimientos comerciales (reemplaza #ventas con fallback de compatibilidad)
    this.router.register('#movimientos-comerciales', this.movimientosComercialesView, true, 'Movimientos Comerciales', 'MOVCOM_VER');
    this.router.register('#ventas', this.movimientosComercialesView, true, 'Movimientos Comerciales', 'MOVCOM_VER');

    // Stock y Bulbos
    this.router.register('#stock', this.stockView, true, 'Consulta de Stock', 'STOCK_VER');
    this.router.register('#movimientos', this.historialMovimientosView, true, 'Historial de Movimientos', 'STOCK_VER');
    this.router.register('#ajustes-stock', this.ajustesStockView, true, 'Ajustes de Stock', 'STOCK_VER');

    // Producción y Campo
    this.router.register('#temporadas', this.temporadasView, true, 'Gestión de Temporadas', 'TEMPORADAS_VER');
    this.router.register('#plantaciones', this.plantacionesView, true, 'Estructura Productiva y Plantaciones', 'PLANTACIONES_VER');
    this.router.register('#transferencias', this.transferenciasView, true, 'Transferencias de Lotes Completos', 'TRANSFERENCIAS_VER');
    this.router.register('#cosechas', this.cosechasView, true, 'Cosechas por Sucursal', 'COSECHAS_VER');

    // Administración
    this.router.register('#clientes', this.clientesView, true, 'Gestión de Clientes', 'CLIENTES_VER');
    this.router.register('#sucursales', this.sucursalesView, true, 'Gestión de Sucursales', 'SUCURSALES_VER');
    this.router.register('#empleados', this.empleadosView, true, 'Gestión de Empleados', 'EMPLEADOS_VER');
    this.router.register('#roles', this.rolesView, true, 'Roles y Permisos', 'ROLES_VER');
    this.router.register('#precios-referencia', this.preciosReferenciaView, true, 'Precios de Referencia', ['PRECIOS_REF_VER', 'PRECIOS_REF_GESTIONAR']);

    // Mi Perfil
    this.router.register('#perfil', this.perfilView, true, 'Mi Perfil de Usuario', null);

    // Escucha cambios de ruta
    this.router.setOnRouteChange((routeInfo) => this._renderRoute(routeInfo));

    // Iniciar router
    this.router.start();
  }

  _renderRoute({ path, params, title, isLogin, view }) {
    document.title = `${title} | SIGA`;

    if (isLogin) {
      // Pantalla limpia para Login sin layout empresarial
      const faModal = document.getElementById('modal-forzar-activacion');
      if (faModal) faModal.remove();

      this.appRoot.className = 'login-mode';
      this.appRoot.innerHTML = '<div id="login-container"></div>';
      view.render(this.appRoot.querySelector('#login-container'), params);
      return;
    }

    // Comprobar si el usuario autenticado requiere cambio obligatorio de contraseña para activar su cuenta
    const currentUser = authService.getUsuarioActual();
    if (currentUser && (currentUser.activado === false || currentUser.debeCambiarPassword === true)) {
      this._forzarActivacionModal(currentUser);
      return;
    } else {
      const faModal = document.getElementById('modal-forzar-activacion');
      if (faModal) faModal.remove();
    }

    // Si ya existe la estructura del layout principal, actualizamos vista y navegación
    if (!this.appRoot.querySelector('.app-container')) {
      this._buildAppLayout();
    }

    // Actualizar título de la barra superior
    const titleEl = document.getElementById('topbar-page-title');
    if (titleEl) titleEl.textContent = title;

    // Actualizar perfil de usuario en la barra superior
    this._updateUserProfile();

    // Aplicar filtrado de navegación según permisos del usuario activo (Feature 26)
    this._applyNavPermissionFiltering();

    // Actualizar indicador activo en el sidebar y auto-expandir la sección contenedora (Feature 25)
    this._updateActiveNav(path);

    // Renderizar la vista dentro del contenedor principal
    const contentArea = document.getElementById('app-view-content');
    if (contentArea) {
      view.render(contentArea, params);
    }

    // Cerrar sidebar en pantallas móviles tras la navegación
    if (window.innerWidth <= 992) {
      this.setMobileSidebar(false);
    }
  }

  _buildAppLayout() {
    const user = authService.getUsuarioActual() || {
      nombre: 'Usuario SIGA',
      rol: 'Administrador'
    };

    const initials = user.nombre
      ? user.nombre.split(' ').map(n => n[0]).slice(0, 2).join('').toUpperCase()
      : 'US';

    this.appRoot.className = '';
    this.appRoot.innerHTML = `
      <div class="app-container">
        <!-- Backdrop para dispositivos móviles -->
        <div class="sidebar-backdrop" id="sidebar-backdrop"></div>

        <!-- Sidebar Lateral con 5 Secciones Colapsables (Feature 24 & 25) -->
        <aside class="app-sidebar" id="app-sidebar">
          <div class="sidebar-header">
            <a href="#dashboard" class="brand">
              <div class="brand-icon">S</div>
              <div class="brand-text">
                <span class="brand-title">SI<span>GA</span></span>
                <span class="brand-subtitle">Gestión Integral</span>
              </div>
            </a>
          </div>

          <nav class="sidebar-nav" id="sidebar-nav">
            <!-- 1. Principal -->
            <div class="nav-section" data-section="principal">
              <button type="button" class="nav-section-header" data-toggle="section" aria-expanded="true">
                <span class="nav-section-title">Principal</span>
                <svg class="nav-section-chevron" viewBox="0 0 24 24">
                  <polyline points="6 9 12 15 18 9"></polyline>
                </svg>
              </button>
              <div class="nav-section-content">
                <a href="#dashboard" class="nav-item" data-path="#dashboard">
                  <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <rect x="3" y="3" width="7" height="9"></rect>
                    <rect x="14" y="3" width="7" height="5"></rect>
                    <rect x="14" y="12" width="7" height="9"></rect>
                    <rect x="3" y="16" width="7" height="5"></rect>
                  </svg>
                  <span class="nav-text">Dashboard</span>
                </a>
                <a href="#reportes" class="nav-item" data-path="#reportes" data-permiso="REPORTES_VER">
                  <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <line x1="18" y1="20" x2="18" y2="10"></line>
                    <line x1="12" y1="20" x2="12" y2="4"></line>
                    <line x1="6" y1="20" x2="6" y2="14"></line>
                  </svg>
                  <span class="nav-text">Reportes Ejecutivos</span>
                </a>
              </div>
            </div>

            <!-- 2. Transacciones -->
            <div class="nav-section" data-section="movimientos-comerciales">
              <button type="button" class="nav-section-header" data-toggle="section" aria-expanded="true">
                <span class="nav-section-title">Transacciones</span>
                <svg class="nav-section-chevron" viewBox="0 0 24 24">
                  <polyline points="6 9 12 15 18 9"></polyline>
                </svg>
              </button>
              <div class="nav-section-content">
                <a href="#movimientos-comerciales" class="nav-item" data-path="#movimientos-comerciales" data-permiso="MOVCOM_VER">
                  <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
                    <polyline points="14 2 14 8 20 8"></polyline>
                    <line x1="16" y1="13" x2="8" y2="13"></line>
                    <line x1="16" y1="17" x2="8" y2="17"></line>
                    <polyline points="10 9 9 9 8 9"></polyline>
                  </svg>
                  <span class="nav-text">Movimientos Comerciales</span>
                </a>
                <a href="#movimientos" class="nav-item" data-path="#movimientos" data-permiso="STOCK_VER,MOVIMIENTOS_VER">
                  <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <polyline points="17 1 21 5 17 9"></polyline>
                    <path d="M3 11V9a4 4 0 0 1 4-4h14"></path>
                    <polyline points="7 23 3 19 7 15"></polyline>
                    <path d="M21 13v2a4 4 0 0 1-4 4H3"></path>
                  </svg>
                  <span class="nav-text">Movimientos</span>
                </a>
                <a href="#ajustes-stock" class="nav-item" data-path="#ajustes-stock" data-permiso="STOCK_VER">
                  <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M12 20v-6M6 20V10M18 20V4"></path>
                    <polyline points="17 1 21 5 17 9"></polyline>
                  </svg>
                  <span class="nav-text">Ajustes de Stock</span>
                </a>
              </div>
            </div>

            <!-- 3. Stock y Bulbos -->
            <div class="nav-section" data-section="stock">
              <button type="button" class="nav-section-header" data-toggle="section" aria-expanded="true">
                <span class="nav-section-title">Stock y Bulbos</span>
                <svg class="nav-section-chevron" viewBox="0 0 24 24">
                  <polyline points="6 9 12 15 18 9"></polyline>
                </svg>
              </button>
              <div class="nav-section-content">
                <a href="#stock" class="nav-item" data-path="#stock" data-permiso="STOCK_VER">
                  <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"></path>
                    <polyline points="3.27 6.96 12 12.01 20.73 6.96"></polyline>
                    <line x1="12" y1="22.08" x2="12" y2="12"></line>
                  </svg>
                  <span class="nav-text">Consulta Stock</span>
                </a>
              </div>
            </div>

            <!-- 4. Producción y Campo -->
            <div class="nav-section" data-section="produccion">
              <button type="button" class="nav-section-header" data-toggle="section" aria-expanded="true">
                <span class="nav-section-title">Producción y Campo</span>
                <svg class="nav-section-chevron" viewBox="0 0 24 24">
                  <polyline points="6 9 12 15 18 9"></polyline>
                </svg>
              </button>
              <div class="nav-section-content">
                <a href="#temporadas" class="nav-item" data-path="#temporadas" data-permiso="TEMPORADAS_VER">
                  <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect>
                    <line x1="16" y1="2" x2="16" y2="6"></line>
                    <line x1="8" y1="2" x2="8" y2="6"></line>
                    <line x1="3" y1="10" x2="21" y2="10"></line>
                  </svg>
                  <span class="nav-text">Temporadas</span>
                </a>
                <a href="#plantaciones" class="nav-item" data-path="#plantaciones" data-permiso="PLANTACIONES_VER">
                  <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M12 2L2 7l10 5 10-5-10-5z"></path>
                    <path d="M2 17l10 5 10-5"></path>
                    <path d="M2 12l10 5 10-5"></path>
                  </svg>
                  <span class="nav-text">Plantaciones</span>
                </a>
                <a href="#transferencias" class="nav-item" data-path="#transferencias" data-permiso="TRANSFERENCIAS_VER">
                  <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M17 1l4 4-4 4"></path>
                    <path d="M3 11V9a4 4 0 0 1 4-4h14"></path>
                    <path d="M7 23l-4-4 4-4"></path>
                    <path d="M21 13v2a4 4 0 0 1-4 4H3"></path>
                  </svg>
                  <span class="nav-text">Transferir Lotes</span>
                </a>
                <a href="#cosechas" class="nav-item" data-path="#cosechas" data-permiso="COSECHAS_VER">
                  <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M11 20A7 7 0 0 1 9.8 6.1C15.5 5 17 4.48 19 2c1 2 2 4.18 2 8 0 5.5-4.78 10-10 10Z"></path>
                    <path d="M2 21c0-3 1.85-5.36 5.08-6C9.5 14.52 12 13 13 12"></path>
                  </svg>
                  <span class="nav-text">Cosechas</span>
                </a>
              </div>
            </div>

            <!-- 5. Administración -->
            <div class="nav-section" data-section="administracion">
              <button type="button" class="nav-section-header" data-toggle="section" aria-expanded="true">
                <span class="nav-section-title">Administración</span>
                <svg class="nav-section-chevron" viewBox="0 0 24 24">
                  <polyline points="6 9 12 15 18 9"></polyline>
                </svg>
              </button>
              <div class="nav-section-content">
                <a href="#clientes" class="nav-item" data-path="#clientes" data-permiso="CLIENTES_VER">
                  <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path>
                    <circle cx="9" cy="7" r="4"></circle>
                    <path d="M23 21v-2a4 4 0 0 0-3-3.87"></path>
                    <path d="M16 3.13a4 4 0 0 1 0 7.75"></path>
                  </svg>
                  <span class="nav-text">Clientes</span>
                </a>
                <a href="#sucursales" class="nav-item" data-path="#sucursales" data-permiso="SUCURSALES_VER">
                  <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path>
                    <polyline points="9 22 9 12 15 12 15 22"></polyline>
                  </svg>
                  <span class="nav-text">Sucursales</span>
                </a>
                <a href="#empleados" class="nav-item" data-path="#empleados" data-permiso="EMPLEADOS_VER">
                  <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
                    <circle cx="12" cy="7" r="4"></circle>
                  </svg>
                  <span class="nav-text">Empleados</span>
                </a>
                <a href="#roles" class="nav-item" data-path="#roles" data-permiso="ROLES_VER">
                  <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"></path>
                  </svg>
                  <span class="nav-text">Roles y Permisos</span>
                </a>
                <a href="#precios-referencia" class="nav-item" data-path="#precios-referencia" data-permiso="PRECIOS_REF_VER,PRECIOS_REF_GESTIONAR">
                  <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <line x1="12" y1="1" x2="12" y2="23"></line>
                    <path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"></path>
                  </svg>
                  <span class="nav-text">Precios de Referencia</span>
                </a>
              </div>
            </div>
          </nav>

          <div class="sidebar-footer">
            <div class="system-status-indicator">
              <span class="status-dot"></span>
              <span>SIGA Conectado</span>
            </div>
          </div>
        </aside>

        <!-- Contenedor Principal (Topbar + Contenido) -->
        <div class="app-main">
          <!-- Topbar Superior (Feature 27: sin campana de notificaciones) -->
          <header class="app-topbar">
            <div class="topbar-left">
              <button type="button" class="btn-sidebar-toggle" id="btn-toggle-sidebar" aria-label="Colapsar o expandir menú">
                <svg class="icon-svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <line x1="3" y1="12" x2="21" y2="12"></line>
                  <line x1="3" y1="6" x2="21" y2="6"></line>
                  <line x1="3" y1="18" x2="21" y2="18"></line>
                </svg>
              </button>
              <h2 class="page-title" id="topbar-page-title">Panel de Control</h2>
            </div>

            <div class="topbar-right">
              <!-- Perfil de Usuario con Navegación a #perfil (Feature 27) -->
              <div class="user-profile" id="user-profile-badge" role="button" tabindex="0" title="Ver mi perfil">
                <div class="user-avatar" id="topbar-user-avatar">${escapeHtml(initials)}</div>
                <div class="user-info">
                  <span class="user-name" id="topbar-user-name">${escapeHtml(user.nombre)}</span>
                  <span class="user-role" id="topbar-user-role">${escapeHtml(user.rol)}</span>
                </div>
              </div>

              <!-- Botón de Cerrar Sesión -->
              <button type="button" class="btn btn-sm btn-outline" id="btn-topbar-logout" title="Cerrar sesión segura">
                <svg class="icon-svg sm" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="color: var(--danger);">
                  <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"></path>
                  <polyline points="16 17 21 12 16 7"></polyline>
                  <line x1="21" y1="12" x2="9" y2="12"></line>
                </svg>
                <span>Salir</span>
              </button>
            </div>
          </header>

          <!-- Área dinámica de vistas -->
          <main class="app-content" id="app-view-content"></main>
        </div>
      </div>
    `;

    // 1. Inicializar persistencia y eventos de colapso de secciones (Features 24 & 25)
    this._initSidebarSections();

    // 2. Conectar navegación al hacer clic en el badge de perfil (Feature 27)
    const profileBadge = document.getElementById('user-profile-badge');
    if (profileBadge) {
      profileBadge.addEventListener('click', () => {
        this.router.navigate('#perfil');
      });
      profileBadge.addEventListener('keydown', (e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault();
          this.router.navigate('#perfil');
        }
      });
    }

    // 3. Eventos del Layout
    const toggleBtn = document.getElementById('btn-toggle-sidebar');
    const backdrop = document.getElementById('sidebar-backdrop');
    const logoutBtn = document.getElementById('btn-topbar-logout');

    if (toggleBtn) {
      toggleBtn.addEventListener('click', () => {
        if (window.innerWidth <= 992) {
          this.setMobileSidebar(!this.isMobileOpen);
        } else {
          this.toggleSidebarCollapse();
        }
      });
    }

    if (backdrop) {
      backdrop.addEventListener('click', () => {
        this.setMobileSidebar(false);
      });
    }

    if (logoutBtn) {
      logoutBtn.addEventListener('click', async (e) => {
        e.preventDefault();
        if (confirm('¿Está seguro de que desea cerrar la sesión actual?')) {
          await authService.logout();
          toast.info('Sesión finalizada correctamente.');
          window.location.hash = '#login';
          this.router.navigate('#login');
        }
      });
    }
  }

  _initSidebarSections() {
    const savedState = getCollapsedSectionsState();
    const sections = this.appRoot.querySelectorAll('.nav-section');

    sections.forEach(section => {
      const sectionId = section.getAttribute('data-section');
      const header = section.querySelector('.nav-section-header');
      const content = section.querySelector('.nav-section-content');

      // Restaurar estado guardado en localStorage
      if (savedState[sectionId] === true) {
        section.classList.add('collapsed');
        if (header) header.setAttribute('aria-expanded', 'false');
        if (content) {
          content.style.maxHeight = '0px';
          content.style.opacity = '0';
        }
      } else {
        section.classList.remove('collapsed');
        if (header) header.setAttribute('aria-expanded', 'true');
        if (content) {
          content.style.maxHeight = '';
          content.style.opacity = '1';
        }
      }

      // Conectar evento toggle
      if (header) {
        header.addEventListener('click', () => {
          const isCurrentlyCollapsed = section.classList.contains('collapsed');

          if (isCurrentlyCollapsed) {
            // Expandir de 0 a scrollHeight instantáneamente
            section.classList.remove('collapsed');
            header.setAttribute('aria-expanded', 'true');
            if (content) {
              const fullHeight = content.scrollHeight;
              content.style.maxHeight = fullHeight + 'px';
              content.style.opacity = '1';

              const onEnd = (e) => {
                if (e.propertyName === 'max-height') {
                  content.style.maxHeight = '';
                  content.removeEventListener('transitionend', onEnd);
                }
              };
              content.addEventListener('transitionend', onEnd);
            }
          } else {
            // Colapsar de scrollHeight a 0 sin latencia
            if (content) {
              content.style.maxHeight = content.scrollHeight + 'px';
              void content.offsetHeight; // Forzar reflow para registrar el punto de inicio exacto
              content.style.maxHeight = '0px';
              content.style.opacity = '0';
            }
            section.classList.add('collapsed');
            header.setAttribute('aria-expanded', 'false');
          }

          if (sectionId) {
            saveCollapsedSectionState(sectionId, !isCurrentlyCollapsed);
          }
        });
      }
    });
  }

  _applyNavPermissionFiltering() {
    // 1. Filtrar ítems individuales según authService.tienePermiso(codigo) o lista separada por comas (Feature 26)
    const navItems = this.appRoot.querySelectorAll('.sidebar-nav .nav-item[data-permiso]');
    navItems.forEach(item => {
      const permisoAttr = item.getAttribute('data-permiso');
      if (permisoAttr) {
        const perms = permisoAttr.split(',').map(p => p.trim());
        const allowed = perms.length > 1 ? authService.tieneAlgunPermiso(perms) : authService.tienePermiso(perms[0]);
        item.classList.toggle('hidden', !allowed);
      } else {
        item.classList.remove('hidden');
      }
    });

    // 2. Ocultar secciones completas si todos sus ítems están ocultos
    const sections = this.appRoot.querySelectorAll('.sidebar-nav .nav-section');
    sections.forEach(section => {
      const visibleItems = section.querySelectorAll('.nav-section-content .nav-item:not(.hidden)');
      if (visibleItems.length === 0) {
        section.classList.add('hidden');
      } else {
        section.classList.remove('hidden');
      }
    });
  }

  toggleSidebarCollapse() {
    this.isSidebarCollapsed = !this.isSidebarCollapsed;
    document.body.classList.toggle('sidebar-collapsed', this.isSidebarCollapsed);
  }

  setMobileSidebar(open) {
    this.isMobileOpen = open;
    document.body.classList.toggle('sidebar-open', open);
  }

  _updateUserProfile() {
    const user = authService.getUsuarioActual();
    if (!user) return;

    const nameEl = document.getElementById('topbar-user-name');
    const roleEl = document.getElementById('topbar-user-role');
    const avatarEl = document.getElementById('topbar-user-avatar');

    if (nameEl) nameEl.textContent = user.nombre;
    if (roleEl) roleEl.textContent = user.rol;
    if (avatarEl && user.nombre) {
      avatarEl.textContent = user.nombre.split(' ').map(n => n[0]).slice(0, 2).join('').toUpperCase();
    }
  }

  _updateActiveNav(currentPath) {
    const navItems = this.appRoot.querySelectorAll('.sidebar-nav .nav-item');
    let activeItem = null;

    navItems.forEach(item => {
      const itemPath = item.getAttribute('data-path');
      const matches = (itemPath === currentPath) ||
        (currentPath === '#ventas' && itemPath === '#movimientos-comerciales') ||
        (currentPath === '#movimientos-comerciales' && itemPath === '#ventas');

      if (matches) {
        item.classList.add('active');
        activeItem = item;
      } else {
        item.classList.remove('active');
      }
    });

    // Auto-expansión: expandir la sección que contiene la ruta activa si estaba colapsada (Feature 25)
    if (activeItem) {
      const parentSection = activeItem.closest('.nav-section');
      if (parentSection && parentSection.classList.contains('collapsed')) {
        parentSection.classList.remove('collapsed');
        const content = parentSection.querySelector('.nav-section-content');
        if (content) {
          content.style.maxHeight = '';
          content.style.opacity = '1';
        }
        const header = parentSection.querySelector('.nav-section-header');
        if (header) header.setAttribute('aria-expanded', 'true');

        const sectionId = parentSection.getAttribute('data-section');
        if (sectionId) {
          saveCollapsedSectionState(sectionId, false);
        }
      }
    }
  }

  _forzarActivacionModal(user) {
    let modal = document.getElementById('modal-forzar-activacion');
    if (!modal) {
      modal = document.createElement('div');
      modal.id = 'modal-forzar-activacion';
      modal.className = 'modal-backdrop is-active';
      modal.setAttribute('role', 'dialog');
      modal.setAttribute('aria-modal', 'true');
      modal.innerHTML = `
        <div class="modal-dialog" style="max-width: 480px;">
          <div class="modal-header" style="background: var(--surface); border-bottom: 1px solid var(--border); padding: 1.25rem 1.5rem;">
            <h3 class="modal-title text-base font-bold" style="color: var(--primary);">Activación de Cuenta requerida</h3>
          </div>
          <div class="modal-body" style="padding: 1.5rem;">
            <div class="alert alert-info mb-3" style="font-size: 0.8125rem; line-height: 1.4;">
              <strong>¡Primer ingreso detectado!</strong> Por motivos de seguridad del sistema, debes cambiar tu contraseña inicial (DNI) para activar tu cuenta y poder utilizar la aplicación.
            </div>
            <form id="form-forzar-activacion">
              <div class="form-group mb-3">
                <label class="form-label font-medium" for="fa-actual">Contraseña actual (tu DNI)</label>
                <input type="password" id="fa-actual" class="form-control" required />
              </div>
              <div class="form-group mb-3">
                <label class="form-label font-medium" for="fa-nueva">Nueva Contraseña <span class="text-danger">*</span></label>
                <input type="password" id="fa-nueva" class="form-control" placeholder="Mínimo 6 caracteres" required minlength="6" />
              </div>
              <div class="form-group mb-3">
                <label class="form-label font-medium" for="fa-confirmar">Confirmar Nueva Contraseña <span class="text-danger">*</span></label>
                <input type="password" id="fa-confirmar" class="form-control" placeholder="Reingrese la nueva contraseña" required minlength="6" />
              </div>
              <div id="fa-error" class="alert alert-danger hidden mb-3" style="font-size: 0.8125rem;"></div>
              <div class="d-flex justify-between align-center mt-4">
                <button type="button" class="btn btn-outline" id="btn-fa-logout">Cerrar Sesión</button>
                <button type="submit" class="btn btn-primary" id="btn-fa-submit">
                  <span class="btn-text">Activar Cuenta</span>
                  <span class="btn-spinner"></span>
                </button>
              </div>
            </form>
          </div>
        </div>
      `;
      document.body.appendChild(modal);

      modal.querySelector('#btn-fa-logout').onclick = async () => {
        await authService.logout();
        modal.remove();
        this.router.navigate('#login');
      };

      modal.querySelector('#form-forzar-activacion').onsubmit = async (e) => {
        e.preventDefault();
        const errorBox = modal.querySelector('#fa-error');
        const submitBtn = modal.querySelector('#btn-fa-submit');
        errorBox.classList.add('hidden');
        errorBox.textContent = '';

        const pActual = modal.querySelector('#fa-actual').value.trim();
        const pNueva = modal.querySelector('#fa-nueva').value;
        const pConfirmar = modal.querySelector('#fa-confirmar').value;

        if (!pActual || !pNueva || !pConfirmar) {
          errorBox.textContent = 'Por favor complete todos los campos.';
          errorBox.classList.remove('hidden');
          return;
        }

        if (pNueva.length < 6) {
          errorBox.textContent = 'La nueva contraseña debe tener al menos 6 caracteres.';
          errorBox.classList.remove('hidden');
          return;
        }

        if (pNueva === pActual) {
          errorBox.textContent = 'La nueva contraseña no puede ser igual a tu contraseña actual.';
          errorBox.classList.remove('hidden');
          return;
        }

        if (pNueva !== pConfirmar) {
          errorBox.textContent = 'Las contraseñas no coinciden.';
          errorBox.classList.remove('hidden');
          return;
        }

        submitBtn.classList.add('is-loading');
        submitBtn.disabled = true;

        try {
          await authService.cambiarPassword(user.id, pActual, pNueva);
          modal.remove();
          toast.success(`Cuenta activada con éxito. Bienvenido/a, ${user.nombre}.`, 'Activación Exitosa');
          this.router.navigate('#dashboard');
        } catch (err) {
          errorBox.textContent = err.message || 'Error al cambiar la contraseña. Verifique su contraseña actual.';
          errorBox.classList.remove('hidden');
        } finally {
          submitBtn.classList.remove('is-loading');
          submitBtn.disabled = false;
        }
      };
    }
  }
}

// Iniciar aplicación cuando el DOM esté listo
document.addEventListener('DOMContentLoaded', () => {
  const app = new App();
  app.init();
});
