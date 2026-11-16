/**
 * SIGA - views/movimientos-comerciales-view.js
 * Vista Unificada de Movimientos Comerciales (#movimientos-comerciales) (Feature 34)
 * - VentaACliente (Tipo 1)
 * - CompraACliente (Tipo 2)
 * - TraspasoEntreClientes (Tipo 3)
 * - Circuito formal de estados (0..4), descomposición MIX (1/3 Cal 1, 2, 3), contratos, facturación y stock.
 */

import { authService } from '../services/auth-service.js';
import { temporadasService } from '../services/temporadas-service.js';
import { clientesService } from '../services/clientes-service.js';
import { stockService, EMPRESA_SINGLETON_ID } from '../services/stock-service.js';
import { preciosReferenciaService } from '../services/precios-referencia-service.js';
import {
  movimientosComercialesService,
  TIPOS_MOVIMIENTO,
  ESTADOS_MOVIMIENTO,
  CLASES_BULBO,
  ACCIONES_APROBACION,
  _parseClaseIndex as parseClaseIndexService
} from '../services/movimientos-comerciales-service.js';
import { getProhibitionIcon } from '../utils/icons.js';
import { toast } from '../utils/toast.js';
import { escapeHtml } from '../utils/escape-html.js';

/**
 * Extrae el índice numérico (0..5) de la clase de bulbo de manera robusta.
 * Maneja números (0..5), strings con dígitos ('Clase0'..'Clase4') y 'MIX' (5).
 */
function _parseClaseIndex(val) {
  if (val === undefined || val === null) return 0;
  if (typeof val === 'number') return isNaN(val) ? 0 : val;
  const s = String(val).trim().toUpperCase();
  if (s === 'MIX' || s === '5') return 5;
  const match = s.match(/\d+/);
  return match ? parseInt(match[0], 10) : 0;
}

export class MovimientosComercialesView {
  constructor(router) {
    this.router = router;
    this.container = null;
    this.movimientos = [];
    this.temporadas = [];
    this.clientes = [];
    this.preciosTemporada = {};
    this.activeTemporadaId = null;

    // Filtros activos
    this.filtro = {
      tipo: 'todos',
      estado: 'todos',
      clienteId: 'todos',
      temporadaId: 'todos',
      fechaDesde: '',
      fechaHasta: '',
      search: ''
    };

    // Estado del modal de creación / edición
    this.currentEditMovimiento = null;
    this.selectedTipo = 1; // 1: Venta, 2: Compra, 3: Traspaso
    this.origenStockData = null;
    this.clienteNuevoStatus = { esNuevo: false, requiereContrato: false };

    // Movimiento activo en modal de detalle / acción
    this.activeMovimiento = null;
  }

  async render(rootElement) {
    this.container = rootElement;

    this.container.innerHTML = `
      <div class="fade-in">
        <!-- Encabezado de la Vista -->
        <div class="d-flex justify-between align-center mb-4 flex-wrap gap-md">
          <div>
            <h1 style="font-size: 1.5rem; margin-bottom: 0.25rem;">Movimientos Comerciales</h1>
            <p class="text-muted text-sm">Gestión unificada de ventas, compras a clientes y traspasos de bulbos con circuito formal de aprobación y entrega</p>
          </div>
          <div class="d-flex align-center gap-sm">
            <button class="btn btn-secondary btn-sm" id="btn-refresh-movimientos" title="Actualizar datos">
              <svg class="icon-svg sm" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="margin-right: 0.25rem;">
                <polyline points="23 4 23 10 17 10"></polyline>
                <polyline points="1 20 1 14 7 14"></polyline>
                <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"></path>
              </svg>
              <span>Actualizar</span>
            </button>
            ${authService.tienePermiso('MOVCOM_CREAR') ? `
              <button class="btn btn-primary btn-sm" id="btn-nuevo-movimiento">
                <svg class="icon-svg sm" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="margin-right: 0.25rem;">
                  <line x1="12" y1="5" x2="12" y2="19"></line>
                  <line x1="5" y1="12" x2="19" y2="12"></line>
                </svg>
                <span>Nuevo Movimiento Comercial</span>
              </button>
            ` : ''}
          </div>
        </div>

        <!-- ===================================================================
             KPI COUNTERS CON FILTRADO RÁPIDO
             =================================================================== -->
        <div class="grid grid-cols-2 md:grid-cols-6 gap-sm mb-4" id="kpi-counters-container">
          <div class="card metric-card p-3 cursor-pointer" data-kpi-estado="todos" style="cursor: pointer; transition: transform var(--transition-fast);">
            <div class="metric-label">Total Registrados</div>
            <div class="metric-value" id="kpi-total" style="font-size: 1.6rem;">0</div>
            <div class="text-xs text-muted">Todos los estados</div>
          </div>
          <div class="card metric-card warning p-3 cursor-pointer" data-kpi-estado="0" style="cursor: pointer; border-left: 4px solid var(--warning);">
            <div class="metric-label" style="color: var(--warning);">Pendientes</div>
            <div class="metric-value" id="kpi-pendientes" style="font-size: 1.6rem; color: var(--warning);">0</div>
            <div class="text-xs text-muted">En revisión</div>
          </div>
          <div class="card metric-card info p-3 cursor-pointer" data-kpi-estado="1" style="cursor: pointer; border-left: 4px solid var(--info);">
            <div class="metric-label" style="color: var(--info);">Aprobados</div>
            <div class="metric-value" id="kpi-aprobados" style="font-size: 1.6rem; color: var(--info);">0</div>
            <div class="text-xs text-muted">Aprobación Admin</div>
          </div>
          <div class="card metric-card primary p-3 cursor-pointer" data-kpi-estado="2" style="cursor: pointer; border-left: 4px solid var(--primary);">
            <div class="metric-label" style="color: var(--primary);">Facturados</div>
            <div class="metric-value" id="kpi-facturados" style="font-size: 1.6rem; color: var(--primary);">0</div>
            <div class="text-xs text-muted">Con comprobante</div>
          </div>
          <div class="card metric-card success p-3 cursor-pointer" data-kpi-estado="3" style="cursor: pointer; border-left: 4px solid var(--success);">
            <div class="metric-label" style="color: var(--success);">Finalizados</div>
            <div class="metric-value" id="kpi-finalizados" style="font-size: 1.6rem; color: var(--success);">0</div>
            <div class="text-xs text-muted">Stock impactado</div>
          </div>
          <div class="card metric-card danger p-3 cursor-pointer" data-kpi-estado="4" style="cursor: pointer; border-left: 4px solid var(--danger);">
            <div class="metric-label" style="color: var(--danger);">Rechazados</div>
            <div class="metric-value" id="kpi-rechazados" style="font-size: 1.6rem; color: var(--danger);">0</div>
            <div class="text-xs text-muted">Observados</div>
          </div>
        </div>

        <!-- ===================================================================
             BARRA DE FILTROS AVANZADOS
             =================================================================== -->
        <div class="card mb-4" style="padding: 1.25rem;">
          <div class="grid grid-cols-1 md:grid-cols-4 lg:grid-cols-7 gap-sm align-center">
            <div>
              <label for="filtro-tipo" class="form-label text-xs font-bold uppercase text-muted mb-1">Tipo:</label>
              <select id="filtro-tipo" class="form-control form-control-sm">
                <option value="todos">Todos los Tipos</option>
                <option value="1">Venta a Cliente</option>
                <option value="2">Compra a Cliente</option>
                <option value="3">Traspaso entre Clientes</option>
              </select>
            </div>
            <div>
              <label for="filtro-estado" class="form-label text-xs font-bold uppercase text-muted mb-1">Estado:</label>
              <select id="filtro-estado" class="form-control form-control-sm">
                <option value="todos">Todos los Estados</option>
                <option value="0">Pendiente Revisión</option>
                <option value="1">Aprobado Admin</option>
                <option value="2">Facturado</option>
                <option value="3">Finalizado</option>
                <option value="4">Rechazado</option>
              </select>
            </div>
            <div>
              <label for="filtro-cliente" class="form-label text-xs font-bold uppercase text-muted mb-1">Cliente:</label>
              <select id="filtro-cliente" class="form-control form-control-sm">
                <option value="todos">Todos los Clientes</option>
              </select>
            </div>
            <div>
              <label for="filtro-temporada" class="form-label text-xs font-bold uppercase text-muted mb-1">Temporada:</label>
              <select id="filtro-temporada" class="form-control form-control-sm">
                <option value="todos">Todas las Temporadas</option>
              </select>
            </div>
            <div>
              <label for="filtro-fecha-desde" class="form-label text-xs font-bold uppercase text-muted mb-1">Desde:</label>
              <input type="date" id="filtro-fecha-desde" class="form-control form-control-sm" />
            </div>
            <div>
              <label for="filtro-fecha-hasta" class="form-label text-xs font-bold uppercase text-muted mb-1">Hasta:</label>
              <input type="date" id="filtro-fecha-hasta" class="form-control form-control-sm" />
            </div>
            <div class="d-flex align-end gap-xs" style="height: 100%; padding-top: 1.25rem;">
              <button class="btn btn-secondary btn-sm" id="btn-limpiar-filtros" style="flex: 1;" title="Restablecer filtros">
                Limpiar
              </button>
            </div>
          </div>
          <div class="mt-2">
            <input type="text" id="filtro-search" class="form-control form-control-sm" placeholder="Buscar por #ID, nombre de cliente, código o motivo..." />
          </div>
        </div>

        <!-- ===================================================================
             TABLA DE MOVIMIENTOS COMERCIALES
             =================================================================== -->
        <div class="table-card">
          <div class="table-responsive">
            <table class="table table-hover" id="tabla-movimientos">
              <thead>
                <tr>
                  <th style="width: 7%;"># ID</th>
                  <th style="width: 10%;">Fecha</th>
                  <th style="width: 14%;">Tipo Operación</th>
                  <th style="width: 20%;">Participantes</th>
                  <th style="width: 9%; text-align: right;">Bulbos</th>
                  <th style="width: 13%; text-align: right;">Total / Neto</th>
                  <th style="width: 11%;">Estado Circuito</th>
                  <th style="width: 8%; text-align: center;">Doc</th>
                  <th style="width: 8%; text-align: center;">Acciones</th>
                </tr>
              </thead>
              <tbody id="tbody-movimientos">
                <tr>
                  <td colspan="9" class="text-center text-muted" style="padding: 2.5rem;">Cargando movimientos comerciales...</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- ===================================================================
             MODAL DINÁMICO: CREACIÓN / EDICIÓN DE MOVIMIENTO
             =================================================================== -->
        <div id="modal-movimiento" class="modal-backdrop">
          <div class="modal-dialog modal-lg">
            <div class="modal-header">
              <h3 class="modal-title" id="modal-movimiento-title">Nuevo Movimiento Comercial</h3>
              <button type="button" class="btn-icon modal-close" id="btn-close-modal-movimiento">&times;</button>
            </div>
            <div class="modal-body p-4" style="max-height: calc(85vh - 120px); overflow-y: auto;">
              <form id="form-movimiento">
                <!-- Selector de Tipo -->
                <div class="mb-4">
                  <label class="form-label font-bold text-xs uppercase text-muted mb-2">Tipo de Movimiento Comercial *</label>
                  <div class="d-flex gap-sm flex-wrap" id="tipo-movimiento-radios">
                    <label class="card p-2 d-flex align-center gap-xs cursor-pointer" style="flex: 1; min-width: 180px; border-radius: var(--radius-md);">
                      <input type="radio" name="radio-tipo-movimiento" value="1" checked />
                      <div>
                        <strong class="d-block text-sm text-primary">Venta a Cliente</strong>
                        <span class="text-xs text-muted">Empresa Central &rarr; Cliente</span>
                      </div>
                    </label>
                    <label class="card p-2 d-flex align-center gap-xs cursor-pointer" style="flex: 1; min-width: 180px; border-radius: var(--radius-md);">
                      <input type="radio" name="radio-tipo-movimiento" value="2" />
                      <div>
                        <strong class="d-block text-sm text-accent">Compra a Cliente</strong>
                        <span class="text-xs text-muted">Cliente &rarr; Empresa (con comisión)</span>
                      </div>
                    </label>
                    <label class="card p-2 d-flex align-center gap-xs cursor-pointer" style="flex: 1; min-width: 180px; border-radius: var(--radius-md);">
                      <input type="radio" name="radio-tipo-movimiento" value="3" />
                      <div>
                        <strong class="d-block text-sm text-info">Traspaso entre Clientes</strong>
                        <span class="text-xs text-muted">Logístico ($0.00)</span>
                      </div>
                    </label>
                  </div>
                </div>

                <!-- Temporada Agrícola -->
                <div class="form-group mb-3">
                  <label for="mov-temporada" class="form-label text-xs font-bold uppercase text-muted">Temporada Agrícola *</label>
                  <select id="mov-temporada" class="form-control" required>
                    <option value="">Seleccione temporada...</option>
                  </select>
                </div>

                <!-- Participantes Dinámicos -->
                <div class="grid grid-cols-1 md:grid-cols-2 gap-md mb-3" id="participantes-container">
                  <!-- Inyectado dinámicamente según selectedTipo -->
                </div>

                <!-- Aviso de Cliente Nuevo (solo Tipo 1) -->
                <div id="aviso-cliente-nuevo" class="mb-3" style="display: none;"></div>

                <!-- Aviso de Comisión (solo Tipo 2) -->
                <div id="container-comision" class="mb-3" style="display: none;"></div>

                <!-- Aviso de Traspaso (solo Tipo 3) -->
                <div id="aviso-traspaso" class="mb-3" style="display: none;">
                  <div class="alert alert-info d-flex align-center gap-sm p-3" style="background: rgba(14, 165, 233, 0.1); border-left: 4px solid var(--info); border-radius: var(--radius-sm);">
                    <span style="font-size: 1.1rem;">ℹ️</span>
                    <div class="text-sm">
                      <strong>Operación Logística:</strong> El traspaso directo entre productores no genera impacto económico comercial ($0.00) y se transfiere de forma directa tras la aprobación administrativa (sin emisión de factura).
                    </div>
                  </div>
                </div>

                <!-- Advertencia de Stock de Origen en tiempo real -->
                <div id="alerta-stock-origen" class="mb-3" style="display: none;"></div>

                <!-- Grilla de Ítems (Clases 0 a 4 + MIX) -->
                <div class="mb-3">
                  <label class="form-label font-bold text-xs uppercase text-muted mb-2">Desglose de Bulbos por Calibre y MIX *</label>
                  <div class="table-responsive">
                    <table class="table table-bordered mb-0" id="tabla-items-movimiento">
                      <thead style="background: var(--surface-alt);">
                        <tr>
                          <th style="width: 25%;">Clase Comercial</th>
                          <th style="width: 25%;">Cantidad (Bulbos)</th>
                          <th style="width: 25%;">Precio Unit. Pactado ($)</th>
                          <th style="width: 25%; text-align: right;">Subtotal ($)</th>
                        </tr>
                      </thead>
                      <tbody id="tbody-items-movimiento">
                        <!-- Inyectado dinámicamente para las 6 clases -->
                      </tbody>
                      <tfoot style="background: var(--surface-alt); font-weight: 700;">
                        <tr>
                          <td>TOTAL BULBOS:</td>
                          <td id="mov-total-bulbos-cell" class="font-bold text-primary">0 unidades</td>
                          <td id="mov-label-tipo-total" style="text-align: right;">MONTO TOTAL:</td>
                          <td id="mov-total-monto-cell" style="text-align: right; color: var(--success); font-size: 1.15rem;">$ 0,00</td>
                        </tr>
                      </tfoot>
                    </table>
                  </div>
                </div>

                <!-- Desglose de Comisión en vivo (solo Tipo 2) -->
                <div id="resumen-comision-compra" class="card p-3 mb-3" style="display: none; background: rgba(229, 142, 38, 0.08); border-left: 4px solid var(--accent);">
                  <div class="d-flex justify-between align-center text-sm mb-1">
                    <span>Monto Bruto de Compra:</span>
                    <strong id="label-bruto-compra">$ 0,00</strong>
                  </div>
                  <div class="d-flex justify-between align-center text-sm mb-1 text-danger">
                    <span>Comisión Administrativa (<span id="label-pct-comision">5</span>%):</span>
                    <strong id="label-monto-comision">- $ 0,00</strong>
                  </div>
                  <div class="d-flex justify-between align-center font-bold text-base border-top pt-2 mt-1">
                    <span>Monto Neto a Liquidar al Cliente:</span>
                    <strong class="text-success" id="label-neto-compra">$ 0,00</strong>
                  </div>
                </div>

                <!-- Observaciones -->
                <div class="form-group mb-4">
                  <label for="mov-observaciones" class="form-label text-xs font-bold uppercase text-muted">Observaciones Generales</label>
                  <textarea id="mov-observaciones" class="form-control" rows="2" placeholder="Notas adicionales sobre la operación, condición de entrega o acuerdos comerciales..."></textarea>
                </div>

                <div class="d-flex justify-end gap-sm">
                  <button type="button" class="btn btn-secondary" id="btn-cancel-modal-movimiento">Cancelar</button>
                  <button type="submit" class="btn btn-primary" id="btn-submit-movimiento">
                    <span>Guardar y Registrar</span>
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>

        <!-- ===================================================================
             MODAL DINÁMICO: DETALLE Y AUDITORÍA DE MOVIMIENTO
             =================================================================== -->
        <div id="modal-detalle-movimiento" class="modal-backdrop">
          <div class="modal-dialog modal-lg">
            <div class="modal-header">
              <h3 class="modal-title" id="modal-detalle-title">Detalle de Operación Comercial</h3>
              <button type="button" class="btn-icon modal-close" id="btn-close-modal-detalle">&times;</button>
            </div>
            <div class="modal-body p-4" id="modal-detalle-body" style="max-height: calc(85vh - 140px); overflow-y: auto;">
              <!-- Inyectado dinámicamente -->
            </div>
            <div class="modal-footer" id="modal-detalle-footer">
              <!-- Botones de acción dinámicos -->
            </div>
          </div>
        </div>

        <!-- ===================================================================
             MODAL DINÁMICO: ACCIÓN DE CIRCUITO (CONTRATO / FACTURA / RECHAZO / FINALIZAR)
             =================================================================== -->
        <div id="modal-accion-circuito" class="modal-backdrop">
          <div class="modal-dialog">
            <div class="modal-header">
              <h3 class="modal-title" id="modal-accion-title">Acción del Circuito</h3>
              <button type="button" class="btn-icon modal-close" id="btn-close-modal-accion">&times;</button>
            </div>
            <div class="modal-body p-4" id="modal-accion-body">
              <!-- Inyectado dinámicamente -->
            </div>
          </div>
        </div>
      </div>
    `;

    this._bindBaseEvents();
    await this._loadInitialData();
    await this._loadMovimientos();
  }

  // ===========================================================================
  // INICIALIZACIÓN Y EVENTOS BASE
  // ===========================================================================

  _bindBaseEvents() {
    // Botón refrescar
    this.container.querySelector('#btn-refresh-movimientos')?.addEventListener('click', async () => {
      await this._loadMovimientos();
    });

    // Botón nuevo movimiento
    this.container.querySelector('#btn-nuevo-movimiento')?.addEventListener('click', () => {
      this._openModalMovimiento(null);
    });

    // KPI click-to-filter
    this.container.querySelectorAll('[data-kpi-estado]').forEach(card => {
      card.addEventListener('click', () => {
        const estado = card.getAttribute('data-kpi-estado');
        const selectEstado = this.container.querySelector('#filtro-estado');
        if (selectEstado) {
          selectEstado.value = estado;
          this.filtro.estado = estado;
          this._loadMovimientos();
        }
      });
    });

    // Filtros
    const bindFilter = (id, prop) => {
      this.container.querySelector(`#${id}`)?.addEventListener('change', (e) => {
        this.filtro[prop] = e.target.value;
        this._loadMovimientos();
      });
    };

    bindFilter('filtro-tipo', 'tipo');
    bindFilter('filtro-estado', 'estado');
    bindFilter('filtro-cliente', 'clienteId');
    bindFilter('filtro-temporada', 'temporadaId');
    bindFilter('filtro-fecha-desde', 'fechaDesde');
    bindFilter('filtro-fecha-hasta', 'fechaHasta');

    // Búsqueda con debounce
    let searchTimeout = null;
    this.container.querySelector('#filtro-search')?.addEventListener('input', (e) => {
      clearTimeout(searchTimeout);
      searchTimeout = setTimeout(() => {
        this.filtro.search = e.target.value;
        this._renderTable();
      }, 300);
    });

    // Limpiar filtros
    this.container.querySelector('#btn-limpiar-filtros')?.addEventListener('click', () => {
      this.filtro = {
        tipo: 'todos',
        estado: 'todos',
        clienteId: 'todos',
        temporadaId: 'todos',
        fechaDesde: '',
        fechaHasta: '',
        search: ''
      };
      const setValue = (id, val) => {
        const el = this.container.querySelector(`#${id}`);
        if (el) el.value = val;
      };
      setValue('filtro-tipo', 'todos');
      setValue('filtro-estado', 'todos');
      setValue('filtro-cliente', 'todos');
      setValue('filtro-temporada', 'todos');
      setValue('filtro-fecha-desde', '');
      setValue('filtro-fecha-hasta', '');
      setValue('filtro-search', '');
      this._loadMovimientos();
    });

    // Modal cerrar listeners
    this.container.querySelector('#btn-close-modal-movimiento')?.addEventListener('click', () => this._closeModalMovimiento());
    this.container.querySelector('#btn-cancel-modal-movimiento')?.addEventListener('click', () => this._closeModalMovimiento());
    this.container.querySelector('#btn-close-modal-detalle')?.addEventListener('click', () => this._closeModalDetalle());
    this.container.querySelector('#btn-close-modal-accion')?.addEventListener('click', () => this._closeModalAccion());

    // Radios tipo movimiento
    this.container.querySelectorAll('input[name="radio-tipo-movimiento"]').forEach(radio => {
      radio.addEventListener('change', (e) => {
        this.selectedTipo = Number(e.target.value);
        this._updateFormTipoLayout();
      });
    });

    // Submit del formulario de movimiento
    this.container.querySelector('#form-movimiento')?.addEventListener('submit', async (e) => {
      e.preventDefault();
      await this._handleSubmitMovimiento();
    });
  }

  async _loadInitialData() {
    try {
      const [temporadas, activa, clientesData] = await Promise.all([
        temporadasService.listar().catch(() => []),
        temporadasService.obtenerActiva().catch(() => null),
        clientesService.listar({ page: 1, pageSize: 200, estado: 'activos' }).catch(() => ({ items: [] }))
      ]);

      this.temporadas = Array.isArray(temporadas) ? temporadas : [];
      this.clientes = clientesData?.items || [];
      this.activeTemporadaId = activa ? Number(activa.id) : (this.temporadas[0]?.id ? Number(this.temporadas[0].id) : null);

      // Poblar select filtro clientes
      const selectClienteFiltro = this.container.querySelector('#filtro-cliente');
      if (selectClienteFiltro) {
        this.clientes.forEach(c => {
          const opt = document.createElement('option');
          opt.value = String(c.id);
          opt.textContent = `👤 ${c.nombre} ${c.apellido} (${c.codigo || 'CLI-' + c.id})`;
          selectClienteFiltro.appendChild(opt);
        });
      }

      // Poblar select filtro temporadas
      const selectTempFiltro = this.container.querySelector('#filtro-temporada');
      if (selectTempFiltro) {
        this.temporadas.forEach(t => {
          const opt = document.createElement('option');
          opt.value = String(t.id);
          opt.textContent = t.nombre || `Temporada #${t.id}`;
          selectTempFiltro.appendChild(opt);
        });
      }
    } catch (err) {
      console.warn('[MovimientosComercialesView] Error al cargar datos iniciales:', err);
    }
  }

  // ===========================================================================
  // CARGA Y RENDERIZADO DE LA TABLA
  // ===========================================================================

  async _loadMovimientos() {
    const tbody = this.container.querySelector('#tbody-movimientos');
    if (!tbody) return;

    tbody.innerHTML = `<tr><td colspan="9" class="text-center text-muted" style="padding: 2.5rem;">Cargando movimientos comerciales...</td></tr>`;

    try {
      this.movimientos = await movimientosComercialesService.listar(this.filtro);
      this._updateKpis();
      this._renderTable();
    } catch (err) {
      console.error('[MovimientosComercialesView] Error al listar:', err);
      tbody.innerHTML = `<tr><td colspan="9" class="text-center text-danger" style="padding: 2.5rem;">${escapeHtml(err.message || 'Error al obtener movimientos')}</td></tr>`;
      toast.error('No se pudieron consultar los movimientos comerciales.');
    }
  }

  _updateKpis() {
    const total = this.movimientos.length;
    const count = (st) => this.movimientos.filter(m => Number(m.estado) === st).length;

    const setKpi = (id, val) => {
      const el = this.container.querySelector(`#${id}`);
      if (el) el.textContent = val;
    };

    setKpi('kpi-total', total);
    setKpi('kpi-pendientes', count(0));
    setKpi('kpi-aprobados', count(1));
    setKpi('kpi-facturados', count(2));
    setKpi('kpi-finalizados', count(3));
    setKpi('kpi-rechazados', count(4));
  }

  _renderTable() {
    const tbody = this.container.querySelector('#tbody-movimientos');
    if (!tbody) return;

    let list = [...this.movimientos];

    // Búsqueda de texto en memoria
    if (this.filtro.search && this.filtro.search.trim() !== '') {
      const q = this.filtro.search.trim().toLowerCase();
      list = list.filter(m => {
        const idMatch = String(m.id).includes(q);
        const origMatch = String(m.nombreOrigen || '').toLowerCase().includes(q);
        const destMatch = String(m.nombreDestino || '').toLowerCase().includes(q);
        const cliMatch = String(m.nombreCliente || '').toLowerCase().includes(q);
        const obsMatch = String(m.observaciones || '').toLowerCase().includes(q);
        const factMatch = String(m.factura?.numeroFactura || '').toLowerCase().includes(q);
        const contMatch = String(m.contrato?.numeroContrato || '').toLowerCase().includes(q);
        return idMatch || origMatch || destMatch || cliMatch || obsMatch || factMatch || contMatch;
      });
    }

    if (list.length === 0) {
      tbody.innerHTML = `
        <tr>
          <td colspan="9" class="text-center text-muted" style="padding: 2.5rem;">
            No se encontraron movimientos comerciales con los filtros aplicados.
          </td>
        </tr>
      `;
      return;
    }

    tbody.innerHTML = list.map(m => {
      const tipoMeta = TIPOS_MOVIMIENTO[m.tipo] || { label: `Tipo ${m.tipo}`, badgeClass: 'badge-secondary' };
      const estadoMeta = ESTADOS_MOVIMIENTO[m.estado] || { label: `Estado ${m.estado}`, badgeClass: 'badge-secondary' };
      const fReg = m.fechaRegistro ? new Date(m.fechaRegistro).toLocaleDateString('es-AR') : '-';

      const totalBulbos = (Number(m.totalBulbos) || 0).toLocaleString('es-AR');
      const montoTotal = Number(m.montoTotal) || 0;
      const montoFmt = m.tipo === 3
        ? '<span class="text-muted">$ 0,00</span>'
        : `$ ${montoTotal.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;

      // Comprobantes indicators
      const contratoBadge = m.contrato
        ? `<span class="badge badge-success text-xs" title="Contrato #${escapeHtml(m.contrato.numeroContrato)}">Contrato</span>`
        : (m.tipo === 1 && m.requiereContrato ? `<span class="badge badge-warning text-xs" title="Requiere contrato previo">Req. Contrato</span>` : '<span class="text-muted text-xs">-</span>');

      const facturaBadge = m.factura
        ? `<span class="badge badge-primary text-xs" title="Factura #${escapeHtml(m.factura.numeroFactura)}">Factura</span>`
        : '<span class="text-muted text-xs">-</span>';

      const origenLabel = m.nombreOrigen || (m.origenParticipanteId === 1 ? 'Empresa Central' : `Cliente #${m.origenParticipanteId}`);
      const destinoLabel = m.nombreDestino || (m.destinoParticipanteId === 1 ? 'Empresa Central' : `Cliente #${m.destinoParticipanteId}`);

      return `
        <tr data-mov-id="${m.id}">
          <td class="font-bold text-main">#${m.id}</td>
          <td class="text-xs">${escapeHtml(fReg)}</td>
          <td>
            <span class="badge ${tipoMeta.badgeClass} text-xs font-bold">
              ${escapeHtml(tipoMeta.label)}
            </span>
          </td>
          <td>
            <div class="d-flex flex-column text-xs">
              <span class="font-medium text-main"><strong>De:</strong> ${escapeHtml(origenLabel)}</span>
              <span class="text-muted"><strong>A:</strong> ${escapeHtml(destinoLabel)}</span>
            </div>
          </td>
          <td style="text-align: right; font-weight: 600;">${totalBulbos}</td>
          <td style="text-align: right; font-weight: 700; color: var(--primary);">${montoFmt}</td>
          <td>
            <span class="badge ${estadoMeta.badgeClass} text-xs">
              <span class="badge-dot"></span>
              ${escapeHtml(estadoMeta.label)}
            </span>
          </td>
          <td style="text-align: center;">
            <div class="d-flex flex-column gap-xs align-center">
              ${contratoBadge}
              ${facturaBadge}
            </div>
          </td>
          <td style="text-align: center;">
            <div class="d-flex justify-center align-center gap-xs flex-wrap">
              <button type="button" class="btn btn-secondary btn-sm btn-ver-detalle" data-id="${m.id}" title="Ver detalle completo">
                <svg class="icon-svg sm" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path>
                  <circle cx="12" cy="12" r="3"></circle>
                </svg>
              </button>
              ${this._renderRowActionButtons(m)}
            </div>
          </td>
        </tr>
      `;
    }).join('');

    this._bindTableActions();
  }

  _renderRowActionButtons(m) {
    const btns = [];
    const st = Number(m.estado);
    const tipo = Number(m.tipo);
    const currentUser = authService.getUsuarioActual();
    const isCreator = currentUser && Number(currentUser.id) === Number(m.usuarioRegistroId);

    // 1. Pendiente de Revisión (0)
    if (st === 0) {
      if (authService.tienePermiso('MOVCOM_APROBAR')) {
        btns.push(`
          <button type="button" class="btn btn-primary btn-sm btn-action-aprobar" data-id="${m.id}" title="Aprobar operación">
            <span>Aprobar</span>
          </button>
        `);
      }
      if (authService.tienePermiso('MOVCOM_RECHAZAR')) {
        btns.push(`
          <button type="button" class="btn btn-outline btn-sm btn-action-rechazar" data-id="${m.id}" title="Rechazar u Observar">
            ${getProhibitionIcon({ size: 'sm', title: 'Rechazar' })}
          </button>
        `);
      }
    }

    // 2. Aprobado Administración (1)
    if (st === 1) {
      if (tipo !== 3) {
        if (authService.tienePermiso('MOVCOM_FACTURAR')) {
          btns.push(`
            <button type="button" class="btn btn-primary btn-sm btn-action-facturar" data-id="${m.id}" title="Emitir Factura">
              <span>Facturar</span>
            </button>
          `);
        }
      } else {
        // Traspaso: salta facturación y va directo a finalizar
        if (authService.tienePermiso('MOVCOM_FINALIZAR')) {
          btns.push(`
            <button type="button" class="btn btn-success btn-sm btn-action-finalizar" data-id="${m.id}" title="Finalizar traspaso e impactar stock">
              <span>Finalizar</span>
            </button>
          `);
        }
      }
      if (authService.tienePermiso('MOVCOM_RECHAZAR')) {
        btns.push(`
          <button type="button" class="btn btn-outline btn-sm btn-action-rechazar" data-id="${m.id}" title="Rechazar u Observar">
            ${getProhibitionIcon({ size: 'sm', title: 'Rechazar' })}
          </button>
        `);
      }
    }

    // 3. Facturado (2)
    if (st === 2 && tipo !== 3) {
      if (authService.tienePermiso('MOVCOM_FINALIZAR')) {
        btns.push(`
          <button type="button" class="btn btn-success btn-sm btn-action-finalizar" data-id="${m.id}" title="Finalizar e impactar stock">
            <span>Finalizar</span>
          </button>
        `);
      }
      if (authService.tienePermiso('MOVCOM_RECHAZAR')) {
        btns.push(`
          <button type="button" class="btn btn-outline btn-sm btn-action-rechazar" data-id="${m.id}" title="Rechazar y anular factura">
            ${getProhibitionIcon({ size: 'sm', title: 'Rechazar' })}
          </button>
        `);
      }
    }

    // 4. Rechazado (4) -> Corregir y Reenviar
    if (st === 4) {
      if (isCreator || authService.tienePermiso('MOVCOM_CORREGIR')) {
        btns.push(`
          <button type="button" class="btn btn-secondary btn-sm btn-action-corregir" data-id="${m.id}" title="Corregir y reenviar">
            <span>Corregir</span>
          </button>
        `);
      }
    }

    return btns.join('');
  }

  _renderDetailActionButtons(mov) {
    const btns = [];
    const st = Number(mov.estado);
    const tipo = Number(mov.tipo);
    const currentUser = authService.getUsuarioActual();
    const isCreator = currentUser && Number(currentUser.id) === Number(mov.usuarioRegistroId);

    // State 0 (PendienteRevision): Aprobar (MOVCOM_APROBAR), Rechazar (MOVCOM_RECHAZAR)
    if (st === 0) {
      if (authService.tienePermiso('MOVCOM_APROBAR')) {
        btns.push(`
          <button type="button" class="btn btn-primary btn-sm btn-detalle-action" id="btn-detalle-aprobar" title="Aprobar operación">
            <span>Aprobar</span>
          </button>
        `);
      }
      if (authService.tienePermiso('MOVCOM_RECHAZAR')) {
        btns.push(`
          <button type="button" class="btn btn-outline btn-sm btn-detalle-action" id="btn-detalle-rechazar" title="Rechazar u Observar">
            ${getProhibitionIcon({ size: 'sm', title: 'Rechazar' })}
            <span style="color: var(--danger); font-weight: 600;">Rechazar</span>
          </button>
        `);
      }
    }

    // State 1 (AprobadoAdmin): Facturar (Tipo 1 & 2), Finalizar (Tipo 3), Rechazar
    if (st === 1) {
      if (tipo !== 3) {
        if (authService.tienePermiso('MOVCOM_FACTURAR')) {
          btns.push(`
            <button type="button" class="btn btn-primary btn-sm btn-detalle-action" id="btn-detalle-facturar" title="Emitir Factura">
              <span>Facturar</span>
            </button>
          `);
        }
      } else {
        // Traspaso: salta facturación y va directo a finalizar
        if (authService.tienePermiso('MOVCOM_FINALIZAR')) {
          btns.push(`
            <button type="button" class="btn btn-success btn-sm btn-detalle-action" id="btn-detalle-finalizar" title="Finalizar traspaso e impactar stock">
              <span>Finalizar</span>
            </button>
          `);
        }
      }
      if (authService.tienePermiso('MOVCOM_RECHAZAR')) {
        btns.push(`
          <button type="button" class="btn btn-outline btn-sm btn-detalle-action" id="btn-detalle-rechazar" title="Rechazar u Observar">
            ${getProhibitionIcon({ size: 'sm', title: 'Rechazar' })}
            <span style="color: var(--danger); font-weight: 600;">Rechazar</span>
          </button>
        `);
      }
    }

    // State 2 (Facturado): Finalizar (MOVCOM_FINALIZAR), Rechazar (MOVCOM_RECHAZAR)
    if (st === 2 && tipo !== 3) {
      if (authService.tienePermiso('MOVCOM_FINALIZAR')) {
        btns.push(`
          <button type="button" class="btn btn-success btn-sm btn-detalle-action" id="btn-detalle-finalizar" title="Finalizar e impactar stock">
            <span>Finalizar</span>
          </button>
        `);
      }
      if (authService.tienePermiso('MOVCOM_RECHAZAR')) {
        btns.push(`
          <button type="button" class="btn btn-outline btn-sm btn-detalle-action" id="btn-detalle-rechazar" title="Rechazar y anular factura">
            ${getProhibitionIcon({ size: 'sm', title: 'Rechazar' })}
            <span style="color: var(--danger); font-weight: 600;">Rechazar</span>
          </button>
        `);
      }
    }

    // State 4 (Rechazado): Corregir y Reenviar (MOVCOM_CORREGIR o creador)
    if (st === 4) {
      if (isCreator || authService.tienePermiso('MOVCOM_CORREGIR')) {
        btns.push(`
          <button type="button" class="btn btn-secondary btn-sm btn-detalle-action" id="btn-detalle-corregir" title="Corregir y reenviar a revisión">
            <span>Corregir y Reenviar</span>
          </button>
        `);
      }
    }

    return btns;
  }

  _bindTableActions() {
    const tbody = this.container.querySelector('#tbody-movimientos');
    if (!tbody) return;

    // Ver Detalle
    tbody.querySelectorAll('.btn-ver-detalle').forEach(b => {
      b.addEventListener('click', () => {
        const id = Number(b.getAttribute('data-id'));
        this._openModalDetalle(id);
      });
    });

    // Aprobar
    tbody.querySelectorAll('.btn-action-aprobar').forEach(b => {
      b.addEventListener('click', () => {
        const id = Number(b.getAttribute('data-id'));
        this._abrirModalAccion('aprobar', id);
      });
    });

    // Facturar
    tbody.querySelectorAll('.btn-action-facturar').forEach(b => {
      b.addEventListener('click', () => {
        const id = Number(b.getAttribute('data-id'));
        this._abrirModalAccion('facturar', id);
      });
    });

    // Finalizar
    tbody.querySelectorAll('.btn-action-finalizar').forEach(b => {
      b.addEventListener('click', () => {
        const id = Number(b.getAttribute('data-id'));
        this._abrirModalAccion('finalizar', id);
      });
    });

    // Rechazar
    tbody.querySelectorAll('.btn-action-rechazar').forEach(b => {
      b.addEventListener('click', () => {
        const id = Number(b.getAttribute('data-id'));
        this._abrirModalAccion('rechazar', id);
      });
    });

    // Corregir y Reenviar
    tbody.querySelectorAll('.btn-action-corregir').forEach(b => {
      b.addEventListener('click', async () => {
        const id = Number(b.getAttribute('data-id'));
        await this._openModalMovimiento(id);
      });
    });
  }

  // ===========================================================================
  // MODAL DE CREACIÓN / EDICIÓN MULTI-TIPO
  // ===========================================================================

  async _openModalMovimiento(editId = null) {
    const modal = this.container.querySelector('#modal-movimiento');
    const title = this.container.querySelector('#modal-movimiento-title');
    const selectTemp = this.container.querySelector('#mov-temporada');
    if (!modal || !title || !selectTemp) return;

    // Cargar temporadas en el selector
    selectTemp.innerHTML = '<option value="">Seleccione temporada...</option>' +
      this.temporadas.map(t => {
        const isSelected = this.activeTemporadaId && Number(t.id) === Number(this.activeTemporadaId);
        return `<option value="${t.id}" ${isSelected ? 'selected' : ''}>${escapeHtml(t.nombre || 'Temporada #' + t.id)}</option>`;
      }).join('');

    selectTemp.addEventListener('change', async (e) => {
      const tId = Number(e.target.value);
      if (tId) {
        await this._loadPreciosReferenciaTemporada(tId);
        this._recalcularTotales();
      }
    });

    if (editId) {
      title.textContent = `Corregir Movimiento Comercial #${editId}`;
      try {
        const mov = await movimientosComercialesService.obtenerPorId(editId);
        this.currentEditMovimiento = mov;
        this.selectedTipo = Number(mov.tipo);
      } catch (err) {
        toast.error('No se pudo cargar el movimiento para editar.');
        return;
      }
    } else {
      title.textContent = 'Nuevo Movimiento Comercial';
      this.currentEditMovimiento = null;
      this.selectedTipo = 1; // Default: Venta
    }

    // Set radio tipo
    const radio = this.container.querySelector(`input[name="radio-tipo-movimiento"][value="${this.selectedTipo}"]`);
    if (radio) radio.checked = true;

    // Deshabilitar cambio de tipo si estamos en edición
    this.container.querySelectorAll('input[name="radio-tipo-movimiento"]').forEach(r => {
      r.disabled = Boolean(editId);
    });

    // Inicializar precios de temporada
    const tempId = this.currentEditMovimiento ? Number(this.currentEditMovimiento.temporadaId) : this.activeTemporadaId;
    if (tempId) {
      selectTemp.value = String(tempId);
      await this._loadPreciosReferenciaTemporada(tempId);
    }

    const submitBtn = this.container.querySelector('#btn-submit-movimiento');
    if (submitBtn) {
      submitBtn.innerHTML = editId ? '<span>Corregir y Reenviar</span>' : '<span>Guardar y Registrar</span>';
    }

    this._updateFormTipoLayout();
    modal.classList.add('is-active');
  }

  _closeModalMovimiento() {
    const modal = this.container.querySelector('#modal-movimiento');
    modal?.classList.remove('is-active');
    this.currentEditMovimiento = null;
    this.origenStockData = null;
  }

  async _loadPreciosReferenciaTemporada(temporadaId) {
    try {
      const list = await preciosReferenciaService.listarPorTemporada(temporadaId);
      this.preciosTemporada = {};
      list.forEach(p => {
        this.preciosTemporada[_parseClaseIndex(p.claseBulbo)] = Number(p.precioReferencia) || 0;
      });
    } catch {
      this.preciosTemporada = {};
    }
  }

  _updateFormTipoLayout() {
    const partContainer = this.container.querySelector('#participantes-container');
    const avisoCliente = this.container.querySelector('#aviso-cliente-nuevo');
    const containerComision = this.container.querySelector('#container-comision');
    const avisoTraspaso = this.container.querySelector('#aviso-traspaso');
    const resumenComision = this.container.querySelector('#resumen-comision-compra');
    const labelTipoTotal = this.container.querySelector('#mov-label-tipo-total');
    if (!partContainer) return;

    avisoCliente.style.display = 'none';
    containerComision.style.display = 'none';
    avisoTraspaso.style.display = 'none';
    resumenComision.style.display = 'none';

    // Generar opciones de clientes
    const clientesOptions = this.clientes.map(c =>
      `<option value="${c.id}">👤 ${escapeHtml(c.nombre)} ${escapeHtml(c.apellido)} (${escapeHtml(c.codigo || 'CLI-' + c.id)})</option>`
    ).join('');

    if (this.selectedTipo === 1) {
      // Venta: Origen = Empresa (locked), Destino = Cliente
      labelTipoTotal.textContent = 'MONTO TOTAL VENTA:';
      partContainer.innerHTML = `
        <div class="form-group mb-0">
          <label class="form-label text-xs font-bold uppercase text-muted">Origen (Vendedor) *</label>
          <input type="text" class="form-control" value="🏢 Empresa Central (Depósito Central, ID #1)" disabled style="background: var(--surface-alt); font-weight: 600;" />
          <input type="hidden" id="mov-origen-id" value="1" />
        </div>
        <div class="form-group mb-0">
          <label for="mov-destino-id" class="form-label text-xs font-bold uppercase text-muted">Destino (Cliente Comprador) *</label>
          <select id="mov-destino-id" class="form-control" required>
            <option value="">Seleccione cliente comprador...</option>
            ${clientesOptions}
          </select>
        </div>
      `;

      // Cargar stock origen de Empresa para advertencias
      this._cargarStockOrigen(EMPRESA_SINGLETON_ID);

      // Event listener sobre destino para verificar cliente nuevo
      const destSelect = this.container.querySelector('#mov-destino-id');
      destSelect?.addEventListener('change', async (e) => {
        const cliId = Number(e.target.value);
        if (cliId) {
          await this._verificarClienteNuevoBadge(cliId);
        } else {
          avisoCliente.style.display = 'none';
        }
      });

    } else if (this.selectedTipo === 2) {
      // Compra: Origen = Cliente, Destino = Empresa (locked), Comisión %
      labelTipoTotal.textContent = 'MONTO NETO A LIQUIDAR:';
      containerComision.style.display = 'block';
      resumenComision.style.display = 'block';

      partContainer.innerHTML = `
        <div class="form-group mb-0">
          <label for="mov-origen-id" class="form-label text-xs font-bold uppercase text-muted">Origen (Cliente Vendedor) *</label>
          <select id="mov-origen-id" class="form-control" required>
            <option value="">Seleccione cliente vendedor...</option>
            ${clientesOptions}
          </select>
        </div>
        <div class="form-group mb-0">
          <label class="form-label text-xs font-bold uppercase text-muted">Destino (Comprador) *</label>
          <input type="text" class="form-control" value="🏢 Empresa Central (Depósito Central, ID #1)" disabled style="background: var(--surface-alt); font-weight: 600;" />
          <input type="hidden" id="mov-destino-id" value="1" />
        </div>
      `;

      containerComision.innerHTML = `
        <div class="card p-3" style="background: var(--surface-alt);">
          <div class="d-flex align-center gap-md flex-wrap">
            <label for="mov-porcentaje-comision" class="form-label font-bold text-xs uppercase text-muted mb-0">
              Comisión Administrativa Retenida (%):
            </label>
            <div class="input-group" style="max-width: 140px;">
              <input type="number" id="mov-porcentaje-comision" class="form-control" min="0" max="100" step="0.5" value="5" required />
              <span class="input-group-text font-bold">%</span>
            </div>
            <span class="text-xs text-muted">Porcentaje deducido del monto bruto de compra.</span>
          </div>
        </div>
      `;

      // Listener en origen para stock
      const origSelect = this.container.querySelector('#mov-origen-id');
      origSelect?.addEventListener('change', (e) => {
        const cliId = Number(e.target.value);
        if (cliId) {
          this._cargarStockOrigen(cliId);
        }
      });

      // Listener en comisión
      this.container.querySelector('#mov-porcentaje-comision')?.addEventListener('input', () => {
        this._recalcularTotales();
      });

    } else if (this.selectedTipo === 3) {
      // Traspaso: Origen = Cliente, Destino = Cliente (Origen != Destino)
      labelTipoTotal.textContent = 'MONTO OPERACIÓN:';
      avisoTraspaso.style.display = 'block';

      partContainer.innerHTML = `
        <div class="form-group mb-0">
          <label for="mov-origen-id" class="form-label text-xs font-bold uppercase text-muted">Cliente Origen (Emisor) *</label>
          <select id="mov-origen-id" class="form-control" required>
            <option value="">Seleccione cliente emisor...</option>
            ${clientesOptions}
          </select>
        </div>
        <div class="form-group mb-0">
          <label for="mov-destino-id" class="form-label text-xs font-bold uppercase text-muted">Cliente Destino (Receptor) *</label>
          <select id="mov-destino-id" class="form-control" required>
            <option value="">Seleccione cliente receptor...</option>
            ${clientesOptions}
          </select>
        </div>
      `;

      const origSelect = this.container.querySelector('#mov-origen-id');
      origSelect?.addEventListener('change', (e) => {
        const cliId = Number(e.target.value);
        if (cliId) {
          this._cargarStockOrigen(cliId);
        }
      });
    }

    // Renderizar grilla de ítems
    this._renderItemsGrid();

    // Si estamos editando, poblar valores previos
    if (this.currentEditMovimiento) {
      this._populateFormEditValues();
    }
  }

  async _verificarClienteNuevoBadge(clienteId) {
    const aviso = this.container.querySelector('#aviso-cliente-nuevo');
    if (!aviso) return;

    try {
      const status = await movimientosComercialesService.verificarClienteNuevo(
        clienteId,
        this.currentEditMovimiento ? this.currentEditMovimiento.id : null
      );
      this.clienteNuevoStatus = status;

      if (status.esNuevo && status.requiereContrato) {
        aviso.style.display = 'block';
        aviso.innerHTML = `
          <div class="alert alert-warning d-flex align-center gap-sm p-3" style="background: rgba(245, 158, 11, 0.1); border-left: 4px solid var(--warning); border-radius: var(--radius-sm);">
            <span style="font-size: 1.2rem;">⚠️</span>
            <div class="text-sm">
              <strong>Cliente Nuevo:</strong> Este cliente no cuenta con ventas previas finalizadas en el sistema.
              Para que la Administración pueda aprobar esta venta, será <strong>obligatorio generar y registrar el contrato marco previo</strong>.
            </div>
          </div>
        `;
      } else {
        aviso.style.display = 'block';
        aviso.innerHTML = `
          <div class="alert alert-success d-flex align-center gap-sm p-2" style="background: rgba(34, 197, 94, 0.1); border-left: 4px solid var(--success); border-radius: var(--radius-sm);">
            <span style="font-size: 1rem;">✓</span>
            <div class="text-xs">
              <strong>Cliente Regular:</strong> Posee historial comercial previo. No requiere generación obligatoria de contrato previo.
            </div>
          </div>
        `;
      }
    } catch (err) {
      console.warn('[MovimientosComercialesView] Error al verificar cliente nuevo:', err);
    }
  }

  async _cargarStockOrigen(origenId) {
    try {
      this.origenStockData = await stockService.obtenerStockPorCliente(origenId);
      this._verificarStockOrigenEnVivo();
    } catch {
      this.origenStockData = null;
    }
  }

  _renderItemsGrid() {
    const tbody = this.container.querySelector('#tbody-items-movimiento');
    if (!tbody) return;

    const isTraspaso = this.selectedTipo === 3;

    tbody.innerHTML = CLASES_BULBO.map(claseMeta => {
      const isMix = claseMeta.id === 5;
      const refPrice = isTraspaso ? 0 : (this.preciosTemporada[claseMeta.id] || 0);

      return `
        <tr data-clase="${claseMeta.id}">
          <td>
            <div class="d-flex flex-column">
              <span class="font-bold text-main">${escapeHtml(claseMeta.label)}</span>
              <span class="text-xs text-muted">${escapeHtml(claseMeta.desc)}</span>
            </div>
          </td>
          <td>
            <input
              type="number"
              class="form-control item-cant"
              data-clase="${claseMeta.id}"
              min="0"
              step="${isMix ? '3' : '1'}"
              placeholder="0"
            />
            ${isMix ? `
              <div id="mix-feedback-error" class="text-xs text-danger font-bold mt-1" style="display: none;">
                La cantidad para MIX debe ser múltiplo de 3 (ej. 300, 600, 900).
              </div>
              <div id="mix-decomp-callout" class="text-xs text-info font-bold mt-1" style="display: none;">
                <!-- Descomposición calculada en vivo -->
              </div>
            ` : ''}
          </td>
          <td>
            <div class="input-group">
              <span class="input-group-text">$</span>
              <input
                type="number"
                class="form-control item-precio"
                data-clase="${claseMeta.id}"
                min="0"
                step="0.01"
                value="${isTraspaso ? '0.00' : (refPrice > 0 ? refPrice.toFixed(2) : '0.00')}"
                ${isTraspaso ? 'disabled' : ''}
              />
            </div>
          </td>
          <td style="text-align: right;">
            <span class="font-bold item-subtotal text-main">$ 0,00</span>
          </td>
        </tr>
      `;
    }).join('');

    // Listeners para recálculo en vivo
    tbody.querySelectorAll('.item-cant, .item-precio').forEach(inp => {
      inp.addEventListener('input', () => {
        this._recalcularTotales();
        this._verificarStockOrigenEnVivo();
      });
    });
  }

  _populateFormEditValues(mov = this.currentEditMovimiento) {
    if (!mov) return;

    const origenSelect = this.container.querySelector('#mov-origen-id');
    const destinoSelect = this.container.querySelector('#mov-destino-id');
    const obsInput = this.container.querySelector('#mov-observaciones');
    const comisionInput = this.container.querySelector('#mov-porcentaje-comision');

    if (origenSelect && mov.origenParticipanteId) origenSelect.value = String(mov.origenParticipanteId);
    if (destinoSelect && mov.destinoParticipanteId) {
      destinoSelect.value = String(mov.destinoParticipanteId);
      if (this.selectedTipo === 1) {
        this._verificarClienteNuevoBadge(mov.destinoParticipanteId);
      }
    }
    if (obsInput) obsInput.value = mov.observaciones || '';
    if (comisionInput && mov.porcentajeComision !== undefined) comisionInput.value = String(mov.porcentajeComision);

    // Cargar stock origen para Tipo 2 y Tipo 3 si origenParticipanteId está presente
    if (mov.origenParticipanteId && (this.selectedTipo === 2 || this.selectedTipo === 3)) {
      this._cargarStockOrigen(mov.origenParticipanteId);
    }

    if (Array.isArray(mov.detalles)) {
      mov.detalles.forEach(d => {
        const cId = _parseClaseIndex(d.claseBulbo);
        const cantInp = this.container.querySelector(`.item-cant[data-clase="${cId}"]`);
        const precInp = this.container.querySelector(`.item-precio[data-clase="${cId}"]`);
        if (cantInp) cantInp.value = String(d.cantidad);
        if (precInp) precInp.value = Number(d.precioUnitarioPactado).toFixed(2);
      });
    }

    this._recalcularTotales();
    this._verificarStockOrigenEnVivo();
  }

  _recalcularTotales() {
    let totalBulbos = 0;
    let montoBruto = 0;

    const isTraspaso = this.selectedTipo === 3;
    const isCompra = this.selectedTipo === 2;

    CLASES_BULBO.forEach(claseMeta => {
      const cId = claseMeta.id;
      const cantInp = this.container.querySelector(`.item-cant[data-clase="${cId}"]`);
      const precInp = this.container.querySelector(`.item-precio[data-clase="${cId}"]`);
      const row = this.container.querySelector(`tr[data-clase="${cId}"]`);

      const cant = Number(cantInp?.value) || 0;
      const prec = isTraspaso ? 0 : (Number(precInp?.value) || 0);

      // Validación MIX múltiplo de 3
      if (cId === 5) {
        const errEl = this.container.querySelector('#mix-feedback-error');
        const calloutEl = this.container.querySelector('#mix-decomp-callout');

        if (cant > 0 && cant % 3 !== 0) {
          if (errEl) errEl.style.display = 'block';
          if (calloutEl) calloutEl.style.display = 'none';
          cantInp.style.borderColor = 'var(--danger)';
        } else {
          if (errEl) errEl.style.display = 'none';
          cantInp.style.borderColor = 'var(--border)';
          if (cant > 0 && calloutEl) {
            const part = cant / 3;
            calloutEl.style.display = 'block';
            calloutEl.textContent = `Descomposición: ${part} Clase 1, ${part} Clase 2, ${part} Clase 3.`;
          } else if (calloutEl) {
            calloutEl.style.display = 'none';
          }
        }
      }

      const subtotal = isTraspaso ? 0 : (cant * prec);
      totalBulbos += cant;
      montoBruto += subtotal;

      const subtotalEl = row?.querySelector('.item-subtotal');
      if (subtotalEl) {
        subtotalEl.textContent = `$ ${subtotal.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
      }
    });

    const totalBulbosCell = this.container.querySelector('#mov-total-bulbos-cell');
    if (totalBulbosCell) {
      totalBulbosCell.textContent = `${totalBulbos.toLocaleString('es-AR')} unidades`;
    }

    const totalMontoCell = this.container.querySelector('#mov-total-monto-cell');

    if (isTraspaso) {
      if (totalMontoCell) totalMontoCell.textContent = '$ 0,00';
    } else if (isCompra) {
      const pctInput = this.container.querySelector('#mov-porcentaje-comision');
      const pct = Math.max(0, Math.min(100, Number(pctInput?.value) || 0));
      const comision = Math.round(montoBruto * (pct / 100) * 100) / 100;
      const neto = Math.round((montoBruto - comision) * 100) / 100;

      const labelBruto = this.container.querySelector('#label-bruto-compra');
      const labelPct = this.container.querySelector('#label-pct-comision');
      const labelCom = this.container.querySelector('#label-monto-comision');
      const labelNeto = this.container.querySelector('#label-neto-compra');

      if (labelBruto) labelBruto.textContent = `$ ${montoBruto.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
      if (labelPct) labelPct.textContent = String(pct);
      if (labelCom) labelCom.textContent = `- $ ${comision.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
      if (labelNeto) labelNeto.textContent = `$ ${neto.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;

      if (totalMontoCell) {
        totalMontoCell.textContent = `$ ${neto.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
      }
    } else {
      // Venta
      if (totalMontoCell) {
        totalMontoCell.textContent = `$ ${montoBruto.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
      }
    }
  }

  _verificarStockOrigenEnVivo() {
    const alertaContainer = this.container.querySelector('#alerta-stock-origen');
    if (!alertaContainer || !this.origenStockData) {
      if (alertaContainer) alertaContainer.style.display = 'none';
      return;
    }

    // Calcular requerimientos físicos descompuestos
    const requeridosFisicos = { 0: 0, 1: 0, 2: 0, 3: 0, 4: 0 };

    for (let c = 0; c <= 4; c++) {
      const cant = Number(this.container.querySelector(`.item-cant[data-clase="${c}"]`)?.value) || 0;
      requeridosFisicos[c] += cant;
    }

    // MIX (5) descompuesto en 1/3 para Clases 1, 2 y 3
    const mixCant = Number(this.container.querySelector(`.item-cant[data-clase="5"]`)?.value) || 0;
    if (mixCant > 0 && mixCant % 3 === 0) {
      const tercio = mixCant / 3;
      requeridosFisicos[1] += tercio;
      requeridosFisicos[2] += tercio;
      requeridosFisicos[3] += tercio;
    }

    const deficiencias = [];
    for (let c = 0; c <= 4; c++) {
      const req = requeridosFisicos[c];
      if (req > 0) {
        const itemStock = this.origenStockData.find(s => _parseClaseIndex(s.claseBulbo ?? s.clase) === c);
        const disp = itemStock ? Number(itemStock.cantidad) : 0;
        if (req > disp) {
          deficiencias.push(`Clase ${c}: requiere ${req.toLocaleString('es-AR')} un., dispone de ${disp.toLocaleString('es-AR')} un.`);
        }
      }
    }

    if (deficiencias.length > 0) {
      alertaContainer.style.display = 'block';
      alertaContainer.innerHTML = `
        <div class="alert alert-danger d-flex align-start gap-sm p-3" style="background: rgba(239, 68, 68, 0.1); border-left: 4px solid var(--danger); border-radius: var(--radius-sm);">
          <span style="font-size: 1.1rem; color: var(--danger);">⚠️</span>
          <div>
            <strong class="text-danger">Advertencia de Stock Insuficiente en Origen:</strong>
            <p class="text-xs text-muted mb-1">
              Las cantidades especificadas (incluida la descomposición de MIX) superan el stock registrado en el titular emisor:
            </p>
            <ul class="text-xs text-danger mb-0" style="padding-left: 1.25rem;">
              ${deficiencias.map(d => `<li>${escapeHtml(d)}</li>`).join('')}
            </ul>
          </div>
        </div>
      `;
    } else {
      alertaContainer.style.display = 'none';
    }
  }

  async _handleSubmitMovimiento() {
    const temporadaId = Number(this.container.querySelector('#mov-temporada')?.value);
    if (!temporadaId) {
      toast.warning('Debe seleccionar la temporada agrícola.');
      return;
    }

    let origenId = 1;
    let destinoId = 1;

    if (this.selectedTipo === 1) {
      // Venta
      origenId = EMPRESA_SINGLETON_ID;
      destinoId = Number(this.container.querySelector('#mov-destino-id')?.value);
      if (!destinoId) {
        toast.warning('Debe seleccionar el cliente comprador.');
        return;
      }
    } else if (this.selectedTipo === 2) {
      // Compra
      origenId = Number(this.container.querySelector('#mov-origen-id')?.value);
      destinoId = EMPRESA_SINGLETON_ID;
      if (!origenId) {
        toast.warning('Debe seleccionar el cliente vendedor.');
        return;
      }
    } else if (this.selectedTipo === 3) {
      // Traspaso
      origenId = Number(this.container.querySelector('#mov-origen-id')?.value);
      destinoId = Number(this.container.querySelector('#mov-destino-id')?.value);
      if (!origenId || !destinoId) {
        toast.warning('Debe seleccionar cliente origen y cliente destino.');
        return;
      }
      if (origenId === destinoId) {
        toast.warning('El cliente origen y destino no pueden ser el mismo participante.');
        return;
      }
    }

    // Armar detalles
    const detalles = [];
    let totalBulbos = 0;

    for (let c = 0; c <= 5; c++) {
      const cantInp = this.container.querySelector(`.item-cant[data-clase="${c}"]`);
      const precInp = this.container.querySelector(`.item-precio[data-clase="${c}"]`);
      const cant = Number(cantInp?.value) || 0;
      const prec = this.selectedTipo === 3 ? 0 : (Number(precInp?.value) || 0);

      if (cant > 0) {
        if (c === 5 && cant % 3 !== 0) {
          toast.warning('La cantidad de ítems MIX debe ser múltiplo de 3.');
          cantInp.focus();
          return;
        }

        if (this.selectedTipo !== 3 && prec <= 0) {
          toast.warning(`Debe especificar un precio unitario pactado mayor a 0 para ${CLASES_BULBO[c].label}.`);
          precInp.focus();
          return;
        }

        detalles.push({
          claseBulbo: c,
          cantidad: cant,
          precioUnitarioPactado: prec
        });
        totalBulbos += cant;
      }
    }

    if (detalles.length === 0 || totalBulbos <= 0) {
      toast.warning('Debe ingresar al menos una clase de bulbo con cantidad mayor a 0.');
      return;
    }

    const obs = this.container.querySelector('#mov-observaciones')?.value.trim() || '';
    const pctComision = this.selectedTipo === 2
      ? (Number(this.container.querySelector('#mov-porcentaje-comision')?.value) || 0)
      : 0;

    const submitBtn = this.container.querySelector('#btn-submit-movimiento');
    if (submitBtn) {
      submitBtn.disabled = true;
      submitBtn.textContent = 'Procesando...';
    }

    try {
      const clienteParticipanteId = this.selectedTipo === 1 ? destinoId : (this.selectedTipo === 2 ? origenId : null);

      if (this.currentEditMovimiento) {
        // Actualizar y reenviar
        const updateDto = {
          origenParticipanteId: origenId,
          destinoParticipanteId: destinoId,
          clienteId: clienteParticipanteId,
          temporadaId,
          porcentajeComision: pctComision,
          observaciones: obs,
          detalles
        };
        await movimientosComercialesService.actualizar(this.currentEditMovimiento.id, updateDto);
        toast.success(`Movimiento Comercial #${this.currentEditMovimiento.id} corregido y reenviado a revisión.`);
      } else {
        // Crear nuevo
        const createDto = {
          tipo: this.selectedTipo,
          temporadaId,
          origenParticipanteId: origenId,
          destinoParticipanteId: destinoId,
          clienteId: clienteParticipanteId,
          porcentajeComision: pctComision,
          observaciones: obs,
          detalles
        };
        const nuevo = await movimientosComercialesService.crear(createDto);
        toast.success(`Movimiento Comercial #${nuevo.id || ''} registrado exitosamente.`);
      }

      this._closeModalMovimiento();
      await this._loadMovimientos();
    } catch (err) {
      console.error('[MovimientosComercialesView] Error al guardar movimiento:', err);
      toast.error('Error al registrar operación: ' + (err.message || 'Verifique los datos'));
    } finally {
      if (submitBtn) {
        submitBtn.disabled = false;
        submitBtn.innerHTML = this.currentEditMovimiento ? '<span>Corregir y Reenviar</span>' : '<span>Guardar y Registrar</span>';
      }
    }
  }

  // ===========================================================================
  // MODAL DE DETALLE Y AUDITORÍA / TIMELINE
  // ===========================================================================

  async _openModalDetalle(id) {
    const modal = this.container.querySelector('#modal-detalle-movimiento');
    const title = this.container.querySelector('#modal-detalle-title');
    const body = this.container.querySelector('#modal-detalle-body');
    const footer = this.container.querySelector('#modal-detalle-footer');
    if (!modal || !title || !body) return;

    title.textContent = `Detalle de Movimiento Comercial #${id}`;
    body.innerHTML = `<div class="text-center text-muted p-4">Cargando auditoría completa...</div>`;
    if (footer) footer.innerHTML = '';
    modal.classList.add('is-active');

    try {
      const mov = await movimientosComercialesService.obtenerPorId(id);
      this.activeMovimiento = mov;

      const tipoMeta = TIPOS_MOVIMIENTO[mov.tipo] || { label: `Tipo ${mov.tipo}`, badgeClass: 'badge-secondary' };
      const estadoMeta = ESTADOS_MOVIMIENTO[mov.estado] || { label: `Estado ${mov.estado}`, badgeClass: 'badge-secondary' };
      const fReg = mov.fechaRegistro ? new Date(mov.fechaRegistro).toLocaleString('es-AR') : '-';

      const origenLabel = mov.nombreOrigen || (mov.origenParticipanteId === 1 ? 'Empresa Central' : `Cliente #${mov.origenParticipanteId}`);
      const destinoLabel = mov.nombreDestino || (mov.destinoParticipanteId === 1 ? 'Empresa Central' : `Cliente #${mov.destinoParticipanteId}`);

      // Comprobantes
      let contratoHtml = `
        <div class="card p-3 mb-3">
          <div class="d-flex justify-between align-center mb-1">
            <strong class="text-xs uppercase text-muted">Contrato de Compraventa</strong>
            ${mov.contrato
              ? '<span class="badge badge-success text-xs">Firmado / Registrado</span>'
              : (mov.tipo === 1 && mov.requiereContrato
                ? '<span class="badge badge-warning text-xs">Requerido Obligatorio</span>'
                : '<span class="text-muted text-xs">No Requerido</span>')}
          </div>
          ${mov.contrato ? `
            <div class="text-sm">
              <div><strong>Nº Contrato:</strong> ${escapeHtml(mov.contrato.numeroContrato)}</div>
              <div class="text-xs text-muted">Fecha Emisión: ${new Date(mov.contrato.fechaEmision).toLocaleString('es-AR')}</div>
              ${mov.contrato.archivoUrl ? `<a href="${escapeHtml(mov.contrato.archivoUrl)}" target="_blank" class="text-xs text-primary d-inline-block mt-1">Ver Archivo Adjunto</a>` : ''}
            </div>
          ` : (mov.tipo === 1 && mov.requiereContrato && authService.tienePermiso('MOVCOM_APROBAR') ? `
            <p class="text-xs text-muted mb-2">Este cliente nuevo requiere el registro de contrato antes de la aprobación.</p>
            <button type="button" class="btn btn-secondary btn-sm" id="btn-modal-add-contrato">
              <span>+ Registrar Contrato</span>
            </button>
          ` : '<span class="text-xs text-muted">No se ha registrado contrato formal.</span>')}
        </div>
      `;

      let facturaHtml = `
        <div class="card p-3 mb-3">
          <div class="d-flex justify-between align-center mb-1">
            <strong class="text-xs uppercase text-muted">Facturación Comercial</strong>
            ${mov.factura ? '<span class="badge badge-primary text-xs">Emitida</span>' : '<span class="text-muted text-xs">Sin Factura</span>'}
          </div>
          ${mov.factura ? `
            <div class="text-sm">
              <div><strong>Nº Factura:</strong> ${escapeHtml(mov.factura.numeroFactura)}</div>
              <div class="text-xs text-muted">Fecha Emisión: ${new Date(mov.factura.fechaEmision).toLocaleString('es-AR')}</div>
              ${mov.factura.observaciones ? `<div class="text-xs text-muted">Notas: ${escapeHtml(mov.factura.observaciones)}</div>` : ''}
              ${mov.factura.archivoUrl ? `<a href="${escapeHtml(mov.factura.archivoUrl)}" target="_blank" class="text-xs text-primary d-inline-block mt-1">Ver Comprobante Digital</a>` : ''}
            </div>
          ` : (Number(mov.estado) === 1 && Number(mov.tipo) !== 3 && authService.tienePermiso('MOVCOM_FACTURAR') ? `
            <p class="text-xs text-muted mb-2">La operación está aprobada y lista para su facturación contable.</p>
            <button type="button" class="btn btn-primary btn-sm" id="btn-modal-add-factura">
              <span>+ Emitir Factura</span>
            </button>
          ` : '<span class="text-xs text-muted">No se ha emitido factura.</span>')}
        </div>
      `;

      // Timeline de Aprobaciones
      const historial = Array.isArray(mov.historialAprobaciones) ? mov.historialAprobaciones : [];
      let timelineHtml = `
        <div class="card p-3 mb-3">
          <strong class="text-xs uppercase text-muted d-block mb-3">Historial de Aprobaciones y Estados</strong>
          ${historial.length === 0 ? '<span class="text-xs text-muted">Sin transiciones registradas.</span>' : `
            <div class="timeline" style="border-left: 2px solid var(--border); margin-left: 0.75rem; padding-left: 1rem;">
              ${historial.map(h => {
                const fHist = h.fecha ? new Date(h.fecha).toLocaleString('es-AR') : '';
                const accionNombre = h.nombreAccion || ACCIONES_APROBACION[h.accion] || `Acción ${h.accion}`;
                return `
                  <div class="mb-3 position-relative">
                    <div style="font-weight: 700; font-size: 0.85rem; color: var(--primary);">${escapeHtml(accionNombre)}</div>
                    <div class="text-xs text-muted">
                      Por: <strong>${escapeHtml(h.nombreUsuarioAprobador || 'Usuario #' + h.usuarioAprobadorId)}</strong> — ${escapeHtml(fHist)}
                    </div>
                    ${h.observaciones ? `
                      <div class="card p-2 mt-1 text-xs" style="background: var(--surface-alt); border-left: 3px solid var(--border-color);">
                        ${escapeHtml(h.observaciones)}
                      </div>
                    ` : ''}
                  </div>
                `;
              }).join('')}
            </div>
          `}
        </div>
      `;

      // Render cuerpo completo
      body.innerHTML = `
        <div class="grid grid-cols-1 md:grid-cols-2 gap-md mb-3">
          <div class="card p-3">
            <span class="text-xs text-muted uppercase font-bold d-block mb-1">Identificación</span>
            <div class="d-flex align-center gap-xs mb-2">
              <span class="font-bold text-lg text-main">#${mov.id}</span>
              <span class="badge ${tipoMeta.badgeClass} text-xs font-bold">${escapeHtml(tipoMeta.label)}</span>
              <span class="badge ${estadoMeta.badgeClass} text-xs">${escapeHtml(estadoMeta.label)}</span>
            </div>
            <div class="text-xs text-muted mb-1">Registrado el: <strong>${escapeHtml(fReg)}</strong></div>
            <div class="text-xs text-muted">Usuario Registro: <strong>${escapeHtml(mov.nombreUsuarioRegistro || '#' + mov.usuarioRegistroId)}</strong></div>
          </div>

          <div class="card p-3">
            <span class="text-xs text-muted uppercase font-bold d-block mb-1">Participantes</span>
            <div class="text-sm mb-1"><strong>Emisor (Origen):</strong> ${escapeHtml(origenLabel)}</div>
            <div class="text-sm"><strong>Receptor (Destino):</strong> ${escapeHtml(destinoLabel)}</div>
          </div>
        </div>

        <!-- Desglose de Bulbos -->
        <div class="card p-3 mb-3">
          <strong class="text-xs uppercase text-muted d-block mb-2">Ítems Comercializados</strong>
          <div class="table-responsive">
            <table class="table table-bordered mb-0">
              <thead style="background: var(--surface-alt);">
                <tr>
                  <th>Clase</th>
                  <th style="text-align: right;">Cantidad</th>
                  <th style="text-align: right;">Precio Unitario</th>
                  <th style="text-align: right;">Subtotal</th>
                </tr>
              </thead>
              <tbody>
                ${(mov.detalles || []).map(d => {
                  const meta = CLASES_BULBO.find(c => c.id === _parseClaseIndex(d.claseBulbo)) || { label: `Clase ${d.claseBulbo}` };
                  return `
                    <tr>
                      <td>${escapeHtml(meta.label)}</td>
                      <td style="text-align: right; font-weight: 600;">${(Number(d.cantidad) || 0).toLocaleString('es-AR')}</td>
                      <td style="text-align: right;">$ ${(Number(d.precioUnitarioPactado) || 0).toFixed(2)}</td>
                      <td style="text-align: right; font-weight: 700;">$ ${(Number(d.subtotal) || 0).toFixed(2)}</td>
                    </tr>
                  `;
                }).join('')}
              </tbody>
              <tfoot style="background: var(--surface-alt); font-weight: 700;">
                <tr>
                  <td>TOTAL BULBOS:</td>
                  <td style="text-align: right; color: var(--primary);">${(Number(mov.totalBulbos) || 0).toLocaleString('es-AR')}</td>
                  <td style="text-align: right;">TOTAL:</td>
                  <td style="text-align: right; color: var(--success); font-size: 1.1rem;">
                    $ ${(Number(mov.montoTotal) || 0).toFixed(2)}
                  </td>
                </tr>
              </tfoot>
            </table>
          </div>
          ${mov.tipo === 2 ? `
            <div class="mt-2 text-xs text-muted text-right">
              Bruto: $ ${(Number(mov.montoBruto) || 0).toFixed(2)} | Comisión (${mov.porcentajeComision}%): -$ ${(Number(mov.montoComision) || 0).toFixed(2)} | Neto: $ ${(Number(mov.montoNeto) || 0).toFixed(2)}
            </div>
          ` : ''}
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-md">
          <div>
            ${contratoHtml}
            ${facturaHtml}
          </div>
          <div>
            ${timelineHtml}
          </div>
        </div>

        ${mov.observaciones ? `
          <div class="card p-3 mt-3">
            <strong class="text-xs uppercase text-muted d-block mb-1">Observaciones Generales</strong>
            <p class="text-sm m-0 text-muted">${escapeHtml(mov.observaciones)}</p>
          </div>
        ` : ''}
      `;

      // Botón add contrato desde detalle
      body.querySelector('#btn-modal-add-contrato')?.addEventListener('click', () => {
        this._closeModalDetalle();
        this._abrirModalAccion('contrato', mov.id);
      });

      // Botón add factura desde detalle
      body.querySelector('#btn-modal-add-factura')?.addEventListener('click', () => {
        this._closeModalDetalle();
        this._abrirModalAccion('facturar', mov.id);
      });

      // Renderizar botones de acción de workflow en el footer del modal de detalle
      let modalFooter = this.container.querySelector('#modal-detalle-footer');
      if (!modalFooter) {
        modalFooter = document.createElement('div');
        modalFooter.className = 'modal-footer';
        modalFooter.id = 'modal-detalle-footer';
        modal.querySelector('.modal-dialog')?.appendChild(modalFooter);
      }

      const actionBtns = this._renderDetailActionButtons(mov);
      modalFooter.innerHTML = `
        <div style="width: 100%; display: flex; justify-content: space-between; align-items: center; gap: var(--spacing-sm); flex-wrap: wrap;">
          <div>
            <button type="button" class="btn btn-secondary btn-sm" id="btn-detalle-cerrar">Cerrar</button>
          </div>
          <div class="d-flex align-center gap-sm flex-wrap">
            ${actionBtns.join('')}
          </div>
        </div>
      `;

      modalFooter.querySelector('#btn-detalle-cerrar')?.addEventListener('click', () => {
        this._closeModalDetalle();
      });

      modalFooter.querySelector('#btn-detalle-aprobar')?.addEventListener('click', () => {
        this._closeModalDetalle();
        this._abrirModalAccion('aprobar', mov.id);
      });

      modalFooter.querySelector('#btn-detalle-facturar')?.addEventListener('click', () => {
        this._closeModalDetalle();
        this._abrirModalAccion('facturar', mov.id);
      });

      modalFooter.querySelector('#btn-detalle-finalizar')?.addEventListener('click', () => {
        this._closeModalDetalle();
        this._abrirModalAccion('finalizar', mov.id);
      });

      modalFooter.querySelector('#btn-detalle-rechazar')?.addEventListener('click', () => {
        this._closeModalDetalle();
        this._abrirModalAccion('rechazar', mov.id);
      });

      modalFooter.querySelector('#btn-detalle-corregir')?.addEventListener('click', async () => {
        this._closeModalDetalle();
        await this._openModalMovimiento(mov.id);
      });

    } catch (err) {
      console.error('[MovimientosComercialesView] Error al cargar detalle:', err);
      body.innerHTML = `<div class="text-center text-danger p-4">${escapeHtml(err.message || 'Error al obtener detalle')}</div>`;
      const modalFooter = this.container.querySelector('#modal-detalle-footer');
      if (modalFooter) {
        modalFooter.innerHTML = `
          <div style="width: 100%; display: flex; justify-content: flex-end;">
            <button type="button" class="btn btn-secondary btn-sm" id="btn-detalle-cerrar">Cerrar</button>
          </div>
        `;
        modalFooter.querySelector('#btn-detalle-cerrar')?.addEventListener('click', () => this._closeModalDetalle());
      }
    }
  }

  _closeModalDetalle() {
    const modal = this.container.querySelector('#modal-detalle-movimiento');
    modal?.classList.remove('is-active');
  }

  // ===========================================================================
  // MODALES DE ACCIÓN ESPECÍFICA (APROBAR, CONTRATO, FACTURAR, FINALIZAR, RECHAZAR)
  // ===========================================================================

  _abrirModalAccion(tipo, movimientoId) {
    let mov = this.movimientos.find(m => Number(m.id) === Number(movimientoId));
    if (!mov && this.activeMovimiento && Number(this.activeMovimiento.id) === Number(movimientoId)) {
      mov = this.activeMovimiento;
    }
    if (!mov) return;
    this.activeMovimiento = mov;

    const modal = this.container.querySelector('#modal-accion-circuito');
    const title = this.container.querySelector('#modal-accion-title');
    const body = this.container.querySelector('#modal-accion-body');
    if (!modal || !title || !body) return;

    if (tipo === 'aprobar') {
      title.textContent = `Aprobar Operación Comercial #${mov.id}`;
      body.innerHTML = `
        <p class="text-sm text-muted mb-3">
          Confirma la aprobación administrativa del movimiento de <strong>${escapeHtml(TIPOS_MOVIMIENTO[mov.tipo]?.label || '')}</strong> por un total de <strong>${(Number(mov.totalBulbos) || 0).toLocaleString('es-AR')} bulbos</strong>.
        </p>
        ${mov.tipo === 1 && mov.requiereContrato && !mov.contrato ? `
          <div class="alert alert-danger p-3 mb-3 text-xs">
            <strong>Atención:</strong> Este cliente es nuevo y requiere registrar previamente el contrato de compraventa antes de la aprobación.
          </div>
        ` : ''}
        <form id="form-accion-aprobar">
          <div class="form-group mb-3">
            <label for="input-obs-accion" class="form-label text-xs font-bold uppercase text-muted">Observaciones de Aprobación (Opcional)</label>
            <input type="text" id="input-obs-accion" class="form-control" placeholder="ej. Aprobado conforme a lista de precios oficial..." />
          </div>
          <div class="d-flex justify-end gap-sm">
            <button type="button" class="btn btn-secondary btn-sm" id="btn-cancel-accion">Cancelar</button>
            <button type="submit" class="btn btn-primary btn-sm">Confirmar Aprobación</button>
          </div>
        </form>
      `;

    } else if (tipo === 'contrato') {
      title.textContent = `Registrar Contrato de Compraventa - Movimiento #${mov.id}`;
      body.innerHTML = `
        <form id="form-accion-contrato">
          <div class="form-group mb-3">
            <label for="input-num-contrato" class="form-label text-xs font-bold uppercase text-muted">Número de Contrato *</label>
            <input type="text" id="input-num-contrato" class="form-control" placeholder="ej. CTR-2026-0042" required />
          </div>
          <div class="form-group mb-3">
            <label for="input-url-contrato" class="form-label text-xs font-bold uppercase text-muted">URL Archivo Digital (Opcional)</label>
            <input type="text" id="input-url-contrato" class="form-control" placeholder="ej. https://documentos.azafran.com/contratos/ctr-0042.pdf" />
          </div>
          <div class="d-flex justify-end gap-sm">
            <button type="button" class="btn btn-secondary btn-sm" id="btn-cancel-accion">Cancelar</button>
            <button type="submit" class="btn btn-primary btn-sm">Guardar Contrato</button>
          </div>
        </form>
      `;

    } else if (tipo === 'facturar') {
      title.textContent = `Emitir Factura Comercial - Movimiento #${mov.id}`;
      body.innerHTML = `
        <form id="form-accion-factura">
          <div class="form-group mb-3">
            <label for="input-num-factura" class="form-label text-xs font-bold uppercase text-muted">Número de Factura *</label>
            <input type="text" id="input-num-factura" class="form-control" placeholder="ej. FC-A-0001-00004291" required />
          </div>
          <div class="form-group mb-3">
            <label for="input-url-factura" class="form-label text-xs font-bold uppercase text-muted">URL Comprobante Digital (Opcional)</label>
            <input type="text" id="input-url-factura" class="form-control" placeholder="ej. https://afip.gob.ar/factura-00004291.pdf" />
          </div>
          <div class="form-group mb-3">
            <label for="input-obs-factura" class="form-label text-xs font-bold uppercase text-muted">Observaciones Contables</label>
            <input type="text" id="input-obs-factura" class="form-control" placeholder="ej. Condición de pago 30 días..." />
          </div>
          <div class="d-flex justify-end gap-sm">
            <button type="button" class="btn btn-secondary btn-sm" id="btn-cancel-accion">Cancelar</button>
            <button type="submit" class="btn btn-primary btn-sm">Emitir Factura</button>
          </div>
        </form>
      `;

    } else if (tipo === 'finalizar') {
      title.textContent = `Finalizar Operación y Entregar Stock - Movimiento #${mov.id}`;
      body.innerHTML = `
        <div class="alert alert-success d-flex align-start gap-sm p-3 mb-3" style="background: rgba(34, 197, 94, 0.1); border-left: 4px solid var(--success); border-radius: var(--radius-sm);">
          <span style="font-size: 1.2rem;">📦</span>
          <div class="text-sm">
            <strong>Impacto Físico en Inventario:</strong> Al confirmar este paso, los <strong>${(Number(mov.totalBulbos) || 0).toLocaleString('es-AR')} bulbos</strong> (incluyendo la descomposición física de calibres MIX) serán descontados del titular de origen e incorporados al inventario de destino de forma irreversible.
          </div>
        </div>
        <form id="form-accion-finalizar">
          <div class="form-group mb-3">
            <label for="input-obs-accion" class="form-label text-xs font-bold uppercase text-muted">Observaciones de Entrega Final</label>
            <input type="text" id="input-obs-accion" class="form-control" placeholder="ej. Entrega presencial en depósito central..." />
          </div>
          <div class="d-flex justify-end gap-sm">
            <button type="button" class="btn btn-secondary btn-sm" id="btn-cancel-accion">Cancelar</button>
            <button type="submit" class="btn btn-success btn-sm">Finalizar e Impactar Inventario</button>
          </div>
        </form>
      `;

    } else if (tipo === 'rechazar') {
      title.textContent = `Rechazar u Observar Operación #${mov.id}`;
      body.innerHTML = `
        <div class="d-flex align-center gap-sm mb-3">
          ${getProhibitionIcon({ size: 'md', title: 'Rechazo' })}
          <span class="text-sm text-danger font-bold">
            Indique obligatoriamente el motivo formal del rechazo u observación.
          </span>
        </div>
        <form id="form-accion-rechazar">
          <div class="form-group mb-3">
            <label for="input-motivo-rechazo" class="form-label text-xs font-bold uppercase text-muted">Motivo del Rechazo *</label>
            <textarea id="input-motivo-rechazo" class="form-control" rows="3" placeholder="Indique claramente la razón por la que se rechaza la operación..." required></textarea>
          </div>
          <div class="d-flex justify-end gap-sm">
            <button type="button" class="btn btn-secondary btn-sm" id="btn-cancel-accion">Cancelar</button>
            <button type="submit" class="btn btn-danger btn-sm">Confirmar Rechazo</button>
          </div>
        </form>
      `;
    }

    body.querySelector('#btn-cancel-accion')?.addEventListener('click', () => this._closeModalAccion());
    this._bindFormAccionSubmit(tipo, mov.id);
    modal.classList.add('is-active');
  }

  _closeModalAccion() {
    const modal = this.container.querySelector('#modal-accion-circuito');
    modal?.classList.remove('is-active');
    this.activeMovimiento = null;
  }

  _bindFormAccionSubmit(tipo, movId) {
    const body = this.container.querySelector('#modal-accion-body');
    if (!body) return;

    if (tipo === 'aprobar') {
      body.querySelector('#form-accion-aprobar')?.addEventListener('submit', async (e) => {
        e.preventDefault();
        const obs = body.querySelector('#input-obs-accion')?.value.trim() || '';
        try {
          await movimientosComercialesService.aprobar(movId, { observaciones: obs });
          toast.success(`Movimiento #${movId} aprobado exitosamente.`);
          this._closeModalAccion();
          await this._loadMovimientos();
        } catch (err) {
          toast.error('Error al aprobar: ' + (err.message || 'Verifique los requisitos'));
        }
      });
    } else if (tipo === 'contrato') {
      body.querySelector('#form-accion-contrato')?.addEventListener('submit', async (e) => {
        e.preventDefault();
        const num = body.querySelector('#input-num-contrato')?.value.trim();
        const url = body.querySelector('#input-url-contrato')?.value.trim() || null;
        if (!num) {
          toast.warning('El número de contrato es obligatorio.');
          return;
        }
        try {
          await movimientosComercialesService.generarContrato(movId, { numeroContrato: num, archivoUrl: url });
          toast.success(`Contrato registrado para el movimiento #${movId}.`);
          this._closeModalAccion();
          await this._loadMovimientos();
        } catch (err) {
          toast.error('Error al registrar contrato: ' + (err.message || 'Error'));
        }
      });
    } else if (tipo === 'facturar') {
      body.querySelector('#form-accion-factura')?.addEventListener('submit', async (e) => {
        e.preventDefault();
        const num = body.querySelector('#input-num-factura')?.value.trim();
        const url = body.querySelector('#input-url-factura')?.value.trim() || null;
        const obs = body.querySelector('#input-obs-factura')?.value.trim() || null;
        if (!num) {
          toast.warning('El número de factura es obligatorio.');
          return;
        }
        try {
          await movimientosComercialesService.facturar(movId, { numeroFactura: num, archivoUrl: url, observaciones: obs });
          toast.success(`Factura emitida para el movimiento #${movId}.`);
          this._closeModalAccion();
          await this._loadMovimientos();
        } catch (err) {
          toast.error('Error al facturar: ' + (err.message || 'Error'));
        }
      });
    } else if (tipo === 'finalizar') {
      body.querySelector('#form-accion-finalizar')?.addEventListener('submit', async (e) => {
        e.preventDefault();
        const obs = body.querySelector('#input-obs-accion')?.value.trim() || '';
        try {
          await movimientosComercialesService.finalizar(movId, { observaciones: obs });
          toast.success(`Movimiento #${movId} finalizado. Inventario físico actualizado exitosamente.`);
          this._closeModalAccion();
          await this._loadMovimientos();
        } catch (err) {
          toast.error('Error al finalizar: ' + (err.message || 'Error en stock'));
        }
      });
    } else if (tipo === 'rechazar') {
      body.querySelector('#form-accion-rechazar')?.addEventListener('submit', async (e) => {
        e.preventDefault();
        const motivo = body.querySelector('#input-motivo-rechazo')?.value.trim();
        if (!motivo) {
          toast.warning('Debe indicar el motivo del rechazo.');
          return;
        }
        try {
          await movimientosComercialesService.rechazar(movId, { motivo });
          toast.success(`Movimiento #${movId} rechazado / observado.`);
          this._closeModalAccion();
          await this._loadMovimientos();
        } catch (err) {
          toast.error('Error al rechazar: ' + (err.message || 'Error'));
        }
      });
    }
  }
}
