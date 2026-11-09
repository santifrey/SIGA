/**
 * SIGA - services/temporadas-service.js
 * Servicio de Gestión de Temporadas Agrícolas y Ciclos Productivos
 */

import { apiClient } from '../utils/api-client.js';
import { storage } from '../utils/storage.js';
import { CONFIG } from '../config.js';

// Estados del ciclo productivo del azafrán
export const ESTADOS_TEMPORADA = [
  'Planificada',
  'Plantación',
  'En Crecimiento',
  'Cosecha',
  'Comercialización',
  'Cerrada'
];

// Semilla inicial de temporadas para desarrollo / modo mock
const SEED_TEMPORADAS = [
  {
    id: 1,
    nombre: 'Temporada 2024-2025',
    fechaInicio: '2024-03-01',
    fechaFin: '2024-11-30',
    estado: 'Cerrada',
    observaciones: 'Temporada histórica cerrada con 100% de cosecha y liquidación efectuada.',
    fechaCreacion: '2024-02-15T09:00:00Z'
  },
  {
    id: 2,
    nombre: 'Temporada 2025-2026',
    fechaInicio: '2025-03-01',
    fechaFin: '2025-11-30',
    estado: 'Plantación',
    observaciones: 'Temporada activa en curso. Etapa de distribución y siembra en filas.',
    fechaCreacion: '2025-02-10T10:30:00Z'
  }
];

class TemporadasService {
  constructor() {
    this._initLocalStore();
  }

  _initLocalStore() {
    if (!storage.get(CONFIG.STORAGE_KEYS.MOCK_TEMPORADAS)) {
      storage.set(CONFIG.STORAGE_KEYS.MOCK_TEMPORADAS, SEED_TEMPORADAS);
    }
  }

  _getLocalTemporadas() {
    this._initLocalStore();
    return storage.get(CONFIG.STORAGE_KEYS.MOCK_TEMPORADAS, SEED_TEMPORADAS);
  }

  _saveLocalTemporadas(list) {
    storage.set(CONFIG.STORAGE_KEYS.MOCK_TEMPORADAS, list);
  }

  /**
   * Helper para desempaquetar ApiResponse<T> si viene envuelto
   */
  _unwrap(res) {
    if (res && typeof res === 'object' && 'data' in res && res.data !== null && res.data !== undefined) {
      return res.data;
    }
    return res;
  }

  /**
   * Listar todas las temporadas
   * GET /api/v1/temporadas
   */
  async listar() {
    try {
      const response = await apiClient.get('/temporadas');
      const data = this._unwrap(response);
      return Array.isArray(data) ? data : (data.items || []);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        console.info('[TemporadasService] Usando fallback local mock.');
        const list = this._getLocalTemporadas();
        return [...list].sort((a, b) => Number(b.id) - Number(a.id));
      }
      throw err;
    }
  }

  /**
   * Alias de listar para compatibilidad
   */
  async listarTodas() {
    return this.listar();
  }

  /**
   * Obtener la temporada activa actual
   * GET /api/v1/temporadas/activa
   */
  async obtenerActiva() {
    try {
      const response = await apiClient.get('/temporadas/activa');
      return this._unwrap(response);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        const list = this._getLocalTemporadas();
        // Se considera activa la más reciente que no esté 'Cerrada'
        const activa = list.find(t => t.estado !== 'Cerrada') || list[list.length - 1] || null;
        return activa;
      }
      throw err;
    }
  }

  /**
   * Obtener temporada por ID
   * GET /api/v1/temporadas/{id}
   */
  async obtenerPorId(id) {
    try {
      const response = await apiClient.get(`/temporadas/${id}`);
      return this._unwrap(response);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        const list = this._getLocalTemporadas();
        const found = list.find(t => String(t.id) === String(id));
        if (!found) throw new Error('Temporada no encontrada');
        return found;
      }
      throw err;
    }
  }

  /**
   * Crear nueva temporada
   * POST /api/v1/temporadas (nombre, fechaInicio, fechaFin)
   */
  async crear(data) {
    try {
      const response = await apiClient.post('/temporadas', data);
      return this._unwrap(response);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        const list = this._getLocalTemporadas();
        const nextId = list.length > 0 ? Math.max(...list.map(t => Number(t.id) || 0)) + 1 : 1;
        const nueva = {
          id: nextId,
          nombre: data.nombre,
          fechaInicio: data.fechaInicio,
          fechaFin: data.fechaFin,
          estado: data.estado || 'Planificada',
          observaciones: data.observaciones || '',
          fechaCreacion: new Date().toISOString()
        };
        list.push(nueva);
        this._saveLocalTemporadas(list);
        return nueva;
      }
      throw err;
    }
  }

  /**
   * Cambiar de estado a una temporada
   * POST /api/v1/temporadas/{id}/cambiar-estado
   */
  async cambiarEstado(id, nuevoEstado) {
    try {
      const payload = typeof nuevoEstado === 'object'
        ? nuevoEstado
        : { nuevoEstado, estado: nuevoEstado };
      const response = await apiClient.post(`/temporadas/${id}/cambiar-estado`, payload);
      return this._unwrap(response);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        const list = this._getLocalTemporadas();
        const index = list.findIndex(t => String(t.id) === String(id));
        if (index === -1) throw new Error('Temporada no encontrada');

        const estadoNombre = typeof nuevoEstado === 'object'
          ? (nuevoEstado.nuevoEstado || nuevoEstado.estado)
          : nuevoEstado;

        list[index].estado = estadoNombre;
        list[index].fechaModificacion = new Date().toISOString();
        this._saveLocalTemporadas(list);
        return list[index];
      }
      throw err;
    }
  }

  /**
   * Retorna el siguiente estado en el flujo productivo o null si ya está cerrada
   */
  obtenerSiguienteEstado(estadoActual) {
    const idx = ESTADOS_TEMPORADA.findIndex(e => e.toLowerCase() === (estadoActual || '').toLowerCase());
    if (idx >= 0 && idx < ESTADOS_TEMPORADA.length - 1) {
      return ESTADOS_TEMPORADA[idx + 1];
    }
    return null;
  }
}

export const temporadasService = new TemporadasService();
