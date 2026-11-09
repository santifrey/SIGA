/**
 * SIGA - config.js
 * Configuración global de la aplicación frontend
 */

export const CONFIG = {
  APP_NAME: 'SIGA',
  APP_SUBTITLE: 'Sistema Integral de Gestión y Administración',
  VERSION: '1.0.0',

  // URL Base de la API backend REST
  // Puede sobreescribirse dinámicamente mediante window.SIGA_API_URL
  API_BASE_URL: window.SIGA_API_URL || 'http://localhost:5000/api/v1',

  // Configuración de almacenamiento
  STORAGE_KEYS: {
    TOKEN: 'siga_auth_token',
    USER: 'siga_auth_user',
    REMEMBER: 'siga_remember_session',
    MOCK_CLIENTES: 'siga_mock_clientes',
    MOCK_SUCURSALES: 'siga_mock_sucursales',
    MOCK_MOVIMIENTOS: 'siga_mock_movimientos',
    MOCK_STOCK: 'siga_mock_stock',
    MOCK_TEMPORADAS: 'siga_mock_temporadas',
    MOCK_PLANTACIONES: 'siga_mock_plantaciones',
    MOCK_LOTES: 'siga_mock_lotes',
    MOCK_TRANSFERENCIAS: 'siga_mock_transferencias',
    MOCK_COSECHAS: 'siga_mock_cosechas',
    MOCK_PRECIOS_CLASE: 'siga_mock_precios_clase',
    MOCK_VENTAS: 'siga_mock_ventas',
  },

  // Configuración de paginación por defecto
  PAGINATION: {
    DEFAULT_PAGE: 1,
    DEFAULT_PAGE_SIZE: 8,
    PAGE_SIZE_OPTIONS: [5, 8, 15, 25],
  },

  // Timeout para solicitudes HTTP (en milisegundos)
  HTTP_TIMEOUT_MS: 10000,

  // Bandera de modo Mock / Fallback automático cuando el backend no esté disponible
  ENABLE_MOCK_FALLBACK: true,
};
