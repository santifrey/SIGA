/**
 * SIGA - views/movimientos-comerciales/movimiento-detalle-modal.js
 * Controlador del Modal de Detalle, Comprobantes y Línea de Tiempo de Auditoría
 * de Movimientos Comerciales.
 */

import { authService } from '../../services/auth-service.js';
import {
  movimientosComercialesService,
  TIPOS_MOVIMIENTO,
  CLASES_BULBO,
  ACCIONES_APROBACION
} from '../../services/movimientos-comerciales-service.js';
import { escapeHtml } from '../../utils/escape-html.js';
import {
  _parseClaseIndex,
  resolverNombreParticipante,
  resolverEstadoVisual,
  renderDetailActionButtons
} from './movimientos-helpers.js';

export class MovimientoDetalleModal {
  /**
   * @param {Object} options
   * @param {HTMLElement} options.container
   * @param {() => Array} options.getClientes
   * @param {(mov: Object) => void} options.onSetActiveMovimiento
   * @param {(tipo: string, movId: number) => void} options.onAbrirAccion
   * @param {(movId: number) => void} options.onDescargarContrato
   * @param {(movId: number) => Promise<void>} options.onCorregirMovimiento
   */
  constructor({
    container,
    getClientes,
    onSetActiveMovimiento,
    onAbrirAccion,
    onDescargarContrato,
    onCorregirMovimiento
  }) {
    this.container = container;
    this.getClientes = getClientes;
    this.onSetActiveMovimiento = onSetActiveMovimiento;
    this.onAbrirAccion = onAbrirAccion;
    this.onDescargarContrato = onDescargarContrato;
    this.onCorregirMovimiento = onCorregirMovimiento;
  }

  bindEvents() {
    this.container.querySelector('#btn-close-modal-detalle')?.addEventListener('click', () => this.close());
  }

  async open(id) {
    const modal = this.container.querySelector('#modal-detalle-movimiento');
    const title = this.container.querySelector('#modal-detalle-title');
    const body = this.container.querySelector('#modal-detalle-body');
    const footer = this.container.querySelector('#modal-detalle-footer');
    if (!modal || !title || !body) return;

    title.textContent = `Detalle de Movimiento Comercial #${id}`;
    body.innerHTML = `<div class="text-center text-muted p-4">Cargando auditoría completa...</div>`;
    if (footer) footer.innerHTML = '';
    modal.classList.add('is-active');

    try {
      const mov = await movimientosComercialesService.obtenerPorId(id);
      if (this.onSetActiveMovimiento) {
        this.onSetActiveMovimiento(mov);
      }

      const {
        tipoNum: movTipoNum,
        estadoNum: movEstadoNum,
        isFacturaAnulada,
        label: friendlyEstadoLabel,
        badgeClass: friendlyBadgeClass
      } = resolverEstadoVisual(mov, true);

      const tipoMeta = TIPOS_MOVIMIENTO[movTipoNum] || TIPOS_MOVIMIENTO[mov.tipo] || {
        label: `Tipo ${mov.tipo}`,
        badgeClass: 'badge-secondary'
      };
      const fReg = mov.fechaRegistro ? new Date(mov.fechaRegistro).toLocaleString('es-AR') : '-';

      const clientes = this.getClientes();
      const origenLabel = resolverNombreParticipante(mov.origenParticipanteId, mov.nombreOrigen, mov, clientes);
      const destinoLabel = resolverNombreParticipante(mov.destinoParticipanteId, mov.nombreDestino, mov, clientes);

      // Comprobantes: Contrato
      const fechaContratoStr = mov.contrato ? (mov.contrato.fechaGeneracion || mov.contrato.fechaEmision) : null;
      const contratoHtml = `
        <div class="card p-3 mb-3">
          <div class="d-flex justify-between align-center mb-1">
            <strong class="text-xs uppercase text-muted">Contrato de Compraventa</strong>
            ${mov.contrato
              ? '<span class="badge badge-success text-xs">Firmado / Registrado</span>'
              : (movTipoNum === 1 && mov.requiereContrato
                ? '<span class="badge badge-warning text-xs">Requerido Obligatorio</span>'
                : '<span class="text-muted text-xs">No Requerido</span>')}
          </div>
          ${mov.contrato ? `
            <div class="text-sm">
              <div><strong>Nº Contrato:</strong> ${escapeHtml(mov.contrato.numeroContrato)}</div>
              <div class="text-xs text-muted">Fecha Generación: ${fechaContratoStr ? new Date(fechaContratoStr).toLocaleString('es-AR') : '-'}</div>
              <div class="d-flex gap-sm mt-2">
                <button type="button" class="btn btn-outline btn-sm" id="btn-modal-descargar-contrato" title="Descargar contrato oficial PDF">
                  <span>📥 Descargar Contrato PDF</span>
                </button>
                <button type="button" class="btn btn-secondary btn-sm" id="btn-modal-regenerar-contrato" title="Regenerar o editar datos de contrato">
                  <span>Editar / Regenerar</span>
                </button>
              </div>
            </div>
          ` : (movTipoNum === 1 && movEstadoNum !== 3 && authService.tienePermiso('MOVCOM_APROBAR') ? `
            <p class="text-xs text-muted mb-2">${mov.requiereContrato ? 'Este cliente nuevo requiere el registro de contrato antes de la aprobación.' : 'Puede registrar un contrato de compraventa asociado a esta operación.'}</p>
            <button type="button" class="btn btn-accent btn-sm" id="btn-modal-add-contrato">
              <span>Generar Contrato</span>
            </button>
          ` : '<span class="text-xs text-muted">No se ha registrado contrato formal.</span>')}
        </div>
      `;

      // Comprobantes: Factura
      const facturaHtml = `
        <div class="card p-3 mb-3">
          <div class="d-flex justify-between align-center mb-1">
            <strong class="text-xs uppercase text-muted">Facturación Comercial</strong>
            ${isFacturaAnulada
              ? '<span class="badge badge-danger text-xs font-bold">Anulada</span>'
              : (mov.factura ? '<span class="badge badge-primary text-xs font-bold">Emitida</span>' : '<span class="text-muted text-xs">Sin Factura</span>')}
          </div>
          ${mov.factura ? `
            <div class="text-sm">
              <div><strong>Nº Factura:</strong> ${isFacturaAnulada ? `<span style="text-decoration: line-through; opacity: 0.7;">${escapeHtml(mov.factura.numeroFactura)}</span> <span class="badge badge-danger text-xs ml-1">ANULADA</span>` : `<strong>${escapeHtml(mov.factura.numeroFactura)}</strong>`}</div>
              <div class="text-xs text-muted">Fecha Emisión: ${new Date(mov.factura.fechaEmision).toLocaleString('es-AR')}</div>
              ${mov.factura.observaciones ? `<div class="text-xs text-muted">Notas / Motivo: ${escapeHtml(mov.factura.observaciones)}</div>` : ''}
              ${!isFacturaAnulada && mov.factura.archivoUrl ? `<a href="${escapeHtml(mov.factura.archivoUrl)}" target="_blank" class="text-xs text-primary d-inline-block mt-1">Ver Comprobante Digital</a>` : ''}
              ${isFacturaAnulada ? `
                <div class="alert alert-warning p-2 mt-2 mb-2 text-xs" style="background: rgba(245, 158, 11, 0.1); border-left: 3px solid var(--warning); border-radius: var(--radius-sm);">
                  ⚠️ Esta factura fue anulada automáticamente al rechazar la operación. Es necesario emitir una nueva factura comercial corregida.
                </div>
                ${(movEstadoNum === 4 || movEstadoNum === 1) && authService.tienePermiso('MOVCOM_FACTURAR') ? `
                  <button type="button" class="btn btn-primary btn-sm mt-1" id="btn-modal-add-factura">
                    <span>+ Emitir Nueva Factura</span>
                  </button>
                ` : ''}
              ` : ''}
            </div>
          ` : ((movEstadoNum === 1 || movEstadoNum === 4) && movTipoNum !== 3 && authService.tienePermiso('MOVCOM_FACTURAR') ? `
            <p class="text-xs text-muted mb-2">${movEstadoNum === 4 ? 'La operación fue rechazada. Puede emitir una nueva factura comercial corregida.' : 'La operación está aprobada y lista para su facturación contable.'}</p>
            <button type="button" class="btn btn-primary btn-sm" id="btn-modal-add-factura">
              <span>+ Emitir Factura</span>
            </button>
          ` : '<span class="text-xs text-muted">No se ha emitido factura.</span>')}
        </div>
      `;

      // Timeline de Aprobaciones
      const historial = Array.isArray(mov.historialAprobaciones) ? mov.historialAprobaciones : [];
      const timelineHtml = `
        <div class="card p-3 mb-3">
          <strong class="text-xs uppercase text-muted d-block mb-3">Historial de Aprobaciones y Estados</strong>
          ${historial.length === 0 ? '<span class="text-xs text-muted">Sin transiciones registradas.</span>' : `
            <div class="timeline" style="border-left: 2px solid var(--border); margin-left: 0.75rem; padding-left: 1rem;">
              ${historial.map(h => {
                const fHist = h.fecha ? new Date(h.fecha).toLocaleString('es-AR') : '';
                const accionNombre = h.nombreAccion || ACCIONES_APROBACION[h.accion] || `Acción ${h.accion}`;
                return `
                  <div class="mb-3 position-relative">
                    <div style="font-weight: 700; font-size: 0.85rem; color: var(--primary);">${escapeHtml(accionNombre)}</div>
                    <div class="text-xs text-muted">
                      Por: <strong>${escapeHtml(h.nombreUsuarioAprobador || 'Usuario #' + h.usuarioAprobadorId)}</strong> — ${escapeHtml(fHist)}
                    </div>
                    ${h.observaciones ? `
                      <div class="card p-2 mt-1 text-xs" style="background: var(--surface-alt); border-left: 3px solid var(--border-color);">
                        ${escapeHtml(h.observaciones)}
                      </div>
                    ` : ''}
                  </div>
                `;
              }).join('')}
            </div>
          `}
        </div>
      `;

      body.innerHTML = `
        <div class="grid grid-cols-1 md:grid-cols-2 gap-md mb-3" style="display: grid; grid-template-columns: repeat(auto-fit, minmax(240px, 1fr)); gap: var(--spacing-md);">
          <div class="card p-3">
            <span class="text-xs text-muted uppercase font-bold d-block mb-1">Identificación</span>
            <div class="d-flex align-center gap-xs mb-2">
              <span class="font-bold text-lg text-main">#${mov.id}</span>
              <span class="badge ${tipoMeta.badgeClass} text-xs font-bold">${escapeHtml(tipoMeta.label)}</span>
              <span class="badge ${friendlyBadgeClass} text-xs font-bold">${escapeHtml(friendlyEstadoLabel)}</span>
            </div>
            <div class="text-xs text-muted mb-1">Registrado el: <strong>${escapeHtml(fReg)}</strong></div>
            <div class="text-xs text-muted">Usuario Registro: <strong>${escapeHtml(mov.nombreUsuarioRegistro || '#' + mov.usuarioRegistroId)}</strong></div>
          </div>

          <div class="card p-3">
            <span class="text-xs text-muted uppercase font-bold d-block mb-1">Participantes</span>
            <div class="text-sm mb-1"><strong>Emisor (Origen):</strong> ${escapeHtml(origenLabel)}</div>
            <div class="text-sm"><strong>Receptor (Destino):</strong> ${escapeHtml(destinoLabel)}</div>
          </div>
        </div>

        <!-- Desglose de Bulbos -->
        <div class="card p-3 mb-3">
          <strong class="text-xs uppercase text-muted d-block mb-2">Ítems Comercializados</strong>
          <div class="table-responsive">
            <table class="table table-bordered mb-0">
              <thead style="background: var(--surface-alt);">
                <tr>
                  <th>Clase</th>
                  <th style="text-align: right;">Cantidad</th>
                  <th style="text-align: right;">Precio Unitario</th>
                  <th style="text-align: right;">Subtotal</th>
                </tr>
              </thead>
              <tbody>
                ${(mov.detalles || []).map(d => {
                  const meta = CLASES_BULBO.find(c => c.id === _parseClaseIndex(d.claseBulbo)) || { label: `Clase ${d.claseBulbo}` };
                  return `
                    <tr>
                      <td>${escapeHtml(meta.label)}</td>
                      <td style="text-align: right; font-weight: 600;">${(Number(d.cantidad) || 0).toLocaleString('es-AR')}</td>
                      <td style="text-align: right;">$ ${(Number(d.precioUnitarioPactado) || 0).toFixed(2)}</td>
                      <td style="text-align: right; font-weight: 700;">$ ${(Number(d.subtotal) || 0).toFixed(2)}</td>
                    </tr>
                  `;
                }).join('')}
              </tbody>
              <tfoot style="background: var(--surface-alt); font-weight: 700;">
                <tr>
                  <td>TOTAL BULBOS:</td>
                  <td style="text-align: right; color: var(--primary);">${(Number(mov.totalBulbos) || 0).toLocaleString('es-AR')}</td>
                  <td style="text-align: right;">TOTAL:</td>
                  <td style="text-align: right; color: var(--success); font-size: 1.1rem;">
                    $ ${(Number(mov.montoTotal) || 0).toFixed(2)}
                  </td>
                </tr>
              </tfoot>
            </table>
          </div>
          ${movTipoNum === 2 ? `
            <div class="mt-2 text-xs text-muted text-right">
              Bruto: $ ${(Number(mov.montoBruto) || 0).toFixed(2)} | Comisión (${mov.porcentajeComision}%): -$ ${(Number(mov.montoComision) || 0).toFixed(2)} | Neto: $ ${(Number(mov.montoNeto) || 0).toFixed(2)}
            </div>
          ` : ''}
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-md" style="display: grid; grid-template-columns: repeat(auto-fit, minmax(240px, 1fr)); gap: var(--spacing-md);">
          <div>
            ${contratoHtml}
            ${facturaHtml}
          </div>
          <div>
            ${timelineHtml}
          </div>
        </div>

        ${mov.observaciones ? `
          <div class="card p-3 mt-3">
            <strong class="text-xs uppercase text-muted d-block mb-1">Observaciones Generales</strong>
            <p class="text-sm m-0 text-muted">${escapeHtml(mov.observaciones)}</p>
          </div>
        ` : ''}
      `;

      // Botones del cuerpo del detalle
      body.querySelector('#btn-modal-add-contrato')?.addEventListener('click', () => {
        this.close();
        this.onAbrirAccion('contrato', mov.id);
      });

      body.querySelector('#btn-modal-regenerar-contrato')?.addEventListener('click', () => {
        this.close();
        this.onAbrirAccion('contrato', mov.id);
      });

      body.querySelector('#btn-modal-descargar-contrato')?.addEventListener('click', () => {
        this.onDescargarContrato(mov.id);
      });

      body.querySelector('#btn-modal-add-factura')?.addEventListener('click', () => {
        this.close();
        this.onAbrirAccion('facturar', mov.id);
      });

      // Footer con acciones de workflow
      let modalFooter = this.container.querySelector('#modal-detalle-footer');
      if (!modalFooter) {
        modalFooter = document.createElement('div');
        modalFooter.className = 'modal-footer';
        modalFooter.id = 'modal-detalle-footer';
        modal.querySelector('.modal-dialog')?.appendChild(modalFooter);
      }

      const actionBtns = renderDetailActionButtons(mov);
      modalFooter.innerHTML = `
        <div style="width: 100%; display: flex; justify-content: space-between; align-items: center; gap: var(--spacing-sm); flex-wrap: wrap;">
          <div>
            <button type="button" class="btn btn-secondary btn-sm" id="btn-detalle-cerrar">Cerrar</button>
          </div>
          <div class="d-flex align-center gap-sm flex-wrap">
            ${actionBtns.join('')}
          </div>
        </div>
      `;

      modalFooter.querySelector('#btn-detalle-cerrar')?.addEventListener('click', () => this.close());

      modalFooter.querySelector('#btn-detalle-contrato')?.addEventListener('click', () => {
        this.close();
        this.onAbrirAccion('contrato', mov.id);
      });

      modalFooter.querySelector('#btn-detalle-ver-contrato')?.addEventListener('click', () => {
        this.close();
        this.onDescargarContrato(mov.id);
      });

      modalFooter.querySelector('#btn-detalle-aprobar')?.addEventListener('click', () => {
        this.close();
        this.onAbrirAccion('aprobar', mov.id);
      });

      modalFooter.querySelector('#btn-detalle-facturar')?.addEventListener('click', () => {
        this.close();
        this.onAbrirAccion('facturar', mov.id);
      });

      modalFooter.querySelector('#btn-detalle-finalizar')?.addEventListener('click', () => {
        this.close();
        this.onAbrirAccion('finalizar', mov.id);
      });

      modalFooter.querySelector('#btn-detalle-rechazar')?.addEventListener('click', () => {
        this.close();
        this.onAbrirAccion('rechazar', mov.id);
      });

      modalFooter.querySelector('#btn-detalle-corregir')?.addEventListener('click', async () => {
        this.close();
        await this.onCorregirMovimiento(mov.id);
      });

    } catch (err) {
      console.error('[MovimientoDetalleModal] Error al cargar detalle:', err);
      body.innerHTML = `<div class="text-center text-danger p-4">${escapeHtml(err.message || 'Error al obtener detalle')}</div>`;
      const modalFooter = this.container.querySelector('#modal-detalle-footer');
      if (modalFooter) {
        modalFooter.innerHTML = `
          <div style="width: 100%; display: flex; justify-content: flex-end;">
            <button type="button" class="btn btn-secondary btn-sm" id="btn-detalle-cerrar">Cerrar</button>
          </div>
        `;
        modalFooter.querySelector('#btn-detalle-cerrar')?.addEventListener('click', () => this.close());
      }
    }
  }

  close() {
    const modal = this.container.querySelector('#modal-detalle-movimiento');
    modal?.classList.remove('is-active');
  }
}
