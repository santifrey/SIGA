/**
 * SIGA - views/transferencias-view.js
 * Vista funcional para Transferencias de Lotes Completos e Historial de Titularidad
 */

import { transferenciasService } from '../services/transferencias-service.js';
import { plantacionesService } from '../services/plantaciones-service.js';
import { temporadasService } from '../services/temporadas-service.js';
import { clientesService } from '../services/clientes-service.js';
import { loadTemplate } from '../utils/template-loader.js';
import { toast } from '../utils/toast.js';
import { escapeHtml } from '../utils/escape-html.js';

export class TransferenciasView {
  constructor() {
    this.container = null;
    this.temporadas = [];
    this.lotes = [];
    this.clientes = [];
    this.transferencias = [];
    this.selectedTemporadaId = null;
  }

  async render(container) {
    this.container = container;
    this.container.innerHTML = await loadTemplate('transferencias.html');

    await this.cargarDatosIniciales();
    this.bindEvents();
  }

  async cargarDatosIniciales() {
    try {
      const [temps, cls, transfs] = await Promise.all([
        temporadasService.listar(),
        clientesService.listar({ pageSize: 100 }),
        transferenciasService.listar()
      ]);

      this.temporadas = Array.isArray(temps) ? temps : temps.items || [];
      this.clientes = Array.isArray(cls) ? cls : cls.items || [];
      this.transferencias = Array.isArray(transfs) ? transfs : transfs.data || [];

      this.renderSelectTemporadas();
      this.renderSelectNuevoTitular();
      this.renderTablaTransferencias();

      // Cargar lotes de la temporada activa o primera
      const activa = this.temporadas.find(t => t.estado === 'Plantacion' || t.estado === 'EnCrecimiento') || this.temporadas[0];
      if (activa) {
        const selectTemp = document.getElementById('transf-temporada');
        if (selectTemp) {
          selectTemp.value = activa.id;
          await this.cargarLotesTemporada(activa.id);
        }
      }
    } catch (err) {
      toast.error('Error al cargar datos de transferencias: ' + err.message);
    }
  }

  renderSelectTemporadas() {
    const select = document.getElementById('transf-temporada');
    if (!select) return;
    select.innerHTML = '<option value="">-- Seleccione Temporada --</option>' +
      this.temporadas.map(t => `<option value="${escapeHtml(t.id)}">${escapeHtml(t.nombre)} (${escapeHtml(t.nombreEstado || t.estado)})</option>`).join('');
  }

  renderSelectNuevoTitular() {
    const select = document.getElementById('transf-nuevo-titular');
    if (!select) return;
    let html = '<option value="">-- Seleccione Nuevo Titular --</option>';
    html += '<option value="1">🏢 Sede Central (Azafrán Mediterráneo S.A.)</option>';
    html += this.clientes.map(c => {
      const nom = c.persona ? `${c.persona.nombre} ${c.persona.apellido}` : (c.nombreCompleto || c.codigoCliente);
      return `<option value="${escapeHtml(c.id)}">👤 ${escapeHtml(nom)} (${escapeHtml(c.codigoCliente)})</option>`;
    }).join('');
    select.innerHTML = html;
  }

  async cargarLotesTemporada(temporadaId) {
    const selectLote = document.getElementById('transf-lote');
    if (!selectLote) return;
    try {
      this.lotes = await plantacionesService.listarLotes(temporadaId);
      if (!this.lotes || this.lotes.length === 0) {
        selectLote.innerHTML = '<option value="">(No hay lotes plantados en esta temporada)</option>';
        return;
      }
      selectLote.innerHTML = '<option value="">-- Seleccione Lote --</option>' +
        this.lotes.map(l => `<option value="${escapeHtml(l.id)}">Lote #${escapeHtml(l.id)} - Fila ${escapeHtml(l.identificacionFila || l.filaId)} | Titular: ${escapeHtml(l.nombreCliente || l.clienteId)} (${escapeHtml(l.cantidadPlantadaTotal)} bulbos)</option>`).join('');
    } catch (err) {
      selectLote.innerHTML = '<option value="">Error al cargar lotes</option>';
    }
  }

  renderTablaTransferencias() {
    const tbody = document.getElementById('tabla-transferencias-body');
    if (!tbody) return;

    if (!this.transferencias || this.transferencias.length === 0) {
      tbody.innerHTML = '<tr><td colspan="8" class="text-center p-3 text-muted">No se registran transferencias de lotes.</td></tr>';
      return;
    }

    tbody.innerHTML = this.transferencias.map(t => {
      const fecha = t.fecha ? new Date(t.fecha).toLocaleDateString() : '-';
      const badgeClass = t.estado === 'Confirmada' ? 'badge-success' : (t.estado === 'Anulada' ? 'badge-danger' : 'badge-warning');
      return `
        <tr>
          <td><strong>#${escapeHtml(t.id)}</strong></td>
          <td>Lote #${escapeHtml(t.loteId)}</td>
          <td>${escapeHtml(t.identificacionFila || '-')} (${escapeHtml(t.nombreSucursal || 'Central')})</td>
          <td>${escapeHtml(t.nombreTitularAnterior || `Titular #${t.titularAnteriorId}`)}</td>
          <td><strong class="text-primary">${escapeHtml(t.nombreNuevoTitular || `Titular #${t.nuevoTitularId}`)}</strong></td>
          <td>${escapeHtml(fecha)}</td>
          <td><span class="badge ${badgeClass}">${escapeHtml(t.estado)}</span></td>
          <td>
            ${t.estado === 'Confirmada' ? `<button class="btn btn-outline-danger btn-sm btn-anular-transf" data-id="${escapeHtml(t.id)}">Anular</button>` : '-'}
          </td>
        </tr>
      `;
    }).join('');
  }

  bindEvents() {
    const selectTemp = document.getElementById('transf-temporada');
    if (selectTemp) {
      selectTemp.addEventListener('change', async (e) => {
        if (e.target.value) {
          await this.cargarLotesTemporada(e.target.value);
        }
      });
    }

    const form = document.getElementById('form-transferir-lote');
    if (form) {
      form.addEventListener('submit', async (e) => {
        e.preventDefault();
        const loteId = document.getElementById('transf-lote')?.value;
        const nuevoTitularId = document.getElementById('transf-nuevo-titular')?.value;
        const observaciones = document.getElementById('transf-obs')?.value;

        if (!loteId || !nuevoTitularId) {
          toast.warning('Debe seleccionar el lote y el nuevo titular.');
          return;
        }

        try {
          await transferenciasService.transferirLote({
            loteId: Number(loteId),
            nuevoTitularId: Number(nuevoTitularId),
            observaciones
          });

          toast.success('Transferencia de lote registrada exitosamente.');
          form.reset();
          const list = await transferenciasService.listar();
          this.transferencias = Array.isArray(list) ? list : list.data || [];
          this.renderTablaTransferencias();
        } catch (err) {
          toast.error('Error al transferir lote: ' + err.message);
        }
      });
    }

    const btnRefresh = document.getElementById('btn-refresh-transf');
    if (btnRefresh) {
      btnRefresh.addEventListener('click', async () => {
        const list = await transferenciasService.listar();
        this.transferencias = Array.isArray(list) ? list : list.data || [];
        this.renderTablaTransferencias();
        toast.info('Lista de transferencias actualizada.');
      });
    }

    const tbody = document.getElementById('tabla-transferencias-body');
    if (tbody) {
      tbody.addEventListener('click', async (e) => {
        const btn = e.target.closest('.btn-anular-transf');
        if (btn) {
          const id = btn.dataset.id;
          const motivo = prompt('Ingrese motivo de la anulación de la transferencia:');
          if (motivo) {
            try {
              await transferenciasService.anular(id, motivo);
              toast.success('Transferencia anulada.');
              const list = await transferenciasService.listar();
              this.transferencias = Array.isArray(list) ? list : list.data || [];
              this.renderTablaTransferencias();
            } catch (err) {
              toast.error('Error al anular: ' + err.message);
            }
          }
        }
      });
    }
  }
}
