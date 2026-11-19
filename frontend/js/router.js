/**
 * SIGA - js/router.js
 * Enrutador Hash SPA con Auth Guards, Permission Guards y soporte de Query Parameters (Feature 26)
 */

import { authService } from './services/auth-service.js';
import { toast } from './utils/toast.js';

export class Router {
  constructor() {
    this.routes = {};
    this.currentRoute = null;
    this.onRouteChange = null;
    this._isNavigating = false;

    // Escucha el evento hashchange nativo del navegador
    window.addEventListener('hashchange', () => this._handleHashChange());
  }

  /**
   * Registra una ruta en la SPA
   * @param {string} path - ej. '#dashboard', '#clientes', '#roles', '#empleados'
   * @param {object} viewInstance - Instancia de la vista con método render(el, params)
   * @param {boolean} requiresAuth - Si requiere sesión activa
   * @param {string} title - Título de la página
   * @param {string|string[]|null} requiredPermission - Código(s) de permiso requerido(s)
   */
  register(path, viewInstance, requiresAuth = true, title = 'SIGA', requiredPermission = null) {
    this.routes[path] = {
      view: viewInstance,
      requiresAuth,
      title,
      requiredPermission
    };
  }

  setOnRouteChange(callback) {
    this.onRouteChange = callback;
  }

  navigate(hashPath) {
    if (window.location.hash === hashPath) {
      this._handleHashChange();
    } else {
      window.location.hash = hashPath;
    }
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
    const raw = hash.replace(/^#+/, '');
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
    if (this._isNavigating) return;
    this._isNavigating = true;

    try {
      const { path, params } = this._parseHash(window.location.hash || '');
      const isAuthenticated = authService.estaAutenticado();

      // Si intenta ir a login pero ya está autenticado -> redirigir a dashboard
      if (path === '#login' && isAuthenticated) {
        this._isNavigating = false;
        this.navigate('#dashboard');
        return;
      }

      // Ruta objetivo o por defecto
      let targetRoute = this.routes[path];

      if (!targetRoute) {
        // Ruta no registrada: redirigir a dashboard o login limpiamente
        console.warn(`[Router] Ruta no registrada: ${path}. Redirigiendo...`);
        const fallback = isAuthenticated ? '#dashboard' : '#login';
        if (path === fallback || !this.routes[fallback]) {
          this.currentRoute = path;
          return;
        }
        this._isNavigating = false;
        this.navigate(fallback);
        return;
      }

      // Guard de Autenticación
      if (targetRoute.requiresAuth && !isAuthenticated) {
        console.warn(`[Router] Acceso no autenticado a ${path}. Redirigiendo a #login...`);
        this._isNavigating = false;
        this.navigate('#login');
        return;
      }

      // Guard de Permisos (Feature 26)
      if (targetRoute.requiresAuth && isAuthenticated && targetRoute.requiredPermission) {
        const hasPermission = Array.isArray(targetRoute.requiredPermission)
          ? authService.tieneAlgunPermiso(targetRoute.requiredPermission)
          : authService.tienePermiso(targetRoute.requiredPermission);

        if (!hasPermission) {
          console.warn(`[Router] Acceso denegado a "${path}". Permiso requerido: ${JSON.stringify(targetRoute.requiredPermission)}`);
          toast.warning('No posee los permisos necesarios para acceder a este módulo.', 'Acceso Restringido');
          this._isNavigating = false;
          this.navigate('#dashboard');
          return;
        }
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
    } finally {
      this._isNavigating = false;
    }
  }
}
