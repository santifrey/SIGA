/**
 * SIGA - views/sucursales-view.js
 * Gestión de Sucursales: Tarjetas / Tabla, Modales de Creación y Edición
 */

import { sucursalesService } from '../services/sucursales-service.js';
import { toast } from '../utils/toast.js';
import { escapeHtml } from '../utils/escape-html.js';

export class SucursalesView {
  constructor(router) {
    this.router = router;
    this.container = null;
    this.searchTerm = '';
    this.filterEstado = 'todas';
    this.viewMode = 'cards'; // 'cards' o 'table'
    this.selectedSucursal = null;
  }

  async render(rootElement, queryParams = {}) {
    this.container = rootElement;
    const autoOpenNew = queryParams && queryParams.action === 'new';

    this.container.innerHTML = `
      <div class="fade-in">
        <!-- Encabezado de Sección -->
        <div class="d-flex justify-between align-center mb-4 flex-wrap gap-md">
          <div>
            <h1 style="font-size: 1.5rem; margin-bottom: 0.25rem;">Gestión de Sucursales</h1>
            <p class="text-muted text-sm">Control de sedes operativas, capacidades instaladas y responsables</p>
          </div>
          <div class="d-flex gap-sm">
            <button class="btn btn-secondary" id="btn-refresh-sucursales" title="Refrescar">
              <svg class="icon-svg sm" viewBox="0 0 24 24">
                <polyline points="23 4 23 10 17 10"></polyline>
                <polyline points="1 20 1 14 7 14"></polyline>
                <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"></path>
              </svg>
              <span>Refrescar</span>
            </button>
            <button class="btn btn-primary" id="btn-new-sucursal">
              <svg class="icon-svg sm" viewBox="0 0 24 24">
                <line x1="12" y1="5" x2="12" y2="19"></line>
                <line x1="5" y1="12" x2="19" y2="12"></line>
              </svg>
              <span>Nueva Sucursal</span>
            </button>
          </div>
        </div>

        <!-- Barra de herramientas (Búsqueda y Modo de vista) -->
        <div class="table-card mb-4" style="border-radius: var(--radius-lg);">
          <div class="table-toolbar">
            <div class="toolbar-search">
              <div class="input-group">
                <span class="input-icon-left">
                  <svg class="icon-svg sm" viewBox="0 0 24 24">
                    <circle cx="11" cy="11" r="8"></circle>
                    <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
                  </svg>
                </span>
                <input
                  type="text"
                  id="search-sucursales"
                  class="form-control has-icon-left"
                  placeholder="Buscar por nombre, código o ciudad..."
                  value="${escapeHtml(this.searchTerm)}"
                />
              </div>
            </div>

            <div class="toolbar-actions">
              <label class="text-xs text-muted font-semibold">Estado:</label>
              <select id="filter-sucursales-estado" class="form-control" style="width: 140px; padding: 0.45rem 2rem 0.45rem 0.75rem;">
                <option value="todas">Todas</option>
                <option value="activas">Solo Activas</option>
                <option value="inactivas">Solo Inactivas</option>
              </select>

              <div class="d-flex gap-xs" style="margin-left: 0.5rem;">
                <button type="button" class="btn btn-sm ${this.viewMode === 'cards' ? 'btn-primary' : 'btn-outline'}" id="btn-mode-cards" title="Vista en tarjetas">
                  <svg class="icon-svg sm" viewBox="0 0 24 24"><rect x="3" y="3" width="7" height="7"></rect><rect x="14" y="3" width="7" height="7"></rect><rect x="14" y="14" width="7" height="7"></rect><rect x="3" y="14" width="7" height="7"></rect></svg>
                </button>
                <button type="button" class="btn btn-sm ${this.viewMode === 'table' ? 'btn-primary' : 'btn-outline'}" id="btn-mode-table" title="Vista en tabla">
                  <svg class="icon-svg sm" viewBox="0 0 24 24"><line x1="8" y1="6" x2="21" y2="6"></line><line x1="8" y1="12" x2="21" y2="12"></line><line x1="8" y1="18" x2="21" y2="18"></line><line x1="3" y1="6" x2="3.01" y2="6"></line><line x1="3" y1="12" x2="3.01" y2="12"></line><line x1="3" y1="18" x2="3.01" y2="18"></line></svg>
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Contenedor dinámico (Tarjetas o Tabla) -->
        <div id="sucursales-container">
          <div class="text-center text-muted" style="padding: 3rem;">Cargando sucursales...</div>
        </div>
      </div>

      <!-- Modal Crear / Editar Sucursal -->
      <div id="modal-sucursal" class="modal-backdrop" role="dialog" aria-modal="true">
        <div class="modal-dialog modal-lg">
          <div class="modal-header">
            <h3 class="modal-title" id="modal-sucursal-title">Nueva Sucursal</h3>
            <button type="button" class="modal-close-btn" id="btn-close-modal-sucursal">
              <svg class="icon-svg sm" viewBox="0 0 24 24"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
            </button>
          </div>
          <form id="form-sucursal" novalidate>
            <div class="modal-body">
              <input type="hidden" id="sucursal-id" />

              <div class="form-row two-cols">
                <div class="form-group">
                  <label for="sucursal-nombre" class="form-label">
                    Nombre de la Sucursal <span class="required-mark">*</span>
                  </label>
                  <input type="text" id="sucursal-nombre" class="form-control" placeholder="ej. Sucursal Córdoba Centro" required />
                  <div class="invalid-feedback">El nombre es obligatorio.</div>
                </div>

                <div class="form-group">
                  <label for="sucursal-codigo" class="form-label">
                    Código Identificador <span class="label-hint">(ej. SUC-CBA-01)</span>
                  </label>
                  <input type="text" id="sucursal-codigo" class="form-control" placeholder="ej. SUC-01" />
                </div>
              </div>

              <div class="form-row two-cols">
                <div class="form-group">
                  <label for="sucursal-direccion" class="form-label">
                    Dirección / Calle y Número <span class="required-mark">*</span>
                  </label>
                  <input type="text" id="sucursal-direccion" class="form-control" placeholder="ej. Bv. Chacabuco 780" required />
                  <div class="invalid-feedback">La dirección es obligatoria.</div>
                </div>

                <div class="form-group">
                  <label for="sucursal-ciudad" class="form-label">Ciudad / Localidad</label>
                  <input type="text" id="sucursal-ciudad" class="form-control" placeholder="ej. Córdoba Capital" />
                </div>
              </div>

              <div class="form-row three-cols">
                <div class="form-group">
                  <label for="sucursal-telefono" class="form-label">Teléfono</label>
                  <input type="text" id="sucursal-telefono" class="form-control" placeholder="ej. +54 351 468-9000" />
                </div>

                <div class="form-group">
                  <label for="sucursal-capacidad" class="form-label">
                    Capacidad (Puestos) <span class="required-mark">*</span>
                  </label>
                  <input type="number" id="sucursal-capacidad" class="form-control" min="1" max="2000" value="50" required />
                </div>

                <div class="form-group">
                  <label for="sucursal-responsable" class="form-label">Responsable / Encargado</label>
                  <input type="text" id="sucursal-responsable" class="form-control" placeholder="ej. Lic. Mariano Valdéz" />
                </div>
              </div>

              <div class="form-group mt-2">
                <label class="form-switch">
                  <input type="checkbox" id="sucursal-activa" class="switch-input" checked />
                  <span class="switch-slider"></span>
                  <span class="form-check-label font-semibold">Sucursal Operativa / Activa</span>
                </label>
              </div>
            </div>

            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" id="btn-cancel-modal-sucursal">Cancelar</button>
              <button type="submit" class="btn btn-primary" id="btn-save-sucursal">
                <span class="btn-text">Guardar Sucursal</span>
                <span class="btn-spinner"></span>
              </button>
            </div>
          </form>
        </div>
      </div>
    `;

    this._bindEvents();
    await this.loadSucursales();

    if (autoOpenNew) {
      this.openModalNuevo();
    }
  }

  _bindEvents() {
    // Buscador
    const searchInput = this.container.querySelector('#search-sucursales');
    searchInput.addEventListener('input', (e) => {
      this.searchTerm = e.target.value;
      this.loadSucursales();
    });

    // Filtro por Estado
    const filterSelect = this.container.querySelector('#filter-sucursales-estado');
    filterSelect.addEventListener('change', (e) => {
      this.filterEstado = e.target.value;
      this.loadSucursales();
    });

    // Toggle Vista Tarjetas / Tabla
    this.container.querySelector('#btn-mode-cards').addEventListener('click', () => {
      this.viewMode = 'cards';
      this.container.querySelector('#btn-mode-cards').className = 'btn btn-sm btn-primary';
      this.container.querySelector('#btn-mode-table').className = 'btn btn-sm btn-outline';
      this.loadSucursales();
    });

    this.container.querySelector('#btn-mode-table').addEventListener('click', () => {
      this.viewMode = 'table';
      this.container.querySelector('#btn-mode-cards').className = 'btn btn-sm btn-outline';
      this.container.querySelector('#btn-mode-table').className = 'btn btn-sm btn-primary';
      this.loadSucursales();
    });

    // Refrescar
    this.container.querySelector('#btn-refresh-sucursales').addEventListener('click', () => {
      this.loadSucursales();
    });

    // Botón nueva sucursal
    this.container.querySelector('#btn-new-sucursal').addEventListener('click', () => {
      this.openModalNuevo();
    });

    // Cierre modal
    this.container.querySelector('#btn-close-modal-sucursal').addEventListener('click', () => this.closeModal());
    this.container.querySelector('#btn-cancel-modal-sucursal').addEventListener('click', () => this.closeModal());

    // Submit form
    this.container.querySelector('#form-sucursal').addEventListener('submit', (e) => this._handleSubmit(e));
  }

  async loadSucursales() {
    const container = this.container.querySelector('#sucursales-container');
    try {
      const list = await sucursalesService.listar({
        search: this.searchTerm,
        estado: this.filterEstado
      });

      if (!list || list.length === 0) {
        container.innerHTML = `
          <div class="table-card">
            <div class="table-empty-state">
              <div class="empty-state-icon">
                <svg class="icon-svg lg" viewBox="0 0 24 24"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path></svg>
              </div>
              <h4 class="empty-state-title">No se encontraron sucursales</h4>
              <p class="empty-state-description">No hay registros que coincidan con la búsqueda actual.</p>
            </div>
          </div>
        `;
        return;
      }

      if (this.viewMode === 'cards') {
        container.innerHTML = `
          <div style="display: grid; grid-template-columns: repeat(auto-fill, minmax(320px, 1fr)); gap: var(--spacing-lg);">
            ${list.map(s => `
              <div class="card" style="transition: transform var(--transition-fast);">
                <div class="card-header" style="background: var(--surface-hover); border-bottom: 1px solid var(--border);">
                  <div class="d-flex align-center gap-sm">
                    <div style="width: 40px; height: 40px; border-radius: var(--radius-md); background: var(--primary-100); color: var(--primary); display: flex; align-items: center; justify-content: center; font-weight: 800; font-size: 0.875rem;">
                      ${s.codigo ? escapeHtml(s.codigo.substring(0, 7)) : 'SUC'}
                    </div>
                    <div>
                      <h4 class="font-bold text-sm text-main" style="margin: 0;">${escapeHtml(s.nombre)}</h4>
                      <small class="text-muted">${escapeHtml(s.ciudad || 'S/D')}</small>
                    </div>
                  </div>
                  <span class="badge ${s.activa ? 'badge-success' : 'badge-warning'}">
                    ${s.activa ? 'Activa' : 'Inactiva'}
                  </span>
                </div>

                <div class="card-body">
                  <div class="d-flex flex-column gap-sm text-sm">
                    <div class="d-flex align-center gap-xs text-body">
                      <svg class="icon-svg sm text-muted" viewBox="0 0 24 24"><path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"></path><circle cx="12" cy="10" r="3"></circle></svg>
                      <span>${escapeHtml(s.direccion || 'Sin dirección')}</span>
                    </div>

                    <div class="d-flex align-center gap-xs text-body">
                      <svg class="icon-svg sm text-muted" viewBox="0 0 24 24"><path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z"></path></svg>
                      <span>${escapeHtml(s.telefono || 'Sin teléfono')}</span>
                    </div>

                    <div class="d-flex align-center gap-xs text-body">
                      <svg class="icon-svg sm text-muted" viewBox="0 0 24 24"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path><circle cx="12" cy="7" r="4"></circle></svg>
                      <span>Resp: <strong>${escapeHtml(s.responsable || 'No asignado')}</strong></span>
                    </div>

                    <div class="mt-2 pt-2 d-flex justify-between align-center" style="border-top: 1px dashed var(--border);">
                      <span class="text-xs text-muted font-semibold">CAPACIDAD OPERATIVA:</span>
                      <span class="font-bold text-primary" style="font-size: 1.1rem;">${escapeHtml(s.capacidad || 0)} puestos</span>
                    </div>
                  </div>
                </div>

                <div class="card-footer">
                  <small class="text-muted">Cód: ${escapeHtml(s.codigo || s.id)}</small>
                  <div class="d-flex gap-xs">
                    <button class="btn btn-sm btn-outline btn-edit-sucursal" data-id="${escapeHtml(s.id)}">
                      Editar
                    </button>
                    ${s.activa ? `
                      <button class="btn btn-sm btn-outline btn-disable-sucursal" data-id="${escapeHtml(s.id)}" style="color: var(--danger); border-color: var(--danger-border);">
                        Desactivar
                      </button>
                    ` : ''}
                  </div>
                </div>
              </div>
            `).join('')}
          </div>
        `;
      } else {
        container.innerHTML = `
          <div class="table-card">
            <div class="table-responsive">
              <table class="table">
                <thead>
                  <tr>
                    <th>Código</th>
                    <th>Nombre</th>
                    <th>Ubicación</th>
                    <th>Teléfono</th>
                    <th>Responsable</th>
                    <th>Capacidad</th>
                    <th>Estado</th>
                    <th style="text-align: right;">Acciones</th>
                  </tr>
                </thead>
                <tbody>
                  ${list.map(s => `
                    <tr>
                      <td><span class="font-bold text-xs" style="color: var(--primary);">${escapeHtml(s.codigo || s.id)}</span></td>
                      <td class="font-semibold text-main">${escapeHtml(s.nombre)}</td>
                      <td>${escapeHtml(s.direccion)}, ${escapeHtml(s.ciudad)}</td>
                      <td>${escapeHtml(s.telefono || '-')}</td>
                      <td>${escapeHtml(s.responsable || 'No asignado')}</td>
                      <td><span class="font-bold text-primary">${escapeHtml(s.capacidad || 0)}</span></td>
                      <td>
                        <span class="badge ${s.activa ? 'badge-success' : 'badge-warning'}">
                          ${s.activa ? 'Activa' : 'Inactiva'}
                        </span>
                      </td>
                      <td>
                        <div class="table-actions justify-end">
                          <button class="btn-icon btn-edit-sucursal" data-id="${escapeHtml(s.id)}" title="Editar">
                            <svg class="icon-svg sm" viewBox="0 0 24 24"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path></svg>
                          </button>
                        </div>
                      </td>
                    </tr>
                  `).join('')}
                </tbody>
              </table>
            </div>
          </div>
        `;
      }

      // Eventos de botones editar / desactivar
      container.querySelectorAll('.btn-edit-sucursal').forEach(btn => {
        btn.addEventListener('click', () => {
          this.openModalEditar(btn.getAttribute('data-id'));
        });
      });

      container.querySelectorAll('.btn-disable-sucursal').forEach(btn => {
        btn.addEventListener('click', async () => {
          const id = btn.getAttribute('data-id');
          if (confirm('¿Desea marcar esta sucursal como inactiva?')) {
            try {
              await sucursalesService.eliminar(id);
              toast.info('Sucursal marcada como inactiva.');
              this.loadSucursales();
            } catch (err) {
              toast.error(err.message || 'Error al desactivar sucursal');
            }
          }
        });
      });

    } catch (err) {
      container.innerHTML = `<div class="text-center" style="padding: 2rem; color: var(--danger);">${escapeHtml(err.message || 'Error al cargar sucursales')}</div>`;
    }
  }

  openModalNuevo() {
    this.selectedSucursal = null;
    const form = this.container.querySelector('#form-sucursal');
    form.reset();
    this.container.querySelector('#sucursal-id').value = '';
    this.container.querySelector('#sucursal-activa').checked = true;
    this.container.querySelector('#modal-sucursal-title').textContent = 'Nueva Sucursal';
    const modal = this.container.querySelector('#modal-sucursal');
    modal.classList.add('is-active');
    setTimeout(() => this.container.querySelector('#sucursal-nombre').focus(), 100);
  }

  async openModalEditar(id) {
    try {
      const s = await sucursalesService.obtenerPorId(id);
      this.selectedSucursal = s;

      this.container.querySelector('#sucursal-id').value = s.id;
      this.container.querySelector('#sucursal-nombre').value = s.nombre || '';
      this.container.querySelector('#sucursal-codigo').value = s.codigo || '';
      this.container.querySelector('#sucursal-direccion').value = s.direccion || '';
      this.container.querySelector('#sucursal-ciudad').value = s.ciudad || '';
      this.container.querySelector('#sucursal-telefono').value = s.telefono || '';
      this.container.querySelector('#sucursal-capacidad').value = s.capacidad || 50;
      this.container.querySelector('#sucursal-responsable').value = s.responsable || '';
      this.container.querySelector('#sucursal-activa').checked = s.activa !== false;

      this.container.querySelector('#modal-sucursal-title').textContent = `Editar Sucursal: ${s.nombre}`;

      const modal = this.container.querySelector('#modal-sucursal');
      modal.classList.add('is-active');
      setTimeout(() => this.container.querySelector('#sucursal-nombre').focus(), 100);
    } catch (err) {
      toast.error('No se pudo cargar la sucursal.');
    }
  }

  closeModal() {
    const modal = this.container.querySelector('#modal-sucursal');
    modal.classList.remove('is-active');
  }

  async _handleSubmit(e) {
    e.preventDefault();

    const id = this.container.querySelector('#sucursal-id').value;
    const nombre = this.container.querySelector('#sucursal-nombre').value.trim();
    const codigo = this.container.querySelector('#sucursal-codigo').value.trim();
    const direccion = this.container.querySelector('#sucursal-direccion').value.trim();
    const ciudad = this.container.querySelector('#sucursal-ciudad').value.trim();
    const telefono = this.container.querySelector('#sucursal-telefono').value.trim();
    const capacidad = Number(this.container.querySelector('#sucursal-capacidad').value) || 0;
    const responsable = this.container.querySelector('#sucursal-responsable').value.trim();
    const activa = this.container.querySelector('#sucursal-activa').checked;

    if (!nombre || !direccion) {
      toast.warning('Por favor complete los campos obligatorios.');
      return;
    }

    const payload = {
      nombre,
      codigo,
      direccion,
      ciudad,
      telefono,
      capacidad,
      responsable,
      activa
    };

    const saveBtn = this.container.querySelector('#btn-save-sucursal');
    saveBtn.classList.add('is-loading');
    saveBtn.disabled = true;

    try {
      if (id) {
        await sucursalesService.actualizar(id, payload);
        toast.success(`Sucursal ${nombre} actualizada.`);
      } else {
        await sucursalesService.crear(payload);
        toast.success(`Sucursal ${nombre} registrada.`);
      }
      this.closeModal();
      this.loadSucursales();
    } catch (err) {
      toast.error(err.message || 'Error al guardar sucursal');
    } finally {
      saveBtn.classList.remove('is-loading');
      saveBtn.disabled = false;
    }
  }
}
