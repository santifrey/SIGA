/**
 * SIGA - views/historial-movimientos-view.js
 * Vista de Movimiento de Bulbos
 * Vista exclusiva para consultar el historial completo y trazabilidad física
 * de todos los movimientos de bulbos (Ventas, Compras, Traspasos, Cosechas, Plantaciones y Ajustes).
 */

import { movimientosService } from '../services/movimientos-service.js';
import { clientesService } from '../services/clientes-service.js';
import { sucursalesService } from '../services/sucursales-service.js';
import { loadTemplate } from '../utils/template-loader.js';
import { toast } from '../utils/toast.js';
import { escapeHtml } from '../utils/escape-html.js';

export class HistorialMovimientosView {
  constructor(router) {
    this.router = router;
    this.container = null;
    this.clientesList = [];
    this.sucursalesList = [];
    this.filterBusqueda = '';
    this.filterTipo = 'todos';
    this.filterEstado = 'todos';
    this.filterEntidad = 'todos';
    this.movimientosCache = [];
  }

  async render(rootElement) {
    this.container = rootElement;
    this.container.innerHTML = await loadTemplate('historial-movimientos.html');
    this._bindEvents();
    await this._loadHistorial();
  }

  _bindEvents() {
    this.container.querySelector('#btn-refresh-historial').addEventListener('click', () => {
      this._loadHistorial();
    });

    this.container.querySelector('#filter-hist-busqueda').addEventListener('input', (e) => {
      this.filterBusqueda = e.target.value.toLowerCase().trim();
      this._renderTable();
    });

    this.container.querySelector('#filter-hist-tipo').addEventListener('change', (e) => {
      this.filterTipo = e.target.value;
      this._renderTable();
    });

    this.container.querySelector('#filter-hist-estado').addEventListener('change', (e) => {
      this.filterEstado = e.target.value;
      this._renderTable();
    });

    this.container.querySelector('#filter-hist-entidad').addEventListener('change', (e) => {
      this.filterEntidad = e.target.value;
      this._renderTable();
    });

    this.container.querySelector('#btn-close-det-mov').addEventListener('click', () => {
      this.container.querySelector('#modal-detalle-mov').classList.remove('is-active');
    });

    this.container.querySelector('#btn-cerrar-det-mov').addEventListener('click', () => {
      this.container.querySelector('#modal-detalle-mov').classList.remove('is-active');
    });
  }

  async _loadHistorial() {
    const tbody = this.container.querySelector('#historial-table-body');
    tbody.innerHTML = `<tr><td colspan="9" class="text-center text-muted" style="padding: 2.5rem;">Cargando movimientos de bulbos...</td></tr>`;

    try {
      const [movResult, cliResult, sucResult] = await Promise.all([
        movimientosService.listar({ pageSize: 500, estado: 'todos' }),
        clientesService.listar({ page: 1, pageSize: 500, estado: 'todos' }).catch(() => ({ items: [] })),
        sucursalesService.listar({ estado: 'todas' }).catch(() => [])
      ]);
      this.clientesList = cliResult.items || [];
      this.sucursalesList = Array.isArray(sucResult) ? sucResult : (sucResult?.items || []);
      this.movimientosCache = movResult.items || [];
      this._populateTitularFilter();
      this._updateKpis();
      this._renderTable();
    } catch (err) {
      tbody.innerHTML = `<tr><td colspan="9" class="text-center text-danger" style="padding: 2.5rem;">${escapeHtml(err.message || 'Error al cargar movimientos de bulbos')}</td></tr>`;
      toast.error('No se pudo cargar el listado de movimientos de bulbos.');
    }
  }

  _populateTitularFilter() {
    const select = this.container.querySelector('#filter-hist-entidad');
    if (!select) return;

    const currentValue = this.filterEntidad;
    select.innerHTML = '';

    // 1. Opciones Generales
    select.add(new Option('Todos los Titulares', 'todos'));
    select.add(new Option('🏢 Todas las Sucursales', 'todas_sucursales'));
    select.add(new Option('👤 Todos los Clientes', 'todos_clientes'));

    // 2. Optgroup Sucursales
    if (this.sucursalesList && this.sucursalesList.length > 0) {
      const groupSuc = document.createElement('optgroup');
      groupSuc.label = '🏢 Por Sucursal';
      this.sucursalesList.forEach(s => {
        const partId = s.participanteComercialId || s.id;
        const text = `🏢 ${s.nombre} (${s.codigo || 'SUC-' + s.id})`;
        groupSuc.appendChild(new Option(text, `sucursal:${partId}`));
      });
      select.appendChild(groupSuc);
    } else {
      const groupSuc = document.createElement('optgroup');
      groupSuc.label = '🏢 Por Sucursal';
      groupSuc.appendChild(new Option('🏢 Sede Central (Depósito Central)', 'sucursal:1'));
      select.appendChild(groupSuc);
    }

    // 3. Optgroup Clientes
    if (this.clientesList && this.clientesList.length > 0) {
      const groupCli = document.createElement('optgroup');
      groupCli.label = '👤 Por Cliente';
      this.clientesList.forEach(c => {
        const nombreCompleto = `${c.nombre || ''} ${c.apellido || ''}`.trim() || c.nombreCompleto || `Cliente #${c.id}`;
        const docInfo = c.documento ? ` - DNI ${c.documento}` : '';
        const text = `👤 ${nombreCompleto}${docInfo} (${c.codigo || 'CLI-' + c.id})`;
        groupCli.appendChild(new Option(text, `cliente:${c.id}`));
      });
      select.appendChild(groupCli);
    }

    if (currentValue && Array.from(select.options).some(o => o.value === currentValue)) {
      select.value = currentValue;
    } else {
      select.value = 'todos';
      this.filterEntidad = 'todos';
    }
  }

  _resolverNombreParticipante(participanteId, rawNombre) {
    const idNum = Number(participanteId);
    if (idNum === 1) return '🏢 Sede Central (Depósito Central)';

    if (this.sucursalesList.length > 0) {
      const foundSuc = this.sucursalesList.find(s => Number(s.participanteComercialId) === idNum || Number(s.id) === idNum);
      if (foundSuc) {
        return `🏢 ${foundSuc.nombre}`;
      }
    }

    return this._resolverNombreCliente(participanteId, rawNombre);
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

    const sinCodigoParentesis = raw.replace(/\s*\(CLI-[^)]+\)/i, '').trim();
    if (sinCodigoParentesis && !/^cliente\s+cli-/i.test(sinCodigoParentesis)) {
      return sinCodigoParentesis;
    }

    return raw || `Cliente #${participanteId}`;
  }

  _updateKpis() {
    const totalMovs = this.movimientosCache.length;
    const totalBulbos = this.movimientosCache.reduce((sum, m) => sum + (Number(m.totalBulbos) || 0), 0);
    const confirmados = this.movimientosCache.filter(m => String(m.estado).toLowerCase() === 'confirmado').length;
    const pendientes = this.movimientosCache.filter(m => String(m.estado).toLowerCase() === 'pendiente').length;

    const elTotal = this.container.querySelector('#kpi-hist-total');
    const elBulbos = this.container.querySelector('#kpi-hist-bulbos');
    const elConf = this.container.querySelector('#kpi-hist-confirmados');
    const elPend = this.container.querySelector('#kpi-hist-pendientes');

    if (elTotal) elTotal.textContent = totalMovs.toLocaleString('es-AR');
    if (elBulbos) elBulbos.textContent = totalBulbos.toLocaleString('es-AR');
    if (elConf) elConf.textContent = confirmados.toLocaleString('es-AR');
    if (elPend) elPend.textContent = pendientes.toLocaleString('es-AR');
  }

  _getTipoOperacionInfo(m) {
    const origId = Number(m.origenId);
    const destId = Number(m.destinoId);
    const origRaw = String(m.origenNombre || '').trim();
    const destRaw = String(m.destinoNombre || '').trim();

    // 1. FK a Plantación (m.plantacionId)
    if (m.plantacionId) {
      const esEmpresa = origId === 1 || this.sucursalesList.some(s => Number(s.participanteComercialId) === origId || Number(s.id) === origId);
      const titular = this._resolverNombreParticipante(origId, origRaw);
      return {
        categoria: 'Plantacion',
        nombre: 'Plantación',
        badgeClass: 'badge-success',
        icon: '🌱',
        origenText: '🌱 Campo / Fila Plantada',
        origenImpacto: null,
        destinoText: titular,
        destinoImpacto: { texto: '- Egreso a Campo', clase: 'badge-danger' },
        afectaEmpresa: esEmpresa,
        afectaCliente: !esEmpresa,
        refId: `PLT #${m.plantacionId}`
      };
    }

    // 2. FK a Cosecha (m.cosechaId)
    if (m.cosechaId) {
      const esEmpresa = destId === 1 || this.sucursalesList.some(s => Number(s.participanteComercialId) === destId || Number(s.id) === destId);
      const titular = this._resolverNombreParticipante(destId, destRaw);
      return {
        categoria: 'Cosecha',
        nombre: 'Cosecha',
        badgeClass: 'badge-warning',
        icon: '🌾',
        origenText: '🌾 Campo / Cosecha',
        origenImpacto: null,
        destinoText: titular,
        destinoImpacto: { texto: '+ Ingreso', clase: 'badge-success' },
        afectaEmpresa: esEmpresa,
        afectaCliente: !esEmpresa,
        refId: `COS #${m.cosechaId}`
      };
    }

    // 3. FK a Movimiento Comercial (m.movimientoComercialId)
    if (m.movimientoComercialId) {
      const obs = String(m.observaciones || '');
      const tipoMc = String(m.tipoMovimientoComercialNombre || m.tipoMovimientoComercial || '').toLowerCase();
      const esTraspaso = tipoMc.includes('traspaso') || obs.includes('TraspasoEntreClientes') || obs.toLowerCase().includes('traspaso');
      const esCompra = tipoMc.includes('compra') || obs.includes('CompraACliente') || obs.toLowerCase().includes('compra a cliente');
      const esVenta = tipoMc.includes('venta') || obs.includes('VentaACliente') || obs.toLowerCase().includes('venta a cliente');

      const origenText = this._resolverNombreParticipante(origId, origRaw);
      const destinoText = this._resolverNombreParticipante(destId, destRaw);

      const origenIsSucursal = origId === 1 || this.sucursalesList.some(s => Number(s.participanteComercialId) === origId || Number(s.id) === origId);
      const destinoIsSucursal = destId === 1 || this.sucursalesList.some(s => Number(s.participanteComercialId) === destId || Number(s.id) === destId);
      const entreSucursales = origenIsSucursal && destinoIsSucursal;

      if (esTraspaso || (!esCompra && !esVenta && ((origId > 1 && destId > 1) || (origenIsSucursal && destinoIsSucursal)))) {
        return {
          categoria: 'Traspaso',
          nombre: entreSucursales ? 'Traspaso entre Sucursales' : 'Traspaso entre Clientes',
          badgeClass: 'badge-accent',
          icon: '🔄',
          origenText,
          origenImpacto: { texto: '- Egreso', clase: 'badge-danger' },
          destinoText,
          destinoImpacto: { texto: '+ Ingreso', clase: 'badge-success' },
          afectaEmpresa: origenIsSucursal || destinoIsSucursal,
          afectaCliente: !origenIsSucursal || !destinoIsSucursal,
          refId: `OP #${m.movimientoComercialId}`
        };
      }

      if (esCompra || (!esVenta && !origenIsSucursal && destinoIsSucursal)) {
        return {
          categoria: 'Compra',
          nombre: 'Compra a Cliente',
          badgeClass: 'badge-info',
          icon: '📥',
          origenText,
          origenImpacto: { texto: '- Egreso', clase: 'badge-danger' },
          destinoText,
          destinoImpacto: { texto: '+ Ingreso', clase: 'badge-success' },
          afectaEmpresa: true,
          afectaCliente: true,
          refId: `OP #${m.movimientoComercialId}`
        };
      }

      // Venta a Cliente
      return {
        categoria: 'Venta',
        nombre: 'Venta a Cliente',
        badgeClass: 'badge-primary',
        icon: '🛒',
        origenText,
        origenImpacto: { texto: '- Egreso', clase: 'badge-danger' },
        destinoText,
        destinoImpacto: { texto: '+ Ingreso', clase: 'badge-success' },
        afectaEmpresa: true,
        afectaCliente: true,
        refId: `OP #${m.movimientoComercialId}`
      };
    }

    // 4. Sin ninguna FK -> Ajuste de Stock
    const obs = String(m.observaciones || '');
    const esEgreso = destRaw.toLowerCase().includes('ajuste') || destId === 0 || obs.includes('[Egreso');
    const esIngreso = !esEgreso;
    const titularId = esIngreso ? destId : origId;
    const titularRaw = esIngreso ? destRaw : origRaw;
    const titular = this._resolverNombreParticipante(titularId, titularRaw);
    const esEmpresa = titularId === 1 || this.sucursalesList.some(s => Number(s.participanteComercialId) === titularId || Number(s.id) === titularId);

    return {
      categoria: 'AjusteStock',
      nombre: esIngreso ? 'Ajuste Stock (+)' : 'Ajuste Stock (-)',
      badgeClass: esIngreso ? 'badge-success' : 'badge-danger',
      icon: esIngreso ? '➕' : '➖',
      origenText: '⚖️ Ajuste de Inventario',
      origenImpacto: null,
      destinoText: titular,
      destinoImpacto: {
        texto: esIngreso ? '+ Ingreso' : '- Egreso',
        clase: esIngreso ? 'badge-success' : 'badge-danger'
      },
      afectaEmpresa: esEmpresa,
      afectaCliente: !esEmpresa,
      refId: null
    };
  }

  _renderTable() {
    const tbody = this.container.querySelector('#historial-table-body');
    let items = [...this.movimientosCache];

    // 1. Filtro de búsqueda (comprobante, nombre y apellido del cliente, tipo u observaciones)
    if (this.filterBusqueda) {
      items = items.filter(m => {
        const opInfo = this._getTipoOperacionInfo(m);
        const idStr = String(m.id || '');
        const compStr = String(m.numeroComprobante || `MOV-${m.id}`).toLowerCase();
        const obsStr = String(m.observaciones || '').toLowerCase();
        const origStr = String(opInfo.origenText || '').toLowerCase();
        const destStr = String(opInfo.destinoText || '').toLowerCase();
        const tipoStr = String(opInfo.nombre || '').toLowerCase();
        return idStr.includes(this.filterBusqueda) ||
          compStr.includes(this.filterBusqueda) ||
          obsStr.includes(this.filterBusqueda) ||
          origStr.includes(this.filterBusqueda) ||
          destStr.includes(this.filterBusqueda) ||
          tipoStr.includes(this.filterBusqueda);
      });
    }

    // 2. Filtro por tipo
    if (this.filterTipo !== 'todos') {
      items = items.filter(m => {
        const info = this._getTipoOperacionInfo(m);
        return info.categoria.toLowerCase() === this.filterTipo.toLowerCase();
      });
    }

    // 3. Filtro por estado
    if (this.filterEstado !== 'todos') {
      items = items.filter(m => String(m.estado).toLowerCase() === this.filterEstado.toLowerCase());
    }

    // 4. Filtro por titular
    if (this.filterEntidad === 'todas_sucursales' || this.filterEntidad === 'empresa') {
      items = items.filter(m => this._getTipoOperacionInfo(m).afectaEmpresa);
    } else if (this.filterEntidad === 'todos_clientes' || this.filterEntidad === 'clientes') {
      items = items.filter(m => this._getTipoOperacionInfo(m).afectaCliente);
    } else if (this.filterEntidad.startsWith('sucursal:')) {
      const targetPartId = Number(this.filterEntidad.replace('sucursal:', ''));
      const foundSuc = this.sucursalesList.find(s => Number(s.participanteComercialId) === targetPartId || Number(s.id) === targetPartId);
      const possibleIds = new Set([targetPartId]);
      if (foundSuc) {
        if (foundSuc.id) possibleIds.add(Number(foundSuc.id));
        if (foundSuc.participanteComercialId) possibleIds.add(Number(foundSuc.participanteComercialId));
      }
      items = items.filter(m => possibleIds.has(Number(m.origenId)) || possibleIds.has(Number(m.destinoId)));
    } else if (this.filterEntidad.startsWith('cliente:')) {
      const targetCliId = Number(this.filterEntidad.replace('cliente:', ''));
      items = items.filter(m =>
        Number(m.origenId) === targetCliId ||
        Number(m.destinoId) === targetCliId ||
        Number(m.clienteId) === targetCliId
      );
    }

    if (items.length === 0) {
      tbody.innerHTML = `<tr><td colspan="9" class="text-center text-muted" style="padding: 2.5rem;">No se encontraron movimientos en el historial que coincidan con los filtros seleccionados.</td></tr>`;
      return;
    }

    tbody.innerHTML = items.map(m => {
      const fechaStr = m.fecha ? new Date(m.fecha).toLocaleDateString('es-AR', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
      }) : '-';

      const opInfo = this._getTipoOperacionInfo(m);

      let badgeEstado = 'badge-warning';
      if (m.estado === 'Confirmado') badgeEstado = 'badge-success';
      if (m.estado === 'Anulado') badgeEstado = 'badge-danger';

      const isPendiente = m.estado === 'Pendiente';

      let motivoLimpio = (m.observaciones || '')
        .replace(/\[Ingreso a Sucursal:[^\]]+\]\s*/i, '')
        .replace(/\[Egreso de Sucursal:[^\]]+\]\s*/i, '')
        .replace(/\[Ingreso a Empresa\]\s*/i, '')
        .replace(/\[Egreso de Empresa\]\s*/i, '')
        .replace(/\[Ingreso a Cliente\]\s*/i, '')
        .replace(/\[Egreso de Cliente\]\s*/i, '')
        .replace(/\[Ajuste de Stock Empresa \([+-]\)\]\s*/i, '')
        .replace(/\(VentaACliente\)/i, '(Venta a Cliente)')
        .replace(/\(CompraACliente\)/i, '(Compra a Cliente)')
        .replace(/\(TraspasoEntreClientes\)/i, '(Traspaso)')
        .trim();

      if (!motivoLimpio) motivoLimpio = opInfo.nombre;

      const origenImpactoHtml = opInfo.origenImpacto
        ? `<span class="badge ${opInfo.origenImpacto.clase}" style="font-size: 0.65rem; padding: 0.1rem 0.35rem; margin-left: 0.25rem;">${escapeHtml(opInfo.origenImpacto.texto)}</span>`
        : '';

      const destinoImpactoHtml = opInfo.destinoImpacto
        ? `<span class="badge ${opInfo.destinoImpacto.clase}" style="font-size: 0.65rem; padding: 0.1rem 0.35rem; margin-left: 0.25rem;">${escapeHtml(opInfo.destinoImpacto.texto)}</span>`
        : '';

      return `
        <tr>
          <td>
            <div class="d-flex flex-column">
              <strong class="text-primary text-xs">${escapeHtml(m.numeroComprobante || 'MOV-' + m.id)}</strong>
              ${opInfo.refId ? `<span class="text-muted" style="font-size: 0.675rem;">Ref: ${escapeHtml(opInfo.refId)}</span>` : ''}
            </div>
          </td>
          <td class="text-xs text-muted">${escapeHtml(fechaStr)}</td>
          <td>
            <span class="badge ${opInfo.badgeClass}" style="font-weight: 700; white-space: nowrap;">
              ${opInfo.icon} ${escapeHtml(opInfo.nombre)}
            </span>
          </td>
          <td class="text-xs text-main font-medium" style="max-width: 175px; word-break: break-word;">
            <div class="d-flex align-center flex-wrap gap-xs">
              <span>${escapeHtml(opInfo.origenText)}</span>
              ${origenImpactoHtml}
            </div>
          </td>
          <td class="text-xs text-main font-medium" style="max-width: 185px; word-break: break-word;">
            <div class="d-flex align-center flex-wrap gap-xs">
              <span>${escapeHtml(opInfo.destinoText)}</span>
              ${destinoImpactoHtml}
            </div>
          </td>
          <td style="text-align: right; font-weight: 800; color: var(--primary);">
            ${(m.totalBulbos || 0).toLocaleString('es-AR')}
          </td>
          <td>
            <span class="badge ${badgeEstado}">${escapeHtml(m.estado)}</span>
          </td>
          <td class="text-xs" style="max-width: 220px; word-break: break-word;">
            <span title="${escapeHtml(m.observaciones || '')}" style="color: var(--text-main); font-weight: 500;">
              ${escapeHtml(motivoLimpio)}
            </span>
          </td>
          <td>
            <div class="table-actions justify-end gap-xs">
              <button class="btn btn-xs btn-outline btn-ver-det-hist" data-id="${escapeHtml(m.id)}" title="Ver detalle completo de calibres y auditoría">
                Detalles
              </button>
              ${isPendiente ? `
                <button class="btn btn-xs btn-outline btn-confirmar-hist" data-id="${escapeHtml(m.id)}" style="color: var(--success); border-color: var(--success);" title="Confirmar movimiento">
                  ✓
                </button>
                <button class="btn btn-xs btn-outline btn-anular-hist" data-id="${escapeHtml(m.id)}" style="color: var(--danger); border-color: var(--danger);" title="Anular movimiento">
                  ✕
                </button>
              ` : ''}
            </div>
          </td>
        </tr>
      `;
    }).join('');

    // Eventos de acciones
    tbody.querySelectorAll('.btn-confirmar-hist').forEach(btn => {
      btn.addEventListener('click', async () => {
        const id = btn.getAttribute('data-id');
        if (confirm('¿Confirmar este movimiento de stock? Se impactará el saldo físico disponible.')) {
          try {
            await movimientosService.confirmar(id);
            toast.success('Movimiento confirmado e impactado con éxito.');
            await this._loadHistorial();
          } catch (err) {
            toast.error(err.message || 'Error al confirmar movimiento');
          }
        }
      });
    });

    tbody.querySelectorAll('.btn-anular-hist').forEach(btn => {
      btn.addEventListener('click', async () => {
        const id = btn.getAttribute('data-id');
        const motivo = prompt('Ingrese el motivo de anulación:') || 'Anulado por usuario';
        try {
          await movimientosService.anular(id, motivo);
          toast.info('Movimiento anulado.');
          await this._loadHistorial();
        } catch (err) {
          toast.error(err.message || 'Error al anular movimiento');
        }
      });
    });

    tbody.querySelectorAll('.btn-ver-det-hist').forEach(btn => {
      btn.addEventListener('click', () => {
        const id = btn.getAttribute('data-id');
        this._verDetalle(id);
      });
    });
  }

  _verDetalle(id) {
    const mov = this.movimientosCache.find(m => String(m.id) === String(id));
    if (!mov) return;

    const modal = this.container.querySelector('#modal-detalle-mov');
    const cuerpo = this.container.querySelector('#det-mov-cuerpo');
    const titulo = this.container.querySelector('#det-mov-titulo');
    const subtitulo = this.container.querySelector('#det-mov-subtitulo');
    const badgeTipo = this.container.querySelector('#det-mov-badge-tipo');
    const badgeEstado = this.container.querySelector('#det-mov-badge-estado');
    const accionesExtra = this.container.querySelector('#det-mov-acciones-extra');

    const opInfo = this._getTipoOperacionInfo(mov);
    titulo.textContent = `Movimiento ${mov.numeroComprobante || 'MOV-' + mov.id}`;
    subtitulo.textContent = opInfo.refId ? `Asociado a ${opInfo.nombre} (${opInfo.refId})` : `Operación: ${opInfo.nombre}`;

    badgeTipo.className = `badge ${opInfo.badgeClass}`;
    badgeTipo.innerHTML = `${opInfo.icon} ${escapeHtml(opInfo.nombre)}`;

    let badgeEstadoClass = 'badge-warning';
    if (mov.estado === 'Confirmado') badgeEstadoClass = 'badge-success';
    if (mov.estado === 'Anulado') badgeEstadoClass = 'badge-danger';

    badgeEstado.className = `badge ${badgeEstadoClass}`;
    badgeEstado.textContent = mov.estado;

    const fechaStr = mov.fecha ? new Date(mov.fecha).toLocaleString('es-AR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    }) : '-';

    const clasesNombres = [
      'Clase 0 (Extra Grande)',
      'Clase 1 (Grande)',
      'Clase 2 (Mediano)',
      'Clase 3 (Chico)',
      'Clase 4 (Semillero)'
    ];

    const totalBulbos = Number(mov.totalBulbos) || 0;
    const cantidadesMap = {};
    (mov.cantidades || []).forEach(c => {
      const idx = Number(c.claseBulbo ?? c.clase) || 0;
      cantidadesMap[idx] = (cantidadesMap[idx] || 0) + (Number(c.cantidad) || 0);
    });

    const cantidadesHtml = clasesNombres.map((nombre, idx) => {
      const cant = cantidadesMap[idx] || 0;
      const pct = totalBulbos > 0 ? ((cant / totalBulbos) * 100).toFixed(1) : '0.0';
      return `
        <tr style="${cant === 0 ? 'opacity: 0.55;' : 'font-weight: 600;'}">
          <td style="padding: 0.45rem 0.75rem;">${escapeHtml(nombre)}</td>
          <td style="padding: 0.45rem 0.75rem; text-align: right;">${cant.toLocaleString('es-AR')}</td>
          <td style="padding: 0.45rem 0.75rem; text-align: right; color: var(--text-muted); font-size: 0.75rem;">${pct}%</td>
        </tr>
      `;
    }).join('');

    cuerpo.innerHTML = `
      <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 1rem; margin-bottom: 1.25rem; background: var(--surface-alt); padding: 1rem; border-radius: var(--radius-md); border: 1px solid var(--border);">
        <div>
          <label class="text-xs text-muted font-bold text-uppercase">Fecha y Hora:</label>
          <div class="font-medium text-main text-sm">${escapeHtml(fechaStr)}</div>
        </div>
        <div>
          <label class="text-xs text-muted font-bold text-uppercase">Usuario Creador:</label>
          <div class="font-medium text-main text-sm">${escapeHtml(mov.usuarioCreadorNombre || `Usuario #${mov.usuarioCreadorId || '-'}`)}</div>
        </div>
        <div>
          <label class="text-xs text-muted font-bold text-uppercase">Origen:</label>
          <div class="font-medium text-main text-sm d-flex align-center flex-wrap gap-xs mt-1">
            <span>${escapeHtml(opInfo.origenText)}</span>
            ${opInfo.origenImpacto ? `<span class="badge ${opInfo.origenImpacto.clase}" style="font-size: 0.7rem;">${escapeHtml(opInfo.origenImpacto.texto)}</span>` : ''}
          </div>
        </div>
        <div>
          <label class="text-xs text-muted font-bold text-uppercase">Destino / Titular Afectado:</label>
          <div class="font-medium text-main text-sm d-flex align-center flex-wrap gap-xs mt-1">
            <span>${escapeHtml(opInfo.destinoText)}</span>
            ${opInfo.destinoImpacto ? `<span class="badge ${opInfo.destinoImpacto.clase}" style="font-size: 0.7rem;">${escapeHtml(opInfo.destinoImpacto.texto)}</span>` : ''}
          </div>
        </div>
      </div>

      <div class="mb-3">
        <label class="text-xs text-muted font-bold text-uppercase mb-2 d-block">Desglose Físico por Calibre (Clases 0 a 4):</label>
        <div style="border: 1px solid var(--border); border-radius: var(--radius-sm); overflow: hidden;">
          <table class="table mb-0" style="font-size: 0.85rem;">
            <thead style="background: var(--surface-hover);">
              <tr>
                <th style="padding: 0.5rem 0.75rem;">Clase / Calibre</th>
                <th style="padding: 0.5rem 0.75rem; text-align: right;">Cantidad</th>
                <th style="padding: 0.5rem 0.75rem; text-align: right; width: 65px;">% Total</th>
              </tr>
            </thead>
            <tbody>
              ${cantidadesHtml}
              <tr style="background: var(--surface-alt); border-top: 2px solid var(--border); font-size: 0.95rem;">
                <td style="padding: 0.6rem 0.75rem; font-weight: 800;">Total General de Bulbos:</td>
                <td style="padding: 0.6rem 0.75rem; text-align: right; font-weight: 800; color: var(--primary); font-size: 1.05rem;">
                  ${totalBulbos.toLocaleString('es-AR')}
                </td>
                <td style="padding: 0.6rem 0.75rem; text-align: right; font-weight: 800; color: var(--text-muted); font-size: 0.8rem;">100%</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <div>
        <label class="text-xs text-muted font-bold text-uppercase mb-1 d-block">Motivo / Observación Registrada:</label>
        <div class="alert alert-info mt-1" style="font-size: 0.875rem; padding: 0.75rem; line-height: 1.45;">
          ${escapeHtml(mov.observaciones || 'Sin observaciones registradas.')}
        </div>
      </div>
    `;

    if (mov.estado === 'Pendiente') {
      accionesExtra.innerHTML = `
        <button class="btn btn-sm btn-success" id="btn-modal-confirmar" title="Confirmar movimiento">
          ✓ Confirmar Movimiento
        </button>
        <button class="btn btn-sm btn-danger" id="btn-modal-anular" title="Anular movimiento">
          ✕ Anular
        </button>
      `;

      accionesExtra.querySelector('#btn-modal-confirmar').addEventListener('click', async () => {
        if (confirm('¿Confirmar este movimiento de stock ahora?')) {
          try {
            await movimientosService.confirmar(mov.id);
            toast.success('Movimiento confirmado e impactado exitosamente.');
            modal.classList.remove('is-active');
            await this._loadHistorial();
          } catch (err) {
            toast.error(err.message || 'Error al confirmar movimiento');
          }
        }
      });

      accionesExtra.querySelector('#btn-modal-anular').addEventListener('click', async () => {
        const motivo = prompt('Ingrese el motivo de anulación:') || 'Anulado por usuario';
        try {
          await movimientosService.anular(mov.id, motivo);
          toast.info('Movimiento anulado.');
          modal.classList.remove('is-active');
          await this._loadHistorial();
        } catch (err) {
          toast.error(err.message || 'Error al anular movimiento');
        }
      });
    } else {
      accionesExtra.innerHTML = '';
    }

    modal.classList.add('is-active');
  }
}

