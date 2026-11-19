/**
 * SIGA - views/ajustes-stock-view.js
 * Vista Funcional de Ajustes de Stock de Bulbos
 * Permite registrar cuando se quitan o añaden bulbos a un cliente o a la empresa
 * con motivo obligatorio registrado en la observación del ajuste para auditoría.
 */

import { movimientosService } from '../services/movimientos-service.js';
import { clientesService } from '../services/clientes-service.js';
import { sucursalesService } from '../services/sucursales-service.js';
import { BulbosCalibresGrid } from '../components/bulbos-calibres-grid.js';
import { ClienteAutocomplete } from '../components/cliente-autocomplete.js';
import { loadTemplate } from '../utils/template-loader.js';
import { toast } from '../utils/toast.js';
import { escapeHtml } from '../utils/escape-html.js';

export class AjustesStockView {
  constructor(router) {
    this.router = router;
    this.container = null;
    this.clientesList = [];
    this.sucursalesList = [];
    this.filterBusqueda = '';
    this.filterEstado = 'todos';
    this.filterEntidad = 'todos';
    this.ajustesCache = [];
    this.calibresGrid = null;
    this.clienteAutocomplete = null;
  }

  async render(rootElement) {
    this.container = rootElement;
    this.container.innerHTML = await loadTemplate('ajustes-stock.html');

    this.calibresGrid = new BulbosCalibresGrid({
      root: this.container,
      inputIdPrefix: 'ajuste-cant-',
      totalSelector: '#ajuste-total-preview',
      gridContainerSelector: '#ajustes-calibres-grid'
    });
    this.calibresGrid.init();

    // Inicializar componente Autocomplete Asíncrono para selección de clientes
    const acEl = this.container.querySelector('#ajuste-cliente-autocomplete');
    if (acEl) {
      this.clienteAutocomplete = new ClienteAutocomplete({
        container: acEl,
        name: 'ajuste-cliente-id',
        placeholder: 'Buscar cliente por DNI, Nombre o Código...',
        required: false
      });
    }

    this._bindEvents();
    await this._loadSucursales();
    await this._loadAjustes();
  }

  _bindEvents() {
    this.container.querySelector('#btn-refresh-ajustes').addEventListener('click', () => {
      this._loadAjustes();
    });

    const inputBusqueda = this.container.querySelector('#filter-ajuste-busqueda');
    if (inputBusqueda) {
      inputBusqueda.addEventListener('input', (e) => {
        this.filterBusqueda = e.target.value.toLowerCase().trim();
        this._renderTable();
      });
    }

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

    const radioSucursal = this.container.querySelector('#radio-entidad-sucursal');
    const radioCliente = this.container.querySelector('#radio-entidad-cliente');
    const boxSucursalSelect = this.container.querySelector('#box-sucursal-select');
    const boxClienteSelect = this.container.querySelector('#box-cliente-select');

    if (radioSucursal) {
      radioSucursal.addEventListener('change', () => {
        if (radioSucursal.checked) {
          boxSucursalSelect?.classList.remove('hidden');
          boxClienteSelect?.classList.add('hidden');
        }
      });
    }
    if (radioCliente) {
      radioCliente.addEventListener('change', () => {
        if (radioCliente.checked) {
          boxSucursalSelect?.classList.add('hidden');
          boxClienteSelect?.classList.remove('hidden');
        }
      });
    }

    this.container.querySelector('#btn-save-pendiente').addEventListener('click', () => {
      this._handleAjusteSubmit(false);
    });

    this.container.querySelector('#form-nuevo-ajuste').addEventListener('submit', (e) => {
      e.preventDefault();
      this._handleAjusteSubmit(true);
    });
  }

  async _loadSucursales() {
    try {
      this.sucursalesList = await sucursalesService.listar({ estado: 'activas' });
      const selectSucursal = this.container.querySelector('#ajuste-sucursal-id');
      if (selectSucursal) {
        selectSucursal.innerHTML = '<option value="">-- Elija una sucursal --</option>';
        this.sucursalesList.forEach(s => {
          const partId = s.participanteComercialId || s.id;
          const text = `🏢 ${s.nombre} (${s.codigo || 'SUC-' + s.id})`;
          selectSucursal.add(new Option(text, String(partId)));
        });

        if (this.sucursalesList.length > 0) {
          selectSucursal.value = String(this.sucursalesList[0].participanteComercialId || this.sucursalesList[0].id);
        }
      }
    } catch (err) {
      console.warn('[AjustesStockView] No se pudo cargar sucursales:', err);
    }
  }

  async _loadClientes() {
    try {
      const data = await clientesService.listar({ page: 1, pageSize: 500, estado: 'todos' });
      this.clientesList = data.items || [];

      const selectCliente = this.container.querySelector('#ajuste-cliente-id');
      selectCliente.innerHTML = '<option value="">-- Elija un cliente --</option>';

      this.clientesList
        .filter(c => c.activo !== false && String(c.estadoCliente || 'Activo').toLowerCase() !== 'inactivo')
        .forEach(c => {
          const nombreCompleto = `${c.nombre || ''} ${c.apellido || ''}`.trim() || c.nombreCompleto || `Cliente #${c.id}`;
          const docInfo = c.documento ? ` (DNI ${c.documento})` : '';
          const text = `👤 ${nombreCompleto}${docInfo}`;
          selectCliente.add(new Option(text, String(c.id)));
        });
    } catch (err) {
      console.warn('[AjustesStockView] No se pudo cargar clientes:', err);
    }
  }

  _resolverNombreCliente(participanteId, rawNombre) {
    const idNum = Number(participanteId);
    const raw = String(rawNombre || '').trim();

    if (idNum > 1 && this.clientesList.length > 0) {
      const foundById = this.clientesList.find(c => Number(c.id) === idNum);
      if (foundById) {
        const full = `${foundById.nombre || ''} ${foundById.apellido || ''}`.trim() || foundById.nombreCompleto;
        if (full) return full;
      }
    }

    if (raw && this.clientesList.length > 0) {
      const foundByCode = this.clientesList.find(c => {
        const cod = String(c.codigoCliente || c.codigo || '').toLowerCase();
        return cod && raw.toLowerCase().includes(cod);
      });
      if (foundByCode) {
        const full = `${foundByCode.nombre || ''} ${foundByCode.apellido || ''}`.trim() || foundByCode.nombreCompleto;
        if (full) return full;
      }
    }

    // Si el backend ya envió "Nombre Apellido (CLI-...)" quitar el código entre paréntesis
    const sinCodigoParentesis = raw.replace(/\s*\(CLI-[^)]+\)/i, '').trim();
    if (sinCodigoParentesis && !/^cliente\s+cli-/i.test(sinCodigoParentesis)) {
      return sinCodigoParentesis;
    }

    return raw || `Cliente #${participanteId}`;
  }

  _obtenerInfoAjuste(m) {
    const obs = String(m.observaciones || '');
    const origId = Number(m.origenId);
    const destId = Number(m.destinoId);
    const origRaw = String(m.origenNombre || '').trim();
    const destRaw = String(m.destinoNombre || '').trim();

    const esEgreso = destRaw.toLowerCase().includes('ajuste') || destId === 0 || obs.includes('[Egreso');
    const esIngreso = !esEgreso;
    const titularId = esIngreso ? destId : origId;
    const titularRaw = esIngreso ? destRaw : origRaw;

    // Detectar si el titular es una sucursal o empresa
    const suc = this.sucursalesList.find(s =>
      Number(s.participanteComercialId) === titularId || Number(s.id) === titularId
    );
    const matchSucTag = obs.match(/\[(?:Ingreso a|Egreso de)\s+Sucursal:\s*([^\]]+)\]/i);
    const esSucursal = Boolean(suc) || titularId === 1 || Boolean(matchSucTag) || obs.includes('[Ingreso a Empresa]') || obs.includes('[Egreso de Empresa]');

    let titularTexto = '';
    if (suc) {
      titularTexto = `🏢 ${suc.nombre}`;
    } else if (matchSucTag && matchSucTag[1]) {
      titularTexto = `🏢 ${matchSucTag[1].trim()}`;
    } else if (titularId === 1) {
      titularTexto = '🏢 Sede Central (Depósito Central)';
    } else if (esSucursal) {
      titularTexto = `🏢 ${titularRaw || `Sucursal #${titularId}`}`;
    } else {
      titularTexto = `👤 ${this._resolverNombreCliente(titularId, titularRaw)}`;
    }

    let motivoLimpio = obs
      .replace(/\[Ingreso a Sucursal:[^\]]+\]\s*/i, '')
      .replace(/\[Egreso de Sucursal:[^\]]+\]\s*/i, '')
      .replace(/\[Ingreso a Empresa\]\s*/i, '')
      .replace(/\[Egreso de Empresa\]\s*/i, '')
      .replace(/\[Ingreso a Cliente\]\s*/i, '')
      .replace(/\[Egreso de Cliente\]\s*/i, '')
      .replace(/\[Ajuste de Stock Empresa \([+-]\)\]\s*/i, '')
      .trim();

    if (!motivoLimpio) motivoLimpio = 'Ajuste general de stock';

    return { esIngreso, esSucursal, esEmpresa: esSucursal, titularTexto, motivoLimpio };
  }

  async _loadAjustes() {
    const tbody = this.container.querySelector('#ajustes-table-body');
    tbody.innerHTML = `<tr><td colspan="8" class="text-center text-muted" style="padding: 2rem;">Cargando ajustes de stock...</td></tr>`;

    try {
      const result = await movimientosService.listar({ estado: 'todos' });
      // Un movimiento es un Ajuste de Stock si no tiene FK a MovimientoComercial, Cosecha ni Plantación
      this.ajustesCache = (result.items || []).filter(
        m => !m.movimientoComercialId && !m.cosechaId && !m.plantacionId
      );
      this._renderTable();
    } catch (err) {
      tbody.innerHTML = `<tr><td colspan="8" class="text-center text-danger" style="padding: 2rem;">${escapeHtml(err.message || 'Error al cargar ajustes')}</td></tr>`;
      toast.error('No se pudo cargar el listado de ajustes de stock.');
    }
  }

  _renderTable() {
    const tbody = this.container.querySelector('#ajustes-table-body');
    let items = [...this.ajustesCache];

    // Filtro por búsqueda de texto (nombre y apellido de cliente, comprobante o motivo)
    if (this.filterBusqueda) {
      items = items.filter(m => {
        const info = this._obtenerInfoAjuste(m);
        const compStr = String(m.numeroComprobante || `AJU-${m.id}`).toLowerCase();
        const titularStr = info.titularTexto.toLowerCase();
        const motivoStr = info.motivoLimpio.toLowerCase();
        const obsStr = String(m.observaciones || '').toLowerCase();
        return compStr.includes(this.filterBusqueda) ||
          titularStr.includes(this.filterBusqueda) ||
          motivoStr.includes(this.filterBusqueda) ||
          obsStr.includes(this.filterBusqueda);
      });
    }

    // Filtro por estado
    if (this.filterEstado !== 'todos') {
      items = items.filter(m => String(m.estado).toLowerCase() === this.filterEstado.toLowerCase());
    }

    // Filtro por entidad
    if (this.filterEntidad === 'sucursales' || this.filterEntidad === 'empresa') {
      items = items.filter(m => this._obtenerInfoAjuste(m).esSucursal);
    } else if (this.filterEntidad === 'clientes') {
      items = items.filter(m => !this._obtenerInfoAjuste(m).esSucursal);
    }

    if (items.length === 0) {
      tbody.innerHTML = `<tr><td colspan="8" class="text-center text-muted" style="padding: 2.5rem;">No hay ajustes de stock que coincidan con los filtros seleccionados.</td></tr>`;
      return;
    }

    tbody.innerHTML = items.map(m => {
      const fechaStr = m.fecha ? new Date(m.fecha).toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' }) : '-';
      const obs = m.observaciones || '';
      const { esIngreso, titularTexto, motivoLimpio } = this._obtenerInfoAjuste(m);

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
          <td class="font-medium text-main text-sm">${escapeHtml(titularTexto)}</td>
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
    const { esIngreso, titularTexto, motivoLimpio } = this._obtenerInfoAjuste(mov);
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
        <label class="text-xs text-muted font-bold text-uppercase">Titular Afectado y Sentido:</label>
        <div class="font-medium text-main d-flex align-center gap-sm mt-1">
          <span>${escapeHtml(titularTexto)}</span>
          <span class="badge ${esIngreso ? 'badge-success' : 'badge-danger'}">${esIngreso ? '+ Ingreso de Stock' : '- Egreso de Stock'}</span>
        </div>
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
          ${escapeHtml(motivoLimpio || mov.observaciones || 'Sin observación asentada')}
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
    const radioSucursal = this.container.querySelector('#radio-entidad-sucursal');
    if (radioSucursal) radioSucursal.checked = true;
    this.container.querySelector('#box-sucursal-select')?.classList.remove('hidden');
    this.container.querySelector('#box-cliente-select')?.classList.add('hidden');
    this.container.querySelector('#ajuste-form-error').classList.add('hidden');
    this.container.querySelector('#ajuste-form-error').textContent = '';
    this.calibresGrid.reset();
    this.clienteAutocomplete?.clear();

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

    const entidadTipo = this.container.querySelector('input[name="ajuste-entidad-tipo"]:checked')?.value || 'sucursal';
    const accion = this.container.querySelector('input[name="ajuste-accion"]:checked')?.value || 'sumar';
    const sucursalId = this.container.querySelector('#ajuste-sucursal-id')?.value;
    const clienteId = this.clienteAutocomplete?.getValue();
    const motivo = this.container.querySelector('#ajuste-motivo')?.value.trim();

    if (entidadTipo === 'sucursal' && !sucursalId) {
      errBox.textContent = 'Debe seleccionar la sucursal para realizar el ajuste de stock.';
      errBox.classList.remove('hidden');
      return;
    }

    if (entidadTipo === 'cliente' && !clienteId) {
      errBox.textContent = 'Debe buscar y seleccionar el cliente para realizar el ajuste de stock.';
      errBox.classList.remove('hidden');
      this.clienteAutocomplete?.markInvalid();
      return;
    }

    if (!motivo) {
      errBox.textContent = 'Debe ingresar el motivo / observación obligatoria del ajuste.';
      errBox.classList.remove('hidden');
      return;
    }

    const cantidades = this.calibresGrid.getItemsArray(false);
    const total = this.calibresGrid.getTotal();

    if (total <= 0) {
      errBox.textContent = 'Debe ingresar una cantidad mayor a cero en al menos una clase de bulbo.';
      errBox.classList.remove('hidden');
      return;
    }

    const btnSubmit = this.container.querySelector(autoConfirmar ? '#btn-save-confirmar' : '#btn-save-pendiente');
    btnSubmit.disabled = true;

    try {
      const suc = this.sucursalesList.find(s => String(s.participanteComercialId || s.id) === String(sucursalId));
      await movimientosService.registrarAjuste({
        entidad: entidadTipo,
        sucursalId: entidadTipo === 'sucursal' ? sucursalId : null,
        sucursalNombre: suc ? suc.nombre : null,
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

