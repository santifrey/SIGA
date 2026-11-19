/**
 * SIGA - views/movimientos-comerciales/movimiento-form-modal.js
 * Controlador del Modal de Creación y Edición Multi-Tipo de Movimientos Comerciales:
 * - Venta a Cliente (Tipo 1)
 * - Compra a Cliente con comisión (Tipo 2)
 * - Traspaso entre Clientes (Tipo 3)
 * - Grilla de calibres 0..4 + MIX (descomposición en tercios) y verificación de stock en vivo.
 */

import { stockService, EMPRESA_SINGLETON_ID } from '../../services/stock-service.js';
import { sucursalesService } from '../../services/sucursales-service.js';
import { preciosReferenciaService } from '../../services/precios-referencia-service.js';
import {
  movimientosComercialesService,
  CLASES_BULBO,
  _parseTipoIndex
} from '../../services/movimientos-comerciales-service.js';
import { ClienteAutocomplete } from '../../components/cliente-autocomplete.js';
import { toast } from '../../utils/toast.js';
import { escapeHtml } from '../../utils/escape-html.js';
import { _parseClaseIndex } from './movimientos-helpers.js';

export class MovimientoFormModal {
  /**
   * @param {Object} options
   * @param {HTMLElement} options.container
   * @param {() => Array} options.getTemporadas
   * @param {() => Array} options.getClientes
   * @param {() => number|null} options.getActiveTemporadaId
   * @param {() => void} options.onOpenNuevoCliente
   * @param {() => Promise<void>} options.onSaved
   */
  constructor({
    container,
    getTemporadas,
    getClientes,
    getActiveTemporadaId,
    onOpenNuevoCliente,
    onSaved
  }) {
    this.container = container;
    this.getTemporadas = getTemporadas;
    this.getClientes = getClientes;
    this.getActiveTemporadaId = getActiveTemporadaId;
    this.onOpenNuevoCliente = onOpenNuevoCliente;
    this.onSaved = onSaved;

    this.currentEditMovimiento = null;
    this.selectedTipo = 1; // 1: Venta, 2: Compra, 3: Traspaso
    this.preciosTemporada = {};
    this.sucursales = [];
    this.origenStockData = null;
    this.clienteNuevoStatus = { esNuevo: false, requiereContrato: false };
    this.acClienteOrigen = null;
    this.acClienteDestino = null;
  }

  bindEvents() {
    this.container.querySelector('#btn-close-modal-movimiento')?.addEventListener('click', () => this.close());
    this.container.querySelector('#btn-cancel-modal-movimiento')?.addEventListener('click', () => this.close());

    this.container.querySelector('#mov-temporada')?.addEventListener('change', async (e) => {
      const tId = Number(e.target.value);
      if (tId) {
        await this._loadPreciosReferenciaTemporada(tId);
        if (this.selectedTipo === 2 && !this.currentEditMovimiento) {
          const tObj = (this.getTemporadas() || []).find(t => Number(t.id) === tId);
          if (tObj && tObj.porcentajeComisionCompra != null) {
            const inpCom = this.container.querySelector('#mov-porcentaje-comision');
            if (inpCom) {
              inpCom.value = Number(tObj.porcentajeComisionCompra);
            }
          }
        }
        this._recalcularTotales();
      }
    });

    this.container.querySelectorAll('input[name="radio-tipo-movimiento"]').forEach(radio => {
      radio.addEventListener('change', (e) => {
        this.selectedTipo = Number(e.target.value);
        this._updateFormTipoLayout();
      });
    });

    this.container.querySelector('#form-movimiento')?.addEventListener('submit', async (e) => {
      e.preventDefault();
      await this._handleSubmitMovimiento();
    });
  }

  async open(editId = null) {
    const modal = this.container.querySelector('#modal-movimiento');
    const title = this.container.querySelector('#modal-movimiento-title');
    const selectTemp = this.container.querySelector('#mov-temporada');
    if (!modal || !title || !selectTemp) return;

    try {
      const fetched = await sucursalesService.listar({ estado: 'activas' });
      this.sucursales = (Array.isArray(fetched) ? fetched : []).filter(s => s.activa !== false && String(s.estado || '').toLowerCase() !== 'inactiva');
    } catch {
      this.sucursales = [];
    }

    const temporadas = this.getTemporadas();
    const activeTemporadaId = this.getActiveTemporadaId();

    selectTemp.innerHTML = '<option value="">Seleccione temporada...</option>' +
      temporadas.map(t => {
        const isSelected = activeTemporadaId && Number(t.id) === Number(activeTemporadaId);
        return `<option value="${t.id}" ${isSelected ? 'selected' : ''}>${escapeHtml(t.nombre || 'Temporada #' + t.id)}</option>`;
      }).join('');

    if (editId) {
      title.textContent = `Corregir Movimiento Comercial #${editId}`;
      try {
        const mov = await movimientosComercialesService.obtenerPorId(editId);
        this.currentEditMovimiento = mov;
        this.selectedTipo = _parseTipoIndex(mov.tipo ?? mov.nombreTipo);
      } catch {
        toast.error('No se pudo cargar el movimiento para editar.');
        return;
      }
    } else {
      title.textContent = 'Nuevo Movimiento Comercial';
      this.currentEditMovimiento = null;
      this.selectedTipo = 1;
    }

    const radio = this.container.querySelector(`input[name="radio-tipo-movimiento"][value="${this.selectedTipo}"]`);
    if (radio) radio.checked = true;

    this.container.querySelectorAll('input[name="radio-tipo-movimiento"]').forEach(r => {
      r.disabled = Boolean(editId);
    });

    const tempId = this.currentEditMovimiento ? Number(this.currentEditMovimiento.temporadaId) : activeTemporadaId;
    if (tempId) {
      selectTemp.value = String(tempId);
      await this._loadPreciosReferenciaTemporada(tempId);
    }

    const submitBtn = this.container.querySelector('#btn-submit-movimiento');
    if (submitBtn) {
      submitBtn.innerHTML = editId ? '<span>Corregir y Reenviar</span>' : '<span>Guardar y Registrar</span>';
    }

    this._updateFormTipoLayout();
    modal.classList.add('is-active');
  }

  close() {
    const modal = this.container.querySelector('#modal-movimiento');
    modal?.classList.remove('is-active');
    this.currentEditMovimiento = null;
    this.origenStockData = null;
  }

  async updateClienteSelectorAfterCreation(selectedId = null) {
    if (selectedId && this.acClienteDestino && this.selectedTipo === 1) {
      await this.acClienteDestino.setValueById(selectedId);
      const cliente = this.acClienteDestino.getSelected();
      if (cliente && cliente.sucursalId) {
        const origSelect = this.container.querySelector('#mov-origen-id');
        if (origSelect) {
          const matchOpt = Array.from(origSelect.options).find(o => o.getAttribute('data-sucursal-id') === String(cliente.sucursalId));
          if (matchOpt && origSelect.value !== matchOpt.value) {
            origSelect.value = matchOpt.value;
            this._cargarStockOrigen(Number(matchOpt.value));
          }
        }
      }
      await this._verificarClienteNuevoBadge(Number(selectedId));
    }
  }

  async _loadPreciosReferenciaTemporada(temporadaId) {
    try {
      const list = await preciosReferenciaService.listarPorTemporada(temporadaId);
      this.preciosTemporada = {};
      list.forEach(p => {
        this.preciosTemporada[_parseClaseIndex(p.claseBulbo)] = Number(p.precioReferencia) || 0;
      });
    } catch {
      this.preciosTemporada = {};
    }
  }

  _updateFormTipoLayout() {
    const partContainer = this.container.querySelector('#participantes-container');
    const avisoCliente = this.container.querySelector('#aviso-cliente-nuevo');
    const containerComision = this.container.querySelector('#container-comision');
    const avisoTraspaso = this.container.querySelector('#aviso-traspaso');
    const resumenComision = this.container.querySelector('#resumen-comision-compra');
    const labelTipoTotal = this.container.querySelector('#mov-label-tipo-total');
    if (!partContainer) return;

    avisoCliente.style.display = 'none';
    containerComision.style.display = 'none';
    avisoTraspaso.style.display = 'none';
    resumenComision.style.display = 'none';

    const clientes = (this.getClientes() || []).filter(c =>
      c.activo !== false &&
      String(c.estadoCliente || c.estado || 'Activo').toLowerCase() !== 'inactivo'
    );
    const clientesOptions = clientes.map(c =>
      `<option value="${c.id}">👤 ${escapeHtml(c.nombre)} ${escapeHtml(c.apellido)} (${escapeHtml(c.codigo || 'CLI-' + c.id)})</option>`
    ).join('');

    const sucursalesActivas = (this.sucursales || []).filter(s =>
      s.activa !== false &&
      String(s.estado || 'Activa').toLowerCase() !== 'inactiva'
    );

    if (this.selectedTipo === 1) {
      labelTipoTotal.textContent = 'MONTO TOTAL VENTA:';

      let sucursalesOptions = sucursalesActivas.map(s => {
        const partId = s.participanteComercialId || s.id;
        const isMatriz = s.esCasaMatriz ? ' (Casa Matriz)' : '';
        return `<option value="${partId}" data-sucursal-id="${s.id}">🏢 ${escapeHtml(s.nombre)}${isMatriz} (Part #${partId})</option>`;
      }).join('');

      if (!sucursalesOptions) {
        sucursalesOptions = `<option value="1" data-sucursal-id="1">🏢 Sede Central (Depósito Central, Part #1)</option>`;
      }

      partContainer.innerHTML = `
        <div class="form-group mb-0">
          <label for="mov-origen-id" class="form-label text-xs font-bold uppercase text-muted">Origen (Sucursal Vendedora) *</label>
          <select id="mov-origen-id" class="form-control" required>
            ${sucursalesOptions}
          </select>
        </div>
        <div class="form-group mb-0">
          <div class="d-flex justify-between align-center" style="margin-bottom: 0.375rem;">
            <label class="form-label text-xs font-bold uppercase text-muted mb-0">Destino (Cliente Comprador) *</label>
            <button type="button" class="btn btn-outline-primary btn-sm" id="btn-nuevo-cliente-venta" style="padding: 2px 8px; font-size: 0.7rem;" title="Registrar un nuevo cliente vinculado a esta sucursal">
              + Nuevo Cliente
            </button>
          </div>
          <div id="autocomplete-box-cliente-destino"></div>
        </div>
      `;

      const origSelect = this.container.querySelector('#mov-origen-id');
      const initialPartId = Number(origSelect?.value) || EMPRESA_SINGLETON_ID;
      this._cargarStockOrigen(initialPartId);

      origSelect?.addEventListener('change', (e) => {
        const partId = Number(e.target.value);
        if (partId) {
          this._cargarStockOrigen(partId);
        }
      });

      this.container.querySelector('#btn-nuevo-cliente-venta')?.addEventListener('click', () => {
        const selectedOption = origSelect?.selectedOptions?.[0];
        const sucursalId = selectedOption?.getAttribute('data-sucursal-id') || null;
        if (this.onOpenNuevoCliente) this.onOpenNuevoCliente(sucursalId);
      });

      const acDestEl = this.container.querySelector('#autocomplete-box-cliente-destino');
      if (acDestEl) {
        this.acClienteDestino = new ClienteAutocomplete({
          container: acDestEl,
          name: 'mov-destino-id',
          placeholder: 'Buscar comprador por DNI, Nombre o Código...',
          required: true,
          onSelect: async (cliente) => {
            if (cliente && cliente.sucursalId && origSelect) {
              const matchOpt = Array.from(origSelect.options).find(o => o.getAttribute('data-sucursal-id') === String(cliente.sucursalId));
              if (matchOpt && origSelect.value !== matchOpt.value) {
                origSelect.value = matchOpt.value;
                this._cargarStockOrigen(Number(matchOpt.value));
              }
            }
            await this._verificarClienteNuevoBadge(cliente.id);
          },
          onClear: () => {
            avisoCliente.style.display = 'none';
            this.clienteNuevoStatus = { esNuevo: false, requiereContrato: false };
          }
        });
      }

    } else if (this.selectedTipo === 2) {
      labelTipoTotal.textContent = 'MONTO NETO A LIQUIDAR:';
      containerComision.style.display = 'block';
      resumenComision.style.display = 'block';

      let sucursalesOptions = sucursalesActivas.map(s => {
        const partId = s.participanteComercialId || s.id;
        const isMatriz = s.esCasaMatriz ? ' (Casa Matriz)' : '';
        return `<option value="${partId}" data-sucursal-id="${s.id}">🏢 ${escapeHtml(s.nombre)}${isMatriz} (Part #${partId})</option>`;
      }).join('');

      if (!sucursalesOptions) {
        sucursalesOptions = `<option value="1" data-sucursal-id="1">🏢 Sede Central (Depósito Central, Part #1)</option>`;
      }

      partContainer.innerHTML = `
        <div class="form-group mb-0">
          <label class="form-label text-xs font-bold uppercase text-muted mb-1">Origen (Cliente Vendedor) *</label>
          <div id="autocomplete-box-cliente-origen"></div>
        </div>
        <div class="form-group mb-0">
          <label for="mov-destino-id" class="form-label text-xs font-bold uppercase text-muted">Destino (Sucursal Compradora) *</label>
          <select id="mov-destino-id" class="form-control" required>
            ${sucursalesOptions}
          </select>
        </div>
      `;

      // Determinar comisión por defecto según temporada activa/seleccionada o valor en edición
      let comisionDefault = 5;
      if (this.currentEditMovimiento?.porcentajeComision != null) {
        comisionDefault = Number(this.currentEditMovimiento.porcentajeComision);
      } else {
        const temporadas = this.getTemporadas() || [];
        const curTempId = Number(this.container.querySelector('#mov-temporada')?.value || this.getActiveTemporadaId());
        const tempObj = temporadas.find(t => Number(t.id) === curTempId);
        if (tempObj && tempObj.porcentajeComisionCompra != null) {
          comisionDefault = Number(tempObj.porcentajeComisionCompra);
        }
      }

      containerComision.innerHTML = `
        <div class="card p-3" style="background: var(--surface-alt);">
          <div class="d-flex align-center gap-md flex-wrap">
            <label for="mov-porcentaje-comision" class="form-label font-bold text-xs uppercase text-muted mb-0">
              Comisión Administrativa Retenida (%):
            </label>
            <div class="input-group" style="max-width: 140px;">
              <input type="number" id="mov-porcentaje-comision" class="form-control" min="0" max="100" step="0.5" value="${comisionDefault}" required />
              <span class="input-group-text font-bold">%</span>
            </div>
            <span class="text-xs text-muted">Porcentaje deducido del monto bruto de compra (definido a nivel temporada).</span>
          </div>
        </div>
      `;

      const destSelect = this.container.querySelector('#mov-destino-id');
      const acOrigEl = this.container.querySelector('#autocomplete-box-cliente-origen');
      if (acOrigEl) {
        this.acClienteOrigen = new ClienteAutocomplete({
          container: acOrigEl,
          name: 'mov-origen-id',
          placeholder: 'Buscar vendedor por DNI, Nombre o Código...',
          required: true,
          onSelect: (cliente) => {
            this._cargarStockOrigen(cliente.id);
            if (cliente && cliente.sucursalId && destSelect) {
              const matchOpt = Array.from(destSelect.options).find(o => o.getAttribute('data-sucursal-id') === String(cliente.sucursalId));
              if (matchOpt) {
                destSelect.value = matchOpt.value;
              }
            }
          },
          onClear: () => {
            this._cargarStockOrigen(0);
          }
        });
      }

      this.container.querySelector('#mov-porcentaje-comision')?.addEventListener('input', () => {
        this._recalcularTotales();
      });

    } else if (this.selectedTipo === 3) {
      labelTipoTotal.textContent = 'MONTO OPERACIÓN:';
      avisoTraspaso.style.display = 'block';

      let sucursalesOptions = sucursalesActivas.map(s => {
        const partId = s.participanteComercialId || s.id;
        const isMatriz = s.esCasaMatriz ? ' (Casa Matriz)' : '';
        return `<option value="${partId}" data-sucursal-id="${s.id}">🏢 ${escapeHtml(s.nombre)}${isMatriz} (Part #${partId})</option>`;
      }).join('');

      if (!sucursalesOptions) {
        sucursalesOptions = `<option value="1" data-sucursal-id="1">🏢 Sede Central (Depósito Central, Part #1)</option>`;
      }

      partContainer.innerHTML = `
        <div class="form-group mb-2" style="grid-column: 1 / -1;">
          <label class="form-label text-xs font-bold uppercase text-muted mb-1">Modalidad de Traspaso *</label>
          <div class="d-flex gap-lg flex-wrap" style="background: var(--surface-alt); padding: 0.5rem 0.75rem; border-radius: var(--radius-sm); border: 1px solid var(--border);">
            <label class="d-flex align-center gap-xs cursor-pointer font-semibold text-xs text-main">
              <input type="radio" name="radio-modalidad-traspaso" value="sucursales" checked />
              <span>🏢 Entre Sucursales (Movimiento inter-sucursal de depósitos)</span>
            </label>
            <label class="d-flex align-center gap-xs cursor-pointer font-semibold text-xs text-main">
              <input type="radio" name="radio-modalidad-traspaso" value="clientes" />
              <span>👤 Entre Clientes (Transferencia de custodia entre clientes)</span>
            </label>
          </div>
        </div>
        <div id="traspaso-dynamic-fields" style="display: contents;"></div>
      `;

      const dynContainer = this.container.querySelector('#traspaso-dynamic-fields');

      const updateTraspasoInputs = (modalidad) => {
        if (!dynContainer) return;
        if (modalidad === 'sucursales') {
          this.acClienteOrigen = null;
          this.acClienteDestino = null;
          dynContainer.innerHTML = `
            <div class="form-group mb-0">
              <label for="mov-origen-id" class="form-label text-xs font-bold uppercase text-muted">Sucursal Origen (Emisora) *</label>
              <select id="mov-origen-id" class="form-control" required>
                <option value="">Seleccione sucursal emisora...</option>
                ${sucursalesOptions}
              </select>
            </div>
            <div class="form-group mb-0">
              <label for="mov-destino-id" class="form-label text-xs font-bold uppercase text-muted">Sucursal Destino (Receptora) *</label>
              <select id="mov-destino-id" class="form-control" required>
                <option value="">Seleccione sucursal receptora...</option>
                ${sucursalesOptions}
              </select>
            </div>
          `;
          const origSelect = this.container.querySelector('#mov-origen-id');
          origSelect?.addEventListener('change', (e) => {
            const id = Number(e.target.value);
            if (id) this._cargarStockOrigen(id);
          });
          this._cargarStockOrigen(0);
        } else {
          dynContainer.innerHTML = `
            <div class="form-group mb-0">
              <label class="form-label text-xs font-bold uppercase text-muted mb-1">Cliente Origen (Emisor / Cedente) *</label>
              <div id="autocomplete-traspaso-origen"></div>
            </div>
            <div class="form-group mb-0">
              <label class="form-label text-xs font-bold uppercase text-muted mb-1">Cliente Destino (Receptor) *</label>
              <div id="autocomplete-traspaso-destino"></div>
            </div>
          `;
          const acOrigEl = this.container.querySelector('#autocomplete-traspaso-origen');
          if (acOrigEl) {
            this.acClienteOrigen = new ClienteAutocomplete({
              container: acOrigEl,
              name: 'mov-origen-id',
              placeholder: 'Buscar cliente emisor por DNI...',
              required: true,
              onSelect: (cli) => {
                this._cargarStockOrigen(cli.id);
              },
              onClear: () => {
                this._cargarStockOrigen(0);
              }
            });
          }
          const acDestEl = this.container.querySelector('#autocomplete-traspaso-destino');
          if (acDestEl) {
            this.acClienteDestino = new ClienteAutocomplete({
              container: acDestEl,
              name: 'mov-destino-id',
              placeholder: 'Buscar cliente receptor por DNI...',
              required: true
            });
          }
          this._cargarStockOrigen(0);
        }
      };

      updateTraspasoInputs('sucursales');

      this.container.querySelectorAll('input[name="radio-modalidad-traspaso"]').forEach(radio => {
        radio.addEventListener('change', (e) => {
          updateTraspasoInputs(e.target.value);
        });
      });
    }

    this._renderItemsGrid();

    if (this.currentEditMovimiento) {
      this._populateFormEditValues();
    }
  }

  async _verificarClienteNuevoBadge(clienteId) {
    const aviso = this.container.querySelector('#aviso-cliente-nuevo');
    if (!aviso) return;

    try {
      const status = await movimientosComercialesService.verificarClienteNuevo(
        clienteId,
        this.currentEditMovimiento ? this.currentEditMovimiento.id : null
      );
      this.clienteNuevoStatus = status;

      if (status.esNuevo && status.requiereContrato) {
        aviso.style.display = 'block';
        aviso.innerHTML = `
          <div class="alert alert-warning d-flex align-center gap-sm p-3" style="background: rgba(245, 158, 11, 0.1); border-left: 4px solid var(--warning); border-radius: var(--radius-sm);">
            <span style="font-size: 1.2rem;">⚠️</span>
            <div class="text-sm">
              <strong>Cliente Nuevo:</strong> Este cliente no cuenta con ventas previas finalizadas en el sistema.
              Para que la Administración pueda aprobar esta venta, será <strong>obligatorio generar y registrar el contrato marco previo</strong>.
            </div>
          </div>
        `;
      } else {
        aviso.style.display = 'block';
        aviso.innerHTML = `
          <div class="alert alert-success d-flex align-center gap-sm p-2" style="background: rgba(34, 197, 94, 0.1); border-left: 4px solid var(--success); border-radius: var(--radius-sm);">
            <span style="font-size: 1rem;">✓</span>
            <div class="text-xs">
              <strong>Cliente Regular:</strong> Posee historial comercial previo. No requiere generación obligatoria de contrato previo.
            </div>
          </div>
        `;
      }
    } catch (err) {
      console.warn('[MovimientoFormModal] Error al verificar cliente nuevo:', err);
    }
  }

  async _cargarStockOrigen(origenId) {
    try {
      this.origenStockData = await stockService.obtenerStockPorCliente(origenId);
      this._verificarStockOrigenEnVivo();
    } catch {
      this.origenStockData = null;
    }
  }

  _renderItemsGrid() {
    const tbody = this.container.querySelector('#tbody-items-movimiento');
    if (!tbody) return;

    const isTraspaso = this.selectedTipo === 3;

    tbody.innerHTML = CLASES_BULBO.map(claseMeta => {
      const isMix = claseMeta.id === 5;
      const refPrice = isTraspaso ? 0 : (this.preciosTemporada[claseMeta.id] || 0);

      return `
        <tr data-clase="${claseMeta.id}">
          <td>
            <div class="d-flex flex-column">
              <span class="font-bold text-main">${escapeHtml(claseMeta.label)}</span>
              <span class="text-xs text-muted">${escapeHtml(claseMeta.desc)}</span>
            </div>
          </td>
          <td>
            <input
              type="number"
              class="form-control item-cant"
              data-clase="${claseMeta.id}"
              min="0"
              step="${isMix ? '3' : '1'}"
              placeholder="0"
            />
            ${isMix ? `
              <div id="mix-feedback-error" class="text-xs text-danger font-bold mt-1" style="display: none;">
                La cantidad para MIX debe ser múltiplo de 3 (ej. 300, 600, 900).
              </div>
              <div id="mix-decomp-callout" class="text-xs text-info font-bold mt-1" style="display: none;">
                <!-- Descomposición calculada en vivo -->
              </div>
            ` : ''}
          </td>
          <td>
            <div class="input-group">
              <span class="input-group-text">$</span>
              <input
                type="number"
                class="form-control item-precio"
                data-clase="${claseMeta.id}"
                min="0"
                step="0.01"
                value="${isTraspaso ? '0.00' : (refPrice > 0 ? refPrice.toFixed(2) : '0.00')}"
                ${isTraspaso ? 'disabled' : ''}
              />
            </div>
          </td>
          <td style="text-align: right;">
            <span class="font-bold item-subtotal text-main">$ 0,00</span>
          </td>
        </tr>
      `;
    }).join('');

    tbody.querySelectorAll('.item-cant, .item-precio').forEach(inp => {
      inp.addEventListener('input', () => {
        this._recalcularTotales();
        this._verificarStockOrigenEnVivo();
      });
    });
  }

  _populateFormEditValues(mov = this.currentEditMovimiento) {
    if (!mov) return;

    const origenSelect = this.container.querySelector('#mov-origen-id');
    const destinoSelect = this.container.querySelector('#mov-destino-id');
    const obsInput = this.container.querySelector('#mov-observaciones');
    const comisionInput = this.container.querySelector('#mov-porcentaje-comision');

    if (this.selectedTipo === 1) {
      if (origenSelect && mov.origenParticipanteId) {
        origenSelect.value = String(mov.origenParticipanteId);
        this._cargarStockOrigen(mov.origenParticipanteId);
      }
      if (this.acClienteDestino && mov.destinoParticipanteId) {
        this.acClienteDestino.setValueById(mov.destinoParticipanteId);
        this._verificarClienteNuevoBadge(mov.destinoParticipanteId);
      } else if (destinoSelect && mov.destinoParticipanteId) {
        destinoSelect.value = String(mov.destinoParticipanteId);
        this._verificarClienteNuevoBadge(mov.destinoParticipanteId);
      }
    } else if (this.selectedTipo === 2) {
      if (this.acClienteOrigen && mov.origenParticipanteId) {
        this.acClienteOrigen.setValueById(mov.origenParticipanteId);
        this._cargarStockOrigen(mov.origenParticipanteId);
      } else if (origenSelect && mov.origenParticipanteId) {
        origenSelect.value = String(mov.origenParticipanteId);
        this._cargarStockOrigen(mov.origenParticipanteId);
      }
      if (destinoSelect && mov.destinoParticipanteId) {
        destinoSelect.value = String(mov.destinoParticipanteId);
      }
    } else if (this.selectedTipo === 3) {
      if (this.acClienteOrigen && mov.origenParticipanteId) {
        this.acClienteOrigen.setValueById(mov.origenParticipanteId);
      } else if (origenSelect && mov.origenParticipanteId) {
        origenSelect.value = String(mov.origenParticipanteId);
      }
      if (this.acClienteDestino && mov.destinoParticipanteId) {
        this.acClienteDestino.setValueById(mov.destinoParticipanteId);
      } else if (destinoSelect && mov.destinoParticipanteId) {
        destinoSelect.value = String(mov.destinoParticipanteId);
      }
    }
    if (obsInput) obsInput.value = mov.observaciones || '';
    if (comisionInput && mov.porcentajeComision !== undefined) comisionInput.value = String(mov.porcentajeComision);

    if (Array.isArray(mov.detalles)) {
      mov.detalles.forEach(d => {
        const cId = _parseClaseIndex(d.claseBulbo);
        const cantInp = this.container.querySelector(`.item-cant[data-clase="${cId}"]`);
        const precInp = this.container.querySelector(`.item-precio[data-clase="${cId}"]`);
        if (cantInp) cantInp.value = String(d.cantidad);
        if (precInp) precInp.value = Number(d.precioUnitarioPactado).toFixed(2);
      });
    }

    this._recalcularTotales();
    this._verificarStockOrigenEnVivo();
  }

  _recalcularTotales() {
    let totalBulbos = 0;
    let montoBruto = 0;

    const isTraspaso = this.selectedTipo === 3;
    const isCompra = this.selectedTipo === 2;

    CLASES_BULBO.forEach(claseMeta => {
      const cId = claseMeta.id;
      const cantInp = this.container.querySelector(`.item-cant[data-clase="${cId}"]`);
      const precInp = this.container.querySelector(`.item-precio[data-clase="${cId}"]`);
      const row = this.container.querySelector(`tr[data-clase="${cId}"]`);

      const cant = Number(cantInp?.value) || 0;
      const prec = isTraspaso ? 0 : (Number(precInp?.value) || 0);

      if (cId === 5) {
        const errEl = this.container.querySelector('#mix-feedback-error');
        const calloutEl = this.container.querySelector('#mix-decomp-callout');

        if (cant > 0 && cant % 3 !== 0) {
          if (errEl) errEl.style.display = 'block';
          if (calloutEl) calloutEl.style.display = 'none';
          cantInp.style.borderColor = 'var(--danger)';
        } else {
          if (errEl) errEl.style.display = 'none';
          cantInp.style.borderColor = 'var(--border)';
          if (cant > 0 && calloutEl) {
            const part = cant / 3;
            calloutEl.style.display = 'block';
            calloutEl.textContent = `Descomposición: ${part} Clase 1, ${part} Clase 2, ${part} Clase 3.`;
          } else if (calloutEl) {
            calloutEl.style.display = 'none';
          }
        }
      }

      const subtotal = isTraspaso ? 0 : (cant * prec);
      totalBulbos += cant;
      montoBruto += subtotal;

      const subtotalEl = row?.querySelector('.item-subtotal');
      if (subtotalEl) {
        subtotalEl.textContent = `$ ${subtotal.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
      }
    });

    const totalBulbosCell = this.container.querySelector('#mov-total-bulbos-cell');
    if (totalBulbosCell) {
      totalBulbosCell.textContent = `${totalBulbos.toLocaleString('es-AR')} unidades`;
    }

    const totalMontoCell = this.container.querySelector('#mov-total-monto-cell');

    if (isTraspaso) {
      if (totalMontoCell) totalMontoCell.textContent = '$ 0,00';
    } else if (isCompra) {
      const pctInput = this.container.querySelector('#mov-porcentaje-comision');
      const pct = Math.max(0, Math.min(100, Number(pctInput?.value) || 0));
      const comision = Math.round(montoBruto * (pct / 100) * 100) / 100;
      const neto = Math.round((montoBruto - comision) * 100) / 100;

      const labelBruto = this.container.querySelector('#label-bruto-compra');
      const labelPct = this.container.querySelector('#label-pct-comision');
      const labelCom = this.container.querySelector('#label-monto-comision');
      const labelNeto = this.container.querySelector('#label-neto-compra');

      if (labelBruto) labelBruto.textContent = `$ ${montoBruto.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
      if (labelPct) labelPct.textContent = String(pct);
      if (labelCom) labelCom.textContent = `- $ ${comision.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
      if (labelNeto) labelNeto.textContent = `$ ${neto.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;

      if (totalMontoCell) {
        totalMontoCell.textContent = `$ ${neto.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
      }
    } else if (totalMontoCell) {
      totalMontoCell.textContent = `$ ${montoBruto.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
    }
  }

  _verificarStockOrigenEnVivo() {
    const alertaContainer = this.container.querySelector('#alerta-stock-origen');
    if (!alertaContainer || !this.origenStockData) {
      if (alertaContainer) alertaContainer.style.display = 'none';
      return;
    }

    const requeridosFisicos = { 0: 0, 1: 0, 2: 0, 3: 0, 4: 0 };

    for (let c = 0; c <= 4; c++) {
      const cant = Number(this.container.querySelector(`.item-cant[data-clase="${c}"]`)?.value) || 0;
      requeridosFisicos[c] += cant;
    }

    const mixCant = Number(this.container.querySelector(`.item-cant[data-clase="5"]`)?.value) || 0;
    if (mixCant > 0 && mixCant % 3 === 0) {
      const tercio = mixCant / 3;
      requeridosFisicos[1] += tercio;
      requeridosFisicos[2] += tercio;
      requeridosFisicos[3] += tercio;
    }

    const deficiencias = [];
    for (let c = 0; c <= 4; c++) {
      const req = requeridosFisicos[c];
      if (req > 0) {
        const itemStock = this.origenStockData.find(s => _parseClaseIndex(s.claseBulbo ?? s.clase) === c);
        const disp = itemStock ? Number(itemStock.cantidad) : 0;
        if (req > disp) {
          deficiencias.push(`Clase ${c}: requiere ${req.toLocaleString('es-AR')} un., dispone de ${disp.toLocaleString('es-AR')} un.`);
        }
      }
    }

    if (deficiencias.length > 0) {
      alertaContainer.style.display = 'block';
      alertaContainer.innerHTML = `
        <div class="alert alert-danger d-flex align-start gap-sm p-3" style="background: rgba(239, 68, 68, 0.1); border-left: 4px solid var(--danger); border-radius: var(--radius-sm);">
          <span style="font-size: 1.1rem; color: var(--danger);">⚠️</span>
          <div>
            <strong class="text-danger">Advertencia de Stock Insuficiente en Origen:</strong>
            <p class="text-xs text-muted mb-1">
              Las cantidades especificadas (incluida la descomposición de MIX) superan el stock registrado en el titular emisor:
            </p>
            <ul class="text-xs text-danger mb-0" style="padding-left: 1.25rem;">
              ${deficiencias.map(d => `<li>${escapeHtml(d)}</li>`).join('')}
            </ul>
          </div>
        </div>
      `;
    } else {
      alertaContainer.style.display = 'none';
    }
  }

  async _handleSubmitMovimiento() {
    const temporadaId = Number(this.container.querySelector('#mov-temporada')?.value);
    if (!temporadaId) {
      toast.warning('Debe seleccionar la temporada agrícola.');
      return;
    }

    let origenId = 1;
    let destinoId = 1;

    if (this.selectedTipo === 1) {
      origenId = Number(this.container.querySelector('#mov-origen-id')?.value) || EMPRESA_SINGLETON_ID;
      destinoId = Number(this.container.querySelector('#mov-destino-id')?.value);
      if (!destinoId) {
        toast.warning('Debe seleccionar el cliente comprador.');
        return;
      }
    } else if (this.selectedTipo === 2) {
      origenId = Number(this.container.querySelector('#mov-origen-id')?.value);
      destinoId = Number(this.container.querySelector('#mov-destino-id')?.value) || EMPRESA_SINGLETON_ID;
      if (!origenId) {
        toast.warning('Debe seleccionar el cliente vendedor.');
        return;
      }
      if (!destinoId) {
        toast.warning('Debe seleccionar la sucursal compradora.');
        return;
      }
    } else if (this.selectedTipo === 3) {
      origenId = Number(this.container.querySelector('#mov-origen-id')?.value);
      destinoId = Number(this.container.querySelector('#mov-destino-id')?.value);
      if (!origenId || !destinoId) {
        toast.warning('Debe seleccionar el participante origen y destino.');
        return;
      }
      if (origenId === destinoId) {
        toast.warning('El participante origen y destino no pueden ser el mismo.');
        return;
      }
    }

    const detalles = [];
    let totalBulbos = 0;

    for (let c = 0; c <= 5; c++) {
      const cantInp = this.container.querySelector(`.item-cant[data-clase="${c}"]`);
      const precInp = this.container.querySelector(`.item-precio[data-clase="${c}"]`);
      const cant = Number(cantInp?.value) || 0;
      const prec = this.selectedTipo === 3 ? 0 : (Number(precInp?.value) || 0);

      if (cant > 0) {
        if (c === 5 && cant % 3 !== 0) {
          toast.warning('La cantidad de ítems MIX debe ser múltiplo de 3.');
          cantInp.focus();
          return;
        }

        if (this.selectedTipo !== 3 && prec <= 0) {
          toast.warning(`Debe especificar un precio unitario pactado mayor a 0 para ${CLASES_BULBO[c].label}.`);
          precInp.focus();
          return;
        }

        detalles.push({
          claseBulbo: c,
          cantidad: cant,
          precioUnitarioPactado: prec
        });
        totalBulbos += cant;
      }
    }

    if (detalles.length === 0 || totalBulbos <= 0) {
      toast.warning('Debe ingresar al menos una clase de bulbo con cantidad mayor a 0.');
      return;
    }

    const obs = this.container.querySelector('#mov-observaciones')?.value.trim() || '';
    const pctComision = this.selectedTipo === 2
      ? (Number(this.container.querySelector('#mov-porcentaje-comision')?.value) || 0)
      : 0;

    const submitBtn = this.container.querySelector('#btn-submit-movimiento');
    if (submitBtn) {
      submitBtn.disabled = true;
      submitBtn.textContent = 'Procesando...';
    }

    try {
      const clienteParticipanteId = this.selectedTipo === 1 ? destinoId : (this.selectedTipo === 2 ? origenId : null);

      if (this.currentEditMovimiento) {
        const updateDto = {
          origenParticipanteId: origenId,
          destinoParticipanteId: destinoId,
          clienteId: clienteParticipanteId,
          temporadaId,
          porcentajeComision: pctComision,
          observaciones: obs,
          detalles
        };
        await movimientosComercialesService.actualizar(this.currentEditMovimiento.id, updateDto);
        toast.success(`Movimiento Comercial #${this.currentEditMovimiento.id} corregido y reenviado a revisión.`);
      } else {
        const createDto = {
          tipo: this.selectedTipo,
          temporadaId,
          origenParticipanteId: origenId,
          destinoParticipanteId: destinoId,
          clienteId: clienteParticipanteId,
          porcentajeComision: pctComision,
          observaciones: obs,
          detalles
        };
        const nuevo = await movimientosComercialesService.crear(createDto);
        toast.success(`Movimiento Comercial #${nuevo.id || ''} registrado exitosamente.`);
      }

      this.close();
      if (this.onSaved) {
        await this.onSaved();
      }
    } catch (err) {
      console.error('[MovimientoFormModal] Error al guardar movimiento:', err);
      toast.error('Error al registrar operación: ' + (err.message || 'Verifique los datos'));
    } finally {
      if (submitBtn) {
        submitBtn.disabled = false;
        submitBtn.innerHTML = this.currentEditMovimiento ? '<span>Corregir y Reenviar</span>' : '<span>Guardar y Registrar</span>';
      }
    }
  }
}
