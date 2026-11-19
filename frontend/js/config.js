/**
 * SIGA - config.js
 * Configuración global de la aplicación frontend
 */

const isLocalEnvironment =
  window.location.hostname === 'localhost' ||
  window.location.hostname === '127.0.0.1' ||
  window.location.protocol === 'file:';

// Reemplazar con la URL del servicio en Google Cloud Run cuando se despliegue (ej: 'https://siga-api-xxxx.run.app/api/v1')
const PRODUCTION_API_URL = '/api/v1';
const LOCAL_API_URL = 'http://localhost:5000/api/v1';

export const CONFIG = {
  APP_NAME: 'SIGA',
  APP_SUBTITLE: 'Sistema Integral de Gestión y Administración',
  VERSION: '1.0.0',

  // URL Base de la API backend REST
  // En local apunta a http://localhost:5000/api/v1; en producción usa window.SIGA_API_URL o PRODUCTION_API_URL
  API_BASE_URL: window.SIGA_API_URL || (isLocalEnvironment ? LOCAL_API_URL : PRODUCTION_API_URL),

  // Configuración de almacenamiento
  STORAGE_KEYS: {
    TOKEN: 'siga_auth_token',
    USER: 'siga_auth_user',
    REMEMBER: 'siga_remember_session',
  },

  // Configuración de paginación por defecto
  PAGINATION: {
    DEFAULT_PAGE: 1,
    DEFAULT_PAGE_SIZE: 8,
    PAGE_SIZE_OPTIONS: [5, 8, 15, 25],
  },

  // Timeout para solicitudes HTTP (en milisegundos)
  HTTP_TIMEOUT_MS: 10000,
};
