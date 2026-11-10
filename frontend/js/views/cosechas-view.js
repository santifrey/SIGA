/**
 * SIGA - views/cosechas-view.js
 * Vista funcional para Gestión de Cosechas por Sucursal y Acreditación de Bulbos
 */

import { cosechasService } from '../services/cosechas-service.js';
import { temporadasService } from '../services/temporadas-service.js';
import { sucursalesService } from '../services/sucursales-service.js';
import { plantacionesService } from '../services/plantaciones-service.js';
import { toast } from '../utils/toast.js';
import { escapeHtml } from '../utils/escape-html.js';

export class CosechasView {
  constructor(router) {
    this.router = router;
    this.container = null;
    this.temporadas = [];
    this.sucursales = [];
    this.cosechasTemporada = [];
    this.selectedTemporadaId = null;
    this.selectedSucursalId = null;
    this.currentCosecha = null;
    this.lotesDisponibles = [];
  }

  async render(rootElement) {
    this.container = rootElement;
    this.container.innerHTML = `
      <div class="fade-in">
        <!-- Encabezado de la Vista -->
        <div class="d-flex justify-between align-center mb-4 flex-wrap gap-md">
          <div>
            <h1 style="font-size: 1.5rem; margin-bottom: 0.25rem;">Gestión de Cosechas por Sucursal</h1>
            <p class="text-muted text-sm">Control de recolección de bulbos por lote y acreditación automática a titulares vigentes.</p>
          </div>
          <div>
            <button class="btn btn-secondary" id="btn-refresh-cosechas" title="Actualizar datos">
              <svg class="icon-svg sm" viewBox="0 0 24 24" width="16" height="16" stroke="currentColor" fill="none" stroke-width="2">
                <polyline points="23 4 23 10 17 10"></polyline>
                <polyline points="1 20 1 14 7 14"></polyline>
                <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"></path>
              </svg>
              <span>Refrescar</span>
            </button>
          </div>
        </div>

        <!-- Filtros y Selectores Principales -->
        <div class="card mb-4" style="background: var(--surface);">
          <div class="card-body p-3">
            <div class="form-row two-cols" style="display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 1rem;">
              <div class="form-group mb-0">
                <label for="select-cosecha-temporada" class="form-label font-bold text-xs uppercase" style="color: var(--primary);">
                  1. Temporada Productiva:
                </label>
                <select id="select-cosecha-temporada" class="form-control" style="font-weight: 600;">
                  <option value="">Cargando temporadas...</option>
                </select>
              </div>

              <div class="form-group mb-0">
                <label for="select-cosecha-sucursal" class="form-label font-bold text-xs uppercase" style="color: var(--accent);">
                  2. Sucursal / Campo:
                </label>
                <select id="select-cosecha-sucursal" class="form-control">
                  <option value="">Cargando sucursales...</option>
                </select>
              </div>
            </div>
          </div>
        </div>

        <!-- Panel Dinámico: Cosecha de la Sucursal Seleccionada -->
        <div id="panel-cosecha-sucursal">
          <div class="text-center p-4 text-muted">Seleccione una temporada y sucursal para gestionar la cosecha.</div>
        </div>

        <!-- Tabla Histórica de Cosechas en la Temporada -->
        <div class="card mt-4">
          <div class="card-header d-flex justify-between align-center p-3 border-bottom">
            <h3 class="card-title text-base m-0">Todas las Cosechas Registradas en la Temporada</h3>
            <span class="text-xs text-muted" id="total-cosechas-badge">0 sucursales</span>
          </div>
          <div class="card-body p-0">
            <div class="table-responsive">
              <table class="table table-hover mb-0">
                <thead>
                  <tr>
                    <th>#</th>
                    <th>Sucursal</th>
                    <th>Estado</th>
                    <th>Fecha Inicio</th>
                    <th>Fecha Cierre</th>
                    <th style="text-align: right;">Total Bulbos Cosechados</th>
                    <th style="text-align: center;">Acción</th>
                  </tr>
                </thead>
                <tbody id="tbody-cosechas-temporada">
                  <tr><td colspan="7" class="text-center p-3 text-muted">Cargando cosechas...</td></tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>
    `;

    await this.cargarDatosIniciales();
    this.bindEvents();
  }

  async cargarDatosIniciales() {
    try {
      const [tempsRes, sucsRes] = await Promise.all([
        temporadasService.listarTodas().catch(() => []),
        sucursalesService.getAll().catch(() => [])
      ]);

      this.temporadas = Array.isArray(tempsRes) ? tempsRes : (tempsRes.items || []);
      this.sucursales = Array.isArray(sucsRes) ? sucsRes : (sucsRes.items || []);

      const selectTemp = document.getElementById('select-cosecha-temporada');
      if (selectTemp) {
        if (this.temporadas.length === 0) {
          selectTemp.innerHTML = '<option value="">No hay temporadas disponibles</option>';
        } else {
          selectTemp.innerHTML = this.temporadas.map(t => {
            const estado = t.nombreEstado || t.estado || '';
            return `<option value="${escapeHtml(t.id)}">${escapeHtml(t.nombre)} (${escapeHtml(estado)})</option>`;
          }).join('');

          // Seleccionar activa por defecto o la primera
          const activa = this.temporadas.find(t => {
            const st = String(t.nombreEstado || t.estado || '').toLowerCase();
            return st.includes('cosecha') || st.includes('crecimiento') || st.includes('plantacion');
          }) || this.temporadas[0];

          this.selectedTemporadaId = activa ? activa.id : this.temporadas[0].id;
          selectTemp.value = this.selectedTemporadaId;
        }
      }

      const selectSuc = document.getElementById('select-cosecha-sucursal');
      if (selectSuc) {
        const sucursalesActivas = this.sucursales.filter(s => s.activa !== false);
        const listaSucs = sucursalesActivas.length > 0 ? sucursalesActivas : this.sucursales;

        if (listaSucs.length === 0) {
          selectSuc.innerHTML = '<option value="">No hay sucursales disponibles</option>';
        } else {
          selectSuc.innerHTML = listaSucs.map(s => {
            return `<option value="${escapeHtml(s.id)}">${escapeHtml(s.nombre)} (${escapeHtml(s.codigo || s.ciudad || 'Sucursal')})</option>`;
          }).join('');

          this.selectedSucursalId = listaSucs[0].id;
          selectSuc.value = this.selectedSucursalId;
        }
      }

      await this.actualizarContexto();
    } catch (err) {
      toast.error('Error al inicializar la gestión de cosechas: ' + err.message);
    }
  }

  bindEvents() {
    const btnRefresh = document.getElementById('btn-refresh-cosechas');
    if (btnRefresh) {
      btnRefresh.addEventListener('click', () => this.actualizarContexto());
    }

    const selectTemp = document.getElementById('select-cosecha-temporada');
    if (selectTemp) {
      selectTemp.addEventListener('change', async (e) => {
        this.selectedTemporadaId = e.target.value;
        await this.actualizarContexto();
      });
    }

    const selectSuc = document.getElementById('select-cosecha-sucursal');
    if (selectSuc) {
      selectSuc.addEventListener('change', async (e) => {
        this.selectedSucursalId = e.target.value;
        await this.actualizarContexto();
      });
    }
  }

  async actualizarContexto() {
    if (!this.selectedTemporadaId || !this.selectedSucursalId) {
      const panel = document.getElementById('panel-cosecha-sucursal');
      if (panel) {
        panel.innerHTML = '<div class="text-center p-4 text-muted">Seleccione una temporada y sucursal.</div>';
      }
      return;
    }

    try {
      // 1. Cargar todas las cosechas de la temporada
      const cosechas = await cosechasService.listarPorTemporada(this.selectedTemporadaId);
      this.cosechasTemporada = Array.isArray(cosechas) ? cosechas : (cosechas.items || []);

      // 2. Buscar si existe cosecha para la sucursal seleccionada
      this.currentCosecha = this.cosechasTemporada.find(c =>
        String(c.sucursalId) === String(this.selectedSucursalId)
      ) || null;

      // 3. Si existe cosecha, obtener datos frescos por ID para asegurar detalles completos
      if (this.currentCosecha && this.currentCosecha.id) {
        try {
          const fresca = await cosechasService.obtenerPorId(this.currentCosecha.id);
          if (fresca) {
            this.currentCosecha = fresca;
          }
        } catch {
          // Si falla obtener por id, usar el objeto de la lista
        }
      }

      // 4. Renderizar panel de la cosecha actual y tabla general
      await this.renderPanelCosecha();
      this.renderTablaCosechasTemporada();
    } catch (err) {
      toast.error('Error al actualizar datos de cosecha: ' + err.message);
    }
  }

  async renderPanelCosecha() {
    const panel = document.getElementById('panel-cosecha-sucursal');
    if (!panel) return;

    const temporadaActual = this.temporadas.find(t => String(t.id) === String(this.selectedTemporadaId));
    const sucursalActual = this.sucursales.find(s => String(s.id) === String(this.selectedSucursalId));

    const nombreSucursal = sucursalActual ? sucursalActual.nombre : `Sucursal #${this.selectedSucursalId}`;
    const nombreTemporada = temporadaActual ? temporadaActual.nombre : `Temporada #${this.selectedTemporadaId}`;

    // CASO 1: NO EXISTE COSECHA PARA ESTA SUCURSAL EN LA TEMPORADA
    if (!this.currentCosecha) {
      panel.innerHTML = `
        <div class="card border-dashed p-4 text-center" style="border: 2px dashed var(--border); background: var(--surface);">
          <div style="max-width: 540px; margin: 0 auto;">
            <div class="mb-3">
              <span class="badge badge-warning" style="font-size: 0.85rem; padding: 0.4rem 0.8rem;">Sin Cosecha Abierta</span>
            </div>
            <h3 class="mb-2" style="font-size: 1.25rem;">No hay cosecha iniciada para ${escapeHtml(nombreSucursal)}</h3>
            <p class="text-muted text-sm mb-4">
              En la <strong>${escapeHtml(nombreTemporada)}</strong> aún no se ha dado inicio formal al proceso de cosecha para esta sucursal.
              Al iniciar la cosecha, podrá registrar los pesajes de bulbos por lote y proceder al cierre para acreditar el inventario.
            </p>

            <form id="form-iniciar-cosecha" class="text-left" style="background: var(--surface-alt); padding: 1.25rem; border-radius: var(--radius-md);">
              <div class="form-group mb-3">
                <label for="nueva-cosecha-obs" class="form-label text-xs font-bold uppercase">Observaciones Iniciales (Opcional)</label>
                <input type="text" id="nueva-cosecha-obs" class="form-control" placeholder="ej. Inicio de cosecha de bulbo primario en lote norte" />
              </div>

              <div class="d-flex justify-end gap-sm">
                <button type="submit" class="btn btn-primary" id="btn-submit-iniciar-cosecha">
                  <svg class="icon-svg sm" viewBox="0 0 24 24" width="16" height="16" stroke="currentColor" fill="none" stroke-width="2">
                    <path d="M12 5v14M5 12h14"></path>
                  </svg>
                  <span>Iniciar Cosecha en esta Sucursal</span>
                </button>
              </div>
            </form>
          </div>
        </div>
      `;

      const formIniciar = document.getElementById('form-iniciar-cosecha');
      if (formIniciar) {
        formIniciar.addEventListener('submit', async (e) => {
          e.preventDefault();
          await this.handleIniciarCosecha();
        });
      }
      return;
    }

    // CASO 2: EXISTE COSECHA (EnProceso o Cerrada)
    const cosecha = this.currentCosecha;
    const isCerrada = Number(cosecha.estado) === 2 || String(cosecha.nombreEstado || '').toLowerCase() === 'cerrada';
    const estadoNombre = isCerrada ? 'Cerrada' : 'En Proceso';
    const badgeClass = isCerrada ? 'badge-success' : 'badge-warning';

    const fechaInicioStr = cosecha.fechaInicio ? new Date(cosecha.fechaInicio).toLocaleDateString() : '-';
    const fechaFinStr = cosecha.fechaFin ? new Date(cosecha.fechaFin).toLocaleDateString() : (isCerrada ? 'Cerrada' : 'Abierta');
    const totalBulbos = Number(cosecha.totalCosechado) || 0;

    // Cargar lotes de la temporada/sucursal para el selector de detalle si está activa
    if (!isCerrada) {
      try {
        const lotes = await plantacionesService.listarLotesPorTemporada(this.selectedTemporadaId);
        const listaLotes = Array.isArray(lotes) ? lotes : (lotes.items || []);
        this.lotesDisponibles = listaLotes.filter(l =>
          String(l.sucursalId ?? (l.fila?.sucursalId ?? '')) === String(this.selectedSucursalId)
        );
      } catch {
        this.lotesDisponibles = [];
      }
    }

    panel.innerHTML = `
      <!-- Tarjeta de Información de la Cosecha Activa -->
      <div class="card mb-4">
        <div class="card-header d-flex justify-between align-center p-3 border-bottom flex-wrap gap-sm">
          <div>
            <h3 class="card-title text-base m-0">
              Cosecha #${escapeHtml(cosecha.id)} - ${escapeHtml(nombreSucursal)}
            </h3>
            <span class="text-xs text-muted">Temporada: ${escapeHtml(nombreTemporada)}</span>
          </div>
          <div class="d-flex align-center gap-sm">
            <span class="badge ${badgeClass}">${escapeHtml(estadoNombre)}</span>
            ${!isCerrada ? `
              <button class="btn btn-danger btn-sm" id="btn-cerrar-cosecha" title="Finaliza la recolección y acredita los bulbos al inventario">
                <svg class="icon-svg sm" viewBox="0 0 24 24" width="14" height="14" stroke="currentColor" fill="none" stroke-width="2">
                  <path d="M5 13l4 4L19 7"></path>
                </svg>
                <span>Cerrar Cosecha y Acreditar Stock</span>
              </button>
            ` : ''}
          </div>
        </div>

        <div class="card-body p-3">
          <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 1rem; margin-bottom: 1rem;">
            <div>
              <div class="text-xs text-muted uppercase font-bold">Fecha de Inicio</div>
              <div class="font-semibold text-main">${escapeHtml(fechaInicioStr)}</div>
            </div>
            <div>
              <div class="text-xs text-muted uppercase font-bold">Fecha de Cierre</div>
              <div class="font-semibold text-main">${escapeHtml(fechaFinStr)}</div>
            </div>
            <div>
              <div class="text-xs text-muted uppercase font-bold">Total Bulbos Registrados</div>
              <div class="font-semibold" style="font-size: 1.25rem; color: var(--primary);">
                ${totalBulbos.toLocaleString()} <span class="text-xs text-muted font-normal">unidades</span>
              </div>
            </div>
            <div>
              <div class="text-xs text-muted uppercase font-bold">Observaciones</div>
              <div class="text-sm text-main">${escapeHtml(cosecha.observaciones || 'Sin observaciones')}</div>
            </div>
          </div>

          ${isCerrada ? `
            <div style="background: rgba(34, 197, 94, 0.1); border-left: 4px solid var(--success); padding: 0.75rem 1rem; border-radius: var(--radius-sm); font-size: 0.875rem;">
              <strong>Cosecha Cerrada:</strong> El proceso de recolección en esta sucursal ha concluido. El stock cosechado fue acreditado automáticamente en el inventario comercial de los titulares vigentes.
            </div>
          ` : ''}
        </div>
      </div>

      <!-- Formulario para Registrar Detalle por Lote (Sólo si está EnProceso) -->
      ${!isCerrada ? `
        <div class="card mb-4" style="border: 1px solid var(--border);">
          <div class="card-header p-3 border-bottom">
            <h4 class="card-title text-sm m-0">Registrar Pesaje / Conteo de Bulbos por Lote</h4>
            <span class="text-xs text-muted">Seleccione un lote plantado e ingrese las cantidades cosechadas por cada calibre/clase</span>
          </div>
          <div class="card-body p-3">
            <form id="form-registrar-detalle">
              <div class="form-group mb-3">
                <label for="select-detalle-lote" class="form-label text-xs font-bold uppercase">Lote Plantado a Cosechar:</label>
                <select id="select-detalle-lote" class="form-control" required>
                  ${this.lotesDisponibles.length === 0
                    ? '<option value="">(No hay lotes plantados en esta sucursal para la temporada)</option>'
                    : '<option value="">-- Seleccione un Lote --</option>' + this.lotesDisponibles.map(l => {
                        const fila = l.filaIdentificacion || l.identificacionFila || l.fila?.identificacion || 'Fila';
                        const titular = l.nombreTitularVigente || l.nombreCliente || (l.cliente ? `${l.cliente.nombre || ''} ${l.cliente.apellido || ''}`.trim() : `Titular #${l.clienteId}`);
                        const totalPl = l.totalBulbos || l.cantidadPlantadaTotal || '-';
                        return `<option value="${escapeHtml(l.id)}">Lote #${escapeHtml(l.id)} - Fila: ${escapeHtml(fila)} | Titular: ${escapeHtml(titular)} (${escapeHtml(totalPl)} bulbos plantados)</option>`;
                      }).join('')
                  }
                </select>
              </div>

              <!-- Desglose de Cantidades por Clase (Clase 0 a Clase 4) -->
              <div class="mb-3">
                <label class="form-label text-xs font-bold uppercase mb-2">Cantidades Cosechadas por Calibre (Clases 0 a 4):</label>
                <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(130px, 1fr)); gap: 0.75rem;">
                  <div class="form-group mb-0">
                    <label for="input-cant-c0" class="text-xs text-muted" title="Grande > 12cm">Clase 0 (>12cm)</label>
                    <input type="number" id="input-cant-c0" class="form-control input-cosecha-clase" min="0" value="0" />
                  </div>
                  <div class="form-group mb-0">
                    <label for="input-cant-c1" class="text-xs text-muted" title="10 - 12cm">Clase 1 (10-12cm)</label>
                    <input type="number" id="input-cant-c1" class="form-control input-cosecha-clase" min="0" value="0" />
                  </div>
                  <div class="form-group mb-0">
                    <label for="input-cant-c2" class="text-xs text-muted" title="8 - 10cm">Clase 2 (8-10cm)</label>
                    <input type="number" id="input-cant-c2" class="form-control input-cosecha-clase" min="0" value="0" />
                  </div>
                  <div class="form-group mb-0">
                    <label for="input-cant-c3" class="text-xs text-muted" title="6 - 8cm">Clase 3 (6-8cm)</label>
                    <input type="number" id="input-cant-c3" class="form-control input-cosecha-clase" min="0" value="0" />
                  </div>
                  <div class="form-group mb-0">
                    <label for="input-cant-c4" class="text-xs text-muted" title="Semillero < 6cm">Clase 4 (&lt;6cm)</label>
                    <input type="number" id="input-cant-c4" class="form-control input-cosecha-clase" min="0" value="0" />
                  </div>
                </div>
              </div>

              <div class="d-flex justify-between align-center flex-wrap gap-sm pt-2 border-top">
                <div class="text-sm">
                  <span class="text-muted">Total a registrar para el lote:</span>
                  <strong id="preview-total-lote" class="text-primary font-bold ml-1">0</strong> bulbos
                </div>
                <button type="submit" class="btn btn-primary" id="btn-submit-detalle" ${this.lotesDisponibles.length === 0 ? 'disabled' : ''}>
                  <svg class="icon-svg sm" viewBox="0 0 24 24" width="14" height="14" stroke="currentColor" fill="none" stroke-width="2">
                    <path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"></path>
                    <polyline points="17 21 17 13 7 13 7 21"></polyline>
                    <polyline points="7 3 7 8 15 8"></polyline>
                  </svg>
                  <span>Guardar Conteo de Lote</span>
                </button>
              </div>
            </form>
          </div>
        </div>
      ` : ''}

      <!-- Tabla de Detalles Registrados -->
      <div class="card">
        <div class="card-header d-flex justify-between align-center p-3 border-bottom">
          <h4 class="card-title text-sm m-0">Detalle de Bulbos Cosechados Registrados</h4>
          <span class="text-xs text-muted">
            ${(cosecha.detalles || []).length} registros
          </span>
        </div>
        <div class="card-body p-0">
          <div class="table-responsive">
            <table class="table table-hover mb-0">
              <thead>
                <tr>
                  <th># Lote</th>
                  <th>Fila / Surco</th>
                  <th>Titular Original</th>
                  <th>Titular Vigente (Acreditar)</th>
                  <th>Clase de Bulbo</th>
                  <th style="text-align: right;">Cantidad Cosechada</th>
                </tr>
              </thead>
              <tbody id="tbody-detalles-cosecha">
                ${this.renderFilasDetalles(cosecha.detalles || [])}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    `;

    // Vincular eventos del panel activo
    if (!isCerrada) {
      // Evento inputs para calcular suma en vivo
      const inputs = panel.querySelectorAll('.input-cosecha-clase');
      inputs.forEach(inp => {
        inp.addEventListener('input', () => this.actualizarPreviewDetalle());
      });

      // Evento submit de registrar detalle
      const formDetalle = document.getElementById('form-registrar-detalle');
      if (formDetalle) {
        formDetalle.addEventListener('submit', async (e) => {
          e.preventDefault();
          await this.handleRegistrarDetalle();
        });
      }

      // Evento botón cerrar cosecha
      const btnCerrar = document.getElementById('btn-cerrar-cosecha');
      if (btnCerrar) {
        btnCerrar.addEventListener('click', async () => {
          await this.handleCerrarCosecha();
        });
      }
    }
  }

  renderFilasDetalles(detalles) {
    if (!detalles || detalles.length === 0) {
      return '<tr><td colspan="6" class="text-center p-4 text-muted">No se han registrado cantidades de bulbos para los lotes de esta cosecha.</td></tr>';
    }

    return detalles.map(d => {
      const loteId = d.loteId ?? d.LoteId ?? '-';
      const fila = d.identificacionFila ?? d.IdentificacionFila ?? '-';
      const orig = d.nombreTitularOriginal ?? d.NombreTitularOriginal ?? `Titular #${d.titularOriginalId ?? d.TitularOriginalId ?? ''}`;
      const vigente = d.nombreTitularVigente ?? d.NombreTitularVigente ?? `Titular #${d.titularVigenteId ?? d.TitularVigenteId ?? ''}`;
      const clase = d.nombreClase ?? d.NombreClase ?? `Clase ${d.claseBulbo ?? d.ClaseBulbo ?? ''}`;
      const cant = Number(d.cantidad ?? d.Cantidad) || 0;

      const isTransferido = (d.titularOriginalId ?? d.TitularOriginalId) !== (d.titularVigenteId ?? d.TitularVigenteId);

      return `
        <tr>
          <td><strong>#${escapeHtml(loteId)}</strong></td>
          <td>${escapeHtml(fila)}</td>
          <td>${escapeHtml(orig)}</td>
          <td>
            <span class="${isTransferido ? 'text-primary font-bold' : ''}">
              ${escapeHtml(vigente)} ${isTransferido ? '<span class="badge badge-info" style="font-size: 0.65rem;">Cesión</span>' : ''}
            </span>
          </td>
          <td><span class="badge badge-accent">${escapeHtml(clase)}</span></td>
          <td style="text-align: right; font-weight: 600;">${cant.toLocaleString()}</td>
        </tr>
      `;
    }).join('');
  }

  actualizarPreviewDetalle() {
    let sum = 0;
    for (let c = 0; c <= 4; c++) {
      const el = document.getElementById(`input-cant-c${c}`);
      sum += Number(el?.value) || 0;
    }
    const preview = document.getElementById('preview-total-lote');
    if (preview) preview.textContent = sum.toLocaleString();
  }

  renderTablaCosechasTemporada() {
    const tbody = document.getElementById('tbody-cosechas-temporada');
    const badgeCount = document.getElementById('total-cosechas-badge');
    if (!tbody) return;

    if (badgeCount) {
      badgeCount.textContent = `${this.cosechasTemporada.length} registradas`;
    }

    if (!this.cosechasTemporada || this.cosechasTemporada.length === 0) {
      tbody.innerHTML = '<tr><td colspan="7" class="text-center p-3 text-muted">No hay cosechas iniciadas en esta temporada.</td></tr>';
      return;
    }

    tbody.innerHTML = this.cosechasTemporada.map(c => {
      const isCerrada = Number(c.estado) === 2 || String(c.nombreEstado || '').toLowerCase() === 'cerrada';
      const badgeClass = isCerrada ? 'badge-success' : 'badge-warning';
      const estadoStr = isCerrada ? 'Cerrada' : 'En Proceso';
      const fechaIni = c.fechaInicio ? new Date(c.fechaInicio).toLocaleDateString() : '-';
      const fechaFin = c.fechaFin ? new Date(c.fechaFin).toLocaleDateString() : (isCerrada ? 'Cerrada' : 'En Proceso');
      const total = Number(c.totalCosechado) || 0;

      const isCurrentSelected = String(c.sucursalId) === String(this.selectedSucursalId);

      return `
        <tr style="${isCurrentSelected ? 'background: var(--surface-alt); font-weight: 500;' : ''}">
          <td><strong>#${escapeHtml(c.id)}</strong></td>
          <td>${escapeHtml(c.nombreSucursal || `Sucursal #${c.sucursalId}`)}</td>
          <td><span class="badge ${badgeClass}">${escapeHtml(estadoStr)}</span></td>
          <td>${escapeHtml(fechaIni)}</td>
          <td>${escapeHtml(fechaFin)}</td>
          <td style="text-align: right; font-weight: 600;">${total.toLocaleString()}</td>
          <td style="text-align: center;">
            <button class="btn btn-outline-primary btn-sm btn-select-sucursal-cosecha" data-sucursal-id="${escapeHtml(c.sucursalId)}">
              ${isCurrentSelected ? 'Gestionando' : 'Seleccionar'}
            </button>
          </td>
        </tr>
      `;
    }).join('');

    // Eventos para cambiar de sucursal haciendo click en la tabla
    const buttons = tbody.querySelectorAll('.btn-select-sucursal-cosecha');
    buttons.forEach(btn => {
      btn.addEventListener('click', async (e) => {
        const sucId = e.currentTarget.getAttribute('data-sucursal-id');
        if (sucId) {
          this.selectedSucursalId = sucId;
          const selectSuc = document.getElementById('select-cosecha-sucursal');
          if (selectSuc) selectSuc.value = sucId;
          await this.actualizarContexto();
        }
      });
    });
  }

  async handleIniciarCosecha() {
    const obsInput = document.getElementById('nueva-cosecha-obs');
    const observaciones = obsInput ? obsInput.value.trim() : '';

    try {
      const dto = {
        temporadaId: Number(this.selectedTemporadaId),
        sucursalId: Number(this.selectedSucursalId),
        observaciones: observaciones || null
      };

      const result = await cosechasService.crear(dto);
      toast.success('Cosecha iniciada con éxito para la sucursal.');
      await this.actualizarContexto();
    } catch (err) {
      toast.error('Error al iniciar la cosecha: ' + err.message);
    }
  }

  async handleRegistrarDetalle() {
    const selectLote = document.getElementById('select-detalle-lote');
    const loteId = selectLote ? selectLote.value : null;

    if (!loteId) {
      toast.warning('Debe seleccionar un lote plantado para registrar el pesaje.');
      return;
    }

    const cantidades = [];
    let sumaTotal = 0;

    for (let c = 0; c <= 4; c++) {
      const inp = document.getElementById(`input-cant-c${c}`);
      const cant = Number(inp?.value) || 0;
      if (cant < 0) {
        toast.warning('Las cantidades cosechadas no pueden ser negativas.');
        return;
      }
      cantidades.push({
        claseBulbo: c,
        cantidad: cant
      });
      sumaTotal += cant;
    }

    if (sumaTotal === 0) {
      toast.warning('Debe ingresar al menos una cantidad mayor a 0 para el lote seleccionado.');
      return;
    }

    try {
      const dto = {
        loteId: Number(loteId),
        cantidades
      };

      await cosechasService.registrarDetalle(this.currentCosecha.id, dto);
      toast.success('Pesaje del lote registrado correctamente.');
      await this.actualizarContexto();
    } catch (err) {
      toast.error('Error al registrar el detalle de cosecha: ' + err.message);
    }
  }

  async handleCerrarCosecha() {
    if (!this.currentCosecha) return;

    const confirma = confirm(
      '¿Está seguro de cerrar la cosecha de esta sucursal?\n\n' +
      'Esta acción marcará los lotes de la sucursal como "Cosechado" y acreditará de forma definitiva ' +
      'el stock de bulbos cosechados a los titulares vigentes (o empresa, según cesión).\n\n' +
      'Esta operación no podrá ser revertida.'
    );

    if (!confirma) return;

    try {
      await cosechasService.cerrar(this.currentCosecha.id);
      toast.success('Cosecha cerrada con éxito. El stock de bulbos ha sido acreditado a los titulares vigentes.');
      await this.actualizarContexto();
    } catch (err) {
      toast.error('Error al cerrar la cosecha: ' + err.message);
    }
  }
}
