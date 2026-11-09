/**
 * SIGA - js/router.js
 * Enrutador Hash SPA con Auth Guards y soporte de Query Parameters
 */

import { authService } from './services/auth-service.js';

export class Router {
  constructor() {
    this.routes = {};
    this.currentRoute = null;
    this.onRouteChange = null;

    // Escucha el evento hashchange nativo del navegador
    window.addEventListener('hashchange', () => this._handleHashChange());
  }

  /**
   * Registra una ruta
   * @param {string} path - ej. '#dashboard', '#clientes', '#temporadas', '#plantaciones'
   * @param {object} viewInstance - Instancia de la vista con método render(el, params)
   * @param {boolean} requiresAuth - Si requiere sesión activa
   * @param {string} title - Título de la página
   */
  register(path, viewInstance, requiresAuth = true, title = 'SIGA') {
    this.routes[path] = {
      view: viewInstance,
      requiresAuth,
      title
    };
  }

  setOnRouteChange(callback) {
    this.onRouteChange = callback;
  }

  navigate(hashPath) {
    window.location.hash = hashPath;
  }

  start() {
    // Si no hay hash, inicia en #dashboard si está logueado, o #login si no
    if (!window.location.hash) {
      this.navigate(authService.estaAutenticado() ? '#dashboard' : '#login');
      return;
    }
    this._handleHashChange();
  }

  _parseHash(hash) {
    const raw = hash.replace(/^#/, '');
    const [pathPart, queryPart] = raw.split('?');
    const path = '#' + (pathPart || '');
    const params = {};

    if (queryPart) {
      const searchParams = new URLSearchParams(queryPart);
      for (const [key, val] of searchParams.entries()) {
        params[key] = val;
      }
    }

    return { path, params };
  }

  async _handleHashChange() {
    const { path, params } = this._parseHash(window.location.hash || '');
    const isAuthenticated = authService.estaAutenticado();

    // Si intenta ir a login pero ya está autenticado -> redirigir a dashboard
    if (path === '#login' && isAuthenticated) {
      this.navigate('#dashboard');
      return;
    }

    // Ruta objetivo o por defecto
    let targetRoute = this.routes[path];

    if (!targetRoute) {
      // Ruta no registrada: redirigir a dashboard o login
      this.navigate(isAuthenticated ? '#dashboard' : '#login');
      return;
    }

    // Guard de Autenticación
    if (targetRoute.requiresAuth && !isAuthenticated) {
      console.warn('[Router] Ruta protegida. Redirigiendo a #login...');
      this.navigate('#login');
      return;
    }

    this.currentRoute = path;

    // Notificar al controlador de layout principal
    if (this.onRouteChange) {
      this.onRouteChange({
        path,
        params,
        title: targetRoute.title,
        isLogin: path === '#login',
        view: targetRoute.view
      });
    }
  }
}
