/**
 * SIGA - views/stock-view.js
 * Vista Funcional de Consulta de Stock por Cliente (Feature 32)
 */

import { stockService, CLASES_BULBO } from '../services/stock-service.js';
import { clientesService } from '../services/clientes-service.js';
import { toast } from '../utils/toast.js';
import { escapeHtml } from '../utils/escape-html.js';

export class StockView {
  constructor(router) {
    this.router = router;
    this.container = null;
    this.selectedClienteId = 'empresa';
  }

  async render(rootElement) {
    this.container = rootElement;

    this.container.innerHTML = `
      <div class="fade-in">
        <!-- Encabezado simple -->
        <div class="d-flex justify-between align-center mb-4 flex-wrap gap-md">
          <div>
            <h1 style="font-size: 1.5rem; margin-bottom: 0.25rem;">Consulta de Stock de Bulbos</h1>
            <p class="text-muted text-sm">Disponibilidad actual por clase para la Empresa o Cliente seleccionado</p>
          </div>
          <div>
            <button class="btn btn-secondary btn-sm" id="btn-refresh-stock">
              <svg class="icon-svg sm" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="margin-right: 0.25rem;">
                <polyline points="23 4 23 10 17 10"></polyline>
                <polyline points="1 20 1 14 7 14"></polyline>
                <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"></path>
              </svg>
              <span>Actualizar</span>
            </button>
          </div>
        </div>

        <!-- Selector simple de Cliente -->
        <div class="table-card mb-4" style="padding: 1rem 1.25rem;">
          <div class="d-flex align-center gap-md flex-wrap">
            <label for="select-cliente-stock" class="form-label" style="margin: 0; min-width: 130px;">
              <strong>Titular del Stock:</strong>
            </label>
            <div style="flex: 1; max-width: 400px;">
              <select id="select-cliente-stock" class="form-control">
                <option value="empresa">🏢 Empresa (Depósito Central)</option>
              </select>
            </div>
          </div>
        </div>

        <!-- Tabla simple de Stock de 5 Clases -->
        <div class="table-card">
          <div class="table-responsive">
            <table class="table" id="stock-table">
              <thead>
                <tr>
                  <th style="width: 15%;">Código</th>
                  <th style="width: 45%;">Clasificación de Bulbo</th>
                  <th style="width: 40%; text-align: right;">Cantidad Disponible (Unidades)</th>
                </tr>
              </thead>
              <tbody id="stock-table-body">
                <tr>
                  <td colspan="3" class="text-center text-muted" style="padding: 2rem;">Cargando stock...</td>
                </tr>
              </tbody>
              <tfoot id="stock-table-foot" style="background: var(--surface-alt); font-weight: 700;">
                <tr>
                  <td colspan="2" style="padding: 1rem; text-align: right;">TOTAL BULBOS EN DEPÓSITO:</td>
                  <td id="stock-total-cell" style="padding: 1rem; text-align: right; color: var(--primary); font-size: 1.1rem;">0</td>
                </tr>
              </tfoot>
            </table>
          </div>
        </div>
      </div>
    `;

    this._bindEvents();
    await this._loadClientesSelect();
    await this._loadStock();
  }

  _bindEvents() {
    const select = this.container.querySelector('#select-cliente-stock');
    if (select) {
      select.addEventListener('change', (e) => {
        this.selectedClienteId = e.target.value;
        this._loadStock();
      });
    }

    const refreshBtn = this.container.querySelector('#btn-refresh-stock');
    if (refreshBtn) {
      refreshBtn.addEventListener('click', () => {
        this._loadStock();
      });
    }
  }

  async _loadClientesSelect() {
    const select = this.container.querySelector('#select-cliente-stock');
    if (!select) return;
    try {
      const data = await clientesService.listar({ page: 1, pageSize: 100, estado: 'activos' });
      const clientes = data?.items || [];

      clientes.forEach(c => {
        const opt = document.createElement('option');
        opt.value = String(c.id);
        opt.textContent = `👤 ${c.nombre} ${c.apellido} (${c.codigo || 'CLI-' + c.id})`;
        select.appendChild(opt);
      });
    } catch (err) {
      console.warn('[StockView] No se pudo cargar listado de clientes:', err);
    }
  }

  async _loadStock() {
    const tbody = this.container.querySelector('#stock-table-body');
    const totalCell = this.container.querySelector('#stock-total-cell');
    if (!tbody || !totalCell) return;

    tbody.innerHTML = `<tr><td colspan="3" class="text-center text-muted" style="padding: 1.5rem;">Consultando stock...</td></tr>`;

    try {
      const rawResult = await stockService.obtenerStockPorCliente(this.selectedClienteId);

      // Defensive: unwrap array regardless of wrapper structure
      let items = [];
      if (Array.isArray(rawResult)) {
        items = rawResult;
      } else if (rawResult && typeof rawResult === 'object') {
        if (Array.isArray(rawResult.stockPorClase)) {
          items = rawResult.stockPorClase;
        } else if (Array.isArray(rawResult.items)) {
          items = rawResult.items;
        } else if (Array.isArray(rawResult.data)) {
          items = rawResult.data;
        }
      }

      // Normalizar las 5 clases físicas fijas (Clase 0 a Clase 4)
      const rows = [0, 1, 2, 3, 4].map(claseNum => {
        const found = Array.isArray(items) ? items.find(it => {
          const c = it.clase !== undefined ? it.clase : it.claseBulbo;
          return Number(c) === claseNum;
        }) : null;

        const meta = CLASES_BULBO[claseNum];
        return {
          claseBulbo: claseNum,
          nombreClase: (found && found.nombreClase) || (meta ? meta.nombre : `Clase ${claseNum}`),
          cantidad: (found && Number(found.cantidad)) || 0
        };
      });

      // Si vienen ítems MIX registrados, también incluir la fila comercial
      const mixFound = Array.isArray(items) ? items.find(it => {
        const c = it.clase !== undefined ? it.clase : it.claseBulbo;
        return Number(c) === 5 || String(c).toUpperCase() === 'MIX';
      }) : null;
      if (mixFound && Number(mixFound.cantidad) > 0) {
        const metaMix = CLASES_BULBO[5];
        rows.push({
          claseBulbo: 'MIX',
          nombreClase: mixFound.nombreClase || (metaMix ? metaMix.nombre : 'MIX Comercial'),
          cantidad: Number(mixFound.cantidad) || 0
        });
      }

      const total = rows.reduce((acc, r) => acc + r.cantidad, 0);

      tbody.innerHTML = rows.map(r => `
        <tr>
          <td><span class="badge badge-primary font-bold">${typeof r.claseBulbo === 'number' ? 'Clase ' + r.claseBulbo : escapeHtml(r.claseBulbo)}</span></td>
          <td class="font-medium text-main">${escapeHtml(r.nombreClase)}</td>
          <td style="text-align: right;">
            <span class="font-bold ${r.cantidad > 0 ? 'text-main' : 'text-muted'}" style="font-size: 1.05rem;">
              ${r.cantidad.toLocaleString('es-AR')}
            </span>
          </td>
        </tr>
      `).join('');

      totalCell.textContent = `${total.toLocaleString('es-AR')} unidades`;
    } catch (err) {
      console.error('[StockView] Error al cargar stock:', err);
      tbody.innerHTML = `<tr><td colspan="3" class="text-center text-danger" style="padding: 1.5rem;">${escapeHtml(err.message || 'Error al obtener stock')}</td></tr>`;
      totalCell.textContent = '0';
      toast.error('No se pudo consultar el stock.');
    }
  }
}
