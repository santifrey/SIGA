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
import { sucursalesService } from '../services/sucursales-service.js';
import { stockService, EMPRESA_SINGLETON_ID } from '../services/stock-service.js';
import { preciosReferenciaService } from '../services/precios-referencia-service.js';
import { contratoGeneratorService } from '../services/contrato-generator-service.js';
import {
  movimientosComercialesService,
  TIPOS_MOVIMIENTO,
  ESTADOS_MOVIMIENTO,
  CLASES_BULBO,
  ACCIONES_APROBACION,
  _parseTipoIndex,
  _parseEstadoIndex,
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
        <div class="grid grid-cols-2 md:grid-cols-6 gap-sm mb-4" id="kpi-counters-container" style="display: grid; grid-template-columns: repeat(6, minmax(0, 1fr)); gap: var(--spacing-sm);">
          <div class="card metric-card p-3 cursor-pointer" data-kpi-estado="todos" style="cursor: pointer; flex-direction: column; align-items: flex-start; gap: 0.25rem; padding: 0.875rem 1rem; transition: transform var(--transition-fast);">
            <div class="metric-label">Total Registrados</div>
            <div class="metric-value" id="kpi-total" style="font-size: 1.6rem;">0</div>
            <div class="text-xs text-muted">Todos los estados</div>
          </div>
          <div class="card metric-card warning p-3 cursor-pointer" data-kpi-estado="0" style="cursor: pointer; flex-direction: column; align-items: flex-start; gap: 0.25rem; padding: 0.875rem 1rem; border-left: 4px solid var(--warning);">
            <div class="metric-label" style="color: var(--warning);">Pend. Contrato / Aprob.</div>
            <div class="metric-value" id="kpi-pendientes" style="font-size: 1.6rem; color: var(--warning);">0</div>
            <div class="text-xs text-muted">Contrato / Revisión</div>
          </div>
          <div class="card metric-card info p-3 cursor-pointer" data-kpi-estado="1" style="cursor: pointer; flex-direction: column; align-items: flex-start; gap: 0.25rem; padding: 0.875rem 1rem; border-left: 4px solid var(--info);">
            <div class="metric-label" style="color: var(--info);">Pend. Factura</div>
            <div class="metric-value" id="kpi-aprobados" style="font-size: 1.6rem; color: var(--info);">0</div>
            <div class="text-xs text-muted">Aprobado comercial</div>
          </div>
          <div class="card metric-card primary p-3 cursor-pointer" data-kpi-estado="2" style="cursor: pointer; flex-direction: column; align-items: flex-start; gap: 0.25rem; padding: 0.875rem 1rem; border-left: 4px solid var(--primary);">
            <div class="metric-label" style="color: var(--primary);">Pend. Aprob. Factura</div>
            <div class="metric-value" id="kpi-facturados" style="font-size: 1.6rem; color: var(--primary);">0</div>
            <div class="text-xs text-muted">Revisión contable</div>
          </div>
          <div class="card metric-card success p-3 cursor-pointer" data-kpi-estado="3" style="cursor: pointer; flex-direction: column; align-items: flex-start; gap: 0.25rem; padding: 0.875rem 1rem; border-left: 4px solid var(--success);">
            <div class="metric-label" style="color: var(--success);">Completados</div>
            <div class="metric-value" id="kpi-finalizados" style="font-size: 1.6rem; color: var(--success);">0</div>
            <div class="text-xs text-muted">Stock impactado</div>
          </div>
          <div class="card metric-card danger p-3 cursor-pointer" data-kpi-estado="4" style="cursor: pointer; flex-direction: column; align-items: flex-start; gap: 0.25rem; padding: 0.875rem 1rem; border-left: 4px solid var(--danger);">
            <div class="metric-label" style="color: var(--danger);">Rechazados</div>
            <div class="metric-value" id="kpi-rechazados" style="font-size: 1.6rem; color: var(--danger);">0</div>
            <div class="text-xs text-muted">Observados / Anulados</div>
          </div>
        </div>

        <!-- ===================================================================
             BARRA DE FILTROS AVANZADOS
             =================================================================== -->
        <div class="card mb-4" style="padding: 1.25rem;">
          <div class="grid grid-cols-1 md:grid-cols-4 lg:grid-cols-7 gap-sm align-center" style="display: grid; grid-template-columns: repeat(auto-fit, minmax(140px, 1fr)); gap: var(--spacing-sm);">
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
                <option value="0">Pendiente Contrato / Aprobación</option>
                <option value="1">Pendiente Factura</option>
                <option value="2">Pendiente Aprobación Factura</option>
                <option value="3">Completado</option>
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
                <div class="grid grid-cols-1 md:grid-cols-2 gap-md mb-3" id="participantes-container" style="display: grid; grid-template-columns: repeat(auto-fit, minmax(240px, 1fr)); gap: var(--spacing-md);">
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

        <!-- ===================================================================
             MODAL DE CREACIÓN DE CLIENTE (REUTILIZADO DE LA VISTA CLIENTES)
             =================================================================== -->
        <div id="modal-cliente" class="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="modal-cliente-title" style="z-index: 100010 !important;">
          <div class="modal-dialog modal-lg">
            <div class="modal-header">
              <h3 class="modal-title" id="modal-cliente-title">Nuevo Cliente</h3>
              <button type="button" class="modal-close-btn" id="btn-close-modal-cliente" aria-label="Cerrar modal">
                <svg class="icon-svg sm" viewBox="0 0 24 24"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
              </button>
            </div>
            <form id="form-cliente" novalidate>
              <div class="modal-body" style="max-height: calc(85vh - 130px); overflow-y: auto;">
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

                  <div class="form-group mb-3">
                    <label for="cliente-contrato-id" class="form-label">
                      ID de Contrato Vinculado
                      <span class="label-hint">(Opcional / Clave foránea al contrato asociado)</span>
                    </label>
                    <input type="number" id="cliente-contrato-id" class="form-control" placeholder="ej. 1, 2, ..." min="1" />
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

    // Modal nuevo cliente (reutilizado de Clientes)
    this.container.querySelector('#btn-close-modal-cliente')?.addEventListener('click', () => this._closeModalNuevoCliente());
    this.container.querySelector('#btn-cancel-modal-cliente')?.addEventListener('click', () => this._closeModalNuevoCliente());
    this.container.querySelector('#form-cliente')?.addEventListener('submit', (e) => this._handleSubmitNuevoCliente(e));

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
    const count = (st) => this.movimientos.filter(m => _parseEstadoIndex(m.estado ?? m.nombreEstado) === st).length;

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

  _resolverNombreParticipante(participanteId, rawNombre, mov = null) {
    const idNum = Number(participanteId);
    if (idNum === 1) {
      if (rawNombre && !rawNombre.startsWith('Participante #') && !rawNombre.startsWith('Cliente #')) {
        return rawNombre;
      }
      return 'Empresa Central';
    }
    const cli = this.clientes.find(c => Number(c.id) === idNum || (rawNombre && (c.codigo === rawNombre || c.codigoCliente === rawNombre)));
    if (cli) {
      const fullName = cli.nombreCompleto || `${cli.nombre || ''} ${cli.apellido || ''}`.trim();
      const cod = cli.codigo || cli.codigoCliente;
      if (fullName) {
        return cod ? `${fullName} (${cod})` : fullName;
      }
    }
    if (mov && Number(mov.clienteId) === idNum && mov.nombreCliente && !mov.nombreCliente.startsWith('CLI-') && !mov.nombreCliente.startsWith('Participante #')) {
      return mov.codigoCliente ? `${mov.nombreCliente} (${mov.codigoCliente})` : mov.nombreCliente;
    }
    if (rawNombre && !rawNombre.startsWith('Participante #') && !rawNombre.startsWith('CLI-')) {
      return rawNombre;
    }
    return rawNombre || `Cliente #${participanteId}`;
  }

  _renderTable() {
    const tbody = this.container.querySelector('#tbody-movimientos');
    if (!tbody) return;

    let list = [...this.movimientos];

    // Búsqueda de texto en memoria
    if (this.filtro.search && this.filtro.search.trim() !== '') {
      const q = this.filtro.search.trim().toLowerCase();
      list = list.filter(m => {
        const origenLabel = this._resolverNombreParticipante(m.origenParticipanteId, m.nombreOrigen, m);
        const destinoLabel = this._resolverNombreParticipante(m.destinoParticipanteId, m.nombreDestino, m);
        const idMatch = String(m.id).includes(q);
        const origMatch = String(origenLabel || '').toLowerCase().includes(q) || String(m.nombreOrigen || '').toLowerCase().includes(q);
        const destMatch = String(destinoLabel || '').toLowerCase().includes(q) || String(m.nombreDestino || '').toLowerCase().includes(q);
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
      const tipoNum = _parseTipoIndex(m.tipo ?? m.nombreTipo);
      const estadoNum = _parseEstadoIndex(m.estado ?? m.nombreEstado);
      const tipoMeta = TIPOS_MOVIMIENTO[tipoNum] || TIPOS_MOVIMIENTO[m.tipo] || { label: `Tipo ${m.tipo}`, badgeClass: 'badge-secondary' };
      const estadoMeta = ESTADOS_MOVIMIENTO[estadoNum] || ESTADOS_MOVIMIENTO[m.estado] || { label: `Estado ${m.estado}`, badgeClass: 'badge-secondary' };
      const fReg = m.fechaRegistro ? new Date(m.fechaRegistro).toLocaleDateString('es-AR') : '-';

      const totalBulbos = (Number(m.totalBulbos) || 0).toLocaleString('es-AR');
      const montoTotal = Number(m.montoTotal) || 0;
      const montoFmt = tipoNum === 3
        ? '<span class="text-muted">$ 0,00</span>'
        : `$ ${montoTotal.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;

      // Friendly state resolution según el ciclo de vida de 5 etapas
      let friendlyEstadoLabel = estadoMeta.label;
      let friendlyBadgeClass = estadoMeta.badgeClass;

      if (estadoNum === 0) {
        if (tipoNum === 1 && m.requiereContrato && (!m.contrato || m.contrato.isDeleted)) {
          friendlyEstadoLabel = 'Pendiente Contrato';
          friendlyBadgeClass = 'badge-warning';
        } else {
          friendlyEstadoLabel = 'Pendiente Aprobación Contrato';
          friendlyBadgeClass = 'badge-info';
        }
      } else if (estadoNum === 1) {
        friendlyEstadoLabel = tipoNum !== 3 ? 'Pendiente Factura' : 'Pendiente Finalización';
        friendlyBadgeClass = 'badge-primary';
      } else if (estadoNum === 2) {
        friendlyEstadoLabel = 'Pendiente Aprobación Factura';
        friendlyBadgeClass = 'badge-purple';
      } else if (estadoNum === 3) {
        friendlyEstadoLabel = 'Completado';
        friendlyBadgeClass = 'badge-success';
      } else if (estadoNum === 4) {
        const isFacturaAnulada = m.factura && (m.factura.estado === 2 || m.factura.estado === 'Anulada' || m.factura.nombreEstado === 'Anulada');
        friendlyEstadoLabel = isFacturaAnulada ? 'Rechazado (Factura Anulada)' : 'Rechazado';
        friendlyBadgeClass = 'badge-danger';
      }

      // Comprobantes indicators
      const contratoBadge = m.contrato
        ? `<span class="badge badge-success text-xs cursor-pointer btn-badge-contrato" data-id="${m.id}" style="cursor: pointer;" title="Descargar Contrato #${escapeHtml(m.contrato.numeroContrato)}">Contrato: #${escapeHtml(m.contrato.numeroContrato)}</span>`
        : (tipoNum === 1 && m.requiereContrato ? `<span class="badge badge-warning text-xs" title="Requiere contrato previo">Req. Contrato</span>` : '<span class="text-muted text-xs">-</span>');

      const isFacturaAnulada = m.factura && (m.factura.estado === 2 || m.factura.estado === 'Anulada' || m.factura.nombreEstado === 'Anulada');
      const facturaBadge = !m.factura
        ? '<span class="text-muted text-xs">-</span>'
        : (isFacturaAnulada
            ? `<span class="badge badge-danger text-xs" title="Factura #${escapeHtml(m.factura.numeroFactura)} (Anulada)">Factura Anulada: #${escapeHtml(m.factura.numeroFactura)}</span>`
            : `<span class="badge badge-primary text-xs" title="Factura #${escapeHtml(m.factura.numeroFactura)}">Factura: #${escapeHtml(m.factura.numeroFactura)}</span>`);

      const origenLabel = this._resolverNombreParticipante(m.origenParticipanteId, m.nombreOrigen, m);
      const destinoLabel = this._resolverNombreParticipante(m.destinoParticipanteId, m.nombreDestino, m);

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
            <span class="badge ${friendlyBadgeClass} text-xs">
              <span class="badge-dot"></span>
              ${escapeHtml(friendlyEstadoLabel)}
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
    const st = _parseEstadoIndex(m.estado ?? m.nombreEstado);
    const tipo = _parseTipoIndex(m.tipo ?? m.nombreTipo);
    const currentUser = authService.getUsuarioActual();
    const isCreator = currentUser && Number(currentUser.id) === Number(m.usuarioRegistroId);

    // 1. Estado 0: Pendiente Contrato / Pendiente Aprobación Contrato
    if (st === 0) {
      if (authService.tienePermiso('MOVCOM_APROBAR')) {
        const reqContrato = tipo === 1 && m.requiereContrato;
        const tieneContrato = Boolean(m.contrato && !m.contrato.isDeleted);

        if (reqContrato && !tieneContrato) {
          btns.push(`
            <button type="button" class="btn btn-warning btn-sm btn-action-contrato" data-id="${m.id}" title="Generar Contrato Comercial PDF">
              <span>Generar Contrato</span>
            </button>
          `);
        } else {
          if (tipo === 1 && tieneContrato) {
            btns.push(`
              <button type="button" class="btn btn-outline btn-sm btn-action-ver-contrato" data-id="${m.id}" title="Descargar / Ver Contrato PDF">
                <span>Ver Contrato</span>
              </button>
            `);
          }
          btns.push(`
            <button type="button" class="btn btn-primary btn-sm btn-action-aprobar" data-id="${m.id}" title="Aprobar contrato / operación y avanzar a Pendiente Factura">
              <span>Aprobar Contrato</span>
            </button>
          `);
        }
      }
      if (authService.tienePermiso('MOVCOM_RECHAZAR')) {
        btns.push(`
          <button type="button" class="btn btn-outline btn-sm btn-action-rechazar" data-id="${m.id}" title="Rechazar u Observar Operación">
            ${getProhibitionIcon({ size: 'sm', title: 'Rechazar' })}
          </button>
        `);
      }
    }

    // 2. Estado 1: Pendiente Factura (o Pendiente Finalización en Traspaso)
    if (st === 1) {
      if (tipo === 1 && m.contrato) {
        btns.push(`
          <button type="button" class="btn btn-outline btn-sm btn-action-ver-contrato" data-id="${m.id}" title="Descargar / Ver Contrato PDF">
            <span>Ver Contrato</span>
          </button>
        `);
      }
      if (tipo !== 3) {
        if (authService.tienePermiso('MOVCOM_FACTURAR')) {
          btns.push(`
            <button type="button" class="btn btn-primary btn-sm btn-action-facturar" data-id="${m.id}" title="Emitir Factura Comercial">
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
          <button type="button" class="btn btn-outline btn-sm btn-action-rechazar" data-id="${m.id}" title="Rechazar u Observar Operación">
            ${getProhibitionIcon({ size: 'sm', title: 'Rechazar' })}
          </button>
        `);
      }
    }

    // 3. Estado 2: Pendiente Aprobación Factura
    if (st === 2 && tipo !== 3) {
      if (tipo === 1 && m.contrato) {
        btns.push(`
          <button type="button" class="btn btn-outline btn-sm btn-action-ver-contrato" data-id="${m.id}" title="Descargar / Ver Contrato PDF">
            <span>Ver Contrato</span>
          </button>
        `);
      }
      if (authService.tienePermiso('MOVCOM_FINALIZAR')) {
        btns.push(`
          <button type="button" class="btn btn-success btn-sm btn-action-finalizar" data-id="${m.id}" title="Aprobar Factura y Finalizar Operación (Impacta stock físico)">
            <span>Aprobar Factura</span>
          </button>
        `);
      }
      if (authService.tienePermiso('MOVCOM_RECHAZAR')) {
        btns.push(`
          <button type="button" class="btn btn-outline-danger btn-sm btn-action-rechazar" data-id="${m.id}" title="Rechazar Factura (Anula la factura actual y permite generar una nueva)">
            <span>Rechazar Factura</span>
          </button>
        `);
      }
    }

    // 4. Estado 4: Rechazado -> Nuevo Contrato, Nueva Factura o Corregir Operación
    if (st === 4) {
      if (tipo === 1 && authService.tienePermiso('MOVCOM_APROBAR')) {
        btns.push(`
          <button type="button" class="btn btn-warning btn-sm btn-action-contrato" data-id="${m.id}" title="Regenerar o editar contrato oficial tras rechazo">
            <span>${m.contrato ? 'Editar Contrato' : 'Generar Contrato'}</span>
          </button>
        `);
      }
      if (authService.tienePermiso('MOVCOM_FACTURAR') && tipo !== 3) {
        btns.push(`
          <button type="button" class="btn btn-primary btn-sm btn-action-facturar" data-id="${m.id}" title="Emitir nueva factura comercial corregida">
            <span>Nueva Factura</span>
          </button>
        `);
      }
      if (isCreator || authService.tienePermiso('MOVCOM_CORREGIR')) {
        btns.push(`
          <button type="button" class="btn btn-secondary btn-sm btn-action-corregir" data-id="${m.id}" title="Corregir datos de la operación y reenviar a revisión">
            <span>Corregir Operación</span>
          </button>
        `);
      }
    }

    // 5. Finalizado (3)
    if (st === 3 && tipo === 1 && m.contrato) {
      btns.push(`
        <button type="button" class="btn btn-outline btn-sm btn-action-ver-contrato" data-id="${m.id}" title="Descargar / Ver Contrato PDF">
          <span>Ver Contrato</span>
        </button>
      `);
    }

    return btns.join('');
  }

  _renderDetailActionButtons(mov) {
    const btns = [];
    const st = _parseEstadoIndex(mov.estado ?? mov.nombreEstado);
    const tipo = _parseTipoIndex(mov.tipo ?? mov.nombreTipo);
    const currentUser = authService.getUsuarioActual();
    const isCreator = currentUser && Number(currentUser.id) === Number(mov.usuarioRegistroId);

    // State 0 (PendienteRevision): Contrato, Aprobar (MOVCOM_APROBAR), Rechazar (MOVCOM_RECHAZAR)
    if (st === 0) {
      if (authService.tienePermiso('MOVCOM_APROBAR')) {
        if (tipo === 1 && !mov.contrato) {
          btns.push(`
            <button type="button" class="btn btn-accent btn-sm btn-detalle-action" id="btn-detalle-contrato" title="Generar Contrato de Compraventa basado en Plantilla">
              <span>Generar Contrato</span>
            </button>
          `);
        }
        if (tipo === 1 && mov.contrato) {
          btns.push(`
            <button type="button" class="btn btn-outline btn-sm btn-detalle-action" id="btn-detalle-ver-contrato" title="Descargar / Ver Contrato PDF">
              <span>Ver Contrato PDF</span>
            </button>
          `);
        }
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

    // State 2 (Facturado): Pendiente Aprobación Factura -> Aprobar Factura, Rechazar Factura
    if (st === 2 && tipo !== 3) {
      if (authService.tienePermiso('MOVCOM_FINALIZAR')) {
        btns.push(`
          <button type="button" class="btn btn-success btn-sm btn-detalle-action" id="btn-detalle-finalizar" title="Aprobar factura y finalizar operación (impacta stock físico)">
            <span>Aprobar Factura</span>
          </button>
        `);
      }
      if (authService.tienePermiso('MOVCOM_RECHAZAR')) {
        btns.push(`
          <button type="button" class="btn btn-outline btn-sm btn-detalle-action" id="btn-detalle-rechazar" title="Rechazar factura (Anula la factura actual y permite generar una nueva)">
            ${getProhibitionIcon({ size: 'sm', title: 'Rechazar' })}
            <span style="color: var(--danger); font-weight: 600;">Rechazar Factura</span>
          </button>
        `);
      }
    }

    // State 4 (Rechazado): Nuevo Contrato, Nueva Factura o Corregir Operación
    if (st === 4) {
      if (tipo === 1 && authService.tienePermiso('MOVCOM_APROBAR')) {
        btns.push(`
          <button type="button" class="btn btn-warning btn-sm btn-detalle-action" id="btn-detalle-contrato" title="Regenerar o editar contrato oficial tras rechazo">
            <span>${mov.contrato ? 'Editar Contrato' : 'Generar Contrato'}</span>
          </button>
        `);
      }
      if (authService.tienePermiso('MOVCOM_FACTURAR') && tipo !== 3) {
        btns.push(`
          <button type="button" class="btn btn-primary btn-sm btn-detalle-action" id="btn-detalle-facturar" title="Emitir nueva factura comercial corregida">
            <span>Nueva Factura</span>
          </button>
        `);
      }
      if (isCreator || authService.tienePermiso('MOVCOM_CORREGIR')) {
        btns.push(`
          <button type="button" class="btn btn-secondary btn-sm btn-detalle-action" id="btn-detalle-corregir" title="Corregir datos de la operación y reenviar a revisión">
            <span>Corregir Operación</span>
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

    // Generar Contrato
    tbody.querySelectorAll('.btn-action-contrato').forEach(b => {
      b.addEventListener('click', () => {
        const id = Number(b.getAttribute('data-id'));
        this._abrirModalAccion('contrato', id);
      });
    });

    // Ver / Descargar Contrato Generado
    tbody.querySelectorAll('.btn-action-ver-contrato, .btn-badge-contrato').forEach(b => {
      b.addEventListener('click', (e) => {
        e.stopPropagation();
        const id = Number(b.getAttribute('data-id'));
        this._descargarOVerContrato(id);
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
        this.selectedTipo = _parseTipoIndex(mov.tipo ?? mov.nombreTipo);
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
          <div class="d-flex justify-between align-center" style="margin-bottom: 0.375rem;">
            <label for="mov-destino-id" class="form-label text-xs font-bold uppercase text-muted mb-0">Destino (Cliente Comprador) *</label>
            <button type="button" class="btn btn-outline-primary btn-sm" id="btn-nuevo-cliente-venta" style="padding: 2px 8px; font-size: 0.7rem;" title="Registrar un nuevo cliente">
              + Nuevo Cliente
            </button>
          </div>
          <select id="mov-destino-id" class="form-control" required>
            <option value="">Seleccione cliente comprador...</option>
            ${clientesOptions}
            <option value="__nuevo__">+ Registrar nuevo cliente...</option>
          </select>
        </div>
      `;

      // Cargar stock origen de Empresa para advertencias
      this._cargarStockOrigen(EMPRESA_SINGLETON_ID);

      // Botón + Nuevo Cliente en Venta
      this.container.querySelector('#btn-nuevo-cliente-venta')?.addEventListener('click', () => {
        this._openModalNuevoCliente();
      });

      // Event listener sobre destino para verificar cliente nuevo o abrir modal de creación
      const destSelect = this.container.querySelector('#mov-destino-id');
      destSelect?.addEventListener('change', async (e) => {
        if (e.target.value === '__nuevo__') {
          e.target.value = '';
          avisoCliente.style.display = 'none';
          this._openModalNuevoCliente();
          return;
        }
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

      const movTipoNum = _parseTipoIndex(mov.tipo ?? mov.nombreTipo);
      const movEstadoNum = _parseEstadoIndex(mov.estado ?? mov.nombreEstado);
      const tipoMeta = TIPOS_MOVIMIENTO[movTipoNum] || TIPOS_MOVIMIENTO[mov.tipo] || { label: `Tipo ${mov.tipo}`, badgeClass: 'badge-secondary' };
      const estadoMeta = ESTADOS_MOVIMIENTO[movEstadoNum] || ESTADOS_MOVIMIENTO[mov.estado] || { label: `Estado ${mov.estado}`, badgeClass: 'badge-secondary' };
      const fReg = mov.fechaRegistro ? new Date(mov.fechaRegistro).toLocaleString('es-AR') : '-';

      // Detección de factura anulada y resolución de estado amigable (5 fases)
      const isFacturaAnulada = mov.factura && (mov.factura.estado === 2 || mov.factura.estado === 'Anulada' || (typeof mov.factura.estado === 'string' && mov.factura.estado.toLowerCase().includes('anulad')));
      let friendlyEstadoLabel = estadoMeta.label;
      let friendlyBadgeClass = estadoMeta.badgeClass;
      if (movEstadoNum === 0) {
        if (!mov.contrato && movTipoNum === 1) {
          friendlyEstadoLabel = 'Pendiente Contrato';
          friendlyBadgeClass = 'badge-warning';
        } else {
          friendlyEstadoLabel = 'Pendiente Aprobación Contrato';
          friendlyBadgeClass = 'badge-info';
        }
      } else if (movEstadoNum === 1) {
        friendlyEstadoLabel = 'Pendiente Factura';
        friendlyBadgeClass = 'badge-primary';
      } else if (movEstadoNum === 2) {
        friendlyEstadoLabel = 'Pendiente Aprobación Factura';
        friendlyBadgeClass = 'badge-purple';
      } else if (movEstadoNum === 3) {
        friendlyEstadoLabel = 'Completado';
        friendlyBadgeClass = 'badge-success';
      } else if (movEstadoNum === 4) {
        friendlyEstadoLabel = isFacturaAnulada ? 'Rechazado (Factura Anulada)' : 'Rechazado';
        friendlyBadgeClass = 'badge-danger';
      }

      const origenLabel = this._resolverNombreParticipante(mov.origenParticipanteId, mov.nombreOrigen, mov);
      const destinoLabel = this._resolverNombreParticipante(mov.destinoParticipanteId, mov.nombreDestino, mov);

      // Comprobantes
      const fechaContratoStr = mov.contrato ? (mov.contrato.fechaGeneracion || mov.contrato.fechaEmision) : null;
      let contratoHtml = `
        <div class="card p-3 mb-3">
          <div class="d-flex justify-between align-center mb-1">
            <strong class="text-xs uppercase text-muted">Contrato de Compraventa</strong>
            ${mov.contrato
              ? '<span class="badge badge-success text-xs">Firmado / Registrado</span>'
              : (movTipoNum === 1 && mov.requiereContrato
                ? '<span class="badge badge-warning text-xs">Requerido Obligatorio</span>'
                : '<span class="text-muted text-xs">No Requerido</span>')}
          </div>
          ${mov.contrato ? `
            <div class="text-sm">
              <div><strong>Nº Contrato:</strong> ${escapeHtml(mov.contrato.numeroContrato)}</div>
              <div class="text-xs text-muted">Fecha Generación: ${fechaContratoStr ? new Date(fechaContratoStr).toLocaleString('es-AR') : '-'}</div>
              <div class="d-flex gap-sm mt-2">
                <button type="button" class="btn btn-outline btn-sm" id="btn-modal-descargar-contrato" title="Descargar contrato oficial PDF">
                  <span>📥 Descargar Contrato PDF</span>
                </button>
                <button type="button" class="btn btn-secondary btn-sm" id="btn-modal-regenerar-contrato" title="Regenerar o editar datos de contrato">
                  <span>Editar / Regenerar</span>
                </button>
              </div>
            </div>
          ` : (movTipoNum === 1 && movEstadoNum !== 3 && authService.tienePermiso('MOVCOM_APROBAR') ? `
            <p class="text-xs text-muted mb-2">${mov.requiereContrato ? 'Este cliente nuevo requiere el registro de contrato antes de la aprobación.' : 'Puede registrar un contrato de compraventa asociado a esta operación.'}</p>
            <button type="button" class="btn btn-accent btn-sm" id="btn-modal-add-contrato">
              <span>Generar Contrato</span>
            </button>
          ` : '<span class="text-xs text-muted">No se ha registrado contrato formal.</span>')}
        </div>
      `;

      let facturaHtml = `
        <div class="card p-3 mb-3">
          <div class="d-flex justify-between align-center mb-1">
            <strong class="text-xs uppercase text-muted">Facturación Comercial</strong>
            ${isFacturaAnulada 
              ? '<span class="badge badge-danger text-xs font-bold">Anulada</span>' 
              : (mov.factura ? '<span class="badge badge-primary text-xs font-bold">Emitida</span>' : '<span class="text-muted text-xs">Sin Factura</span>')}
          </div>
          ${mov.factura ? `
            <div class="text-sm">
              <div><strong>Nº Factura:</strong> ${isFacturaAnulada ? `<span style="text-decoration: line-through; opacity: 0.7;">${escapeHtml(mov.factura.numeroFactura)}</span> <span class="badge badge-danger text-xs ml-1">ANULADA</span>` : `<strong>${escapeHtml(mov.factura.numeroFactura)}</strong>`}</div>
              <div class="text-xs text-muted">Fecha Emisión: ${new Date(mov.factura.fechaEmision).toLocaleString('es-AR')}</div>
              ${mov.factura.observaciones ? `<div class="text-xs text-muted">Notas / Motivo: ${escapeHtml(mov.factura.observaciones)}</div>` : ''}
              ${!isFacturaAnulada && mov.factura.archivoUrl ? `<a href="${escapeHtml(mov.factura.archivoUrl)}" target="_blank" class="text-xs text-primary d-inline-block mt-1">Ver Comprobante Digital</a>` : ''}
              ${isFacturaAnulada ? `
                <div class="alert alert-warning p-2 mt-2 mb-2 text-xs" style="background: rgba(245, 158, 11, 0.1); border-left: 3px solid var(--warning); border-radius: var(--radius-sm);">
                  ⚠️ Esta factura fue anulada automáticamente al rechazar la operación. Es necesario emitir una nueva factura comercial corregida.
                </div>
                ${(movEstadoNum === 4 || movEstadoNum === 1) && authService.tienePermiso('MOVCOM_FACTURAR') ? `
                  <button type="button" class="btn btn-primary btn-sm mt-1" id="btn-modal-add-factura">
                    <span>+ Emitir Nueva Factura</span>
                  </button>
                ` : ''}
              ` : ''}
            </div>
          ` : ((movEstadoNum === 1 || movEstadoNum === 4) && movTipoNum !== 3 && authService.tienePermiso('MOVCOM_FACTURAR') ? `
            <p class="text-xs text-muted mb-2">${movEstadoNum === 4 ? 'La operación fue rechazada. Puede emitir una nueva factura comercial corregida.' : 'La operación está aprobada y lista para su facturación contable.'}</p>
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
        <div class="grid grid-cols-1 md:grid-cols-2 gap-md mb-3" style="display: grid; grid-template-columns: repeat(auto-fit, minmax(240px, 1fr)); gap: var(--spacing-md);">
          <div class="card p-3">
            <span class="text-xs text-muted uppercase font-bold d-block mb-1">Identificación</span>
            <div class="d-flex align-center gap-xs mb-2">
              <span class="font-bold text-lg text-main">#${mov.id}</span>
              <span class="badge ${tipoMeta.badgeClass} text-xs font-bold">${escapeHtml(tipoMeta.label)}</span>
              <span class="badge ${friendlyBadgeClass} text-xs font-bold">${escapeHtml(friendlyEstadoLabel)}</span>
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
          ${movTipoNum === 2 ? `
            <div class="mt-2 text-xs text-muted text-right">
              Bruto: $ ${(Number(mov.montoBruto) || 0).toFixed(2)} | Comisión (${mov.porcentajeComision}%): -$ ${(Number(mov.montoComision) || 0).toFixed(2)} | Neto: $ ${(Number(mov.montoNeto) || 0).toFixed(2)}
            </div>
          ` : ''}
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-md" style="display: grid; grid-template-columns: repeat(auto-fit, minmax(240px, 1fr)); gap: var(--spacing-md);">
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

      modalFooter.querySelector('#btn-detalle-contrato')?.addEventListener('click', () => {
        this._closeModalDetalle();
        this._abrirModalAccion('contrato', mov.id);
      });

      modalFooter.querySelector('#btn-detalle-ver-contrato')?.addEventListener('click', () => {
        this._closeModalDetalle();
        this._descargarOVerContrato(mov.id);
      });

      body.querySelector('#btn-modal-add-contrato')?.addEventListener('click', () => {
        this._closeModalDetalle();
        this._abrirModalAccion('contrato', mov.id);
      });

      body.querySelector('#btn-modal-regenerar-contrato')?.addEventListener('click', () => {
        this._closeModalDetalle();
        this._abrirModalAccion('contrato', mov.id);
      });

      body.querySelector('#btn-modal-descargar-contrato')?.addEventListener('click', () => {
        this._descargarOVerContrato(mov.id);
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

  async _abrirModalAccion(tipo, movimientoId) {
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
      const movTipoNum = _parseTipoIndex(mov.tipo ?? mov.nombreTipo);
      title.textContent = `Aprobar Operación Comercial #${mov.id}`;
      body.innerHTML = `
        <p class="text-sm text-muted mb-3">
          Confirma la aprobación administrativa del movimiento de <strong>${escapeHtml(TIPOS_MOVIMIENTO[movTipoNum]?.label || TIPOS_MOVIMIENTO[mov.tipo]?.label || '')}</strong> por un total de <strong>${(Number(mov.totalBulbos) || 0).toLocaleString('es-AR')} bulbos</strong>.
        </p>
        ${movTipoNum === 1 && !mov.contrato ? `
          <div class="alert alert-warning d-flex justify-between align-center gap-sm p-3 mb-3 text-xs" style="background: rgba(245, 158, 11, 0.1); border-left: 4px solid var(--warning); border-radius: var(--radius-sm);">
            <div>
              <strong>Contrato de Compraventa:</strong> ${mov.requiereContrato ? 'Este cliente es nuevo y requiere registrar previamente el contrato antes de aprobar.' : 'Si el cliente es nuevo, recuerde registrar el contrato previo antes de aprobar.'}
            </div>
            <button type="button" class="btn btn-accent btn-sm" id="btn-aprobar-ir-contrato" style="flex-shrink: 0;">
              Generar Contrato
            </button>
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
      body.querySelector('#btn-aprobar-ir-contrato')?.addEventListener('click', () => {
        this._abrirModalAccion('contrato', mov.id);
      });

    } else if (tipo === 'contrato') {
      const dialog = modal.querySelector('.modal-dialog');
      dialog?.classList.add('modal-lg');

      title.textContent = `Generar Contrato para Cliente - Operación #${mov.id}`;

      // Spinner temporal mientras se cargan datos complementarios
      body.innerHTML = `
        <div class="text-center p-4">
          <div class="btn-spinner" style="position: static; border-top-color: var(--primary); width: 28px; height: 28px; margin: 0 auto 12px;"></div>
          <p class="text-sm text-muted">Cargando datos del cliente, sucursales y plantilla de contrato...</p>
        </div>
      `;
      modal.classList.add('is-active');

      let clienteData = null;
      if (mov.clienteId) {
        try {
          clienteData = await clientesService.obtenerPorId(mov.clienteId);
        } catch (e) {
          console.warn('[MovComView] Error al obtener cliente:', e);
        }
      }
      if (!clienteData && this.clientes) {
        clienteData = this.clientes.find(c => Number(c.id) === Number(mov.clienteId));
      }

      let sucursales = [];
      try {
        const sucRes = await sucursalesService.getAll();
        sucursales = Array.isArray(sucRes) ? sucRes : (sucRes?.items || []);
      } catch (e) {
        console.warn('[MovComView] Error al listar sucursales:', e);
      }

      // Precalcular cantidades de cormos a partir de los detalles de la venta
      let cormillos = 0;
      let n1 = 0;
      let n2 = 0;
      let n3 = 0;
      let n4 = 0;

      if (Array.isArray(mov.detalles)) {
        mov.detalles.forEach(d => {
          const cl = _parseClaseIndex(d.claseBulbo ?? d.clase);
          const cant = Number(d.cantidad) || 0;
          if (cl === 0) cormillos += cant;
          else if (cl === 1) n1 += cant;
          else if (cl === 2) n2 += cant;
          else if (cl === 3) n3 += cant;
          else if (cl === 4) n4 += cant;
          else if (cl === 5) {
            // MIX: se descompone 1/3 a Calibre 1, Calibre 2 y Calibre 3
            const tercio = Math.floor(cant / 3);
            n1 += tercio;
            n2 += tercio;
            n3 += (cant - 2 * tercio);
          }
        });
      }

      const anioActual = new Date().getFullYear();
      const numSugerido = mov.contrato?.numeroContrato || `CTR-${anioActual}-${String(mov.id).padStart(4, '0')}`;
      const currentUser = authService.getUsuarioActual();
      const referenteSugerido = currentUser ? (currentUser.nombreCompleto || currentUser.nombre || 'Federico Carlos Paak') : 'Federico Carlos Paak';

      const cliNombre = clienteData ? (clienteData.nombreCompleto || `${clienteData.nombre || ''} ${clienteData.apellido || ''}`).trim() : (mov.nombreCliente || '');
      const cliDoc = clienteData?.documento || clienteData?.dni || clienteData?.cuit || '';
      const cliTel = clienteData?.telefono || '';
      const cliEmail = clienteData?.email || '';
      const cliDir = clienteData?.direccion || '';
      const cliFechaNac = clienteData?.fechaNacimiento || '';
      const cliProf = clienteData?.profesion || 'Productor / Inversor';
      const cliEstCivil = clienteData?.estadoCivil || 'Soltero/a';

      body.innerHTML = `
        <div class="alert alert-info d-flex align-center gap-sm p-3 mb-3 text-xs" style="background: rgba(74, 21, 75, 0.08); border-left: 4px solid var(--primary); border-radius: var(--radius-sm);">
          <span style="font-size: 1.25rem;">📄</span>
          <div>
            <strong>Generador Oficial de Contratos:</strong> Complete los campos requeridos y haga clic en <strong>Generar Contrato</strong> para obtener el archivo PDF oficial basado en el modelo legal <em>MODELO DE CONTRATO PARA CLIENTES</em> con los datos del cliente, sucursal, fecha y el anexo correspondiente.
          </div>
        </div>

        <form id="form-accion-contrato">
          <!-- SECCIÓN 1: DATOS DEL CONTRATO, SUCURSAL Y FECHA -->
          <div class="card p-3 mb-3" style="background: var(--bg-surface, #f9fafb); border: 1px solid var(--border-color, #e5e7eb);">
            <h6 class="text-xs font-bold uppercase text-primary mb-2">1. Datos del Contrato, Sucursal y Período</h6>
            <div class="form-row two-cols mb-2">
              <div class="form-group">
                <label for="input-num-contrato" class="form-label text-xs font-bold uppercase text-muted">Número de Contrato *</label>
                <input type="text" id="input-num-contrato" class="form-control" value="${escapeHtml(numSugerido)}" required />
              </div>
              <div class="form-group">
                <label for="input-anio-inicio" class="form-label text-xs font-bold uppercase text-muted">Año de Inicio *</label>
                <input type="number" id="input-anio-inicio" class="form-control" value="${anioActual}" min="2020" max="2050" required />
              </div>
            </div>
            <div class="form-row two-cols">
              <div class="form-group">
                <label for="select-sucursal-contrato" class="form-label text-xs font-bold uppercase text-muted">Sucursal / Predio</label>
                <select id="select-sucursal-contrato" class="form-control">
                  <option value="Central - Villa General Belgrano">Central - Villa General Belgrano</option>
                  ${sucursales.map(s => `<option value="${escapeHtml(s.nombre || s.codigo)}">${escapeHtml(s.nombre || s.codigo)}</option>`).join('')}
                </select>
              </div>
              <div class="form-group">
                <label for="input-referente-contrato" class="form-label text-xs font-bold uppercase text-muted">Referente Prestador</label>
                <input type="text" id="input-referente-contrato" class="form-control" value="${escapeHtml(referenteSugerido)}" />
              </div>
            </div>
          </div>

          <!-- SECCIÓN 2: DATOS DEL CLIENTE (COMITENTE) -->
          <div class="card p-3 mb-3" style="background: var(--bg-surface, #f9fafb); border: 1px solid var(--border-color, #e5e7eb);">
            <h6 class="text-xs font-bold uppercase text-primary mb-2">2. Datos del Cliente (COMITENTE)</h6>
            <div class="form-row two-cols mb-2">
              <div class="form-group">
                <label for="input-cliente-nombre" class="form-label text-xs font-bold uppercase text-muted">Nombre y Apellido *</label>
                <input type="text" id="input-cliente-nombre" class="form-control" value="${escapeHtml(cliNombre)}" required />
              </div>
              <div class="form-group">
                <label for="input-cliente-doc" class="form-label text-xs font-bold uppercase text-muted">D.N.I. / C.U.I.T. / C.U.I.L. *</label>
                <input type="text" id="input-cliente-doc" class="form-control" value="${escapeHtml(cliDoc)}" required />
              </div>
            </div>
            <div class="form-row two-cols mb-2">
              <div class="form-group">
                <label for="input-cliente-tel" class="form-label text-xs font-bold uppercase text-muted">Teléfono</label>
                <input type="text" id="input-cliente-tel" class="form-control" value="${escapeHtml(cliTel)}" />
              </div>
              <div class="form-group">
                <label for="input-cliente-email" class="form-label text-xs font-bold uppercase text-muted">Domicilio Electrónico (Email)</label>
                <input type="email" id="input-cliente-email" class="form-control" value="${escapeHtml(cliEmail)}" />
              </div>
            </div>
            <div class="form-group mb-2">
              <label for="input-cliente-dir" class="form-label text-xs font-bold uppercase text-muted">Dirección / Domicilio Real</label>
              <input type="text" id="input-cliente-dir" class="form-control" value="${escapeHtml(cliDir)}" placeholder="ej. Ruta Prov. S-210, Villa General Belgrano" />
            </div>
            <div class="form-row three-cols">
              <div class="form-group">
                <label for="input-cliente-fecnac" class="form-label text-xs font-bold uppercase text-muted">Nacido el día</label>
                <input type="text" id="input-cliente-fecnac" class="form-control" value="${escapeHtml(cliFechaNac)}" placeholder="ej. 15/05/1985" />
              </div>
              <div class="form-group">
                <label for="input-cliente-prof" class="form-label text-xs font-bold uppercase text-muted">Profesión</label>
                <input type="text" id="input-cliente-prof" class="form-control" value="${escapeHtml(cliProf)}" />
              </div>
              <div class="form-group">
                <label for="input-cliente-estcivil" class="form-label text-xs font-bold uppercase text-muted">Estado Civil</label>
                <input type="text" id="input-cliente-estcivil" class="form-control" value="${escapeHtml(cliEstCivil)}" />
              </div>
            </div>
          </div>

          <!-- SECCIÓN 3: CLÁUSULA PRIMERA - CANTIDADES DE CORMOS -->
          <div class="card p-3 mb-3" style="background: var(--bg-surface, #f9fafb); border: 1px solid var(--border-color, #e5e7eb);">
            <div class="d-flex justify-between align-center mb-2">
              <h6 class="text-xs font-bold uppercase text-primary">3. Cantidades de Cormos (Cláusula Primera)</h6>
              <span class="text-xs text-muted">Autocompletado desde los ítems de la operación</span>
            </div>
            <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(90px, 1fr)); gap: 8px;">
              <div class="form-group">
                <label for="input-cormos-cormillos" class="form-label text-xs font-bold uppercase text-muted">Cormillos</label>
                <input type="number" id="input-cormos-cormillos" class="form-control font-mono" value="${cormillos}" min="0" />
              </div>
              <div class="form-group">
                <label for="input-cormos-n1" class="form-label text-xs font-bold uppercase text-muted">Calibre 1</label>
                <input type="number" id="input-cormos-n1" class="form-control font-mono" value="${n1}" min="0" />
              </div>
              <div class="form-group">
                <label for="input-cormos-n2" class="form-label text-xs font-bold uppercase text-muted">Calibre 2</label>
                <input type="number" id="input-cormos-n2" class="form-control font-mono" value="${n2}" min="0" />
              </div>
              <div class="form-group">
                <label for="input-cormos-n3" class="form-label text-xs font-bold uppercase text-muted">Calibre 3</label>
                <input type="number" id="input-cormos-n3" class="form-control font-mono" value="${n3}" min="0" />
              </div>
              <div class="form-group">
                <label for="input-cormos-n4" class="form-label text-xs font-bold uppercase text-muted">Calibre 4</label>
                <input type="number" id="input-cormos-n4" class="form-control font-mono" value="${n4}" min="0" />
              </div>
            </div>
          </div>

          <!-- SECCIÓN 4: CHECKBOX PARA ANEXO DE COPROPIETARIOS (ÚLTIMA PÁGINA) -->
          <div class="card p-3 mb-3" style="background: var(--bg-surface, #f9fafb); border: 2px solid var(--border-color, #e5e7eb); border-radius: var(--radius-sm);">
            <label class="form-check-label d-flex align-center gap-sm mb-1" style="font-weight: 600; cursor: pointer; font-size: 0.95rem;">
              <input type="checkbox" id="chk-anexo-copropietarios" class="form-check-input" style="width: 18px; height: 18px; cursor: pointer;" />
              <span>¿Necesita el anexo de copropietarios? (Última página)</span>
            </label>
            <div class="text-xs text-muted" style="margin-left: 26px;">
              Si no se marca, el contrato se emitirá únicamente con sus 3 páginas principales. Si se marca, se anexará la 4ª página oficial para registro y firma de copropietarios.
            </div>

            <div id="section-copropietarios" class="mt-3 pt-3" style="display: none; border-top: 1px dashed var(--border-color, #d1d5db);">
              <h6 class="text-xs font-bold uppercase text-primary mb-2">Datos de Copropietarios (Página 4 - Opcionales)</h6>
              <div class="form-row two-cols mb-2">
                <input type="text" id="coprop-1-nombre" class="form-control" placeholder="Copropietario 1 - Nombre y Apellido" />
                <input type="text" id="coprop-1-dni" class="form-control" placeholder="Copropietario 1 - DNI / CUIT" />
              </div>
              <div class="form-row two-cols mb-2">
                <input type="text" id="coprop-2-nombre" class="form-control" placeholder="Copropietario 2 - Nombre y Apellido" />
                <input type="text" id="coprop-2-dni" class="form-control" placeholder="Copropietario 2 - DNI / CUIT" />
              </div>
              <div class="form-row two-cols">
                <input type="text" id="coprop-3-nombre" class="form-control" placeholder="Copropietario 3 - Nombre y Apellido" />
                <input type="text" id="coprop-3-dni" class="form-control" placeholder="Copropietario 3 - DNI / CUIT" />
              </div>
            </div>
          </div>

          <!-- BOTONES DE ACCIÓN -->
          <div class="d-flex justify-between align-center gap-sm pt-2">
            <button type="button" class="btn btn-secondary btn-sm" id="btn-cancel-accion">Cancelar</button>
            <div class="d-flex gap-sm">
              <button type="button" class="btn btn-accent btn-sm" id="btn-descargar-borrador-pdf" title="Genera el PDF y lo descarga de inmediato para revisión">
                <span>📥 Descargar PDF</span>
              </button>
              <button type="submit" class="btn btn-primary btn-sm" id="btn-submit-contrato">
                <span>Generar Contrato</span>
              </button>
            </div>
          </div>
        </form>
      `;

      const chkAnexo = body.querySelector('#chk-anexo-copropietarios');
      const secCoprop = body.querySelector('#section-copropietarios');
      if (chkAnexo && secCoprop) {
        chkAnexo.addEventListener('change', () => {
          secCoprop.style.display = chkAnexo.checked ? 'block' : 'none';
        });
      }

      body.querySelector('#btn-descargar-borrador-pdf')?.addEventListener('click', async () => {
        const payload = this._getContratoFormData(body);
        if (!payload.numeroContrato) {
          toast.warning('El número de contrato es obligatorio.');
          return;
        }
        try {
          toast.info('Generando documento PDF desde la plantilla...');
          const res = await contratoGeneratorService.generarContrato(payload);
          contratoGeneratorService.guardarContratoCache(mov.id, res.dataUrl, res.filename);
          contratoGeneratorService.descargarPdf(res.blob, res.filename);
          toast.success(`PDF generado exitosamente (${res.pageCount} páginas). Descarga iniciada.`);
        } catch (err) {
          toast.error('Error al generar PDF: ' + (err.message || 'Error'));
        }
      });

    } else if (tipo === 'facturar') {
      const isReemplazo = mov.factura && (mov.factura.estado === 2 || mov.factura.estado === 'Anulada' || (typeof mov.factura.estado === 'string' && mov.factura.estado.toLowerCase().includes('anulad')) || mov.estado === 4 || mov.estado === 'Rechazado');
      title.textContent = isReemplazo ? `Emitir Nueva Factura Comercial (Reemplazo) - Movimiento #${mov.id}` : `Emitir Factura Comercial - Movimiento #${mov.id}`;
      body.innerHTML = `
        ${isReemplazo ? `
          <div class="alert alert-info d-flex align-center gap-sm p-3 mb-3 text-xs" style="background: rgba(30, 64, 175, 0.08); border-left: 4px solid var(--primary); border-radius: var(--radius-sm);">
            <span style="font-size: 1.25rem;">ℹ️</span>
            <div>
              <strong>Re-emisión de Factura:</strong> La factura anterior fue anulada tras el rechazo. Al emitir esta nueva factura, la operación avanzará a <strong>Pendiente Aprobación Factura</strong> para su revisión final.
            </div>
          </div>
        ` : ''}
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
            <button type="submit" class="btn btn-primary btn-sm">${isReemplazo ? 'Emitir Nueva Factura' : 'Emitir Factura'}</button>
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
      const movEstadoNum = _parseEstadoIndex(mov.estado ?? mov.nombreEstado);
      const isRechazoFactura = (movEstadoNum === 2 || mov.factura);
      title.textContent = isRechazoFactura ? `Rechazar Factura / Observar Operación #${mov.id}` : `Rechazar u Observar Operación #${mov.id}`;
      body.innerHTML = `
        <div class="d-flex align-center gap-sm mb-3">
          ${getProhibitionIcon({ size: 'md', title: 'Rechazo' })}
          <span class="text-sm text-danger font-bold">
            Indique obligatoriamente el motivo formal del rechazo u observación.
          </span>
        </div>
        ${mov.factura ? `
          <div class="alert alert-warning p-2 mb-3 text-xs" style="background: rgba(239, 68, 68, 0.08); border-left: 4px solid var(--danger); border-radius: var(--radius-sm);">
            ⚠️ <strong>Atención:</strong> Al confirmar el rechazo, la factura actual <strong>#${escapeHtml(mov.factura.numeroFactura || '')}</strong> quedará marcada automáticamente como <strong>ANULADA</strong>. La operación volverá a estado <em>Rechazado</em> permitiendo emitir una nueva factura comercial corregida o editar los datos de la operación.
          </div>
        ` : ''}
        <form id="form-accion-rechazar">
          <div class="form-group mb-3">
            <label for="input-motivo-rechazo" class="form-label text-xs font-bold uppercase text-muted">Motivo del Rechazo *</label>
            <textarea id="input-motivo-rechazo" class="form-control" rows="3" placeholder="Indique claramente la razón por la que se rechaza la operación o factura..." required></textarea>
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
    const dialog = modal?.querySelector('.modal-dialog');
    dialog?.classList.remove('modal-lg');
    this.activeMovimiento = null;
  }

  _getContratoFormData(body) {
    const chkAnexo = body.querySelector('#chk-anexo-copropietarios')?.checked ?? false;
    return {
      numeroContrato: body.querySelector('#input-num-contrato')?.value.trim() || '',
      anioInicio: body.querySelector('#input-anio-inicio')?.value.trim() || String(new Date().getFullYear()),
      sucursal: body.querySelector('#select-sucursal-contrato')?.value || 'Central - Villa General Belgrano',
      referente: body.querySelector('#input-referente-contrato')?.value.trim() || 'Federico Carlos Paak',
      cliente: {
        nombreCompleto: body.querySelector('#input-cliente-nombre')?.value.trim() || '',
        documento: body.querySelector('#input-cliente-doc')?.value.trim() || '',
        telefono: body.querySelector('#input-cliente-tel')?.value.trim() || '',
        email: body.querySelector('#input-cliente-email')?.value.trim() || '',
        direccion: body.querySelector('#input-cliente-dir')?.value.trim() || '',
        nacidoElDia: body.querySelector('#input-cliente-fecnac')?.value.trim() || '',
        profesion: body.querySelector('#input-cliente-prof')?.value.trim() || '',
        estadoCivil: body.querySelector('#input-cliente-estcivil')?.value.trim() || '',
      },
      cormos: {
        cormillos: body.querySelector('#input-cormos-cormillos')?.value || '0',
        n1: body.querySelector('#input-cormos-n1')?.value || '0',
        n2: body.querySelector('#input-cormos-n2')?.value || '0',
        n3: body.querySelector('#input-cormos-n3')?.value || '0',
        n4: body.querySelector('#input-cormos-n4')?.value || '0',
      },
      incluirAnexoCopropietarios: chkAnexo,
      copropietarios: [
        {
          nombre: body.querySelector('#coprop-1-nombre')?.value.trim() || '',
          dni: body.querySelector('#coprop-1-dni')?.value.trim() || '',
        },
        {
          nombre: body.querySelector('#coprop-2-nombre')?.value.trim() || '',
          dni: body.querySelector('#coprop-2-dni')?.value.trim() || '',
        },
        {
          nombre: body.querySelector('#coprop-3-nombre')?.value.trim() || '',
          dni: body.querySelector('#coprop-3-dni')?.value.trim() || '',
        },
      ],
    };
  }

  async _descargarOVerContrato(movId) {
    let mov = this.movimientos.find(m => Number(m.id) === Number(movId)) || this.activeMovimiento;
    if (!mov) {
      try {
        mov = await movimientosComercialesService.obtenerPorId(movId);
      } catch (e) {}
    }
    if (!mov) return;

    // Verificar si ya está en caché local para descarga directa
    const cached = contratoGeneratorService.obtenerContratoCache(movId);
    if (cached && cached.dataUrl) {
      toast.info('Descargando contrato generado previamente...');
      const cleanNum = (mov.contrato?.numeroContrato || 'CTR').replace(/[^a-zA-Z0-9_-]/g, '_');
      const filename = cached.filename || `Contrato_${cleanNum}.pdf`;
      try {
        const res = await fetch(cached.dataUrl);
        const blob = await res.blob();
        contratoGeneratorService.descargarPdf(blob, filename);
        return;
      } catch (e) {
        console.warn('No se pudo usar caché, abriendo modal:', e);
      }
    }

    // Si no está en caché o se desea regenerar/ver datos, abrir modal
    this._abrirModalAccion('contrato', movId);
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
        const payload = this._getContratoFormData(body);
        if (!payload.numeroContrato) {
          toast.warning('El número de contrato es obligatorio.');
          return;
        }
        if (!payload.cliente.nombreCompleto) {
          toast.warning('El nombre del cliente es obligatorio.');
          return;
        }

        const submitBtn = body.querySelector('#btn-submit-contrato');
        if (submitBtn) {
          submitBtn.disabled = true;
          submitBtn.textContent = 'Generando...';
        }

        try {
          toast.info('Generando documento PDF desde la plantilla oficial...');
          const res = await contratoGeneratorService.generarContrato(payload);

          // Descarga automática del archivo completado
          contratoGeneratorService.descargarPdf(res.blob, res.filename);
          contratoGeneratorService.guardarContratoCache(movId, res.dataUrl, res.filename);

          // Registrar en el backend / servicio
          await movimientosComercialesService.generarContrato(movId, {
            numeroContrato: payload.numeroContrato,
            archivoUrl: res.filename,
          });

          toast.success(`Contrato #${payload.numeroContrato} (${res.pageCount} págs.) generado y asociado con éxito.`);
          this._closeModalAccion();
          await this._loadMovimientos();
        } catch (err) {
          console.error('[MovComView] Error al generar contrato:', err);
          toast.error('Error al generar contrato: ' + (err.message || 'Error desconocido'));
        } finally {
          if (submitBtn) {
            submitBtn.disabled = false;
            submitBtn.textContent = 'Generar Contrato';
          }
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

  // ===========================================================================
  // MODAL DE CREACIÓN DE CLIENTE (REUTILIZADO DE CLIENTES)
  // ===========================================================================

  _openModalNuevoCliente() {
    const form = this.container.querySelector('#form-cliente');
    if (form) form.reset();

    const idInp = this.container.querySelector('#cliente-id');
    if (idInp) idInp.value = '';

    const contratoInp = this.container.querySelector('#cliente-contrato-id');
    if (contratoInp) contratoInp.value = '';

    const titleEl = this.container.querySelector('#modal-cliente-title');
    if (titleEl) titleEl.textContent = 'Nuevo Cliente';

    const activoChk = this.container.querySelector('#cliente-activo');
    if (activoChk) activoChk.checked = true;

    this._clearClienteValidationErrors();

    const modal = this.container.querySelector('#modal-cliente');
    if (modal) {
      modal.classList.add('is-active');
      setTimeout(() => this.container.querySelector('#cliente-nombre')?.focus(), 100);
    }
  }

  _closeModalNuevoCliente() {
    const modal = this.container.querySelector('#modal-cliente');
    modal?.classList.remove('is-active');
  }

  _clearClienteValidationErrors() {
    const inputs = this.container.querySelectorAll('#form-cliente .form-control');
    inputs.forEach(i => i.classList.remove('is-invalid'));
  }

  async _recargarClientesEnSelectores(selectedId = null) {
    try {
      const clientesData = await clientesService.listar({ page: 1, pageSize: 200, estado: 'activos' }).catch(() => ({ items: [] }));
      this.clientes = Array.isArray(clientesData) ? clientesData : (clientesData?.items || []);
    } catch (err) {
      console.warn('[MovimientosComercialesView] Error al recargar clientes:', err);
    }

    // Actualizar filtro de clientes de la vista principal
    const selectClienteFiltro = this.container.querySelector('#filtro-cliente');
    if (selectClienteFiltro) {
      const prevVal = selectClienteFiltro.value;
      selectClienteFiltro.innerHTML = '<option value="todos">Todos los Clientes</option>';
      this.clientes.forEach(c => {
        const opt = document.createElement('option');
        opt.value = String(c.id);
        opt.textContent = `👤 ${c.nombre} ${c.apellido} (${c.codigo || 'CLI-' + c.id})`;
        selectClienteFiltro.appendChild(opt);
      });
      selectClienteFiltro.value = prevVal || 'todos';
    }

    // Actualizar select de destino en modal de venta
    const destSelect = this.container.querySelector('#mov-destino-id');
    if (destSelect && this.selectedTipo === 1) {
      const clientesOptions = this.clientes.map(c =>
        `<option value="${c.id}">👤 ${escapeHtml(c.nombre)} ${escapeHtml(c.apellido)} (${escapeHtml(c.codigo || 'CLI-' + c.id)})</option>`
      ).join('');
      destSelect.innerHTML = `
        <option value="">Seleccione cliente comprador...</option>
        ${clientesOptions}
        <option value="__nuevo__">+ Registrar nuevo cliente...</option>
      `;
      if (selectedId) {
        destSelect.value = String(selectedId);
        await this._verificarClienteNuevoBadge(Number(selectedId));
      }
    }
  }

  async _handleSubmitNuevoCliente(e) {
    e.preventDefault();

    const nombre = this.container.querySelector('#cliente-nombre')?.value.trim() || '';
    const apellido = this.container.querySelector('#cliente-apellido')?.value.trim() || '';
    const tipoDocumento = this.container.querySelector('#cliente-tipo-doc')?.value || 'DNI';
    const documento = this.container.querySelector('#cliente-doc')?.value.trim() || '';
    const email = this.container.querySelector('#cliente-email')?.value.trim() || '';
    const telefono = this.container.querySelector('#cliente-telefono')?.value.trim() || '';
    const codigo = this.container.querySelector('#cliente-codigo')?.value.trim() || '';
    const contratoIdVal = this.container.querySelector('#cliente-contrato-id')?.value.trim() || '';
    const activo = this.container.querySelector('#cliente-activo')?.checked ?? true;
    const observaciones = this.container.querySelector('#cliente-observaciones')?.value.trim() || '';

    let valid = true;
    this._clearClienteValidationErrors();

    if (!nombre) {
      this.container.querySelector('#cliente-nombre')?.classList.add('is-invalid');
      valid = false;
    }
    if (!apellido) {
      this.container.querySelector('#cliente-apellido')?.classList.add('is-invalid');
      valid = false;
    }
    if (!documento) {
      this.container.querySelector('#cliente-doc')?.classList.add('is-invalid');
      valid = false;
    }
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!email || !emailRegex.test(email)) {
      this.container.querySelector('#cliente-email')?.classList.add('is-invalid');
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
      const nuevoCliente = await clientesService.crear(payload);
      toast.success(`Cliente ${nombre} ${apellido} registrado. Acceso: ${email} | Clave: DNI`);

      this._closeModalNuevoCliente();
      await this._recargarClientesEnSelectores(nuevoCliente?.id);
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
