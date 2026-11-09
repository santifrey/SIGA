/**
 * SIGA - js/app.js
 * Orquestador principal de la Single Page Application (SPA)
 */

import { Router } from './router.js';
import { apiClient } from './utils/api-client.js';
import { authService } from './services/auth-service.js';
import { toast } from './utils/toast.js';

import { LoginView } from './views/login-view.js';
import { DashboardView } from './views/dashboard-view.js';
import { ClientesView } from './views/clientes-view.js';
import { SucursalesView } from './views/sucursales-view.js';
import { StockView } from './views/stock-view.js';
import { MovimientosView } from './views/movimientos-view.js';
import { TemporadasView } from './views/temporadas-view.js';
import { PlantacionesView } from './views/plantaciones-view.js';
import { TransferenciasView } from './views/transferencias-view.js';
import { CosechasView } from './views/cosechas-view.js';
import { VentasView } from './views/ventas-view.js';
import { ReportesView } from './views/reportes-view.js';

class App {
  constructor() {
    this.appRoot = document.getElementById('app');
    this.router = new Router();

    // Vistas
    this.loginView = new LoginView(this.router);
    this.dashboardView = new DashboardView(this.router);
    this.clientesView = new ClientesView(this.router);
    this.sucursalesView = new SucursalesView(this.router);
    this.stockView = new StockView(this.router);
    this.movimientosView = new MovimientosView(this.router);
    this.temporadasView = new TemporadasView(this.router);
    this.plantacionesView = new PlantacionesView(this.router);
    this.transferenciasView = new TransferenciasView(this.router);
    this.cosechasView = new CosechasView(this.router);
    this.ventasView = new VentasView(this.router);
    this.reportesView = new ReportesView(this.router);

    this.isSidebarCollapsed = false;
    this.isMobileOpen = false;
  }

  init() {
    // Interceptor 401: cuando una petición no autorizada llega, redirigir a #login
    apiClient.setOnUnauthorized(() => {
      toast.warning('Sesión expirada. Por favor identifíquese nuevamente.');
      this.router.navigate('#login');
    });

    // Registrar rutas de la SPA
    this.router.register('#login', this.loginView, false, 'Iniciar Sesión');
    this.router.register('#dashboard', this.dashboardView, true, 'Panel de Control');
    this.router.register('#reportes', this.reportesView, true, 'Reportes Ejecutivos y Trazabilidad');
    this.router.register('#clientes', this.clientesView, true, 'Gestión de Clientes');
    this.router.register('#sucursales', this.sucursalesView, true, 'Gestión de Sucursales');
    this.router.register('#ventas', this.ventasView, true, 'Ventas y Circuito Administrativo');
    this.router.register('#stock', this.stockView, true, 'Consulta de Stock');
    this.router.register('#movimientos', this.movimientosView, true, 'Movimientos de Stock');
    this.router.register('#temporadas', this.temporadasView, true, 'Gestión de Temporadas');
    this.router.register('#plantaciones', this.plantacionesView, true, 'Estructura Productiva y Plantaciones');
    this.router.register('#transferencias', this.transferenciasView, true, 'Transferencias de Lotes Completos');
    this.router.register('#cosechas', this.cosechasView, true, 'Cosechas por Sucursal');

    // Escucha cambios de ruta
    this.router.setOnRouteChange((routeInfo) => this._renderRoute(routeInfo));

    // Iniciar router
    this.router.start();
  }

  _renderRoute({ path, params, title, isLogin, view }) {
    document.title = `${title} | SIGA`;

    if (isLogin) {
      // Pantalla limpia para Login sin layout empresarial
      this.appRoot.className = 'login-mode';
      this.appRoot.innerHTML = '<div id="login-container"></div>';
      view.render(this.appRoot.querySelector('#login-container'), params);
      return;
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

    // Actualizar indicador activo en el sidebar
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

        <!-- Sidebar Lateral -->
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

          <nav class="sidebar-nav">
            <div class="nav-section-title">Principal</div>
            <a href="#dashboard" class="nav-item" data-path="#dashboard">
              <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <rect x="3" y="3" width="7" height="9"></rect>
                <rect x="14" y="3" width="7" height="5"></rect>
                <rect x="14" y="12" width="7" height="9"></rect>
                <rect x="3" y="16" width="7" height="5"></rect>
              </svg>
              <span class="nav-text">Dashboard</span>
            </a>

            <a href="#reportes" class="nav-item" data-path="#reportes">
              <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="18" y1="20" x2="18" y2="10"></line>
                <line x1="12" y1="20" x2="12" y2="4"></line>
                <line x1="6" y1="20" x2="6" y2="14"></line>
              </svg>
              <span class="nav-text">Reportes Ejecutivos</span>
            </a>

            <div class="nav-section-title">Administración</div>
            <a href="#clientes" class="nav-item" data-path="#clientes">
              <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path>
                <circle cx="9" cy="7" r="4"></circle>
                <path d="M23 21v-2a4 4 0 0 0-3-3.87"></path>
                <path d="M16 3.13a4 4 0 0 1 0 7.75"></path>
              </svg>
              <span class="nav-text">Clientes</span>
            </a>

            <a href="#sucursales" class="nav-item" data-path="#sucursales">
              <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path>
                <polyline points="9 22 9 12 15 12 15 22"></polyline>
              </svg>
              <span class="nav-text">Sucursales</span>
            </a>

            <a href="#ventas" class="nav-item" data-path="#ventas">
              <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
                <polyline points="14 2 14 8 20 8"></polyline>
                <line x1="16" y1="13" x2="8" y2="13"></line>
                <line x1="16" y1="17" x2="8" y2="17"></line>
                <polyline points="10 9 9 9 8 9"></polyline>
              </svg>
              <span class="nav-text">Ventas y Contratos</span>
            </a>

            <div class="nav-section-title">Stock y Bulbos</div>
            <a href="#stock" class="nav-item" data-path="#stock">
              <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"></path>
                <polyline points="3.27 6.96 12 12.01 20.73 6.96"></polyline>
                <line x1="12" y1="22.08" x2="12" y2="12"></line>
              </svg>
              <span class="nav-text">Consulta Stock</span>
            </a>

            <a href="#movimientos" class="nav-item" data-path="#movimientos">
              <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <polyline points="17 1 21 5 17 9"></polyline>
                <path d="M3 11V9a4 4 0 0 1 4-4h14"></path>
                <polyline points="7 23 3 19 7 15"></polyline>
                <path d="M21 13v2a4 4 0 0 1-4 4H3"></path>
              </svg>
              <span class="nav-text">Movimientos</span>
            </a>
            <div class="nav-section-title">Producción y Campo</div>
            <a href="#temporadas" class="nav-item" data-path="#temporadas">
              <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect>
                <line x1="16" y1="2" x2="16" y2="6"></line>
                <line x1="8" y1="2" x2="8" y2="6"></line>
                <line x1="3" y1="10" x2="21" y2="10"></line>
              </svg>
              <span class="nav-text">Temporadas</span>
            </a>

            <a href="#plantaciones" class="nav-item" data-path="#plantaciones">
              <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M12 2L2 7l10 5 10-5-10-5z"></path>
                <path d="M2 17l10 5 10-5"></path>
                <path d="M2 12l10 5 10-5"></path>
              </svg>
              <span class="nav-text">Plantaciones</span>
            </a>

            <a href="#transferencias" class="nav-item" data-path="#transferencias">
              <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M17 1l4 4-4 4"></path>
                <path d="M3 11V9a4 4 0 0 1 4-4h14"></path>
                <path d="M7 23l-4-4 4-4"></path>
                <path d="M21 13v2a4 4 0 0 1-4 4H3"></path>
              </svg>
              <span class="nav-text">Transferir Lotes</span>
            </a>

            <a href="#cosechas" class="nav-item" data-path="#cosechas">
              <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M11 20A7 7 0 0 1 9.8 6.1C15.5 5 17 4.48 19 2c1 2 2 4.18 2 8 0 5.5-4.78 10-10 10Z"></path>
                <path d="M2 21c0-3 1.85-5.36 5.08-6C9.5 14.52 12 13 13 12"></path>
              </svg>
              <span class="nav-text">Cosechas</span>
            </a>
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
          <!-- Topbar Superior -->
          <header class="app-topbar">
            <div class="topbar-left">
              <button type="button" class="btn-sidebar-toggle" id="btn-toggle-sidebar" aria-label="Colapsar o expandir menú">
                <svg class="icon-svg" viewBox="0 0 24 24">
                  <line x1="3" y1="12" x2="21" y2="12"></line>
                  <line x1="3" y1="6" x2="21" y2="6"></line>
                  <line x1="3" y1="18" x2="21" y2="18"></line>
                </svg>
              </button>
              <h2 class="page-title" id="topbar-page-title">Panel de Control</h2>
            </div>

            <div class="topbar-right">
              <!-- Perfil de Usuario -->
              <div class="user-profile" id="user-profile-badge" title="Usuario activo">
                <div class="user-avatar" id="topbar-user-avatar">${initials}</div>
                <div class="user-info">
                  <span class="user-name" id="topbar-user-name">${user.nombre}</span>
                  <span class="user-role" id="topbar-user-role">${user.rol}</span>
                </div>
              </div>

              <!-- Botón de Cerrar Sesión -->
              <button type="button" class="btn btn-sm btn-outline" id="btn-topbar-logout" title="Cerrar sesión segura">
                <svg class="icon-svg sm" viewBox="0 0 24 24" style="color: var(--danger);">
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

    // Eventos del Layout
    const toggleBtn = document.getElementById('btn-toggle-sidebar');
    const backdrop = document.getElementById('sidebar-backdrop');
    const logoutBtn = document.getElementById('btn-topbar-logout');

    toggleBtn.addEventListener('click', () => {
      if (window.innerWidth <= 992) {
        this.setMobileSidebar(!this.isMobileOpen);
      } else {
        this.toggleSidebarCollapse();
      }
    });

    backdrop.addEventListener('click', () => {
      this.setMobileSidebar(false);
    });

    logoutBtn.addEventListener('click', () => {
      if (confirm('¿Está seguro de que desea cerrar la sesión actual?')) {
        authService.logout();
        toast.info('Sesión finalizada correctamente.');
        this.router.navigate('#login');
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
    navItems.forEach(item => {
      const itemPath = item.getAttribute('data-path');
      if (itemPath === currentPath) {
        item.classList.add('active');
      } else {
        item.classList.remove('active');
      }
    });
  }
}

// Iniciar aplicación cuando el DOM esté listo
document.addEventListener('DOMContentLoaded', () => {
  const app = new App();
  app.init();
});
