/**
 * SIGA - views/plantaciones-view.js
 * Vista básica de Registro de Estructura Productiva, Plantaciones y Distribución de Lotes
 */

import { plantacionesService } from '../services/plantaciones-service.js';
import { temporadasService } from '../services/temporadas-service.js';
import { sucursalesService } from '../services/sucursales-service.js';
import { clientesService } from '../services/clientes-service.js';
import { BulbosCalibresGrid } from '../components/bulbos-calibres-grid.js';
import { loadTemplate } from '../utils/template-loader.js';
import { toast } from '../utils/toast.js';
import { escapeHtml } from '../utils/escape-html.js';

export class PlantacionesView {
  constructor(router) {
    this.router = router;
    this.container = null;
    this.temporadas = [];
    this.sucursales = [];
    this.clientes = [];
    this.filas = [];
    this.isNuevaFila = false;
    this.selectedTemporadaId = null;
    this.selectedSucursalId = null;
    this.lotes = [];
    this.currentPlantacion = null;
    this.calibresGrid = null;
  }

  async render(rootElement) {
    this.container = rootElement;
    this.container.innerHTML = await loadTemplate('plantaciones.html');

    this.calibresGrid = new BulbosCalibresGrid({
      root: this.container,
      inputIdPrefix: 'cant-c',
      totalSelector: '#label-total-bulbos',
      gridContainerSelector: '#plantaciones-calibres-grid'
    });
    this.calibresGrid.init();

    this._bindEvents();
    await this._loadInitialDropdowns();
  }

  _bindEvents() {
    this.container.querySelector('#btn-refresh-plantaciones').addEventListener('click', async () => {
      await Promise.all([
        this._reloadFilas(),
        this._reloadLotes()
      ]);
      toast.info('Datos de plantación actualizados');
    });

    // Cambio de temporada seleccionada
    const tempSelect = this.container.querySelector('#select-temporada');
    tempSelect.addEventListener('change', async (e) => {
      this.selectedTemporadaId = e.target.value;
      await Promise.all([
        this._reloadFilas(),
        this._reloadLotes()
      ]);
    });

    // Cambio de sucursal
    const sucSelect = this.container.querySelector('#select-sucursal');
    sucSelect.addEventListener('change', async (e) => {
      this.selectedSucursalId = e.target.value;
      await this._reloadFilas();
    });

    // Selector de fila existente
    const filaSelect = this.container.querySelector('#select-fila');
    filaSelect.addEventListener('change', () => {
      this._handleFilaSelectionChange();
    });

    // Toggle Nueva Fila
    this.container.querySelector('#btn-toggle-nueva-fila').addEventListener('click', () => {
      this._setNuevaFilaMode(!this.isNuevaFila);
    });

    // Cancelar Nueva Fila
    this.container.querySelector('#btn-cancel-nueva-fila').addEventListener('click', () => {
      this._setNuevaFilaMode(false);
    });

    // Submit del formulario
    this.container.querySelector('#form-distribucion').addEventListener('submit', (e) => {
      this._handleSubmitDistribucion(e);
    });
  }

  async _loadInitialDropdowns() {
    try {
      const [temporadas, sucursales, clientesRes] = await Promise.all([
        temporadasService.listar().catch(() => []),
        sucursalesService.listar().catch(() => []),
        clientesService.listar({ pageSize: 100, estado: 'activos' }).catch(() => ({ items: [] }))
      ]);

      this.temporadas = temporadas || [];
      this.sucursales = Array.isArray(sucursales) ? sucursales : (sucursales.items || []);
      this.clientes = Array.isArray(clientesRes) ? clientesRes : (clientesRes.items || []);

      // Poblar selector de temporadas
      const tempSelect = this.container.querySelector('#select-temporada');
      if (this.temporadas.length > 0) {
        // Encontrar activa o primera
        const activa = this.temporadas.find(t => t.estado !== 'Cerrada') || this.temporadas[0];
        this.selectedTemporadaId = activa.id;

        tempSelect.innerHTML = this.temporadas.map(t => `
          <option value="${escapeHtml(t.id)}" ${String(t.id) === String(this.selectedTemporadaId) ? 'selected' : ''}>
            ${escapeHtml(t.nombre)} (${escapeHtml(t.estado)})
          </option>
        `).join('');
      } else {
        tempSelect.innerHTML = '<option value="">No hay temporadas creadas</option>';
      }

      // Poblar selector de sucursales
      const sucSelect = this.container.querySelector('#select-sucursal');
      if (this.sucursales.length > 0) {
        this.selectedSucursalId = this.sucursales[0].id;
        sucSelect.innerHTML = this.sucursales.map(s => `
          <option value="${escapeHtml(s.id)}">
            ${escapeHtml(s.nombre)} (${escapeHtml(s.ciudad || 'Principal')})
          </option>
        `).join('');
      } else {
        sucSelect.innerHTML = '<option value="1">Sucursal Central</option>';
        this.selectedSucursalId = 1;
      }

      // Poblar selector de clientes
      const cliSelect = this.container.querySelector('#select-cliente');
      if (this.clientes.length > 0) {
        cliSelect.innerHTML = `
          <option value="">-- Seleccionar Cliente Titular --</option>
          ${this.clientes.map(c => `
            <option value="${escapeHtml(c.id)}">
              ${c.codigo ? `[${escapeHtml(c.codigo)}] ` : ''}${escapeHtml(c.nombre)} ${escapeHtml(c.apellido)} (${escapeHtml(c.documento || c.email || 'Titular')})
            </option>
          `).join('')}
        `;
      } else {
        cliSelect.innerHTML = '<option value="">Sin clientes disponibles</option>';
      }

      if (this.selectedTemporadaId) {
        await Promise.all([
          this._reloadFilas(),
          this._reloadLotes()
        ]);
      }
    } catch (err) {
      toast.error('Error al inicializar listas para plantación: ' + err.message);
    }
  }

  async _reloadFilas() {
    const filaSelect = this.container.querySelector('#select-fila');
    if (!filaSelect) return;

    if (!this.selectedSucursalId || !this.selectedTemporadaId) {
      filaSelect.innerHTML = '<option value="">Seleccione temporada y sucursal</option>';
      filaSelect.disabled = true;
      return;
    }

    filaSelect.disabled = false;
    filaSelect.innerHTML = '<option value="">Cargando filas...</option>';

    try {
      const filas = await plantacionesService.listarFilasPorSucursalYTemporada(
        this.selectedSucursalId,
        this.selectedTemporadaId
      );
      this.filas = Array.isArray(filas) ? filas : (filas?.items || []);

      if (this.filas.length > 0) {
        filaSelect.innerHTML = `
          <option value="">-- Seleccionar Fila (${this.filas.length} disponibles) --</option>
          ${this.filas.map(f => `
            <option value="${escapeHtml(f.id)}">
              Fila ${escapeHtml(f.identificacion)} (Cap: ${(f.capacidad || 0).toLocaleString()} | Lotes: ${f.totalLotes || 0})
            </option>
          `).join('')}
          <option value="__nueva__">+ Crear Nueva Fila...</option>
        `;
      } else {
        filaSelect.innerHTML = `
          <option value="">No hay filas registradas aún</option>
          <option value="__nueva__">+ Crear Nueva Fila...</option>
        `;
      }

      if (!this.isNuevaFila) {
        this._handleFilaSelectionChange();
      }
    } catch (err) {
      filaSelect.innerHTML = '<option value="">Error al cargar filas</option>';
      toast.error('Error al cargar filas: ' + err.message);
    }
  }

  _handleFilaSelectionChange() {
    const filaSelect = this.container.querySelector('#select-fila');
    const loteInput = this.container.querySelector('#input-lote-orden');
    const helpLote = this.container.querySelector('#help-lote-orden');
    const selectedVal = filaSelect.value;

    if (selectedVal === '__nueva__') {
      this._setNuevaFilaMode(true);
      return;
    }

    if (!selectedVal) {
      loteInput.value = '';
      helpLote.textContent = 'Seleccione una fila para calcular el orden sugerido';
      return;
    }

    const selectedFila = this.filas.find(f => String(f.id) === String(selectedVal));
    if (selectedFila) {
      const nextOrden = (selectedFila.totalLotes || 0) + 1;
      loteInput.value = nextOrden;
      helpLote.textContent = `Sugerido: Lote #${nextOrden} (${selectedFila.totalLotes || 0} lotes existentes en Fila ${selectedFila.identificacion})`;
    }
  }

  _setNuevaFilaMode(enable) {
    this.isNuevaFila = enable;
    const panel = this.container.querySelector('#panel-nueva-fila');
    const toggleBtn = this.container.querySelector('#btn-toggle-nueva-fila');
    const filaSelect = this.container.querySelector('#select-fila');
    const inputNum = this.container.querySelector('#input-nueva-fila-num');
    const inputCap = this.container.querySelector('#input-nueva-fila-cap');
    const loteInput = this.container.querySelector('#input-lote-orden');
    const helpLote = this.container.querySelector('#help-lote-orden');

    if (enable) {
      panel.classList.remove('hidden');
      toggleBtn.classList.add('active');
      toggleBtn.textContent = '✕ Cancelar Nueva';
      filaSelect.value = '__nueva__';

      // Sugerir próximo número entero de fila disponible
      const existingNums = this.filas
        .map(f => parseInt(f.identificacion, 10))
        .filter(n => !isNaN(n) && n > 0);
      const nextFilaNum = existingNums.length > 0 ? Math.max(...existingNums) + 1 : (this.filas.length + 1);
      inputNum.value = nextFilaNum;
      inputCap.value = 5000;

      // Para nueva fila, el primer lote siempre es orden 1
      loteInput.value = 1;
      helpLote.textContent = 'Sugerido: Lote #1 (Primer lote de la nueva fila)';
      inputNum.focus();
    } else {
      panel.classList.add('hidden');
      toggleBtn.classList.remove('active');
      toggleBtn.textContent = '+ Nueva Fila';
      inputNum.value = '';
      if (filaSelect.value === '__nueva__') {
        filaSelect.value = '';
      }
      this._handleFilaSelectionChange();
    }
  }

  async _reloadLotes() {
    if (!this.selectedTemporadaId) return;

    const tbody = this.container.querySelector('#tbody-lotes');
    tbody.innerHTML = `
      <tr class="table-loading-row">
        <td colspan="6" class="text-center py-4">
          <span>Consultando lotes de la temporada...</span>
        </td>
      </tr>
    `;

    try {
      // Obtener o inicializar la plantación de la temporada
      this.currentPlantacion = await plantacionesService.obtenerPorTemporada(this.selectedTemporadaId).catch(() => null);

      if (!this.currentPlantacion) {
        this.lotes = [];
        this._renderTablaLotes();
        return;
      }

      // Obtener lotes de la temporada
      const lotes = await plantacionesService.listarLotes(this.selectedTemporadaId).catch(() => []);
      this.lotes = Array.isArray(lotes) ? lotes : (lotes?.items || []);

      this._renderTablaLotes();
    } catch (err) {
      tbody.innerHTML = `
        <tr>
          <td colspan="6" class="text-center py-4 text-danger">
            Error al consultar lotes: ${escapeHtml(err.message)}
          </td>
        </tr>
      `;
    }
  }

  _renderTablaLotes() {
    const tbody = this.container.querySelector('#tbody-lotes');
    const countEl = this.container.querySelector('#total-lotes-count');

    countEl.textContent = `${this.lotes.length} lotes registrados`;

    if (this.lotes.length === 0) {
      tbody.innerHTML = `
        <tr>
          <td colspan="6" class="text-center py-4 text-muted">
            No se han registrado lotes para esta temporada aún. Utilice el formulario superior para registrar el primero.
          </td>
        </tr>
      `;
      return;
    }

    tbody.innerHTML = this.lotes.map(l => {
      // Resolver nombre de cliente
      let clienteNombre = l.clienteNombre;
      if (!clienteNombre && l.clienteId) {
        const c = this.clientes.find(cli => String(cli.id) === String(l.clienteId));
        clienteNombre = c ? `${c.nombre} ${c.apellido}` : `Cliente #${l.clienteId}`;
      }

      // Desglose por clases
      const desgloseMap = {};
      (l.cantidades || []).forEach(item => {
        desgloseMap[item.claseBulbo] = item.cantidad;
      });

      const c0 = desgloseMap[0] || 0;
      const c1 = desgloseMap[1] || 0;
      const c2 = desgloseMap[2] || 0;
      const c3 = desgloseMap[3] || 0;
      const c4 = desgloseMap[4] || 0;

      return `
        <tr>
          <td>
            <span class="font-bold text-xs" style="color: var(--accent-800); background: var(--accent-100); padding: 3px 8px; border-radius: var(--radius-sm);">
              Fila ${escapeHtml(l.filaIdentificacion || l.identificacionFila || 'F-??')}
            </span>
          </td>
          <td>
            <span class="font-semibold text-main">
              ${escapeHtml(l.numeroLote || `LOT-${l.id}`)}
              ${l.orden ? `<span class="badge badge-secondary ml-1" style="font-size: 0.7rem;">Orden ${escapeHtml(l.orden)}</span>` : ''}
            </span>
          </td>
          <td>
            <div class="font-medium text-main">${escapeHtml(clienteNombre || 'Sin asignar')}</div>
          </td>
          <td style="text-align: right;">
            <span class="font-bold" style="color: var(--primary);">${(l.totalBulbos || 0).toLocaleString()}</span>
          </td>
          <td>
            <div class="text-xs font-mono" style="background: var(--surface-alt); padding: 4px 8px; border-radius: var(--radius-sm); display: inline-block;">
              <strong>C0:</strong> ${escapeHtml(c0)} |
              <strong>C1:</strong> ${escapeHtml(c1)} |
              <strong>C2:</strong> ${escapeHtml(c2)} |
              <strong>C3:</strong> ${escapeHtml(c3)} |
              <strong>C4:</strong> ${escapeHtml(c4)}
            </div>
          </td>
          <td>
            <span class="text-xs text-muted">
              ${escapeHtml(l.fechaRegistro ? new Date(l.fechaRegistro).toLocaleDateString() : '-')}
            </span>
          </td>
        </tr>
      `;
    }).join('');
  }

  async _handleSubmitDistribucion(e) {
    e.preventDefault();

    if (!this.selectedTemporadaId) {
      toast.warning('Seleccione una temporada válida.');
      return;
    }
    if (!this.selectedSucursalId) {
      toast.warning('Seleccione una sucursal válida.');
      return;
    }

    const clienteId = this.container.querySelector('#select-cliente').value;
    if (!clienteId) {
      toast.warning('Seleccione el cliente titular del lote.');
      return;
    }

    let filaId = null;
    let numeroFila = null;
    let identificacionFila = '';
    let capacidadFila = 0;

    if (this.isNuevaFila) {
      const numVal = this.container.querySelector('#input-nueva-fila-num').value.trim();
      const capVal = this.container.querySelector('#input-nueva-fila-cap').value.trim();
      numeroFila = parseInt(numVal, 10);
      capacidadFila = parseInt(capVal, 10);

      if (isNaN(numeroFila) || numeroFila <= 0) {
        toast.warning('Ingrese un número entero positivo para la nueva fila.');
        this.container.querySelector('#input-nueva-fila-num').focus();
        return;
      }
      if (isNaN(capacidadFila) || capacidadFila <= 0) {
        toast.warning('Ingrese una capacidad entera positiva para la nueva fila.');
        this.container.querySelector('#input-nueva-fila-cap').focus();
        return;
      }
      identificacionFila = String(numeroFila);
    } else {
      const selectedVal = this.container.querySelector('#select-fila').value;
      if (!selectedVal || selectedVal === '__nueva__') {
        toast.warning('Seleccione una fila existente o cree una nueva fila.');
        return;
      }
      filaId = parseInt(selectedVal, 10);
      const selectedFila = this.filas.find(f => f.id === filaId);
      if (!selectedFila) {
        toast.warning('Fila seleccionada inválida.');
        return;
      }
      identificacionFila = String(selectedFila.identificacion);
      const parsedNum = parseInt(identificacionFila, 10);
      numeroFila = !isNaN(parsedNum) ? parsedNum : null;
      capacidadFila = Number(selectedFila.capacidad) || 5000;
    }

    // Validar orden de lote
    const loteVal = this.container.querySelector('#input-lote-orden').value.trim();
    const numeroLote = parseInt(loteVal, 10);
    if (isNaN(numeroLote) || numeroLote <= 0) {
      toast.warning('Ingrese un número entero positivo para el orden del lote.');
      this.container.querySelector('#input-lote-orden').focus();
      return;
    }

    // Cantidades de bulbos
    const detalles = this.calibresGrid.getItemsArray(false);
    const totalBulbos = this.calibresGrid.getTotal();

    if (totalBulbos <= 0) {
      toast.warning('Debe ingresar al menos una cantidad de bulbos mayor a cero.');
      return;
    }

    const submitBtn = this.container.querySelector('#btn-registrar-lote');
    submitBtn.classList.add('is-loading');
    submitBtn.disabled = true;

    try {
      let plantacion = this.currentPlantacion;
      if (!plantacion || !plantacion.id) {
        plantacion = await plantacionesService.obtenerPorTemporada(this.selectedTemporadaId).catch(() => null);
        if (!plantacion || !plantacion.id) {
          plantacion = await plantacionesService.crear({
            temporadaId: Number(this.selectedTemporadaId),
            observaciones: 'Plantación general del ciclo'
          }).catch(() => null);
        }
      }

      const payload = {
        plantacionId: plantacion?.id ? Number(plantacion.id) : null,
        temporadaId: Number(this.selectedTemporadaId),
        sucursalId: Number(this.selectedSucursalId),
        filaId: filaId,
        numeroFila: numeroFila,
        identificacionFila: identificacionFila,
        capacidadFila: capacidadFila,
        numeroLote: numeroLote,
        orden: numeroLote,
        clienteId: Number(clienteId),
        detalles: detalles,
        cantidades: detalles
      };

      await plantacionesService.registrarDistribucion(plantacion?.id, payload);

      toast.success(`Lote #${numeroLote} en Fila ${identificacionFila} registrado exitosamente con ${totalBulbos.toLocaleString()} bulbos.`);

      // Resetear formulario
      this._setNuevaFilaMode(false);
      this.container.querySelector('#select-fila').value = '';
      this.container.querySelector('#input-lote-orden').value = '';
      this.container.querySelector('#select-cliente').value = '';
      this.calibresGrid.reset();

      // Recargar filas y lotes en simultáneo
      await Promise.all([
        this._reloadFilas(),
        this._reloadLotes()
      ]);
    } catch (err) {
      toast.error(err.message || 'Error al registrar lote plantado');
    } finally {
      submitBtn.classList.remove('is-loading');
      submitBtn.disabled = false;
    }
  }
}
