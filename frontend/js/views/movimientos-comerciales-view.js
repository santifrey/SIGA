/**
 * SIGA - views/movimientos-comerciales-view.js
 * Vista Unificada de Movimientos Comerciales (#movimientos-comerciales) (Feature 34)
 * Coordinador principal: KPIs, Filtros, Tabla de Movimientos y Submódulos de Modales.
 */

import { authService } from '../services/auth-service.js';
import { temporadasService } from '../services/temporadas-service.js';
import { clientesService } from '../services/clientes-service.js';
import {
  movimientosComercialesService,
  TIPOS_MOVIMIENTO,
  _parseEstadoIndex
} from '../services/movimientos-comerciales-service.js';
import { ClienteModal } from '../components/cliente-modal.js';
import { loadTemplate } from '../utils/template-loader.js';
import { toast } from '../utils/toast.js';
import { escapeHtml } from '../utils/escape-html.js';
import {
  resolverNombreParticipante,
  resolverEstadoVisual,
  renderRowActionButtons
} from './movimientos-comerciales/movimientos-helpers.js';
import { MovimientoFormModal } from './movimientos-comerciales/movimiento-form-modal.js';
import { MovimientoDetalleModal } from './movimientos-comerciales/movimiento-detalle-modal.js';
import { MovimientoAccionesModal } from './movimientos-comerciales/movimiento-acciones-modal.js';

export class MovimientosComercialesView {
  constructor(router) {
    this.router = router;
    this.container = null;
    this.movimientos = [];
    this.temporadas = [];
    this.clientes = [];
    this.activeTemporadaId = null;
    this.activeMovimiento = null;

    this.filtro = {
      tipo: 'todos',
      estado: 'todos',
      clienteId: 'todos',
      temporadaId: 'todos',
      fechaDesde: '',
      fechaHasta: '',
      search: ''
    };

    // Submódulos de modales
    this.clienteModal = null;
    this.formModal = null;
    this.detalleModal = null;
    this.accionesModal = null;
  }

  async render(rootElement) {
    this.container = rootElement;
    this.container.innerHTML = await loadTemplate('movimientos-comerciales.html');

    // Control de permiso para botón "Nuevo Movimiento Comercial"
    const btnNuevo = this.container.querySelector('#btn-nuevo-movimiento');
    if (btnNuevo && !authService.tienePermiso('MOVCOM_CREAR')) {
      btnNuevo.remove();
    }

    // Inicializar componente reutilizable de alta rápida de cliente
    this.clienteModal = new ClienteModal({
      container: this.container,
      onSaved: async (nuevoCliente) => {
        await this._recargarClientesEnSelectores(nuevoCliente?.id);
      }
    });
    await this.clienteModal.init();

    // Inicializar submódulos de modales del circuito comercial
    this.formModal = new MovimientoFormModal({
      container: this.container,
      getTemporadas: () => this.temporadas,
      getClientes: () => this.clientes,
      getActiveTemporadaId: () => this.activeTemporadaId,
      onOpenNuevoCliente: (defaultSucursalId) => this.clienteModal.openNuevo(defaultSucursalId),
      onSaved: async () => await this._loadMovimientos()
    });

    this.accionesModal = new MovimientoAccionesModal({
      container: this.container,
      getMovimientos: () => this.movimientos,
      getClientes: () => this.clientes,
      getActiveMovimiento: () => this.activeMovimiento,
      setActiveMovimiento: (mov) => { this.activeMovimiento = mov; },
      onCompleted: async () => await this._loadMovimientos()
    });

    this.detalleModal = new MovimientoDetalleModal({
      container: this.container,
      getClientes: () => this.clientes,
      onSetActiveMovimiento: (mov) => { this.activeMovimiento = mov; },
      onAbrirAccion: (tipo, movId) => this.accionesModal.open(tipo, movId),
      onDescargarContrato: (movId) => this.accionesModal.descargarOVerContrato(movId),
      onCorregirMovimiento: async (movId) => await this.formModal.open(movId)
    });

    this._bindBaseEvents();
    this.formModal.bindEvents();
    this.detalleModal.bindEvents();
    this.accionesModal.bindEvents();

    await this._loadInitialData();
    await this._loadMovimientos();
  }

  _bindBaseEvents() {
    this.container.querySelector('#btn-refresh-movimientos')?.addEventListener('click', async () => {
      await this._loadMovimientos();
    });

    this.container.querySelector('#btn-nuevo-movimiento')?.addEventListener('click', () => {
      this.formModal.open(null);
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

      const selectClienteFiltro = this.container.querySelector('#filtro-cliente');
      if (selectClienteFiltro) {
        this.clientes.forEach(c => {
          const opt = document.createElement('option');
          opt.value = String(c.id);
          opt.textContent = `👤 ${c.nombre} ${c.apellido} (${c.codigo || 'CLI-' + c.id})`;
          selectClienteFiltro.appendChild(opt);
        });
      }

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

  async _recargarClientesEnSelectores(selectedId = null) {
    try {
      const clientesData = await clientesService.listar({ page: 1, pageSize: 200, estado: 'activos' }).catch(() => ({ items: [] }));
      this.clientes = Array.isArray(clientesData) ? clientesData : (clientesData?.items || []);
    } catch (err) {
      console.warn('[MovimientosComercialesView] Error al recargar clientes:', err);
    }

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

    await this.formModal.updateClienteSelectorAfterCreation(selectedId);
  }

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

  _renderTable() {
    const tbody = this.container.querySelector('#tbody-movimientos');
    if (!tbody) return;

    let list = [...this.movimientos];

    if (this.filtro.search && this.filtro.search.trim() !== '') {
      const q = this.filtro.search.trim().toLowerCase();
      list = list.filter(m => {
        const origenLabel = resolverNombreParticipante(m.origenParticipanteId, m.nombreOrigen, m, this.clientes);
        const destinoLabel = resolverNombreParticipante(m.destinoParticipanteId, m.nombreDestino, m, this.clientes);
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
      const {
        tipoNum,
        isFacturaAnulada,
        label: friendlyEstadoLabel,
        badgeClass: friendlyBadgeClass
      } = resolverEstadoVisual(m, false);

      const tipoMeta = TIPOS_MOVIMIENTO[tipoNum] || TIPOS_MOVIMIENTO[m.tipo] || {
        label: `Tipo ${m.tipo}`,
        badgeClass: 'badge-secondary'
      };
      const fReg = m.fechaRegistro ? new Date(m.fechaRegistro).toLocaleDateString('es-AR') : '-';

      const totalBulbos = (Number(m.totalBulbos) || 0).toLocaleString('es-AR');
      const montoTotal = Number(m.montoTotal) || 0;
      const montoFmt = tipoNum === 3
        ? '<span class="text-muted">$ 0,00</span>'
        : `$ ${montoTotal.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;

      const contratoBadge = m.contrato
        ? `<span class="badge badge-success text-xs cursor-pointer btn-badge-contrato" data-id="${m.id}" style="cursor: pointer;" title="Descargar Contrato #${escapeHtml(m.contrato.numeroContrato)}">Contrato: #${escapeHtml(m.contrato.numeroContrato)}</span>`
        : (tipoNum === 1 && m.requiereContrato ? `<span class="badge badge-warning text-xs" title="Requiere contrato previo">Req. Contrato</span>` : '<span class="text-muted text-xs">-</span>');

      const facturaBadge = !m.factura
        ? '<span class="text-muted text-xs">-</span>'
        : (isFacturaAnulada
          ? `<span class="badge badge-danger text-xs" title="Factura #${escapeHtml(m.factura.numeroFactura)} (Anulada)">Factura Anulada: #${escapeHtml(m.factura.numeroFactura)}</span>`
          : `<span class="badge badge-primary text-xs" title="Factura #${escapeHtml(m.factura.numeroFactura)}">Factura: #${escapeHtml(m.factura.numeroFactura)}</span>`);

      const origenLabel = resolverNombreParticipante(m.origenParticipanteId, m.nombreOrigen, m, this.clientes);
      const destinoLabel = resolverNombreParticipante(m.destinoParticipanteId, m.nombreDestino, m, this.clientes);

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
              ${renderRowActionButtons(m)}
            </div>
          </td>
        </tr>
      `;
    }).join('');

    this._bindTableActions();
  }

  _bindTableActions() {
    const tbody = this.container.querySelector('#tbody-movimientos');
    if (!tbody) return;

    tbody.querySelectorAll('.btn-ver-detalle').forEach(b => {
      b.addEventListener('click', () => {
        const id = Number(b.getAttribute('data-id'));
        this.detalleModal.open(id);
      });
    });

    tbody.querySelectorAll('.btn-action-contrato').forEach(b => {
      b.addEventListener('click', () => {
        const id = Number(b.getAttribute('data-id'));
        this.accionesModal.open('contrato', id);
      });
    });

    tbody.querySelectorAll('.btn-action-ver-contrato, .btn-badge-contrato').forEach(b => {
      b.addEventListener('click', (e) => {
        e.stopPropagation();
        const id = Number(b.getAttribute('data-id'));
        this.accionesModal.descargarOVerContrato(id);
      });
    });

    tbody.querySelectorAll('.btn-action-aprobar').forEach(b => {
      b.addEventListener('click', () => {
        const id = Number(b.getAttribute('data-id'));
        this.accionesModal.open('aprobar', id);
      });
    });

    tbody.querySelectorAll('.btn-action-facturar').forEach(b => {
      b.addEventListener('click', () => {
        const id = Number(b.getAttribute('data-id'));
        this.accionesModal.open('facturar', id);
      });
    });

    tbody.querySelectorAll('.btn-action-finalizar').forEach(b => {
      b.addEventListener('click', () => {
        const id = Number(b.getAttribute('data-id'));
        this.accionesModal.open('finalizar', id);
      });
    });

    tbody.querySelectorAll('.btn-action-rechazar').forEach(b => {
      b.addEventListener('click', () => {
        const id = Number(b.getAttribute('data-id'));
        this.accionesModal.open('rechazar', id);
      });
    });

    tbody.querySelectorAll('.btn-action-corregir').forEach(b => {
      b.addEventListener('click', async () => {
        const id = Number(b.getAttribute('data-id'));
        await this.formModal.open(id);
      });
    });
  }
}
