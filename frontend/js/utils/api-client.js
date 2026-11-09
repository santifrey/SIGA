/**
 * SIGA - utils/api-client.js
 * Cliente HTTP basado en Fetch con interceptor de token Bearer y manejo de 401
 */

import { CONFIG } from '../config.js';
import { storage } from './storage.js';
import { toast } from './toast.js';

class ApiClient {
  constructor() {
    this.baseUrl = CONFIG.API_BASE_URL;
    this.onUnauthorizedCallback = null;
  }

  /**
   * Registra un callback que se ejecuta cuando el servidor retorna 401 Unauthorized
   * (habitualmente para redireccionar a la vista de login).
   */
  setOnUnauthorized(callback) {
    this.onUnauthorizedCallback = callback;
  }

  /**
   * Realiza una petición HTTP con fetch, inyección de token e interceptores
   */
  async request(endpoint, options = {}) {
    const url = endpoint.startsWith('http') ? endpoint : `${this.baseUrl}${endpoint}`;

    const headers = {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
      ...(options.headers || {}),
    };

    // Inyección automática del Token JWT si existe sesión activa
    const token = storage.getToken();
    if (token && !headers['Authorization']) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    const config = {
      ...options,
      headers,
    };

    // Abort controller para timeout
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), CONFIG.HTTP_TIMEOUT_MS);
    config.signal = controller.signal;

    try {
      const response = await fetch(url, config);
      clearTimeout(timeoutId);

      // Manejo de 401 Unauthorized
      if (response.status === 401) {
        console.warn('[ApiClient] 401 Unauthorized recibido. Cerrando sesión...');
        storage.clearSession();
        if (this.onUnauthorizedCallback) {
          this.onUnauthorizedCallback();
        } else {
          window.location.hash = '#login';
        }
        throw new Error('Su sesión ha expirado o no tiene autorización. Inicie sesión nuevamente.');
      }

      // Manejo de respuestas vacías (204 No Content)
      if (response.status === 204) {
        return null;
      }

      let data;
      const contentType = response.headers.get('content-type') || '';
      if (contentType.includes('application/json')) {
        data = await response.json();
      } else {
        data = await response.text();
      }

      if (!response.ok) {
        const errorMsg = (data && (data.message || data.error || data.title)) ||
          `Error en la petición (${response.status}: ${response.statusText})`;
        const error = new Error(errorMsg);
        error.status = response.status;
        error.data = data;
        throw error;
      }

      return data;
    } catch (err) {
      clearTimeout(timeoutId);

      if (err.name === 'AbortError') {
        const timeoutErr = new Error('La solicitud excedió el tiempo de espera del servidor.');
        timeoutErr.isTimeout = true;
        throw timeoutErr;
      }

      // Si es un error de red (conexión rechazada o CORS)
      if (err instanceof TypeError && err.message.toLowerCase().includes('fetch')) {
        console.warn(`[ApiClient] No fue posible conectar con el backend (${url}).`, err.message);
        const networkErr = new Error('No fue posible conectar con el servidor backend (' + this.baseUrl + ').');
        networkErr.isNetworkError = true;
        throw networkErr;
      }

      throw err;
    }
  }

  // Métodos de conveniencia
  get(endpoint, params = null) {
    let url = endpoint;
    if (params) {
      const query = new URLSearchParams();
      Object.entries(params).forEach(([key, val]) => {
        if (val !== undefined && val !== null && val !== '') {
          query.append(key, val);
        }
      });
      const queryString = query.toString();
      if (queryString) {
        url += (url.includes('?') ? '&' : '?') + queryString;
      }
    }
    return this.request(url, { method: 'GET' });
  }

  post(endpoint, body = {}) {
    return this.request(endpoint, {
      method: 'POST',
      body: JSON.stringify(body),
    });
  }

  put(endpoint, body = {}) {
    return this.request(endpoint, {
      method: 'PUT',
      body: JSON.stringify(body),
    });
  }

  delete(endpoint) {
    return this.request(endpoint, {
      method: 'DELETE',
    });
  }
}

export const apiClient = new ApiClient();
