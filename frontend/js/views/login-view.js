/**
 * SIGA - views/login-view.js
 * Vista del Formulario de Inicio de Sesión
 */

import { authService } from '../services/auth-service.js';
import { activacionCuentaModal } from '../components/activacion-cuenta-modal.js';
import { loadTemplate } from '../utils/template-loader.js';
import { toast } from '../utils/toast.js';
import { escapeHtml } from '../utils/html.js';

export class LoginView {
  constructor(router) {
    this.router = router;
    this.container = null;
  }

  async render(rootElement) {
    this.container = rootElement;
    this.container.innerHTML = await loadTemplate('login.html');
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

    togglePassBtn.addEventListener('click', () => {
      const isPass = passwordInput.getAttribute('type') === 'password';
      passwordInput.setAttribute('type', isPass ? 'text' : 'password');
    });

    this.container.querySelector('#btn-fill-admin').addEventListener('click', () => {
      emailInput.value = 'admin';
      passwordInput.value = 'Admin123!';
      alertBox.classList.add('hidden');
    });

    this.container.querySelector('#btn-fill-op').addEventListener('click', () => {
      emailInput.value = 'operador@siga.com';
      passwordInput.value = 'operador123';
      alertBox.classList.add('hidden');
    });

    form.addEventListener('submit', async (e) => {
      e.preventDefault();

      const email = emailInput.value.trim();
      const password = passwordInput.value;
      const remember = rememberCheckbox.checked;

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

      submitBtn.classList.add('is-loading');
      submitBtn.disabled = true;
      alertBox.classList.add('hidden');

      try {
        const user = await authService.login(email, password, remember);

        if (user.activado === false || user.debeCambiarPassword === true) {
          await activacionCuentaModal.open(user, this.router, password);
          return;
        }

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
