/**
 * SIGA - utils/toast.js
 * Sistema de alertas toast no intrusivas y animadas
 */

import { escapeHtml } from './html.js';

class ToastService {
  constructor() {
    this.container = null;
    this.icons = {
      success: `<svg class="toast-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path>
        <polyline points="22 4 12 14.01 9 11.01"></polyline>
      </svg>`,
      error: `<svg class="toast-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <circle cx="12" cy="12" r="10"></circle>
        <line x1="15" y1="9" x2="9" y2="15"></line>
        <line x1="9" y1="9" x2="15" y2="15"></line>
      </svg>`,
      warning: `<svg class="toast-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"></path>
        <line x1="12" y1="9" x2="12" y2="13"></line>
        <line x1="12" y1="17" x2="12.01" y2="17"></line>
      </svg>`,
      info: `<svg class="toast-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <circle cx="12" cy="12" r="10"></circle>
        <line x1="12" y1="16" x2="12" y2="12"></line>
        <line x1="12" y1="8" x2="12.01" y2="8"></line>
      </svg>`,
    };
  }

  getContainer() {
    if (!this.container) {
      this.container = document.getElementById('toast-container');
      if (!this.container) {
        this.container = document.createElement('div');
        this.container.id = 'toast-container';
        this.container.className = 'toast-container';
        document.body.appendChild(this.container);
      }
    }
    return this.container;
  }

  show({ type = 'info', title = '', message = '', duration = 4000 }) {
    const container = this.getContainer();

    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.setAttribute('role', 'alert');

    const iconHtml = this.icons[type] || this.icons.info;

    toast.innerHTML = `
      ${iconHtml}
      <div class="toast-content">
        ${title ? `<div class="toast-title">${escapeHtml(title)}</div>` : ''}
        <div class="toast-message">${escapeHtml(message)}</div>
      </div>
      <button type="button" class="toast-close" aria-label="Cerrar notificación">
        <svg class="icon-svg sm" viewBox="0 0 24 24" fill="none" stroke="currentColor">
          <line x1="18" y1="6" x2="6" y2="18"></line>
          <line x1="6" y1="6" x2="18" y2="18"></line>
        </svg>
      </button>
      <div class="toast-progress"></div>
    `;

    const closeBtn = toast.querySelector('.toast-close');
    const progressBar = toast.querySelector('.toast-progress');

    let isClosed = false;
    const removeToast = () => {
      if (isClosed) return;
      isClosed = true;
      toast.classList.add('removing');
      toast.addEventListener('transitionend', () => {
        if (toast.parentElement) toast.remove();
      }, { once: true });
    };

    closeBtn.addEventListener('click', removeToast);

    // Animación de la barra de progreso
    if (duration > 0) {
      progressBar.style.transition = `transform ${duration}ms linear`;
      // Forzar reflujo antes de iniciar la transición
      requestAnimationFrame(() => {
        progressBar.style.transform = 'scaleX(0)';
      });

      const timer = setTimeout(removeToast, duration);

      // Pausar al pasar el mouse por encima
      toast.addEventListener('mouseenter', () => {
        clearTimeout(timer);
        progressBar.style.transition = 'none';
      });
      toast.addEventListener('mouseleave', () => {
        setTimeout(removeToast, 1500);
      });
    }

    container.appendChild(toast);
  }

  success(message, title = 'Operación Exitosa', duration = 3500) {
    this.show({ type: 'success', title, message, duration });
  }

  error(message, title = 'Error', duration = 5000) {
    this.show({ type: 'error', title, message, duration });
  }

  warning(message, title = 'Advertencia', duration = 4500) {
    this.show({ type: 'warning', title, message, duration });
  }

  info(message, title = 'Información', duration = 4000) {
    this.show({ type: 'info', title, message, duration });
  }
}

export const toast = new ToastService();
