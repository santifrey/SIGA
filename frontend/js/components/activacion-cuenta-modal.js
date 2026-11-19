/**
 * SIGA - components/activacion-cuenta-modal.js
 * Modal reutilizable de cambio obligatorio de contraseña por primer ingreso.
 * Compartido entre App y LoginView.
 */

import { authService } from '../services/auth-service.js';
import { loadTemplate } from '../utils/template-loader.js';
import { toast } from '../utils/toast.js';

export const activacionCuentaModal = {
  /**
   * Muestra el modal de activación obligatoria de cuenta.
   * @param {Object} user - Usuario autenticado que requiere activación
   * @param {Object} router - Instancia del Router SPA
   * @param {string} [currentPassword=''] - Contraseña actual pre-cargada (si viene desde el login)
   */
  async open(user, router, currentPassword = '') {
    this.remove();

    const html = await loadTemplate('activacion-cuenta-modal.html');
    document.body.insertAdjacentHTML('beforeend', html);

    const modal = document.getElementById('modal-forzar-activacion');
    if (!modal) return;

    const actualInput = modal.querySelector('#fa-actual');
    const nuevaInput = modal.querySelector('#fa-nueva');
    const confirmarInput = modal.querySelector('#fa-confirmar');
    const errorBox = modal.querySelector('#fa-error');
    const submitBtn = modal.querySelector('#btn-fa-submit');

    if (actualInput) actualInput.value = currentPassword || '';

    modal.querySelector('#btn-fa-logout')?.addEventListener('click', async () => {
      await authService.logout();
      this.remove();
      router.navigate('#login');
    });

    modal.querySelector('#form-forzar-activacion')?.addEventListener('submit', async (e) => {
      e.preventDefault();
      errorBox.classList.add('hidden');
      errorBox.textContent = '';

      const pActual = actualInput.value.trim();
      const pNueva = nuevaInput.value;
      const pConfirmar = confirmarInput.value;

      if (!pActual || !pNueva || !pConfirmar) {
        errorBox.textContent = 'Por favor complete todos los campos.';
        errorBox.classList.remove('hidden');
        return;
      }

      if (pNueva.length < 6) {
        errorBox.textContent = 'La nueva contraseña debe tener al menos 6 caracteres.';
        errorBox.classList.remove('hidden');
        return;
      }

      if (pNueva === pActual) {
        errorBox.textContent = 'La nueva contraseña no puede ser igual a tu contraseña actual.';
        errorBox.classList.remove('hidden');
        return;
      }

      if (pNueva !== pConfirmar) {
        errorBox.textContent = 'Las contraseñas no coinciden.';
        errorBox.classList.remove('hidden');
        return;
      }

      submitBtn.classList.add('is-loading');
      submitBtn.disabled = true;

      try {
        await authService.cambiarPassword(user.id, pActual, pNueva);
        this.remove();
        toast.success(`Cuenta activada con éxito. Bienvenido/a, ${user.nombre}.`, 'Activación Exitosa');
        router.navigate('#dashboard');
      } catch (err) {
        errorBox.textContent = err.message || 'Error al cambiar la contraseña. Verifique su contraseña actual.';
        errorBox.classList.remove('hidden');
      } finally {
        submitBtn.classList.remove('is-loading');
        submitBtn.disabled = false;
      }
    });
  },

  remove() {
    document.getElementById('modal-forzar-activacion')?.remove();
  }
};
