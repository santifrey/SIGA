/**
 * SIGA - views/stock-view.js
 * Vista Funcional de Consulta de Stock con Búsqueda Rápida de Clientes y Selector de Sucursal
 */

import { stockService, CLASES_BULBO } from '../services/stock-service.js';
import { clientesService } from '../services/clientes-service.js';
import { sucursalesService } from '../services/sucursales-service.js';
import { authService } from '../services/auth-service.js';
import { ClienteAutocomplete } from '../components/cliente-autocomplete.js';
import { loadTemplate } from '../utils/template-loader.js';
import { toast } from '../utils/toast.js';
import { escapeHtml } from '../utils/escape-html.js';

export class StockView {
  constructor(router) {
    this.router = router;
    this.container = null;
    this.selectedClienteId = null;
    this.modo = 'sucursal'; // 'sucursal' | 'cliente'
    this.clienteAutocomplete = null;
  }

  async render(rootElement) {
    this.container = rootElement;
    this.container.innerHTML = await loadTemplate('stock.html');

    this._initAutocomplete();
    this._bindEvents();
    await this._initModo();
  }

  _initAutocomplete() {
    const acEl = this.container.querySelector('#stock-cliente-autocomplete');
    if (!acEl) return;

    this.clienteAutocomplete = new ClienteAutocomplete({
      container: acEl,
      name: 'stock-cliente-id',
      placeholder: 'Escriba DNI, Nombre, Apellido o Código (ej: CLI-0001)...',
      onSelect: (cliente) => {
        if (cliente && cliente.id) {
          this.selectedClienteId = String(cliente.id);
          this._loadStock();
        }
      },
      onClear: () => {
        this.selectedClienteId = null;
        this._showPromptSelectCliente();
      }
    });
  }

  _bindEvents() {
    const btnRefresh = this.container.querySelector('#btn-refresh-stock');
    if (btnRefresh) {
      btnRefresh.addEventListener('click', () => {
        if (this.selectedClienteId) {
          this._loadStock();
        } else if (this.modo === 'cliente') {
          this._showPromptSelectCliente();
        }
      });
    }

    const btnModoSucursal = this.container.querySelector('#btn-modo-sucursal');
    const btnModoCliente = this.container.querySelector('#btn-modo-cliente');

    if (btnModoSucursal) {
      btnModoSucursal.addEventListener('click', () => {
        this._cambiarModo('sucursal');
      });
    }

    if (btnModoCliente) {
      btnModoCliente.addEventListener('click', () => {
        this._cambiarModo('cliente');
      });
    }

    const selectSucursal = this.container.querySelector('#select-sucursal-stock');
    if (selectSucursal) {
      selectSucursal.addEventListener('change', (e) => {
        this.selectedClienteId = e.target.value;
        if (this.selectedClienteId) {
          this._loadStock();
        }
      });
    }
  }

  async _initModo() {
    const alcanceStock = authService.obtenerAlcance('STOCK_VER');

    if (alcanceStock === 'PROPIO') {
      // Modo exclusivo Cliente: no tiene acceso a depósitos de sucursal
      const modoContainer = this.container.querySelector('#modo-titular-container');
      const panelSucursal = this.container.querySelector('#panel-sucursal');
      const panelCliente = this.container.querySelector('#panel-cliente');

      if (modoContainer) modoContainer.classList.add('hidden');
      if (panelSucursal) panelSucursal.classList.add('hidden');
      if (panelCliente) panelCliente.classList.remove('hidden');

      this.modo = 'cliente';

      // Cargar automáticamente su propio perfil de cliente
      try {
        const res = await clientesService.listar({ page: 1, pageSize: 1 });
        const propio = res?.items?.[0];
        if (propio && propio.id) {
          this.selectedClienteId = String(propio.id);
          this.clienteAutocomplete?.setValue(propio);

          // Ocultar botón de cambiar para usuarios con alcance estrictamente propio
          const btnChange = this.container.querySelector('#ac-btn-change-stock-cliente-id');
          if (btnChange) btnChange.classList.add('hidden');

          await this._loadStock();
        } else {
          this._showPromptSelectCliente();
        }
      } catch (err) {
        console.warn('[StockView] No se pudo cargar cliente propio:', err);
        this._showPromptSelectCliente();
      }
    } else {
      // Personal interno / Administradores: pueden consultar depósitos de sucursales o buscar clientes
      await this._loadSucursales();
      this._cambiarModo('sucursal');
    }
  }

  _cambiarModo(nuevoModo) {
    this.modo = nuevoModo;
    const btnModoSucursal = this.container.querySelector('#btn-modo-sucursal');
    const btnModoCliente = this.container.querySelector('#btn-modo-cliente');
    const panelSucursal = this.container.querySelector('#panel-sucursal');
    const panelCliente = this.container.querySelector('#panel-cliente');

    if (nuevoModo === 'sucursal') {
      btnModoSucursal?.classList.add('btn-primary');
      btnModoSucursal?.classList.remove('btn-secondary');
      btnModoCliente?.classList.add('btn-secondary');
      btnModoCliente?.classList.remove('btn-primary');

      panelSucursal?.classList.remove('hidden');
      panelCliente?.classList.add('hidden');

      const selectSucursal = this.container.querySelector('#select-sucursal-stock');
      if (selectSucursal && selectSucursal.value) {
        this.selectedClienteId = selectSucursal.value;
        this._loadStock();
      }
    } else {
      btnModoCliente?.classList.add('btn-primary');
      btnModoCliente?.classList.remove('btn-secondary');
      btnModoSucursal?.classList.add('btn-secondary');
      btnModoSucursal?.classList.remove('btn-primary');

      panelCliente?.classList.remove('hidden');
      panelSucursal?.classList.add('hidden');

      const clienteSel = this.clienteAutocomplete?.getValue();
      if (clienteSel) {
        this.selectedClienteId = String(clienteSel);
        this._loadStock();
      } else {
        this.selectedClienteId = null;
        this._showPromptSelectCliente();
      }
    }
  }

  async _loadSucursales() {
    const select = this.container.querySelector('#select-sucursal-stock');
    if (!select) return;
    select.innerHTML = '<option value="">Cargando sucursales...</option>';

    try {
      const sucursales = await sucursalesService.listar({ estado: 'activas' });
      select.innerHTML = '';

      if (sucursales && sucursales.length > 0) {
        sucursales.forEach((s) => {
          const opt = document.createElement('option');
          const partId = s.participanteComercialId || s.id;
          opt.value = String(partId);
          opt.textContent = `🏢 ${s.nombre} (${s.codigo || 'SUC-' + s.id})${s.esCasaMatriz ? ' [Sede Central]' : ''}`;
          select.appendChild(opt);
        });
        select.value = String(sucursales[0].participanteComercialId || sucursales[0].id);
        this.selectedClienteId = select.value;
      } else {
        const opt = document.createElement('option');
        opt.value = '1';
        opt.textContent = '🏢 Sede Central (Depósito Principal)';
        select.appendChild(opt);
        this.selectedClienteId = '1';
      }
    } catch (err) {
      console.warn('[StockView] Error al cargar sucursales:', err);
      select.innerHTML = '<option value="1">🏢 Sede Central (Depósito Principal)</option>';
      this.selectedClienteId = '1';
    }
  }

  _showPromptSelectCliente() {
    const tbody = this.container.querySelector('#stock-table-body');
    const totalCell = this.container.querySelector('#stock-total-cell');
    if (tbody) {
      tbody.innerHTML = `
        <tr>
          <td colspan="3" class="text-center text-muted" style="padding: 3rem 1.5rem;">
            <div style="font-size: 2rem; margin-bottom: 0.5rem;">🔍</div>
            <p class="font-semibold text-main mb-1">Búsqueda rápida de clientes</p>
            <p class="text-sm text-muted" style="max-width: 420px; margin: 0 auto;">
              Escriba en la caja de búsqueda superior el <strong>DNI, Nombre o Código</strong> del cliente para consultar al instante sus existencias de bulbos en custodia.
            </p>
          </td>
        </tr>`;
    }
    if (totalCell) {
      totalCell.textContent = '0';
    }
  }

  async _loadStock() {
    const tbody = this.container.querySelector('#stock-table-body');
    const totalCell = this.container.querySelector('#stock-total-cell');
    if (!tbody || !totalCell) return;

    if (!this.selectedClienteId) {
      this._showPromptSelectCliente();
      return;
    }

    tbody.innerHTML = `<tr><td colspan="3" class="text-center text-muted" style="padding: 1.5rem;">Consultando existencias de stock...</td></tr>`;

    try {
      const rawResult = await stockService.obtenerStockPorCliente(this.selectedClienteId);

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
