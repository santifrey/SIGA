/**
 * SIGA - views/stock-view.js
 * Vista Funcional de Consulta de Stock por Cliente (Feature 32)
 */

import { stockService, CLASES_BULBO } from '../services/stock-service.js';
import { clientesService } from '../services/clientes-service.js';
import { sucursalesService } from '../services/sucursales-service.js';
import { loadTemplate } from '../utils/template-loader.js';
import { toast } from '../utils/toast.js';
import { escapeHtml } from '../utils/escape-html.js';

export class StockView {
  constructor(router) {
    this.router = router;
    this.container = null;
    this.selectedClienteId = '1';
  }

  async render(rootElement) {
    this.container = rootElement;
    this.container.innerHTML = await loadTemplate('stock.html');

    this._bindEvents();
    await this._loadTitularesSelect();
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

  async _loadTitularesSelect() {
    const select = this.container.querySelector('#select-cliente-stock');
    if (!select) return;
    select.innerHTML = '';

    try {
      // 1. Cargar Sucursales activas
      const sucursales = await sucursalesService.listar({ estado: 'activas' });
      if (sucursales && sucursales.length > 0) {
        const groupSuc = document.createElement('optgroup');
        groupSuc.label = '🏢 Sucursales';
        sucursales.forEach(s => {
          const opt = document.createElement('option');
          const partId = s.participanteComercialId || s.id;
          opt.value = String(partId);
          opt.textContent = `🏢 ${s.nombre} (${s.codigo || 'SUC-' + s.id})`;
          groupSuc.appendChild(opt);
        });
        select.appendChild(groupSuc);
        this.selectedClienteId = String(sucursales[0].participanteComercialId || sucursales[0].id);
      } else {
        const opt = document.createElement('option');
        opt.value = '1';
        opt.textContent = '🏢 Sede Central (Depósito Central)';
        select.appendChild(opt);
        this.selectedClienteId = '1';
      }

      // 2. Cargar Clientes activos
      const data = await clientesService.listar({ page: 1, pageSize: 200, estado: 'activos' });
      const clientes = data?.items || [];
      if (clientes && clientes.length > 0) {
        const groupCli = document.createElement('optgroup');
        groupCli.label = '👤 Clientes';
        clientes.forEach(c => {
          const opt = document.createElement('option');
          opt.value = String(c.id);
          const nombreCompleto = `${c.nombre || ''} ${c.apellido || ''}`.trim() || c.nombreCompleto || `Cliente #${c.id}`;
          const docInfo = c.documento ? ` - DNI ${c.documento}` : '';
          opt.textContent = `👤 ${nombreCompleto}${docInfo} (${c.codigo || 'CLI-' + c.id})`;
          groupCli.appendChild(opt);
        });
        select.appendChild(groupCli);
      }

      select.value = this.selectedClienteId;
    } catch (err) {
      console.warn('[StockView] No se pudo cargar titulares de stock:', err);
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
