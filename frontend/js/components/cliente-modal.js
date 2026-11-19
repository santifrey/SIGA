/**
 * SIGA - components/cliente-modal.js
 * Componente reutilizable de Modal de Creación y Edición de Cliente.
 * Compartido entre ClientesView y MovimientosComercialesView sin duplicar HTML ni validaciones.
 */

import { clientesService } from '../services/clientes-service.js';
import { sucursalesService } from '../services/sucursales-service.js';
import { toast } from '../utils/toast.js';
import { loadTemplate } from '../utils/template-loader.js';

export class ClienteModal {
  /**
   * @param {Object} options
   * @param {HTMLElement} options.container - Contenedor de la vista donde se montará el modal
   * @param {(clienteGuardado: Object, isEdit: boolean) => Promise<void>|void} [options.onSaved] - Callback ejecutado al guardar con éxito
   */
  constructor({ container, onSaved }) {
    this.container = container;
    this.onSaved = onSaved;
    this.selectedCliente = null;
  }

  /**
   * Inyecta la plantilla HTML del modal en el contenedor (si aún no existe) y enlaza sus eventos.
   */
  async init() {
    if (!this.container.querySelector('#modal-cliente')) {
      const html = await loadTemplate('cliente-modal.html');
      this.container.insertAdjacentHTML('beforeend', html);
    }
    await this._loadSucursalesDropdown();
    this._bindEvents();
  }

  async _loadSucursalesDropdown() {
    try {
      const sucursales = await sucursalesService.listar({ estado: 'activas' });
      const select = this.container.querySelector('#cliente-sucursal');
      if (select && Array.isArray(sucursales)) {
        select.innerHTML = '<option value="">Sin sucursal asignada (Central / No definida)</option>' +
          sucursales.map(s => `<option value="${s.id}">🏢 ${s.nombre}</option>`).join('');
      }
    } catch (err) {
      console.warn('[ClienteModal] No se pudieron cargar sucursales:', err);
    }
  }

  _bindEvents() {
    this.container.querySelector('#btn-close-modal-cliente')?.addEventListener('click', () => this.close());
    this.container.querySelector('#btn-cancel-modal-cliente')?.addEventListener('click', () => this.close());
    this.container.querySelector('#form-cliente')?.addEventListener('submit', (e) => this._handleSubmit(e));
    this.container.querySelector('#form-cliente')?.addEventListener('input', (e) => {
      if (e.target?.classList?.contains('is-invalid')) {
        e.target.classList.remove('is-invalid');
      }
    });
  }

  openNuevo(defaultSucursalId = null) {
    this.selectedCliente = null;
    const form = this.container.querySelector('#form-cliente');
    if (form) form.reset();

    const idInp = this.container.querySelector('#cliente-id');
    if (idInp) idInp.value = '';

    const sucInp = this.container.querySelector('#cliente-sucursal');
    if (sucInp) sucInp.value = defaultSucursalId ? String(defaultSucursalId) : '';

    const contratoInp = this.container.querySelector('#cliente-contrato-id');
    if (contratoInp) contratoInp.value = '';

    const titleEl = this.container.querySelector('#modal-cliente-title');
    if (titleEl) titleEl.textContent = 'Nuevo Cliente';

    const activoChk = this.container.querySelector('#cliente-activo');
    if (activoChk) activoChk.checked = true;

    this._clearValidationErrors();

    const modal = this.container.querySelector('#modal-cliente');
    if (modal) {
      modal.classList.add('is-active');
      setTimeout(() => this.container.querySelector('#cliente-nombre')?.focus(), 100);
    }
  }

  async openEditar(id) {
    this._clearValidationErrors();
    try {
      const cliente = await clientesService.obtenerPorId(id);
      this.selectedCliente = cliente;

      this.container.querySelector('#cliente-id').value = cliente.id;
      this.container.querySelector('#cliente-nombre').value = cliente.nombre || '';
      this.container.querySelector('#cliente-apellido').value = cliente.apellido || '';
      this.container.querySelector('#cliente-tipo-doc').value = cliente.tipoDocumento || 'DNI';
      this.container.querySelector('#cliente-doc').value = cliente.documento || '';
      this.container.querySelector('#cliente-email').value = cliente.email || '';
      this.container.querySelector('#cliente-telefono').value = cliente.telefono || '';
      if (this.container.querySelector('#cliente-sucursal')) {
        this.container.querySelector('#cliente-sucursal').value = cliente.sucursalId || '';
      }
      this.container.querySelector('#cliente-codigo').value = cliente.codigo || '';
      this.container.querySelector('#cliente-contrato-id').value = cliente.contratoId || '';
      this.container.querySelector('#cliente-activo').checked = cliente.activo !== false;
      this.container.querySelector('#cliente-observaciones').value = cliente.observaciones || '';

      const titleEl = this.container.querySelector('#modal-cliente-title');
      if (titleEl) titleEl.textContent = `Editar Cliente: ${cliente.nombre} ${cliente.apellido}`;

      const modal = this.container.querySelector('#modal-cliente');
      if (modal) {
        modal.classList.add('is-active');
        setTimeout(() => this.container.querySelector('#cliente-nombre')?.focus(), 100);
      }
    } catch {
      toast.error('No se pudo cargar el cliente para editar.');
    }
  }

  close() {
    const modal = this.container.querySelector('#modal-cliente');
    modal?.classList.remove('is-active');
  }

  _clearValidationErrors() {
    this.container.querySelectorAll('#form-cliente .form-control').forEach(i => i.classList.remove('is-invalid'));
    const valDoc = this.container.querySelector('#val-doc');
    if (valDoc) valDoc.textContent = 'El número de documento es obligatorio.';
    const valEmail = this.container.querySelector('#val-email');
    if (valEmail) valEmail.textContent = 'Ingrese un correo electrónico válido.';
    const valTel = this.container.querySelector('#val-telefono');
    if (valTel) valTel.textContent = 'El teléfono solo debe contener números y símbolos válidos (+, -, espacios).';
  }

  async _handleSubmit(e) {
    e.preventDefault();

    const id = this.container.querySelector('#cliente-id')?.value || '';
    const nombre = this.container.querySelector('#cliente-nombre')?.value.trim() || '';
    const apellido = this.container.querySelector('#cliente-apellido')?.value.trim() || '';
    const tipoDocumento = this.container.querySelector('#cliente-tipo-doc')?.value || 'DNI';
    const documento = this.container.querySelector('#cliente-doc')?.value.trim() || '';
    const email = this.container.querySelector('#cliente-email')?.value.trim() || '';
    const telefono = this.container.querySelector('#cliente-telefono')?.value.trim() || '';
    const sucursalIdVal = this.container.querySelector('#cliente-sucursal')?.value || '';
    const codigo = this.container.querySelector('#cliente-codigo')?.value.trim() || '';
    const contratoIdVal = this.container.querySelector('#cliente-contrato-id')?.value.trim() || '';
    const activo = this.container.querySelector('#cliente-activo')?.checked ?? true;
    const observaciones = this.container.querySelector('#cliente-observaciones')?.value.trim() || '';

    const errors = [];
    this._clearValidationErrors();

    // 1. Nombre
    if (!nombre) {
      this.container.querySelector('#cliente-nombre')?.classList.add('is-invalid');
      errors.push('El nombre es obligatorio.');
    }

    // 2. Apellido
    if (!apellido) {
      this.container.querySelector('#cliente-apellido')?.classList.add('is-invalid');
      errors.push('El apellido es obligatorio.');
    }

    // 3. Documento (DNI / CUIT / Pasaporte)
    const docInput = this.container.querySelector('#cliente-doc');
    const valDoc = this.container.querySelector('#val-doc');
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
    const emailInput = this.container.querySelector('#cliente-email');
    const valEmail = this.container.querySelector('#val-email');
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

    // 5. Teléfono (opcional, pero si se ingresa debe tener formato telefónico)
    if (telefono) {
      const telInput = this.container.querySelector('#cliente-telefono');
      const valTel = this.container.querySelector('#val-telefono');
      const telRegex = /^[\d\s+\-()]{6,25}$/;
      if (!telRegex.test(telefono)) {
        telInput?.classList.add('is-invalid');
        const msg = 'El teléfono ingresado solo debe contener números y símbolos válidos (+, -, espacios).';
        if (valTel) valTel.textContent = msg;
        errors.push(msg);
      }
    }

    if (errors.length > 0) {
      const firstInvalid = this.container.querySelector('.form-control.is-invalid');
      firstInvalid?.focus();

      if (errors.length === 1) {
        toast.warning(errors[0], 'Campo Inválido');
      } else {
        toast.warning(errors[0], `Por favor revise el formulario (${errors.length} campos con error)`);
      }
      return;
    }

    const payload = {
      nombre,
      apellido,
      tipoDocumento,
      documento,
      email,
      telefono,
      sucursalId: sucursalIdVal ? parseInt(sucursalIdVal, 10) : null,
      codigo,
      contratoId: contratoIdVal ? parseInt(contratoIdVal, 10) : null,
      activo,
      observaciones
    };

    const saveBtn = this.container.querySelector('#btn-save-cliente');
    if (saveBtn) {
      saveBtn.classList.add('is-loading');
      saveBtn.disabled = true;
    }

    try {
      let savedCliente = null;
      const isEdit = Boolean(id);

      if (isEdit) {
        savedCliente = await clientesService.actualizar(id, payload);
        toast.success(`Cliente ${nombre} ${apellido} actualizado exitosamente.`);
      } else {
        savedCliente = await clientesService.crear(payload);
        toast.success(`Cliente ${nombre} ${apellido} registrado. Acceso: ${email} | Clave: DNI`);
      }

      this.close();
      if (this.onSaved) {
        await this.onSaved(savedCliente, isEdit);
      }
    } catch (err) {
      toast.error(err.message || 'Error al guardar el cliente');
    } finally {
      if (saveBtn) {
        saveBtn.classList.remove('is-loading');
        saveBtn.disabled = false;
      }
    }
  }
}
