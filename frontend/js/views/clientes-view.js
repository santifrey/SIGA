/**
 * SIGA - views/clientes-view.js
 * Gestión Integral de Clientes: Listado, Paginación, Búsqueda, Modales de CRUD y Baja Lógica
 */

import { clientesService } from '../services/clientes-service.js';
import { ClienteModal } from '../components/cliente-modal.js';
import { ConfirmDeleteModal } from '../components/confirm-delete-modal.js';
import { renderPagination } from '../components/pagination.js';
import { loadTemplate } from '../utils/template-loader.js';
import { toast } from '../utils/toast.js';
import { CONFIG } from '../config.js';
import { escapeHtml } from '../utils/escape-html.js';
import { getProhibitionIcon } from '../utils/icons.js';

export class ClientesView {
  constructor(router) {
    this.router = router;
    this.container = null;
    this.currentPage = 1;
    this.pageSize = CONFIG.PAGINATION.DEFAULT_PAGE_SIZE;
    this.searchTerm = '';
    this.filterEstado = 'todos';
    this.debounceTimer = null;
    this.clienteModal = null;
    this.deleteModal = null;
  }

  async render(rootElement, queryParams = {}) {
    this.container = rootElement;
    const autoOpenNew = queryParams && queryParams.action === 'new';

    this.container.innerHTML = await loadTemplate('clientes.html');

    // Inicializar valores de filtros y paginación en la plantilla
    const searchInput = this.container.querySelector('#search-input');
    if (searchInput) searchInput.value = this.searchTerm;

    const filterSelect = this.container.querySelector('#filter-estado');
    if (filterSelect) filterSelect.value = this.filterEstado;

    const sizeSelect = this.container.querySelector('#select-page-size');
    if (sizeSelect) {
      sizeSelect.innerHTML = CONFIG.PAGINATION.PAGE_SIZE_OPTIONS.map(size => `
        <option value="${size}" ${this.pageSize === size ? 'selected' : ''}>${size} filas</option>
      `).join('');
    }

    // Inicializar componente reutilizable de Modal de Cliente
    this.clienteModal = new ClienteModal({
      container: this.container,
      onSaved: async () => {
        await this.loadClientes();
      }
    });
    await this.clienteModal.init();

    // Inicializar componente reutilizable de Confirmación de Baja
    this.deleteModal = new ConfirmDeleteModal({
      container: this.container,
      title: 'Baja de Cliente',
      confirmText: 'Confirmar Baja',
      hintText: 'Se realizará la baja lógica del registro. Podrá reactivarlo en cualquier momento desde el filtro de inactivos.',
      onConfirm: async (id) => {
        await clientesService.eliminar(id);
        toast.success('Baja lógica aplicada con éxito.', 'Cliente Deshabilitado');
        await this.loadClientes();
      }
    });
    await this.deleteModal.init();

    this._bindEvents();
    await this.loadClientes();

    if (autoOpenNew) {
      this.clienteModal.openNuevo();
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
      this.clienteModal.openNuevo();
    });
  }

  async loadClientes() {
    const tbody = this.container.querySelector('#clientes-table-body');
    const paginationInfo = this.container.querySelector('#pagination-info');
    const paginationControls = this.container.querySelector('#pagination-controls');

    tbody.innerHTML = `
      <tr class="table-loading-row">
        <td colspan="8">
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

      if (items.length === 0) {
        tbody.innerHTML = `
          <tr>
            <td colspan="8">
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
                ${escapeHtml(cliente.codigo || `CLI-${cliente.id}`)}
              </span>
            </td>
            <td>
              <div class="font-semibold text-main">${escapeHtml(cliente.nombre)} ${escapeHtml(cliente.apellido)}</div>
              <small class="text-muted text-truncate" style="max-width: 200px; display: block;">${escapeHtml(cliente.observaciones || 'Sin notas')}</small>
            </td>
            <td>
              <div class="text-sm font-medium">${escapeHtml(cliente.tipoDocumento || 'DNI')}</div>
              <div class="text-xs text-muted">${escapeHtml(cliente.documento)}</div>
            </td>
            <td>
              <div class="text-sm text-main">${escapeHtml(cliente.email)}</div>
              <div class="text-xs text-muted">${escapeHtml(cliente.telefono || 'Sin teléfono')}</div>
            </td>
            <td>
              ${cliente.sucursalNombre ? `
                <span class="badge badge-secondary text-xs font-semibold">
                  🏢 ${escapeHtml(cliente.sucursalNombre)}
                </span>
              ` : `
                <span class="text-xs text-muted">-</span>
              `}
            </td>
            <td>
              ${cliente.numeroContrato ? `
                <span class="badge badge-primary font-mono text-xs" title="Contrato Asociado: #${escapeHtml(cliente.contratoId || '')} - ${escapeHtml(cliente.numeroContrato)}">
                  ${escapeHtml(cliente.numeroContrato)}
                </span>
              ` : `
                <span class="text-muted text-xs">Sin Contrato</span>
              `}
            </td>
            <td>
              <span class="badge ${cliente.activo ? 'badge-success' : 'badge-danger'}">
                <span class="badge-dot"></span>
                ${cliente.activo ? 'Activo' : 'Inactivo'}
              </span>
            </td>
            <td>
              <div class="table-actions justify-end">
                <button class="btn-icon btn-action-edit" data-id="${escapeHtml(cliente.id)}" title="Editar cliente">
                  <svg class="icon-svg sm" viewBox="0 0 24 24">
                    <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path>
                    <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path>
                  </svg>
                </button>
                ${cliente.activo ? `
                  <button class="btn-icon btn-action-delete" data-id="${escapeHtml(cliente.id)}" data-name="${escapeHtml(cliente.nombre)} ${escapeHtml(cliente.apellido)}" title="Dar de baja (soft delete)">
                    ${getProhibitionIcon({ size: 'sm', title: 'Dar de baja (soft delete)' })}
                  </button>
                ` : `
                  <button class="btn-icon btn-action-reactivate" data-id="${escapeHtml(cliente.id)}" title="Reactivar cliente">
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

        tbody.querySelectorAll('.btn-action-edit').forEach(btn => {
          btn.addEventListener('click', () => {
            const id = btn.getAttribute('data-id');
            this.clienteModal.openEditar(id);
          });
        });

        tbody.querySelectorAll('.btn-action-delete').forEach(btn => {
          btn.addEventListener('click', () => {
            const id = btn.getAttribute('data-id');
            const name = btn.getAttribute('data-name');
            this.deleteModal.open({
              id,
              promptHtml: `Está a punto de dar de baja lógica al cliente <strong>${escapeHtml(name)}</strong>. Esta acción conservará el historial contable pero deshabilitará su actividad.`
            });
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

      renderPagination({
        infoEl: paginationInfo,
        controlsEl: paginationControls,
        currentPage: this.currentPage,
        pageSize: this.pageSize,
        totalItems: total,
        totalPages,
        entityLabel: 'clientes',
        onPageChange: (newPage) => {
          this.currentPage = newPage;
          this.loadClientes();
        }
      });

    } catch (err) {
      tbody.innerHTML = `
        <tr>
          <td colspan="7" class="text-center" style="padding: 2rem; color: var(--danger);">
            ${escapeHtml(err.message || 'Error al cargar clientes.')}
          </td>
        </tr>
      `;
      toast.error('No se pudo cargar el listado de clientes.');
    }
  }
}
