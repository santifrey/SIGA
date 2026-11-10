/**
 * SIGA - views/login-view.js
 * Vista del Formulario de Inicio de Sesión
 */

import { authService } from '../services/auth-service.js';
import { toast } from '../utils/toast.js';
import { escapeHtml } from '../utils/html.js';

export class LoginView {
  constructor(router) {
    this.router = router;
    this.container = null;
  }

  render(rootElement) {
    this.container = rootElement;
    this.container.innerHTML = `
      <div class="login-wrapper">
        <div class="login-card fade-in">
          <div class="login-brand">
            <div class="login-brand-icon">S</div>
            <h1 class="login-title">SIGA</h1>
            <p class="login-subtitle">Sistema Integral de Gestión y Administración</p>
          </div>

          <form id="login-form" novalidate>
            <div class="form-group mb-3">
              <label for="login-email" class="form-label">
                Correo Electrónico / Usuario
                <span class="required-mark">*</span>
              </label>
              <div class="input-group">
                <span class="input-icon-left">
                  <svg class="icon-svg sm" viewBox="0 0 24 24">
                    <path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"></path>
                    <polyline points="22,6 12,13 2,6"></polyline>
                  </svg>
                </span>
                <input
                  type="text"
                  id="login-email"
                  class="form-control has-icon-left"
                  placeholder="ej. admin@siga.com"
                  value="admin@siga.com"
                  required
                  autocomplete="username"
                />
              </div>
              <div class="form-feedback invalid-feedback" id="email-error"></div>
            </div>

            <div class="form-group mb-3">
              <label for="login-password" class="form-label">
                Contraseña
                <span class="required-mark">*</span>
              </label>
              <div class="input-group">
                <span class="input-icon-left">
                  <svg class="icon-svg sm" viewBox="0 0 24 24">
                    <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
                    <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
                  </svg>
                </span>
                <input
                  type="password"
                  id="login-password"
                  class="form-control has-icon-left has-icon-right"
                  placeholder="••••••••"
                  value="admin123"
                  required
                  autocomplete="current-password"
                />
                <button type="button" class="input-icon-right" id="toggle-password-btn" aria-label="Mostrar u ocultar contraseña">
                  <svg class="icon-svg sm" id="eye-icon" viewBox="0 0 24 24">
                    <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path>
                    <circle cx="12" cy="12" r="3"></circle>
                  </svg>
                </button>
              </div>
              <div class="form-feedback invalid-feedback" id="password-error"></div>
            </div>

            <div class="d-flex align-center justify-between mb-4">
              <label class="form-check">
                <input type="checkbox" id="remember-me" class="form-check-input" checked />
                <span class="form-check-label text-sm">Recordar sesión</span>
              </label>
              <small class="text-muted">SIGA v1.0 Enterprise</small>
            </div>

            <div id="login-alert-box" class="mb-3 hidden"></div>

            <button type="submit" id="btn-submit-login" class="btn btn-primary btn-lg" style="width: 100%;">
              <span class="btn-text">Iniciar Sesión</span>
              <span class="btn-spinner"></span>
            </button>
          </form>

          <div class="mt-4 pt-3 text-center" style="border-top: 1px solid var(--border-light);">
            <small class="text-muted d-block mb-1">
              <strong>Credenciales de prueba disponibles:</strong>
            </small>
            <div class="d-flex justify-center gap-sm">
              <button type="button" class="btn btn-sm btn-outline" id="btn-fill-admin">Admin</button>
              <button type="button" class="btn btn-sm btn-outline" id="btn-fill-op">Operador</button>
            </div>
          </div>
        </div>
      </div>
    `;

    this._setupEventListeners();
  }

  _setupEventListeners() {
    const form = this.container.querySelector('#login-form');
    const emailInput = this.container.querySelector('#login-email');
    const passwordInput = this.container.querySelector('#login-password');
    const rememberCheckbox = this.container.querySelector('#remember-me');
    const togglePassBtn = this.container.querySelector('#toggle-password-btn');
    const submitBtn = this.container.querySelector('#btn-submit-login');
    const alertBox = this.container.querySelector('#login-alert-box');

    // Toggle de visibilidad de contraseña
    togglePassBtn.addEventListener('click', () => {
      const isPass = passwordInput.getAttribute('type') === 'password';
      passwordInput.setAttribute('type', isPass ? 'text' : 'password');
    });

    // Rellenado rápido de credenciales demo
    this.container.querySelector('#btn-fill-admin').addEventListener('click', () => {
      emailInput.value = 'admin@siga.com';
      passwordInput.value = 'admin123';
      alertBox.classList.add('hidden');
    });

    this.container.querySelector('#btn-fill-op').addEventListener('click', () => {
      emailInput.value = 'operador@siga.com';
      passwordInput.value = 'operador123';
      alertBox.classList.add('hidden');
    });

    // Envío del formulario
    form.addEventListener('submit', async (e) => {
      e.preventDefault();

      const email = emailInput.value.trim();
      const password = passwordInput.value;
      const remember = rememberCheckbox.checked;

      // Validación client-side
      let valid = true;
      if (!email) {
        emailInput.classList.add('is-invalid');
        this.container.querySelector('#email-error').textContent = 'Ingrese su usuario o correo';
        valid = false;
      } else {
        emailInput.classList.remove('is-invalid');
      }

      if (!password) {
        passwordInput.classList.add('is-invalid');
        this.container.querySelector('#password-error').textContent = 'Ingrese su contraseña';
        valid = false;
      } else {
        passwordInput.classList.remove('is-invalid');
      }

      if (!valid) return;

      // Estado de carga
      submitBtn.classList.add('is-loading');
      submitBtn.disabled = true;
      alertBox.classList.add('hidden');

      try {
        const user = await authService.login(email, password, remember);
        toast.success(`Bienvenido/a al sistema, ${user.nombre}.`, 'Acceso Concedido');
        this.router.navigate('#dashboard');
      } catch (err) {
        alertBox.className = 'mb-3 fade-in';
        alertBox.innerHTML = `
          <div style="background: var(--danger-bg); border: 1px solid var(--danger-border); color: var(--danger-text); padding: 0.75rem 1rem; border-radius: var(--radius-md); font-size: 0.8125rem;">
            ${escapeHtml(err.message || 'Error al autenticar. Verifique sus credenciales.')}
          </div>
        `;
        alertBox.classList.remove('hidden');
        toast.error(err.message || 'Credenciales inválidas');
      } finally {
        submitBtn.classList.remove('is-loading');
        submitBtn.disabled = false;
      }
    });
  }
}
