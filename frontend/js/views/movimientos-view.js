/**
 * SIGA - views/movimientos-view.js
 * Vista Funcional Sencilla de Movimientos de Stock de Bulbos
 */

import { movimientosService } from '../services/movimientos-service.js';
import { clientesService } from '../services/clientes-service.js';
import { toast } from '../utils/toast.js';
import { escapeHtml } from '../utils/escape-html.js';

export class MovimientosView {
  constructor(router) {
    this.router = router;
    this.container = null;
    this.clientesList = [];
    this.filterEstado = 'todos';
  }

  async render(rootElement) {
    this.container = rootElement;

    this.container.innerHTML = `
      <div class="fade-in">
        <!-- Encabezado simple -->
        <div class="d-flex justify-between align-center mb-4 flex-wrap gap-md">
          <div>
            <h1 style="font-size: 1.5rem; margin-bottom: 0.25rem;">Movimientos de Stock</h1>
            <p class="text-muted text-sm">Transferencias y movimientos de bulbos entre clientes y depósito central</p>
          </div>
          <div class="d-flex gap-sm">
            <button class="btn btn-secondary btn-sm" id="btn-refresh-movimientos">
              <span>Refrescar</span>
            </button>
            <button class="btn btn-primary" id="btn-nuevo-movimiento">
              <span>+ Nuevo Movimiento</span>
            </button>
          </div>
        </div>

        <!-- Filtro simple -->
        <div class="table-card mb-4" style="padding: 0.75rem 1.25rem;">
          <div class="d-flex align-center gap-md flex-wrap">
            <label class="text-xs text-muted font-semibold">Filtrar por Estado:</label>
            <select id="filter-mov-estado" class="form-control" style="width: 160px; padding: 0.4rem 0.75rem;">
              <option value="todos">Todos</option>
              <option value="Pendiente">Solo Pendientes</option>
              <option value="Confirmado">Solo Confirmados</option>
              <option value="Anulado">Solo Anulados</option>
            </select>
          </div>
        </div>

        <!-- Tabla simple de Movimientos -->
        <div class="table-card">
          <div class="table-responsive">
            <table class="table" id="movimientos-table">
              <thead>
                <tr>
                  <th>Comprobante</th>
                  <th>Fecha</th>
                  <th>Origen</th>
                  <th>Destino</th>
                  <th style="text-align: right;">Total Bulbos</th>
                  <th>Estado</th>
                  <th style="text-align: right;">Acciones</th>
                </tr>
              </thead>
              <tbody id="movimientos-table-body">
                <tr>
                  <td colspan="7" class="text-center text-muted" style="padding: 2rem;">Cargando movimientos...</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <!-- Modal Simple de Nuevo Movimiento -->
      <div id="modal-nuevo-mov" class="modal-backdrop" role="dialog" aria-modal="true">
        <div class="modal-dialog modal-lg">
          <div class="modal-header">
            <h3 class="modal-title">Registrar Movimiento de Bulbos</h3>
            <button type="button" class="modal-close-btn" id="btn-close-modal-mov">✕</button>
          </div>
          <form id="form-nuevo-mov" novalidate>
            <div class="modal-body">
              <!-- Origen y Destino -->
              <div class="form-row two-cols mb-3">
                <div class="form-group">
                  <label class="form-label" for="mov-origen">
                    Origen <span class="required-mark">*</span>
                  </label>
                  <select id="mov-origen" class="form-control" required>
                    <option value="empresa">🏢 Empresa (Depósito Central)</option>
                  </select>
                </div>

                <div class="form-group">
                  <label class="form-label" for="mov-destino">
                    Destino <span class="required-mark">*</span>
                  </label>
                  <select id="mov-destino" class="form-control" required>
                    <option value="">-- Seleccionar Destino --</option>
                    <option value="empresa">🏢 Empresa (Depósito Central)</option>
                  </select>
                </div>
              </div>

              <!-- Ingreso de Cantidades por Clase -->
              <div class="form-section mb-3">
                <label class="form-label mb-2">
                  <strong>Cantidades de Bulbos por Clase (0 a 4):</strong>
                </label>
                <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(130px, 1fr)); gap: 0.75rem;">
                  <div class="form-group">
                    <label class="text-xs font-semibold text-muted" for="cant-clase-0">Clase 0 (Extra Gde)</label>
                    <input type="number" id="cant-clase-0" class="form-control text-right" min="0" value="0" />
                  </div>
                  <div class="form-group">
                    <label class="text-xs font-semibold text-muted" for="cant-clase-1">Clase 1 (Grande)</label>
                    <input type="number" id="cant-clase-1" class="form-control text-right" min="0" value="0" />
                  </div>
                  <div class="form-group">
                    <label class="text-xs font-semibold text-muted" for="cant-clase-2">Clase 2 (Mediano)</label>
                    <input type="number" id="cant-clase-2" class="form-control text-right" min="0" value="0" />
                  </div>
                  <div class="form-group">
                    <label class="text-xs font-semibold text-muted" for="cant-clase-3">Clase 3 (Chico)</label>
                    <input type="number" id="cant-clase-3" class="form-control text-right" min="0" value="0" />
                  </div>
                  <div class="form-group">
                    <label class="text-xs font-semibold text-muted" for="cant-clase-4">Clase 4 (Semillero)</label>
                    <input type="number" id="cant-clase-4" class="form-control text-right" min="0" value="0" />
                  </div>
                </div>
                <div class="text-right mt-2 text-sm">
                  Total a transferir: <strong id="mov-total-preview" class="text-primary font-bold">0</strong> unidades
                </div>
              </div>

              <!-- Observaciones -->
              <div class="form-group">
                <label class="form-label" for="mov-obs">Observaciones / Motivo</label>
                <input type="text" id="mov-obs" class="form-control" placeholder="ej. Asignación inicial de bulbos para cultivo" />
              </div>
            </div>

            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" id="btn-cancel-mov">Cancelar</button>
              <button type="submit" class="btn btn-primary" id="btn-save-mov">
                <span>Guardar como Pendiente</span>
              </button>
            </div>
          </form>
        </div>
      </div>
    `;

    this._bindEvents();
    await this._loadClientes();
    await this._loadMovimientos();
  }

  _bindEvents() {
    // Refrescar
    this.container.querySelector('#btn-refresh-movimientos').addEventListener('click', () => {
      this._loadMovimientos();
    });

    // Filtro estado
    this.container.querySelector('#filter-mov-estado').addEventListener('change', (e) => {
      this.filterEstado = e.target.value;
      this._loadMovimientos();
    });

    // Modal nuevo
    this.container.querySelector('#btn-nuevo-movimiento').addEventListener('click', () => {
      this._openModalNuevo();
    });

    this.container.querySelector('#btn-close-modal-mov').addEventListener('click', () => this._closeModal());
    this.container.querySelector('#btn-cancel-mov').addEventListener('click', () => this._closeModal());

    // Cálculo dinámico de total en el modal
    const inputsCant = this.container.querySelectorAll('[id^="cant-clase-"]');
    inputsCant.forEach(inp => {
      inp.addEventListener('input', () => this._updateTotalPreview());
    });

    // Envío del formulario
    this.container.querySelector('#form-nuevo-mov').addEventListener('submit', (e) => this._handleSubmit(e));
  }

  _updateTotalPreview() {
    let sum = 0;
    for (let c = 0; c <= 4; c++) {
      const val = Number(this.container.querySelector(`#cant-clase-${c}`).value) || 0;
      sum += Math.max(0, val);
    }
    const previewEl = this.container.querySelector('#mov-total-preview');
    if (previewEl) previewEl.textContent = sum.toLocaleString('es-AR');
  }

  async _loadClientes() {
    try {
      const data = await clientesService.listar({ page: 1, pageSize: 100, estado: 'activos' });
      this.clientesList = data.items || [];

      const selectOrigen = this.container.querySelector('#mov-origen');
      const selectDestino = this.container.querySelector('#mov-destino');

      this.clientesList.forEach(c => {
        const text = `👤 ${c.nombre} ${c.apellido}`;
        selectOrigen.add(new Option(text, String(c.id)));
        selectDestino.add(new Option(text, String(c.id)));
      });
    } catch (err) {
      console.warn('[MovimientosView] Error al cargar clientes:', err);
    }
  }

  async _loadMovimientos() {
    const tbody = this.container.querySelector('#movimientos-table-body');
    tbody.innerHTML = `<tr><td colspan="7" class="text-center text-muted" style="padding: 1.5rem;">Cargando movimientos...</td></tr>`;

    try {
      const result = await movimientosService.listar({ estado: this.filterEstado });
      const items = result.items || [];

      if (items.length === 0) {
        tbody.innerHTML = `<tr><td colspan="7" class="text-center text-muted" style="padding: 2rem;">No hay movimientos registrados.</td></tr>`;
        return;
      }

      tbody.innerHTML = items.map(m => {
        const fechaStr = m.fecha ? new Date(m.fecha).toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' }) : '-';
        
        let badgeClass = 'badge-warning';
        if (m.estado === 'Confirmado') badgeClass = 'badge-success';
        if (m.estado === 'Anulado') badgeClass = 'badge-danger';

        const isPendiente = m.estado === 'Pendiente';

        return `
          <tr>
            <td><strong class="text-primary text-xs">${escapeHtml(m.numeroComprobante || 'MOV-' + m.id)}</strong></td>
            <td class="text-xs text-muted">${escapeHtml(fechaStr)}</td>
            <td class="font-medium text-main text-sm">${escapeHtml(m.origenNombre || m.origenId)}</td>
            <td class="font-medium text-main text-sm">${escapeHtml(m.destinoNombre || m.destinoId)}</td>
            <td style="text-align: right; font-weight: 700;">${(m.totalBulbos || 0).toLocaleString('es-AR')}</td>
            <td>
              <span class="badge ${badgeClass}">${escapeHtml(m.estado)}</span>
            </td>
            <td>
              <div class="table-actions justify-end">
                ${isPendiente ? `
                  <button class="btn btn-sm btn-outline btn-confirmar-mov" data-id="${escapeHtml(m.id)}" style="color: var(--success); border-color: var(--success-border);" title="Confirmar movimiento">
                    Confirmar
                  </button>
                  <button class="btn btn-sm btn-outline btn-anular-mov" data-id="${escapeHtml(m.id)}" style="color: var(--danger); border-color: var(--danger-border);" title="Anular movimiento">
                    Anular
                  </button>
                ` : `
                  <span class="text-xs text-muted">-</span>
                `}
              </div>
            </td>
          </tr>
        `;
      }).join('');

      // Eventos Confirmar / Anular
      tbody.querySelectorAll('.btn-confirmar-mov').forEach(btn => {
        btn.addEventListener('click', async () => {
          const id = btn.getAttribute('data-id');
          if (confirm('¿Confirmar este movimiento? Se actualizará el stock disponible.')) {
            try {
              await movimientosService.confirmar(id);
              toast.success('Movimiento confirmado exitosamente.');
              this._loadMovimientos();
            } catch (err) {
              toast.error(err.message || 'Error al confirmar movimiento');
            }
          }
        });
      });

      tbody.querySelectorAll('.btn-anular-mov').forEach(btn => {
        btn.addEventListener('click', async () => {
          const id = btn.getAttribute('data-id');
          const motivo = prompt('Ingrese el motivo de la anulación:') || 'Anulado por usuario';
          try {
            await movimientosService.anular(id, motivo);
            toast.info('Movimiento anulado.');
            this._loadMovimientos();
          } catch (err) {
            toast.error(err.message || 'Error al anular movimiento');
          }
        });
      });

    } catch (err) {
      tbody.innerHTML = `<tr><td colspan="7" class="text-center text-danger" style="padding: 1.5rem;">${escapeHtml(err.message || 'Error al cargar movimientos')}</td></tr>`;
      toast.error('No se pudo cargar el listado de movimientos.');
    }
  }

  _openModalNuevo() {
    const form = this.container.querySelector('#form-nuevo-mov');
    form.reset();
    this.container.querySelector('#mov-origen').value = 'empresa';
    this.container.querySelector('#mov-destino').value = '';
    this._updateTotalPreview();

    const modal = this.container.querySelector('#modal-nuevo-mov');
    modal.classList.add('is-active');
  }

  _closeModal() {
    const modal = this.container.querySelector('#modal-nuevo-mov');
    modal.classList.remove('is-active');
  }

  async _handleSubmit(e) {
    e.preventDefault();

    const origenId = this.container.querySelector('#mov-origen').value;
    const destinoId = this.container.querySelector('#mov-destino').value;
    const observaciones = this.container.querySelector('#mov-obs').value.trim();

    if (!origenId || !destinoId) {
      toast.warning('Debe seleccionar el Origen y el Destino.');
      return;
    }

    if (origenId === destinoId) {
      toast.warning('El Origen y el Destino no pueden ser el mismo.');
      return;
    }

    const cantidades = [];
    let total = 0;
    for (let c = 0; c <= 4; c++) {
      const val = Number(this.container.querySelector(`#cant-clase-${c}`).value) || 0;
      if (val > 0) {
        cantidades.push({ claseBulbo: c, cantidad: val });
        total += val;
      }
    }

    if (total <= 0) {
      toast.warning('Debe ingresar una cantidad mayor a cero en al menos una clase.');
      return;
    }

    // Resolver nombres para visualización
    const origenNombre = origenId === 'empresa'
      ? 'Empresa (Depósito Central)'
      : (this.clientesList.find(c => String(c.id) === String(origenId))?.nombre + ' ' + this.clientesList.find(c => String(c.id) === String(origenId))?.apellido || `Cliente #${origenId}`);

    const destinoNombre = destinoId === 'empresa'
      ? 'Empresa (Depósito Central)'
      : (this.clientesList.find(c => String(c.id) === String(destinoId))?.nombre + ' ' + this.clientesList.find(c => String(c.id) === String(destinoId))?.apellido || `Cliente #${destinoId}`);

    const payload = {
      origenId,
      origenNombre,
      destinoId,
      destinoNombre,
      observaciones,
      cantidades
    };

    const saveBtn = this.container.querySelector('#btn-save-mov');
    saveBtn.disabled = true;

    try {
      await movimientosService.crear(payload);
      toast.success('Movimiento registrado como Pendiente.');
      this._closeModal();
      this._loadMovimientos();
    } catch (err) {
      toast.error(err.message || 'Error al registrar movimiento');
    } finally {
      saveBtn.disabled = false;
    }
  }
}
