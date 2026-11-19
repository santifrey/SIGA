/**
 * SIGA - views/perfil-view.js
 * Vista de Perfil de Usuario (#perfil)
 * Muestra información personal, rol, estado y permite modificar datos personales y de contacto.
 */

import { authService } from '../services/auth-service.js';
import { loadTemplate } from '../utils/template-loader.js';
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
    this.container.innerHTML = await loadTemplate('perfil.html');

    this.container.querySelector('#btn-refresh-perfil').addEventListener('click', () => this._loadPerfil());
    this._setupModalEvents();
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
      : 'US';

    const fechaAcceso = p.ultimoAcceso
      ? new Date(p.ultimoAcceso).toLocaleString('es-AR', { dateStyle: 'medium', timeStyle: 'short' })
      : 'Primer acceso registrado';

    const estadoStr = typeof p.estado === 'string'
      ? p.estado
      : (p.estado === 1 ? 'Activo' : (p.estado === 2 ? 'Bloqueado' : 'Inactivo'));

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
                <h2 style="font-size: 1.4rem; margin: 0; font-weight: 700; color: var(--text-main);">${escapeHtml(p.nombreCompleto || `${p.nombre || ''} ${p.apellido || ''}`.trim() || 'Usuario')}</h2>
                <span class="badge badge-primary font-bold">${escapeHtml(p.rol || 'Sin Rol')}</span>
                <span class="badge ${estadoStr === 'Activo' ? 'badge-success' : 'badge-danger'}">
                  <span class="badge-dot"></span>${escapeHtml(estadoStr)}
                </span>
              </div>
              <div class="text-xs text-muted mt-2">
                Último acceso: <strong>${escapeHtml(fechaAcceso)}</strong>
              </div>
            </div>
          </div>
        </div>

        <!-- 2. Tarjeta Datos Personales y de Contacto -->
        <div class="card">
          <div class="card-header d-flex justify-between align-center">
            <h3 class="card-title d-flex align-center gap-xs">
              <svg class="icon-svg sm" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
                <circle cx="12" cy="7" r="4"></circle>
              </svg>
              <span>Información Personal y Contacto</span>
            </h3>
            <button class="btn btn-sm btn-primary d-flex align-center gap-xs" id="btn-abrir-editar-perfil">
              <svg class="icon-svg sm" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path>
                <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path>
              </svg>
              <span>Modificar Datos</span>
            </button>
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
                <label class="text-xs text-muted font-bold text-uppercase">Documento de Identidad (DNI)</label>
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
              ${p.legajo ? `
                <div>
                  <label class="text-xs text-muted font-bold text-uppercase">Legajo</label>
                  <div class="font-medium text-main" style="font-size: 1rem;"><span class="badge badge-secondary font-mono">${escapeHtml(p.legajo)}</span></div>
                </div>
              ` : ''}
              ${p.posicion ? `
                <div>
                  <label class="text-xs text-muted font-bold text-uppercase">Posición / Cargo</label>
                  <div class="font-medium text-main" style="font-size: 1rem;">${escapeHtml(p.posicion)}</div>
                </div>
              ` : ''}
            </div>
          </div>
        </div>
      </div>
    `;

    const btnEditar = contentEl.querySelector('#btn-abrir-editar-perfil');
    if (btnEditar) {
      btnEditar.addEventListener('click', () => this._abrirModalEdicion());
    }
  }

  _setupModalEvents() {
    const modal = this.container.querySelector('#modal-editar-perfil');
    const btnClose = this.container.querySelector('#btn-close-modal-perfil');
    const btnCancel = this.container.querySelector('#btn-cancelar-perfil');
    const form = this.container.querySelector('#form-editar-perfil');

    const cerrarModal = () => {
      modal.classList.remove('is-active');
    };

    if (btnClose) btnClose.addEventListener('click', cerrarModal);
    if (btnCancel) btnCancel.addEventListener('click', cerrarModal);

    if (form) {
      form.addEventListener('submit', async (e) => {
        e.preventDefault();
        const errBox = this.container.querySelector('#perfil-form-error');
        const submitBtn = this.container.querySelector('#btn-guardar-perfil');
        errBox.classList.add('hidden');
        errBox.textContent = '';

        const nombre = this.container.querySelector('#perfil-nombre').value.trim();
        const apellido = this.container.querySelector('#perfil-apellido').value.trim();
        const documento = this.container.querySelector('#perfil-documento').value.trim();
        const email = this.container.querySelector('#perfil-email').value.trim();
        const telefono = this.container.querySelector('#perfil-telefono').value.trim();

        if (!nombre || !apellido || !documento || !email) {
          errBox.textContent = 'Por favor complete todos los campos obligatorios (Nombre, Apellido, DNI y Correo Electrónico).';
          errBox.classList.remove('hidden');
          return;
        }

        submitBtn.disabled = true;
        submitBtn.classList.add('is-loading');

        try {
          await authService.actualizarPerfil({
            nombre,
            apellido,
            documento,
            email,
            telefono: telefono || null
          });

          toast.success('Información personal y de contacto actualizada exitosamente.');
          cerrarModal();

          // Actualizar topbar nombre y avatar si cambiaron
          const topbarName = document.getElementById('topbar-user-name');
          const topbarAvatar = document.getElementById('topbar-user-avatar');
          const nombreCompleto = `${nombre} ${apellido}`.trim();
          if (topbarName) topbarName.textContent = nombreCompleto;
          if (topbarAvatar) {
            topbarAvatar.textContent = nombreCompleto.split(' ').map(n => n[0]).slice(0, 2).join('').toUpperCase();
          }

          await this._loadPerfil();
        } catch (err) {
          const msg = err.message || 'Error al actualizar información de perfil.';
          errBox.textContent = msg;
          errBox.classList.remove('hidden');
          toast.error(msg);
        } finally {
          submitBtn.disabled = false;
          submitBtn.classList.remove('is-loading');
        }
      });
    }
  }

  _abrirModalEdicion() {
    const p = this.perfil || {};
    const modal = this.container.querySelector('#modal-editar-perfil');
    const errBox = this.container.querySelector('#perfil-form-error');
    errBox.classList.add('hidden');
    errBox.textContent = '';

    this.container.querySelector('#perfil-nombre').value = p.nombre || '';
    this.container.querySelector('#perfil-apellido').value = p.apellido || '';
    this.container.querySelector('#perfil-documento').value = p.documento || '';
    this.container.querySelector('#perfil-email').value = p.email || '';
    this.container.querySelector('#perfil-telefono').value = p.telefono || '';

    modal.classList.add('is-active');
  }
}
