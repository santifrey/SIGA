/**
 * SIGA - js/app.js
 * Orquestador principal de la Single Page Application (SPA)
 * Implementa Shell de 5 Secciones Colapsables, Animaciones, Persistencia Local,
 * Auto-expansión, Route Guards, Filtrado de Navegación por Permisos y Navegación de Perfil (Features 24-27).
 */

import { Router } from './router.js';
import { apiClient } from './utils/api-client.js';
import { authService } from './services/auth-service.js';
import { activacionCuentaModal } from './components/activacion-cuenta-modal.js';
import { loadTemplate } from './utils/template-loader.js';
import { toast } from './utils/toast.js';

import { LoginView } from './views/login-view.js';
import { DashboardView } from './views/dashboard-view.js';
import { ClientesView } from './views/clientes-view.js';
import { SucursalesView } from './views/sucursales-view.js';
import { StockView } from './views/stock-view.js';
import { HistorialMovimientosView } from './views/historial-movimientos-view.js';
import { AjustesStockView } from './views/ajustes-stock-view.js';
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

    this.loginView = new LoginView(this.router);
    this.dashboardView = new DashboardView(this.router);
    this.clientesView = new ClientesView(this.router);
    this.sucursalesView = new SucursalesView(this.router);
    this.stockView = new StockView(this.router);
    this.historialMovimientosView = new HistorialMovimientosView(this.router);
    this.ajustesStockView = new AjustesStockView(this.router);
    this.temporadasView = new TemporadasView(this.router);
    this.plantacionesView = new PlantacionesView(this.router);
    this.transferenciasView = new TransferenciasView(this.router);
    this.cosechasView = new CosechasView(this.router);
    this.movimientosComercialesView = new MovimientosComercialesView(this.router);
    this.reportesView = new ReportesView(this.router);

    this.perfilView = new PerfilView(this.router);
    this.empleadosView = new EmpleadosView(this.router);
    this.rolesView = new RolesView(this.router);
    this.preciosReferenciaView = new PreciosReferenciaView(this.router);

    this.isSidebarCollapsed = false;
    this.isMobileOpen = false;
  }

  init() {
    apiClient.setOnUnauthorized(() => {
      toast.warning('Sesión expirada. Por favor identifíquese nuevamente.');
      this.router.navigate('#login');
    });

    this.router.register('#login', this.loginView, false, 'Iniciar Sesión', null);
    this.router.register('#dashboard', this.dashboardView, true, 'Panel de Control', null);
    this.router.register('#reportes', this.reportesView, true, 'Reportes Ejecutivos', 'REPORTES_VER');

    // Movimientos comerciales
    this.router.register('#movimientos-comerciales', this.movimientosComercialesView, true, 'Movimientos Comerciales', 'MOVCOM_VER');

    // Stock y Bulbos
    this.router.register('#stock', this.stockView, true, 'Consulta de Stock', 'STOCK_VER');
    this.router.register('#movimientos', this.historialMovimientosView, true, 'Movimiento de Bulbos', 'STOCK_VER');
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

    this.router.setOnRouteChange((routeInfo) => this._renderRoute(routeInfo));
    this.router.start();
  }

  async _renderRoute({ path, params, title, isLogin, view }) {
    document.title = `${title} | SIGA`;

    if (isLogin) {
      activacionCuentaModal.remove();
      this.appRoot.className = 'login-mode';
      this.appRoot.innerHTML = '<div id="login-container"></div>';
      await view.render(this.appRoot.querySelector('#login-container'), params);
      return;
    }

    const currentUser = authService.getUsuarioActual();
    if (currentUser && (currentUser.activado === false || currentUser.debeCambiarPassword === true)) {
      await activacionCuentaModal.open(currentUser, this.router);
      return;
    } else {
      activacionCuentaModal.remove();
    }

    if (!this.appRoot.querySelector('.app-container')) {
      await this._buildAppLayout();
    }

    const titleEl = document.getElementById('topbar-page-title');
    if (titleEl) titleEl.textContent = title;

    this._updateUserProfile();
    this._applyNavPermissionFiltering();
    this._updateActiveNav(path);

    const contentArea = document.getElementById('app-view-content');
    if (contentArea) {
      await view.render(contentArea, params);
    }

    if (window.innerWidth <= 992) {
      this.setMobileSidebar(false);
    }
  }

  async _buildAppLayout() {
    this.appRoot.className = '';
    this.appRoot.innerHTML = await loadTemplate('app-layout.html');

    this._updateUserProfile();
    this._initSidebarSections();

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

      if (header) {
        header.addEventListener('click', () => {
          const isCurrentlyCollapsed = section.classList.contains('collapsed');

          if (isCurrentlyCollapsed) {
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
            if (content) {
              content.style.maxHeight = content.scrollHeight + 'px';
              void content.offsetHeight;
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
    const user = authService.getUsuarioActual() || {
      nombre: 'Usuario SIGA',
      rol: 'Administrador'
    };

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
      if (itemPath === currentPath) {
        item.classList.add('active');
        activeItem = item;
      } else {
        item.classList.remove('active');
      }
    });

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
}

function initApp() {
  const app = new App();
  app.init();
}

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', initApp);
} else {
  initApp();
}
