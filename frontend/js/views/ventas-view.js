/**
 * SIGA - views/ventas-view.js
 * Vista integral para Comercialización de Bulbos:
 * - Sección 1: Precios de Referencia por Clase (Regla DP-4)
 * - Sección 2: Registro de Nueva Venta de Bulbos
 * - Sección 3: Listado y Circuito Administrativo (Contratos, Facturación, Aprobaciones)
 */

import { ventasService } from '../services/ventas-service.js';
import { temporadasService } from '../services/temporadas-service.js';
import { clientesService } from '../services/clientes-service.js';
import { toast } from '../utils/toast.js';
import { escapeHtml } from '../utils/html.js';

export class VentasView {
  constructor(router) {
    this.router = router;
    this.container = null;
    this.temporadas = [];
    this.clientes = [];
    this.ventas = [];
    this.preciosClaseActuales = {};
    this.selectedTemporadaPreciosId = null;
    this.selectedTemporadaVentaId = null;
    this.currentModalVenta = null;
    this.filterEstado = 'todos';
  }

  async render(rootElement) {
    this.container = rootElement;
    this.container.innerHTML = `
      <div class="fade-in">
        <!-- Encabezado de la Vista -->
        <div class="d-flex justify-between align-center mb-4 flex-wrap gap-md">
          <div>
            <h1 style="font-size: 1.5rem; margin-bottom: 0.25rem;">Ventas y Circuito Administrativo</h1>
            <p class="text-muted text-sm">Precios de referencia por clase, contratos de compraventa, facturación y entrega de stock.</p>
          </div>
          <div>
            <button class="btn btn-secondary" id="btn-refresh-ventas" title="Actualizar datos">
              <svg class="icon-svg sm" viewBox="0 0 24 24" width="16" height="16" stroke="currentColor" fill="none" stroke-width="2">
                <polyline points="23 4 23 10 17 10"></polyline>
                <polyline points="1 20 1 14 7 14"></polyline>
                <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"></path>
              </svg>
              <span>Refrescar</span>
            </button>
          </div>
        </div>

        <!-- ===================================================================
             SECCIÓN 1: PRECIOS DE REFERENCIA POR CLASE (REGLA DP-4)
             =================================================================== -->
        <div class="card mb-4">
          <div class="card-header d-flex justify-between align-center p-3 border-bottom flex-wrap gap-sm">
            <div>
              <h3 class="card-title text-base m-0">1. Precios de Referencia por Calibre (Regla DP-4)</h3>
              <span class="text-xs text-muted">Defina los precios oficiales de referencia por clase de bulbo para cada temporada</span>
            </div>
            <div class="d-flex align-center gap-sm">
              <label for="select-precios-temporada" class="text-xs font-bold uppercase text-muted mb-0">Temporada:</label>
              <select id="select-precios-temporada" class="form-control" style="width: auto; min-width: 200px;">
                <option value="">Cargando temporadas...</option>
              </select>
            </div>
          </div>
          <div class="card-body p-3">
            <form id="form-precios-referencia">
              <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(170px, 1fr)); gap: 1rem; margin-bottom: 1rem;">
                <div class="form-group mb-0">
                  <label for="input-precio-c0" class="form-label text-xs font-bold">Clase 0 (&gt; 12cm)</label>
                  <div class="input-group">
                    <span class="input-group-text">$</span>
                    <input type="number" id="input-precio-c0" class="form-control input-precio-ref" min="0" step="0.5" placeholder="0.00" />
                  </div>
                  <span class="text-xs text-muted">Bulbo calibre premium</span>
                </div>
                <div class="form-group mb-0">
                  <label for="input-precio-c1" class="form-label text-xs font-bold">Clase 1 (10 - 12cm)</label>
                  <div class="input-group">
                    <span class="input-group-text">$</span>
                    <input type="number" id="input-precio-c1" class="form-control input-precio-ref" min="0" step="0.5" placeholder="0.00" />
                  </div>
                  <span class="text-xs text-muted">Bulbo comercial estándar</span>
                </div>
                <div class="form-group mb-0">
                  <label for="input-precio-c2" class="form-label text-xs font-bold">Clase 2 (8 - 10cm)</label>
                  <div class="input-group">
                    <span class="input-group-text">$</span>
                    <input type="number" id="input-precio-c2" class="form-control input-precio-ref" min="0" step="0.5" placeholder="0.00" />
                  </div>
                  <span class="text-xs text-muted">Bulbo mediano</span>
                </div>
                <div class="form-group mb-0">
                  <label for="input-precio-c3" class="form-label text-xs font-bold">Clase 3 (6 - 8cm)</label>
                  <div class="input-group">
                    <span class="input-group-text">$</span>
                    <input type="number" id="input-precio-c3" class="form-control input-precio-ref" min="0" step="0.5" placeholder="0.00" />
                  </div>
                  <span class="text-xs text-muted">Bulbo desarrollo</span>
                </div>
                <div class="form-group mb-0">
                  <label for="input-precio-c4" class="form-label text-xs font-bold">Clase 4 (&lt; 6cm)</label>
                  <div class="input-group">
                    <span class="input-group-text">$</span>
                    <input type="number" id="input-precio-c4" class="form-control input-precio-ref" min="0" step="0.5" placeholder="0.00" />
                  </div>
                  <span class="text-xs text-muted">Semillero / Cría</span>
                </div>
              </div>

              <div class="d-flex justify-end">
                <button type="submit" class="btn btn-secondary btn-sm" id="btn-guardar-precios">
                  <svg class="icon-svg sm" viewBox="0 0 24 24" width="14" height="14" stroke="currentColor" fill="none" stroke-width="2">
                    <path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"></path>
                    <polyline points="17 21 17 13 7 13 7 21"></polyline>
                    <polyline points="7 3 7 8 15 8"></polyline>
                  </svg>
                  <span>Actualizar Precios de Referencia</span>
                </button>
              </div>
            </form>
          </div>
        </div>

        <!-- ===================================================================
             SECCIÓN 2: REGISTRO DE NUEVA VENTA DE BULBOS
             =================================================================== -->
        <div class="card mb-4">
          <div class="card-header p-3 border-bottom">
            <h3 class="card-title text-base m-0">2. Registrar Nueva Operación de Venta</h3>
            <span class="text-xs text-muted">Cargue el cliente comprador, cantidades por clase y verifique los precios pactados</span>
          </div>
          <div class="card-body p-3">
            <form id="form-nueva-venta">
              <div class="form-row" style="display: grid; grid-template-columns: repeat(auto-fit, minmax(240px, 1fr)); gap: 1rem; margin-bottom: 1rem;">
                <div class="form-group mb-0">
                  <label for="select-venta-cliente" class="form-label text-xs font-bold uppercase">
                    Cliente Comprador <span class="required-mark">*</span>
                  </label>
                  <select id="select-venta-cliente" class="form-control" required>
                    <option value="">Cargando clientes...</option>
                  </select>
                </div>

                <div class="form-group mb-0">
                  <label for="select-venta-temporada" class="form-label text-xs font-bold uppercase">
                    Temporada de Referencia (Precios)
                  </label>
                  <select id="select-venta-temporada" class="form-control">
                    <option value="">(Sin temporada / Precios libres)</option>
                  </select>
                </div>

                <div class="form-group mb-0">
                  <label for="input-venta-obs" class="form-label text-xs font-bold uppercase">
                    Observaciones Comerciales
                  </label>
                  <input type="text" id="input-venta-obs" class="form-control" placeholder="ej. Venta con entrega pactada para siembra" />
                </div>
              </div>

              <!-- Tabla de Ítems por Clase de Bulbo -->
              <div class="table-responsive mb-3">
                <table class="table mb-0" style="background: var(--surface);">
                  <thead>
                    <tr>
                      <th style="width: 25%;">Calibre / Clase de Bulbo</th>
                      <th style="width: 25%;">Cantidad (Bulbos)</th>
                      <th style="width: 25%;">Precio Unitario Pactado ($)</th>
                      <th style="width: 25%; text-align: right;">Subtotal ($)</th>
                    </tr>
                  </thead>
                  <tbody>
                    ${[0, 1, 2, 3, 4].map(c => `
                      <tr>
                        <td>
                          <strong>Clase ${c}</strong>
                          <span class="text-xs text-muted d-block">${this.getDescripcionClase(c)}</span>
                        </td>
                        <td>
                          <input type="number" id="venta-cant-c${c}" class="form-control venta-calc-cant" min="0" value="0" style="max-width: 160px;" />
                        </td>
                        <td>
                          <div class="input-group" style="max-width: 180px;">
                            <span class="input-group-text">$</span>
                            <input type="number" id="venta-precio-c${c}" class="form-control venta-calc-precio" min="0" step="0.5" value="0" />
                          </div>
                        </td>
                        <td style="text-align: right; vertical-align: middle; font-weight: 600;">
                          <span id="venta-subtotal-c${c}">$ 0,00</span>
                        </td>
                      </tr>
                    `).join('')}
                  </tbody>
                  <tfoot>
                    <tr style="background: var(--surface-alt); font-size: 1rem;">
                      <th colspan="2" style="text-align: left;">
                        Total Bulbos: <span id="label-total-bulbos-venta" class="font-bold text-primary">0</span> unidades
                      </th>
                      <th colspan="2" style="text-align: right;">
                        Monto Total: <span id="label-monto-total-venta" class="font-bold text-success" style="font-size: 1.25rem;">$ 0,00</span>
                      </th>
                    </tr>
                  </tfoot>
                </table>
              </div>

              <div class="d-flex justify-end gap-sm">
                <button type="submit" class="btn btn-primary" id="btn-submit-venta">
                  <svg class="icon-svg sm" viewBox="0 0 24 24" width="16" height="16" stroke="currentColor" fill="none" stroke-width="2">
                    <line x1="12" y1="5" x2="12" y2="19"></line>
                    <line x1="5" y1="12" x2="19" y2="12"></line>
                  </svg>
                  <span>Registrar Venta de Bulbos</span>
                </button>
              </div>
            </form>
          </div>
        </div>

        <!-- ===================================================================
             SECCIÓN 3: LISTADO Y CIRCUITO ADMINISTRATIVO DE VENTAS
             =================================================================== -->
        <div class="card">
          <div class="card-header d-flex justify-between align-center p-3 border-bottom flex-wrap gap-sm">
            <div>
              <h3 class="card-title text-base m-0">3. Circuito Administrativo de Ventas Registradas</h3>
              <span class="text-xs text-muted">Controle el avance de cada venta: Revisión &rarr; Contrato &rarr; Aprobación &rarr; Facturación &rarr; Stock</span>
            </div>
            <div class="d-flex align-center gap-sm">
              <label for="filter-estado-ventas" class="text-xs font-bold uppercase text-muted mb-0">Filtrar Estado:</label>
              <select id="filter-estado-ventas" class="form-control" style="width: auto;">
                <option value="todos">Todos los Estados</option>
                <option value="0">Registrada</option>
                <option value="1">En Revisión Administración</option>
                <option value="2">Aprobada Administración</option>
                <option value="3">Facturada</option>
                <option value="4">Completada (Stock Entregado)</option>
                <option value="5">Rechazada / Observada</option>
              </select>
            </div>
          </div>
          <div class="card-body p-0">
            <div class="table-responsive">
              <table class="table table-hover mb-0">
                <thead>
                  <tr>
                    <th># ID</th>
                    <th>Fecha</th>
                    <th>Cliente</th>
                    <th style="text-align: right;">Total Bulbos</th>
                    <th style="text-align: right;">Monto Total</th>
                    <th>Estado Circuito</th>
                    <th>Contrato</th>
                    <th>Factura</th>
                    <th style="text-align: center; min-width: 220px;">Acciones del Circuito</th>
                  </tr>
                </thead>
                <tbody id="tbody-ventas">
                  <tr><td colspan="9" class="text-center p-4 text-muted">Cargando ventas registradas...</td></tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>

        <!-- ===================================================================
             MODALES DINÁMICOS: ACCIÓN DE CIRCUITO Y DETALLE DE VENTA
             =================================================================== -->
        <!-- Modal Acción de Circuito (Contrato / Factura / Aprobación / Rechazo) -->
        <div id="modal-circuito" class="modal-backdrop">
          <div class="modal-dialog">
            <div class="modal-header">
              <h3 class="modal-title" id="modal-circuito-title">Acción del Circuito</h3>
              <button type="button" class="btn-icon modal-close" id="btn-close-modal-circuito">&times;</button>
            </div>
            <div class="modal-body p-4" id="modal-circuito-body">
              <!-- Inyectado dinámicamente -->
            </div>
          </div>
        </div>

        <!-- Modal Detalle y Auditoría de Venta -->
        <div id="modal-detalle-venta" class="modal-backdrop">
          <div class="modal-dialog modal-lg">
            <div class="modal-header">
              <h3 class="modal-title" id="modal-detalle-title">Detalle de Operación de Venta</h3>
              <button type="button" class="btn-icon modal-close" id="btn-close-modal-detalle">&times;</button>
            </div>
            <div class="modal-body p-4" id="modal-detalle-body">
              <!-- Inyectado dinámicamente -->
            </div>
          </div>
        </div>
      </div>
    `;

    await this.cargarDatosIniciales();
    this.bindEvents();
  }

  getDescripcionClase(clase) {
    switch (Number(clase)) {
      case 0: return 'Calibre superior (>12 cm) - Floración máxima';
      case 1: return 'Calibre primera (10 - 12 cm) - Comercial';
      case 2: return 'Calibre mediano (8 - 10 cm)';
      case 3: return 'Calibre menor (6 - 8 cm) - En desarrollo';
      case 4: return 'Calibre semillero (<6 cm) - Multiplicación';
      default: return '';
    }
  }

  async cargarDatosIniciales() {
    try {
      const [tempsRes, clsRes, vtsRes] = await Promise.all([
        temporadasService.listarTodas().catch(() => []),
        clientesService.listar({ pageSize: 100 }).catch(() => []),
        ventasService.listarTodas().catch(() => [])
      ]);

      this.temporadas = Array.isArray(tempsRes) ? tempsRes : (tempsRes.items || []);
      this.clientes = Array.isArray(clsRes) ? clsRes : (clsRes.items || []);
      this.ventas = Array.isArray(vtsRes) ? vtsRes : (vtsRes.items || []);

      // Poblar selector de temporadas en Sección 1
      const selectTempPrecios = document.getElementById('select-precios-temporada');
      const selectTempVenta = document.getElementById('select-venta-temporada');

      if (selectTempPrecios) {
        if (this.temporadas.length === 0) {
          selectTempPrecios.innerHTML = '<option value="">No hay temporadas registradas</option>';
        } else {
          selectTempPrecios.innerHTML = this.temporadas.map(t =>
            `<option value="${escapeHtml(t.id)}">${escapeHtml(t.nombre)} (${escapeHtml(t.nombreEstado || t.estado || '')})</option>`
          ).join('');

          // Activa por defecto o primera
          const activa = this.temporadas.find(t => {
            const st = String(t.nombreEstado || t.estado || '').toLowerCase();
            return !st.includes('cerrada');
          }) || this.temporadas[0];

          this.selectedTemporadaPreciosId = activa ? activa.id : this.temporadas[0].id;
          selectTempPrecios.value = this.selectedTemporadaPreciosId;
        }
      }

      // Poblar selector de temporadas en Sección 2
      if (selectTempVenta) {
        let opts = '<option value="">(Sin temporada / Precios libres)</option>';
        opts += this.temporadas.map(t =>
          `<option value="${escapeHtml(t.id)}" ${String(t.id) === String(this.selectedTemporadaPreciosId) ? 'selected' : ''}>${escapeHtml(t.nombre)}</option>`
        ).join('');
        selectTempVenta.innerHTML = opts;
        this.selectedTemporadaVentaId = this.selectedTemporadaPreciosId;
      }

      // Poblar selector de clientes en Sección 2
      const selectCliente = document.getElementById('select-venta-cliente');
      if (selectCliente) {
        if (this.clientes.length === 0) {
          selectCliente.innerHTML = '<option value="">No hay clientes registrados</option>';
        } else {
          selectCliente.innerHTML = '<option value="">-- Seleccione Cliente Comprador --</option>' +
            this.clientes.map(c => {
              const nombre = c.persona ? `${c.persona.nombre} ${c.persona.apellido}` : (c.razonSocial || c.nombreCompleto || c.codigoCliente);
              return `<option value="${escapeHtml(c.id)}">${escapeHtml(nombre)} (${escapeHtml(c.codigoCliente || 'CLI')})</option>`;
            }).join('');
        }
      }

      // Cargar precios de referencia de la temporada seleccionada
      if (this.selectedTemporadaPreciosId) {
        await this.cargarPreciosReferencia(this.selectedTemporadaPreciosId);
      }

      // Renderizar tabla de ventas
      this.renderTablaVentas();
    } catch (err) {
      toast.error('Error al inicializar la vista de ventas: ' + err.message);
    }
  }

  async cargarPreciosReferencia(temporadaId) {
    try {
      const precios = await ventasService.listarPreciosPorTemporada(temporadaId);
      this.preciosClaseActuales = {};

      (precios || []).forEach(p => {
        this.preciosClaseActuales[p.claseBulbo] = Number(p.precioReferencia) || 0;
      });

      // Rellenar inputs de la Sección 1
      for (let c = 0; c <= 4; c++) {
        const inp = document.getElementById(`input-precio-c${c}`);
        if (inp) {
          inp.value = this.preciosClaseActuales[c] !== undefined ? this.preciosClaseActuales[c] : '';
        }
      }

      // Pre-cargar precios por defecto en la Sección 2 (Nueva Venta)
      this.aplicarPreciosReferenciaEnFormularioVenta();
    } catch (err) {
      toast.error('Error al cargar precios de referencia: ' + err.message);
    }
  }

  aplicarPreciosReferenciaEnFormularioVenta() {
    for (let c = 0; c <= 4; c++) {
      const inpPrecio = document.getElementById(`venta-precio-c${c}`);
      if (inpPrecio) {
        const ref = this.preciosClaseActuales[c];
        if (ref !== undefined && ref > 0) {
          inpPrecio.value = ref;
        }
      }
    }
    this.actualizarCalculoVenta();
  }

  actualizarCalculoVenta() {
    let totalBulbos = 0;
    let montoTotal = 0;

    for (let c = 0; c <= 4; c++) {
      const inpCant = document.getElementById(`venta-cant-c${c}`);
      const inpPrecio = document.getElementById(`venta-precio-c${c}`);
      const lblSubtotal = document.getElementById(`venta-subtotal-c${c}`);

      const cant = Number(inpCant?.value) || 0;
      const precio = Number(inpPrecio?.value) || 0;
      const subtotal = cant * precio;

      totalBulbos += cant;
      montoTotal += subtotal;

      if (lblSubtotal) {
        lblSubtotal.textContent = `$ ${subtotal.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
      }
    }

    const lblTotalBulbos = document.getElementById('label-total-bulbos-venta');
    if (lblTotalBulbos) lblTotalBulbos.textContent = totalBulbos.toLocaleString('es-AR');

    const lblMontoTotal = document.getElementById('label-monto-total-venta');
    if (lblMontoTotal) {
      lblMontoTotal.textContent = `$ ${montoTotal.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
    }
  }

  bindEvents() {
    // Refrescar
    const btnRefresh = document.getElementById('btn-refresh-ventas');
    if (btnRefresh) {
      btnRefresh.addEventListener('click', async () => {
        await this.recargarVentas();
        if (this.selectedTemporadaPreciosId) {
          await this.cargarPreciosReferencia(this.selectedTemporadaPreciosId);
        }
        toast.info('Datos comerciales actualizados.');
      });
    }

    // Cambio de temporada en Precios de Referencia
    const selectTempPrecios = document.getElementById('select-precios-temporada');
    if (selectTempPrecios) {
      selectTempPrecios.addEventListener('change', async (e) => {
        this.selectedTemporadaPreciosId = e.target.value;
        if (this.selectedTemporadaPreciosId) {
          await this.cargarPreciosReferencia(this.selectedTemporadaPreciosId);
        }
      });
    }

    // Submit guardar precios de referencia
    const formPrecios = document.getElementById('form-precios-referencia');
    if (formPrecios) {
      formPrecios.addEventListener('submit', async (e) => {
        e.preventDefault();
        await this.handleGuardarPreciosReferencia();
      });
    }

    // Cambio de temporada en Sección Nueva Venta
    const selectTempVenta = document.getElementById('select-venta-temporada');
    if (selectTempVenta) {
      selectTempVenta.addEventListener('change', async (e) => {
        this.selectedTemporadaVentaId = e.target.value;
        if (this.selectedTemporadaVentaId) {
          await this.cargarPreciosReferencia(this.selectedTemporadaVentaId);
        }
      });
    }

    // Eventos de cálculo dinámico en formulario de venta
    for (let c = 0; c <= 4; c++) {
      const cant = document.getElementById(`venta-cant-c${c}`);
      const precio = document.getElementById(`venta-precio-c${c}`);
      cant?.addEventListener('input', () => this.actualizarCalculoVenta());
      precio?.addEventListener('input', () => this.actualizarCalculoVenta());
    }

    // Submit nueva venta
    const formVenta = document.getElementById('form-nueva-venta');
    if (formVenta) {
      formVenta.addEventListener('submit', async (e) => {
        e.preventDefault();
        await this.handleRegistrarVenta();
      });
    }

    // Filtro por estado
    const filterEstado = document.getElementById('filter-estado-ventas');
    if (filterEstado) {
      filterEstado.addEventListener('change', (e) => {
        this.filterEstado = e.target.value;
        this.renderTablaVentas();
      });
    }

    // Cerrar modales
    document.getElementById('btn-close-modal-circuito')?.addEventListener('click', () => this.closeModalCircuito());
    document.getElementById('btn-close-modal-detalle')?.addEventListener('click', () => this.closeModalDetalle());
  }

  async handleGuardarPreciosReferencia() {
    if (!this.selectedTemporadaPreciosId) {
      toast.warning('Seleccione una temporada para configurar los precios.');
      return;
    }

    try {
      const promesas = [];
      for (let c = 0; c <= 4; c++) {
        const inp = document.getElementById(`input-precio-c${c}`);
        const precio = Number(inp?.value);
        if (precio > 0) {
          promesas.push(ventasService.configurarPrecioClase({
            temporadaId: Number(this.selectedTemporadaPreciosId),
            claseBulbo: c,
            precioReferencia: precio
          }));
        }
      }

      if (promesas.length === 0) {
        toast.warning('Ingrese al menos un precio mayor a 0 para guardar.');
        return;
      }

      await Promise.all(promesas);
      toast.success('Precios de referencia actualizados exitosamente.');
      await this.cargarPreciosReferencia(this.selectedTemporadaPreciosId);
    } catch (err) {
      toast.error('Error al actualizar precios de referencia: ' + err.message);
    }
  }

  async handleRegistrarVenta() {
    const clienteId = document.getElementById('select-venta-cliente')?.value;
    if (!clienteId) {
      toast.warning('Debe seleccionar un cliente comprador.');
      return;
    }

    const temporadaId = document.getElementById('select-venta-temporada')?.value || null;
    const obs = document.getElementById('input-venta-obs')?.value.trim() || '';

    const detalles = [];
    let totalBulbos = 0;

    for (let c = 0; c <= 4; c++) {
      const cant = Number(document.getElementById(`venta-cant-c${c}`)?.value) || 0;
      const precio = Number(document.getElementById(`venta-precio-c${c}`)?.value) || 0;

      if (cant > 0) {
        if (precio <= 0) {
          toast.warning(`Debe especificar un precio unitario pactado mayor a 0 para Clase ${c}.`);
          return;
        }
        detalles.push({
          claseBulbo: c,
          cantidad: cant,
          precioUnitarioPactado: precio
        });
        totalBulbos += cant;
      }
    }

    if (detalles.length === 0 || totalBulbos <= 0) {
      toast.warning('Debe ingresar al menos una clase con cantidad mayor a 0 para registrar la venta.');
      return;
    }

    try {
      const dto = {
        clienteId: Number(clienteId),
        temporadaId: temporadaId ? Number(temporadaId) : null,
        origenParticipanteId: 1, // Empresa Singleton
        observaciones: obs || null,
        detalles
      };

      const nueva = await ventasService.crear(dto);
      toast.success(`Venta #${nueva.id} registrada con éxito. Estado: Registrada.`);

      // Limpiar cantidades
      for (let c = 0; c <= 4; c++) {
        const inp = document.getElementById(`venta-cant-c${c}`);
        if (inp) inp.value = 0;
      }
      const obsInput = document.getElementById('input-venta-obs');
      if (obsInput) obsInput.value = '';

      this.actualizarCalculoVenta();
      await this.recargarVentas();
    } catch (err) {
      toast.error('Error al registrar venta: ' + err.message);
    }
  }

  async recargarVentas() {
    try {
      const res = await ventasService.listarTodas();
      this.ventas = Array.isArray(res) ? res : (res.items || []);
      this.renderTablaVentas();
    } catch (err) {
      toast.error('Error al recargar ventas: ' + err.message);
    }
  }

  renderTablaVentas() {
    const tbody = document.getElementById('tbody-ventas');
    if (!tbody) return;

    let lista = [...this.ventas];
    if (this.filterEstado !== 'todos') {
      lista = lista.filter(v => Number(v.estado) === Number(this.filterEstado));
    }

    if (lista.length === 0) {
      tbody.innerHTML = '<tr><td colspan="9" class="text-center p-4 text-muted">No se registran ventas con los filtros aplicados.</td></tr>';
      return;
    }

    tbody.innerHTML = lista.map(v => {
      const fecha = v.fecha ? new Date(v.fecha).toLocaleDateString() : '-';
      const montoFmt = `$ ${(Number(v.montoTotal) || 0).toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
      const bulbosFmt = (Number(v.totalBulbos) || 0).toLocaleString('es-AR');

      const estadoInfo = this.obtenerEstadoInfo(v.estado, v.nombreEstado);

      // Contrato
      const contratoBadge = v.contrato
        ? `<span class="badge badge-primary" title="${escapeHtml(v.contrato.numeroContrato)}">CTR: ${escapeHtml(v.contrato.numeroContrato)}</span>`
        : `<span class="badge badge-secondary text-muted">Sin Contrato</span>`;

      // Factura
      const facturaBadge = v.factura
        ? `<span class="badge badge-accent" title="${escapeHtml(v.factura.numeroFactura)}">FAC: ${escapeHtml(v.factura.numeroFactura)}</span>`
        : `<span class="badge badge-secondary text-muted">Sin Factura</span>`;

      return `
        <tr>
          <td><strong>#${escapeHtml(v.id)}</strong></td>
          <td>${escapeHtml(fecha)}</td>
          <td>
            <strong>${escapeHtml(v.nombreCliente || `Cliente #${v.clienteId}`)}</strong>
            <span class="text-xs text-muted d-block">${escapeHtml(v.codigoCliente || '')}</span>
          </td>
          <td style="text-align: right; font-weight: 600;">${bulbosFmt}</td>
          <td style="text-align: right; font-weight: 700; color: var(--primary);">${montoFmt}</td>
          <td>
            <span class="badge ${estadoInfo.badgeClass}">
              <span class="badge-dot"></span>
              ${escapeHtml(estadoInfo.label)}
            </span>
          </td>
          <td>${contratoBadge}</td>
          <td>${facturaBadge}</td>
          <td style="text-align: center;">
            <div class="d-flex justify-center align-center gap-xs flex-wrap">
              ${this.renderBotonesCircuito(v)}
              <button class="btn btn-outline btn-sm btn-ver-detalle" data-id="${escapeHtml(v.id)}" title="Ver auditoría y desglose">
                Detalles
              </button>
            </div>
          </td>
        </tr>
      `;
    }).join('');

    this.bindBotonesCircuito(tbody);
  }

  obtenerEstadoInfo(estado, nombreEstado) {
    const st = Number(estado);
    switch (st) {
      case 0:
        return { label: 'Registrada', badgeClass: 'badge-secondary' };
      case 1:
        return { label: 'En Revisión (Admin)', badgeClass: 'badge-warning' };
      case 2:
        return { label: 'Aprobada (Admin)', badgeClass: 'badge-info' };
      case 3:
        return { label: 'Facturada', badgeClass: 'badge-accent' };
      case 4:
        return { label: 'Completada', badgeClass: 'badge-success' };
      case 5:
        return { label: 'Rechazada / Obs.', badgeClass: 'badge-danger' };
      case 6:
        return { label: 'Anulada', badgeClass: 'badge-danger' };
      default:
        return { label: nombreEstado || 'Desconocido', badgeClass: 'badge-secondary' };
    }
  }

  renderBotonesCircuito(v) {
    const st = Number(v.estado);

    // 1. En Registrada (0) o Rechazada (5): botón "Enviar a Revisión"
    if (st === 0 || st === 5) {
      return `
        <button class="btn btn-primary btn-sm btn-enviar-revision" data-id="${escapeHtml(v.id)}">
          Enviar a Revisión
        </button>
      `;
    }

    // 2. En EnRevisionAdministracion (1):
    //    - Si no tiene contrato: "Generar Contrato"
    //    - Si ya tiene contrato: "Aprobar Administración"
    if (st === 1) {
      if (!v.contrato) {
        return `
          <button class="btn btn-primary btn-sm btn-generar-contrato" data-id="${escapeHtml(v.id)}">
            Generar Contrato
          </button>
          <button class="btn btn-outline-danger btn-sm btn-rechazar-venta" data-id="${escapeHtml(v.id)}">
            Rechazar
          </button>
        `;
      } else {
        return `
          <button class="btn btn-success btn-sm btn-aprobar-admin" data-id="${escapeHtml(v.id)}">
            Aprobar Administración
          </button>
          <button class="btn btn-outline-danger btn-sm btn-rechazar-venta" data-id="${escapeHtml(v.id)}">
            Rechazar
          </button>
        `;
      }
    }

    // 3. En AprobadaAdministracion (2): botón "Emitir Factura (Contabilidad)"
    if (st === 2) {
      return `
        <button class="btn btn-accent btn-sm btn-emitir-factura" data-id="${escapeHtml(v.id)}">
          Emitir Factura
        </button>
        <button class="btn btn-outline-danger btn-sm btn-rechazar-venta" data-id="${escapeHtml(v.id)}">
          Rechazar
        </button>
      `;
    }

    // 4. En Facturada (3): botón "Aprobación Final y Entregar Stock"
    if (st === 3) {
      return `
        <button class="btn btn-success btn-sm btn-aprobar-final" data-id="${escapeHtml(v.id)}">
          Aprobar Final y Entregar Stock
        </button>
        <button class="btn btn-outline-danger btn-sm btn-rechazar-venta" data-id="${escapeHtml(v.id)}">
          Rechazar
        </button>
      `;
    }

    // 5. En Completada (4): finalizada
    if (st === 4) {
      return `<span class="badge badge-success" style="font-size: 0.7rem;">Stock Entregado</span>`;
    }

    return '';
  }

  bindBotonesCircuito(container) {
    // Ver Detalle
    container.querySelectorAll('.btn-ver-detalle').forEach(b => {
      b.addEventListener('click', () => this.abrirModalDetalle(b.getAttribute('data-id')));
    });

    // Enviar a Revisión
    container.querySelectorAll('.btn-enviar-revision').forEach(b => {
      b.addEventListener('click', () => this.abrirModalAccion('revision', b.getAttribute('data-id')));
    });

    // Generar Contrato
    container.querySelectorAll('.btn-generar-contrato').forEach(b => {
      b.addEventListener('click', () => this.abrirModalAccion('contrato', b.getAttribute('data-id')));
    });

    // Aprobar Administración
    container.querySelectorAll('.btn-aprobar-admin').forEach(b => {
      b.addEventListener('click', () => this.abrirModalAccion('aprobar-admin', b.getAttribute('data-id')));
    });

    // Emitir Factura
    container.querySelectorAll('.btn-emitir-factura').forEach(b => {
      b.addEventListener('click', () => this.abrirModalAccion('factura', b.getAttribute('data-id')));
    });

    // Aprobar Final
    container.querySelectorAll('.btn-aprobar-final').forEach(b => {
      b.addEventListener('click', () => this.abrirModalAccion('aprobar-final', b.getAttribute('data-id')));
    });

    // Rechazar
    container.querySelectorAll('.btn-rechazar-venta').forEach(b => {
      b.addEventListener('click', () => this.abrirModalAccion('rechazar', b.getAttribute('data-id')));
    });
  }

  abrirModalAccion(tipo, ventaId) {
    const venta = this.ventas.find(v => String(v.id) === String(ventaId));
    if (!venta) return;
    this.currentModalVenta = venta;

    const modal = document.getElementById('modal-circuito');
    const title = document.getElementById('modal-circuito-title');
    const body = document.getElementById('modal-circuito-body');

    if (!modal || !title || !body) return;

    if (tipo === 'revision') {
      title.textContent = `Enviar a Revisión - Venta #${venta.id}`;
      body.innerHTML = `
        <p class="text-sm text-muted mb-3">
          La venta pasará al estado <strong>En Revisión Administración</strong> para que la Secretaría elabore el contrato formal.
        </p>
        <form id="form-modal-accion">
          <div class="form-group mb-3">
            <label for="modal-input-obs" class="form-label text-xs font-bold uppercase">Observaciones para Secretaría (Opcional)</label>
            <input type="text" id="modal-input-obs" class="form-control" placeholder="ej. Cliente solicita envío de copia digital" />
          </div>
          <div class="d-flex justify-end gap-sm">
            <button type="button" class="btn btn-secondary btn-sm" id="btn-cancel-modal-accion">Cancelar</button>
            <button type="submit" class="btn btn-primary btn-sm">Confirmar Envío a Revisión</button>
          </div>
        </form>
      `;
    } else if (tipo === 'contrato') {
      title.textContent = `Generar Contrato de Compraventa - Venta #${venta.id}`;
      const numSugerido = `CTR-${new Date().getFullYear()}-${String(venta.id).padStart(3, '0')}`;
      body.innerHTML = `
        <p class="text-sm text-muted mb-3">
          Registre el número formal de contrato generado para el comprador <strong>${escapeHtml(venta.nombreCliente)}</strong>.
        </p>
        <form id="form-modal-accion">
          <div class="form-group mb-3">
            <label for="modal-input-contrato" class="form-label text-xs font-bold uppercase">Número de Contrato <span class="required-mark">*</span></label>
            <input type="text" id="modal-input-contrato" class="form-control" value="${escapeHtml(numSugerido)}" required />
          </div>
          <div class="form-group mb-3">
            <label for="modal-input-url" class="form-label text-xs font-bold uppercase">Enlace al Documento Firmado (URL Opcional)</label>
            <input type="url" id="modal-input-url" class="form-control" placeholder="https://almacenamiento.ejemplo.com/contratos/ctr-01.pdf" />
          </div>
          <div class="d-flex justify-end gap-sm">
            <button type="button" class="btn btn-secondary btn-sm" id="btn-cancel-modal-accion">Cancelar</button>
            <button type="submit" class="btn btn-primary btn-sm">Generar y Asociar Contrato</button>
          </div>
        </form>
      `;
    } else if (tipo === 'aprobar-admin') {
      title.textContent = `Aprobar Administración y Contrato - Venta #${venta.id}`;
      body.innerHTML = `
        <p class="text-sm text-muted mb-3">
          El contrato <strong>${escapeHtml(venta.contrato?.numeroContrato || 'Generado')}</strong> ha sido verificado. Al confirmar, la venta pasará a estado <strong>Aprobada Administración</strong> para posterior facturación contable.
        </p>
        <form id="form-modal-accion">
          <div class="form-group mb-3">
            <label for="modal-input-obs" class="form-label text-xs font-bold uppercase">Observaciones de Secretaría (Opcional)</label>
            <input type="text" id="modal-input-obs" class="form-control" placeholder="ej. Contrato firmado en conformidad" />
          </div>
          <div class="d-flex justify-end gap-sm">
            <button type="button" class="btn btn-secondary btn-sm" id="btn-cancel-modal-accion">Cancelar</button>
            <button type="submit" class="btn btn-success btn-sm">Aprobar por Administración</button>
          </div>
        </form>
      `;
    } else if (tipo === 'factura') {
      title.textContent = `Emitir Factura (Contabilidad) - Venta #${venta.id}`;
      const numFacSugerido = `FAC-A-0001-${String(venta.id).padStart(8, '0')}`;
      body.innerHTML = `
        <p class="text-sm text-muted mb-3">
          Monto total a facturar: <strong class="text-success">$ ${(Number(venta.montoTotal) || 0).toLocaleString('es-AR', { minimumFractionDigits: 2 })}</strong>.
        </p>
        <form id="form-modal-accion">
          <div class="form-group mb-3">
            <label for="modal-input-factura" class="form-label text-xs font-bold uppercase">Número de Factura / Comprobante <span class="required-mark">*</span></label>
            <input type="text" id="modal-input-factura" class="form-control" value="${escapeHtml(numFacSugerido)}" required />
          </div>
          <div class="form-group mb-3">
            <label for="modal-input-url" class="form-label text-xs font-bold uppercase">Enlace PDF Comprobante AFIP (URL Opcional)</label>
            <input type="url" id="modal-input-url" class="form-control" placeholder="https://almacenamiento.ejemplo.com/facturas/fac-01.pdf" />
          </div>
          <div class="form-group mb-3">
            <label for="modal-input-obs" class="form-label text-xs font-bold uppercase">Observaciones Contables</label>
            <input type="text" id="modal-input-obs" class="form-control" placeholder="ej. Facturado con condición de pago a 30 días" />
          </div>
          <div class="d-flex justify-end gap-sm">
            <button type="button" class="btn btn-secondary btn-sm" id="btn-cancel-modal-accion">Cancelar</button>
            <button type="submit" class="btn btn-accent btn-sm">Emitir Factura Contable</button>
          </div>
        </form>
      `;
    } else if (tipo === 'aprobar-final') {
      title.textContent = `Aprobación Final y Entrega de Stock - Venta #${venta.id}`;
      body.innerHTML = `
        <div style="background: rgba(34, 197, 94, 0.1); border-left: 4px solid var(--success); padding: 0.75rem 1rem; border-radius: var(--radius-sm); font-size: 0.875rem; margin-bottom: 1rem;">
          <strong>Paso Definitivo:</strong> Al completar esta aprobación, se generará el movimiento comercial en el stock y los bulbos serán acreditados en el inventario del comprador <strong>${escapeHtml(venta.nombreCliente)}</strong>.
        </div>
        <form id="form-modal-accion">
          <div class="form-group mb-3">
            <label for="modal-input-obs" class="form-label text-xs font-bold uppercase">Observaciones de Entrega Final</label>
            <input type="text" id="modal-input-obs" class="form-control" placeholder="ej. Retiro de bulbos en depósito central" />
          </div>
          <div class="d-flex justify-end gap-sm">
            <button type="button" class="btn btn-secondary btn-sm" id="btn-cancel-modal-accion">Cancelar</button>
            <button type="submit" class="btn btn-success btn-sm">Completar Venta y Entregar Stock</button>
          </div>
        </form>
      `;
    } else if (tipo === 'rechazar') {
      title.textContent = `Rechazar u Observar Venta #${venta.id}`;
      body.innerHTML = `
        <p class="text-sm text-danger mb-3 font-semibold">
          Indique obligatoriamente el motivo del rechazo u observación para notificar al comercial.
        </p>
        <form id="form-modal-accion">
          <div class="form-group mb-3">
            <label for="modal-input-obs" class="form-label text-xs font-bold uppercase">Motivo del Rechazo <span class="required-mark">*</span></label>
            <textarea id="modal-input-obs" class="form-control" rows="3" placeholder="ej. Precios pactados por debajo del margen mínimo sin autorización" required></textarea>
          </div>
          <div class="d-flex justify-end gap-sm">
            <button type="button" class="btn btn-secondary btn-sm" id="btn-cancel-modal-accion">Cancelar</button>
            <button type="submit" class="btn btn-danger btn-sm">Confirmar Rechazo</button>
          </div>
        </form>
      `;
    }

    document.getElementById('btn-cancel-modal-accion')?.addEventListener('click', () => this.closeModalCircuito());

    const formModal = document.getElementById('form-modal-accion');
    if (formModal) {
      formModal.addEventListener('submit', async (e) => {
        e.preventDefault();
        await this.ejecutarAccionCircuito(tipo, venta.id);
      });
    }

    modal.classList.add('is-active');
  }

  closeModalCircuito() {
    const modal = document.getElementById('modal-circuito');
    modal?.classList.remove('is-active');
    this.currentModalVenta = null;
  }

  async ejecutarAccionCircuito(tipo, ventaId) {
    try {
      if (tipo === 'revision') {
        const obs = document.getElementById('modal-input-obs')?.value || '';
        await ventasService.enviarARevision(ventaId, obs);
        toast.success(`Venta #${ventaId} enviada a revisión exitosamente.`);
      } else if (tipo === 'contrato') {
        const num = document.getElementById('modal-input-contrato')?.value.trim();
        const url = document.getElementById('modal-input-url')?.value.trim();
        if (!num) {
          toast.warning('El número de contrato es obligatorio.');
          return;
        }
        await ventasService.generarContrato(ventaId, { numeroContrato: num, archivoUrl: url || null });
        toast.success(`Contrato ${num} generado para la venta #${ventaId}.`);
      } else if (tipo === 'aprobar-admin') {
        const obs = document.getElementById('modal-input-obs')?.value || '';
        await ventasService.aprobarAdministracion(ventaId, obs);
        toast.success(`Venta #${ventaId} aprobada por Administración.`);
      } else if (tipo === 'factura') {
        const num = document.getElementById('modal-input-factura')?.value.trim();
        const url = document.getElementById('modal-input-url')?.value.trim();
        const obs = document.getElementById('modal-input-obs')?.value || '';
        if (!num) {
          toast.warning('El número de factura es obligatorio.');
          return;
        }
        await ventasService.emitirFactura(ventaId, { numeroFactura: num, archivoUrl: url || null, observaciones: obs || null });
        toast.success(`Factura ${num} emitida para la venta #${ventaId}.`);
      } else if (tipo === 'aprobar-final') {
        const obs = document.getElementById('modal-input-obs')?.value || '';
        await ventasService.aprobarFinal(ventaId, obs);
        toast.success(`Venta #${ventaId} aprobada finalmente. Stock entregado y acreditado.`);
      } else if (tipo === 'rechazar') {
        const obs = document.getElementById('modal-input-obs')?.value.trim();
        if (!obs) {
          toast.warning('Debe ingresar el motivo del rechazo.');
          return;
        }
        await ventasService.rechazar(ventaId, obs);
        toast.warning(`Venta #${ventaId} rechazada / observada.`);
      }

      this.closeModalCircuito();
      await this.recargarVentas();
    } catch (err) {
      toast.error('Error al procesar la acción: ' + err.message);
    }
  }

  abrirModalDetalle(ventaId) {
    const venta = this.ventas.find(v => String(v.id) === String(ventaId));
    if (!venta) return;

    const modal = document.getElementById('modal-detalle-venta');
    const title = document.getElementById('modal-detalle-title');
    const body = document.getElementById('modal-detalle-body');

    if (!modal || !title || !body) return;

    title.textContent = `Auditoría y Detalle de Venta #${venta.id}`;

    const fechaFmt = venta.fecha ? new Date(venta.fecha).toLocaleString() : '-';
    const montoFmt = `$ ${(Number(venta.montoTotal) || 0).toLocaleString('es-AR', { minimumFractionDigits: 2 })}`;
    const estadoInfo = this.obtenerEstadoInfo(venta.estado, venta.nombreEstado);

    body.innerHTML = `
      <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 1rem; margin-bottom: 1.5rem; background: var(--surface-alt); padding: 1rem; border-radius: var(--radius-md);">
        <div>
          <span class="text-xs text-muted font-bold uppercase">Cliente Comprador:</span>
          <div class="font-bold text-main">${escapeHtml(venta.nombreCliente || `Cliente #${venta.clienteId}`)}</div>
          <span class="text-xs text-muted">${escapeHtml(venta.codigoCliente || '')}</span>
        </div>
        <div>
          <span class="text-xs text-muted font-bold uppercase">Fecha de Registro:</span>
          <div class="font-semibold text-main">${escapeHtml(fechaFmt)}</div>
        </div>
        <div>
          <span class="text-xs text-muted font-bold uppercase">Estado Actual:</span>
          <div><span class="badge ${estadoInfo.badgeClass}">${escapeHtml(estadoInfo.label)}</span></div>
        </div>
        <div>
          <span class="text-xs text-muted font-bold uppercase">Monto Total Facturado:</span>
          <div class="font-bold text-success" style="font-size: 1.25rem;">${montoFmt}</div>
        </div>
      </div>

      <!-- Desglose de ítems -->
      <h4 class="text-sm font-bold uppercase mb-2">Desglose de Bulbos por Calibre</h4>
      <div class="table-responsive mb-4">
        <table class="table mb-0">
          <thead>
            <tr>
              <th>Calibre / Clase</th>
              <th style="text-align: right;">Cantidad</th>
              <th style="text-align: right;">Precio Pactado</th>
              <th style="text-align: right;">Subtotal</th>
            </tr>
          </thead>
          <tbody>
            ${(venta.detalles || []).map(d => `
              <tr>
                <td><strong>${escapeHtml(d.nombreClase || `Clase ${d.claseBulbo}`)}</strong></td>
                <td style="text-align: right; font-weight: 600;">${(Number(d.cantidad) || 0).toLocaleString('es-AR')}</td>
                <td style="text-align: right;">$ ${(Number(d.precioUnitarioPactado) || 0).toLocaleString('es-AR', { minimumFractionDigits: 2 })}</td>
                <td style="text-align: right; font-weight: 700;">$ ${(Number(d.subtotal) || 0).toLocaleString('es-AR', { minimumFractionDigits: 2 })}</td>
              </tr>
            `).join('')}
          </tbody>
        </table>
      </div>

      <!-- Datos de Contrato y Factura -->
      <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(260px, 1fr)); gap: 1rem; margin-bottom: 1.5rem;">
        <div class="card p-3" style="background: var(--surface);">
          <h5 class="text-xs font-bold uppercase mb-2 text-primary">Contrato de Compraventa</h5>
          ${venta.contrato ? `
            <div class="text-sm"><strong>Número:</strong> ${escapeHtml(venta.contrato.numeroContrato)}</div>
            <div class="text-xs text-muted">Generado: ${escapeHtml(new Date(venta.contrato.fechaGeneracion).toLocaleDateString())}</div>
            ${this._safeUrl(venta.contrato.archivoUrl) ? `<a href="${escapeHtml(this._safeUrl(venta.contrato.archivoUrl))}" target="_blank" rel="noopener noreferrer" class="text-xs text-primary font-bold mt-1 d-block">Descargar Archivo Contrato</a>` : ''}
          ` : '<span class="text-xs text-muted">No se ha generado contrato aún.</span>'}
        </div>

        <div class="card p-3" style="background: var(--surface);">
          <h5 class="text-xs font-bold uppercase mb-2 text-accent">Factura Comercial (Contabilidad)</h5>
          ${venta.factura ? `
            <div class="text-sm"><strong>Número:</strong> ${escapeHtml(venta.factura.numeroFactura)}</div>
            <div class="text-xs text-muted">Emitida: ${escapeHtml(new Date(venta.factura.fechaEmision).toLocaleDateString())}</div>
            ${this._safeUrl(venta.factura.archivoUrl) ? `<a href="${escapeHtml(this._safeUrl(venta.factura.archivoUrl))}" target="_blank" rel="noopener noreferrer" class="text-xs text-primary font-bold mt-1 d-block">Descargar Comprobante PDF</a>` : ''}
          ` : '<span class="text-xs text-muted">Pendiente de facturación contable.</span>'}
        </div>
      </div>

      <!-- Historial de Aprobaciones / Trazabilidad -->
      <h4 class="text-sm font-bold uppercase mb-2">Trazabilidad y Circuito de Aprobaciones</h4>
      <div class="table-responsive">
        <table class="table mb-0">
          <thead>
            <tr>
              <th>Fecha</th>
              <th>Acción</th>
              <th>Responsable</th>
              <th>Observaciones</th>
            </tr>
          </thead>
          <tbody>
            ${(venta.historialAprobaciones && venta.historialAprobaciones.length > 0) ? venta.historialAprobaciones.map(h => `
              <tr>
                <td class="text-xs">${escapeHtml(h.fecha ? new Date(h.fecha).toLocaleString() : '-')}</td>
                <td><span class="badge badge-secondary">${escapeHtml(h.nombreAccion || `Acción #${h.accion}`)}</span></td>
                <td class="text-sm">${escapeHtml(h.nombreUsuarioAprobador || 'Usuario')}</td>
                <td class="text-xs text-muted">${escapeHtml(h.observaciones || 'Sin notas')}</td>
              </tr>
            `).join('') : '<tr><td colspan="4" class="text-center p-3 text-muted text-xs">Sin registros de auditoría aún.</td></tr>'}
          </tbody>
        </table>
      </div>

      <div class="d-flex justify-end mt-4">
        <button type="button" class="btn btn-secondary btn-sm" id="btn-close-detalle-footer">Cerrar</button>
      </div>
    `;

    document.getElementById('btn-close-detalle-footer')?.addEventListener('click', () => this.closeModalDetalle());
    modal.classList.add('is-active');
  }

  closeModalDetalle() {
    const modal = document.getElementById('modal-detalle-venta');
    modal?.classList.remove('is-active');
  }

  /**
   * Devuelve la URL solo si no usa un esquema peligroso (javascript:, data:, vbscript:).
   * Retorna '' si la URL es vacía o insegura.
   */
  _safeUrl(url) {
    if (!url) return '';
    const s = String(url).trim();
    if (/^(javascript|data|vbscript):/i.test(s.replace(/[\s\u0000-\u001f]/g, ''))) return '';
    return s;
  }
}
