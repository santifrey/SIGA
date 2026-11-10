/**
 * SIGA - views/plantaciones-view.js
 * Vista básica de Registro de Estructura Productiva, Plantaciones y Distribución de Lotes
 */

import { plantacionesService } from '../services/plantaciones-service.js';
import { temporadasService } from '../services/temporadas-service.js';
import { sucursalesService } from '../services/sucursales-service.js';
import { clientesService } from '../services/clientes-service.js';
import { toast } from '../utils/toast.js';
import { escapeHtml } from '../utils/escape-html.js';

export class PlantacionesView {
  constructor(router) {
    this.router = router;
    this.container = null;
    this.temporadas = [];
    this.sucursales = [];
    this.clientes = [];
    this.selectedTemporadaId = null;
    this.selectedSucursalId = null;
    this.lotes = [];
    this.currentPlantacion = null;
  }

  async render(rootElement) {
    this.container = rootElement;

    this.container.innerHTML = `
      <div class="fade-in">
        <!-- Encabezado de la Vista -->
        <div class="d-flex justify-between align-center mb-4 flex-wrap gap-md">
          <div>
            <h1 style="font-size: 1.5rem; margin-bottom: 0.25rem;">Estructura Productiva y Plantaciones</h1>
            <p class="text-muted text-sm">Registro de distribución física de bulbos en surcos/filas por cliente</p>
          </div>
          <div>
            <button class="btn btn-secondary" id="btn-refresh-plantaciones" title="Actualizar datos">
              <svg class="icon-svg sm" viewBox="0 0 24 24">
                <polyline points="23 4 23 10 17 10"></polyline>
                <polyline points="1 20 1 14 7 14"></polyline>
                <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"></path>
              </svg>
              <span>Refrescar</span>
            </button>
          </div>
        </div>

        <!-- Barra de Contexto: Selectores Principales -->
        <div class="card mb-4" style="background: var(--surface);">
          <div class="card-body p-3">
            <div class="form-row two-cols">
              <div class="form-group mb-0">
                <label for="select-temporada" class="form-label font-bold text-xs uppercase" style="color: var(--primary);">
                  1. Seleccionar Temporada Productiva:
                </label>
                <select id="select-temporada" class="form-control" style="font-weight: 600;">
                  <option value="">Cargando temporadas...</option>
                </select>
              </div>

              <div class="form-group mb-0">
                <label for="select-sucursal" class="form-label font-bold text-xs uppercase" style="color: var(--accent);">
                  2. Sucursal / Campo de Siembra:
                </label>
                <select id="select-sucursal" class="form-control">
                  <option value="">Cargando sucursales...</option>
                </select>
              </div>
            </div>
          </div>
        </div>

        <!-- Formulario Básico: Registrar Distribución Física en Campo -->
        <div class="card mb-4">
          <div class="card-header">
            <h3 class="card-title text-base">Registrar Lote Plantado (Distribución Física)</h3>
            <span class="text-xs text-muted">Asigne la fila física y las cantidades de bulbos por clase</span>
          </div>
          <div class="card-body">
            <form id="form-distribucion" novalidate>
              <div class="form-row two-cols mb-3">
                <div class="form-group">
                  <label for="fila-id" class="form-label">
                    Identificador de Fila / Surco <span class="required-mark">*</span>
                  </label>
                  <input
                    type="text"
                    id="fila-id"
                    class="form-control"
                    placeholder="ej. F-01, Fila Norte 3"
                    required
                  />
                  <small class="text-muted text-xs">Identificador único del surco o bancal en el campo</small>
                </div>

                <div class="form-group">
                  <label for="select-cliente" class="form-label">
                    Cliente (Titular del Lote) <span class="required-mark">*</span>
                  </label>
                  <select id="select-cliente" class="form-control" required>
                    <option value="">-- Seleccionar Cliente --</option>
                  </select>
                  <small class="text-muted text-xs">Inversor o propietario de los bulbos plantados</small>
                </div>
              </div>

              <div class="form-group mb-3">
                <label class="form-label font-semibold mb-2">Cantidades de Bulbos por Clase:</label>
                <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(130px, 1fr)); gap: var(--spacing-sm);">
                  <div class="p-2" style="background: var(--surface-alt); border-radius: var(--radius-sm); border: 1px solid var(--border);">
                    <label for="cant-c0" class="text-xs font-bold d-block mb-1" style="color: var(--primary);">Clase 0 (Extra Gde)</label>
                    <input type="number" id="cant-c0" class="form-control cant-input" min="0" value="0" />
                  </div>
                  <div class="p-2" style="background: var(--surface-alt); border-radius: var(--radius-sm); border: 1px solid var(--border);">
                    <label for="cant-c1" class="text-xs font-bold d-block mb-1" style="color: var(--primary);">Clase 1 (Grande)</label>
                    <input type="number" id="cant-c1" class="form-control cant-input" min="0" value="0" />
                  </div>
                  <div class="p-2" style="background: var(--surface-alt); border-radius: var(--radius-sm); border: 1px solid var(--border);">
                    <label for="cant-c2" class="text-xs font-bold d-block mb-1" style="color: var(--primary);">Clase 2 (Mediano)</label>
                    <input type="number" id="cant-c2" class="form-control cant-input" min="0" value="0" />
                  </div>
                  <div class="p-2" style="background: var(--surface-alt); border-radius: var(--radius-sm); border: 1px solid var(--border);">
                    <label for="cant-c3" class="text-xs font-bold d-block mb-1" style="color: var(--primary);">Clase 3 (Chico)</label>
                    <input type="number" id="cant-c3" class="form-control cant-input" min="0" value="0" />
                  </div>
                  <div class="p-2" style="background: var(--surface-alt); border-radius: var(--radius-sm); border: 1px solid var(--border);">
                    <label for="cant-c4" class="text-xs font-bold d-block mb-1" style="color: var(--primary);">Clase 4 (Semillero)</label>
                    <input type="number" id="cant-c4" class="form-control cant-input" min="0" value="0" />
                  </div>
                </div>
              </div>

              <div class="d-flex justify-between align-center flex-wrap gap-md mt-4 pt-3 border-top">
                <div class="text-sm font-semibold">
                  <span>Total Bulbos a Plantar:</span>
                  <span id="label-total-bulbos" class="font-bold text-main ml-2" style="font-size: 1.15rem; color: var(--accent-700);">0</span>
                </div>
                <button type="submit" class="btn btn-primary" id="btn-registrar-lote">
                  <svg class="icon-svg sm" viewBox="0 0 24 24">
                    <polyline points="20 6 9 17 4 12"></polyline>
                  </svg>
                  <span class="btn-text">Registrar Lote Plantado</span>
                  <span class="btn-spinner"></span>
                </button>
              </div>
            </form>
          </div>
        </div>

        <!-- Tabla de Lotes Registrados para la Temporada -->
        <div class="table-card">
          <div class="table-header p-3 border-bottom d-flex justify-between align-center">
            <h4 class="font-semibold text-main m-0">Lotes Registrados en la Temporada</h4>
            <span class="text-xs text-muted" id="total-lotes-count">0 lotes</span>
          </div>

          <div class="table-responsive">
            <table class="table" id="tabla-lotes">
              <thead>
                <tr>
                  <th style="width: 100px;">Fila</th>
                  <th>Lote #</th>
                  <th>Cliente (Titular)</th>
                  <th style="text-align: right;">Total Bulbos</th>
                  <th>Desglose por Clase (C0 | C1 | C2 | C3 | C4)</th>
                  <th>Fecha Registro</th>
                </tr>
              </thead>
              <tbody id="tbody-lotes">
                <tr class="table-loading-row">
                  <td colspan="6" class="text-center py-4">
                    <span>Cargando lotes...</span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    `;

    this._bindEvents();
    await this._loadInitialDropdowns();
  }

  _bindEvents() {
    this.container.querySelector('#btn-refresh-plantaciones').addEventListener('click', () => {
      this._reloadLotes();
    });

    // Cambio de temporada seleccionada
    const tempSelect = this.container.querySelector('#select-temporada');
    tempSelect.addEventListener('change', (e) => {
      this.selectedTemporadaId = e.target.value;
      this._reloadLotes();
    });

    // Cambio de sucursal
    const sucSelect = this.container.querySelector('#select-sucursal');
    sucSelect.addEventListener('change', (e) => {
      this.selectedSucursalId = e.target.value;
    });

    // Cálculo dinámico de total al tipear en cantidades
    const cantInputs = this.container.querySelectorAll('.cant-input');
    cantInputs.forEach(input => {
      input.addEventListener('input', () => this._updateTotalPreview());
    });

    // Submit del formulario
    this.container.querySelector('#form-distribucion').addEventListener('submit', (e) => {
      this._handleSubmitDistribucion(e);
    });
  }

  _updateTotalPreview() {
    let total = 0;
    for (let c = 0; c <= 4; c++) {
      const el = this.container.querySelector(`#cant-c${c}`);
      total += Number(el?.value) || 0;
    }
    const label = this.container.querySelector('#label-total-bulbos');
    if (label) label.textContent = total.toLocaleString();
  }

  async _loadInitialDropdowns() {
    try {
      const [temporadas, sucursales, clientesRes] = await Promise.all([
        temporadasService.listar().catch(() => []),
        sucursalesService.listar().catch(() => []),
        clientesService.listar({ pageSize: 100, estado: 'activos' }).catch(() => ({ items: [] }))
      ]);

      this.temporadas = temporadas || [];
      this.sucursales = Array.isArray(sucursales) ? sucursales : (sucursales.items || []);
      this.clientes = Array.isArray(clientesRes) ? clientesRes : (clientesRes.items || []);

      // Poblar selector de temporadas
      const tempSelect = this.container.querySelector('#select-temporada');
      if (this.temporadas.length > 0) {
        // Encontrar activa o primera
        const activa = this.temporadas.find(t => t.estado !== 'Cerrada') || this.temporadas[0];
        this.selectedTemporadaId = activa.id;

        tempSelect.innerHTML = this.temporadas.map(t => `
          <option value="${escapeHtml(t.id)}" ${String(t.id) === String(this.selectedTemporadaId) ? 'selected' : ''}>
            ${escapeHtml(t.nombre)} (${escapeHtml(t.estado)})
          </option>
        `).join('');
      } else {
        tempSelect.innerHTML = '<option value="">No hay temporadas creadas</option>';
      }

      // Poblar selector de sucursales
      const sucSelect = this.container.querySelector('#select-sucursal');
      if (this.sucursales.length > 0) {
        this.selectedSucursalId = this.sucursales[0].id;
        sucSelect.innerHTML = this.sucursales.map(s => `
          <option value="${escapeHtml(s.id)}">
            ${escapeHtml(s.nombre)} (${escapeHtml(s.ciudad || 'Principal')})
          </option>
        `).join('');
      } else {
        sucSelect.innerHTML = '<option value="1">Sucursal Central</option>';
        this.selectedSucursalId = 1;
      }

      // Poblar selector de clientes
      const cliSelect = this.container.querySelector('#select-cliente');
      if (this.clientes.length > 0) {
        cliSelect.innerHTML = `
          <option value="">-- Seleccionar Cliente Titular --</option>
          ${this.clientes.map(c => `
            <option value="${escapeHtml(c.id)}">
              ${c.codigo ? `[${escapeHtml(c.codigo)}] ` : ''}${escapeHtml(c.nombre)} ${escapeHtml(c.apellido)} (${escapeHtml(c.documento || c.email || 'Titular')})
            </option>
          `).join('')}
        `;
      } else {
        cliSelect.innerHTML = '<option value="">Sin clientes disponibles</option>';
      }

      if (this.selectedTemporadaId) {
        await this._reloadLotes();
      }
    } catch (err) {
      toast.error('Error al inicializar listas para plantación: ' + err.message);
    }
  }

  async _reloadLotes() {
    if (!this.selectedTemporadaId) return;

    const tbody = this.container.querySelector('#tbody-lotes');
    tbody.innerHTML = `
      <tr class="table-loading-row">
        <td colspan="6" class="text-center py-4">
          <span>Consultando lotes de la temporada...</span>
        </td>
      </tr>
    `;

    try {
      // Obtener o inicializar la plantación de la temporada
      this.currentPlantacion = await plantacionesService.obtenerPorTemporada(this.selectedTemporadaId);

      // Obtener lotes de la temporada
      const lotes = await plantacionesService.listarLotes(this.selectedTemporadaId);
      this.lotes = Array.isArray(lotes) ? lotes : (lotes.items || []);

      this._renderTablaLotes();
    } catch (err) {
      tbody.innerHTML = `
        <tr>
          <td colspan="6" class="text-center py-4 text-danger">
            Error al consultar lotes: ${escapeHtml(err.message)}
          </td>
        </tr>
      `;
    }
  }

  _renderTablaLotes() {
    const tbody = this.container.querySelector('#tbody-lotes');
    const countEl = this.container.querySelector('#total-lotes-count');

    countEl.textContent = `${this.lotes.length} lotes registrados`;

    if (this.lotes.length === 0) {
      tbody.innerHTML = `
        <tr>
          <td colspan="6" class="text-center py-4 text-muted">
            No se han registrado lotes para esta temporada aún. Utilice el formulario superior para registrar el primero.
          </td>
        </tr>
      `;
      return;
    }

    tbody.innerHTML = this.lotes.map(l => {
      // Resolver nombre de cliente
      let clienteNombre = l.clienteNombre;
      if (!clienteNombre && l.clienteId) {
        const c = this.clientes.find(cli => String(cli.id) === String(l.clienteId));
        clienteNombre = c ? `${c.nombre} ${c.apellido}` : `Cliente #${l.clienteId}`;
      }

      // Desglose por clases
      const desgloseMap = {};
      (l.cantidades || []).forEach(item => {
        desgloseMap[item.claseBulbo] = item.cantidad;
      });

      const c0 = desgloseMap[0] || 0;
      const c1 = desgloseMap[1] || 0;
      const c2 = desgloseMap[2] || 0;
      const c3 = desgloseMap[3] || 0;
      const c4 = desgloseMap[4] || 0;

      return `
        <tr>
          <td>
            <span class="font-bold text-xs" style="color: var(--accent-800); background: var(--accent-100); padding: 3px 8px; border-radius: var(--radius-sm);">
              ${escapeHtml(l.filaIdentificacion || 'F-??')}
            </span>
          </td>
          <td>
            <span class="font-semibold text-main">${escapeHtml(l.numeroLote || `LOT-${l.id}`)}</span>
          </td>
          <td>
            <div class="font-medium text-main">${escapeHtml(clienteNombre || 'Sin asignar')}</div>
          </td>
          <td style="text-align: right;">
            <span class="font-bold" style="color: var(--primary);">${(l.totalBulbos || 0).toLocaleString()}</span>
          </td>
          <td>
            <div class="text-xs font-mono" style="background: var(--surface-alt); padding: 4px 8px; border-radius: var(--radius-sm); display: inline-block;">
              <strong>C0:</strong> ${escapeHtml(c0)} |
              <strong>C1:</strong> ${escapeHtml(c1)} |
              <strong>C2:</strong> ${escapeHtml(c2)} |
              <strong>C3:</strong> ${escapeHtml(c3)} |
              <strong>C4:</strong> ${escapeHtml(c4)}
            </div>
          </td>
          <td>
            <span class="text-xs text-muted">
              ${escapeHtml(l.fechaRegistro ? new Date(l.fechaRegistro).toLocaleDateString() : '-')}
            </span>
          </td>
        </tr>
      `;
    }).join('');
  }

  async _handleSubmitDistribucion(e) {
    e.preventDefault();

    if (!this.selectedTemporadaId) {
      toast.warning('Seleccione una temporada válida.');
      return;
    }

    const filaIdentificacion = this.container.querySelector('#fila-id').value.trim();
    const clienteId = this.container.querySelector('#select-cliente').value;

    if (!filaIdentificacion) {
      toast.warning('Ingrese el identificador de la fila (ej. F-01).');
      return;
    }
    if (!clienteId) {
      toast.warning('Seleccione el cliente titular del lote.');
      return;
    }

    const cantidades = [];
    let totalBulbos = 0;
    for (let c = 0; c <= 4; c++) {
      const q = Number(this.container.querySelector(`#cant-c${c}`).value) || 0;
      cantidades.push({ claseBulbo: c, cantidad: q });
      totalBulbos += q;
    }

    if (totalBulbos <= 0) {
      toast.warning('Debe ingresar al menos una cantidad de bulbos mayor a cero.');
      return;
    }

    const submitBtn = this.container.querySelector('#btn-registrar-lote');
    submitBtn.classList.add('is-loading');
    submitBtn.disabled = true;

    try {
      // Asegurar que exista una plantación creada para la temporada seleccionada
      let plantacion = this.currentPlantacion;
      if (!plantacion || !plantacion.id) {
        plantacion = await plantacionesService.obtenerPorTemporada(this.selectedTemporadaId);
        if (!plantacion || !plantacion.id) {
          plantacion = await plantacionesService.crear({
            temporadaId: this.selectedTemporadaId,
            observaciones: 'Plantación general del ciclo'
          });
        }
      }

      const payload = {
        temporadaId: Number(this.selectedTemporadaId),
        sucursalId: Number(this.selectedSucursalId || 1),
        filaIdentificacion,
        capacidadFila: totalBulbos,
        clienteId: Number(clienteId),
        cantidades
      };

      await plantacionesService.registrarDistribucion(plantacion.id, payload);

      toast.success(`Lote en fila "${filaIdentificacion}" registrado exitosamente con ${totalBulbos.toLocaleString()} bulbos.`);

      // Limpiar formulario
      this.container.querySelector('#fila-id').value = '';
      for (let c = 0; c <= 4; c++) {
        this.container.querySelector(`#cant-c${c}`).value = '0';
      }
      this._updateTotalPreview();

      // Recargar tabla de lotes
      await this._reloadLotes();
    } catch (err) {
      toast.error(err.message || 'Error al registrar lote plantado');
    } finally {
      submitBtn.classList.remove('is-loading');
      submitBtn.disabled = false;
    }
  }
}
