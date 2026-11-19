/**
 * SIGA - components/cliente-autocomplete.js
 * Componente Autocomplete / Typeahead Asíncrono de Clientes con Live Search
 * Diseñado para operar con altos volúmenes de clientes (6.000+) con 0 lag.
 */

import { clientesService } from '../services/clientes-service.js';
import { escapeHtml } from '../utils/html.js';

export class ClienteAutocomplete {
  /**
   * @param {Object} options
   * @param {HTMLElement} options.container Contenedor DOM donde se monta el componente
   * @param {string} [options.name='clienteId'] Atributo name / id base del input oculto
   * @param {string} [options.placeholder='Buscar por DNI, Nombre o Código...']
   * @param {Function} [options.onSelect] Callback al seleccionar un cliente: (cliente) => {}
   * @param {Function} [options.onClear] Callback al deseleccionar: () => {}
   * @param {boolean} [options.required=false]
   * @param {string} [options.emptyText='No se encontraron clientes coincidentes']
   */
  constructor({
    container,
    name = 'clienteId',
    placeholder = 'Buscar cliente por DNI, Nombre o Código...',
    onSelect = null,
    onClear = null,
    required = false,
    emptyText = 'No se encontraron clientes con ese criterio'
  }) {
    if (!container) throw new Error('[ClienteAutocomplete] container is required.');
    this.container = container;
    this.name = name;
    this.placeholder = placeholder;
    this.onSelect = onSelect;
    this.onClear = onClear;
    this.required = required;
    this.emptyText = emptyText;

    this.selectedCliente = null;
    this.results = [];
    this.highlightedIndex = -1;
    this.debounceTimer = null;
    this.isSearching = false;

    this._render();
    this._bindEvents();
  }

  _render() {
    this.container.innerHTML = `
      <div class="autocomplete-wrapper" id="ac-wrap-${this.name}">
        <input type="hidden" id="${this.name}" name="${this.name}" value="" ${this.required ? 'required' : ''} />

        <!-- Caja de Búsqueda -->
        <div class="autocomplete-input-box" id="ac-box-${this.name}">
          <span class="autocomplete-icon-search">
            <svg class="icon-svg sm" viewBox="0 0 24 24"><circle cx="11" cy="11" r="8"></circle><line x1="21" y1="21" x2="16.65" y2="16.65"></line></svg>
          </span>
          <input
            type="text"
            class="form-control autocomplete-input"
            id="ac-input-${this.name}"
            placeholder="${escapeHtml(this.placeholder)}"
            autocomplete="off"
            spellcheck="false"
          />
          <span class="autocomplete-spinner" id="ac-spin-${this.name}"></span>
        </div>

        <!-- Menú Desplegable Flotante -->
        <ul class="autocomplete-dropdown hidden" id="ac-drop-${this.name}" role="listbox"></ul>

        <!-- Ficha de Cliente Seleccionado -->
        <div class="autocomplete-selected-card hidden" id="ac-card-${this.name}">
          <div class="autocomplete-selected-info">
            <div class="autocomplete-selected-avatar" id="ac-avatar-${this.name}">CL</div>
            <div class="autocomplete-selected-text">
              <span class="autocomplete-selected-name" id="ac-name-${this.name}">-</span>
              <div class="autocomplete-selected-details" id="ac-details-${this.name}">-</div>
            </div>
          </div>
          <button type="button" class="autocomplete-change-btn" id="ac-btn-change-${this.name}" title="Cambiar cliente">
            ✕ Cambiar
          </button>
        </div>
      </div>
    `;

    this.elWrap = this.container.querySelector(`#ac-wrap-${this.name}`);
    this.elHidden = this.container.querySelector(`#${this.name}`);
    this.elBox = this.container.querySelector(`#ac-box-${this.name}`);
    this.elInput = this.container.querySelector(`#ac-input-${this.name}`);
    this.elSpin = this.container.querySelector(`#ac-spin-${this.name}`);
    this.elDrop = this.container.querySelector(`#ac-drop-${this.name}`);
    this.elCard = this.container.querySelector(`#ac-card-${this.name}`);
    this.elAvatar = this.container.querySelector(`#ac-avatar-${this.name}`);
    this.elName = this.container.querySelector(`#ac-name-${this.name}`);
    this.elDetails = this.container.querySelector(`#ac-details-${this.name}`);
    this.elBtnChange = this.container.querySelector(`#ac-btn-change-${this.name}`);
  }

  _bindEvents() {
    // Escribir en el input de búsqueda (con debounce de 200ms, soporta desde 1 caracter o vacío)
    this.elInput.addEventListener('input', (e) => {
      this.clearInvalid();
      const val = e.target.value.trim();
      clearTimeout(this.debounceTimer);

      this.debounceTimer = setTimeout(() => {
        this._performSearch(val);
      }, 200);
    });

    // Navegación con teclado en el menú
    this.elInput.addEventListener('keydown', (e) => {
      if (this.elDrop.classList.contains('hidden') || this.results.length === 0) {
        if (e.key === 'Enter') {
          e.preventDefault();
        }
        return;
      }

      switch (e.key) {
        case 'ArrowDown':
          e.preventDefault();
          this.highlightedIndex = (this.highlightedIndex + 1) % this.results.length;
          this._updateHighlightedItem();
          break;
        case 'ArrowUp':
          e.preventDefault();
          this.highlightedIndex = (this.highlightedIndex - 1 + this.results.length) % this.results.length;
          this._updateHighlightedItem();
          break;
        case 'Enter':
          e.preventDefault();
          if (this.highlightedIndex >= 0 && this.highlightedIndex < this.results.length) {
            this.setValue(this.results[this.highlightedIndex]);
          }
          break;
        case 'Escape':
          this._hideDropdown();
          break;
      }
    });

    // Abrir sugerencias inmediatas al hacer foco o clic en el input
    this.elInput.addEventListener('focus', () => {
      this._performSearch(this.elInput.value.trim());
    });
    this.elInput.addEventListener('click', () => {
      if (this.elDrop.classList.contains('hidden')) {
        this._performSearch(this.elInput.value.trim());
      }
    });

    // Cerrar al hacer clic fuera del componente
    document.addEventListener('click', (e) => {
      if (!this.container.contains(e.target)) {
        this._hideDropdown();
      }
    });

    // Botón para cambiar o quitar el cliente seleccionado
    this.elBtnChange.addEventListener('click', (e) => {
      e.preventDefault();
      e.stopPropagation();
      this.clear();
      this.elInput.focus();
    });
  }

  async _performSearch(query) {
    this._setLoading(true);
    try {
      this.results = await clientesService.buscarRapido(query, 10);
      this.highlightedIndex = this.results.length > 0 ? 0 : -1;
      this._renderDropdownResults(query);
      this._showDropdown();
    } catch (err) {
      console.warn('[ClienteAutocomplete] Error al buscar clientes:', err);
      this.results = [];
      this._renderDropdownResults(query);
      this._showDropdown();
    } finally {
      this._setLoading(false);
    }
  }

  _renderDropdownResults(query) {
    if (!this.results || this.results.length === 0) {
      this.elDrop.innerHTML = `
        <li class="autocomplete-empty">
          <svg class="icon-svg sm text-muted mb-1" viewBox="0 0 24 24"><circle cx="11" cy="11" r="8"></circle><line x1="21" y1="21" x2="16.65" y2="16.65"></line></svg>
          <div>${escapeHtml(this.emptyText)}</div>
          <small class="text-muted">Intente con otro DNI, nombre o código</small>
        </li>
      `;
      return;
    }

    const html = this.results.map((c, index) => {
      const nombreCompleto = `${c.nombre || ''} ${c.apellido || ''}`.trim() || c.nombreCompleto || `Cliente #${c.id}`;
      const doc = c.documento ? `DNI: ${c.documento}` : 'Sin documento';
      const cod = c.codigo || c.codigoCliente || `CLI-${c.id}`;
      const suc = c.sucursalNombre ? `🏢 ${c.sucursalNombre}` : '';

      return `
        <li class="autocomplete-item ${index === this.highlightedIndex ? 'is-selected' : ''}" data-index="${index}" role="option">
          <div class="autocomplete-item-main">
            <span class="autocomplete-item-name">${escapeHtml(nombreCompleto)}</span>
            <div class="autocomplete-item-meta">
              <span>🪪 ${escapeHtml(doc)}</span>
              <span>🏷️ ${escapeHtml(cod)}</span>
              ${suc ? `<span>${escapeHtml(suc)}</span>` : ''}
            </div>
          </div>
          <span class="autocomplete-item-badge">Seleccionar</span>
        </li>
      `;
    }).join('');

    const hintText = query
      ? 'Use las teclas ↑ / ↓ para navegar y Enter para seleccionar'
      : 'Escriba DNI, Nombre o Código para filtrar resultados';

    const hintHtml = `
      <div class="autocomplete-hint">
        ${escapeHtml(hintText)}
      </div>
    `;

    this.elDrop.innerHTML = html + hintHtml;

    // Vincular clic en cada elemento de resultado
    this.elDrop.querySelectorAll('.autocomplete-item').forEach((item) => {
      item.addEventListener('click', (e) => {
        e.preventDefault();
        e.stopPropagation();
        const idx = Number(item.getAttribute('data-index'));
        if (this.results[idx]) {
          this.setValue(this.results[idx]);
        }
      });
    });
  }

  _updateHighlightedItem() {
    const items = this.elDrop.querySelectorAll('.autocomplete-item');
    items.forEach((item, index) => {
      if (index === this.highlightedIndex) {
        item.classList.add('is-selected');
        item.scrollIntoView({ block: 'nearest' });
      } else {
        item.classList.remove('is-selected');
      }
    });
  }

  _showDropdown() {
    this.elDrop.classList.remove('hidden');
  }

  _hideDropdown() {
    this.elDrop.classList.add('hidden');
    this.highlightedIndex = -1;
  }

  _setLoading(isLoading) {
    this.isSearching = isLoading;
    if (isLoading) {
      this.elSpin.classList.add('is-active');
    } else {
      this.elSpin.classList.remove('is-active');
    }
  }

  /**
   * Establece un cliente seleccionado programáticamente o por selección de usuario
   * @param {Object} cliente 
   */
  setValue(cliente) {
    if (!cliente || !cliente.id) {
      this.clear();
      return;
    }

    this.selectedCliente = cliente;
    this.elHidden.value = String(cliente.id);

    const nombreCompleto = `${cliente.nombre || ''} ${cliente.apellido || ''}`.trim() || cliente.nombreCompleto || `Cliente #${cliente.id}`;
    const initials = (cliente.nombre?.[0] || 'C') + (cliente.apellido?.[0] || 'L');
    const doc = cliente.documento ? `DNI: ${cliente.documento}` : '';
    const cod = cliente.codigo || cliente.codigoCliente || `CLI-${cliente.id}`;
    const suc = cliente.sucursalNombre ? `Sucursal: ${cliente.sucursalNombre}` : '';

    this.elAvatar.textContent = initials.toUpperCase();
    this.elName.textContent = nombreCompleto;

    const details = [doc, cod, suc].filter(Boolean).map(d => `<span class="badge badge-secondary" style="font-size: 0.72rem;">${escapeHtml(d)}</span>`).join(' ');
    this.elDetails.innerHTML = details;

    this._hideDropdown();
    this.elBox.classList.add('hidden');
    this.elCard.classList.remove('hidden');
    this.clearInvalid();

    if (this.onSelect) {
      this.onSelect(cliente);
    }
  }

  /**
   * Carga y establece el valor a partir del ID del cliente
   * @param {number|string} id 
   */
  async setValueById(id) {
    if (!id || Number(id) <= 0) {
      this.clear();
      return;
    }
    try {
      this._setLoading(true);
      const cliente = await clientesService.obtenerPorId(id);
      if (cliente) {
        this.setValue(cliente);
      }
    } catch (err) {
      console.warn(`[ClienteAutocomplete] No se pudo cargar cliente #${id}:`, err);
    } finally {
      this._setLoading(false);
    }
  }

  /**
   * Limpia la selección y restaura la barra de búsqueda
   */
  clear() {
    this.selectedCliente = null;
    this.elHidden.value = '';
    this.elInput.value = '';
    this.results = [];
    this.highlightedIndex = -1;

    this.elCard.classList.add('hidden');
    this.elBox.classList.remove('hidden');
    this._hideDropdown();

    if (this.onClear) {
      this.onClear();
    }
  }

  /**
   * Obtiene el ID del cliente seleccionado o null
   * @returns {number|null}
   */
  getValue() {
    const val = this.elHidden.value;
    return val ? Number(val) : null;
  }

  /**
   * Obtiene el objeto cliente completo seleccionado o null
   * @returns {Object|null}
   */
  getSelected() {
    return this.selectedCliente;
  }

  markInvalid() {
    this.elInput.classList.add('is-invalid');
    this.elCard.style.borderColor = 'var(--danger, #ef4444)';
  }

  clearInvalid() {
    this.elInput.classList.remove('is-invalid');
    this.elCard.style.borderColor = '';
  }

  setDisabled(disabled) {
    this.elInput.disabled = Boolean(disabled);
    this.elBtnChange.disabled = Boolean(disabled);
    if (disabled) {
      this.elWrap.style.opacity = '0.6';
      this.elWrap.style.pointerEvents = 'none';
    } else {
      this.elWrap.style.opacity = '1';
      this.elWrap.style.pointerEvents = '';
    }
  }
}

