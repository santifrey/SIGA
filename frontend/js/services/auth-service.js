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
      // Intento de llamada al backend real (enviando username y email para compatibilidad)
      const response = await apiClient.post('/auth/login', {
        username: email.trim(),
        email: email.trim(),
        password
      });
      
      // Manejo de respuesta estándar backend (ApiResponse<LoginResponseDto>)
      const dataPayload = response.data || response;
      const token = dataPayload.token || response.token;
      const user = response.usuario || response.user || (dataPayload.username ? {
        id: dataPayload.usuarioId || 1,
        nombre: dataPayload.nombreCompleto || dataPayload.username,
        email: dataPayload.email || email,
        rol: (dataPayload.roles && dataPayload.roles.length > 0) ? dataPayload.roles[0] : 'Administrador',
        roles: dataPayload.roles || ['ADMINISTRADOR']
      } : {
        email,
        nombre: email.split('@')[0],
        rol: 'Administrador'
      });

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
        const isDefaultAdmin = (email === 'admin@siga.com' || email === 'admin' || email === 'admin@azafranmediterraneo.com') && (password === 'admin123' || password === 'Admin123!');
        const isDefaultOp = (email === 'operador@siga.com' || email === 'operador') && (password === 'operador123' || password === 'Operador123!');

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
          throw new Error('Credenciales incorrectas. Para el modo demo use admin con Admin123! o admin123');
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
