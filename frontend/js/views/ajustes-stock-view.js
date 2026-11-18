/**
 * SIGA - views/ajustes-stock-view.js
 * Vista Funcional de Ajustes de Stock de Bulbos
 * Permite registrar cuando se quitan o añaden bulbos a un cliente o a la empresa
 * con motivo obligatorio registrado en la observación del ajuste para auditoría.
 */

import { movimientosService } from '../services/movimientos-service.js';
import { clientesService } from '../services/clientes-service.js';
import { toast } from '../utils/toast.js';
import { escapeHtml } from '../utils/escape-html.js';

export class AjustesStockView {
  constructor(router) {
    this.router = router;
    this.container = null;
    this.clientesList = [];
    this.filterEstado = 'todos';
    this.filterEntidad = 'todos';
    this.ajustesCache = [];
  }

  async render(rootElement) {
    this.container = rootElement;

    this.container.innerHTML = `
      <div class="fade-in">
        <!-- Encabezado -->
        <div class="d-flex justify-between align-center mb-4 flex-wrap gap-md">
          <div>
            <h1 style="font-size: 1.5rem; margin-bottom: 0.25rem;">Ajustes de Stock</h1>
            <p class="text-muted text-sm">Registro de ingresos y egresos manuales de bulbos para clientes o la empresa con motivo obligatorio</p>
          </div>
          <div class="d-flex gap-sm">
            <button class="btn btn-secondary btn-sm" id="btn-refresh-ajustes" title="Actualizar lista">
              <svg class="icon-svg sm" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <polyline points="23 4 23 10 17 10"></polyline>
                <polyline points="1 20 1 14 7 14"></polyline>
                <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"></path>
              </svg>
              <span>Refrescar</span>
            </button>
            <button class="btn btn-primary" id="btn-nuevo-ajuste">
              <svg class="icon-svg sm" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="12" y1="5" x2="12" y2="19"></line>
                <line x1="5" y1="12" x2="19" y2="12"></line>
              </svg>
              <span>Nuevo Ajuste de Stock</span>
            </button>
          </div>
        </div>

        <!-- Filtros Rápidos -->
        <div class="table-card mb-4" style="padding: 0.85rem 1.25rem;">
          <div class="d-flex align-center gap-lg flex-wrap">
            <div class="d-flex align-center gap-sm">
              <label class="text-xs text-muted font-bold text-uppercase">Estado:</label>
              <select id="filter-ajuste-estado" class="form-control" style="width: 170px; padding: 0.4rem 0.75rem;">
                <option value="todos">Todos los Estados</option>
                <option value="Pendiente">Solo Pendientes</option>
                <option value="Confirmado">Solo Confirmados</option>
                <option value="Anulado">Solo Anulados</option>
              </select>
            </div>
            <div class="d-flex align-center gap-sm">
              <label class="text-xs text-muted font-bold text-uppercase">Titular:</label>
              <select id="filter-ajuste-entidad" class="form-control" style="width: 200px; padding: 0.4rem 0.75rem;">
                <option value="todos">Todos los Titulares</option>
                <option value="empresa">🏢 Solo Empresa Central</option>
                <option value="clientes">👤 Solo Clientes</option>
              </select>
            </div>
          </div>
        </div>

        <!-- Tabla de Ajustes de Stock -->
        <div class="table-card">
          <div class="table-responsive">
            <table class="table" id="ajustes-table">
              <thead>
                <tr>
                  <th style="width: 130px;">Comprobante</th>
                  <th style="width: 140px;">Fecha</th>
                  <th style="width: 110px;">Sentido</th>
                  <th>Titular Afectado</th>
                  <th style="text-align: right; width: 110px;">Total Bulbos</th>
                  <th>Motivo / Observación del Ajuste</th>
                  <th style="width: 120px;">Estado</th>
                  <th style="text-align: right; width: 150px;">Acciones</th>
                </tr>
              </thead>
              <tbody id="ajustes-table-body">
                <tr>
                  <td colspan="8" class="text-center text-muted" style="padding: 2.5rem;">Cargando ajustes de stock...</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- Modal de Nuevo Ajuste de Stock -->
        <div id="modal-nuevo-ajuste" class="modal-backdrop" role="dialog" aria-modal="true">
          <div class="modal-dialog modal-lg" style="max-width: 680px;">
            <div class="modal-header">
              <div>
                <h3 class="modal-title font-bold" style="color: var(--primary);">Registrar Ajuste de Stock de Bulbos</h3>
                <p class="text-xs text-muted mt-1" style="margin-bottom: 0;">Seleccione si añade o quita bulbos a la empresa o a un cliente y detalle el motivo obligatorio.</p>
              </div>
              <button type="button" class="modal-close-btn" id="btn-close-modal-ajuste" aria-label="Cerrar modal">✕</button>
            </div>
            <form id="form-nuevo-ajuste" novalidate>
              <div class="modal-body" style="padding: 1.5rem;">

                <!-- 1. Selección de Titular / Entidad -->
                <div class="form-section mb-4" style="background: var(--surface-alt); padding: 1rem; border-radius: var(--radius-md); border: 1px solid var(--border);">
                  <label class="form-label font-bold mb-2 text-main">
                    1. ¿A quién se le aplica el ajuste? <span class="text-danger">*</span>
                  </label>
                  <div class="d-flex gap-lg mb-2 flex-wrap">
                    <label class="d-flex align-center gap-xs font-medium cursor-pointer" style="cursor: pointer;">
                      <input type="radio" name="ajuste-entidad-tipo" value="empresa" id="radio-entidad-empresa" checked />
                      <span>🏢 Empresa (Depósito Central)</span>
                    </label>
                    <label class="d-flex align-center gap-xs font-medium cursor-pointer" style="cursor: pointer;">
                      <input type="radio" name="ajuste-entidad-tipo" value="cliente" id="radio-entidad-cliente" />
                      <span>👤 Cliente de la empresa</span>
                    </label>
                  </div>

                  <div id="box-cliente-select" class="hidden mt-2">
                    <label class="form-label font-semibold text-xs" for="ajuste-cliente-id">Seleccionar Cliente:</label>
                    <select id="ajuste-cliente-id" class="form-control">
                      <option value="">-- Elija un cliente --</option>
                    </select>
                  </div>
                </div>

                <!-- 2. Acción / Sentido del Ajuste -->
                <div class="form-section mb-4" style="background: var(--surface-alt); padding: 1rem; border-radius: var(--radius-md); border: 1px solid var(--border);">
                  <label class="form-label font-bold mb-2 text-main">
                    2. ¿Qué acción realiza el ajuste? <span class="text-danger">*</span>
                  </label>
                  <div class="d-flex gap-lg flex-wrap">
                    <label class="d-flex align-center gap-xs font-semibold cursor-pointer text-success" style="cursor: pointer;">
                      <input type="radio" name="ajuste-accion" value="sumar" checked />
                      <span>➕ Añadir bulbos (Ingreso de Stock)</span>
                    </label>
                    <label class="d-flex align-center gap-xs font-semibold cursor-pointer text-danger" style="cursor: pointer;">
                      <input type="radio" name="ajuste-accion" value="restar" />
                      <span>➖ Quitar bulbos (Egreso / Merma / Baja de Stock)</span>
                    </label>
                  </div>
                </div>

                <!-- 3. Cantidades por Clase (0 a 4) -->
                <div class="form-section mb-4">
                  <label class="form-label font-bold mb-2 text-main">
                    3. Cantidades de Bulbos por Clase (0 a 4): <span class="text-danger">*</span>
                  </label>
                  <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(115px, 1fr)); gap: 0.65rem;">
                    <div class="form-group mb-0">
                      <label class="text-xs font-semibold text-muted" for="ajuste-cant-0">Clase 0 (Extra Gde)</label>
                      <input type="number" id="ajuste-cant-0" class="form-control text-right" min="0" value="0" />
                    </div>
                    <div class="form-group mb-0">
                      <label class="text-xs font-semibold text-muted" for="ajuste-cant-1">Clase 1 (Grande)</label>
                      <input type="number" id="ajuste-cant-1" class="form-control text-right" min="0" value="0" />
                    </div>
                    <div class="form-group mb-0">
                      <label class="text-xs font-semibold text-muted" for="ajuste-cant-2">Clase 2 (Mediano)</label>
                      <input type="number" id="ajuste-cant-2" class="form-control text-right" min="0" value="0" />
                    </div>
                    <div class="form-group mb-0">
                      <label class="text-xs font-semibold text-muted" for="ajuste-cant-3">Clase 3 (Chico)</label>
                      <input type="number" id="ajuste-cant-3" class="form-control text-right" min="0" value="0" />
                    </div>
                    <div class="form-group mb-0">
                      <label class="text-xs font-semibold text-muted" for="ajuste-cant-4">Clase 4 (Semillero)</label>
                      <input type="number" id="ajuste-cant-4" class="form-control text-right" min="0" value="0" />
                    </div>
                  </div>
                  <div class="d-flex justify-between align-center mt-2" style="background: var(--surface-hover); padding: 0.5rem 0.75rem; border-radius: var(--radius-sm);">
                    <span class="text-xs text-muted">Total de unidades del ajuste:</span>
                    <span class="font-bold text-primary" style="font-size: 1.1rem;"><span id="ajuste-total-preview">0</span> bulbos</span>
                  </div>
                </div>

                <!-- 4. Motivo / Observación del Ajuste (Obligatorio) -->
                <div class="form-group mb-3">
                  <label class="form-label font-bold text-main" for="ajuste-motivo">
                    4. Motivo / Observación del Ajuste <span class="text-danger">*</span>
                  </label>
                  <textarea id="ajuste-motivo" class="form-control" rows="3" required placeholder="Especifique el motivo por el cual se quitan o añaden bulbos (ej: Merma por clasificación en depósito, Ajuste de recuento físico de inventario, Corrección de saldo inicial, etc.)"></textarea>
                  <small class="text-muted text-xs">Este motivo quedará permanentemente asentado en la observación del ajuste para fines de trazabilidad y auditoría.</small>
                </div>

                <div id="ajuste-form-error" class="alert alert-danger hidden mb-2" style="font-size: 0.8125rem;"></div>
              </div>

              <div class="modal-footer d-flex justify-between">
                <button type="button" class="btn btn-outline" id="btn-cancel-ajuste">Cancelar</button>
                <div class="d-flex gap-sm">
                  <button type="button" class="btn btn-secondary" id="btn-save-pendiente">
                    <span>Guardar como Pendiente</span>
                  </button>
                  <button type="submit" class="btn btn-primary" id="btn-save-confirmar">
                    <span>Guardar y Confirmar</span>
                  </button>
                </div>
              </div>
            </form>
          </div>
        </div>

        <!-- Modal de Detalle de Ajuste -->
        <div id="modal-detalle-ajuste" class="modal-backdrop" role="dialog" aria-modal="true">
          <div class="modal-dialog" style="max-width: 520px;">
            <div class="modal-header">
              <h3 class="modal-title font-bold text-primary" id="detalle-titulo">Detalle de Ajuste de Stock</h3>
              <button type="button" class="modal-close-btn" id="btn-close-modal-detalle">✕</button>
            </div>
            <div class="modal-body" id="detalle-cuerpo" style="padding: 1.5rem;">
              <!-- Cargado dinámicamente -->
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-outline" id="btn-cerrar-detalle">Cerrar</button>
            </div>
          </div>
        </div>

      </div>
    `;

    this._bindEvents();
    await this._loadClientes();
    await this._loadAjustes();
  }

  _bindEvents() {
    this.container.querySelector('#btn-refresh-ajustes').addEventListener('click', () => {
      this._loadAjustes();
    });

    this.container.querySelector('#filter-ajuste-estado').addEventListener('change', (e) => {
      this.filterEstado = e.target.value;
      this._renderTable();
    });

    this.container.querySelector('#filter-ajuste-entidad').addEventListener('change', (e) => {
      this.filterEntidad = e.target.value;
      this._renderTable();
    });

    this.container.querySelector('#btn-nuevo-ajuste').addEventListener('click', () => {
      this._openModalNuevo();
    });

    this.container.querySelector('#btn-close-modal-ajuste').addEventListener('click', () => this._closeModal());
    this.container.querySelector('#btn-cancel-ajuste').addEventListener('click', () => this._closeModal());

    this.container.querySelector('#btn-close-modal-detalle').addEventListener('click', () => {
      this.container.querySelector('#modal-detalle-ajuste').classList.remove('is-active');
    });
    this.container.querySelector('#btn-cerrar-detalle').addEventListener('click', () => {
      this.container.querySelector('#modal-detalle-ajuste').classList.remove('is-active');
    });

    const radioEmpresa = this.container.querySelector('#radio-entidad-empresa');
    const radioCliente = this.container.querySelector('#radio-entidad-cliente');
    const boxClienteSelect = this.container.querySelector('#box-cliente-select');

    radioEmpresa.addEventListener('change', () => {
      if (radioEmpresa.checked) boxClienteSelect.classList.add('hidden');
    });
    radioCliente.addEventListener('change', () => {
      if (radioCliente.checked) boxClienteSelect.classList.remove('hidden');
    });

    const inputsCant = this.container.querySelectorAll('[id^="ajuste-cant-"]');
    inputsCant.forEach(inp => {
      inp.addEventListener('input', () => this._updateTotalPreview());
    });

    this.container.querySelector('#btn-save-pendiente').addEventListener('click', () => {
      this._handleAjusteSubmit(false);
    });

    this.container.querySelector('#form-nuevo-ajuste').addEventListener('submit', (e) => {
      e.preventDefault();
      this._handleAjusteSubmit(true);
    });
  }

  _updateTotalPreview() {
    let sum = 0;
    for (let c = 0; c <= 4; c++) {
      const val = Number(this.container.querySelector(`#ajuste-cant-${c}`).value) || 0;
      sum += Math.max(0, val);
    }
    const previewEl = this.container.querySelector('#ajuste-total-preview');
    if (previewEl) previewEl.textContent = sum.toLocaleString('es-AR');
  }

  async _loadClientes() {
    try {
      const data = await clientesService.listar({ page: 1, pageSize: 150, estado: 'activos' });
      this.clientesList = data.items || [];

      const selectCliente = this.container.querySelector('#ajuste-cliente-id');
      selectCliente.innerHTML = '<option value="">-- Elija un cliente --</option>';

      this.clientesList.forEach(c => {
        const text = `👤 ${c.nombre} ${c.apellido} (${c.codigoCliente || 'DNI ' + (c.documento || c.id)})`;
        selectCliente.add(new Option(text, String(c.id)));
      });
    } catch (err) {
      console.warn('[AjustesStockView] No se pudo cargar clientes:', err);
    }
  }

  async _loadAjustes() {
    const tbody = this.container.querySelector('#ajustes-table-body');
    tbody.innerHTML = `<tr><td colspan="8" class="text-center text-muted" style="padding: 2rem;">Cargando ajustes de stock...</td></tr>`;

    try {
      const result = await movimientosService.listar({ estado: 'todos' });
      // Filtrar únicamente los movimientos que son ajustes manuales de stock
      this.ajustesCache = (result.items || []).filter(m => {
        const tipo = String(m.tipoOperacion || '').toLowerCase();
        const obs = (m.observaciones || '').toLowerCase();
        return tipo.includes('ajuste') ||
          obs.includes('[ingreso') ||
          obs.includes('[egreso') ||
          obs.includes('[ajuste') ||
          m.origenId === 0 ||
          m.destinoId === 0 ||
          (!m.movimientoComercialId && !m.cosechaId && !m.plantacionId);
      });
      this._renderTable();
    } catch (err) {
      tbody.innerHTML = `<tr><td colspan="8" class="text-center text-danger" style="padding: 2rem;">${escapeHtml(err.message || 'Error al cargar ajustes')}</td></tr>`;
      toast.error('No se pudo cargar el listado de ajustes de stock.');
    }
  }

  _renderTable() {
    const tbody = this.container.querySelector('#ajustes-table-body');
    let items = [...this.ajustesCache];

    // Filtro por estado
    if (this.filterEstado !== 'todos') {
      items = items.filter(m => String(m.estado).toLowerCase() === this.filterEstado.toLowerCase());
    }

    // Filtro por entidad
    if (this.filterEntidad === 'empresa') {
      items = items.filter(m => {
        const obs = (m.observaciones || '').toLowerCase();
        const orig = (m.origenNombre || '').toLowerCase();
        const dest = (m.destinoNombre || '').toLowerCase();
        return obs.includes('empresa') || (orig.includes('empresa') && dest.includes('ajuste')) || (dest.includes('empresa') && orig.includes('ajuste'));
      });
    } else if (this.filterEntidad === 'clientes') {
      items = items.filter(m => {
        const obs = (m.observaciones || '').toLowerCase();
        const orig = (m.origenNombre || '').toLowerCase();
        const dest = (m.destinoNombre || '').toLowerCase();
        return obs.includes('cliente') || (!orig.includes('ajuste') && !dest.includes('ajuste') && (orig.includes('cliente') || dest.includes('cliente')));
      });
    }

    if (items.length === 0) {
      tbody.innerHTML = `<tr><td colspan="8" class="text-center text-muted" style="padding: 2.5rem;">No hay ajustes de stock que coincidan con los filtros seleccionados.</td></tr>`;
      return;
    }

    tbody.innerHTML = items.map(m => {
      const fechaStr = m.fecha ? new Date(m.fecha).toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' }) : '-';

      const obs = m.observaciones || '';
      let esIngreso = false;
      let titularTexto = '';

      if (obs.includes('[Ingreso a Empresa]') || obs.includes('[Ajuste de Stock Empresa (+)]')) {
        esIngreso = true;
        titularTexto = '🏢 Empresa (Depósito Central)';
      } else if (obs.includes('[Egreso de Empresa]') || obs.includes('[Ajuste de Stock Empresa (-)]')) {
        esIngreso = false;
        titularTexto = '🏢 Empresa (Depósito Central)';
      } else if (obs.includes('[Ingreso a Cliente]')) {
        esIngreso = true;
        titularTexto = `👤 ${escapeHtml(m.destinoNombre || `Cliente #${m.destinoId}`)}`;
      } else if (obs.includes('[Egreso de Cliente]')) {
        esIngreso = false;
        titularTexto = `👤 ${escapeHtml(m.origenNombre || `Cliente #${m.origenId}`)}`;
      } else {
        if (m.origenId === 1 || String(m.origenId) === 'empresa' || (m.origenNombre || '').includes('Ajuste')) {
          esIngreso = true;
          titularTexto = m.destinoId === 1 ? '🏢 Empresa Central' : `👤 ${escapeHtml(m.destinoNombre || `Cliente #${m.destinoId}`)}`;
        } else {
          esIngreso = false;
          titularTexto = `👤 ${escapeHtml(m.origenNombre || `Cliente #${m.origenId}`)}`;
        }
      }

      let motivoLimpio = obs
        .replace(/\[Ingreso a Empresa\]\s*/i, '')
        .replace(/\[Egreso de Empresa\]\s*/i, '')
        .replace(/\[Ingreso a Cliente\]\s*/i, '')
        .replace(/\[Egreso de Cliente\]\s*/i, '')
        .replace(/\[Ajuste de Stock Empresa \([+-]\)\]\s*/i, '')
        .trim();

      if (!motivoLimpio) motivoLimpio = 'Ajuste general de stock';

      let badgeEstado = 'badge-warning';
      if (m.estado === 'Confirmado') badgeEstado = 'badge-success';
      if (m.estado === 'Anulado') badgeEstado = 'badge-danger';

      const isPendiente = m.estado === 'Pendiente';

      return `
        <tr>
          <td><strong class="text-primary text-xs">${escapeHtml(m.numeroComprobante || 'AJU-' + m.id)}</strong></td>
          <td class="text-xs text-muted">${escapeHtml(fechaStr)}</td>
          <td>
            <span class="badge ${esIngreso ? 'badge-success' : 'badge-danger'}" style="font-weight: 700;">
              ${esIngreso ? '+ Ingreso' : '- Egreso'}
            </span>
          </td>
          <td class="font-medium text-main text-sm">${titularTexto}</td>
          <td style="text-align: right; font-weight: 800; color: var(--primary);">
            ${(m.totalBulbos || 0).toLocaleString('es-AR')}
          </td>
          <td class="text-sm" style="max-width: 280px; word-break: break-word;">
            <span title="${escapeHtml(obs)}" style="color: var(--text-main); font-weight: 500;">
              ${escapeHtml(motivoLimpio)}
            </span>
          </td>
          <td>
            <span class="badge ${badgeEstado}">${escapeHtml(m.estado)}</span>
          </td>
          <td>
            <div class="table-actions justify-end gap-xs">
              <button class="btn btn-xs btn-outline btn-ver-detalle" data-id="${escapeHtml(m.id)}" title="Ver detalle de clases">
                Detalles
              </button>
              ${isPendiente ? `
                <button class="btn btn-xs btn-outline btn-confirmar-ajuste" data-id="${escapeHtml(m.id)}" style="color: var(--success); border-color: var(--success);" title="Confirmar e impactar stock">
                  ✓
                </button>
                <button class="btn btn-xs btn-outline btn-anular-ajuste" data-id="${escapeHtml(m.id)}" style="color: var(--danger); border-color: var(--danger);" title="Anular ajuste">
                  ✕
                </button>
              ` : ''}
            </div>
          </td>
        </tr>
      `;
    }).join('');

    tbody.querySelectorAll('.btn-confirmar-ajuste').forEach(btn => {
      btn.addEventListener('click', async () => {
        const id = btn.getAttribute('data-id');
        if (confirm('¿Confirmar este ajuste de stock? Se impactará de inmediato el saldo disponible.')) {
          try {
            await movimientosService.confirmar(id);
            toast.success('Ajuste de stock confirmado e impactado con éxito.');
            await this._loadAjustes();
          } catch (err) {
            toast.error(err.message || 'Error al confirmar ajuste');
          }
        }
      });
    });

    tbody.querySelectorAll('.btn-anular-ajuste').forEach(btn => {
      btn.addEventListener('click', async () => {
        const id = btn.getAttribute('data-id');
        const motivo = prompt('Ingrese el motivo de anulación:') || 'Anulado por usuario';
        try {
          await movimientosService.anular(id, motivo);
          toast.info('Ajuste de stock anulado.');
          await this._loadAjustes();
        } catch (err) {
          toast.error(err.message || 'Error al anular ajuste');
        }
      });
    });

    tbody.querySelectorAll('.btn-ver-detalle').forEach(btn => {
      btn.addEventListener('click', () => {
        const id = btn.getAttribute('data-id');
        this._verDetalleAjuste(id);
      });
    });
  }

  _verDetalleAjuste(id) {
    const mov = this.ajustesCache.find(m => String(m.id) === String(id));
    if (!mov) return;

    const modal = this.container.querySelector('#modal-detalle-ajuste');
    const cuerpo = this.container.querySelector('#detalle-cuerpo');
    const titulo = this.container.querySelector('#detalle-titulo');

    titulo.textContent = `Ajuste ${mov.numeroComprobante || 'AJU-' + mov.id}`;

    const fechaStr = mov.fecha ? new Date(mov.fecha).toLocaleString('es-AR') : '-';
    const cantidadesHtml = (mov.cantidades || []).map(c => `
      <div class="d-flex justify-between py-1 border-b" style="border-bottom: 1px dashed var(--border); font-size: 0.875rem;">
        <span>${escapeHtml(c.nombreClase || `Clase ${c.claseBulbo ?? c.clase}`)}:</span>
        <strong class="text-primary">${Number(c.cantidad).toLocaleString('es-AR')} unidades</strong>
      </div>
    `).join('') || '<div class="text-muted text-xs">Sin detalle de clases</div>';

    cuerpo.innerHTML = `
      <div class="mb-3">
        <label class="text-xs text-muted font-bold text-uppercase">Fecha y Hora:</label>
        <div class="font-medium text-main">${escapeHtml(fechaStr)}</div>
      </div>
      <div class="mb-3">
        <label class="text-xs text-muted font-bold text-uppercase">Origen y Destino:</label>
        <div class="font-medium text-main">${escapeHtml(mov.origenNombre || mov.origenId)} ➔ ${escapeHtml(mov.destinoNombre || mov.destinoId)}</div>
      </div>
      <div class="mb-3">
        <label class="text-xs text-muted font-bold text-uppercase">Desglose por Calibre:</label>
        <div class="mt-1" style="background: var(--surface-alt); padding: 0.75rem; border-radius: var(--radius-sm);">
          ${cantidadesHtml}
          <div class="d-flex justify-between pt-2 mt-1" style="border-top: 1px solid var(--border); font-size: 0.95rem;">
            <strong>Total Ajustado:</strong>
            <strong class="text-primary font-bold">${(mov.totalBulbos || 0).toLocaleString('es-AR')} bulbos</strong>
          </div>
        </div>
      </div>
      <div class="mb-3">
        <label class="text-xs text-muted font-bold text-uppercase">Motivo / Observación Registrada:</label>
        <div class="alert alert-info mt-1" style="font-size: 0.875rem; padding: 0.75rem;">
          ${escapeHtml(mov.observaciones || 'Sin observación asentada')}
        </div>
      </div>
      <div>
        <label class="text-xs text-muted font-bold text-uppercase">Estado del Ajuste:</label>
        <div class="mt-1">
          <span class="badge ${mov.estado === 'Confirmado' ? 'badge-success' : (mov.estado === 'Anulado' ? 'badge-danger' : 'badge-warning')}">
            ${escapeHtml(mov.estado)}
          </span>
        </div>
      </div>
    `;

    modal.classList.add('is-active');
  }

  _openModalNuevo() {
    const form = this.container.querySelector('#form-nuevo-ajuste');
    form.reset();
    this.container.querySelector('#radio-entidad-empresa').checked = true;
    this.container.querySelector('#box-cliente-select').classList.add('hidden');
    this.container.querySelector('#ajuste-form-error').classList.add('hidden');
    this.container.querySelector('#ajuste-form-error').textContent = '';
    this._updateTotalPreview();

    const modal = this.container.querySelector('#modal-nuevo-ajuste');
    modal.classList.add('is-active');
  }

  _closeModal() {
    const modal = this.container.querySelector('#modal-nuevo-ajuste');
    modal.classList.remove('is-active');
  }

  async _handleAjusteSubmit(autoConfirmar) {
    const errBox = this.container.querySelector('#ajuste-form-error');
    errBox.classList.add('hidden');
    errBox.textContent = '';

    const entidadTipo = this.container.querySelector('input[name="ajuste-entidad-tipo"]:checked')?.value || 'empresa';
    const accion = this.container.querySelector('input[name="ajuste-accion"]:checked')?.value || 'sumar';
    const clienteId = this.container.querySelector('#ajuste-cliente-id').value;
    const motivo = this.container.querySelector('#ajuste-motivo').value.trim();

    if (entidadTipo === 'cliente' && !clienteId) {
      errBox.textContent = 'Debe seleccionar el cliente para realizar el ajuste de stock.';
      errBox.classList.remove('hidden');
      return;
    }

    if (!motivo) {
      errBox.textContent = 'Debe ingresar el motivo / observación obligatoria del ajuste.';
      errBox.classList.remove('hidden');
      return;
    }

    const cantidades = [];
    let total = 0;
    for (let c = 0; c <= 4; c++) {
      const val = Number(this.container.querySelector(`#ajuste-cant-${c}`).value) || 0;
      if (val > 0) {
        cantidades.push({ clase: c, cantidad: val });
        total += val;
      }
    }

    if (total <= 0) {
      errBox.textContent = 'Debe ingresar una cantidad mayor a cero en al menos una clase de bulbo.';
      errBox.classList.remove('hidden');
      return;
    }

    const btnSubmit = this.container.querySelector(autoConfirmar ? '#btn-save-confirmar' : '#btn-save-pendiente');
    btnSubmit.disabled = true;

    try {
      await movimientosService.registrarAjuste({
        entidad: entidadTipo,
        clienteId: entidadTipo === 'cliente' ? clienteId : null,
        accion,
        cantidades,
        motivo,
        autoConfirmar
      });

      toast.success(autoConfirmar
        ? 'Ajuste de stock registrado y confirmado exitosamente.'
        : 'Ajuste de stock registrado en estado Pendiente.');

      this._closeModal();
      await this._loadAjustes();
    } catch (err) {
      const msg = err.message || 'Error al registrar el ajuste de stock.';
      errBox.textContent = msg;
      errBox.classList.remove('hidden');
      toast.error(msg);
    } finally {
      btnSubmit.disabled = false;
    }
  }
}

