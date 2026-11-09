/**
 * SIGA - utils/storage.js
 * Abstracción de almacenamiento seguro (localStorage / sessionStorage)
 */

import { CONFIG } from '../config.js';

class StorageManager {
  constructor() {
    this.keys = CONFIG.STORAGE_KEYS;
  }

  /**
   * Determina el almacenamiento activo (localStorage si recordar sesión está activo, sino sessionStorage)
   */
  getStorage(preferLocal = false) {
    if (preferLocal || localStorage.getItem(this.keys.REMEMBER) === 'true') {
      return localStorage;
    }
    return sessionStorage;
  }

  /**
   * Guarda el token JWT de autenticación
   * @param {string} token - Token JWT
   * @param {boolean} remember - Si se debe persistir permanentemente
   */
  setToken(token, remember = false) {
    if (!token) return;
    if (remember) {
      localStorage.setItem(this.keys.REMEMBER, 'true');
      localStorage.setItem(this.keys.TOKEN, token);
      sessionStorage.removeItem(this.keys.TOKEN);
    } else {
      localStorage.removeItem(this.keys.REMEMBER);
      sessionStorage.setItem(this.keys.TOKEN, token);
      localStorage.removeItem(this.keys.TOKEN);
    }
  }

  /**
   * Obtiene el token JWT actual de cualquiera de los storages
   * @returns {string|null}
   */
  getToken() {
    return sessionStorage.getItem(this.keys.TOKEN) || localStorage.getItem(this.keys.TOKEN);
  }

  /**
   * Elimina el token JWT
   */
  removeToken() {
    sessionStorage.removeItem(this.keys.TOKEN);
    localStorage.removeItem(this.keys.TOKEN);
  }

  /**
   * Guarda los datos del usuario actual
   * @param {object} user - Datos del usuario autenticado
   * @param {boolean} remember
   */
  setUser(user, remember = false) {
    if (!user) return;
    const serialized = JSON.stringify(user);
    if (remember || localStorage.getItem(this.keys.REMEMBER) === 'true') {
      localStorage.setItem(this.keys.USER, serialized);
      sessionStorage.removeItem(this.keys.USER);
    } else {
      sessionStorage.setItem(this.keys.USER, serialized);
      localStorage.removeItem(this.keys.USER);
    }
  }

  /**
   * Obtiene el objeto del usuario actual
   * @returns {object|null}
   */
  getUser() {
    const raw = sessionStorage.getItem(this.keys.USER) || localStorage.getItem(this.keys.USER);
    if (!raw) return null;
    try {
      return JSON.parse(raw);
    } catch (err) {
      console.error('[Storage] Error al parsear usuario:', err);
      return null;
    }
  }

  /**
   * Elimina el usuario almacenado
   */
  removeUser() {
    sessionStorage.removeItem(this.keys.USER);
    localStorage.removeItem(this.keys.USER);
  }

  /**
   * Verifica si existe una sesión válida activa
   * @returns {boolean}
   */
  hasValidSession() {
    return Boolean(this.getToken() && this.getUser());
  }

  /**
   * Limpia toda la sesión de autenticación
   */
  clearSession() {
    this.removeToken();
    this.removeUser();
    localStorage.removeItem(this.keys.REMEMBER);
  }

  /**
   * Métodos utilitarios genéricos de almacenamiento local
   */
  get(key, defaultValue = null) {
    const raw = localStorage.getItem(key);
    if (raw === null) return defaultValue;
    try {
      return JSON.parse(raw);
    } catch {
      return raw;
    }
  }

  set(key, value) {
    if (typeof value === 'object') {
      localStorage.setItem(key, JSON.stringify(value));
    } else {
      localStorage.setItem(key, String(value));
    }
  }

  remove(key) {
    localStorage.removeItem(key);
  }
}

export const storage = new StorageManager();
