/**
 * SIGA - views/precios-referencia-view.js
 * Vista Standalone de Precios de Referencia Oficiales (#precios-referencia) (Feature 33)
 * Parámetros oficiales por calibre (Clases 0..4) y MIX comercial.
 */

import { authService } from '../services/auth-service.js';
import { temporadasService } from '../services/temporadas-service.js';
import { preciosReferenciaService, CLASES_BULBO } from '../services/precios-referencia-service.js';
import { loadTemplate } from '../utils/template-loader.js';
import { toast } from '../utils/toast.js';
import { escapeHtml } from '../utils/escape-html.js';

export class PreciosReferenciaView {
  constructor(router) {
    this.router = router;
    this.container = null;
    this.temporadas = [];
    this.selectedTemporadaId = null;
    this.selectedTemporada = null;
    this.precios = [];
    this.canManage = false;
    this.isLoading = false;
  }

  async render(rootElement) {
    this.container = rootElement;
    this.canManage = authService.tienePermiso('PRECIOS_REF_GESTIONAR');

    this.container.innerHTML = await loadTemplate('precios-referencia.html');

    if (!this.canManage) {
      this.container.querySelector('#btn-guardar-header')?.remove();
      const alertContainer = this.container.querySelector('#alert-container-precios');
      if (alertContainer) {
        alertContainer.innerHTML = `
          <div class="alert alert-info d-flex align-center gap-sm mb-4" style="background: rgba(14, 165, 233, 0.1); border-left: 4px solid var(--info); padding: 0.85rem 1.25rem; border-radius: var(--radius-md);">
            <span style="font-size: 1.1rem;">ℹ️</span>
            <div>
              <strong>Modo Consulta (Solo Lectura):</strong> No posee permisos para modificar precios de referencia oficiales.
              La edición requiere el permiso <code style="font-weight: 700; color: var(--primary);">PRECIOS_REF_GESTIONAR</code>.
            </div>
          </div>
        `;
      }
    }

    this._bindBaseEvents();
    await this._loadTemporadas();
  }

  _bindBaseEvents() {
    const select = this.container.querySelector('#select-temporada-precios');
    select?.addEventListener('change', async (e) => {
      const val = Number(e.target.value);
      if (val && val !== this.selectedTemporadaId) {
        this.selectedTemporadaId = val;
        this.selectedTemporada = this.temporadas.find(t => Number(t.id) === val) || null;
        this._updateTemporadaMeta();
        await this._loadPrecios();
      }
    });

    const refreshBtn = this.container.querySelector('#btn-refresh-precios');
    refreshBtn?.addEventListener('click', async () => {
      await this._loadPrecios();
    });

    const headerSaveBtn = this.container.querySelector('#btn-guardar-header');
    headerSaveBtn?.addEventListener('click', async () => {
      await this._guardarPreciosLote();
    });

    const batchSaveBtn = this.container.querySelector('#btn-guardar-lote');
    batchSaveBtn?.addEventListener('click', async () => {
      await this._guardarPreciosLote();
    });

    const guardarComisionBtn = this.container.querySelector('#btn-guardar-comision');
    guardarComisionBtn?.addEventListener('click', async () => {
      await this._guardarComisionTemporada();
    });

    const resetBtn = this.container.querySelector('#btn-reset-precios');
    resetBtn?.addEventListener('click', () => {
      this._renderPreciosTable();
      toast.info('Se restablecieron los valores en pantalla.');
    });
  }

  async _loadTemporadas() {
    const select = this.container.querySelector('#select-temporada-precios');
    try {
      const [todas, activa] = await Promise.all([
        temporadasService.listar().catch(() => []),
        temporadasService.obtenerActiva().catch(() => null)
      ]);

      this.temporadas = Array.isArray(todas) ? todas : [];
      if (this.temporadas.length === 0) {
        select.innerHTML = '<option value="">No hay temporadas registradas</option>';
        return;
      }

      // Ordenar por ID descendente
      this.temporadas.sort((a, b) => Number(b.id) - Number(a.id));

      // Seleccionar activa por defecto o la primera
      const defaultId = activa ? Number(activa.id) : Number(this.temporadas[0].id);
      this.selectedTemporadaId = defaultId;
      this.selectedTemporada = this.temporadas.find(t => Number(t.id) === defaultId) || this.temporadas[0];

      select.innerHTML = this.temporadas.map(t => {
        const isActiva = activa && Number(activa.id) === Number(t.id);
        const label = `${t.nombre || 'Temporada #' + t.id}${isActiva ? ' (Activa)' : ''}`;
        return `<option value="${t.id}" ${Number(t.id) === defaultId ? 'selected' : ''}>${escapeHtml(label)}</option>`;
      }).join('');

      this._updateTemporadaMeta();
      await this._loadPrecios();
    } catch (err) {
      console.error('[PreciosReferenciaView] Error al cargar temporadas:', err);
      select.innerHTML = '<option value="">Error al cargar temporadas</option>';
      toast.error('No se pudieron cargar las temporadas.');
    }
  }

  _isTemporadaCerrada() {
    if (!this.selectedTemporada) return false;
    const est = String(this.selectedTemporada.estado || '').toLowerCase();
    return est === 'cerrada' || Number(this.selectedTemporada.estado) === 5;
  }

  _updateTemporadaMeta() {
    const metaContainer = this.container.querySelector('#temporada-meta-badge');
    const alertContainer = this.container.querySelector('#alert-container-precios');
    if (!metaContainer || !this.selectedTemporada) return;

    const isClosed = this._isTemporadaCerrada();
    const estadoStr = this.selectedTemporada.estado || (isClosed ? 'Cerrada' : 'Activa');
    const badgeClass = isClosed ? 'badge-danger' : 'badge-success';

    let fechasStr = '';
    if (this.selectedTemporada.fechaInicio && this.selectedTemporada.fechaFin) {
      const fIni = new Date(this.selectedTemporada.fechaInicio).toLocaleDateString('es-AR');
      const fFin = new Date(this.selectedTemporada.fechaFin).toLocaleDateString('es-AR');
      fechasStr = `<span class="text-xs text-muted">(${fIni} al ${fFin})</span>`;
    }

    metaContainer.innerHTML = `
      <span class="badge ${badgeClass}" style="font-size: 0.85rem; padding: 0.4rem 0.75rem;">
        <span class="badge-dot"></span>
        ${escapeHtml(estadoStr)}
      </span>
      ${fechasStr}
    `;

    // Renderizar banners informativos
    const banners = [];
    if (!this.canManage) {
      banners.push(`
        <div class="alert alert-info d-flex align-center gap-sm mb-4" style="background: rgba(14, 165, 233, 0.1); border-left: 4px solid var(--info); padding: 0.85rem 1.25rem; border-radius: var(--radius-md);">
          <span style="font-size: 1.1rem;">ℹ️</span>
          <div>
            <strong>Modo Consulta (Solo Lectura):</strong> No posee permisos para modificar precios de referencia oficiales.
            La edición requiere el permiso <code style="font-weight: 700; color: var(--primary);">PRECIOS_REF_GESTIONAR</code>.
          </div>
        </div>
      `);
    }

    if (isClosed) {
      banners.push(`
        <div class="alert alert-warning d-flex align-center gap-sm mb-4" style="background: rgba(245, 158, 11, 0.1); border-left: 4px solid var(--warning); padding: 0.85rem 1.25rem; border-radius: var(--radius-md);">
          <span style="font-size: 1.1rem;">⚠️</span>
          <div>
            <strong>Temporada Cerrada:</strong> Esta temporada ha concluido su ciclo productivo.
            Sus precios de referencia históricos se encuentran congelados y no pueden ser alterados.
          </div>
        </div>
      `);
    }

    if (alertContainer) {
      alertContainer.innerHTML = banners.join('');
    }

    // Ocultar o mostrar botón de guardar en cabecera según estado
    const headerSaveBtn = this.container.querySelector('#btn-guardar-header');
    if (headerSaveBtn) {
      headerSaveBtn.style.display = (this.canManage && !isClosed) ? 'inline-flex' : 'none';
    }

    // Controlar campos de comisión de compra según permisos y estado
    const comisionInput = this.container.querySelector('#input-comision-compra');
    const comisionBtn = this.container.querySelector('#btn-guardar-comision');
    const isEditable = this.canManage && !isClosed;

    if (comisionInput) {
      comisionInput.disabled = !isEditable;
    }
    if (comisionBtn) {
      comisionBtn.style.display = isEditable ? 'inline-flex' : 'none';
    }
  }

  async _loadPrecios() {
    const tbody = this.container.querySelector('#tbody-precios-referencia');
    const footer = this.container.querySelector('#footer-acciones-precios');
    if (!tbody) return;

    tbody.innerHTML = `<tr><td colspan="6" class="text-center text-muted" style="padding: 2.5rem;">Cargando precios de referencia...</td></tr>`;
    if (footer) footer.style.display = 'none';

    try {
      this.isLoading = true;
      this.precios = await preciosReferenciaService.listarPorTemporada(this.selectedTemporadaId);

      // Sincronizar campo de comisión de la temporada
      const comisionVal = this.precios[0]?.porcentajeComisionCompra != null
        ? Number(this.precios[0].porcentajeComisionCompra)
        : Number(this.selectedTemporada?.porcentajeComisionCompra ?? 0);

      if (this.selectedTemporada) {
        this.selectedTemporada.porcentajeComisionCompra = comisionVal;
      }

      const comisionInput = this.container.querySelector('#input-comision-compra');
      if (comisionInput) {
        comisionInput.value = comisionVal.toString();
      }

      this._renderPreciosTable();
    } catch (err) {
      console.error('[PreciosReferenciaView] Error al cargar precios:', err);
      tbody.innerHTML = `<tr><td colspan="6" class="text-center text-danger" style="padding: 2.5rem;">${escapeHtml(err.message || 'Error al obtener precios')}</td></tr>`;
      toast.error('No se pudieron consultar los precios de referencia.');
    } finally {
      this.isLoading = false;
    }
  }

  _getBadgeClaseColor(claseId) {
    switch (claseId) {
      case 0: return 'background: #fef3c7; color: #b45309; border: 1px solid #fde68a;'; // Gold
      case 1: return 'background: #ede9fe; color: #6d28d9; border: 1px solid #ddd6fe;'; // Purple
      case 2: return 'background: #e0e7ff; color: #4338ca; border: 1px solid #c7d2fe;'; // Indigo
      case 3: return 'background: #e0f2fe; color: #0369a1; border: 1px solid #bae6fd;'; // Blue
      case 4: return 'background: #f1f5f9; color: #475569; border: 1px solid #e2e8f0;'; // Slate
      case 5: return 'background: #ffedd5; color: #c2410c; border: 1px solid #fed7aa;'; // Amber (MIX)
      default: return 'background: var(--surface-alt); color: var(--text-main);';
    }
  }

  _renderPreciosTable() {
    const tbody = this.container.querySelector('#tbody-precios-referencia');
    const footer = this.container.querySelector('#footer-acciones-precios');
    if (!tbody) return;

    const isEditable = this.canManage && !this._isTemporadaCerrada();

    if (footer) {
      footer.style.display = isEditable ? 'flex' : 'none';
    }

    tbody.innerHTML = CLASES_BULBO.map(claseMeta => {
      const precioItem = this.precios.find(p => p.claseBulbo === claseMeta.id);
      const precioVal = precioItem ? Number(precioItem.precioReferencia) : 0;
      const fVig = precioItem?.fechaVigencia
        ? new Date(precioItem.fechaVigencia).toLocaleDateString('es-AR')
        : '<span class="text-muted">Sin registrar</span>';

      const badgeStyle = this._getBadgeClaseColor(claseMeta.id);

      return `
        <tr data-clase-id="${claseMeta.id}">
          <td>
            <span class="badge font-bold" style="${badgeStyle} font-size: 0.85rem; padding: 0.35rem 0.65rem;">
              ${escapeHtml(claseMeta.label)}
            </span>
          </td>
          <td>
            <span class="font-medium text-main">${escapeHtml(claseMeta.calibres)}</span>
          </td>
          <td>
            <span class="text-sm text-muted">${escapeHtml(claseMeta.descripcion)}</span>
          </td>
          <td>
            <span class="text-xs text-muted">${fVig}</span>
          </td>
          <td style="text-align: right;">
            ${isEditable ? `
              <div class="input-group" style="max-width: 170px; margin-left: auto;">
                <span class="input-group-text font-bold" style="background: var(--surface-alt);">$</span>
                <input
                  type="number"
                  step="0.01"
                  min="0.01"
                  class="form-control input-precio-ref"
                  data-clase-id="${claseMeta.id}"
                  value="${precioVal > 0 ? precioVal.toFixed(2) : ''}"
                  placeholder="0.00"
                  style="text-align: right; font-weight: 700; font-size: 1rem;"
                />
              </div>
            ` : `
              <span class="font-bold text-primary" style="font-size: 1.15rem;">
                $ ${precioVal.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
              </span>
            `}
          </td>
          <td style="text-align: center;">
            ${isEditable ? `
              <button
                type="button"
                class="btn btn-secondary btn-sm btn-guardar-individual"
                data-clase-id="${claseMeta.id}"
                title="Guardar este precio individualmente"
              >
                <span>Guardar</span>
              </button>
            ` : `
              <span class="badge badge-secondary text-xs">Bloqueado</span>
            `}
          </td>
        </tr>
      `;
    }).join('');

    if (isEditable) {
      this._bindTableEvents();
    }
  }

  _bindTableEvents() {
    const tbody = this.container.querySelector('#tbody-precios-referencia');
    if (!tbody) return;

    // Validación interactiva en tiempo real sobre los inputs
    tbody.querySelectorAll('.input-precio-ref').forEach(input => {
      input.addEventListener('input', (e) => {
        const val = Number(e.target.value);
        if (isNaN(val) || val <= 0) {
          e.target.style.borderColor = 'var(--danger)';
          e.target.style.boxShadow = '0 0 0 1px var(--danger)';
        } else {
          e.target.style.borderColor = 'var(--border)';
          e.target.style.boxShadow = 'none';
        }
      });
    });

    // Guardado individual
    tbody.querySelectorAll('.btn-guardar-individual').forEach(btn => {
      btn.addEventListener('click', async (e) => {
        const claseId = Number(btn.getAttribute('data-clase-id'));
        await this._guardarPrecioIndividual(claseId);
      });
    });
  }

  async _guardarPrecioIndividual(claseId) {
    const input = this.container.querySelector(`.input-precio-ref[data-clase-id="${claseId}"]`);
    if (!input) return;

    const val = Number(input.value);
    if (isNaN(val) || val <= 0) {
      input.style.borderColor = 'var(--danger)';
      input.style.boxShadow = '0 0 0 1px var(--danger)';
      toast.warning('El precio debe ser un número estrictamente mayor a 0.');
      input.focus();
      return;
    }

    try {
      const btn = this.container.querySelector(`.btn-guardar-individual[data-clase-id="${claseId}"]`);
      if (btn) {
        btn.disabled = true;
        btn.textContent = 'Guardando...';
      }

      await preciosReferenciaService.guardarPrecioIndividual(this.selectedTemporadaId, claseId, val);
      toast.success(`Precio para ${CLASES_BULBO[claseId].label} actualizado exitosamente.`);

      input.style.borderColor = 'var(--success)';
      setTimeout(() => {
        if (input) input.style.borderColor = 'var(--border)';
      }, 1500);

      await this._loadPrecios();
    } catch (err) {
      console.error('[PreciosReferenciaView] Error al guardar precio individual:', err);
      toast.error('Error al guardar precio: ' + (err.message || 'Verifique los datos'));
    }
  }

  async _guardarPreciosLote() {
    if (!this.canManage || this._isTemporadaCerrada()) return;

    const inputs = this.container.querySelectorAll('.input-precio-ref');
    const items = [];
    let hasErrors = false;

    inputs.forEach(inp => {
      const claseId = Number(inp.getAttribute('data-clase-id'));
      const val = Number(inp.value);
      if (isNaN(val) || val <= 0) {
        inp.style.borderColor = 'var(--danger)';
        inp.style.boxShadow = '0 0 0 1px var(--danger)';
        hasErrors = true;
      } else {
        inp.style.borderColor = 'var(--border)';
        inp.style.boxShadow = 'none';
        items.push({
          claseBulbo: claseId,
          precioReferencia: val
        });
      }
    });

    if (hasErrors || items.length < 6) {
      toast.warning('Debe ingresar un precio válido (> $0.00) para cada una de las 6 clases antes de guardar.');
      return;
    }

    try {
      const saveBtn = this.container.querySelector('#btn-guardar-lote');
      const headerBtn = this.container.querySelector('#btn-guardar-header');
      if (saveBtn) { saveBtn.disabled = true; saveBtn.textContent = 'Guardando...'; }
      if (headerBtn) { headerBtn.disabled = true; headerBtn.textContent = 'Guardando...'; }

      const comisionInput = this.container.querySelector('#input-comision-compra');
      const comisionVal = comisionInput ? Number(comisionInput.value) : null;

      await preciosReferenciaService.guardarPreciosTemporada(this.selectedTemporadaId, items, comisionVal);
      toast.success('Precios de referencia y parámetros de la temporada guardados exitosamente.');

      if (comisionVal !== null && !isNaN(comisionVal) && this.selectedTemporada) {
        this.selectedTemporada.porcentajeComisionCompra = comisionVal;
        const itemInList = this.temporadas.find(t => Number(t.id) === Number(this.selectedTemporadaId));
        if (itemInList) itemInList.porcentajeComisionCompra = comisionVal;
      }

      await this._loadPrecios();
    } catch (err) {
      console.error('[PreciosReferenciaView] Error al guardar lote:', err);
      toast.error('Error al guardar precios de temporada: ' + (err.message || 'Ocurrió un error'));
    } finally {
      const saveBtn = this.container.querySelector('#btn-guardar-lote');
      const headerBtn = this.container.querySelector('#btn-guardar-header');
      if (saveBtn) { saveBtn.disabled = false; saveBtn.innerHTML = '<span>Guardar Precios de la Temporada</span>'; }
      if (headerBtn) { headerBtn.disabled = false; headerBtn.innerHTML = '<span>Guardar Precios</span>'; }
    }
  }

  async _guardarComisionTemporada() {
    if (!this.canManage || this._isTemporadaCerrada()) return;

    const input = this.container.querySelector('#input-comision-compra');
    const btn = this.container.querySelector('#btn-guardar-comision');
    if (!input) return;

    const val = Number(input.value);
    if (isNaN(val) || val < 0 || val > 100) {
      input.style.borderColor = 'var(--danger)';
      toast.warning('El porcentaje de comisión debe estar entre 0% y 100%.');
      input.focus();
      return;
    }

    try {
      if (btn) {
        btn.disabled = true;
        btn.innerHTML = '<span>Guardando...</span>';
      }

      await temporadasService.actualizarComisionCompra(this.selectedTemporadaId, val);
      toast.success(`Comisión retenida de compra actualizada a ${val.toFixed(2)}% para esta temporada.`);

      if (this.selectedTemporada) {
        this.selectedTemporada.porcentajeComisionCompra = val;
      }
      const itemInList = this.temporadas.find(t => Number(t.id) === Number(this.selectedTemporadaId));
      if (itemInList) {
        itemInList.porcentajeComisionCompra = val;
      }

      input.style.borderColor = 'var(--success)';
      setTimeout(() => {
        if (input) input.style.borderColor = 'var(--border)';
      }, 1500);
    } catch (err) {
      console.error('[PreciosReferenciaView] Error al guardar comisión:', err);
      toast.error('Error al guardar comisión: ' + (err.message || 'Ocurrió un error'));
    } finally {
      if (btn) {
        btn.disabled = false;
        btn.innerHTML = `
          <svg class="icon-svg sm" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="20 6 9 17 4 12"></polyline>
          </svg>
          <span>Guardar Comisión</span>
        `;
      }
    }
  }
}
