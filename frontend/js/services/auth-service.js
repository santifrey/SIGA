/**
 * SIGA - services/auth-service.js
 * Servicio de Autenticación y Control de Sesión
 */

import { apiClient } from '../utils/api-client.js';
import { storage } from '../utils/storage.js';
import { CONFIG } from '../config.js';

class AuthService {
  /**
   * Inicia sesión con credenciales
   * @param {string} email
   * @param {string} password
   * @param {boolean} remember
   * @returns {Promise<object>} Datos del usuario autenticado
   */
  async login(email, password, remember = false) {
    try {
      // Intento de llamada al backend real
      const response = await apiClient.post('/auth/login', { email, password });
      
      // Manejo de respuesta estándar backend (token + usuario)
      const token = response.token || (response.data && response.data.token);
      const user = response.usuario || response.user || (response.data && response.data.user) || {
        email,
        nombre: email.split('@')[0],
        rol: 'Administrador'
      };

      if (!token) {
        throw new Error('La respuesta del servidor no incluyó el token de acceso.');
      }

      storage.setToken(token, remember);
      storage.setUser(user, remember);
      return user;

    } catch (err) {
      // Si el backend no está disponible y está habilitado el modo de demostración/fallback
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        console.info('[AuthService] Backend no detectado. Utilizando autenticación de demostración local.');
        
        // Verificación de credenciales demo
        const isDefaultAdmin = (email === 'admin@siga.com' || email === 'admin') && password === 'admin123';
        const isDefaultOp = (email === 'operador@siga.com' || email === 'operador') && password === 'operador123';

        if (isDefaultAdmin || isDefaultOp) {
          const mockUser = {
            id: isDefaultAdmin ? 1 : 2,
            nombre: isDefaultAdmin ? 'Administrador General' : 'Operador Sucursal',
            email: email.includes('@') ? email : `${email}@siga.com`,
            rol: isDefaultAdmin ? 'Administrador' : 'Operador',
            sucursal: 'Sucursal Central (Córdoba)',
            ultimoAcceso: new Date().toISOString()
          };
          const mockToken = 'mock_jwt_siga_' + btoa(JSON.stringify({ sub: mockUser.id, role: mockUser.rol, exp: Date.now() + 86400000 }));
          
          storage.setToken(mockToken, remember);
          storage.setUser(mockUser, remember);
          return mockUser;
        } else {
          throw new Error('Credenciales incorrectas. Para el modo demo use admin@siga.com con admin123');
        }
      }

      throw err;
    }
  }

  /**
   * Cierra la sesión activa
   */
  async logout() {
    try {
      // Intenta avisar al backend de forma opcional (best-effort)
      await apiClient.post('/auth/logout').catch(() => {});
    } finally {
      storage.clearSession();
    }
  }

  /**
   * Obtiene los datos del usuario actualmente autenticado
   * @returns {object|null}
   */
  getUsuarioActual() {
    return storage.getUser();
  }

  /**
   * Indica si hay una sesión activa y válida
   * @returns {boolean}
   */
  estaAutenticado() {
    return storage.hasValidSession();
  }
}

export const authService = new AuthService();
