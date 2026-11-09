/**
 * SIGA - views/clientes-view.js
 * Gestión Integral de Clientes: Listado, Paginación, Búsqueda, Modales de CRUD y Baja Lógica
 */

import { clientesService } from '../services/clientes-service.js';
import { toast } from '../utils/toast.js';
import { CONFIG } from '../config.js';

export class ClientesView {
  constructor(router) {
    this.router = router;
    this.container = null;
    this.currentPage = 1;
    this.pageSize = CONFIG.PAGINATION.DEFAULT_PAGE_SIZE;
    this.searchTerm = '';
    this.filterEstado = 'todos';
    this.debounceTimer = null;
    this.selectedCliente = null;
  }

  async render(rootElement, queryParams = {}) {
    this.container = rootElement;

    // Si viene con parámetro ?action=new abriremos el modal luego de renderizar
    const autoOpenNew = queryParams && queryParams.action === 'new';

    this.container.innerHTML = `
      <div class="fade-in">
        <!-- Encabezado de Sección -->
        <div class="d-flex justify-between align-center mb-4 flex-wrap gap-md">
          <div>
            <h1 style="font-size: 1.5rem; margin-bottom: 0.25rem;">Gestión de Clientes</h1>
            <p class="text-muted text-sm">Administración de legajos de clientes, personas y cuentas</p>
          </div>
          <div class="d-flex gap-sm">
            <button class="btn btn-secondary" id="btn-refresh-clientes" title="Actualizar listado">
              <svg class="icon-svg sm" viewBox="0 0 24 24">
                <polyline points="23 4 23 10 17 10"></polyline>
                <polyline points="1 20 1 14 7 14"></polyline>
                <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"></path>
              </svg>
              <span>Refrescar</span>
            </button>
            <button class="btn btn-primary" id="btn-new-cliente">
              <svg class="icon-svg sm" viewBox="0 0 24 24">
                <line x1="12" y1="5" x2="12" y2="19"></line>
                <line x1="5" y1="12" x2="19" y2="12"></line>
              </svg>
              <span>Nuevo Cliente</span>
            </button>
          </div>
        </div>

        <!-- Tarjeta de Contenido / Tabla -->
        <div class="table-card">
          <!-- Barra de herramientas (Búsqueda y Filtros) -->
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
                  id="search-input"
                  class="form-control has-icon-left"
                  placeholder="Buscar por nombre, documento, código o email..."
                  value="${this.searchTerm}"
                />
              </div>
            </div>

            <div class="toolbar-actions">
              <label class="text-xs text-muted font-semibold">Estado:</label>
              <select id="filter-estado" class="form-control" style="width: 140px; padding: 0.45rem 2rem 0.45rem 0.75rem;">
                <option value="todos" ${this.filterEstado === 'todos' ? 'selected' : ''}>Todos</option>
                <option value="activos" ${this.filterEstado === 'activos' ? 'selected' : ''}>Solo Activos</option>
                <option value="inactivos" ${this.filterEstado === 'inactivos' ? 'selected' : ''}>Solo Inactivos</option>
              </select>

              <label class="text-xs text-muted font-semibold ml-2">Mostrar:</label>
              <select id="select-page-size" class="form-control" style="width: 90px; padding: 0.45rem 1.8rem 0.45rem 0.75rem;">
                ${CONFIG.PAGINATION.PAGE_SIZE_OPTIONS.map(size => `
                  <option value="${size}" ${this.pageSize === size ? 'selected' : ''}>${size} filas</option>
                `).join('')}
              </select>
            </div>
          </div>

          <!-- Tabla de Datos -->
          <div class="table-responsive">
            <table class="table" id="clientes-table">
              <thead>
                <tr>
                  <th>Código</th>
                  <th>Cliente (Persona)</th>
                  <th>Identificación</th>
                  <th>Contacto</th>
                  <th>Estado</th>
                  <th style="text-align: right;">Acciones</th>
                </tr>
              </thead>
              <tbody id="clientes-table-body">
                <tr class="table-loading-row">
                  <td colspan="6">
                    <div class="d-flex align-center justify-center gap-sm">
                      <div class="btn-spinner" style="position: static; border-top-color: var(--primary);"></div>
                      <span>Cargando clientes...</span>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- Paginación -->
          <div class="table-pagination" id="clientes-pagination">
            <div class="pagination-info" id="pagination-info">Mostrando 0 de 0 registros</div>
            <div class="pagination-controls" id="pagination-controls"></div>
          </div>
        </div>
      </div>

      <!-- Modal de Creación / Edición de Cliente -->
      <div id="modal-cliente" class="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="modal-cliente-title">
        <div class="modal-dialog modal-lg">
          <div class="modal-header">
            <h3 class="modal-title" id="modal-cliente-title">Nuevo Cliente</h3>
            <button type="button" class="modal-close-btn" id="btn-close-modal-cliente" aria-label="Cerrar modal">
              <svg class="icon-svg sm" viewBox="0 0 24 24"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
            </button>
          </div>
          <form id="form-cliente" novalidate>
            <div class="modal-body">
              <input type="hidden" id="cliente-id" />

              <!-- Sección 1: Datos de Persona -->
              <div class="form-section">
                <div class="form-section-title">
                  <svg class="icon-svg sm" viewBox="0 0 24 24"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path><circle cx="12" cy="7" r="4"></circle></svg>
                  <span>1. Datos de la Persona</span>
                </div>

                <div class="form-row two-cols">
                  <div class="form-group">
                    <label for="cliente-nombre" class="form-label">
                      Nombre <span class="required-mark">*</span>
                    </label>
                    <input type="text" id="cliente-nombre" class="form-control" placeholder="ej. Martín" required />
                    <div class="invalid-feedback" id="val-nombre">El nombre es obligatorio.</div>
                  </div>

                  <div class="form-group">
                    <label for="cliente-apellido" class="form-label">
                      Apellido <span class="required-mark">*</span>
                    </label>
                    <input type="text" id="cliente-apellido" class="form-control" placeholder="ej. Gómez" required />
                    <div class="invalid-feedback" id="val-apellido">El apellido es obligatorio.</div>
                  </div>
                </div>

                <div class="form-row two-cols">
                  <div class="form-group">
                    <label for="cliente-tipo-doc" class="form-label">Tipo Documento</label>
                    <select id="cliente-tipo-doc" class="form-control">
                      <option value="DNI">DNI - Documento Nacional</option>
                      <option value="CUIT">CUIT / CUIL</option>
                      <option value="PASAPORTE">Pasaporte</option>
                    </select>
                  </div>

                  <div class="form-group">
                    <label for="cliente-doc" class="form-label">
                      Número de Documento <span class="required-mark">*</span>
                    </label>
                    <input type="text" id="cliente-doc" class="form-control" placeholder="ej. 32456789" required />
                    <div class="invalid-feedback" id="val-doc">El número de documento es obligatorio.</div>
                  </div>
                </div>

                <div class="form-row two-cols">
                  <div class="form-group">
                    <label for="cliente-email" class="form-label">
                      Correo Electrónico <span class="required-mark">*</span>
                    </label>
                    <input type="email" id="cliente-email" class="form-control" placeholder="ej. cliente@correo.com" required />
                    <div class="invalid-feedback" id="val-email">Ingrese un correo electrónico válido.</div>
                  </div>

                  <div class="form-group">
                    <label for="cliente-telefono" class="form-label">Teléfono / WhatsApp</label>
                    <input type="text" id="cliente-telefono" class="form-control" placeholder="ej. +54 351 456-7890" />
                  </div>
                </div>
              </div>

              <!-- Sección 2: Datos Comerciales de Cliente -->
              <div>
                <div class="form-section-title">
                  <svg class="icon-svg sm" viewBox="0 0 24 24"><rect x="2" y="7" width="20" height="14" rx="2" ry="2"></rect><path d="M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16"></path></svg>
                  <span>2. Información Comercial</span>
                </div>

                <div class="form-row two-cols">
                  <div class="form-group">
                    <label for="cliente-codigo" class="form-label">
                      Código de Cliente
                      <span class="label-hint">(Autogenerado si está vacío)</span>
                    </label>
                    <input type="text" id="cliente-codigo" class="form-control" placeholder="ej. CLI-00101" />
                  </div>

                  <div class="form-group d-flex justify-center" style="margin-top: 1.5rem;">
                    <label class="form-switch">
                      <input type="checkbox" id="cliente-activo" class="switch-input" checked />
                      <span class="switch-slider"></span>
                      <span class="form-check-label font-semibold">Cliente Habilitado / Activo</span>
                    </label>
                  </div>
                </div>

                <div class="form-group">
                  <label for="cliente-observaciones" class="form-label">Observaciones y Notas</label>
                  <textarea id="cliente-observaciones" class="form-control" rows="3" placeholder="Información crediticia, preferencias o datos de facturación..."></textarea>
                </div>
              </div>
            </div>

            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" id="btn-cancel-modal-cliente">Cancelar</button>
              <button type="submit" class="btn btn-primary" id="btn-save-cliente">
                <span class="btn-text">Guardar Cliente</span>
                <span class="btn-spinner"></span>
              </button>
            </div>
          </form>
        </div>
      </div>

      <!-- Modal de Confirmación de Baja Lógica (Soft Delete) -->
      <div id="modal-delete-cliente" class="modal-backdrop" role="dialog" aria-modal="true">
        <div class="modal-dialog modal-sm">
          <div class="modal-header">
            <h3 class="modal-title" style="color: var(--danger);">Baja de Cliente</h3>
            <button type="button" class="modal-close-btn" id="btn-close-delete-modal">
              <svg class="icon-svg sm" viewBox="0 0 24 24"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
            </button>
          </div>
          <div class="modal-body text-center">
            <div style="width: 54px; height: 54px; border-radius: 50%; background: var(--danger-bg); color: var(--danger); display: flex; align-items: center; justify-content: center; margin: 0 auto 1rem;">
              <svg class="icon-svg lg" viewBox="0 0 24 24">
                <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"></path>
                <line x1="12" y1="9" x2="12" y2="13"></line>
                <line x1="12" y1="17" x2="12.01" y2="17"></line>
              </svg>
            </div>
            <h4 class="mb-2">¿Dar de baja a este cliente?</h4>
            <p class="text-sm text-muted mb-3" id="delete-cliente-prompt">
              Se realizará la baja lógica del registro. Podrá reactivarlo en cualquier momento desde el filtro de inactivos.
            </p>
          </div>
          <div class="modal-footer justify-center">
            <button type="button" class="btn btn-secondary" id="btn-cancel-delete">Cancelar</button>
            <button type="button" class="btn btn-danger" id="btn-confirm-delete">
              <span>Confirmar Baja</span>
            </button>
          </div>
        </div>
      </div>
    `;

    this._bindEvents();
    await this.loadClientes();

    if (autoOpenNew) {
      this.openModalNuevo();
    }
  }

  _bindEvents() {
    // Buscador con debounce (300ms)
    const searchInput = this.container.querySelector('#search-input');
    searchInput.addEventListener('input', (e) => {
      clearTimeout(this.debounceTimer);
      this.debounceTimer = setTimeout(() => {
        this.searchTerm = e.target.value;
        this.currentPage = 1;
        this.loadClientes();
      }, 300);
    });

    // Filtro por Estado
    const filterSelect = this.container.querySelector('#filter-estado');
    filterSelect.addEventListener('change', (e) => {
      this.filterEstado = e.target.value;
      this.currentPage = 1;
      this.loadClientes();
    });

    // Selector de tamaño de página
    const sizeSelect = this.container.querySelector('#select-page-size');
    sizeSelect.addEventListener('change', (e) => {
      this.pageSize = Number(e.target.value);
      this.currentPage = 1;
      this.loadClientes();
    });

    // Botón refrescar
    this.container.querySelector('#btn-refresh-clientes').addEventListener('click', () => {
      this.loadClientes();
    });

    // Botón nuevo cliente
    this.container.querySelector('#btn-new-cliente').addEventListener('click', () => {
      this.openModalNuevo();
    });

    // Cierre de modal edición
    this.container.querySelector('#btn-close-modal-cliente').addEventListener('click', () => this.closeModalCliente());
    this.container.querySelector('#btn-cancel-modal-cliente').addEventListener('click', () => this.closeModalCliente());

    // Cierre de modal delete
    this.container.querySelector('#btn-close-delete-modal').addEventListener('click', () => this.closeModalDelete());
    this.container.querySelector('#btn-cancel-delete').addEventListener('click', () => this.closeModalDelete());

    // Confirmación de baja
    this.container.querySelector('#btn-confirm-delete').addEventListener('click', () => this._handleConfirmDelete());

    // Submit del formulario
    this.container.querySelector('#form-cliente').addEventListener('submit', (e) => this._handleSubmitCliente(e));
  }

  async loadClientes() {
    const tbody = this.container.querySelector('#clientes-table-body');
    const paginationInfo = this.container.querySelector('#pagination-info');
    const paginationControls = this.container.querySelector('#pagination-controls');

    tbody.innerHTML = `
      <tr class="table-loading-row">
        <td colspan="6">
          <div class="d-flex align-center justify-center gap-sm">
            <span class="spinner-icon">⏳</span>
            <span>Consultando registros...</span>
          </div>
        </td>
      </tr>
    `;

    try {
      const result = await clientesService.listar({
        page: this.currentPage,
        pageSize: this.pageSize,
        search: this.searchTerm,
        estado: this.filterEstado
      });

      const items = result.items || [];
      const total = result.total || items.length;
      const totalPages = result.totalPages || Math.ceil(total / this.pageSize) || 1;

      // Render cuerpo de tabla
      if (items.length === 0) {
        tbody.innerHTML = `
          <tr>
            <td colspan="6">
              <div class="table-empty-state">
                <div class="empty-state-icon">
                  <svg class="icon-svg lg" viewBox="0 0 24 24"><circle cx="12" cy="12" r="10"></circle><line x1="8" y1="12" x2="16" y2="12"></line></svg>
                </div>
                <h4 class="empty-state-title">No se encontraron clientes</h4>
                <p class="empty-state-description">No hay registros que coincidan con los criterios de búsqueda o filtros aplicados.</p>
                <button class="btn btn-outline-primary btn-sm" id="btn-empty-reset">Limpiar Filtros</button>
              </div>
            </td>
          </tr>
        `;
        const resetBtn = tbody.querySelector('#btn-empty-reset');
        if (resetBtn) {
          resetBtn.addEventListener('click', () => {
            this.searchTerm = '';
            this.filterEstado = 'todos';
            this.container.querySelector('#search-input').value = '';
            this.container.querySelector('#filter-estado').value = 'todos';
            this.loadClientes();
          });
        }
      } else {
        tbody.innerHTML = items.map(cliente => `
          <tr>
            <td>
              <span class="font-bold text-xs" style="color: var(--primary); background: var(--primary-100); padding: 3px 8px; border-radius: var(--radius-sm);">
                ${cliente.codigo || `CLI-${cliente.id}`}
              </span>
            </td>
            <td>
              <div class="font-semibold text-main">${cliente.nombre} ${cliente.apellido}</div>
              <small class="text-muted text-truncate" style="max-width: 200px; display: block;">${cliente.observaciones || 'Sin notas'}</small>
            </td>
            <td>
              <div class="text-sm font-medium">${cliente.tipoDocumento || 'DNI'}</div>
              <div class="text-xs text-muted">${cliente.documento}</div>
            </td>
            <td>
              <div class="text-sm text-main">${cliente.email}</div>
              <div class="text-xs text-muted">${cliente.telefono || 'Sin teléfono'}</div>
            </td>
            <td>
              <span class="badge ${cliente.activo ? 'badge-success' : 'badge-danger'}">
                <span class="badge-dot"></span>
                ${cliente.activo ? 'Activo' : 'Inactivo'}
              </span>
            </td>
            <td>
              <div class="table-actions justify-end">
                <button class="btn-icon btn-action-edit" data-id="${cliente.id}" title="Editar cliente">
                  <svg class="icon-svg sm" viewBox="0 0 24 24">
                    <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path>
                    <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path>
                  </svg>
                </button>
                ${cliente.activo ? `
                  <button class="btn-icon btn-action-delete" data-id="${cliente.id}" data-name="${cliente.nombre} ${cliente.apellido}" title="Dar de baja (soft delete)">
                    <svg class="icon-svg sm" viewBox="0 0 24 24" style="color: var(--danger);">
                      <polyline points="3 6 5 6 21 6"></polyline>
                      <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
                    </svg>
                  </button>
                ` : `
                  <button class="btn-icon btn-action-reactivate" data-id="${cliente.id}" title="Reactivar cliente">
                    <svg class="icon-svg sm" viewBox="0 0 24 24" style="color: var(--success);">
                      <polyline points="23 4 23 10 17 10"></polyline>
                      <path d="M20.49 15a9 9 0 1 1-2.12-9.36L23 10"></path>
                    </svg>
                  </button>
                `}
              </div>
            </td>
          </tr>
        `).join('');

        // Conectar botones de acciones
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
              await clientesService.reactivar(id);
              toast.success('Cliente reactivado correctamente.');
              this.loadClientes();
            } catch (err) {
              toast.error(err.message || 'Error al reactivar cliente');
            }
          });
        });
      }

      // Render Paginación
      const startItem = total === 0 ? 0 : (this.currentPage - 1) * this.pageSize + 1;
      const endItem = Math.min(this.currentPage * this.pageSize, total);
      paginationInfo.textContent = `Mostrando ${startItem} a ${endItem} de ${total} clientes`;

      this._renderPaginationControls(paginationControls, totalPages);

    } catch (err) {
      tbody.innerHTML = `
        <tr>
          <td colspan="6" class="text-center" style="padding: 2rem; color: var(--danger);">
            ${err.message || 'Error al cargar clientes.'}
          </td>
        </tr>
      `;
      toast.error('No se pudo cargar el listado de clientes.');
    }
  }

  _renderPaginationControls(container, totalPages) {
    if (totalPages <= 1) {
      container.innerHTML = '';
      return;
    }

    let html = `
      <button class="btn-page" id="page-prev" ${this.currentPage === 1 ? 'disabled' : ''}>
        ‹
      </button>
    `;

    for (let p = 1; p <= totalPages; p++) {
      if (p === 1 || p === totalPages || (p >= this.currentPage - 1 && p <= this.currentPage + 1)) {
        html += `
          <button class="btn-page ${p === this.currentPage ? 'active' : ''}" data-page="${p}">
            ${p}
          </button>
        `;
      } else if (p === this.currentPage - 2 || p === this.currentPage + 2) {
        html += `<span style="padding: 0 4px; color: var(--text-light);">...</span>`;
      }
    }

    html += `
      <button class="btn-page" id="page-next" ${this.currentPage === totalPages ? 'disabled' : ''}>
        ›
      </button>
    `;

    container.innerHTML = html;

    const prevBtn = container.querySelector('#page-prev');
    if (prevBtn) {
      prevBtn.addEventListener('click', () => {
        if (this.currentPage > 1) {
          this.currentPage--;
          this.loadClientes();
        }
      });
    }

    const nextBtn = container.querySelector('#page-next');
    if (nextBtn) {
      nextBtn.addEventListener('click', () => {
        if (this.currentPage < totalPages) {
          this.currentPage++;
          this.loadClientes();
        }
      });
    }

    container.querySelectorAll('.btn-page[data-page]').forEach(btn => {
      btn.addEventListener('click', () => {
        this.currentPage = Number(btn.getAttribute('data-page'));
        this.loadClientes();
      });
    });
  }

  openModalNuevo() {
    this.selectedCliente = null;
    const form = this.container.querySelector('#form-cliente');
    form.reset();
    this.container.querySelector('#cliente-id').value = '';
    this.container.querySelector('#modal-cliente-title').textContent = 'Nuevo Cliente';
    this.container.querySelector('#cliente-activo').checked = true;
    this._clearValidationErrors();

    const modal = this.container.querySelector('#modal-cliente');
    modal.classList.add('is-active');
    setTimeout(() => this.container.querySelector('#cliente-nombre').focus(), 100);
  }

  async openModalEditar(id) {
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
      this.container.querySelector('#cliente-codigo').value = cliente.codigo || '';
      this.container.querySelector('#cliente-activo').checked = cliente.activo !== false;
      this.container.querySelector('#cliente-observaciones').value = cliente.observaciones || '';

      this.container.querySelector('#modal-cliente-title').textContent = `Editar Cliente: ${cliente.nombre} ${cliente.apellido}`;

      const modal = this.container.querySelector('#modal-cliente');
      modal.classList.add('is-active');
      setTimeout(() => this.container.querySelector('#cliente-nombre').focus(), 100);
    } catch (err) {
      toast.error('No se pudo cargar el cliente para editar.');
    }
  }

  closeModalCliente() {
    const modal = this.container.querySelector('#modal-cliente');
    modal.classList.remove('is-active');
  }

  openModalDelete(id, name) {
    this.selectedDeleteId = id;
    const prompt = this.container.querySelector('#delete-cliente-prompt');
    prompt.innerHTML = `Está a punto de dar de baja lógica al cliente <strong>${name}</strong>. Esta acción conservará el historial contable pero deshabilitará su actividad.`;
    const modal = this.container.querySelector('#modal-delete-cliente');
    modal.classList.add('is-active');
  }

  closeModalDelete() {
    const modal = this.container.querySelector('#modal-delete-cliente');
    modal.classList.remove('is-active');
    this.selectedDeleteId = null;
  }

  async _handleConfirmDelete() {
    if (!this.selectedDeleteId) return;
    try {
      await clientesService.eliminar(this.selectedDeleteId);
      toast.success('Baja lógica aplicada con éxito.', 'Cliente Deshabilitado');
      this.closeModalDelete();
      this.loadClientes();
    } catch (err) {
      toast.error(err.message || 'Error al dar de baja al cliente');
    }
  }

  _clearValidationErrors() {
    const inputs = this.container.querySelectorAll('#form-cliente .form-control');
    inputs.forEach(i => i.classList.remove('is-invalid'));
  }

  async _handleSubmitCliente(e) {
    e.preventDefault();

    const id = this.container.querySelector('#cliente-id').value;
    const nombre = this.container.querySelector('#cliente-nombre').value.trim();
    const apellido = this.container.querySelector('#cliente-apellido').value.trim();
    const tipoDocumento = this.container.querySelector('#cliente-tipo-doc').value;
    const documento = this.container.querySelector('#cliente-doc').value.trim();
    const email = this.container.querySelector('#cliente-email').value.trim();
    const telefono = this.container.querySelector('#cliente-telefono').value.trim();
    const codigo = this.container.querySelector('#cliente-codigo').value.trim();
    const activo = this.container.querySelector('#cliente-activo').checked;
    const observaciones = this.container.querySelector('#cliente-observaciones').value.trim();

    // Validaciones
    let valid = true;
    this._clearValidationErrors();

    if (!nombre) {
      this.container.querySelector('#cliente-nombre').classList.add('is-invalid');
      valid = false;
    }
    if (!apellido) {
      this.container.querySelector('#cliente-apellido').classList.add('is-invalid');
      valid = false;
    }
    if (!documento) {
      this.container.querySelector('#cliente-doc').classList.add('is-invalid');
      valid = false;
    }
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!email || !emailRegex.test(email)) {
      this.container.querySelector('#cliente-email').classList.add('is-invalid');
      valid = false;
    }

    if (!valid) {
      toast.warning('Por favor complete los campos obligatorios correctamente.');
      return;
    }

    const payload = {
      nombre,
      apellido,
      tipoDocumento,
      documento,
      email,
      telefono,
      codigo,
      activo,
      observaciones
    };

    const saveBtn = this.container.querySelector('#btn-save-cliente');
    saveBtn.classList.add('is-loading');
    saveBtn.disabled = true;

    try {
      if (id) {
        await clientesService.actualizar(id, payload);
        toast.success(`Cliente ${nombre} ${apellido} actualizado exitosamente.`);
      } else {
        await clientesService.crear(payload);
        toast.success(`Cliente ${nombre} ${apellido} registrado exitosamente.`);
      }

      this.closeModalCliente();
      this.loadClientes();
    } catch (err) {
      toast.error(err.message || 'Error al guardar el cliente');
    } finally {
      saveBtn.classList.remove('is-loading');
      saveBtn.disabled = false;
    }
  }
}
