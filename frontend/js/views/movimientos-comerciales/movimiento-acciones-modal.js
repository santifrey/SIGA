/**
 * SIGA - views/movimientos-comerciales/movimiento-acciones-modal.js
 * Controlador del Modal de Acciones del Circuito de Movimientos Comerciales:
 * - Aprobación administrativa
 * - Generación y descarga de Contrato Oficial PDF (con Anexo de Copropietarios opcional)
 * - Emisión y reemplazo de Factura Comercial
 * - Finalización e impacto de inventario físico
 * - Rechazo / Observación con motivo obligatorio
 */

import { authService } from '../../services/auth-service.js';
import { clientesService } from '../../services/clientes-service.js';
import { sucursalesService } from '../../services/sucursales-service.js';
import { contratoGeneratorService } from '../../services/contrato-generator-service.js';
import {
  movimientosComercialesService,
  TIPOS_MOVIMIENTO,
  _parseTipoIndex,
  _parseEstadoIndex
} from '../../services/movimientos-comerciales-service.js';
import { getProhibitionIcon } from '../../utils/icons.js';
import { toast } from '../../utils/toast.js';
import { escapeHtml } from '../../utils/escape-html.js';
import { _parseClaseIndex, esFacturaAnulada } from './movimientos-helpers.js';

export class MovimientoAccionesModal {
  /**
   * @param {Object} options
   * @param {HTMLElement} options.container
   * @param {() => Array} options.getMovimientos
   * @param {() => Array} options.getClientes
   * @param {() => Object|null} options.getActiveMovimiento
   * @param {(mov: Object|null) => void} options.setActiveMovimiento
   * @param {() => Promise<void>} options.onCompleted
   */
  constructor({
    container,
    getMovimientos,
    getClientes,
    getActiveMovimiento,
    setActiveMovimiento,
    onCompleted
  }) {
    this.container = container;
    this.getMovimientos = getMovimientos;
    this.getClientes = getClientes;
    this.getActiveMovimiento = getActiveMovimiento;
    this.setActiveMovimiento = setActiveMovimiento;
    this.onCompleted = onCompleted;
  }

  bindEvents() {
    this.container.querySelector('#btn-close-modal-accion')?.addEventListener('click', () => this.close());
  }

  async open(tipo, movimientoId) {
    const movimientos = this.getMovimientos();
    const activeMov = this.getActiveMovimiento();
    let mov = movimientos.find(m => Number(m.id) === Number(movimientoId));
    if (!mov && activeMov && Number(activeMov.id) === Number(movimientoId)) {
      mov = activeMov;
    }
    if (!mov) return;
    this.setActiveMovimiento(mov);

    const modal = this.container.querySelector('#modal-accion-circuito');
    const title = this.container.querySelector('#modal-accion-title');
    const body = this.container.querySelector('#modal-accion-body');
    if (!modal || !title || !body) return;

    if (tipo === 'aprobar') {
      const movTipoNum = _parseTipoIndex(mov.tipo ?? mov.nombreTipo);
      title.textContent = `Aprobar Operación Comercial #${mov.id}`;
      body.innerHTML = `
        <p class="text-sm text-muted mb-3">
          Confirma la aprobación administrativa del movimiento de <strong>${escapeHtml(TIPOS_MOVIMIENTO[movTipoNum]?.label || TIPOS_MOVIMIENTO[mov.tipo]?.label || '')}</strong> por un total de <strong>${(Number(mov.totalBulbos) || 0).toLocaleString('es-AR')} bulbos</strong>.
        </p>
        ${movTipoNum === 1 && !mov.contrato ? `
          <div class="alert alert-warning d-flex justify-between align-center gap-sm p-3 mb-3 text-xs" style="background: rgba(245, 158, 11, 0.1); border-left: 4px solid var(--warning); border-radius: var(--radius-sm);">
            <div>
              <strong>Contrato de Compraventa:</strong> ${mov.requiereContrato ? 'Este cliente es nuevo y requiere registrar previamente el contrato antes de aprobar.' : 'Si el cliente es nuevo, recuerde registrar el contrato previo antes de aprobar.'}
            </div>
            <button type="button" class="btn btn-accent btn-sm" id="btn-aprobar-ir-contrato" style="flex-shrink: 0;">
              Generar Contrato
            </button>
          </div>
        ` : ''}
        <form id="form-accion-aprobar">
          <div class="form-group mb-3">
            <label for="input-obs-accion" class="form-label text-xs font-bold uppercase text-muted">Observaciones de Aprobación (Opcional)</label>
            <input type="text" id="input-obs-accion" class="form-control" placeholder="ej. Aprobado conforme a lista de precios oficial..." />
          </div>
          <div class="d-flex justify-end gap-sm">
            <button type="button" class="btn btn-secondary btn-sm" id="btn-cancel-accion">Cancelar</button>
            <button type="submit" class="btn btn-primary btn-sm">Confirmar Aprobación</button>
          </div>
        </form>
      `;
      body.querySelector('#btn-aprobar-ir-contrato')?.addEventListener('click', () => {
        this.open('contrato', mov.id);
      });

    } else if (tipo === 'contrato') {
      const dialog = modal.querySelector('.modal-dialog');
      dialog?.classList.add('modal-lg');

      title.textContent = `Generar Contrato para Cliente - Operación #${mov.id}`;
      body.innerHTML = `
        <div class="text-center p-4">
          <div class="btn-spinner" style="position: static; border-top-color: var(--primary); width: 28px; height: 28px; margin: 0 auto 12px;"></div>
          <p class="text-sm text-muted">Cargando datos del cliente, sucursales y plantilla de contrato...</p>
        </div>
      `;
      modal.classList.add('is-active');

      let clienteData = null;
      if (mov.clienteId) {
        try {
          clienteData = await clientesService.obtenerPorId(mov.clienteId);
        } catch (e) {
          console.warn('[MovimientoAccionesModal] Error al obtener cliente:', e);
        }
      }
      const clientes = this.getClientes();
      if (!clienteData && clientes) {
        clienteData = clientes.find(c => Number(c.id) === Number(mov.clienteId));
      }

      let sucursales = [];
      try {
        const sucRes = await sucursalesService.listar({ estado: 'activas' });
        sucursales = (Array.isArray(sucRes) ? sucRes : (sucRes?.items || [])).filter(s => s.activa !== false && String(s.estado || '').toLowerCase() !== 'inactiva');
      } catch (e) {
        console.warn('[MovimientoAccionesModal] Error al listar sucursales:', e);
      }

      let cormillos = 0;
      let n1 = 0;
      let n2 = 0;
      let n3 = 0;
      let n4 = 0;

      if (Array.isArray(mov.detalles)) {
        mov.detalles.forEach(d => {
          const cl = _parseClaseIndex(d.claseBulbo ?? d.clase);
          const cant = Number(d.cantidad) || 0;
          if (cl === 0) cormillos += cant;
          else if (cl === 1) n1 += cant;
          else if (cl === 2) n2 += cant;
          else if (cl === 3) n3 += cant;
          else if (cl === 4) n4 += cant;
          else if (cl === 5) {
            const tercio = Math.floor(cant / 3);
            n1 += tercio;
            n2 += tercio;
            n3 += (cant - 2 * tercio);
          }
        });
      }

      const anioActual = new Date().getFullYear();
      const numSugerido = mov.contrato?.numeroContrato || `CTR-${anioActual}-${String(mov.id).padStart(4, '0')}`;
      const currentUser = authService.getUsuarioActual();
      const referenteSugerido = currentUser ? (currentUser.nombreCompleto || currentUser.nombre || 'Federico Carlos Paak') : 'Federico Carlos Paak';

      const cliNombre = clienteData ? (clienteData.nombreCompleto || `${clienteData.nombre || ''} ${clienteData.apellido || ''}`).trim() : (mov.nombreCliente || '');
      const cliDoc = clienteData?.documento || clienteData?.dni || clienteData?.cuit || '';
      const cliTel = clienteData?.telefono || '';
      const cliEmail = clienteData?.email || '';
      const cliDir = clienteData?.direccion || '';
      const cliFechaNac = clienteData?.fechaNacimiento || '';
      const cliProf = clienteData?.profesion || 'Productor / Inversor';
      const cliEstCivil = clienteData?.estadoCivil || 'Soltero/a';

      body.innerHTML = `
        <div class="alert alert-info d-flex align-center gap-sm p-3 mb-3 text-xs" style="background: rgba(74, 21, 75, 0.08); border-left: 4px solid var(--primary); border-radius: var(--radius-sm);">
          <span style="font-size: 1.25rem;">📄</span>
          <div>
            <strong>Generador Oficial de Contratos:</strong> Complete los campos requeridos y haga clic en <strong>Generar Contrato</strong> para obtener el archivo PDF oficial basado en el modelo legal <em>MODELO DE CONTRATO PARA CLIENTES</em> con los datos del cliente, sucursal, fecha y el anexo correspondiente.
          </div>
        </div>

        <form id="form-accion-contrato">
          <!-- SECCIÓN 1: DATOS DEL CONTRATO, SUCURSAL Y FECHA -->
          <div class="card p-3 mb-3" style="background: var(--bg-surface, #f9fafb); border: 1px solid var(--border-color, #e5e7eb);">
            <h6 class="text-xs font-bold uppercase text-primary mb-2">1. Datos del Contrato, Sucursal y Período</h6>
            <div class="form-row two-cols mb-2">
              <div class="form-group">
                <label for="input-num-contrato" class="form-label text-xs font-bold uppercase text-muted">Número de Contrato *</label>
                <input type="text" id="input-num-contrato" class="form-control" value="${escapeHtml(numSugerido)}" required />
              </div>
              <div class="form-group">
                <label for="input-anio-inicio" class="form-label text-xs font-bold uppercase text-muted">Año de Inicio *</label>
                <input type="number" id="input-anio-inicio" class="form-control" value="${anioActual}" min="2020" max="2050" required />
              </div>
            </div>
            <div class="form-row two-cols">
              <div class="form-group">
                <label for="select-sucursal-contrato" class="form-label text-xs font-bold uppercase text-muted">Sucursal / Predio</label>
                <select id="select-sucursal-contrato" class="form-control">
                  <option value="Central - Villa General Belgrano">Central - Villa General Belgrano</option>
                  ${sucursales.map(s => `<option value="${escapeHtml(s.nombre || s.codigo)}">${escapeHtml(s.nombre || s.codigo)}</option>`).join('')}
                </select>
              </div>
              <div class="form-group">
                <label for="input-referente-contrato" class="form-label text-xs font-bold uppercase text-muted">Referente Prestador</label>
                <input type="text" id="input-referente-contrato" class="form-control" value="${escapeHtml(referenteSugerido)}" />
              </div>
            </div>
          </div>

          <!-- SECCIÓN 2: DATOS DEL CLIENTE (COMITENTE) -->
          <div class="card p-3 mb-3" style="background: var(--bg-surface, #f9fafb); border: 1px solid var(--border-color, #e5e7eb);">
            <h6 class="text-xs font-bold uppercase text-primary mb-2">2. Datos del Cliente (COMITENTE)</h6>
            <div class="form-row two-cols mb-2">
              <div class="form-group">
                <label for="input-cliente-nombre" class="form-label text-xs font-bold uppercase text-muted">Nombre y Apellido *</label>
                <input type="text" id="input-cliente-nombre" class="form-control" value="${escapeHtml(cliNombre)}" required />
              </div>
              <div class="form-group">
                <label for="input-cliente-doc" class="form-label text-xs font-bold uppercase text-muted">D.N.I. / C.U.I.T. / C.U.I.L. *</label>
                <input type="text" id="input-cliente-doc" class="form-control" value="${escapeHtml(cliDoc)}" required />
              </div>
            </div>
            <div class="form-row two-cols mb-2">
              <div class="form-group">
                <label for="input-cliente-tel" class="form-label text-xs font-bold uppercase text-muted">Teléfono</label>
                <input type="text" id="input-cliente-tel" class="form-control" value="${escapeHtml(cliTel)}" />
              </div>
              <div class="form-group">
                <label for="input-cliente-email" class="form-label text-xs font-bold uppercase text-muted">Domicilio Electrónico (Email)</label>
                <input type="email" id="input-cliente-email" class="form-control" value="${escapeHtml(cliEmail)}" />
              </div>
            </div>
            <div class="form-group mb-2">
              <label for="input-cliente-dir" class="form-label text-xs font-bold uppercase text-muted">Dirección / Domicilio Real</label>
              <input type="text" id="input-cliente-dir" class="form-control" value="${escapeHtml(cliDir)}" placeholder="ej. Ruta Prov. S-210, Villa General Belgrano" />
            </div>
            <div class="form-row three-cols">
              <div class="form-group">
                <label for="input-cliente-fecnac" class="form-label text-xs font-bold uppercase text-muted">Nacido el día</label>
                <input type="text" id="input-cliente-fecnac" class="form-control" value="${escapeHtml(cliFechaNac)}" placeholder="ej. 15/05/1985" />
              </div>
              <div class="form-group">
                <label for="input-cliente-prof" class="form-label text-xs font-bold uppercase text-muted">Profesión</label>
                <input type="text" id="input-cliente-prof" class="form-control" value="${escapeHtml(cliProf)}" />
              </div>
              <div class="form-group">
                <label for="input-cliente-estcivil" class="form-label text-xs font-bold uppercase text-muted">Estado Civil</label>
                <input type="text" id="input-cliente-estcivil" class="form-control" value="${escapeHtml(cliEstCivil)}" />
              </div>
            </div>
          </div>

          <!-- SECCIÓN 3: CLÁUSULA PRIMERA - CANTIDADES DE CORMOS -->
          <div class="card p-3 mb-3" style="background: var(--bg-surface, #f9fafb); border: 1px solid var(--border-color, #e5e7eb);">
            <div class="d-flex justify-between align-center mb-2">
              <h6 class="text-xs font-bold uppercase text-primary">3. Cantidades de Cormos (Cláusula Primera)</h6>
              <span class="text-xs text-muted">Autocompletado desde los ítems de la operación</span>
            </div>
            <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(90px, 1fr)); gap: 8px;">
              <div class="form-group">
                <label for="input-cormos-cormillos" class="form-label text-xs font-bold uppercase text-muted">Cormillos</label>
                <input type="number" id="input-cormos-cormillos" class="form-control font-mono" value="${cormillos}" min="0" />
              </div>
              <div class="form-group">
                <label for="input-cormos-n1" class="form-label text-xs font-bold uppercase text-muted">Calibre 1</label>
                <input type="number" id="input-cormos-n1" class="form-control font-mono" value="${n1}" min="0" />
              </div>
              <div class="form-group">
                <label for="input-cormos-n2" class="form-label text-xs font-bold uppercase text-muted">Calibre 2</label>
                <input type="number" id="input-cormos-n2" class="form-control font-mono" value="${n2}" min="0" />
              </div>
              <div class="form-group">
                <label for="input-cormos-n3" class="form-label text-xs font-bold uppercase text-muted">Calibre 3</label>
                <input type="number" id="input-cormos-n3" class="form-control font-mono" value="${n3}" min="0" />
              </div>
              <div class="form-group">
                <label for="input-cormos-n4" class="form-label text-xs font-bold uppercase text-muted">Calibre 4</label>
                <input type="number" id="input-cormos-n4" class="form-control font-mono" value="${n4}" min="0" />
              </div>
            </div>
          </div>

          <!-- SECCIÓN 4: CHECKBOX PARA ANEXO DE COPROPIETARIOS (ÚLTIMA PÁGINA) -->
          <div class="card p-3 mb-3" style="background: var(--bg-surface, #f9fafb); border: 2px solid var(--border-color, #e5e7eb); border-radius: var(--radius-sm);">
            <label class="form-check-label d-flex align-center gap-sm mb-1" style="font-weight: 600; cursor: pointer; font-size: 0.95rem;">
              <input type="checkbox" id="chk-anexo-copropietarios" class="form-check-input" style="width: 18px; height: 18px; cursor: pointer;" />
              <span>¿Necesita el anexo de copropietarios? (Última página)</span>
            </label>
            <div class="text-xs text-muted" style="margin-left: 26px;">
              Si no se marca, el contrato se emitirá únicamente con sus 3 páginas principales. Si se marca, se anexará la 4ª página oficial para registro y firma de copropietarios.
            </div>

            <div id="section-copropietarios" class="mt-3 pt-3" style="display: none; border-top: 1px dashed var(--border-color, #d1d5db);">
              <h6 class="text-xs font-bold uppercase text-primary mb-2">Datos de Copropietarios (Página 4 - Opcionales)</h6>
              <div class="form-row two-cols mb-2">
                <input type="text" id="coprop-1-nombre" class="form-control" placeholder="Copropietario 1 - Nombre y Apellido" />
                <input type="text" id="coprop-1-dni" class="form-control" placeholder="Copropietario 1 - DNI / CUIT" />
              </div>
              <div class="form-row two-cols mb-2">
                <input type="text" id="coprop-2-nombre" class="form-control" placeholder="Copropietario 2 - Nombre y Apellido" />
                <input type="text" id="coprop-2-dni" class="form-control" placeholder="Copropietario 2 - DNI / CUIT" />
              </div>
              <div class="form-row two-cols">
                <input type="text" id="coprop-3-nombre" class="form-control" placeholder="Copropietario 3 - Nombre y Apellido" />
                <input type="text" id="coprop-3-dni" class="form-control" placeholder="Copropietario 3 - DNI / CUIT" />
              </div>
            </div>
          </div>

          <!-- BOTONES DE ACCIÓN -->
          <div class="d-flex justify-between align-center gap-sm pt-2">
            <button type="button" class="btn btn-secondary btn-sm" id="btn-cancel-accion">Cancelar</button>
            <div class="d-flex gap-sm">
              <button type="button" class="btn btn-accent btn-sm" id="btn-descargar-borrador-pdf" title="Genera el PDF y lo descarga de inmediato para revisión">
                <span>📥 Descargar PDF</span>
              </button>
              <button type="submit" class="btn btn-primary btn-sm" id="btn-submit-contrato">
                <span>Generar Contrato</span>
              </button>
            </div>
          </div>
        </form>
      `;

      const chkAnexo = body.querySelector('#chk-anexo-copropietarios');
      const secCoprop = body.querySelector('#section-copropietarios');
      if (chkAnexo && secCoprop) {
        chkAnexo.addEventListener('change', () => {
          secCoprop.style.display = chkAnexo.checked ? 'block' : 'none';
        });
      }

      body.querySelector('#btn-descargar-borrador-pdf')?.addEventListener('click', async () => {
        const payload = this._getContratoFormData(body);
        if (!payload.numeroContrato) {
          toast.warning('El número de contrato es obligatorio.');
          return;
        }
        try {
          toast.info('Generando documento PDF desde la plantilla...');
          const res = await contratoGeneratorService.generarContrato(payload);
          contratoGeneratorService.guardarContratoCache(mov.id, res.dataUrl, res.filename);
          contratoGeneratorService.descargarPdf(res.blob, res.filename);
          toast.success(`PDF generado exitosamente (${res.pageCount} páginas). Descarga iniciada.`);
        } catch (err) {
          toast.error('Error al generar PDF: ' + (err.message || 'Error'));
        }
      });

    } else if (tipo === 'facturar') {
      const isReemplazo = esFacturaAnulada(mov) || (mov.factura && (mov.estado === 4 || mov.estado === 'Rechazado'));
      title.textContent = isReemplazo ? `Emitir Nueva Factura Comercial (Reemplazo) - Movimiento #${mov.id}` : `Emitir Factura Comercial - Movimiento #${mov.id}`;
      body.innerHTML = `
        ${isReemplazo ? `
          <div class="alert alert-info d-flex align-center gap-sm p-3 mb-3 text-xs" style="background: rgba(30, 64, 175, 0.08); border-left: 4px solid var(--primary); border-radius: var(--radius-sm);">
            <span style="font-size: 1.25rem;">ℹ️</span>
            <div>
              <strong>Re-emisión de Factura:</strong> La factura anterior fue anulada tras el rechazo. Al emitir esta nueva factura, la operación avanzará a <strong>Pendiente Aprobación Factura</strong> para su revisión final.
            </div>
          </div>
        ` : ''}
        <form id="form-accion-factura">
          <div class="form-group mb-3">
            <label for="input-num-factura" class="form-label text-xs font-bold uppercase text-muted">Número de Factura *</label>
            <input type="text" id="input-num-factura" class="form-control" placeholder="ej. FC-A-0001-00004291" required />
          </div>
          <div class="form-group mb-3">
            <label for="input-url-factura" class="form-label text-xs font-bold uppercase text-muted">URL Comprobante Digital (Opcional)</label>
            <input type="text" id="input-url-factura" class="form-control" placeholder="ej. https://afip.gob.ar/factura-00004291.pdf" />
          </div>
          <div class="form-group mb-3">
            <label for="input-obs-factura" class="form-label text-xs font-bold uppercase text-muted">Observaciones Contables</label>
            <input type="text" id="input-obs-factura" class="form-control" placeholder="ej. Condición de pago 30 días..." />
          </div>
          <div class="d-flex justify-end gap-sm">
            <button type="button" class="btn btn-secondary btn-sm" id="btn-cancel-accion">Cancelar</button>
            <button type="submit" class="btn btn-primary btn-sm">${isReemplazo ? 'Emitir Nueva Factura' : 'Emitir Factura'}</button>
          </div>
        </form>
      `;

    } else if (tipo === 'finalizar') {
      title.textContent = `Finalizar Operación y Entregar Stock - Movimiento #${mov.id}`;
      body.innerHTML = `
        <div class="alert alert-success d-flex align-start gap-sm p-3 mb-3" style="background: rgba(34, 197, 94, 0.1); border-left: 4px solid var(--success); border-radius: var(--radius-sm);">
          <span style="font-size: 1.2rem;">📦</span>
          <div class="text-sm">
            <strong>Impacto Físico en Inventario:</strong> Al confirmar este paso, los <strong>${(Number(mov.totalBulbos) || 0).toLocaleString('es-AR')} bulbos</strong> (incluyendo la descomposición física de calibres MIX) serán descontados del titular de origen e incorporados al inventario de destino de forma irreversible.
          </div>
        </div>
        <form id="form-accion-finalizar">
          <div class="form-group mb-3">
            <label for="input-obs-accion" class="form-label text-xs font-bold uppercase text-muted">Observaciones de Entrega Final</label>
            <input type="text" id="input-obs-accion" class="form-control" placeholder="ej. Entrega presencial en depósito central..." />
          </div>
          <div class="d-flex justify-end gap-sm">
            <button type="button" class="btn btn-secondary btn-sm" id="btn-cancel-accion">Cancelar</button>
            <button type="submit" class="btn btn-success btn-sm">Finalizar e Impactar Inventario</button>
          </div>
        </form>
      `;

    } else if (tipo === 'rechazar') {
      const movEstadoNum = _parseEstadoIndex(mov.estado ?? mov.nombreEstado);
      const isRechazoFactura = (movEstadoNum === 2 || mov.factura);
      title.textContent = isRechazoFactura ? `Rechazar Factura / Observar Operación #${mov.id}` : `Rechazar u Observar Operación #${mov.id}`;
      body.innerHTML = `
        <div class="d-flex align-center gap-sm mb-3">
          ${getProhibitionIcon({ size: 'md', title: 'Rechazo' })}
          <span class="text-sm text-danger font-bold">
            Indique obligatoriamente el motivo formal del rechazo u observación.
          </span>
        </div>
        ${mov.factura ? `
          <div class="alert alert-warning p-2 mb-3 text-xs" style="background: rgba(239, 68, 68, 0.08); border-left: 4px solid var(--danger); border-radius: var(--radius-sm);">
            ⚠️ <strong>Atención:</strong> Al confirmar el rechazo, la factura actual <strong>#${escapeHtml(mov.factura.numeroFactura || '')}</strong> quedará marcada automáticamente como <strong>ANULADA</strong>. La operación volverá a estado <em>Rechazado</em> permitiendo emitir una nueva factura comercial corregida o editar los datos de la operación.
          </div>
        ` : ''}
        <form id="form-accion-rechazar">
          <div class="form-group mb-3">
            <label for="input-motivo-rechazo" class="form-label text-xs font-bold uppercase text-muted">Motivo del Rechazo *</label>
            <textarea id="input-motivo-rechazo" class="form-control" rows="3" placeholder="Indique claramente la razón por la que se rechaza la operación o factura..." required></textarea>
          </div>
          <div class="d-flex justify-end gap-sm">
            <button type="button" class="btn btn-secondary btn-sm" id="btn-cancel-accion">Cancelar</button>
            <button type="submit" class="btn btn-danger btn-sm">Confirmar Rechazo</button>
          </div>
        </form>
      `;
    }

    body.querySelector('#btn-cancel-accion')?.addEventListener('click', () => this.close());
    this._bindFormAccionSubmit(tipo, mov.id);
    modal.classList.add('is-active');
  }

  close() {
    const modal = this.container.querySelector('#modal-accion-circuito');
    modal?.classList.remove('is-active');
    const dialog = modal?.querySelector('.modal-dialog');
    dialog?.classList.remove('modal-lg');
    this.setActiveMovimiento(null);
  }

  async descargarOVerContrato(movId) {
    const movimientos = this.getMovimientos();
    let mov = movimientos.find(m => Number(m.id) === Number(movId)) || this.getActiveMovimiento();
    if (!mov) {
      try {
        mov = await movimientosComercialesService.obtenerPorId(movId);
      } catch {}
    }
    if (!mov) return;

    const cached = contratoGeneratorService.obtenerContratoCache(movId);
    if (cached && cached.dataUrl) {
      toast.info('Descargando contrato generado previamente...');
      const cleanNum = (mov.contrato?.numeroContrato || 'CTR').replace(/[^a-zA-Z0-9_-]/g, '_');
      const filename = cached.filename || `Contrato_${cleanNum}.pdf`;
      try {
        const res = await fetch(cached.dataUrl);
        const blob = await res.blob();
        contratoGeneratorService.descargarPdf(blob, filename);
        return;
      } catch (e) {
        console.warn('No se pudo usar caché, abriendo modal:', e);
      }
    }

    this.open('contrato', movId);
  }

  _getContratoFormData(body) {
    const chkAnexo = body.querySelector('#chk-anexo-copropietarios')?.checked ?? false;
    return {
      numeroContrato: body.querySelector('#input-num-contrato')?.value.trim() || '',
      anioInicio: body.querySelector('#input-anio-inicio')?.value.trim() || String(new Date().getFullYear()),
      sucursal: body.querySelector('#select-sucursal-contrato')?.value || 'Central - Villa General Belgrano',
      referente: body.querySelector('#input-referente-contrato')?.value.trim() || 'Federico Carlos Paak',
      cliente: {
        nombreCompleto: body.querySelector('#input-cliente-nombre')?.value.trim() || '',
        documento: body.querySelector('#input-cliente-doc')?.value.trim() || '',
        telefono: body.querySelector('#input-cliente-tel')?.value.trim() || '',
        email: body.querySelector('#input-cliente-email')?.value.trim() || '',
        direccion: body.querySelector('#input-cliente-dir')?.value.trim() || '',
        nacidoElDia: body.querySelector('#input-cliente-fecnac')?.value.trim() || '',
        profesion: body.querySelector('#input-cliente-prof')?.value.trim() || '',
        estadoCivil: body.querySelector('#input-cliente-estcivil')?.value.trim() || '',
      },
      cormos: {
        cormillos: body.querySelector('#input-cormos-cormillos')?.value || '0',
        n1: body.querySelector('#input-cormos-n1')?.value || '0',
        n2: body.querySelector('#input-cormos-n2')?.value || '0',
        n3: body.querySelector('#input-cormos-n3')?.value || '0',
        n4: body.querySelector('#input-cormos-n4')?.value || '0',
      },
      incluirAnexoCopropietarios: chkAnexo,
      copropietarios: [
        {
          nombre: body.querySelector('#coprop-1-nombre')?.value.trim() || '',
          dni: body.querySelector('#coprop-1-dni')?.value.trim() || '',
        },
        {
          nombre: body.querySelector('#coprop-2-nombre')?.value.trim() || '',
          dni: body.querySelector('#coprop-2-dni')?.value.trim() || '',
        },
        {
          nombre: body.querySelector('#coprop-3-nombre')?.value.trim() || '',
          dni: body.querySelector('#coprop-3-dni')?.value.trim() || '',
        },
      ],
    };
  }

  _bindFormAccionSubmit(tipo, movId) {
    const body = this.container.querySelector('#modal-accion-body');
    if (!body) return;

    if (tipo === 'aprobar') {
      body.querySelector('#form-accion-aprobar')?.addEventListener('submit', async (e) => {
        e.preventDefault();
        const obs = body.querySelector('#input-obs-accion')?.value.trim() || '';
        try {
          await movimientosComercialesService.aprobar(movId, { observaciones: obs });
          toast.success(`Movimiento #${movId} aprobado exitosamente.`);
          this.close();
          if (this.onCompleted) await this.onCompleted();
        } catch (err) {
          toast.error('Error al aprobar: ' + (err.message || 'Verifique los requisitos'));
        }
      });
    } else if (tipo === 'contrato') {
      body.querySelector('#form-accion-contrato')?.addEventListener('submit', async (e) => {
        e.preventDefault();
        const payload = this._getContratoFormData(body);
        if (!payload.numeroContrato) {
          toast.warning('El número de contrato es obligatorio.');
          return;
        }
        if (!payload.cliente.nombreCompleto) {
          toast.warning('El nombre del cliente es obligatorio.');
          return;
        }

        const submitBtn = body.querySelector('#btn-submit-contrato');
        if (submitBtn) {
          submitBtn.disabled = true;
          submitBtn.textContent = 'Generando...';
        }

        try {
          toast.info('Generando documento PDF desde la plantilla oficial...');
          const res = await contratoGeneratorService.generarContrato(payload);

          contratoGeneratorService.descargarPdf(res.blob, res.filename);
          contratoGeneratorService.guardarContratoCache(movId, res.dataUrl, res.filename);

          await movimientosComercialesService.generarContrato(movId, {
            numeroContrato: payload.numeroContrato,
            archivoUrl: res.filename,
          });

          toast.success(`Contrato #${payload.numeroContrato} (${res.pageCount} págs.) generado y asociado con éxito.`);
          this.close();
          if (this.onCompleted) await this.onCompleted();
        } catch (err) {
          console.error('[MovimientoAccionesModal] Error al generar contrato:', err);
          toast.error('Error al generar contrato: ' + (err.message || 'Error desconocido'));
        } finally {
          if (submitBtn) {
            submitBtn.disabled = false;
            submitBtn.textContent = 'Generar Contrato';
          }
        }
      });
    } else if (tipo === 'facturar') {
      body.querySelector('#form-accion-factura')?.addEventListener('submit', async (e) => {
        e.preventDefault();
        const num = body.querySelector('#input-num-factura')?.value.trim();
        const url = body.querySelector('#input-url-factura')?.value.trim() || null;
        const obs = body.querySelector('#input-obs-factura')?.value.trim() || null;
        if (!num) {
          toast.warning('El número de factura es obligatorio.');
          return;
        }
        try {
          await movimientosComercialesService.facturar(movId, { numeroFactura: num, archivoUrl: url, observaciones: obs });
          toast.success(`Factura emitida para el movimiento #${movId}.`);
          this.close();
          if (this.onCompleted) await this.onCompleted();
        } catch (err) {
          toast.error('Error al facturar: ' + (err.message || 'Error'));
        }
      });
    } else if (tipo === 'finalizar') {
      body.querySelector('#form-accion-finalizar')?.addEventListener('submit', async (e) => {
        e.preventDefault();
        const obs = body.querySelector('#input-obs-accion')?.value.trim() || '';
        try {
          await movimientosComercialesService.finalizar(movId, { observaciones: obs });
          toast.success(`Movimiento #${movId} finalizado. Inventario físico actualizado exitosamente.`);
          this.close();
          if (this.onCompleted) await this.onCompleted();
        } catch (err) {
          toast.error('Error al finalizar: ' + (err.message || 'Error en stock'));
        }
      });
    } else if (tipo === 'rechazar') {
      body.querySelector('#form-accion-rechazar')?.addEventListener('submit', async (e) => {
        e.preventDefault();
        const motivo = body.querySelector('#input-motivo-rechazo')?.value.trim();
        if (!motivo) {
          toast.warning('Debe indicar el motivo del rechazo.');
          return;
        }
        try {
          await movimientosComercialesService.rechazar(movId, { motivo });
          toast.success(`Movimiento #${movId} rechazado / observado.`);
          this.close();
          if (this.onCompleted) await this.onCompleted();
        } catch (err) {
          toast.error('Error al rechazar: ' + (err.message || 'Error'));
        }
      });
    }
  }
}
