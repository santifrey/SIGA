/**
 * SIGA - views/reportes-view.js
 * Vista Ejecutiva de Reportes, Ficha Integral de Clientes y Rendimiento Productivo
 */

import { reportesService } from '../services/reportes-service.js';
import { clientesService } from '../services/clientes-service.js';
import { temporadasService } from '../services/temporadas-service.js';
import { toast } from '../utils/toast.js';
import { escapeHtml } from '../utils/html.js';

export class ReportesView {
  constructor(router) {
    this.router = router;
    this.container = null;
    this.clientes = [];
    this.temporadas = [];
    this.selectedClienteId = null;
    this.selectedTemporadaId = null;
    this.resumenEjecutivo = null;
    this.reporteCliente = null;
    this.reporteTemporada = null;
  }

  async render(rootElement) {
    this.container = rootElement;
    this.container.innerHTML = `
      <div class="fade-in">
        <!-- Encabezado de la Vista -->
        <div class="d-flex justify-between align-center mb-4 flex-wrap gap-md">
          <div>
            <h1 style="font-size: 1.5rem; margin-bottom: 0.25rem;">Reportes Ejecutivos y Trazabilidad</h1>
            <p class="text-muted text-sm">Tablero general de control, estado 360° de clientes y rendimiento productivo por temporada.</p>
          </div>
          <div>
            <button class="btn btn-secondary" id="btn-refresh-reportes" title="Actualizar todos los reportes">
              <svg class="icon-svg sm" viewBox="0 0 24 24" width="16" height="16" stroke="currentColor" fill="none" stroke-width="2">
                <polyline points="23 4 23 10 17 10"></polyline>
                <polyline points="1 20 1 14 7 14"></polyline>
                <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"></path>
              </svg>
              <span>Refrescar Reportes</span>
            </button>
          </div>
        </div>

        <!-- ===================================================================
             SECCIÓN 1: RESUMEN EJECUTIVO GENERAL
             =================================================================== -->
        <div class="mb-4" id="contenedor-resumen-ejecutivo">
          <div class="card p-4 text-center text-muted">Cargando métricas ejecutivas...</div>
        </div>

        <!-- ===================================================================
             SECCIÓN 2: FICHA / ESTADO INTEGRAL DE CLIENTE (360°)
             =================================================================== -->
        <div class="card mb-4">
          <div class="card-header d-flex justify-between align-center p-3 border-bottom flex-wrap gap-sm">
            <div>
              <h3 class="card-title text-base m-0">2. Ficha y Estado Integral de Cliente (Visión 360°)</h3>
              <span class="text-xs text-muted">Stock actual en custodia, derechos sobre lotes plantados e historial de cosechas</span>
            </div>
            <div class="d-flex align-center gap-sm">
              <label for="select-reporte-cliente" class="text-xs font-bold uppercase text-muted mb-0">Cliente:</label>
              <select id="select-reporte-cliente" class="form-control" style="min-width: 260px;">
                <option value="">Cargando clientes...</option>
              </select>
            </div>
          </div>
          <div class="card-body p-4" id="contenedor-reporte-cliente">
            <div class="text-center p-4 text-muted">Seleccione un cliente para ver su ficha integral.</div>
          </div>
        </div>

        <!-- ===================================================================
             SECCIÓN 3: RENDIMIENTO Y TRAZABILIDAD PRODUCTIVA POR TEMPORADA
             =================================================================== -->
        <div class="card mb-4">
          <div class="card-header d-flex justify-between align-center p-3 border-bottom flex-wrap gap-sm">
            <div>
              <h3 class="card-title text-base m-0">3. Rendimiento y Trazabilidad Productiva por Temporada</h3>
              <span class="text-xs text-muted">Comparativa siembra vs cosecha por clase de bulbo, rendimiento por campo y trazabilidad por lote</span>
            </div>
            <div class="d-flex align-center gap-sm">
              <label for="select-reporte-temporada" class="text-xs font-bold uppercase text-muted mb-0">Temporada:</label>
              <select id="select-reporte-temporada" class="form-control" style="min-width: 220px;">
                <option value="">Cargando temporadas...</option>
              </select>
            </div>
          </div>
          <div class="card-body p-4" id="contenedor-reporte-temporada">
            <div class="text-center p-4 text-muted">Seleccione una temporada para ver su rendimiento productivo.</div>
          </div>
        </div>
      </div>
    `;

    await this.cargarDatosIniciales();
    this.bindEvents();
  }

  async cargarDatosIniciales() {
    try {
      const [resumen, clsRes, tempsRes] = await Promise.all([
        reportesService.obtenerResumenEjecutivo().catch(() => null),
        clientesService.listar({ pageSize: 100 }).catch(() => []),
        temporadasService.listarTodas().catch(() => [])
      ]);

      this.resumenEjecutivo = resumen;
      this.clientes = Array.isArray(clsRes) ? clsRes : (clsRes.items || []);
      this.temporadas = Array.isArray(tempsRes) ? tempsRes : (tempsRes.items || []);

      // Renderizar Sección 1: Resumen Ejecutivo
      this.renderResumenEjecutivo();

      // Poblar selector de clientes
      const selectCli = document.getElementById('select-reporte-cliente');
      if (selectCli) {
        if (this.clientes.length === 0) {
          selectCli.innerHTML = '<option value="">No hay clientes registrados</option>';
        } else {
          selectCli.innerHTML = '<option value="">-- Seleccione Cliente --</option>' +
            this.clientes.map(c => {
              const nom = c.persona ? `${c.persona.nombre} ${c.persona.apellido}` : (c.razonSocial || c.nombreCompleto || c.codigoCliente);
              return `<option value="${escapeHtml(c.id)}">${escapeHtml(nom)} (${escapeHtml(c.codigoCliente || 'CLI')})</option>`;
            }).join('');

          // Seleccionar primer cliente por defecto
          this.selectedClienteId = this.clientes[0].id;
          selectCli.value = this.selectedClienteId;
          await this.cargarReporteCliente(this.selectedClienteId);
        }
      }

      // Poblar selector de temporadas
      const selectTemp = document.getElementById('select-reporte-temporada');
      if (selectTemp) {
        if (this.temporadas.length === 0) {
          selectTemp.innerHTML = '<option value="">No hay temporadas registradas</option>';
        } else {
          selectTemp.innerHTML = '<option value="">-- Seleccione Temporada --</option>' +
            this.temporadas.map(t => `<option value="${escapeHtml(t.id)}">${escapeHtml(t.nombre)} (${escapeHtml(t.nombreEstado || t.estado || '')})</option>`).join('');

          // Seleccionar temporada activa o primera
          const activa = this.temporadas.find(t => {
            const st = String(t.nombreEstado || t.estado || '').toLowerCase();
            return !st.includes('cerrada');
          }) || this.temporadas[0];

          this.selectedTemporadaId = activa ? activa.id : this.temporadas[0].id;
          selectTemp.value = this.selectedTemporadaId;
          await this.cargarReporteTemporada(this.selectedTemporadaId);
        }
      }
    } catch (err) {
      toast.error('Error al inicializar reportes ejecutivos: ' + err.message);
    }
  }

  bindEvents() {
    // Botón refrescar
    document.getElementById('btn-refresh-reportes')?.addEventListener('click', async () => {
      try {
        this.resumenEjecutivo = await reportesService.obtenerResumenEjecutivo();
        this.renderResumenEjecutivo();
        if (this.selectedClienteId) await this.cargarReporteCliente(this.selectedClienteId);
        if (this.selectedTemporadaId) await this.cargarReporteTemporada(this.selectedTemporadaId);
        toast.info('Reportes ejecutivos actualizados.');
      } catch (err) {
        toast.error('Error al refrescar reportes: ' + err.message);
      }
    });

    // Cambio cliente
    document.getElementById('select-reporte-cliente')?.addEventListener('change', async (e) => {
      this.selectedClienteId = e.target.value;
      if (this.selectedClienteId) {
        await this.cargarReporteCliente(this.selectedClienteId);
      } else {
        const c = document.getElementById('contenedor-reporte-cliente');
        if (c) c.innerHTML = '<div class="text-center p-4 text-muted">Seleccione un cliente para ver su ficha integral.</div>';
      }
    });

    // Cambio temporada
    document.getElementById('select-reporte-temporada')?.addEventListener('change', async (e) => {
      this.selectedTemporadaId = e.target.value;
      if (this.selectedTemporadaId) {
        await this.cargarReporteTemporada(this.selectedTemporadaId);
      } else {
        const c = document.getElementById('contenedor-reporte-temporada');
        if (c) c.innerHTML = '<div class="text-center p-4 text-muted">Seleccione una temporada para ver su rendimiento productivo.</div>';
      }
    });
  }

  /* =========================================================================
     SECCIÓN 1: RESUMEN EJECUTIVO GENERAL
     ========================================================================= */

  renderResumenEjecutivo() {
    const contenedor = document.getElementById('contenedor-resumen-ejecutivo');
    if (!contenedor) return;

    const r = this.resumenEjecutivo || {};
    const clientesActivos = Number(r.totalClientesActivos ?? r.TotalClientesActivos ?? 0);
    const sucursalesActivas = Number(r.totalSucursalesActivas ?? r.TotalSucursalesActivas ?? 0);
    const tempNombre = r.temporadaActivaNombre ?? r.TemporadaActivaNombre ?? 'En Planificación';
    const tempEstado = r.temporadaActivaEstado ?? r.TemporadaActivaEstado ?? 'Activa';
    const plantadosActiva = Number(r.totalBulbosPlantadosTemporadaActiva ?? r.TotalBulbosPlantadosTemporadaActiva ?? 0);
    const cosechadosActiva = Number(r.totalBulbosCosechadosTemporadaActiva ?? r.TotalBulbosCosechadosTemporadaActiva ?? 0);
    const montoVentasCompletadas = Number(r.montoTotalVentasCompletadas ?? r.MontoTotalVentasCompletadas ?? 0);
    const totalVentasCompletadas = Number(r.totalVentasCompletadas ?? r.TotalVentasCompletadas ?? 0);

    contenedor.innerHTML = `
      <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(240px, 1fr)); gap: 1rem;">
        <!-- KPI 1: Clientes Activos -->
        <div class="card p-3" style="border-left: 4px solid var(--primary); background: var(--surface);">
          <div class="text-xs text-muted uppercase font-bold mb-1">Clientes Activos</div>
          <div class="font-bold text-main" style="font-size: 1.8rem; line-height: 1.1;">
            ${clientesActivos.toLocaleString('es-AR')}
          </div>
          <div class="text-xs text-muted mt-2">Titulares con contratos o lotes vigentes</div>
        </div>

        <!-- KPI 2: Sucursales / Campos Activos -->
        <div class="card p-3" style="border-left: 4px solid var(--accent); background: var(--surface);">
          <div class="text-xs text-muted uppercase font-bold mb-1">Campos / Sucursales</div>
          <div class="font-bold text-main" style="font-size: 1.8rem; line-height: 1.1;">
            ${sucursalesActivas.toLocaleString('es-AR')}
          </div>
          <div class="text-xs text-muted mt-2">Unidades productivas operativas</div>
        </div>

        <!-- KPI 3: Temporada Activa (Plantado vs Cosechado) -->
        <div class="card p-3" style="border-left: 4px solid var(--info); background: var(--surface);">
          <div class="d-flex justify-between align-center mb-1">
            <span class="text-xs text-muted uppercase font-bold">Temporada Activa</span>
            <span class="badge badge-info" style="font-size: 0.65rem;">${escapeHtml(tempEstado)}</span>
          </div>
          <div class="font-bold text-main" style="font-size: 1.1rem; margin-bottom: 0.25rem;">
            ${escapeHtml(tempNombre)}
          </div>
          <div class="text-xs">
            <span class="text-muted">Plantado:</span> <strong>${plantadosActadosFmt(plantadosActiva)}</strong> | 
            <span class="text-muted">Cosechado:</span> <strong class="text-success">${plantadosActadosFmt(cosechadosActiva)}</strong>
          </div>
        </div>

        <!-- KPI 4: Ventas Comerciales Completadas -->
        <div class="card p-3" style="border-left: 4px solid var(--success); background: var(--surface);">
          <div class="text-xs text-muted uppercase font-bold mb-1">Ventas Completadas</div>
          <div class="font-bold text-success" style="font-size: 1.6rem; line-height: 1.1;">
            $ ${montoVentasCompletadas.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
          </div>
          <div class="text-xs text-muted mt-2">${totalVentasCompletadas} operaciones comerciales cerradas</div>
        </div>
      </div>
    `;

    function plantadosActadosFmt(num) {
      return num.toLocaleString('es-AR') + ' u.';
    }
  }

  /* =========================================================================
     SECCIÓN 2: FICHA / ESTADO INTEGRAL DE CLIENTE (VISIÓN 360°)
     ========================================================================= */

  async cargarReporteCliente(clienteId) {
    const contenedor = document.getElementById('contenedor-reporte-cliente');
    if (!contenedor) return;

    contenedor.innerHTML = '<div class="text-center p-4 text-muted">Consultando estado integral del cliente...</div>';

    try {
      const rep = await reportesService.obtenerEstadoIntegralCliente(clienteId);
      this.reporteCliente = rep;
      this.renderReporteCliente();
    } catch (err) {
      contenedor.innerHTML = `<div class="p-3 text-danger">Error al cargar la ficha del cliente: ${escapeHtml(err.message)}</div>`;
    }
  }

  renderReporteCliente() {
    const contenedor = document.getElementById('contenedor-reporte-cliente');
    if (!contenedor || !this.reporteCliente) return;

    const r = this.reporteCliente;
    const nombre = r.nombreCompleto ?? r.NombreCompleto ?? 'Cliente';
    const codigo = r.codigoCliente ?? r.CodigoCliente ?? '';
    const doc = r.documento ?? r.Documento ?? '-';
    const email = r.email ?? r.Email ?? 'Sin email';
    const totalStock = Number(r.totalBulbosEnStock ?? r.TotalBulbosEnStock ?? 0);
    const stockPorClase = r.stockDisponiblePorClase ?? r.StockDisponiblePorClase ?? [];
    const derechosLotes = r.derechosSobreLotesPlantados ?? r.DerechosSobreLotesPlantados ?? [];
    const historialProd = r.historialProductivoPorTemporada ?? r.HistorialProductivoPorTemporada ?? [];

    contenedor.innerHTML = `
      <!-- Encabezado Ficha Cliente -->
      <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 1rem; margin-bottom: 1.5rem; background: var(--surface-alt); padding: 1rem; border-radius: var(--radius-md);">
        <div>
          <span class="text-xs text-muted uppercase font-bold">Cliente:</span>
          <div class="font-bold text-main" style="font-size: 1.15rem;">${escapeHtml(nombre)}</div>
          <span class="text-xs text-muted">Legajo: ${escapeHtml(codigo)}</span>
        </div>
        <div>
          <span class="text-xs text-muted uppercase font-bold">Documento / CUIT:</span>
          <div class="font-semibold text-main">${escapeHtml(doc)}</div>
          <span class="text-xs text-muted">${escapeHtml(email)}</span>
        </div>
        <div>
          <span class="text-xs text-muted uppercase font-bold">Total Stock en Custodia:</span>
          <div class="font-bold text-primary" style="font-size: 1.4rem;">
            ${totalStock.toLocaleString('es-AR')} <span class="text-xs text-muted font-normal">bulbos</span>
          </div>
        </div>
      </div>

      <!-- 2.1 Stock Disponible por Clase -->
      <h4 class="text-sm font-bold uppercase mb-2">Stock Disponible en Custodia por Calibre</h4>
      <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(130px, 1fr)); gap: 0.75rem; margin-bottom: 1.5rem;">
        ${[0, 1, 2, 3, 4].map(c => {
          const item = stockPorClase.find(s => Number(s.claseBulbo ?? s.ClaseBulbo) === c);
          const cant = Number(item ? (item.cantidadDisponible ?? item.CantidadDisponible ?? 0) : 0);
          return `
            <div class="card p-2 text-center" style="background: var(--surface);">
              <span class="badge badge-accent mb-1" style="font-size: 0.65rem;">Clase ${c}</span>
              <div class="font-bold text-main" style="font-size: 1.1rem;">${cant.toLocaleString('es-AR')}</div>
              <span class="text-xs text-muted">unidades</span>
            </div>
          `;
        }).join('')}
      </div>

      <!-- 2.2 Derechos sobre Lotes Plantados -->
      <div class="mb-4">
        <div class="d-flex justify-between align-center mb-2">
          <h4 class="text-sm font-bold uppercase m-0">Derechos Vigentes sobre Lotes Plantados</h4>
          <span class="text-xs text-muted">${derechosLotes.length} lotes asignados</span>
        </div>
        <div class="table-responsive">
          <table class="table table-hover mb-0">
            <thead>
              <tr>
                <th># Lote</th>
                <th>Temporada</th>
                <th>Campo / Sucursal</th>
                <th>Fila / Surco</th>
                <th>Estado del Lote</th>
                <th style="text-align: right;">Bulbos Plantados</th>
                <th style="text-align: center;">Origen Titularidad</th>
              </tr>
            </thead>
            <tbody>
              ${derechosLotes.length === 0 ? '<tr><td colspan="7" class="text-center p-3 text-muted">El cliente no posee lotes plantados en este momento.</td></tr>' :
                derechosLotes.map(d => {
                  const loteId = d.loteId ?? d.LoteId;
                  const tempNom = d.nombreTemporada ?? d.NombreTemporada ?? '-';
                  const sucNom = d.nombreSucursal ?? d.NombreSucursal ?? '-';
                  const fila = d.identificacionFila ?? d.IdentificacionFila ?? '-';
                  const estadoLote = d.nombreEstadoLote ?? d.NombreEstadoLote ?? 'Plantado';
                  const cant = Number(d.cantidadPlantadaTotal ?? d.CantidadPlantadaTotal ?? 0);
                  const cesion = Boolean(d.adquiridoPorTransferencia ?? d.AdquiridoPorTransferencia);

                  return `
                    <tr>
                      <td><strong>#${escapeHtml(loteId)}</strong></td>
                      <td>${escapeHtml(tempNom)}</td>
                      <td>${escapeHtml(sucNom)}</td>
                      <td>${escapeHtml(fila)}</td>
                      <td><span class="badge badge-secondary">${escapeHtml(estadoLote)}</span></td>
                      <td style="text-align: right; font-weight: 600;">${cant.toLocaleString('es-AR')}</td>
                      <td style="text-align: center;">
                        ${cesion
                          ? '<span class="badge badge-info" title="Adquirido por contrato de cesión">Cesión de Derechos</span>'
                          : '<span class="badge badge-secondary">Titular Original</span>'}
                      </td>
                    </tr>
                  `;
                }).join('')}
            </tbody>
          </table>
        </div>
      </div>

      <!-- 2.3 Historial Productivo por Temporada -->
      <div>
        <h4 class="text-sm font-bold uppercase mb-2">Historial Productivo por Temporada</h4>
        <div class="table-responsive">
          <table class="table table-hover mb-0">
            <thead>
              <tr>
                <th>Temporada</th>
                <th style="text-align: right;">Bulbos Plantados</th>
                <th style="text-align: right;">Bulbos Cosechados</th>
                <th style="text-align: right;">Diferencia Neta</th>
                <th style="text-align: right;">% Rendimiento (Multiplicación)</th>
              </tr>
            </thead>
            <tbody>
              ${historialProd.length === 0 ? '<tr><td colspan="5" class="text-center p-3 text-muted">Sin historial productivo registrado para este cliente.</td></tr>' :
                historialProd.map(h => {
                  const tempNom = h.nombreTemporada ?? h.NombreTemporada ?? '-';
                  const plant = Number(h.bulbosPlantados ?? h.BulbosPlantados ?? 0);
                  const cosech = Number(h.bulbosCosechados ?? h.BulbosCosechados ?? 0);
                  const dif = Number(h.diferenciaNeta ?? h.DiferenciaNeta ?? (cosech - plant));
                  const pct = Number(h.porcentajeRendimiento ?? h.PorcentajeRendimiento ?? 0);

                  const difBadge = dif >= 0 ? 'text-success font-bold' : 'text-danger font-bold';
                  const difSign = dif > 0 ? `+${dif.toLocaleString('es-AR')}` : dif.toLocaleString('es-AR');

                  return `
                    <tr>
                      <td><strong>${escapeHtml(tempNom)}</strong></td>
                      <td style="text-align: right;">${plant.toLocaleString('es-AR')}</td>
                      <td style="text-align: right; font-weight: 600;">${cosech.toLocaleString('es-AR')}</td>
                      <td style="text-align: right;" class="${difBadge}">${difSign}</td>
                      <td style="text-align: right;">
                        <span class="badge ${pct >= 100 ? 'badge-success' : (pct > 0 ? 'badge-warning' : 'badge-secondary')}">
                          ${pct.toFixed(2)} %
                        </span>
                      </td>
                    </tr>
                  `;
                }).join('')}
            </tbody>
          </table>
        </div>
      </div>
    `;
  }

  /* =========================================================================
     SECCIÓN 3: RENDIMIENTO Y TRAZABILIDAD PRODUCTIVA POR TEMPORADA
     ========================================================================= */

  async cargarReporteTemporada(temporadaId) {
    const contenedor = document.getElementById('contenedor-reporte-temporada');
    if (!contenedor) return;

    contenedor.innerHTML = '<div class="text-center p-4 text-muted">Consultando rendimiento productivo de la temporada...</div>';

    try {
      const rep = await reportesService.obtenerRendimientoTemporada(temporadaId);
      this.reporteTemporada = rep;
      this.renderReporteTemporada();
    } catch (err) {
      contenedor.innerHTML = `<div class="p-3 text-danger">Error al cargar reporte de temporada: ${escapeHtml(err.message)}</div>`;
    }
  }

  renderReporteTemporada() {
    const contenedor = document.getElementById('contenedor-reporte-temporada');
    if (!contenedor || !this.reporteTemporada) return;

    const r = this.reporteTemporada;
    const tempNom = r.nombreTemporada ?? r.NombreTemporada ?? 'Temporada';
    const totalPlant = Number(r.totalPlantadoTemporada ?? r.TotalPlantadoTemporada ?? 0);
    const totalCosech = Number(r.totalCosechadoTemporada ?? r.TotalCosechadoTemporada ?? 0);
    const difGlobal = Number(r.diferenciaNetaTemporada ?? r.DiferenciaNetaTemporada ?? (totalCosech - totalPlant));
    const pctGlobal = Number(r.porcentajeRendimientoGlobal ?? r.PorcentajeRendimientoGlobal ?? 0);

    const compClases = r.comparativaPorClase ?? r.ComparativaPorClase ?? [];
    const desgSucursales = r.desglosePorSucursal ?? r.DesglosePorSucursal ?? [];
    const trazLotes = r.trazabilidadPorLote ?? r.TrazabilidadPorLote ?? [];

    contenedor.innerHTML = `
      <!-- Tarjetas Resumen de Temporada -->
      <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 1rem; margin-bottom: 1.5rem; background: var(--surface-alt); padding: 1rem; border-radius: var(--radius-md);">
        <div>
          <span class="text-xs text-muted uppercase font-bold">Temporada Productiva:</span>
          <div class="font-bold text-main" style="font-size: 1.15rem;">${escapeHtml(tempNom)}</div>
        </div>
        <div>
          <span class="text-xs text-muted uppercase font-bold">Total Plantado:</span>
          <div class="font-bold text-main" style="font-size: 1.3rem;">${totalPlant.toLocaleString('es-AR')} u.</div>
        </div>
        <div>
          <span class="text-xs text-muted uppercase font-bold">Total Cosechado:</span>
          <div class="font-bold text-success" style="font-size: 1.3rem;">${totalCosech.toLocaleString('es-AR')} u.</div>
        </div>
        <div>
          <span class="text-xs text-muted uppercase font-bold">Rendimiento Multiplicador:</span>
          <div>
            <span class="badge ${pctGlobal >= 100 ? 'badge-success' : 'badge-warning'}" style="font-size: 0.95rem; padding: 0.35rem 0.75rem;">
              ${pctGlobal.toFixed(2)} % (${difGlobal >= 0 ? '+' : ''}${difGlobal.toLocaleString('es-AR')})
            </span>
          </div>
        </div>
      </div>

      <!-- 3.1 Comparativa por Clase de Bulbo -->
      <div class="mb-4">
        <h4 class="text-sm font-bold uppercase mb-2">3.1 Comparativa Productiva por Clase de Bulbo (Calibre)</h4>
        <div class="table-responsive">
          <table class="table table-hover mb-0">
            <thead>
              <tr>
                <th>Calibre / Clase</th>
                <th style="text-align: right;">Cantidad Plantada</th>
                <th style="text-align: right;">Cantidad Cosechada</th>
                <th style="text-align: right;">Diferencia Neta</th>
                <th style="text-align: right;">% Rendimiento</th>
              </tr>
            </thead>
            <tbody>
              ${compClases.length === 0 ? '<tr><td colspan="5" class="text-center p-3 text-muted">Sin datos comparativos por clase.</td></tr>' :
                compClases.map(c => {
                  const claseId = c.claseBulbo ?? c.ClaseBulbo;
                  const nombreClase = c.nombreClase ?? c.NombreClase ?? `Clase ${claseId}`;
                  const plant = Number(c.cantidadPlantada ?? c.CantidadPlantada ?? 0);
                  const cosech = Number(c.cantidadCosechada ?? c.CantidadCosechada ?? 0);
                  const dif = Number(c.diferenciaNeta ?? c.DiferenciaNeta ?? (cosech - plant));
                  const pct = Number(c.porcentajeRendimiento ?? c.PorcentajeRendimiento ?? 0);

                  const difBadge = dif >= 0 ? 'text-success font-bold' : 'text-danger font-bold';
                  const difSign = dif > 0 ? `+${dif.toLocaleString('es-AR')}` : dif.toLocaleString('es-AR');

                  return `
                    <tr>
                      <td>
                        <strong>${escapeHtml(nombreClase)}</strong>
                        <span class="text-xs text-muted d-block">${this.getDescripcionClase(claseId)}</span>
                      </td>
                      <td style="text-align: right;">${plant.toLocaleString('es-AR')}</td>
                      <td style="text-align: right; font-weight: 600;">${cosech.toLocaleString('es-AR')}</td>
                      <td style="text-align: right;" class="${difBadge}">${difSign}</td>
                      <td style="text-align: right;">
                        <span class="badge ${pct >= 100 ? 'badge-success' : (pct > 0 ? 'badge-warning' : 'badge-secondary')}">
                          ${pct.toFixed(2)} %
                        </span>
                      </td>
                    </tr>
                  `;
                }).join('')}
            </tbody>
          </table>
        </div>
      </div>

      <!-- 3.2 Desglose por Sucursal -->
      <div class="mb-4">
        <h4 class="text-sm font-bold uppercase mb-2">3.2 Rendimiento por Sucursal / Campo de Cultivo</h4>
        <div class="table-responsive">
          <table class="table table-hover mb-0">
            <thead>
              <tr>
                <th>Sucursal / Campo</th>
                <th>Estado Cosecha</th>
                <th style="text-align: right;">Lotes Plantados</th>
                <th style="text-align: right;">Total Plantado</th>
                <th style="text-align: right;">Total Cosechado</th>
                <th style="text-align: right;">Diferencia Neta</th>
                <th style="text-align: right;">% Rendimiento</th>
              </tr>
            </thead>
            <tbody>
              ${desgSucursales.length === 0 ? '<tr><td colspan="7" class="text-center p-3 text-muted">Sin datos por sucursal en esta temporada.</td></tr>' :
                desgSucursales.map(s => {
                  const sucNom = s.nombreSucursal ?? s.NombreSucursal ?? '-';
                  const estCosecha = s.estadoCosecha ?? s.EstadoCosecha ?? 'Sin Iniciar';
                  const lotesCant = Number(s.cantidadLotes ?? s.CantidadLotes ?? 0);
                  const plant = Number(s.totalPlantado ?? s.TotalPlantado ?? 0);
                  const cosech = Number(s.totalCosechado ?? s.TotalCosechado ?? 0);
                  const dif = Number(s.diferenciaNeta ?? s.DiferenciaNeta ?? (cosech - plant));
                  const pct = Number(s.porcentajeRendimiento ?? s.PorcentajeRendimiento ?? 0);

                  const difBadge = dif >= 0 ? 'text-success font-bold' : 'text-danger font-bold';
                  const difSign = dif > 0 ? `+${dif.toLocaleString('es-AR')}` : dif.toLocaleString('es-AR');

                  return `
                    <tr>
                      <td><strong>${escapeHtml(sucNom)}</strong></td>
                      <td><span class="badge ${estCosecha === 'Cerrada' ? 'badge-success' : 'badge-warning'}">${escapeHtml(estCosecha)}</span></td>
                      <td style="text-align: right;">${lotesCant}</td>
                      <td style="text-align: right;">${plant.toLocaleString('es-AR')}</td>
                      <td style="text-align: right; font-weight: 600;">${cosech.toLocaleString('es-AR')}</td>
                      <td style="text-align: right;" class="${difBadge}">${difSign}</td>
                      <td style="text-align: right;">
                        <span class="badge ${pct >= 100 ? 'badge-success' : (pct > 0 ? 'badge-warning' : 'badge-secondary')}">
                          ${pct.toFixed(2)} %
                        </span>
                      </td>
                    </tr>
                  `;
                }).join('')}
            </tbody>
          </table>
        </div>
      </div>

      <!-- 3.3 Trazabilidad Detallada por Lote -->
      <div>
        <div class="d-flex justify-between align-center mb-2">
          <h4 class="text-sm font-bold uppercase m-0">3.3 Trazabilidad Productiva por Lote Plantado</h4>
          <span class="text-xs text-muted">${trazLotes.length} lotes auditados</span>
        </div>
        <div class="table-responsive">
          <table class="table table-hover mb-0">
            <thead>
              <tr>
                <th># Lote</th>
                <th>Campo / Sucursal</th>
                <th>Fila / Surco</th>
                <th>Titular Original</th>
                <th>Titular Vigente (Acreditación)</th>
                <th style="text-align: right;">Plantado</th>
                <th style="text-align: right;">Cosechado</th>
                <th style="text-align: right;">Diferencia</th>
              </tr>
            </thead>
            <tbody>
              ${trazLotes.length === 0 ? '<tr><td colspan="8" class="text-center p-3 text-muted">No hay lotes registrados para esta temporada.</td></tr>' :
                trazLotes.map(l => {
                  const loteId = l.loteId ?? l.LoteId;
                  const sucNom = l.nombreSucursal ?? l.NombreSucursal ?? '-';
                  const fila = l.identificacionFila ?? l.IdentificacionFila ?? '-';
                  const orig = l.nombreTitularOriginal ?? l.NombreTitularOriginal ?? `Titular #${l.titularOriginalId ?? l.TitularOriginalId ?? ''}`;
                  const vigente = l.nombreTitularVigente ?? l.NombreTitularVigente ?? `Titular #${l.titularVigenteId ?? l.TitularVigenteId ?? ''}`;
                  const transferido = (l.titularOriginalId ?? l.TitularOriginalId) !== (l.titularVigenteId ?? l.TitularVigenteId);
                  const plant = Number(l.cantidadPlantada ?? l.CantidadPlantada ?? 0);
                  const cosech = Number(l.cantidadCosechada ?? l.CantidadCosechada ?? 0);
                  const dif = cosech - plant;
                  const difBadge = dif >= 0 ? 'text-success font-bold' : 'text-danger font-bold';
                  const difSign = dif > 0 ? `+${dif.toLocaleString('es-AR')}` : dif.toLocaleString('es-AR');

                  return `
                    <tr>
                      <td><strong>#${escapeHtml(loteId)}</strong></td>
                      <td>${escapeHtml(sucNom)}</td>
                      <td>${escapeHtml(fila)}</td>
                      <td>${escapeHtml(orig)}</td>
                      <td>
                        <span class="${transferido ? 'text-primary font-bold' : ''}">
                          ${escapeHtml(vigente)} ${transferido ? '<span class="badge badge-info" style="font-size: 0.65rem;">Cesión</span>' : ''}
                        </span>
                      </td>
                      <td style="text-align: right;">${plant.toLocaleString('es-AR')}</td>
                      <td style="text-align: right; font-weight: 600;">${cosech.toLocaleString('es-AR')}</td>
                      <td style="text-align: right;" class="${difBadge}">${difSign}</td>
                    </tr>
                  `;
                }).join('')}
            </tbody>
          </table>
        </div>
      </div>
    `;
  }

  getDescripcionClase(clase) {
    switch (Number(clase)) {
      case 0: return 'Calibre >12 cm (Premium)';
      case 1: return 'Calibre 10 - 12 cm (Comercial estándar)';
      case 2: return 'Calibre 8 - 10 cm (Mediano)';
      case 3: return 'Calibre 6 - 8 cm (Desarrollo)';
      case 4: return 'Calibre <6 cm (Semillero)';
      default: return '';
    }
  }
}
