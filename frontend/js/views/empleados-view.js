/**
 * SIGA - views/empleados-view.js
 * Vista de Gestión de Empleados (#empleados) (Feature 30)
 * Administra nómina interna (Persona + Usuario no-CLIENTE), selector de roles,
 * baja lógica con símbolo de prohibición SVG y salvaguarda para el último administrador.
 */

import { empleadosService } from '../services/empleados-service.js';
import { rolesService } from '../services/roles-service.js';
import { authService } from '../services/auth-service.js';
import { toast } from '../utils/toast.js';
import { escapeHtml } from '../utils/escape-html.js';
import { getProhibitionIcon } from '../utils/icons.js';

export class EmpleadosView {
  constructor(router) {
    this.router = router;
    this.container = null;
    this.empleados = [];
    this.roles = [];
    this.selectedEmpleado = null;
    this.selectedDeleteId = null;

    // Filtros y paginación
    this.filterSearch = '';
    this.filterRol = '';
    this.filterEstado = 'TODOS';
    this.currentPage = 1;
    this.pageSize = 10;
  }

  async render(rootElement) {
    this.container = rootElement;
    const canManage = authService.tienePermiso('EMPLEADOS_GESTIONAR');

    this.container.innerHTML = `
      <div class="fade-in">
        <!-- Encabezado de Sección -->
        <div class="d-flex justify-between align-center mb-4 flex-wrap gap-md">
          <div>
            <h1 style="font-size: 1.5rem; margin-bottom: 0.25rem;">Gestión de Empleados</h1>
            <p class="text-muted text-sm">Administración de usuarios internos, personal operativo y asignación de roles</p>
          </div>
          <div class="d-flex gap-sm">
            <button class="btn btn-secondary" id="btn-refresh-empleados" title="Recargar lista">
              <svg class="icon-svg sm" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <polyline points="23 4 23 10 17 10"></polyline>
                <polyline points="1 20 1 14 7 14"></polyline>
                <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"></path>
              </svg>
              <span>Refrescar</span>
            </button>
            ${canManage ? `
              <button class="btn btn-primary" id="btn-nuevo-empleado">
                <svg class="icon-svg sm" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <line x1="12" y1="5" x2="12" y2="19"></line>
                  <line x1="5" y1="12" x2="19" y2="12"></line>
                </svg>
                <span>Nuevo Empleado</span>
              </button>
            ` : ''}
          </div>
        </div>

        <!-- Barra de Filtros -->
        <div class="card mb-4" style="padding: 1rem 1.25rem;">
          <div class="d-flex gap-md flex-wrap align-center">
            <div style="flex: 2; min-width: 220px;">
              <input type="text" id="filtro-buscar" class="form-control" placeholder="Buscar por nombre, usuario, documento o email..." />
            </div>
            <div style="flex: 1; min-width: 160px;">
              <select id="filtro-rol" class="form-control">
                <option value="">Todos los Roles</option>
              </select>
            </div>
            <div style="flex: 1; min-width: 140px;">
              <select id="filtro-estado" class="form-control">
                <option value="TODOS">Todos los Estados</option>
                <option value="ACTIVO">Solo Activos</option>
                <option value="INACTIVO">Solo Inactivos</option>
              </select>
            </div>
          </div>
        </div>

        <!-- Tabla de Empleados -->
        <div class="card">
          <div class="table-responsive">
            <table class="table" id="tabla-empleados">
              <thead>
                <tr>
                  <th>Empleado</th>
                  <th>Documento</th>
                  <th>Contacto</th>
                  <th>Rol</th>
                  <th>Estado</th>
                  <th>Último Acceso</th>
                  <th style="text-align: right;">Acciones</th>
                </tr>
              </thead>
              <tbody id="tbody-empleados">
                <tr>
                  <td colspan="7" class="text-center text-muted" style="padding: 2.5rem;">
                    <div class="btn-spinner" style="display:inline-block; width:24px; height:24px; border-width:2px;"></div>
                    <p class="mt-2">Cargando nómina de empleados...</p>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- Paginación y Contador -->
          <div class="d-flex justify-between align-center p-3 border-top flex-wrap gap-sm">
            <div class="text-sm text-muted" id="empleados-pagination-info">Mostrando 0 empleados</div>
            <div class="d-flex gap-xs" id="empleados-pagination-controls"></div>
          </div>
        </div>

        <!-- Modal Crear / Editar Empleado -->
        <div class="modal-backdrop" id="modal-empleado">
          <div class="modal" style="max-width: 650px;">
            <div class="modal-header">
              <h3 class="modal-title" id="modal-empleado-title">Nuevo Empleado</h3>
              <button type="button" class="btn-close" id="btn-close-modal-empleado">&times;</button>
            </div>
            <form id="form-empleado">
              <div class="modal-body" style="max-height: 70vh; overflow-y: auto;">
                <input type="hidden" id="emp-id" value="" />
                
                <h4 class="text-xs text-muted font-bold text-uppercase mb-2">Datos Personales</h4>
                <div class="grid-two-cols mb-3" style="display:grid; grid-template-columns: 1fr 1fr; gap: 0.75rem;">
                  <div class="form-group">
                    <label class="form-label" for="emp-nombre">Nombre <span class="text-danger">*</span></label>
                    <input type="text" id="emp-nombre" class="form-control" required />
                  </div>
                  <div class="form-group">
                    <label class="form-label" for="emp-apellido">Apellido <span class="text-danger">*</span></label>
                    <input type="text" id="emp-apellido" class="form-control" required />
                  </div>
                </div>

                <div class="grid-two-cols mb-3" style="display:grid; grid-template-columns: 1fr 1fr; gap: 0.75rem;">
                  <div class="form-group">
                    <label class="form-label" for="emp-documento">Documento (DNI/CUIT) <span class="text-danger">*</span></label>
                    <input type="text" id="emp-documento" class="form-control" required />
                  </div>
                  <div class="form-group">
                    <label class="form-label" for="emp-telefono">Teléfono</label>
                    <input type="tel" id="emp-telefono" class="form-control" />
                  </div>
                </div>

                <div class="form-group mb-3">
                  <label class="form-label" for="emp-email">Correo Electrónico <span class="text-danger">*</span></label>
                  <input type="email" id="emp-email" class="form-control" required />
                </div>

                <hr style="border: 0; border-top: 1px solid var(--border); margin: 1rem 0;" />
                <h4 class="text-xs text-muted font-bold text-uppercase mb-2">Credenciales y Acceso al Sistema</h4>

                <div class="grid-two-cols mb-3" style="display:grid; grid-template-columns: 1fr 1fr; gap: 0.75rem;">
                  <div class="form-group">
                    <label class="form-label" for="emp-username">Nombre de Usuario <span class="text-danger">*</span></label>
                    <input type="text" id="emp-username" class="form-control" required />
                  </div>
                  <div class="form-group">
                    <label class="form-label" for="emp-password" id="lbl-emp-password">Contraseña <span class="text-danger">*</span></label>
                    <input type="password" id="emp-password" class="form-control" />
                    <span class="text-xs text-muted" id="help-emp-password">Dejar en blanco para no modificar si edita.</span>
                  </div>
                </div>

                <div class="grid-two-cols mb-3" style="display:grid; grid-template-columns: 1fr 1fr; gap: 0.75rem;">
                  <div class="form-group">
                    <label class="form-label" for="emp-rol">Rol del Empleado <span class="text-danger">*</span></label>
                    <select id="emp-rol" class="form-control" required>
                      <option value="">Seleccione un Rol...</option>
                    </select>
                  </div>
                  <div class="form-group" style="display:flex; align-items:center; margin-top: 1.5rem;">
                    <label class="d-flex align-center gap-xs" style="cursor: pointer;">
                      <input type="checkbox" id="emp-activo" checked />
                      <span class="font-medium text-sm">Usuario Activo</span>
                    </label>
                  </div>
                </div>

                <div id="form-empleado-error" class="alert alert-danger hidden mt-2"></div>
              </div>
              <div class="modal-footer">
                <button type="button" class="btn btn-secondary" id="btn-cancel-empleado">Cancelar</button>
                <button type="submit" class="btn btn-primary" id="btn-save-empleado">Guardar Empleado</button>
              </div>
            </form>
          </div>
        </div>

        <!-- Modal Confirmación de Desactivación (Prohibition) -->
        <div class="modal-backdrop" id="modal-delete-empleado">
          <div class="modal" style="max-width: 480px;">
            <div class="modal-header">
              <h3 class="modal-title" style="color: var(--danger);">Desactivar Empleado</h3>
              <button type="button" class="btn-close" id="btn-close-modal-delete">&times;</button>
            </div>
            <div class="modal-body">
              <div class="d-flex gap-md align-start">
                <div style="flex-shrink: 0; margin-top: 2px;">
                  ${getProhibitionIcon({ size: 'lg', title: 'Desactivar Empleado' })}
                </div>
                <div>
                  <p id="delete-empleado-prompt" style="font-size: 0.95rem; line-height: 1.4; margin: 0;"></p>
                  <p class="text-muted text-xs mt-2">El usuario perderá acceso inmediato a la plataforma pero se conservará su historial y trazabilidad.</p>
                </div>
              </div>
              <div id="delete-empleado-error" class="alert alert-danger hidden mt-3"></div>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" id="btn-cancel-delete">Cancelar</button>
              <button type="button" class="btn btn-danger" id="btn-confirm-delete">Confirmar Baja</button>
            </div>
          </div>
        </div>
      </div>
    `;

    this._bindEvents();
    await this._loadInitialData();
  }

  _bindEvents() {
    this.container.querySelector('#btn-refresh-empleados').addEventListener('click', () => this.loadEmpleados());

    const btnNuevo = this.container.querySelector('#btn-nuevo-empleado');
    if (btnNuevo) {
      btnNuevo.addEventListener('click', () => this.openModalNuevo());
    }

    // Filtros
    this.container.querySelector('#filtro-buscar').addEventListener('input', (e) => {
      this.filterSearch = e.target.value.toLowerCase().trim();
      this.currentPage = 1;
      this._renderTable();
    });

    this.container.querySelector('#filtro-rol').addEventListener('change', (e) => {
      this.filterRol = e.target.value;
      this.currentPage = 1;
      this._renderTable();
    });

    this.container.querySelector('#filtro-estado').addEventListener('change', (e) => {
      this.filterEstado = e.target.value;
      this.currentPage = 1;
      this._renderTable();
    });

    // Formulario Empleado
    this.container.querySelector('#btn-close-modal-empleado').addEventListener('click', () => this.closeModalEmpleado());
    this.container.querySelector('#btn-cancel-empleado').addEventListener('click', () => this.closeModalEmpleado());
    this.container.querySelector('#form-empleado').addEventListener('submit', (e) => this._handleSubmitEmpleado(e));

    // Modal Delete / Desactivar
    this.container.querySelector('#btn-close-modal-delete').addEventListener('click', () => this.closeModalDelete());
    this.container.querySelector('#btn-cancel-delete').addEventListener('click', () => this.closeModalDelete());
    this.container.querySelector('#btn-confirm-delete').addEventListener('click', () => this._handleConfirmDelete());
  }

  async _loadInitialData() {
    try {
      this.roles = await rolesService.listar();
      this._populateRolesDropdowns();
    } catch (err) {
      console.warn('[EmpleadosView] No se pudieron cargar los roles:', err);
    }
    await this.loadEmpleados();
  }

  _populateRolesDropdowns() {
    const filterSelect = this.container.querySelector('#filtro-rol');
    const formSelect = this.container.querySelector('#emp-rol');

    filterSelect.innerHTML = '<option value="">Todos los Roles</option>';
    formSelect.innerHTML = '<option value="">Seleccione un Rol...</option>';

    // Excluir rol CLIENTE del personal interno si existe
    const staffRoles = this.roles.filter(r => r.nombre !== 'CLIENTE');

    staffRoles.forEach(rol => {
      const optFilter = document.createElement('option');
      optFilter.value = rol.nombre;
      optFilter.textContent = rol.nombre;
      filterSelect.appendChild(optFilter);

      const optForm = document.createElement('option');
      optForm.value = rol.id;
      optForm.textContent = rol.nombre;
      formSelect.appendChild(optForm);
    });
  }

  async loadEmpleados() {
    const tbody = this.container.querySelector('#tbody-empleados');
    tbody.innerHTML = `
      <tr>
        <td colspan="7" class="text-center text-muted" style="padding: 2.5rem;">
          <div class="btn-spinner" style="display:inline-block; width:24px; height:24px; border-width:2px;"></div>
          <p class="mt-2">Cargando nómina de empleados...</p>
        </td>
      </tr>
    `;

    try {
      this.empleados = await empleadosService.listar();
      this._renderTable();
    } catch (err) {
      tbody.innerHTML = `
        <tr>
          <td colspan="7" class="text-center" style="padding: 2.5rem; color: var(--danger);">
            ${escapeHtml(err.message || 'Error al cargar empleados.')}
          </td>
        </tr>
      `;
      toast.error('No se pudo cargar la nómina de empleados.');
    }
  }

  _renderTable() {
    const tbody = this.container.querySelector('#tbody-empleados');
    const infoEl = this.container.querySelector('#empleados-pagination-info');
    const controlsEl = this.container.querySelector('#empleados-pagination-controls');
    const canManage = authService.tienePermiso('EMPLEADOS_GESTIONAR');

    // Filtrar
    let filtered = this.empleados.filter(emp => {
      const fullName = (emp.nombreCompleto || `${emp.nombre || ''} ${emp.apellido || ''}`).toLowerCase();
      const username = (emp.username || '').toLowerCase();
      const doc = (emp.documento || '').toLowerCase();
      const email = (emp.email || '').toLowerCase();

      const matchesSearch = !this.filterSearch ||
        fullName.includes(this.filterSearch) ||
        username.includes(this.filterSearch) ||
        doc.includes(this.filterSearch) ||
        email.includes(this.filterSearch);

      const matchesRol = !this.filterRol || emp.rol === this.filterRol;

      const isActivo = emp.estado === 'Activo' || emp.estado === 1 || emp.activo === true;
      let matchesEstado = true;
      if (this.filterEstado === 'ACTIVO') matchesEstado = isActivo;
      else if (this.filterEstado === 'INACTIVO') matchesEstado = !isActivo;

      return matchesSearch && matchesRol && matchesEstado;
    });

    const total = filtered.length;
    const totalPages = Math.ceil(total / this.pageSize) || 1;
    if (this.currentPage > totalPages) this.currentPage = totalPages;

    const startIdx = (this.currentPage - 1) * this.pageSize;
    const paginated = filtered.slice(startIdx, startIdx + this.pageSize);

    if (paginated.length === 0) {
      tbody.innerHTML = `
        <tr>
          <td colspan="7" class="text-center text-muted" style="padding: 2.5rem;">
            No se encontraron empleados registrados que coincidan con los filtros.
          </td>
        </tr>
      `;
      infoEl.textContent = 'Mostrando 0 empleados';
      controlsEl.innerHTML = '';
      return;
    }

    tbody.innerHTML = paginated.map(emp => {
      const isActivo = emp.estado === 'Activo' || emp.estado === 1 || emp.activo === true;
      const initials = (emp.nombreCompleto || emp.username || 'EM')
        .split(' ').map(n => n[0]).slice(0, 2).join('').toUpperCase();

      const fechaAcceso = emp.ultimoAcceso
        ? new Date(emp.ultimoAcceso).toLocaleString('es-AR', { dateStyle: 'short', timeStyle: 'short' })
        : 'Sin ingresos';

      return `
        <tr>
          <td>
            <div class="d-flex align-center gap-sm">
              <div style="width: 32px; height: 32px; border-radius: var(--radius-full); background: var(--surface-alt); border: 1px solid var(--border); display:flex; align-items:center; justify-content:center; font-weight:700; font-size:0.75rem; color: var(--primary); flex-shrink: 0;">
                ${escapeHtml(initials)}
              </div>
              <div>
                <div class="font-bold text-main">${escapeHtml(emp.nombreCompleto || `${emp.nombre || ''} ${emp.apellido || ''}`.trim() || emp.username)}</div>
                <div class="text-xs text-muted">@${escapeHtml(emp.username)}</div>
              </div>
            </div>
          </td>
          <td>
            <div class="font-medium">${escapeHtml(emp.documento || '-')}</div>
          </td>
          <td>
            <div class="text-sm">${escapeHtml(emp.email || '-')}</div>
            <div class="text-xs text-muted">${escapeHtml(emp.telefono || 'Sin teléfono')}</div>
          </td>
          <td>
            <span class="badge badge-primary font-bold">${escapeHtml(emp.rol || 'Sin Rol')}</span>
          </td>
          <td>
            <span class="badge ${isActivo ? 'badge-success' : 'badge-danger'}">
              <span class="badge-dot"></span>${isActivo ? 'Activo' : 'Inactivo'}
            </span>
          </td>
          <td>
            <div class="text-xs text-muted">${escapeHtml(fechaAcceso)}</div>
          </td>
          <td>
            <div class="table-actions justify-end">
              ${canManage ? `
                <button class="btn-icon btn-action-edit" data-id="${escapeHtml(emp.id)}" title="Editar empleado">
                  <svg class="icon-svg sm" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path>
                    <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path>
                  </svg>
                </button>
                ${isActivo ? `
                  <button class="btn-icon btn-action-delete" data-id="${escapeHtml(emp.id)}" data-name="${escapeHtml(emp.nombreCompleto || emp.username)}" title="Dar de baja / Desactivar">
                    ${getProhibitionIcon({ size: 'sm', title: 'Dar de baja / Desactivar' })}
                  </button>
                ` : ''}
              ` : '<span class="text-xs text-muted">Solo lectura</span>'}
            </div>
          </td>
        </tr>
      `;
    }).join('');

    // Conectar botones de edición y borrado
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

    // Paginación
    const startItem = total === 0 ? 0 : (this.currentPage - 1) * this.pageSize + 1;
    const endItem = Math.min(this.currentPage * this.pageSize, total);
    infoEl.textContent = `Mostrando ${startItem} a ${endItem} de ${total} empleados`;

    this._renderPaginationControls(controlsEl, totalPages);
  }

  _renderPaginationControls(container, totalPages) {
    if (totalPages <= 1) {
      container.innerHTML = '';
      return;
    }

    let html = '';
    for (let p = 1; p <= totalPages; p++) {
      html += `
        <button class="btn btn-sm ${p === this.currentPage ? 'btn-primary' : 'btn-outline'}" data-page="${p}">
          ${p}
        </button>
      `;
    }
    container.innerHTML = html;

    container.querySelectorAll('button').forEach(btn => {
      btn.addEventListener('click', () => {
        this.currentPage = parseInt(btn.getAttribute('data-page'), 10);
        this._renderTable();
      });
    });
  }

  openModalNuevo() {
    this.selectedEmpleado = null;
    const form = this.container.querySelector('#form-empleado');
    form.reset();
    this.container.querySelector('#emp-id').value = '';
    this.container.querySelector('#modal-empleado-title').textContent = 'Nuevo Empleado';
    this.container.querySelector('#emp-activo').checked = true;
    this.container.querySelector('#emp-password').required = true;
    this.container.querySelector('#lbl-emp-password').innerHTML = 'Contraseña <span class="text-danger">*</span>';
    this.container.querySelector('#help-emp-password').textContent = 'Mínimo 6 caracteres con mayúsculas y números.';

    const errBox = this.container.querySelector('#form-empleado-error');
    errBox.classList.add('hidden');
    errBox.textContent = '';

    const modal = this.container.querySelector('#modal-empleado');
    modal.classList.add('is-active');
  }

  async openModalEditar(id) {
    const errBox = this.container.querySelector('#form-empleado-error');
    errBox.classList.add('hidden');
    errBox.textContent = '';

    try {
      const emp = await empleadosService.obtenerPorId(id);
      this.selectedEmpleado = emp;

      this.container.querySelector('#emp-id').value = emp.id;
      this.container.querySelector('#emp-nombre').value = emp.nombre || '';
      this.container.querySelector('#emp-apellido').value = emp.apellido || '';
      this.container.querySelector('#emp-documento').value = emp.documento || '';
      this.container.querySelector('#emp-telefono').value = emp.telefono || '';
      this.container.querySelector('#emp-email').value = emp.email || '';
      this.container.querySelector('#emp-username').value = emp.username || '';
      this.container.querySelector('#emp-password').value = '';
      this.container.querySelector('#emp-password').required = false;
      this.container.querySelector('#lbl-emp-password').innerHTML = 'Contraseña (Opcional)';
      this.container.querySelector('#help-emp-password').textContent = 'Dejar en blanco para conservar la contraseña actual.';
      this.container.querySelector('#emp-rol').value = emp.rolId || '';
      this.container.querySelector('#emp-activo').checked = emp.estado === 'Activo' || emp.estado === 1 || emp.activo === true;

      this.container.querySelector('#modal-empleado-title').textContent = `Editar Empleado: ${emp.nombreCompleto || emp.username}`;

      const modal = this.container.querySelector('#modal-empleado');
      modal.classList.add('is-active');
    } catch (err) {
      toast.error('No se pudo cargar la información del empleado.');
    }
  }

  closeModalEmpleado() {
    const modal = this.container.querySelector('#modal-empleado');
    modal.classList.remove('is-active');
  }

  async _handleSubmitEmpleado(e) {
    e.preventDefault();
    const saveBtn = this.container.querySelector('#btn-save-empleado');
    const errBox = this.container.querySelector('#form-empleado-error');
    errBox.classList.add('hidden');
    errBox.textContent = '';

    const id = this.container.querySelector('#emp-id').value;
    const nombre = this.container.querySelector('#emp-nombre').value.trim();
    const apellido = this.container.querySelector('#emp-apellido').value.trim();
    const documento = this.container.querySelector('#emp-documento').value.trim();
    const telefono = this.container.querySelector('#emp-telefono').value.trim();
    const email = this.container.querySelector('#emp-email').value.trim();
    const username = this.container.querySelector('#emp-username').value.trim();
    const password = this.container.querySelector('#emp-password').value;
    const rolIdVal = this.container.querySelector('#emp-rol').value;
    const activo = this.container.querySelector('#emp-activo').checked;

    if (!nombre || !apellido || !documento || !email || !username || !rolIdVal) {
      errBox.textContent = 'Por favor complete todos los campos obligatorios.';
      errBox.classList.remove('hidden');
      return;
    }

    if (!id && !password) {
      errBox.textContent = 'Debe especificar una contraseña para el nuevo empleado.';
      errBox.classList.remove('hidden');
      return;
    }

    // Salvaguarda UI: Si se modifica el único administrador activo desmarcándolo como activo o cambiando su rol
    if (id && this.selectedEmpleado && (this.selectedEmpleado.rol === 'ADMINISTRADOR' || this.selectedEmpleado.rol === 'Administrador')) {
      const selectedRolObj = this.roles.find(r => String(r.id) === String(rolIdVal));
      const willBeActiveAdmin = activo && selectedRolObj && (selectedRolObj.nombre === 'ADMINISTRADOR' || selectedRolObj.nombre === 'Administrador');
      if (!willBeActiveAdmin) {
        const activeAdmins = this.empleados.filter(e => 
          (e.rol === 'ADMINISTRADOR' || e.rol === 'Administrador') && 
          (e.estado === 'Activo' || e.activo === true)
        );
        if (activeAdmins.length <= 1) {
          errBox.textContent = 'No se puede desactivar el único administrador activo.';
          errBox.classList.remove('hidden');
          return;
        }
      }
    }

    const payload = {
      nombre,
      apellido,
      documento,
      telefono: telefono || null,
      email,
      username,
      rolId: parseInt(rolIdVal, 10),
      activo,
      estado: activo ? 'Activo' : 'Inactivo'
    };

    if (password) {
      payload.password = password;
    }

    saveBtn.disabled = true;
    saveBtn.classList.add('is-loading');

    try {
      if (id) {
        await empleadosService.actualizar(id, payload);
        toast.success(`Empleado ${nombre} ${apellido} actualizado correctamente.`);
      } else {
        await empleadosService.crear(payload);
        toast.success(`Empleado ${nombre} ${apellido} creado exitosamente.`);
      }

      this.closeModalEmpleado();
      await this.loadEmpleados();
    } catch (err) {
      const msg = err.message || 'Error al guardar empleado.';
      errBox.textContent = msg;
      errBox.classList.remove('hidden');
      toast.error(msg);
    } finally {
      saveBtn.disabled = false;
      saveBtn.classList.remove('is-loading');
    }
  }

  openModalDelete(id, name) {
    this.selectedDeleteId = id;
    const prompt = this.container.querySelector('#delete-empleado-prompt');
    const errBox = this.container.querySelector('#delete-empleado-error');
    const confirmBtn = this.container.querySelector('#btn-confirm-delete');
    errBox.classList.add('hidden');
    errBox.textContent = '';
    if (confirmBtn) confirmBtn.disabled = false;

    // Salvaguarda UI: comprobar si es el único administrador activo
    const targetEmp = this.empleados.find(e => String(e.id) === String(id));
    if (targetEmp && (targetEmp.rol === 'ADMINISTRADOR' || targetEmp.rol === 'Administrador')) {
      const activeAdmins = this.empleados.filter(e => 
        (e.rol === 'ADMINISTRADOR' || e.rol === 'Administrador') && 
        (e.estado === 'Activo' || e.activo === true)
      );
      if (activeAdmins.length <= 1) {
        errBox.textContent = 'No se puede desactivar el único administrador activo.';
        errBox.classList.remove('hidden');
        if (confirmBtn) confirmBtn.disabled = true;
      }
    }

    prompt.innerHTML = `¿Está seguro de que desea dar de baja al empleado <strong>${escapeHtml(name)}</strong>?`;
    const modal = this.container.querySelector('#modal-delete-empleado');
    modal.classList.add('is-active');
  }

  closeModalDelete() {
    const modal = this.container.querySelector('#modal-delete-empleado');
    modal.classList.remove('is-active');
    this.selectedDeleteId = null;
  }

  async _handleConfirmDelete() {
    if (!this.selectedDeleteId) return;

    const confirmBtn = this.container.querySelector('#btn-confirm-delete');
    const errBox = this.container.querySelector('#delete-empleado-error');
    errBox.classList.add('hidden');
    errBox.textContent = '';

    confirmBtn.disabled = true;
    confirmBtn.classList.add('is-loading');

    try {
      await empleadosService.desactivar(this.selectedDeleteId);
      toast.success('Empleado desactivado exitosamente.');
      this.closeModalDelete();
      await this.loadEmpleados();
    } catch (err) {
      const msg = err.message || 'Error al desactivar empleado';
      errBox.textContent = msg;
      errBox.classList.remove('hidden');
      toast.error(msg);
    } finally {
      confirmBtn.disabled = false;
      confirmBtn.classList.remove('is-loading');
    }
  }
}
