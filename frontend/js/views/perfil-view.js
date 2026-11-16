/**
 * SIGA - views/perfil-view.js
 * Vista de Perfil de Usuario (#perfil) (Feature 29)
 * Muestra información personal, rol, estado, último acceso y catálogo de permisos agrupados por módulo.
 */

import { authService } from '../services/auth-service.js';
import { toast } from '../utils/toast.js';
import { escapeHtml } from '../utils/escape-html.js';

export class PerfilView {
  constructor(router) {
    this.router = router;
    this.container = null;
    this.perfil = null;
  }

  async render(rootElement) {
    this.container = rootElement;
    this.container.innerHTML = `
      <div class="fade-in">
        <div class="d-flex justify-between align-center mb-4 flex-wrap gap-md">
          <div>
            <h1 style="font-size: 1.5rem; margin-bottom: 0.25rem;">Mi Perfil</h1>
            <p class="text-muted text-sm">Información de usuario, credenciales y permisos asignados en el sistema</p>
          </div>
          <button class="btn btn-secondary" id="btn-refresh-perfil" title="Actualizar datos">
            <svg class="icon-svg sm" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <polyline points="23 4 23 10 17 10"></polyline>
              <polyline points="1 20 1 14 7 14"></polyline>
              <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"></path>
            </svg>
            <span>Refrescar</span>
          </button>
        </div>

        <div id="perfil-loading" class="text-center text-muted" style="padding: 3rem;">
          <div class="btn-spinner" style="display:inline-block; width:30px; height:30px; border-width:3px;"></div>
          <p class="mt-2">Cargando perfil de usuario...</p>
        </div>

        <div id="perfil-content" class="hidden"></div>
      </div>
    `;

    this.container.querySelector('#btn-refresh-perfil').addEventListener('click', () => this._loadPerfil());
    await this._loadPerfil();
  }

  async _loadPerfil() {
    const loadingEl = this.container.querySelector('#perfil-loading');
    const contentEl = this.container.querySelector('#perfil-content');
    loadingEl.classList.remove('hidden');
    contentEl.classList.add('hidden');

    try {
      this.perfil = await authService.obtenerPerfilCompleto();
      this._renderContent();
      loadingEl.classList.add('hidden');
      contentEl.classList.remove('hidden');
    } catch (err) {
      loadingEl.innerHTML = `
        <div class="alert alert-danger" style="display:inline-block; text-align:left;">
          <strong>Error al cargar el perfil:</strong> ${escapeHtml(err.message || 'Error de conexión')}
          <div class="mt-2">
            <button class="btn btn-sm btn-outline-primary" id="btn-retry-perfil">Reintentar</button>
          </div>
        </div>
      `;
      loadingEl.querySelector('#btn-retry-perfil')?.addEventListener('click', () => this._loadPerfil());
      toast.error('No se pudo cargar la información del perfil.');
    }
  }

  _renderContent() {
    const p = this.perfil;
    const contentEl = this.container.querySelector('#perfil-content');
    const initials = p.nombreCompleto
      ? p.nombreCompleto.split(' ').map(n => n[0]).slice(0, 2).join('').toUpperCase()
      : (p.username ? p.username.substring(0, 2).toUpperCase() : 'US');

    const fechaAcceso = p.ultimoAcceso
      ? new Date(p.ultimoAcceso).toLocaleString('es-AR', { dateStyle: 'medium', timeStyle: 'short' })
      : 'Primer acceso registrado';

    const estadoStr = typeof p.estado === 'string'
      ? p.estado
      : (p.estado === 1 ? 'Activo' : (p.estado === 2 ? 'Bloqueado' : 'Inactivo'));

    // Agrupación de permisos por módulo
    const grupos = this._agruparPermisos(p.permisos || []);

    contentEl.innerHTML = `
      <div class="d-flex flex-column gap-lg">
        <!-- 1. Tarjeta Resumen de Identidad -->
        <div class="card" style="padding: 1.5rem; background: linear-gradient(135deg, var(--surface) 0%, var(--surface-alt) 100%);">
          <div class="d-flex align-center gap-lg flex-wrap">
            <div style="width: 72px; height: 72px; border-radius: var(--radius-full); background: linear-gradient(135deg, var(--accent) 0%, var(--primary-600) 100%); display:flex; align-items:center; justify-content:center; color:#fff; font-size:1.75rem; font-weight:800; box-shadow: var(--shadow-md); flex-shrink: 0;">
              ${escapeHtml(initials)}
            </div>
            <div style="flex: 1; min-width: 200px;">
              <div class="d-flex align-center gap-sm flex-wrap">
                <h2 style="font-size: 1.4rem; margin: 0; font-weight: 700; color: var(--text-main);">${escapeHtml(p.nombreCompleto || p.username)}</h2>
                <span class="badge badge-primary font-bold">${escapeHtml(p.rol || 'Sin Rol')}</span>
                <span class="badge ${estadoStr === 'Activo' ? 'badge-success' : 'badge-danger'}">
                  <span class="badge-dot"></span>${escapeHtml(estadoStr)}
                </span>
              </div>
              <div class="text-sm text-muted mt-1">
                <span>@${escapeHtml(p.username)}</span> &bull; 
                <span>ID Usuario: #${escapeHtml(p.usuarioId)}</span> &bull; 
                <span>ID Persona: #${escapeHtml(p.personaId)}</span>
              </div>
              <div class="text-xs text-muted mt-1">
                Último acceso: <strong>${escapeHtml(fechaAcceso)}</strong>
              </div>
            </div>
          </div>
        </div>

        <!-- 2. Tarjeta Datos Personales y de Contacto -->
        <div class="card">
          <div class="card-header">
            <h3 class="card-title d-flex align-center gap-xs">
              <svg class="icon-svg sm" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
                <circle cx="12" cy="7" r="4"></circle>
              </svg>
              <span>Información Personal y Contacto</span>
            </h3>
          </div>
          <div class="card-body" style="padding: 1.5rem;">
            <div class="grid-two-cols" style="display: grid; grid-template-columns: repeat(auto-fit, minmax(260px, 1fr)); gap: 1.25rem;">
              <div>
                <label class="text-xs text-muted font-bold text-uppercase">Nombre</label>
                <div class="font-medium text-main" style="font-size: 1rem;">${escapeHtml(p.nombre || '-')}</div>
              </div>
              <div>
                <label class="text-xs text-muted font-bold text-uppercase">Apellido</label>
                <div class="font-medium text-main" style="font-size: 1rem;">${escapeHtml(p.apellido || '-')}</div>
              </div>
              <div>
                <label class="text-xs text-muted font-bold text-uppercase">Documento de Identidad</label>
                <div class="font-medium text-main" style="font-size: 1rem;">${escapeHtml(p.documento || '-')}</div>
              </div>
              <div>
                <label class="text-xs text-muted font-bold text-uppercase">Correo Electrónico</label>
                <div class="font-medium text-main" style="font-size: 1rem;">
                  <a href="mailto:${escapeHtml(p.email)}" class="text-primary font-semibold">${escapeHtml(p.email || '-')}</a>
                </div>
              </div>
              <div>
                <label class="text-xs text-muted font-bold text-uppercase">Teléfono de Contacto</label>
                <div class="font-medium text-main" style="font-size: 1rem;">${escapeHtml(p.telefono || 'No registrado')}</div>
              </div>
              <div>
                <label class="text-xs text-muted font-bold text-uppercase">Rol Asignado</label>
                <div class="font-medium text-main" style="font-size: 1rem;">${escapeHtml(p.rol || '-')}</div>
              </div>
            </div>
          </div>
        </div>

        <!-- 3. Tarjeta Permisos Asignados Categorizados -->
        <div class="card">
          <div class="card-header d-flex justify-between align-center">
            <h3 class="card-title d-flex align-center gap-xs">
              <svg class="icon-svg sm" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
                <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
              </svg>
              <span>Permisos del Sistema Asignados</span>
            </h3>
            <span class="badge badge-accent font-bold">${(p.permisos || []).length} permisos activos</span>
          </div>
          <div class="card-body" style="padding: 1.5rem;">
            ${(p.permisos && p.permisos.length > 0) ? `
              <div class="d-flex flex-column gap-md">
                ${Object.entries(grupos).map(([modulo, perms]) => `
                  <div style="background: var(--surface-alt); padding: 1rem; border-radius: var(--radius-md); border: 1px solid var(--border);">
                    <div class="d-flex justify-between align-center mb-2">
                      <h4 style="font-size: 0.875rem; font-weight: 700; color: var(--primary); text-transform: uppercase; letter-spacing: 0.5px; margin: 0;">
                        ${escapeHtml(modulo)}
                      </h4>
                      <span class="text-xs text-muted font-bold">${perms.length}</span>
                    </div>
                    <div class="d-flex flex-wrap gap-xs">
                      ${perms.map(codigo => `
                        <span class="badge badge-secondary" style="font-family: monospace; font-size: 0.75rem;">
                          <span class="badge-dot" style="background-color: var(--primary);"></span>
                          ${escapeHtml(codigo)}
                        </span>
                      `).join('')}
                    </div>
                  </div>
                `).join('')}
              </div>
            ` : `
              <p class="text-muted text-center" style="padding: 1.5rem;">No cuenta con permisos asignados.</p>
            `}
          </div>
        </div>
      </div>
    `;
  }

  _agruparPermisos(permisos) {
    const mapa = {
      'Seguridad': [],
      'Personal': [],
      'Clientes': [],
      'Sucursales': [],
      'Stock y Bulbos': [],
      'Comercial': [],
      'Producción y Campo': [],
      'Reportes': [],
      'Mi Cuenta': [],
      'Otros': []
    };

    permisos.forEach(p => {
      const pUpper = String(p).toUpperCase();
      if (pUpper.startsWith('ROLES_') || pUpper.startsWith('PERMISOS_') || pUpper.startsWith('USUARIOS_')) mapa['Seguridad'].push(p);
      else if (pUpper.startsWith('EMPLEADOS_')) mapa['Personal'].push(p);
      else if (pUpper.startsWith('CLIENTES_')) mapa['Clientes'].push(p);
      else if (pUpper.startsWith('SUCURSALES_')) mapa['Sucursales'].push(p);
      else if (pUpper.startsWith('STOCK_') || pUpper.startsWith('MOVIMIENTOS_')) mapa['Stock y Bulbos'].push(p);
      else if (pUpper.startsWith('MOVCOM_') || pUpper.startsWith('PRECIOS_REF_')) mapa['Comercial'].push(p);
      else if (pUpper.startsWith('TEMPORADAS_') || pUpper.startsWith('PLANTACIONES_') || pUpper.startsWith('TRANSFERENCIAS_') || pUpper.startsWith('COSECHAS_')) mapa['Producción y Campo'].push(p);
      else if (pUpper.startsWith('REPORTES_')) mapa['Reportes'].push(p);
      else if (pUpper.startsWith('PERFIL_')) mapa['Mi Cuenta'].push(p);
      else mapa['Otros'].push(p);
    });

    // Eliminar grupos vacíos
    const resultado = {};
    for (const [key, val] of Object.entries(mapa)) {
      if (val.length > 0) resultado[key] = val;
    }
    return resultado;
  }
}
