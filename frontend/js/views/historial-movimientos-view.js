/**
 * SIGA - views/historial-movimientos-view.js
 * Vista del Historial de Movimientos de Bulbos
 * Vista exclusiva para consultar el historial completo y trazabilidad física
 * de todos los movimientos de bulbos (Ventas, Compras, Traspasos, Cosechas, Plantaciones y Ajustes).
 */

import { movimientosService } from '../services/movimientos-service.js';
import { toast } from '../utils/toast.js';
import { escapeHtml } from '../utils/escape-html.js';

export class HistorialMovimientosView {
  constructor(router) {
    this.router = router;
    this.container = null;
    this.filterBusqueda = '';
    this.filterTipo = 'todos';
    this.filterEstado = 'todos';
    this.filterEntidad = 'todos';
    this.movimientosCache = [];
  }

  async render(rootElement) {
    this.container = rootElement;

    this.container.innerHTML = `
      <div class="fade-in">
        <!-- Encabezado Principal del Historial -->
        <div class="d-flex justify-between align-center mb-4 flex-wrap gap-md">
          <div>
            <div class="d-flex align-center gap-sm">
              <h1 style="font-size: 1.5rem; margin-bottom: 0.25rem;">Historial de Movimientos</h1>
              <span class="badge badge-primary text-xs" style="text-transform: uppercase; font-weight: 700;">Trazabilidad Física</span>
            </div>
            <p class="text-muted text-sm">Registro cronológico de todas las entradas, salidas y transferencias de bulbos en el sistema</p>
          </div>
          <div class="d-flex gap-sm">
            <button class="btn btn-secondary btn-sm" id="btn-refresh-historial" title="Actualizar lista desde el servidor">
              <svg class="icon-svg sm" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <polyline points="23 4 23 10 17 10"></polyline>
                <polyline points="1 20 1 14 7 14"></polyline>
                <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"></path>
              </svg>
              <span>Refrescar</span>
            </button>
          </div>
        </div>

        <!-- Tarjetas de Resumen KPI -->
        <div class="metric-grid mb-4">
          <div class="metric-card primary">
            <div class="metric-icon-wrap">
              <svg class="icon-svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <polyline points="17 1 21 5 17 9"></polyline>
                <path d="M3 11V9a4 4 0 0 1 4-4h14"></path>
                <polyline points="7 23 3 19 7 15"></polyline>
                <path d="M21 13v2a4 4 0 0 1-4 4H3"></path>
              </svg>
            </div>
            <div class="metric-content">
              <span class="metric-label">Total Movimientos</span>
              <div class="metric-value" id="kpi-hist-total">0</div>
              <span class="metric-subtext">Registros en historial</span>
            </div>
          </div>

          <div class="metric-card accent">
            <div class="metric-icon-wrap">
              <svg class="icon-svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <circle cx="12" cy="12" r="10"></circle>
                <polyline points="12 6 12 12 16 14"></polyline>
              </svg>
            </div>
            <div class="metric-content">
              <span class="metric-label">Bulbos Movilizados</span>
              <div class="metric-value" id="kpi-hist-bulbos">0</div>
              <span class="metric-subtext">Unidades físicas totales</span>
            </div>
          </div>

          <div class="metric-card success">
            <div class="metric-icon-wrap">
              <svg class="icon-svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path>
                <polyline points="22 4 12 14.01 9 11.01"></polyline>
              </svg>
            </div>
            <div class="metric-content">
              <span class="metric-label">Confirmados</span>
              <div class="metric-value" id="kpi-hist-confirmados">0</div>
              <span class="metric-subtext">Impacto en stock asentado</span>
            </div>
          </div>

          <div class="metric-card warning">
            <div class="metric-icon-wrap">
              <svg class="icon-svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <circle cx="12" cy="12" r="10"></circle>
                <line x1="12" y1="8" x2="12" y2="12"></line>
                <line x1="12" y1="16" x2="12.01" y2="16"></line>
              </svg>
            </div>
            <div class="metric-content">
              <span class="metric-label">Pendientes</span>
              <div class="metric-value" id="kpi-hist-pendientes">0</div>
              <span class="metric-subtext">Por revisar o confirmar</span>
            </div>
          </div>
        </div>

        <!-- Filtros y Buscador en Vivo -->
        <div class="table-card mb-4" style="padding: 1rem 1.25rem;">
          <div class="d-flex align-center gap-md flex-wrap justify-between">
            <!-- Buscador -->
            <div style="flex: 1; min-width: 240px;">
              <input 
                type="text" 
                id="filter-hist-busqueda" 
                class="form-control" 
                placeholder="🔍 Buscar por comprobante, participante u observaciones..." 
                style="padding: 0.5rem 0.85rem;"
              />
            </div>

            <div class="d-flex align-center gap-md flex-wrap">
              <!-- Filtro por Tipo de Operación -->
              <div class="d-flex align-center gap-xs">
                <label class="text-xs text-muted font-bold text-uppercase">Tipo:</label>
                <select id="filter-hist-tipo" class="form-control" style="width: 175px; padding: 0.45rem 0.65rem;">
                  <option value="todos">Todos los Tipos</option>
                  <option value="Venta">🛒 Ventas a Clientes</option>
                  <option value="Compra">📥 Compras a Clientes</option>
                  <option value="Traspaso">🔄 Traspasos</option>
                  <option value="Cosecha">🌾 Cosechas</option>
                  <option value="Plantacion">🌱 Plantaciones</option>
                  <option value="AjusteStock">⚖️ Ajustes de Stock</option>
                </select>
              </div>

              <!-- Filtro por Estado -->
              <div class="d-flex align-center gap-xs">
                <label class="text-xs text-muted font-bold text-uppercase">Estado:</label>
                <select id="filter-hist-estado" class="form-control" style="width: 160px; padding: 0.45rem 0.65rem;">
                  <option value="todos">Todos los Estados</option>
                  <option value="Confirmado">Solo Confirmados</option>
                  <option value="Pendiente">Solo Pendientes</option>
                  <option value="Anulado">Solo Anulados</option>
                </select>
              </div>

              <!-- Filtro por Titular -->
              <div class="d-flex align-center gap-xs">
                <label class="text-xs text-muted font-bold text-uppercase">Titular:</label>
                <select id="filter-hist-entidad" class="form-control" style="width: 190px; padding: 0.45rem 0.65rem;">
                  <option value="todos">Todos los Titulares</option>
                  <option value="empresa">🏢 Depósito Central</option>
                  <option value="clientes">👤 Solo Clientes</option>
                </select>
              </div>
            </div>
          </div>
        </div>

        <!-- Tabla Histórica de Movimientos -->
        <div class="table-card">
          <div class="table-responsive">
            <table class="table" id="historial-table">
              <thead>
                <tr>
                  <th style="width: 130px;">Comprobante</th>
                  <th style="width: 135px;">Fecha</th>
                  <th style="width: 145px;">Tipo de Operación</th>
                  <th>Origen</th>
                  <th>Destino</th>
                  <th style="text-align: right; width: 115px;">Total Bulbos</th>
                  <th style="width: 110px;">Estado</th>
                  <th>Observaciones / Motivo</th>
                  <th style="text-align: right; width: 130px;">Acciones</th>
                </tr>
              </thead>
              <tbody id="historial-table-body">
                <tr>
                  <td colspan="9" class="text-center text-muted" style="padding: 2.5rem;">Cargando historial de movimientos...</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- Modal de Detalle de Movimiento -->
        <div id="modal-detalle-mov" class="modal-backdrop" role="dialog" aria-modal="true">
          <div class="modal-dialog modal-lg" style="max-width: 620px;">
            <div class="modal-header">
              <div>
                <div class="d-flex align-center gap-sm">
                  <h3 class="modal-title font-bold text-primary" id="det-mov-titulo" style="margin-bottom: 0;">Detalle de Movimiento</h3>
                  <span id="det-mov-badge-tipo"></span>
                  <span id="det-mov-badge-estado"></span>
                </div>
                <p class="text-xs text-muted mt-1" style="margin-bottom: 0;" id="det-mov-subtitulo">Comprobante y trazabilidad de inventario</p>
              </div>
              <button type="button" class="modal-close-btn" id="btn-close-det-mov">✕</button>
            </div>
            <div class="modal-body" id="det-mov-cuerpo" style="padding: 1.5rem;">
              <!-- Cargado dinámicamente -->
            </div>
            <div class="modal-footer d-flex justify-between align-center">
              <div id="det-mov-acciones-extra" class="d-flex gap-xs"></div>
              <button type="button" class="btn btn-outline" id="btn-cerrar-det-mov">Cerrar</button>
            </div>
          </div>
        </div>

      </div>
    `;

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
    tbody.innerHTML = `<tr><td colspan="9" class="text-center text-muted" style="padding: 2.5rem;">Cargando historial de movimientos...</td></tr>`;

    try {
      const result = await movimientosService.listar({ estado: 'todos' });
      this.movimientosCache = result.items || [];
      this._updateKpis();
      this._renderTable();
    } catch (err) {
      tbody.innerHTML = `<tr><td colspan="9" class="text-center text-danger" style="padding: 2.5rem;">${escapeHtml(err.message || 'Error al cargar el historial de movimientos')}</td></tr>`;
      toast.error('No se pudo cargar el historial de movimientos de bulbos.');
    }
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
    const tipo = String(m.tipoOperacion || '').toLowerCase();
    const obs = (m.observaciones || '').toLowerCase();

    // Movimiento Comercial
    if (m.movimientoComercialId || tipo.includes('venta') || tipo.includes('compra') || tipo.includes('traspaso')) {
      if (tipo.includes('compra')) {
        return {
          categoria: 'Compra',
          nombre: 'Compra a Cliente',
          badgeClass: 'badge-info',
          icon: '📥',
          origenText: m.origenNombre || `Cliente #${m.origenId}`,
          destinoText: '🏢 Depósito Central',
          refId: m.movimientoComercialId ? `#${m.movimientoComercialId}` : null
        };
      }
      if (tipo.includes('traspaso')) {
        return {
          categoria: 'Traspaso',
          nombre: 'Traspaso entre Clientes',
          badgeClass: 'badge-accent',
          icon: '🔄',
          origenText: m.origenNombre || `Cliente #${m.origenId}`,
          destinoText: m.destinoNombre || `Cliente #${m.destinoId}`,
          refId: m.movimientoComercialId ? `#${m.movimientoComercialId}` : null
        };
      }
      return {
        categoria: 'Venta',
        nombre: 'Venta a Cliente',
        badgeClass: 'badge-primary',
        icon: '🛒',
        origenText: '🏢 Depósito Central',
        destinoText: m.destinoNombre || `Cliente #${m.destinoId}`,
        refId: m.movimientoComercialId ? `#${m.movimientoComercialId}` : null
      };
    }

    // Cosecha
    if (m.cosechaId || tipo.includes('cosecha')) {
      return {
        categoria: 'Cosecha',
        nombre: 'Cosecha',
        badgeClass: 'badge-warning',
        icon: '🌾',
        origenText: '🌱 Campo / Fila',
        destinoText: m.destinoNombre || (m.destinoId === 1 ? '🏢 Depósito Central' : `Cliente #${m.destinoId}`),
        refId: m.cosechaId ? `#${m.cosechaId}` : null
      };
    }

    // Plantación
    if (m.plantacionId || tipo.includes('planta')) {
      return {
        categoria: 'Plantacion',
        nombre: 'Plantación',
        badgeClass: 'badge-success',
        icon: '🌱',
        origenText: m.origenNombre || (m.origenId === 1 ? '🏢 Depósito Central' : `Cliente #${m.origenId}`),
        destinoText: '🌱 Fila / Lote',
        refId: m.plantacionId ? `#${m.plantacionId}` : null
      };
    }

    // Ajuste de stock
    let esIngreso = false;
    if (obs.includes('[ingreso') || obs.includes('(+)') || m.origenId === 0 || (m.origenNombre || '').toLowerCase().includes('ajuste')) {
      esIngreso = true;
    }

    return {
      categoria: 'AjusteStock',
      nombre: esIngreso ? 'Ajuste Stock (+)' : 'Ajuste Stock (-)',
      badgeClass: esIngreso ? 'badge-success' : 'badge-danger',
      icon: esIngreso ? '➕' : '➖',
      origenText: m.origenNombre || (m.origenId === 1 ? '🏢 Depósito Central' : (m.origenId === 0 ? '⚖️ Ajustes' : `Cliente #${m.origenId}`)),
      destinoText: m.destinoNombre || (m.destinoId === 1 ? '🏢 Depósito Central' : (m.destinoId === 0 ? '⚖️ Ajustes' : `Cliente #${m.destinoId}`)),
      refId: null
    };
  }

  _renderTable() {
    const tbody = this.container.querySelector('#historial-table-body');
    let items = [...this.movimientosCache];

    // 1. Filtro de búsqueda
    if (this.filterBusqueda) {
      items = items.filter(m => {
        const idStr = String(m.id || '');
        const compStr = String(m.numeroComprobante || '').toLowerCase();
        const obsStr = String(m.observaciones || '').toLowerCase();
        const origStr = String(m.origenNombre || '').toLowerCase();
        const destStr = String(m.destinoNombre || '').toLowerCase();
        return idStr.includes(this.filterBusqueda) ||
          compStr.includes(this.filterBusqueda) ||
          obsStr.includes(this.filterBusqueda) ||
          origStr.includes(this.filterBusqueda) ||
          destStr.includes(this.filterBusqueda);
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
    if (this.filterEntidad === 'empresa') {
      items = items.filter(m => {
        const orig = (m.origenNombre || '').toLowerCase();
        const dest = (m.destinoNombre || '').toLowerCase();
        const obs = (m.observaciones || '').toLowerCase();
        return m.origenId === 1 || m.destinoId === 1 ||
          orig.includes('empresa') || orig.includes('central') ||
          dest.includes('empresa') || dest.includes('central') ||
          obs.includes('empresa');
      });
    } else if (this.filterEntidad === 'clientes') {
      items = items.filter(m => {
        const orig = (m.origenNombre || '').toLowerCase();
        const dest = (m.destinoNombre || '').toLowerCase();
        return (m.origenId > 1 && !orig.includes('ajuste')) ||
          (m.destinoId > 1 && !dest.includes('ajuste'));
      });
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
        .replace(/\[Ingreso a Empresa\]\s*/i, '')
        .replace(/\[Egreso de Empresa\]\s*/i, '')
        .replace(/\[Ingreso a Cliente\]\s*/i, '')
        .replace(/\[Egreso de Cliente\]\s*/i, '')
        .replace(/\[Ajuste de Stock Empresa \([+-]\)\]\s*/i, '')
        .trim();

      if (!motivoLimpio) motivoLimpio = opInfo.nombre;

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
          <td class="text-xs text-main font-medium" style="max-width: 150px; word-break: break-word;">
            ${escapeHtml(opInfo.origenText)}
          </td>
          <td class="text-xs text-main font-medium" style="max-width: 150px; word-break: break-word;">
            ${escapeHtml(opInfo.destinoText)}
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
          <div class="font-medium text-main text-sm">${escapeHtml(opInfo.origenText)}</div>
        </div>
        <div>
          <label class="text-xs text-muted font-bold text-uppercase">Destino:</label>
          <div class="font-medium text-main text-sm">${escapeHtml(opInfo.destinoText)}</div>
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

