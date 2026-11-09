/**
 * SIGA - views/transferencias-view.js
 * Vista funcional para Transferencias de Lotes Completos e Historial de Titularidad
 */

import { transferenciasService } from '../services/transferencias-service.js';
import { plantacionesService } from '../services/plantaciones-service.js';
import { temporadasService } from '../services/temporadas-service.js';
import { clientesService } from '../services/clientes-service.js';
import { toast } from '../utils/toast.js';

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
    this.container.innerHTML = `
      <div class="view-header">
        <div>
          <h2>Transferencias de Lotes Completos</h2>
          <p class="text-muted">Cesión total de derechos productivos y de cosecha de lotes plantados.</p>
        </div>
      </div>

      <!-- Panel de Registro de Transferencia -->
      <div class="card mb-4">
        <div class="card-header">
          <h3 class="card-title">Registrar Transferencia de Lote</h3>
        </div>
        <div class="card-body">
          <form id="form-transferir-lote">
            <div class="grid grid-3 gap-3 mb-3">
              <div class="form-group">
                <label for="transf-temporada">1. Temporada</label>
                <select id="transf-temporada" class="form-control" required>
                  <option value="">Cargando temporadas...</option>
                </select>
              </div>

              <div class="form-group">
                <label for="transf-lote">2. Lote a Transferir</label>
                <select id="transf-lote" class="form-control" required>
                  <option value="">Seleccione temporada primero...</option>
                </select>
              </div>

              <div class="form-group">
                <label for="transf-nuevo-titular">3. Nuevo Titular (Comprador)</label>
                <select id="transf-nuevo-titular" class="form-control" required>
                  <option value="">Cargando participantes...</option>
                </select>
              </div>
            </div>

            <div class="form-group mb-3">
              <label for="transf-obs">Observaciones / Motivo de la Cesión</label>
              <input type="text" id="transf-obs" class="form-control" placeholder="Ej: Venta de lote en etapa de crecimiento" />
            </div>

            <div class="d-flex justify-content-between align-items-center">
              <div id="lote-info-preview" class="text-muted small"></div>
              <button type="submit" class="btn btn-primary">
                Confirmar Transferencia de Lote Completo
              </button>
            </div>
          </form>
        </div>
      </div>

      <!-- Tabla de Transferencias Realizadas -->
      <div class="card">
        <div class="card-header d-flex justify-content-between align-items-center">
          <h3 class="card-title">Historial de Transferencias Realizadas</h3>
          <button id="btn-refresh-transf" class="btn btn-outline-secondary btn-sm">Actualizar</button>
        </div>
        <div class="card-body p-0">
          <div class="table-responsive">
            <table class="table table-hover mb-0">
              <thead>
                <tr>
                  <th>#</th>
                  <th>Lote</th>
                  <th>Fila / Sucursal</th>
                  <th>Titular Anterior</th>
                  <th>Nuevo Titular</th>
                  <th>Fecha</th>
                  <th>Estado</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody id="tabla-transferencias-body">
                <tr><td colspan="8" class="text-center p-3 text-muted">Cargando transferencias...</td></tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    `;

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
      this.temporadas.map(t => `<option value="${t.id}">${t.nombre} (${t.nombreEstado || t.estado})</option>`).join('');
  }

  renderSelectNuevoTitular() {
    const select = document.getElementById('transf-nuevo-titular');
    if (!select) return;
    let html = '<option value="">-- Seleccione Nuevo Titular --</option>';
    html += '<option value="1">🏢 Empresa (Azafrán Mediterráneo S.A.)</option>';
    html += this.clientes.map(c => {
      const nom = c.persona ? `${c.persona.nombre} ${c.persona.apellido}` : (c.nombreCompleto || c.codigoCliente);
      return `<option value="${c.id}">👤 ${nom} (${c.codigoCliente})</option>`;
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
        this.lotes.map(l => `<option value="${l.id}">Lote #${l.id} - Fila ${l.identificacionFila || l.filaId} | Titular: ${l.nombreCliente || l.clienteId} (${l.cantidadPlantadaTotal} bulbos)</option>`).join('');
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
          <td><strong>#${t.id}</strong></td>
          <td>Lote #${t.loteId}</td>
          <td>${t.identificacionFila || '-'} (${t.nombreSucursal || 'Central'})</td>
          <td>${t.nombreTitularAnterior || `Titular #${t.titularAnteriorId}`}</td>
          <td><strong class="text-primary">${t.nombreNuevoTitular || `Titular #${t.nuevoTitularId}`}</strong></td>
          <td>${fecha}</td>
          <td><span class="badge ${badgeClass}">${t.estado}</span></td>
          <td>
            ${t.estado === 'Confirmada' ? `<button class="btn btn-outline-danger btn-sm btn-anular-transf" data-id="${t.id}">Anular</button>` : '-'}
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
