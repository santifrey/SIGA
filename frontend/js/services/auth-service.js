/**
 * SIGA - services/auth-service.js
 * Servicio de Autenticación, Control de Sesión y Gestión Granular de Permisos (Feature 26)
 */

import { apiClient } from '../utils/api-client.js';
import { storage } from '../utils/storage.js';
import { CONFIG } from '../config.js';

class AuthService {
  constructor() {
    this._permisosSet = null; // Caché en memoria para lookups O(1)
  }

  /**
   * Inicia sesión con credenciales
   * @param {string} email
   * @param {string} password
   * @param {boolean} remember
   * @returns {Promise<object>} Datos del usuario autenticado
   */
  async login(email, password, remember = false) {
    // Llamada directa al backend
    const response = await apiClient.post('/auth/login', {
      username: email.trim(),
      email: email.trim(),
      password
    });

    // Manejo de respuesta estándar backend (ApiResponse<LoginResponseDto>)
    const dataPayload = response.data || response;
    const token = dataPayload.token || response.token;

    const rawPermisos = Array.isArray(dataPayload.permisos) ? dataPayload.permisos : [];

    const user = response.usuario || response.user || (dataPayload.username ? {
      id: dataPayload.usuarioId || 1,
      personaId: dataPayload.personaId || 1,
      nombre: dataPayload.nombreCompleto || dataPayload.username,
      email: dataPayload.email || email,
      rol: (dataPayload.roles && dataPayload.roles.length > 0) ? dataPayload.roles[0] : (dataPayload.rol || 'Administrador'),
      roles: dataPayload.roles || [dataPayload.rol || 'ADMINISTRADOR'],
      permisos: rawPermisos,
      activado: dataPayload.activado ?? (dataPayload.debeCambiarPassword === false),
      debeCambiarPassword: dataPayload.debeCambiarPassword ?? (dataPayload.activado === false)
    } : {
      email,
      nombre: email.split('@')[0],
      rol: 'Administrador',
      roles: ['ADMINISTRADOR'],
      permisos: rawPermisos,
      activado: true,
      debeCambiarPassword: false
    });

    if (!token) {
      throw new Error('La respuesta del servidor no incluyó el token de acceso.');
    }

    // Si el payload directo no trajo permisos, intentar decodificarlos del JWT
    if (!user.permisos || user.permisos.length === 0) {
      user.permisos = this._extraerPermisosDeJwt(token);
    }

    this._guardarPermisosEnMemoria(user.permisos);
    storage.setToken(token, remember);
    storage.setUser(user, remember);
    return user;
  }

  /**
   * Cierra la sesión activa
   */
  async logout() {
    this._permisosSet = null;
    storage.clearSession();
    try {
      // Intenta avisar al backend de forma opcional (best-effort)
      await apiClient.post('/auth/logout').catch(() => { });
    } catch {
      // No bloqueante
    }
  }

  /**
   * Actualiza los datos personales y de contacto del usuario autenticado
   * @param {object} datos { nombre, apellido, documento, email, telefono }
   * @returns {Promise<object>}
   */
  async actualizarPerfil(datos) {
    const response = await apiClient.put('/auth/me', datos);
    const data = (response && typeof response === 'object' && 'data' in response && response.data !== null && response.data !== undefined)
      ? response.data
      : response;

    if (data) {
      const user = this.getUsuarioActual() || {};
      const updatedUser = {
        ...user,
        nombre: data.nombreCompleto || `${data.nombre || ''} ${data.apellido || ''}`.trim() || user.nombre,
        nombreCompleto: data.nombreCompleto,
        email: data.email || user.email,
        telefono: data.telefono,
        documento: data.documento
      };
      storage.setUser(updatedUser);
    }
    return data;
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

  /**
   * Determina si el usuario actual cuenta con un permiso específico.
   * Si codigo es null, undefined o vacío, retorna true para sesión autenticada.
   * @param {string|null|undefined} codigo - ej. 'ROLES_VER'
   * @returns {boolean}
   */
  tienePermiso(codigo) {
    if (!this.estaAutenticado()) {
      return false;
    }
    if (!codigo || typeof codigo !== 'string' || codigo.trim() === '') {
      return true;
    }

    // Superadministrador: acceso total preventivo
    const user = this.getUsuarioActual();
    if (user && (user.rol === 'ADMINISTRADOR' || (Array.isArray(user.roles) && user.roles.includes('ADMINISTRADOR')) || user.rol === 'Administrador')) {
      return true;
    }

    const normalizedCode = codigo.trim().toUpperCase();
    const set = this._obtenerPermisosSet();
    return set.has(normalizedCode);
  }

  /**
   * Determina si el usuario cuenta con AL MENOS UNO de los permisos provistos (OR lógico).
   * @param {string[]} codigos - Lista de permisos
   * @returns {boolean}
   */
  tieneAlgunPermiso(codigos) {
    if (!this.estaAutenticado()) return false;
    if (!Array.isArray(codigos) || codigos.length === 0) return true;
    return codigos.some(c => this.tienePermiso(c));
  }

  /**
   * Determina si el usuario cuenta con TODOS los permisos provistos (AND lógico).
   * @param {string[]} codigos - Lista de permisos
   * @returns {boolean}
   */
  tieneTodosLosPermisos(codigos) {
    if (!this.estaAutenticado()) return false;
    if (!Array.isArray(codigos) || codigos.length === 0) return true;
    return codigos.every(c => this.tienePermiso(c));
  }

  /**
   * Obtiene la lista completa de códigos de permiso del usuario actual.
   * @returns {string[]}
   */
  getPermisos() {
    return Array.from(this._obtenerPermisosSet());
  }

  /**
   * Refresca los datos del usuario y sus permisos consultando GET /api/v1/auth/me
   * @returns {Promise<object>}
   */
  async obtenerPerfilCompleto() {
    try {
      const response = await apiClient.get('/auth/me');
      const data = (response && typeof response === 'object' && 'data' in response && response.data !== null && response.data !== undefined)
        ? response.data
        : response;

      if (data) {
        const user = this.getUsuarioActual() || {};
        const permisos = Array.isArray(data.permisos) ? data.permisos : (user.permisos || []);
        const updatedUser = {
          ...user,
          id: data.usuarioId || user.id,
          personaId: data.personaId || user.personaId,
          nombre: data.nombreCompleto || `${data.nombre || ''} ${data.apellido || ''}`.trim() || user.nombre,
          nombreCompleto: data.nombreCompleto,
          email: data.email || user.email,
          rol: data.rol || user.rol,
          roles: data.roles || (data.rol ? [data.rol] : user.roles || ['ADMINISTRADOR']),
          permisos,
          documento: data.documento,
          telefono: data.telefono,
          ultimoAcceso: data.ultimoAcceso,
          estado: data.estado
        };

        this._guardarPermisosEnMemoria(permisos);
        storage.setUser(updatedUser);
        return data;
      }
      return data;
    } catch (err) {
      throw err;
    }
  }

  // --- MÉTODOS PRIVADOS DE CACHÉ Y PARSEO JWT ---

  _guardarPermisosEnMemoria(permisosList) {
    if (Array.isArray(permisosList)) {
      this._permisosSet = new Set(permisosList.map(p => String(p).trim().toUpperCase()));
    } else {
      this._permisosSet = new Set();
    }
  }

  _obtenerPermisosSet() {
    if (this._permisosSet) {
      return this._permisosSet;
    }

    // 1. Intentar desde storage.getUser()
    const user = this.getUsuarioActual();
    if (user && Array.isArray(user.permisos) && user.permisos.length > 0) {
      this._guardarPermisosEnMemoria(user.permisos);
      return this._permisosSet;
    }

    // 2. Intentar decodificar desde el token JWT almacenado
    const token = storage.getToken();
    if (token) {
      const decodedPermisos = this._extraerPermisosDeJwt(token);
      if (decodedPermisos.length > 0) {
        if (user) {
          user.permisos = decodedPermisos;
          storage.setUser(user);
        }
        this._guardarPermisosEnMemoria(decodedPermisos);
        return this._permisosSet;
      }
    }

    // 3. Fallback vacío
    this._permisosSet = new Set();
    return this._permisosSet;
  }

  /**
   * Cambia la contraseña del usuario y activa la cuenta
   * @param {number} usuarioId
   * @param {string} passwordActual
   * @param {string} passwordNueva
   */
  async cambiarPassword(usuarioId, passwordActual, passwordNueva) {
    const res = await apiClient.post(`/usuarios/${usuarioId}/cambiar-password`, {
      passwordActual,
      passwordNueva
    });

    const user = storage.getUser();
    if (user) {
      user.activado = true;
      user.debeCambiarPassword = false;
      storage.setUser(user);
    }

    return res;
  }

  _extraerPermisosDeJwt(token) {
    if (!token || typeof token !== 'string') return [];
    try {
      const parts = token.split('.');
      if (parts.length < 2) return [];
      const base64Url = parts[1];
      const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
      const jsonPayload = decodeURIComponent(
        atob(base64)
          .split('')
          .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
          .join('')
      );
      const payload = JSON.parse(jsonPayload);
      if (!payload) return [];

      const permClaim = payload.permiso || payload.permisos || payload.permission;
      if (Array.isArray(permClaim)) {
        return permClaim.map(p => String(p).trim().toUpperCase());
      } else if (typeof permClaim === 'string' && permClaim.trim() !== '') {
        return [permClaim.trim().toUpperCase()];
      }
      return [];
    } catch {
      return [];
    }
  }
}

export const authService = new AuthService();
