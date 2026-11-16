/**
 * SIGA - services/plantaciones-service.js
 * Servicio de Gestión de Plantaciones, Distribución Física y Lotes de Bulbos
 */

import { apiClient } from '../utils/api-client.js';
import { storage } from '../utils/storage.js';
import { CONFIG } from '../config.js';

// Semilla inicial de plantaciones
const SEED_PLANTACIONES = [
  {
    id: 1,
    temporadaId: 2,
    observaciones: 'Plantación general Temporada 2025-2026 en Sucursal Central',
    fechaCreacion: '2025-03-01T08:30:00Z'
  }
];

// Semilla inicial de filas para mock fallback
const SEED_FILAS = [
  {
    id: 1,
    sucursalId: 1,
    temporadaId: 2,
    numeroFila: 1,
    identificacion: '1',
    capacidad: 5000,
    estado: 0,
    totalLotes: 1,
    fechaCreacion: '2025-03-01T08:30:00Z'
  },
  {
    id: 2,
    sucursalId: 1,
    temporadaId: 2,
    numeroFila: 2,
    identificacion: '2',
    capacidad: 5000,
    estado: 0,
    totalLotes: 1,
    fechaCreacion: '2025-03-01T08:30:00Z'
  },
  {
    id: 3,
    sucursalId: 1,
    temporadaId: 2,
    numeroFila: 3,
    identificacion: '3',
    capacidad: 6000,
    estado: 0,
    totalLotes: 0,
    fechaCreacion: '2025-03-01T08:30:00Z'
  }
];

// Semilla inicial de lotes plantados
const SEED_LOTES = [
  {
    id: 1,
    numeroLote: 'LOT-2025-001',
    plantacionId: 1,
    temporadaId: 2,
    sucursalId: 1,
    filaIdentificacion: 'F-01',
    capacidadFila: 5000,
    clienteId: 1,
    cantidades: [
      { claseBulbo: 0, cantidad: 500 },
      { claseBulbo: 1, cantidad: 1200 },
      { claseBulbo: 2, cantidad: 1500 },
      { claseBulbo: 3, cantidad: 800 },
      { claseBulbo: 4, cantidad: 0 }
    ],
    totalBulbos: 4000,
    fechaRegistro: '2025-03-05T10:00:00Z'
  },
  {
    id: 2,
    numeroLote: 'LOT-2025-002',
    plantacionId: 1,
    temporadaId: 2,
    sucursalId: 1,
    filaIdentificacion: 'F-02',
    capacidadFila: 5000,
    clienteId: 2,
    cantidades: [
      { claseBulbo: 0, cantidad: 200 },
      { claseBulbo: 1, cantidad: 800 },
      { claseBulbo: 2, cantidad: 1000 },
      { claseBulbo: 3, cantidad: 500 },
      { claseBulbo: 4, cantidad: 300 }
    ],
    totalBulbos: 2800,
    fechaRegistro: '2025-03-06T14:30:00Z'
  }
];

class PlantacionesService {
  constructor() {
    this._initLocalStore();
  }

  _initLocalStore() {
    if (!storage.get(CONFIG.STORAGE_KEYS.MOCK_PLANTACIONES)) {
      storage.set(CONFIG.STORAGE_KEYS.MOCK_PLANTACIONES, SEED_PLANTACIONES);
    }
    if (!storage.get(CONFIG.STORAGE_KEYS.MOCK_LOTES)) {
      storage.set(CONFIG.STORAGE_KEYS.MOCK_LOTES, SEED_LOTES);
    }
    if (!storage.get('siga_mock_filas')) {
      storage.set('siga_mock_filas', SEED_FILAS);
    }
  }

  _getLocalFilas() {
    this._initLocalStore();
    return storage.get('siga_mock_filas', SEED_FILAS);
  }

  _saveLocalFilas(list) {
    storage.set('siga_mock_filas', list);
  }

  /**
   * Listar filas por sucursal y temporada
   * GET /api/v1/filas/por-sucursal/{sucursalId}/temporada/{temporadaId}
   */
  async listarFilasPorSucursalYTemporada(sucursalId, temporadaId) {
    try {
      const response = await apiClient.get(`/filas/por-sucursal/${sucursalId}/temporada/${temporadaId}`);
      const data = this._unwrap(response);
      return Array.isArray(data) ? data : (data?.items || []);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        const filas = this._getLocalFilas();
        return filas.filter(f =>
          String(f.sucursalId) === String(sucursalId) &&
          String(f.temporadaId) === String(temporadaId)
        );
      }
      throw err;
    }
  }

  _getLocalPlantaciones() {
    this._initLocalStore();
    return storage.get(CONFIG.STORAGE_KEYS.MOCK_PLANTACIONES, SEED_PLANTACIONES);
  }

  _saveLocalPlantaciones(list) {
    storage.set(CONFIG.STORAGE_KEYS.MOCK_PLANTACIONES, list);
  }

  _getLocalLotes() {
    this._initLocalStore();
    return storage.get(CONFIG.STORAGE_KEYS.MOCK_LOTES, SEED_LOTES);
  }

  _saveLocalLotes(list) {
    storage.set(CONFIG.STORAGE_KEYS.MOCK_LOTES, list);
  }

  _unwrap(res) {
    if (res && typeof res === 'object' && 'data' in res && res.data !== null && res.data !== undefined) {
      return res.data;
    }
    return res;
  }

  /**
   * Obtener plantación asociada a una temporada
   * GET /api/v1/plantaciones/por-temporada/{temporadaId}
   */
  async obtenerPorTemporada(temporadaId) {
    try {
      const response = await apiClient.get(`/plantaciones/por-temporada/${temporadaId}`);
      return this._unwrap(response);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        const list = this._getLocalPlantaciones();
        let plantacion = list.find(p => String(p.temporadaId) === String(temporadaId));
        if (!plantacion) {
          // Si no existe, crear registro mock inicial para asegurar la operación
          const nextId = list.length > 0 ? Math.max(...list.map(p => Number(p.id) || 0)) + 1 : 1;
          plantacion = {
            id: nextId,
            temporadaId: Number(temporadaId),
            observaciones: `Plantación Temporada #${temporadaId}`,
            fechaCreacion: new Date().toISOString()
          };
          list.push(plantacion);
          this._saveLocalPlantaciones(list);
        }
        return plantacion;
      }
      throw err;
    }
  }

  /**
   * Crear nueva plantación para una temporada
   * POST /api/v1/plantaciones (temporadaId, observaciones)
   */
  async crear(data) {
    try {
      const response = await apiClient.post('/plantaciones', data);
      return this._unwrap(response);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        const list = this._getLocalPlantaciones();
        const nextId = list.length > 0 ? Math.max(...list.map(p => Number(p.id) || 0)) + 1 : 1;
        const nueva = {
          id: nextId,
          temporadaId: Number(data.temporadaId),
          observaciones: data.observaciones || '',
          fechaCreacion: new Date().toISOString()
        };
        list.push(nueva);
        this._saveLocalPlantaciones(list);
        return nueva;
      }
      throw err;
    }
  }

  /**
   * Registrar distribución física de lote en campo
   * POST /api/v1/plantaciones/{plantacionId}/distribucion
   */
  async registrarDistribucion(plantacionId, data) {
    try {
      // Normalizar detalles para el DTO backend (Detalles) y mock (cantidades)
      const detallesNormalizados = (data.detalles || data.cantidades || []).map(c => ({
        clase: Number(c.clase ?? c.claseBulbo),
        claseBulbo: Number(c.claseBulbo ?? c.clase),
        cantidad: Number(c.cantidad) || 0
      }));

      const hasPlantacionId = plantacionId && Number(plantacionId) > 0;
      const url = hasPlantacionId ? `/plantaciones/${plantacionId}/distribucion` : '/plantaciones/distribucion';

      const payload = {
        plantacionId: hasPlantacionId ? Number(plantacionId) : null,
        temporadaId: Number(data.temporadaId),
        sucursalId: Number(data.sucursalId),
        filaId: data.filaId ? Number(data.filaId) : null,
        numeroFila: data.numeroFila ? Number(data.numeroFila) : null,
        identificacionFila: String(data.identificacionFila || data.filaIdentificacion || ''),
        capacidadFila: Number(data.capacidadFila) || 0,
        numeroLote: data.numeroLote ? Number(data.numeroLote) : (data.orden ? Number(data.orden) : null),
        orden: data.orden ? Number(data.orden) : (data.numeroLote ? Number(data.numeroLote) : null),
        clienteId: Number(data.clienteId),
        detalles: detallesNormalizados,
        cantidades: detallesNormalizados
      };

      const response = await apiClient.post(url, payload);
      return this._unwrap(response);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        const lotes = this._getLocalLotes();
        const filas = this._getLocalFilas();
        const nextId = lotes.length > 0 ? Math.max(...lotes.map(l => Number(l.id) || 0)) + 1 : 1;

        const cantidades = (data.detalles || data.cantidades || []).map(c => ({
          claseBulbo: Number(c.claseBulbo ?? c.clase),
          cantidad: Number(c.cantidad) || 0
        }));
        const totalBulbos = cantidades.reduce((sum, c) => sum + c.cantidad, 0);

        // Actualizar o crear fila en mock
        let fila = null;
        if (data.filaId) {
          fila = filas.find(f => String(f.id) === String(data.filaId));
        } else {
          const ident = String(data.numeroFila || data.identificacionFila);
          fila = filas.find(f =>
            String(f.sucursalId) === String(data.sucursalId) &&
            String(f.temporadaId) === String(data.temporadaId) &&
            f.identificacion === ident
          );
        }

        if (fila) {
          fila.totalLotes = (fila.totalLotes || 0) + 1;
          fila.estado = 1; // Plantada
        } else {
          const nextFilaId = filas.length > 0 ? Math.max(...filas.map(f => Number(f.id) || 0)) + 1 : 1;
          fila = {
            id: nextFilaId,
            sucursalId: Number(data.sucursalId),
            temporadaId: Number(data.temporadaId),
            identificacion: String(data.numeroFila || data.identificacionFila || nextFilaId),
            capacidad: Number(data.capacidadFila) || 5000,
            estado: 1,
            totalLotes: 1,
            fechaCreacion: new Date().toISOString()
          };
          filas.push(fila);
        }
        this._saveLocalFilas(filas);

        const nuevoLote = {
          id: nextId,
          numeroLote: `LOT-${data.temporadaId || 'TEMP'}-${String(nextId).padStart(3, '0')}`,
          orden: Number(data.numeroLote || data.orden || fila.totalLotes),
          plantacionId: Number(plantacionId),
          temporadaId: Number(data.temporadaId || 1),
          sucursalId: Number(data.sucursalId),
          filaId: fila.id,
          filaIdentificacion: fila.identificacion,
          capacidadFila: fila.capacidad,
          clienteId: Number(data.clienteId),
          cantidades,
          totalBulbos,
          fechaRegistro: new Date().toISOString()
        };

        lotes.unshift(nuevoLote);
        this._saveLocalLotes(lotes);
        return nuevoLote;
      }
      throw err;
    }
  }

  /**
   * Listar lotes registrados para una temporada dada
   * GET /api/v1/lotes/por-temporada/{temporadaId}
   */
  async listarLotes(temporadaId) {
    try {
      const response = await apiClient.get(`/lotes/por-temporada/${temporadaId}`);
      const data = this._unwrap(response);
      return Array.isArray(data) ? data : (data.items || []);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        const lotes = this._getLocalLotes();
        if (!temporadaId) return lotes;
        return lotes.filter(l => String(l.temporadaId) === String(temporadaId));
      }
      throw err;
    }
  }

  /**
   * Alias de listarLotes para compatibilidad
   */
  async listarLotesPorTemporada(temporadaId) {
    return this.listarLotes(temporadaId);
  }
}

export const plantacionesService = new PlantacionesService();
