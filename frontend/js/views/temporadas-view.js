/**
 * SIGA - views/temporadas-view.js
 * Vista básica de Gestión de Temporadas Agrícolas y Estados del Ciclo
 */

import { temporadasService, ESTADOS_TEMPORADA } from '../services/temporadas-service.js';
import { toast } from '../utils/toast.js';
import { escapeHtml } from '../utils/escape-html.js';

export class TemporadasView {
  constructor(router) {
    this.router = router;
    this.container = null;
    this.temporadaActiva = null;
    this.temporadas = [];
  }

  async render(rootElement) {
    this.container = rootElement;

    this.container.innerHTML = `
      <div class="fade-in">
        <!-- Encabezado de la Vista -->
        <div class="d-flex justify-between align-center mb-4 flex-wrap gap-md">
          <div>
            <h1 style="font-size: 1.5rem; margin-bottom: 0.25rem;">Gestión de Temporadas</h1>
            <p class="text-muted text-sm">Control de ciclos anuales y fases del cultivo de azafrán</p>
          </div>
          <div class="d-flex gap-sm">
            <button class="btn btn-secondary" id="btn-refresh-temporadas" title="Refrescar temporadas">
              <svg class="icon-svg sm" viewBox="0 0 24 24">
                <polyline points="23 4 23 10 17 10"></polyline>
                <polyline points="1 20 1 14 7 14"></polyline>
                <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"></path>
              </svg>
              <span>Refrescar</span>
            </button>
            <button class="btn btn-primary" id="btn-nueva-temporada">
              <svg class="icon-svg sm" viewBox="0 0 24 24">
                <line x1="12" y1="5" x2="12" y2="19"></line>
                <line x1="5" y1="12" x2="19" y2="12"></line>
              </svg>
              <span>+ Nueva Temporada</span>
            </button>
          </div>
        </div>

        <!-- Banner de Temporada Activa y Avance de Estado -->
        <div class="card mb-4" id="banner-temporada-activa" style="border-left: 5px solid var(--primary); background: linear-gradient(to right, var(--surface), var(--primary-50));">
          <div class="card-body d-flex justify-between align-center flex-wrap gap-md">
            <div>
              <div class="text-xs text-muted font-bold" style="letter-spacing: 0.5px; text-transform: uppercase;">
                Temporada Activa del Sistema
              </div>
              <div class="d-flex align-center gap-md mt-1">
                <h3 class="font-bold text-main" id="activa-nombre" style="margin: 0; font-size: 1.25rem;">Cargando...</h3>
                <span id="activa-badge" class="badge">---</span>
              </div>
              <p class="text-sm text-muted mt-1" id="activa-periodo" style="margin-bottom: 0;">-</p>
            </div>
            <div id="activa-accion-container">
              <!-- Botón dinámico para avanzar estado -->
            </div>
          </div>
        </div>

        <!-- Tabla de Listado de Temporadas -->
        <div class="table-card">
          <div class="table-header p-3 border-bottom d-flex justify-between align-center">
            <h4 class="font-semibold text-main m-0">Historial de Temporadas</h4>
            <span class="text-xs text-muted" id="total-temporadas-count">0 temporadas registradas</span>
          </div>

          <div class="table-responsive">
            <table class="table" id="tabla-temporadas">
              <thead>
                <tr>
                  <th style="width: 80px;">ID</th>
                  <th>Nombre de Temporada</th>
                  <th>Fecha de Inicio</th>
                  <th>Fecha de Fin</th>
                  <th>Estado Actual</th>
                  <th style="text-align: right;">Acciones</th>
                </tr>
              </thead>
              <tbody id="tbody-temporadas">
                <tr class="table-loading-row">
                  <td colspan="6" class="text-center py-4">
                    <span>Consultando temporadas...</span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <!-- Modal Crear Nueva Temporada -->
      <div id="modal-nueva-temporada" class="modal-backdrop" role="dialog" aria-modal="true">
        <div class="modal-dialog">
          <div class="modal-header">
            <h3 class="modal-title">+ Registrar Nueva Temporada</h3>
            <button type="button" class="modal-close-btn" id="btn-close-modal-temporada" aria-label="Cerrar modal">
              <svg class="icon-svg sm" viewBox="0 0 24 24"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
            </button>
          </div>
          <form id="form-temporada" novalidate>
            <div class="modal-body">
              <div class="form-group mb-3">
                <label for="temp-nombre" class="form-label">
                  Nombre de la Temporada <span class="required-mark">*</span>
                </label>
                <input
                  type="text"
                  id="temp-nombre"
                  class="form-control"
                  placeholder="ej. Temporada 2026-2027"
                  required
                />
              </div>

              <div class="form-row two-cols mb-3">
                <div class="form-group">
                  <label for="temp-fecha-inicio" class="form-label">
                    Fecha de Inicio <span class="required-mark">*</span>
                  </label>
                  <input
                    type="date"
                    id="temp-fecha-inicio"
                    class="form-control"
                    required
                  />
                </div>
                <div class="form-group">
                  <label for="temp-fecha-fin" class="form-label">
                    Fecha Estimada de Fin <span class="required-mark">*</span>
                  </label>
                  <input
                    type="date"
                    id="temp-fecha-fin"
                    class="form-control"
                    required
                  />
                </div>
              </div>

              <div class="form-group">
                <label for="temp-observaciones" class="form-label">Observaciones / Notas</label>
                <textarea
                  id="temp-observaciones"
                  class="form-control"
                  rows="3"
                  placeholder="Objetivos productivos, variedades de bulbos o particularidades climáticas..."
                ></textarea>
              </div>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" id="btn-cancelar-modal-temporada">Cancelar</button>
              <button type="submit" class="btn btn-primary" id="btn-guardar-temporada">
                <span class="btn-text">Guardar Temporada</span>
                <span class="btn-spinner"></span>
              </button>
            </div>
          </form>
        </div>
      </div>
    `;

    this._bindEvents();
    await this.loadData();
  }

  _bindEvents() {
    this.container.querySelector('#btn-refresh-temporadas').addEventListener('click', () => {
      this.loadData();
    });

    this.container.querySelector('#btn-nueva-temporada').addEventListener('click', () => {
      this._openModal();
    });

    this.container.querySelector('#btn-close-modal-temporada').addEventListener('click', () => {
      this._closeModal();
    });

    this.container.querySelector('#btn-cancelar-modal-temporada').addEventListener('click', () => {
      this._closeModal();
    });

    this.container.querySelector('#form-temporada').addEventListener('submit', (e) => {
      this._handleSubmit(e);
    });
  }

  _getBadgeClass(estado) {
    switch ((estado || '').toLowerCase()) {
      case 'planificada':
        return 'badge-info';
      case 'plantación':
      case 'plantacion':
        return 'badge-accent';
      case 'en crecimiento':
      case 'encrecimiento':
        return 'badge-success';
      case 'cosecha':
        return 'badge-warning';
      case 'comercialización':
      case 'comercializacion':
        return 'badge-primary';
      case 'cerrada':
        return 'badge-secondary';
      default:
        return 'badge-info';
    }
  }

  async loadData() {
    try {
      const [temporadas, activa] = await Promise.all([
        temporadasService.listar(),
        temporadasService.obtenerActiva().catch(() => null)
      ]);

      this.temporadas = temporadas || [];
      this.temporadaActiva = activa || (this.temporadas.length > 0 ? this.temporadas[0] : null);

      this._renderBannerActiva();
      this._renderTabla();
    } catch (err) {
      toast.error(err.message || 'Error al cargar las temporadas');
      const tbody = this.container.querySelector('#tbody-temporadas');
      tbody.innerHTML = `
        <tr>
          <td colspan="6" class="text-center py-4 text-danger">
            Error al consultar temporadas: ${escapeHtml(err.message)}
          </td>
        </tr>
      `;
    }
  }

  _renderBannerActiva() {
    const nombreEl = this.container.querySelector('#activa-nombre');
    const badgeEl = this.container.querySelector('#activa-badge');
    const periodoEl = this.container.querySelector('#activa-periodo');
    const accionContainer = this.container.querySelector('#activa-accion-container');

    if (!this.temporadaActiva) {
      nombreEl.textContent = 'Sin Temporada Activa';
      badgeEl.className = 'badge badge-secondary';
      badgeEl.textContent = 'Ninguna';
      periodoEl.textContent = 'Cree una nueva temporada para iniciar el ciclo agrícola.';
      accionContainer.innerHTML = '';
      return;
    }

    const t = this.temporadaActiva;
    nombreEl.textContent = t.nombre;
    badgeEl.className = `badge ${this._getBadgeClass(t.estado)}`;
    badgeEl.innerHTML = `<span class="badge-dot"></span> ${escapeHtml(t.estado)}`;
    periodoEl.textContent = `Período: ${t.fechaInicio ? new Date(t.fechaInicio).toLocaleDateString() : 'N/A'} al ${t.fechaFin ? new Date(t.fechaFin).toLocaleDateString() : 'N/A'}`;

    const siguienteEstado = temporadasService.obtenerSiguienteEstado(t.estado);

    if (siguienteEstado) {
      accionContainer.innerHTML = `
        <button class="btn btn-accent" id="btn-avanzar-activa">
          <svg class="icon-svg sm" viewBox="0 0 24 24">
            <polygon points="5 3 19 12 5 21 5 3"></polygon>
          </svg>
          <span>Avanzar a "${escapeHtml(siguienteEstado)}"</span>
        </button>
      `;

      accionContainer.querySelector('#btn-avanzar-activa').addEventListener('click', () => {
        this._confirmarAvanzarEstado(t, siguienteEstado);
      });
    } else {
      accionContainer.innerHTML = `
        <span class="badge badge-secondary" style="padding: 0.5rem 1rem; font-size: 0.85rem;">
          Temporada en estado final (${escapeHtml(t.estado)})
        </span>
      `;
    }
  }

  _renderTabla() {
    const tbody = this.container.querySelector('#tbody-temporadas');
    const countEl = this.container.querySelector('#total-temporadas-count');

    countEl.textContent = `${this.temporadas.length} temporadas registradas`;

    if (this.temporadas.length === 0) {
      tbody.innerHTML = `
        <tr>
          <td colspan="6" class="text-center py-4 text-muted">
            No hay temporadas registradas aún. Haga clic en "+ Nueva Temporada" para crear la primera.
          </td>
        </tr>
      `;
      return;
    }

    tbody.innerHTML = this.temporadas.map(t => {
      const siguiente = temporadasService.obtenerSiguienteEstado(t.estado);
      const isActiva = this.temporadaActiva && String(this.temporadaActiva.id) === String(t.id);

      return `
        <tr class="${isActiva ? 'table-row-active' : ''}">
          <td>
            <span class="font-bold text-xs" style="color: var(--primary); background: var(--primary-100); padding: 3px 8px; border-radius: var(--radius-sm);">
              #${escapeHtml(t.id)}
            </span>
          </td>
          <td>
            <div class="font-semibold text-main">${escapeHtml(t.nombre)}</div>
            ${isActiva ? '<span class="badge badge-primary text-xs" style="margin-top: 2px;">Activa</span>' : ''}
            ${t.observaciones ? `<small class="text-muted text-truncate d-block" style="max-width: 250px;">${escapeHtml(t.observaciones)}</small>` : ''}
          </td>
          <td>${escapeHtml(t.fechaInicio ? new Date(t.fechaInicio).toLocaleDateString() : '-')}</td>
          <td>${escapeHtml(t.fechaFin ? new Date(t.fechaFin).toLocaleDateString() : '-')}</td>
          <td>
            <span class="badge ${this._getBadgeClass(t.estado)}">
              <span class="badge-dot"></span>
              ${escapeHtml(t.estado)}
            </span>
          </td>
          <td style="text-align: right;">
            ${siguiente ? `
              <button class="btn btn-sm btn-outline-primary btn-avanzar-fila" data-id="${escapeHtml(t.id)}" data-sig="${escapeHtml(siguiente)}" data-nombre="${escapeHtml(t.nombre)}">
                <span>Avanzar a ${escapeHtml(siguiente)}</span>
                <svg class="icon-svg sm" viewBox="0 0 24 24"><polyline points="9 18 15 12 9 6"></polyline></svg>
              </button>
            ` : `
              <span class="text-xs text-muted">Finalizada</span>
            `}
          </td>
        </tr>
      `;
    }).join('');

    tbody.querySelectorAll('.btn-avanzar-fila').forEach(btn => {
      btn.addEventListener('click', () => {
        const id = btn.getAttribute('data-id');
        const sig = btn.getAttribute('data-sig');
        const nombre = btn.getAttribute('data-nombre');
        const temp = this.temporadas.find(t => String(t.id) === String(id)) || { id, nombre };
        this._confirmarAvanzarEstado(temp, sig);
      });
    });
  }

  async _confirmarAvanzarEstado(temporada, siguienteEstado) {
    const conf = confirm(
      `¿Desea avanzar la "${temporada.nombre}" al siguiente estado: "${siguienteEstado}"?\n\nEsta acción modificará la fase productiva del ciclo.`
    );
    if (!conf) return;

    try {
      await temporadasService.cambiarEstado(temporada.id, siguienteEstado);
      toast.success(`La temporada avanzó a estado "${siguienteEstado}" con éxito.`);
      await this.loadData();
    } catch (err) {
      toast.error(err.message || 'Error al cambiar de estado');
    }
  }

  _openModal() {
    const modal = this.container.querySelector('#modal-nueva-temporada');
    const form = this.container.querySelector('#form-temporada');
    form.reset();

    // Fechas sugeridas: inicio hoy, fin en 8 meses
    const hoy = new Date().toISOString().split('T')[0];
    const fechaFinSug = new Date();
    fechaFinSug.setMonth(fechaFinSug.getMonth() + 8);
    const fin = fechaFinSug.toISOString().split('T')[0];

    this.container.querySelector('#temp-fecha-inicio').value = hoy;
    this.container.querySelector('#temp-fecha-fin').value = fin;

    modal.classList.add('is-active');
    setTimeout(() => this.container.querySelector('#temp-nombre').focus(), 100);
  }

  _closeModal() {
    const modal = this.container.querySelector('#modal-nueva-temporada');
    modal.classList.remove('is-active');
  }

  async _handleSubmit(e) {
    e.preventDefault();

    const nombre = this.container.querySelector('#temp-nombre').value.trim();
    const fechaInicio = this.container.querySelector('#temp-fecha-inicio').value;
    const fechaFin = this.container.querySelector('#temp-fecha-fin').value;
    const observaciones = this.container.querySelector('#temp-observaciones').value.trim();

    if (!nombre) {
      toast.warning('Ingrese el nombre de la temporada.');
      return;
    }
    if (!fechaInicio || !fechaFin) {
      toast.warning('Ingrese fechas de inicio y fin válidas.');
      return;
    }
    if (new Date(fechaFin) <= new Date(fechaInicio)) {
      toast.warning('La fecha de fin debe ser posterior a la fecha de inicio.');
      return;
    }

    const saveBtn = this.container.querySelector('#btn-guardar-temporada');
    saveBtn.classList.add('is-loading');
    saveBtn.disabled = true;

    try {
      await temporadasService.crear({
        nombre,
        fechaInicio,
        fechaFin,
        observaciones
      });

      toast.success(`Temporada "${nombre}" registrada correctamente.`);
      this._closeModal();
      await this.loadData();
    } catch (err) {
      toast.error(err.message || 'Error al guardar la temporada');
    } finally {
      saveBtn.classList.remove('is-loading');
      saveBtn.disabled = false;
    }
  }
}
