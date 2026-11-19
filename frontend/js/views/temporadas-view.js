/**
 * SIGA - views/temporadas-view.js
 * Vista básica de Gestión de Temporadas Agrícolas y Estados del Ciclo
 */

import { temporadasService, ESTADOS_TEMPORADA } from '../services/temporadas-service.js';
import { loadTemplate } from '../utils/template-loader.js';
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
    this.container.innerHTML = await loadTemplate('temporadas.html');
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
      this.temporadaActiva = activa || this.temporadas.find(t => {
        const est = (t.estado || '').toLowerCase();
        return est !== 'cerrada' && est !== 'planificada';
      }) || null;

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
      const esPlanificada = (t.estado || '').toLowerCase() === 'planificada';
      const otraActiva = esPlanificada ? this.temporadas.find(o => String(o.id) !== String(t.id) && (o.estado || '').toLowerCase() !== 'cerrada' && (o.estado || '').toLowerCase() !== 'planificada') : null;

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
            ${siguiente ? (
              otraActiva ? `
                <button class="btn btn-sm btn-outline-warning btn-avanzar-fila" data-id="${escapeHtml(t.id)}" data-sig="${escapeHtml(siguiente)}" data-nombre="${escapeHtml(t.nombre)}" data-bloqueo="${escapeHtml(otraActiva.nombre)}" title="Requiere que '${escapeHtml(otraActiva.nombre)}' esté cerrada primero">
                  <span>Avanzar a ${escapeHtml(siguiente)}</span>
                  <svg class="icon-svg sm" viewBox="0 0 24 24"><polyline points="9 18 15 12 9 6"></polyline></svg>
                </button>
              ` : `
                <button class="btn btn-sm btn-outline-primary btn-avanzar-fila" data-id="${escapeHtml(t.id)}" data-sig="${escapeHtml(siguiente)}" data-nombre="${escapeHtml(t.nombre)}">
                  <span>Avanzar a ${escapeHtml(siguiente)}</span>
                  <svg class="icon-svg sm" viewBox="0 0 24 24"><polyline points="9 18 15 12 9 6"></polyline></svg>
                </button>
              `
            ) : `
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
    const esPlanificada = (temporada.estado || '').toLowerCase() === 'planificada';
    if (esPlanificada) {
      const otraActiva = this.temporadas.find(o => String(o.id) !== String(temporada.id) && (o.estado || '').toLowerCase() !== 'cerrada' && (o.estado || '').toLowerCase() !== 'planificada');
      if (otraActiva) {
        toast.warning(`No se puede iniciar la temporada "${temporada.nombre}". La temporada activa anterior "${otraActiva.nombre}" debe estar cerrada primero.`);
        return;
      }
    }

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
