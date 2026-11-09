/**
 * SIGA - views/dashboard-view.js
 * Vista del Dashboard Principal y Métricas Ejecutivas
 */

import { authService } from '../services/auth-service.js';
import { clientesService } from '../services/clientes-service.js';
import { sucursalesService } from '../services/sucursales-service.js';
import { CONFIG } from '../config.js';

export class DashboardView {
  constructor(router) {
    this.router = router;
    this.container = null;
  }

  async render(rootElement) {
    this.container = rootElement;
    const user = authService.getUsuarioActual() || {
      nombre: 'Usuario SIGA',
      rol: 'Administrador',
      email: 'usuario@siga.com',
      sucursal: 'Casa Central'
    };

    // Render inicial con esqueleto / loader
    this.container.innerHTML = `
      <div class="fade-in">
        <!-- Tarjeta de Bienvenida -->
        <div class="card mb-4" style="background: linear-gradient(135deg, #4A154B 0%, #2b0b2c 100%); color: #ffffff; border: none;">
          <div class="card-body d-flex justify-between align-center flex-wrap gap-md" style="padding: 1.75rem 2rem;">
            <div>
              <div class="d-flex align-center gap-sm mb-2">
                <span class="badge" style="background: rgba(229, 142, 38, 0.25); color: #fcd34d; border: 1px solid rgba(229, 142, 38, 0.4);">
                  Panel de Control SIGA
                </span>
                <span class="text-xs" style="color: rgba(255, 255, 255, 0.7);">
                  ${new Date().toLocaleDateString('es-AR', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}
                </span>
              </div>
              <h2 style="color: #ffffff; font-size: 1.75rem; margin-bottom: 0.35rem;">
                Hola, ${user.nombre}
              </h2>
              <p style="color: #deb0e5; font-size: 0.9375rem; margin: 0;">
                Rol: <strong>${user.rol}</strong> | Asignado a: <strong>${user.sucursal || 'Central Córdoba'}</strong>
              </p>
            </div>
            <div class="d-flex gap-sm">
              <button class="btn btn-accent" id="btn-quick-new-client">
                <svg class="icon-svg sm" viewBox="0 0 24 24">
                  <path d="M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path>
                  <circle cx="8.5" cy="7" r="4"></circle>
                  <line x1="20" y1="8" x2="20" y2="14"></line>
                  <line x1="23" y1="11" x2="17" y2="11"></line>
                </svg>
                <span>Nuevo Cliente</span>
              </button>
              <button class="btn btn-secondary" id="btn-quick-new-branch" style="background: rgba(255,255,255,0.12); color: #fff; border-color: rgba(255,255,255,0.2);">
                <svg class="icon-svg sm" viewBox="0 0 24 24">
                  <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path>
                  <polyline points="9 22 9 12 15 12 15 22"></polyline>
                </svg>
                <span>Nueva Sucursal</span>
              </button>
            </div>
          </div>
        </div>

        <!-- Grilla de Métricas KPI -->
        <div class="metric-grid">
          <!-- Métrica: Total Clientes -->
          <div class="metric-card">
            <div class="metric-icon-wrap">
              <svg class="icon-svg lg" viewBox="0 0 24 24">
                <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path>
                <circle cx="9" cy="7" r="4"></circle>
                <path d="M23 21v-2a4 4 0 0 0-3-3.87"></path>
                <path d="M16 3.13a4 4 0 0 1 0 7.75"></path>
              </svg>
            </div>
            <div class="metric-details">
              <span class="metric-label">Cartera de Clientes</span>
              <div class="metric-value" id="kpi-total-clientes">--</div>
              <span class="metric-subtext" id="kpi-sub-clientes">Cargando datos...</span>
            </div>
          </div>

          <!-- Métrica: Sucursales Activas -->
          <div class="metric-card accent">
            <div class="metric-icon-wrap">
              <svg class="icon-svg lg" viewBox="0 0 24 24">
                <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path>
                <polyline points="9 22 9 12 15 12 15 22"></polyline>
              </svg>
            </div>
            <div class="metric-details">
              <span class="metric-label">Sucursales Activas</span>
              <div class="metric-value" id="kpi-total-sucursales">--</div>
              <span class="metric-subtext" id="kpi-sub-sucursales">Cargando sedes...</span>
            </div>
          </div>

          <!-- Métrica: Capacidad Total -->
          <div class="metric-card success">
            <div class="metric-icon-wrap">
              <svg class="icon-svg lg" viewBox="0 0 24 24">
                <rect x="2" y="7" width="20" height="14" rx="2" ry="2"></rect>
                <path d="M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16"></path>
              </svg>
            </div>
            <div class="metric-details">
              <span class="metric-label">Capacidad de Puestos</span>
              <div class="metric-value" id="kpi-capacidad">--</div>
              <span class="metric-subtext">Plazas operativas habilitadas</span>
            </div>
          </div>

          <!-- Métrica: Estado del Sistema -->
          <div class="metric-card info">
            <div class="metric-icon-wrap">
              <svg class="icon-svg lg" viewBox="0 0 24 24">
                <polyline points="22 12 18 12 15 21 9 3 6 12 2 12"></polyline>
              </svg>
            </div>
            <div class="metric-details">
              <span class="metric-label">Estado de Plataforma</span>
              <div class="metric-value" style="font-size: 1.35rem;" id="kpi-status-name">Online</div>
              <span class="metric-subtext" id="kpi-status-sub">API v1 / Fallback Mock Activo</span>
            </div>
          </div>
        </div>

        <!-- Filas de Contenido: Clientes Recientes y Sucursales -->
        <div style="display: grid; grid-template-columns: 2fr 1.2fr; gap: var(--spacing-xl); margin-bottom: var(--spacing-xl);">
          <!-- Clientes Recientes -->
          <div class="card">
            <div class="card-header">
              <div>
                <h3 class="card-title">Clientes Registrados Recientemente</h3>
                <span class="card-subtitle">Últimas incorporaciones a la base de datos</span>
              </div>
              <button class="btn btn-sm btn-outline" id="btn-goto-clientes">
                <span>Ver Todos</span>
                <svg class="icon-svg sm" viewBox="0 0 24 24"><polyline points="9 18 15 12 9 6"></polyline></svg>
              </button>
            </div>
            <div class="card-body" style="padding: 0;">
              <div class="table-responsive">
                <table class="table" id="dashboard-recent-clients-table">
                  <thead>
                    <tr>
                      <th>Cliente</th>
                      <th>Documento</th>
                      <th>Email</th>
                      <th>Estado</th>
                    </tr>
                  </thead>
                  <tbody id="dashboard-recent-clients-body">
                    <tr class="table-loading-row">
                      <td colspan="4">Cargando registros...</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
          </div>

          <!-- Distribución de Sucursales -->
          <div class="card">
            <div class="card-header">
              <div>
                <h3 class="card-title">Sedes y Sucursales</h3>
                <span class="card-subtitle">Cobertura geográfica operativa</span>
              </div>
              <button class="btn btn-sm btn-outline" id="btn-goto-sucursales">
                <span>Ver Sedes</span>
                <svg class="icon-svg sm" viewBox="0 0 24 24"><polyline points="9 18 15 12 9 6"></polyline></svg>
              </button>
            </div>
            <div class="card-body" id="dashboard-branches-list" style="padding: var(--spacing-md);">
              <div class="text-center text-muted" style="padding: 2rem;">Cargando sucursales...</div>
            </div>
          </div>
        </div>
      </div>
    `;

    // Conectar eventos de botones rápidos
    this.container.querySelector('#btn-quick-new-client').addEventListener('click', () => {
      this.router.navigate('#clientes?action=new');
    });

    this.container.querySelector('#btn-quick-new-branch').addEventListener('click', () => {
      this.router.navigate('#sucursales?action=new');
    });

    this.container.querySelector('#btn-goto-clientes').addEventListener('click', () => {
      this.router.navigate('#clientes');
    });

    this.container.querySelector('#btn-goto-sucursales').addEventListener('click', () => {
      this.router.navigate('#sucursales');
    });

    // Carga de datos asíncronos
    await this._loadDashboardData();
  }

  async _loadDashboardData() {
    try {
      const [metricasClientes, metricasSucursales, clientesData, sucursalesList] = await Promise.all([
        clientesService.obtenerMetricas(),
        sucursalesService.obtenerMetricas(),
        clientesService.listar({ page: 1, pageSize: 5 }),
        sucursalesService.listar()
      ]);

      // Métricas Clientes
      const totalClientesEl = this.container.querySelector('#kpi-total-clientes');
      const subClientesEl = this.container.querySelector('#kpi-sub-clientes');
      if (totalClientesEl) totalClientesEl.textContent = metricasClientes.total;
      if (subClientesEl) subClientesEl.textContent = `${metricasClientes.activos} activos / ${metricasClientes.inactivos} inactivos`;

      // Métricas Sucursales
      const totalSucEl = this.container.querySelector('#kpi-total-sucursales');
      const subSucEl = this.container.querySelector('#kpi-sub-sucursales');
      if (totalSucEl) totalSucEl.textContent = `${metricasSucursales.activas} / ${metricasSucursales.total}`;
      if (subSucEl) subSucEl.textContent = `${metricasSucursales.activas} sedes en operación`;

      // Capacidad
      const capEl = this.container.querySelector('#kpi-capacidad');
      if (capEl) capEl.textContent = `${metricasSucursales.capacidadTotal} puestos`;

      // Render Clientes Recientes
      const tbody = this.container.querySelector('#dashboard-recent-clients-body');
      if (tbody) {
        const items = clientesData.items || [];
        if (items.length === 0) {
          tbody.innerHTML = `<tr><td colspan="4" class="text-center text-muted" style="padding: 2rem;">No hay clientes registrados aún.</td></tr>`;
        } else {
          tbody.innerHTML = items.map(c => `
            <tr>
              <td>
                <div class="font-semibold text-main">${c.nombre} ${c.apellido}</div>
                <small class="text-muted">${c.codigo || 'S/C'}</small>
              </td>
              <td>${c.tipoDocumento || 'DNI'} ${c.documento}</td>
              <td><span class="text-muted text-sm">${c.email}</span></td>
              <td>
                <span class="badge ${c.activo ? 'badge-success' : 'badge-danger'}">
                  <span class="badge-dot"></span>
                  ${c.activo ? 'Activo' : 'Inactivo'}
                </span>
              </td>
            </tr>
          `).join('');
        }
      }

      // Render Sucursales
      const branchContainer = this.container.querySelector('#dashboard-branches-list');
      if (branchContainer) {
        if (!sucursalesList || sucursalesList.length === 0) {
          branchContainer.innerHTML = `<div class="text-center text-muted">No hay sucursales registradas.</div>`;
        } else {
          branchContainer.innerHTML = sucursalesList.slice(0, 4).map(s => `
            <div style="display: flex; align-items: center; justify-content: space-between; padding: 0.75rem; border-radius: var(--radius-md); margin-bottom: 0.5rem; background: var(--surface-alt);">
              <div style="display: flex; align-items: center; gap: 0.75rem;">
                <div style="width: 36px; height: 36px; border-radius: var(--radius-sm); background: var(--primary-100); color: var(--primary); display: flex; align-items: center; justify-content: center; font-weight: 700;">
                  ${s.codigo ? s.codigo.substring(4, 7) : 'SUC'}
                </div>
                <div>
                  <div class="font-medium text-sm text-main">${s.nombre}</div>
                  <div class="text-xs text-muted">${s.ciudad} • Capacidad: ${s.capacidad || 0}</div>
                </div>
              </div>
              <span class="badge ${s.activa ? 'badge-success' : 'badge-warning'}">
                ${s.activa ? 'Activa' : 'Inactiva'}
              </span>
            </div>
          `).join('');
        }
      }

    } catch (err) {
      console.error('[DashboardView] Error al cargar métricas:', err);
    }
  }
}
