/**
 * SIGA - views/empleados-view.js
 * Vista de Gestión de Empleados (#empleados) (Feature 30)
 * Administra nómina interna (Persona + Usuario no-CLIENTE), selector de roles,
 * baja lógica con símbolo de prohibición SVG y salvaguarda para el último administrador.
 */

import { empleadosService } from '../services/empleados-service.js';
import { rolesService } from '../services/roles-service.js';
import { sucursalesService } from '../services/sucursales-service.js';
import { authService } from '../services/auth-service.js';
import { ConfirmDeleteModal } from '../components/confirm-delete-modal.js';
import { renderPagination } from '../components/pagination.js';
import { loadTemplate } from '../utils/template-loader.js';
import { toast } from '../utils/toast.js';
import { escapeHtml } from '../utils/escape-html.js';
import { getProhibitionIcon } from '../utils/icons.js';

export class EmpleadosView {
  constructor(router) {
    this.router = router;
    this.container = null;
    this.empleados = [];
    this.roles = [];
    this.sucursales = [];
    this.selectedEmpleado = null;
    this.deleteModal = null;

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

    this.container.innerHTML = await loadTemplate('empleados.html');

    if (!canManage) {
      this.container.querySelector('#btn-nuevo-empleado')?.remove();
    }

    this.deleteModal = new ConfirmDeleteModal({
      container: this.container,
      title: 'Desactivar Empleado',
      confirmText: 'Confirmar Baja',
      hintText: 'El usuario perderá acceso inmediato a la plataforma pero se conservará su historial y trazabilidad.',
      onConfirm: async (id) => {
        await empleadosService.desactivar(id);
        toast.success('Empleado desactivado exitosamente.');
        await this.loadEmpleados();
      }
    });
    await this.deleteModal.init();

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
    this.container.querySelector('#form-empleado')?.addEventListener('input', (e) => {
      if (e.target?.classList?.contains('is-invalid')) {
        e.target.classList.remove('is-invalid');
      }
    });
    this.container.querySelector('#form-empleado')?.addEventListener('change', (e) => {
      if (e.target?.classList?.contains('is-invalid')) {
        e.target.classList.remove('is-invalid');
      }
    });
  }

  async _loadInitialData() {
    try {
      const [roles, sucursales] = await Promise.all([
        rolesService.listar().catch(() => []),
        sucursalesService.listar().catch(() => [])
      ]);
      this.roles = Array.isArray(roles) ? roles : [];
      this.sucursales = Array.isArray(sucursales) ? sucursales : [];
      this._populateRolesDropdowns();
      this._populateSucursalesDropdown();
    } catch (err) {
      console.warn('[EmpleadosView] No se pudieron cargar los datos iniciales:', err);
    }
    await this.loadEmpleados();
  }

  _populateSucursalesDropdown() {
    const sucSelect = this.container.querySelector('#emp-sucursal');
    if (!sucSelect) return;
    sucSelect.innerHTML = '<option value="">Sin sucursal asignada (Central / Corporativo)</option>' +
      this.sucursales.map(s => `<option value="${s.id}">🏢 ${escapeHtml(s.nombre)}</option>`).join('');
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
        <td colspan="10" class="text-center text-muted" style="padding: 2.5rem;">
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
          <td colspan="10" class="text-center" style="padding: 2.5rem; color: var(--danger);">
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
      const legajo = (emp.legajo || '').toLowerCase();
      const posicion = (emp.posicion || '').toLowerCase();

      const matchesSearch = !this.filterSearch ||
        fullName.includes(this.filterSearch) ||
        username.includes(this.filterSearch) ||
        doc.includes(this.filterSearch) ||
        email.includes(this.filterSearch) ||
        legajo.includes(this.filterSearch) ||
        posicion.includes(this.filterSearch);

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
          <td colspan="10" class="text-center text-muted" style="padding: 2.5rem;">
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
            <span class="badge badge-secondary font-mono">${escapeHtml(emp.legajo || '-')}</span>
          </td>
          <td>
            <div class="text-sm font-medium text-main">${escapeHtml(emp.posicion || '-')}</div>
          </td>
          <td>
            <div class="font-medium">${escapeHtml(emp.documento || '-')}</div>
          </td>
          <td>
            <div class="text-sm">${escapeHtml(emp.email || '-')}</div>
            <div class="text-xs text-muted">${escapeHtml(emp.telefono || 'Sin teléfono')}</div>
          </td>
          <td>
            ${emp.sucursalNombre ? `
              <span class="badge badge-secondary text-xs font-semibold">
                🏢 ${escapeHtml(emp.sucursalNombre)}
              </span>
            ` : `
              <span class="text-xs text-muted">-</span>
            `}
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
                ` : `
                  <button class="btn-icon btn-action-reactivate" data-id="${escapeHtml(emp.id)}" title="Reactivar empleado">
                    <svg class="icon-svg sm" viewBox="0 0 24 24" style="color: var(--success);">
                      <polyline points="23 4 23 10 17 10"></polyline>
                      <path d="M20.49 15a9 9 0 1 1-2.12-9.36L23 10"></path>
                    </svg>
                  </button>
                `}
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

    tbody.querySelectorAll('.btn-action-reactivate').forEach(btn => {
      btn.addEventListener('click', async () => {
        const id = btn.getAttribute('data-id');
        try {
          await empleadosService.reactivar(id);
          toast.success('Empleado reactivado correctamente.');
          await this.loadEmpleados();
        } catch (err) {
          toast.error(err.message || 'Error al reactivar empleado');
        }
      });
    });

    // Paginación
    renderPagination({
      infoEl,
      controlsEl,
      currentPage: this.currentPage,
      pageSize: this.pageSize,
      totalItems: total,
      totalPages,
      entityLabel: 'empleados',
      onPageChange: (newPage) => {
        this.currentPage = newPage;
        this._renderTable();
      }
    });
  }

  _clearValidationErrors() {
    this.container.querySelectorAll('#form-empleado .form-control').forEach(i => i.classList.remove('is-invalid'));
    const valNombre = this.container.querySelector('#val-emp-nombre');
    if (valNombre) valNombre.textContent = 'El nombre es obligatorio.';
    const valApellido = this.container.querySelector('#val-emp-apellido');
    if (valApellido) valApellido.textContent = 'El apellido es obligatorio.';
    const valDoc = this.container.querySelector('#val-emp-doc');
    if (valDoc) valDoc.textContent = 'El número de documento es obligatorio.';
    const valEmail = this.container.querySelector('#val-emp-email');
    if (valEmail) valEmail.textContent = 'El correo electrónico es obligatorio.';
    const valTel = this.container.querySelector('#val-emp-telefono');
    if (valTel) valTel.textContent = 'El teléfono solo debe contener números y símbolos válidos (+, -, espacios).';
    const valRol = this.container.querySelector('#val-emp-rol');
    if (valRol) valRol.textContent = 'Debe seleccionar un rol para el empleado.';
  }

  openModalNuevo() {
    this.selectedEmpleado = null;
    this._clearValidationErrors();
    const form = this.container.querySelector('#form-empleado');
    form.reset();
    this.container.querySelector('#emp-id').value = '';
    this.container.querySelector('#emp-legajo').value = '';
    this.container.querySelector('#emp-posicion').value = '';
    if (this.container.querySelector('#emp-tipo-doc')) {
      this.container.querySelector('#emp-tipo-doc').value = 'DNI';
    }
    if (this.container.querySelector('#emp-sucursal')) {
      this.container.querySelector('#emp-sucursal').value = '';
    }
    this.container.querySelector('#modal-empleado-title').textContent = 'Nuevo Empleado';
    this.container.querySelector('#emp-activo').checked = true;

    const modal = this.container.querySelector('#modal-empleado');
    modal.classList.add('is-active');
    setTimeout(() => this.container.querySelector('#emp-nombre')?.focus(), 50);
  }

  async openModalEditar(id) {
    this._clearValidationErrors();

    try {
      const emp = await empleadosService.obtenerPorId(id);
      this.selectedEmpleado = emp;

      this.container.querySelector('#emp-id').value = emp.id;
      this.container.querySelector('#emp-legajo').value = emp.legajo || '';
      this.container.querySelector('#emp-posicion').value = emp.posicion || '';
      if (this.container.querySelector('#emp-sucursal')) {
        this.container.querySelector('#emp-sucursal').value = emp.sucursalId || '';
      }
      this.container.querySelector('#emp-nombre').value = emp.nombre || '';
      this.container.querySelector('#emp-apellido').value = emp.apellido || '';

      const docVal = emp.documento || '';
      this.container.querySelector('#emp-documento').value = docVal;
      if (this.container.querySelector('#emp-tipo-doc')) {
        const cleanDoc = docVal.replace(/[-\s]/g, '');
        if (cleanDoc.length === 11) {
          this.container.querySelector('#emp-tipo-doc').value = 'CUIT';
        } else {
          this.container.querySelector('#emp-tipo-doc').value = 'DNI';
        }
      }

      this.container.querySelector('#emp-telefono').value = emp.telefono || '';
      this.container.querySelector('#emp-email').value = emp.email || '';
      this.container.querySelector('#emp-rol').value = emp.rolId || '';
      this.container.querySelector('#emp-activo').checked = emp.estado === 'Activo' || emp.estado === 1 || emp.activo === true;

      this.container.querySelector('#modal-empleado-title').textContent = `Editar Empleado: ${emp.nombreCompleto || emp.username}`;

      const modal = this.container.querySelector('#modal-empleado');
      modal.classList.add('is-active');
      setTimeout(() => this.container.querySelector('#emp-nombre')?.focus(), 50);
    } catch (err) {
      toast.error('No se pudo cargar la información del empleado.');
    }
  }

  closeModalEmpleado() {
    this._clearValidationErrors();
    const modal = this.container.querySelector('#modal-empleado');
    modal.classList.remove('is-active');
  }

  async _handleSubmitEmpleado(e) {
    e.preventDefault();
    const saveBtn = this.container.querySelector('#btn-save-empleado');

    const id = this.container.querySelector('#emp-id').value;
    const legajo = this.container.querySelector('#emp-legajo').value.trim();
    const posicion = this.container.querySelector('#emp-posicion').value.trim();
    const sucursalIdVal = this.container.querySelector('#emp-sucursal')?.value || '';
    const nombre = this.container.querySelector('#emp-nombre').value.trim();
    const apellido = this.container.querySelector('#emp-apellido').value.trim();
    const tipoDocumento = this.container.querySelector('#emp-tipo-doc')?.value || 'DNI';
    const documento = this.container.querySelector('#emp-documento').value.trim();
    const telefono = this.container.querySelector('#emp-telefono').value.trim();
    const email = this.container.querySelector('#emp-email').value.trim();
    const rolIdVal = this.container.querySelector('#emp-rol').value;
    const activo = this.container.querySelector('#emp-activo').checked;

    const errors = [];
    this._clearValidationErrors();

    // 1. Nombre
    if (!nombre) {
      this.container.querySelector('#emp-nombre')?.classList.add('is-invalid');
      errors.push('El nombre es obligatorio.');
    }

    // 2. Apellido
    if (!apellido) {
      this.container.querySelector('#emp-apellido')?.classList.add('is-invalid');
      errors.push('El apellido es obligatorio.');
    }

    // 3. Documento (DNI / CUIT / Pasaporte)
    const docInput = this.container.querySelector('#emp-documento');
    const valDoc = this.container.querySelector('#val-emp-doc');
    if (!documento) {
      docInput?.classList.add('is-invalid');
      if (valDoc) valDoc.textContent = 'El número de documento es obligatorio.';
      errors.push('El número de documento es obligatorio.');
    } else if (tipoDocumento === 'DNI') {
      const cleanDoc = documento.replace(/\./g, '');
      if (!/^\d{7,9}$/.test(cleanDoc)) {
        docInput?.classList.add('is-invalid');
        const msg = 'El DNI debe ser numérico y contener entre 7 y 9 dígitos.';
        if (valDoc) valDoc.textContent = msg;
        errors.push(msg);
      }
    } else if (tipoDocumento === 'CUIT') {
      const cleanCuit = documento.replace(/[-\s]/g, '');
      if (!/^\d{11}$/.test(cleanCuit)) {
        docInput?.classList.add('is-invalid');
        const msg = 'El CUIT debe contener 11 dígitos numéricos.';
        if (valDoc) valDoc.textContent = msg;
        errors.push(msg);
      }
    }

    // 4. Correo electrónico
    const emailInput = this.container.querySelector('#emp-email');
    const valEmail = this.container.querySelector('#val-emp-email');
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!email) {
      emailInput?.classList.add('is-invalid');
      if (valEmail) valEmail.textContent = 'El correo electrónico es obligatorio.';
      errors.push('El correo electrónico es obligatorio.');
    } else if (!emailRegex.test(email)) {
      emailInput?.classList.add('is-invalid');
      const msg = 'El formato del correo electrónico ingresado no es válido (ej. usuario@dominio.com).';
      if (valEmail) valEmail.textContent = msg;
      errors.push(msg);
    }

    // 5. Teléfono (opcional, pero si se ingresa debe tener formato válido)
    if (telefono) {
      const telInput = this.container.querySelector('#emp-telefono');
      const valTel = this.container.querySelector('#val-emp-telefono');
      const telRegex = /^[\d\s+\-()]{6,25}$/;
      if (!telRegex.test(telefono)) {
        telInput?.classList.add('is-invalid');
        const msg = 'El teléfono ingresado solo debe contener números y símbolos válidos (+, -, espacios).';
        if (valTel) valTel.textContent = msg;
        errors.push(msg);
      }
    }

    // 6. Rol del sistema
    const rolInput = this.container.querySelector('#emp-rol');
    if (!rolIdVal) {
      rolInput?.classList.add('is-invalid');
      errors.push('Debe seleccionar un rol para el empleado.');
    }

    if (errors.length > 0) {
      const firstInvalid = this.container.querySelector('#form-empleado .form-control.is-invalid');
      firstInvalid?.focus();

      if (errors.length === 1) {
        toast.warning(errors[0], 'Campo Inválido');
      } else {
        toast.warning(errors[0], `Por favor revise el formulario (${errors.length} campos con error)`);
      }
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
          toast.warning('No se puede desactivar el único administrador activo.', 'Acción Denegada');
          return;
        }
      }
    }

    const payload = {
      nombre,
      apellido,
      documento,
      legajo: legajo || null,
      posicion: posicion || null,
      telefono: telefono || null,
      sucursalId: sucursalIdVal ? parseInt(sucursalIdVal, 10) : null,
      email,
      username: email.toLowerCase(),
      rolId: parseInt(rolIdVal, 10),
      activo,
      estado: activo ? 'Activo' : 'Inactivo'
    };

    if (!id) {
      // Siempre asignar DNI como clave inicial para nuevos empleados
      payload.password = documento;
    }

    saveBtn.disabled = true;
    saveBtn.classList.add('is-loading');

    try {
      if (id) {
        await empleadosService.actualizar(id, payload);
        toast.success(`Empleado ${nombre} ${apellido} actualizado correctamente.`);
      } else {
        await empleadosService.crear(payload);
        toast.success(`Empleado ${nombre} ${apellido} creado exitosamente. Acceso: ${email.toLowerCase()} | Clave: DNI`);
      }

      this.closeModalEmpleado();
      await this.loadEmpleados();
    } catch (err) {
      const msg = err.message || 'Error al guardar empleado.';
      toast.error(msg);
    } finally {
      saveBtn.disabled = false;
      saveBtn.classList.remove('is-loading');
    }
  }

  openModalDelete(id, name) {
    let blockingError = null;
    const targetEmp = this.empleados.find(e => String(e.id) === String(id));
    if (targetEmp && (targetEmp.rol === 'ADMINISTRADOR' || targetEmp.rol === 'Administrador')) {
      const activeAdmins = this.empleados.filter(e =>
        (e.rol === 'ADMINISTRADOR' || e.rol === 'Administrador') &&
        (e.estado === 'Activo' || e.activo === true)
      );
      if (activeAdmins.length <= 1) {
        blockingError = 'No se puede desactivar el único administrador activo.';
      }
    }

    this.deleteModal.open({
      id,
      promptHtml: `¿Está seguro de que desea dar de baja al empleado <strong>${escapeHtml(name)}</strong>?`,
      blockingError
    });
  }
}
