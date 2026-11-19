/**
 * SIGA - components/bulbos-calibres-grid.js
 * Componente reutilizable de Grilla de 5 Calibres de Bulbos (Clase 0 a Clase 4)
 * con recálculo automático del total en vivo.
 * Compartido entre PlantacionesView, AjustesStockView y CosechasView.
 */

export const CALIBRES_BULBO_FISICOS = [
  { id: 0, label: 'Clase 0 (Extra Gde)', rango: '>12cm' },
  { id: 1, label: 'Clase 1 (Grande)', rango: '10-12cm' },
  { id: 2, label: 'Clase 2 (Mediano)', rango: '8-10cm' },
  { id: 3, label: 'Clase 3 (Chico)', rango: '6-8cm' },
  { id: 4, label: 'Clase 4 (Semillero)', rango: '<6cm' }
];

export class BulbosCalibresGrid {
  /**
   * @param {Object} options
   * @param {HTMLElement} options.root - Contenedor raíz donde buscar los elementos
   * @param {string} options.inputIdPrefix - Prefijo de ID para los inputs 0..4 (ej. 'cant-c', 'ajuste-cant-', 'input-cant-c')
   * @param {string} options.totalSelector - Selector del elemento donde mostrar la suma total (ej. '#label-total-bulbos')
   * @param {string} [options.gridContainerSelector] - Si se indica, inyecta dinámicamente el HTML de los 5 inputs en ese contenedor
   */
  constructor({ root, inputIdPrefix, totalSelector, gridContainerSelector = null }) {
    this.root = root;
    this.inputIdPrefix = inputIdPrefix;
    this.totalSelector = totalSelector;
    this.gridContainerSelector = gridContainerSelector;
  }

  /**
   * Renderiza los 5 inputs (si se configuró `gridContainerSelector`) y enlaza el evento `input`
   * para recalcular el total en vivo.
   */
  init() {
    if (this.gridContainerSelector) {
      const gridEl = this.root.querySelector(this.gridContainerSelector);
      if (gridEl) {
        gridEl.innerHTML = BulbosCalibresGrid.renderHtml(this.inputIdPrefix);
      }
    }
    this.bindEvents();
    this.updateTotalPreview();
  }

  /**
   * Genera el HTML estándar de la grilla de 5 calibres (Clases 0 a 4).
   * @param {string} inputIdPrefix - Prefijo para los IDs de cada input (ej. 'cant-c' genera 'cant-c0'..'cant-c4')
   */
  static renderHtml(inputIdPrefix) {
    return `
      <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(120px, 1fr)); gap: 0.65rem;">
        ${CALIBRES_BULBO_FISICOS.map(c => `
          <div class="p-2" style="background: var(--surface-alt); border-radius: var(--radius-sm); border: 1px solid var(--border);">
            <label for="${inputIdPrefix}${c.id}" class="text-xs font-bold d-block mb-1" style="color: var(--primary);" title="${c.rango}">
              ${c.label}
            </label>
            <input
              type="number"
              id="${inputIdPrefix}${c.id}"
              class="form-control text-right bulbo-calibre-input"
              data-clase="${c.id}"
              min="0"
              value="0"
            />
          </div>
        `).join('')}
      </div>
    `;
  }

  bindEvents() {
    for (let c = 0; c <= 4; c++) {
      const input = this.root.querySelector(`#${this.inputIdPrefix}${c}`);
      input?.addEventListener('input', () => this.updateTotalPreview());
    }
  }

  updateTotalPreview() {
    const total = this.getTotal();
    const totalEl = this.root.querySelector(this.totalSelector);
    if (totalEl) {
      totalEl.textContent = total.toLocaleString('es-AR');
    }
    return total;
  }

  /**
   * Devuelve un objeto `{ c0, c1, c2, c3, c4, total }` con las cantidades actuales.
   */
  getValues() {
    const values = {};
    let total = 0;
    for (let c = 0; c <= 4; c++) {
      const raw = Number(this.root.querySelector(`#${this.inputIdPrefix}${c}`)?.value) || 0;
      const val = Math.max(0, raw);
      values[`c${c}`] = val;
      total += val;
    }
    values.total = total;
    return values;
  }

  /**
   * Devuelve un array `[{ claseBulbo: 0..4, cantidad }]` solo con las clases cuya cantidad sea > 0
   * (o las 5 clases si `includeZero = true`).
   */
  getItemsArray(includeZero = false) {
    const items = [];
    for (let c = 0; c <= 4; c++) {
      const raw = Number(this.root.querySelector(`#${this.inputIdPrefix}${c}`)?.value) || 0;
      const cantidad = Math.max(0, raw);
      if (includeZero || cantidad > 0) {
        items.push({ claseBulbo: c, clase: c, cantidad });
      }
    }
    return items;
  }

  getTotal() {
    return this.getValues().total;
  }

  reset() {
    for (let c = 0; c <= 4; c++) {
      const input = this.root.querySelector(`#${this.inputIdPrefix}${c}`);
      if (input) input.value = '0';
    }
    this.updateTotalPreview();
  }
}
