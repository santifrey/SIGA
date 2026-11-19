/**
 * SIGA - views/dashboard-view.js
 * Vista del Dashboard Principal y Métricas Ejecutivas
 */

import { authService } from '../services/auth-service.js';
import { clientesService } from '../services/clientes-service.js';
import { sucursalesService } from '../services/sucursales-service.js';
import { loadTemplate } from '../utils/template-loader.js';
import { CONFIG } from '../config.js';
import { escapeHtml } from '../utils/html.js';

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

    this.container.innerHTML = await loadTemplate('dashboard.html');

    const dateEl = this.container.querySelector('#dashboard-current-date');
    if (dateEl) {
      dateEl.textContent = new Date().toLocaleDateString('es-AR', {
        weekday: 'long',
        year: 'numeric',
        month: 'long',
        day: 'numeric'
      });
    }

    const titleEl = this.container.querySelector('#dashboard-welcome-title');
    if (titleEl) titleEl.textContent = `Hola, ${user.nombre}`;

    const subtitleEl = this.container.querySelector('#dashboard-welcome-subtitle');
    if (subtitleEl) {
      subtitleEl.innerHTML = `Rol: <strong>${escapeHtml(user.rol)}</strong> | Asignado a: <strong>${escapeHtml(user.sucursal || 'Central Córdoba')}</strong>`;
    }

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
                <div class="font-semibold text-main">${escapeHtml(c.nombre)} ${escapeHtml(c.apellido)}</div>
                <small class="text-muted">${escapeHtml(c.codigo || 'S/C')}</small>
              </td>
              <td>${escapeHtml(c.tipoDocumento || 'DNI')} ${escapeHtml(c.documento)}</td>
              <td><span class="text-muted text-sm">${escapeHtml(c.email)}</span></td>
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
                  ${escapeHtml(s.codigo ? s.codigo.substring(4, 7) : 'SUC')}
                </div>
                <div>
                  <div class="font-medium text-sm text-main">${escapeHtml(s.nombre)}</div>
                  <div class="text-xs text-muted">${escapeHtml(s.ciudad)} • Capacidad: ${escapeHtml(s.capacidad || 0)}</div>
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
