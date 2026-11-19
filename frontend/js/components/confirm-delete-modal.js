/**
 * SIGA - components/confirm-delete-modal.js
 * Modal reutilizable de confirmación de baja lógica o eliminación con ícono de prohibición.
 * Compartido entre ClientesView, EmpleadosView y RolesView.
 */

import { loadTemplate } from '../utils/template-loader.js';
import { getProhibitionIcon } from '../utils/icons.js';
import { toast } from '../utils/toast.js';

export class ConfirmDeleteModal {
  /**
   * @param {Object} options
   * @param {HTMLElement} options.container - Contenedor de la vista donde se montará el modal
   * @param {string} [options.title='Confirmar Baja'] - Título del modal
   * @param {string} [options.confirmText='Confirmar Baja'] - Texto del botón de confirmación
   * @param {string} [options.hintText=''] - Texto secundario explicativo debajo de la pregunta
   * @param {(id: string|number) => Promise<void>} options.onConfirm - Acción asíncrona a ejecutar al confirmar
   */
  constructor({
    container,
    title = 'Confirmar Baja',
    confirmText = 'Confirmar Baja',
    hintText = '',
    onConfirm
  }) {
    this.container = container;
    this.title = title;
    this.confirmText = confirmText;
    this.hintText = hintText;
    this.onConfirm = onConfirm;
    this.targetId = null;
  }

  async init() {
    if (!this.container.querySelector('#modal-confirm-delete')) {
      const html = await loadTemplate('confirm-delete-modal.html');
      this.container.insertAdjacentHTML('beforeend', html);
    }

    const titleEl = this.container.querySelector('#confirm-delete-title');
    if (titleEl) titleEl.textContent = this.title;

    const hintEl = this.container.querySelector('#confirm-delete-hint');
    if (hintEl) hintEl.textContent = this.hintText;

    const confirmBtn = this.container.querySelector('#btn-submit-confirm-delete');
    if (confirmBtn) confirmBtn.textContent = this.confirmText;

    const iconWrap = this.container.querySelector('#confirm-delete-icon-wrap');
    if (iconWrap) {
      iconWrap.innerHTML = getProhibitionIcon({ size: 'lg', title: this.title });
    }

    this.container.querySelector('#btn-close-confirm-delete')?.addEventListener('click', () => this.close());
    this.container.querySelector('#btn-cancel-confirm-delete')?.addEventListener('click', () => this.close());
    confirmBtn?.addEventListener('click', () => this._handleConfirm());
  }

  /**
   * Abre el modal para un registro específico.
   * @param {Object} params
   * @param {string|number} params.id - ID del registro a dar de baja / eliminar
   * @param {string} params.promptHtml - Mensaje principal en HTML (ej. `¿Dar de baja a <strong>...</strong>?`)
   * @param {string|null} [params.blockingError=null] - Si se especifica, muestra el error y deshabilita el botón de confirmar
   */
  open({ id, promptHtml, blockingError = null }) {
    this.targetId = id;
    const modal = this.container.querySelector('#modal-confirm-delete');
    const promptEl = this.container.querySelector('#confirm-delete-prompt');
    const errBox = this.container.querySelector('#confirm-delete-error');
    const confirmBtn = this.container.querySelector('#btn-submit-confirm-delete');

    if (promptEl) promptEl.innerHTML = promptHtml;

    if (errBox) {
      if (blockingError) {
        errBox.textContent = blockingError;
        errBox.classList.remove('hidden');
      } else {
        errBox.textContent = '';
        errBox.classList.add('hidden');
      }
    }

    if (confirmBtn) {
      confirmBtn.disabled = Boolean(blockingError);
    }

    modal?.classList.add('is-active');
  }

  close() {
    const modal = this.container.querySelector('#modal-confirm-delete');
    modal?.classList.remove('is-active');
    this.targetId = null;
  }

  async _handleConfirm() {
    if (!this.targetId || !this.onConfirm) return;

    const confirmBtn = this.container.querySelector('#btn-submit-confirm-delete');
    const errBox = this.container.querySelector('#confirm-delete-error');
    if (errBox) {
      errBox.classList.add('hidden');
      errBox.textContent = '';
    }

    if (confirmBtn) {
      confirmBtn.disabled = true;
      confirmBtn.classList.add('is-loading');
    }

    try {
      await this.onConfirm(this.targetId);
      this.close();
    } catch (err) {
      const msg = err.message || 'Error al procesar la baja';
      if (errBox) {
        errBox.textContent = msg;
        errBox.classList.remove('hidden');
      }
      toast.error(msg);
    } finally {
      if (confirmBtn) {
        confirmBtn.disabled = false;
        confirmBtn.classList.remove('is-loading');
      }
    }
  }
}
