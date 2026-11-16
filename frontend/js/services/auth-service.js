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

    } catch (err) {
      // Si el backend no está disponible y está habilitado el modo de demostración/fallback
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        console.info('[AuthService] Backend no detectado. Utilizando autenticación de demostración local.');

        // Verificación de credenciales demo
        const isDefaultAdmin = (email === 'admin@siga.com' || email === 'admin' || email === 'admin@azafranmediterraneo.com') && (password === 'admin123' || password === 'Admin123!');
        const isDefaultOp = (email === 'operador@siga.com' || email === 'operador') && (password === 'operador123' || password === 'Operador123!');

        if (isDefaultAdmin || isDefaultOp) {
          const adminPermisos = [
            'ROLES_VER', 'ROLES_GESTIONAR', 'PERMISOS_VER',
            'EMPLEADOS_VER', 'EMPLEADOS_GESTIONAR',
            'CLIENTES_VER', 'CLIENTES_GESTIONAR',
            'SUCURSALES_VER', 'SUCURSALES_GESTIONAR',
            'STOCK_VER', 'STOCK_AJUSTAR',
            'MOVCOM_VER', 'MOVCOM_CREAR', 'MOVCOM_APROBAR', 'MOVCOM_FACTURAR', 'MOVCOM_FINALIZAR', 'MOVCOM_RECHAZAR', 'MOVCOM_CORREGIR',
            'PRECIOS_REF_VER', 'PRECIOS_REF_GESTIONAR',
            'TEMPORADAS_VER', 'TEMPORADAS_GESTIONAR',
            'PLANTACIONES_VER', 'PLANTACIONES_GESTIONAR',
            'TRANSFERENCIAS_VER', 'TRANSFERENCIAS_GESTIONAR',
            'COSECHAS_VER', 'COSECHAS_GESTIONAR',
            'REPORTES_VER',
            'PERFIL_VER', 'PERFIL_MODIFICAR'
          ];
          const operadorPermisos = [
            'CLIENTES_VER', 'STOCK_VER', 'MOVCOM_VER', 'MOVCOM_CREAR',
            'TEMPORADAS_VER', 'PLANTACIONES_VER', 'TRANSFERENCIAS_VER', 'COSECHAS_VER',
            'REPORTES_VER', 'PERFIL_VER', 'PERFIL_MODIFICAR'
          ];

          const mockPermisos = isDefaultAdmin ? adminPermisos : operadorPermisos;
          const mockUser = {
            id: isDefaultAdmin ? 1 : 2,
            personaId: isDefaultAdmin ? 1 : 2,
            nombre: isDefaultAdmin ? 'Administrador General' : 'Operador Sucursal',
            email: email.includes('@') ? email : `${email}@siga.com`,
            rol: isDefaultAdmin ? 'ADMINISTRADOR' : 'VENDEDOR',
            roles: isDefaultAdmin ? ['ADMINISTRADOR'] : ['VENDEDOR'],
            permisos: mockPermisos,
            sucursal: 'Sucursal Central (Córdoba)',
            ultimoAcceso: new Date().toISOString()
          };
          const mockToken = 'mock_jwt_siga_' + btoa(JSON.stringify({
            sub: mockUser.id,
            role: mockUser.rol,
            permiso: mockPermisos,
            exp: Date.now() + 86400000
          }));

          this._guardarPermisosEnMemoria(mockPermisos);
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
      await apiClient.post('/auth/logout').catch(() => { });
    } finally {
      this._permisosSet = null;
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
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        const user = this.getUsuarioActual() || {};
        const mockProfile = {
          usuarioId: user.id || 1,
          personaId: user.personaId || 1,
          username: user.email ? user.email.split('@')[0] : 'admin',
          nombre: 'Administrador',
          apellido: 'SIGA',
          nombreCompleto: user.nombre || 'Administrador SIGA',
          email: user.email || 'admin@azafranmediterraneo.com',
          documento: '00000000',
          telefono: '123456789',
          rolId: 1,
          rol: user.rol || 'ADMINISTRADOR',
          roles: user.roles || ['ADMINISTRADOR'],
          permisos: user.permisos || [
            'ROLES_VER', 'ROLES_GESTIONAR', 'PERMISOS_VER',
            'EMPLEADOS_VER', 'EMPLEADOS_GESTIONAR',
            'CLIENTES_VER', 'CLIENTES_GESTIONAR',
            'SUCURSALES_VER', 'SUCURSALES_GESTIONAR',
            'STOCK_VER', 'STOCK_AJUSTAR',
            'MOVCOM_VER', 'MOVCOM_CREAR', 'MOVCOM_APROBAR', 'MOVCOM_FACTURAR', 'MOVCOM_FINALIZAR', 'MOVCOM_RECHAZAR', 'MOVCOM_CORREGIR',
            'PRECIOS_REF_VER', 'PRECIOS_REF_GESTIONAR',
            'TEMPORADAS_VER', 'TEMPORADAS_GESTIONAR',
            'PLANTACIONES_VER', 'PLANTACIONES_GESTIONAR',
            'TRANSFERENCIAS_VER', 'TRANSFERENCIAS_GESTIONAR',
            'COSECHAS_VER', 'COSECHAS_GESTIONAR',
            'REPORTES_VER', 'PERFIL_VER', 'PERFIL_MODIFICAR'
          ],
          ultimoAcceso: new Date().toISOString(),
          estado: 'Activo'
        };
        return mockProfile;
      }
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
