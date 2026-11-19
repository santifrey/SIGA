/**
 * SIGA - views/roles-view.js
 * Vista de Gestión de Roles y Permisos (#roles) (Feature 31)
 * Administra roles, matriz de permisos agrupada por módulo, protección de roles del sistema,
 * conteo de usuarios y símbolo de prohibición SVG para acciones de eliminación.
 */

import { rolesService } from '../services/roles-service.js';
import { authService } from '../services/auth-service.js';
import { ConfirmDeleteModal } from '../components/confirm-delete-modal.js';
import { loadTemplate } from '../utils/template-loader.js';
import { toast } from '../utils/toast.js';
import { escapeHtml } from '../utils/escape-html.js';
import { getProhibitionIcon } from '../utils/icons.js';

export class RolesView {
  constructor(router) {
    this.router = router;
    this.container = null;
    this.roles = [];
    this.catalogoPermisos = [];
    this.selectedRol = null;
    this.deleteModal = null;
  }

  async render(rootElement) {
    this.container = rootElement;
    const canManage = authService.tienePermiso('ROLES_GESTIONAR');

    this.container.innerHTML = await loadTemplate('roles.html');

    if (!canManage) {
      this.container.querySelector('#btn-nuevo-rol')?.remove();
    }

    this.deleteModal = new ConfirmDeleteModal({
      container: this.container,
      title: 'Eliminar Rol',
      confirmText: 'Confirmar Eliminación',
      hintText: 'Los roles con usuarios activos asignados o configurados como rol de sistema no pueden eliminarse.',
      onConfirm: async (id) => {
        await rolesService.eliminar(id);
        toast.success('Rol eliminado exitosamente.');
        await this.loadRoles();
      }
    });
    await this.deleteModal.init();

    this._bindEvents();
    await this._loadInitialData();
  }

  _bindEvents() {
    this.container.querySelector('#btn-refresh-roles').addEventListener('click', () => this.loadRoles());

    const btnNuevo = this.container.querySelector('#btn-nuevo-rol');
    if (btnNuevo) {
      btnNuevo.addEventListener('click', () => this.openModalNuevo());
    }

    // Modal Form Rol
    this.container.querySelector('#btn-close-modal-rol').addEventListener('click', () => this.closeModalRol());
    this.container.querySelector('#btn-cancel-rol').addEventListener('click', () => this.closeModalRol());
    this.container.querySelector('#form-rol').addEventListener('submit', (e) => this._handleSubmitRol(e));

    // Seleccionar todos / Deseleccionar todos
    this.container.querySelector('#btn-select-all-permisos').addEventListener('click', () => {
      this.container.querySelectorAll('.permiso-checkbox').forEach(cb => { cb.checked = true; });
      this.container.querySelectorAll('.modulo-select-all').forEach(cb => { cb.checked = true; });
    });

    this.container.querySelector('#btn-deselect-all-permisos').addEventListener('click', () => {
      this.container.querySelectorAll('.permiso-checkbox').forEach(cb => { cb.checked = false; });
      this.container.querySelectorAll('.modulo-select-all').forEach(cb => { cb.checked = false; });
    });
  }

  async _loadInitialData() {
    try {
      this.catalogoPermisos = await rolesService.listarPermisos();
    } catch (err) {
      console.warn('[RolesView] No se pudo cargar el catálogo de permisos:', err);
    }
    await this.loadRoles();
  }

  async loadRoles() {
    const tbody = this.container.querySelector('#tbody-roles');
    tbody.innerHTML = `
      <tr>
        <td colspan="6" class="text-center text-muted" style="padding: 2.5rem;">
          <div class="btn-spinner" style="display:inline-block; width:24px; height:24px; border-width:2px;"></div>
          <p class="mt-2">Cargando roles del sistema...</p>
        </td>
      </tr>
    `;

    try {
      this.roles = await rolesService.listar();
      this._renderTable();
    } catch (err) {
      tbody.innerHTML = `
        <tr>
          <td colspan="6" class="text-center text-muted" style="padding: 2.5rem; color: var(--danger);">
            ${escapeHtml(err.message || 'Error al cargar roles.')}
          </td>
        </tr>
      `;
      toast.error('No se pudo cargar la lista de roles.');
    }
  }

  _renderTable() {
    const tbody = this.container.querySelector('#tbody-roles');
    const canManage = authService.tienePermiso('ROLES_GESTIONAR');

    if (!this.roles || this.roles.length === 0) {
      tbody.innerHTML = `
        <tr>
          <td colspan="6" class="text-center text-muted" style="padding: 2.5rem;">
            No se encontraron roles registrados.
          </td>
        </tr>
      `;
      return;
    }

    tbody.innerHTML = this.roles.map(rol => {
      const isSistema = rol.esSistema === true;
      const totalUsers = typeof rol.totalUsuarios === 'number' ? rol.totalUsuarios : 0;
      const permisosCount = Array.isArray(rol.permisos) ? rol.permisos.length : 0;

      return `
        <tr>
          <td>
            <div class="font-bold text-main" style="font-family: monospace; font-size: 0.95rem;">
              ${escapeHtml(rol.nombre)}
            </div>
          </td>
          <td>
            <div class="text-sm text-muted">${escapeHtml(rol.descripcion || 'Sin descripción')}</div>
          </td>
          <td>
            ${isSistema ? `
              <span class="badge badge-accent font-bold" title="Rol protegido del sistema">
                <span class="badge-dot" style="background-color: var(--accent);"></span>Sistema
              </span>
            ` : `
              <span class="badge badge-secondary">Personalizado</span>
            `}
          </td>
          <td>
            <span class="badge badge-primary font-bold">
              ${totalUsers} ${totalUsers === 1 ? 'usuario' : 'usuarios'}
            </span>
          </td>
          <td>
            <span class="badge badge-secondary" title="${permisosCount} permisos configurados">
              ${permisosCount} permisos
            </span>
          </td>
          <td>
            <div class="table-actions justify-end">
              ${canManage ? `
                <button class="btn-icon btn-action-edit" data-id="${escapeHtml(rol.id)}" title="Editar permisos y detalles">
                  <svg class="icon-svg sm" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path>
                    <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path>
                  </svg>
                </button>
                ${!isSistema ? `
                  <button class="btn-icon btn-action-delete" data-id="${escapeHtml(rol.id)}" data-name="${escapeHtml(rol.nombre)}" title="Eliminar rol personalizado">
                    ${getProhibitionIcon({ size: 'sm', title: 'Eliminar rol personalizado' })}
                  </button>
                ` : `
                  <span class="badge badge-secondary" title="Los roles de sistema están protegidos contra eliminación" style="cursor:not-allowed; opacity:0.6;">Protegido</span>
                `}
              ` : '<span class="text-xs text-muted">Solo lectura</span>'}
            </div>
          </td>
        </tr>
      `;
    }).join('');

    tbody.querySelectorAll('.btn-action-edit').forEach(btn => {
      btn.addEventListener('click', () => {
        const id = btn.getAttribute('data-id');
        this.openModalEditar(id);
      });
    });

    tbody.querySelectorAll('.btn-action-delete').forEach(btn => {
      btn.addEventListener('click', () => {
        const id = btn.getAttribute('data-id');
        const name = btn.getAttribute('data-name');
        this.openModalDelete(id, name);
      });
    });
  }

  _renderMatrizPermisos(permisosSeleccionados = []) {
    const contenedor = this.container.querySelector('#contenedor-matriz-permisos');
    const selectedSet = new Set((permisosSeleccionados || []).map(p => String(p).trim().toUpperCase()));

    // Agrupar catálogo maestro por módulo
    const modulos = {};
    this.catalogoPermisos.forEach(p => {
      const mod = p.modulo || 'General';
      if (!modulos[mod]) modulos[mod] = [];
      modulos[mod].push(p);
    });

    let html = '';
    for (const [modulo, perms] of Object.entries(modulos)) {
      const moduloId = modulo.replace(/\s+/g, '-').toLowerCase();
      const allChecked = perms.every(p => selectedSet.has(p.codigo.toUpperCase()));

      html += `
        <div class="card" style="padding: 0.85rem; background: var(--surface-alt); border: 1px solid var(--border);">
          <div class="d-flex justify-between align-center mb-2 pb-1 border-bottom">
            <label class="d-flex align-center gap-xs" style="cursor: pointer; margin: 0;">
              <input type="checkbox" class="modulo-select-all" data-modulo="${escapeHtml(moduloId)}" ${allChecked ? 'checked' : ''} />
              <strong style="font-size: 0.85rem; color: var(--primary); text-transform: uppercase; letter-spacing: 0.5px;">${escapeHtml(modulo)}</strong>
            </label>
            <span class="text-xs text-muted font-bold">${perms.length} permisos</span>
          </div>
          <div class="grid-two-cols" style="display: grid; grid-template-columns: repeat(auto-fill, minmax(240px, 1fr)); gap: 0.5rem;">
            ${perms.map(p => {
              const checked = selectedSet.has(p.codigo.toUpperCase());
              return `
                <label class="d-flex align-start gap-xs text-sm" style="cursor: pointer; padding: 4px; border-radius: var(--radius-sm); transition: background 0.15s;" title="${escapeHtml(p.descripcion || p.nombre)}">
                  <input type="checkbox" class="permiso-checkbox modulo-${escapeHtml(moduloId)}" value="${escapeHtml(p.codigo)}" ${checked ? 'checked' : ''} style="margin-top: 3px;" />
                  <div>
                    <span class="font-medium" style="font-family: monospace; font-size: 0.8rem;">${escapeHtml(p.codigo)}</span>
                    <div class="text-xs text-muted" style="line-height: 1.2;">${escapeHtml(p.nombre)}</div>
                  </div>
                </label>
              `;
            }).join('')}
          </div>
        </div>
      `;
    }

    contenedor.innerHTML = html;

    // Conectar select-all por módulo
    contenedor.querySelectorAll('.modulo-select-all').forEach(headerCb => {
      headerCb.addEventListener('change', () => {
        const moduloId = headerCb.getAttribute('data-modulo');
        const checkboxes = contenedor.querySelectorAll(`.modulo-${moduloId}`);
        checkboxes.forEach(cb => { cb.checked = headerCb.checked; });
      });
    });

    // Sincronizar header checkbox si se desmarca uno
    contenedor.querySelectorAll('.permiso-checkbox').forEach(cb => {
      cb.addEventListener('change', () => {
        const classNames = Array.from(cb.classList);
        const moduloClass = classNames.find(c => c.startsWith('modulo-'));
        if (moduloClass) {
          const moduloId = moduloClass.replace('modulo-', '');
          const headerCb = contenedor.querySelector(`.modulo-select-all[data-modulo="${moduloId}"]`);
          if (headerCb) {
            const allInGroup = contenedor.querySelectorAll(`.${moduloClass}`);
            const allChecked = Array.from(allInGroup).every(c => c.checked);
            headerCb.checked = allChecked;
          }
        }
      });
    });
  }

  openModalNuevo() {
    this.selectedRol = null;
    const form = this.container.querySelector('#form-rol');
    form.reset();
    this.container.querySelector('#rol-id').value = '';
    this.container.querySelector('#modal-rol-title').textContent = 'Nuevo Rol Personalizado';

    const nombreInput = this.container.querySelector('#rol-nombre');
    nombreInput.disabled = false;
    this.container.querySelector('#help-rol-nombre').textContent = 'En mayúsculas sin espacios recomendados.';

    const errBox = this.container.querySelector('#form-rol-error');
    errBox.classList.add('hidden');
    errBox.textContent = '';

    this._renderMatrizPermisos([]);

    const modal = this.container.querySelector('#modal-rol');
    modal.classList.add('is-active');
  }

  async openModalEditar(id) {
    const errBox = this.container.querySelector('#form-rol-error');
    errBox.classList.add('hidden');
    errBox.textContent = '';

    try {
      const rol = await rolesService.obtenerPorId(id);
      this.selectedRol = rol;

      this.container.querySelector('#rol-id').value = rol.id;
      const nombreInput = this.container.querySelector('#rol-nombre');
      nombreInput.value = rol.nombre || '';
      this.container.querySelector('#rol-descripcion').value = rol.descripcion || '';

      // Si es rol de sistema, el backend no permite renombrar
      if (rol.esSistema) {
        nombreInput.disabled = true;
        this.container.querySelector('#help-rol-nombre').textContent = 'Rol protegido del sistema: el nombre no puede modificarse.';
      } else {
        nombreInput.disabled = false;
        this.container.querySelector('#help-rol-nombre').textContent = 'En mayúsculas sin espacios recomendados.';
      }

      this.container.querySelector('#modal-rol-title').textContent = `Editar Rol: ${rol.nombre}`;

      this._renderMatrizPermisos(rol.permisos || []);

      const modal = this.container.querySelector('#modal-rol');
      modal.classList.add('is-active');
    } catch (err) {
      toast.error('No se pudo cargar la información del rol.');
    }
  }

  closeModalRol() {
    const modal = this.container.querySelector('#modal-rol');
    modal.classList.remove('is-active');
  }

  async _handleSubmitRol(e) {
    e.preventDefault();
    const saveBtn = this.container.querySelector('#btn-save-rol');
    const errBox = this.container.querySelector('#form-rol-error');
    errBox.classList.add('hidden');
    errBox.textContent = '';

    const id = this.container.querySelector('#rol-id').value;
    const nombre = this.container.querySelector('#rol-nombre').value.trim().toUpperCase();
    const descripcion = this.container.querySelector('#rol-descripcion').value.trim();

    // Obtener permisos seleccionados
    const checkedPermisos = Array.from(this.container.querySelectorAll('.permiso-checkbox:checked'))
      .map(cb => cb.value);

    if (!nombre) {
      errBox.textContent = 'El nombre del rol es obligatorio.';
      errBox.classList.remove('hidden');
      return;
    }

    // Salvaguarda UI: Prevenir renombrar roles del sistema
    if (id && this.selectedRol && this.selectedRol.esSistema) {
      if (this.selectedRol.nombre.toUpperCase() !== nombre) {
        errBox.textContent = 'No se permite modificar el nombre de un rol de sistema.';
        errBox.classList.remove('hidden');
        return;
      }
    }

    const payload = {
      nombre,
      descripcion: descripcion || null,
      permisos: checkedPermisos
    };

    saveBtn.disabled = true;
    saveBtn.classList.add('is-loading');

    try {
      if (id) {
        await rolesService.actualizar(id, payload);
        toast.success(`Rol ${nombre} actualizado correctamente.`);
      } else {
        await rolesService.crear(payload);
        toast.success(`Rol ${nombre} creado exitosamente.`);
      }

      this.closeModalRol();
      await this.loadRoles();
    } catch (err) {
      const msg = err.message || 'Error al guardar el rol.';
      errBox.textContent = msg;
      errBox.classList.remove('hidden');
      toast.error(msg);
    } finally {
      saveBtn.disabled = false;
      saveBtn.classList.remove('is-loading');
    }
  }

  openModalDelete(id, name) {
    let blockingError = null;
    const rol = this.roles.find(r => String(r.id) === String(id));
    if (rol && rol.esSistema) {
      blockingError = 'No se puede eliminar un rol protegido del sistema.';
    } else if (rol && rol.totalUsuarios > 0) {
      blockingError = 'No se puede eliminar el rol porque tiene usuarios asignados. Reasigne los usuarios antes de eliminar el rol.';
    }

    this.deleteModal.open({
      id,
      promptHtml: `¿Está seguro de que desea eliminar el rol <strong>${escapeHtml(name)}</strong>?`,
      blockingError
    });
  }
}
