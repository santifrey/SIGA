/**
 * SIGA - services/movimientos-service.js
 * Servicio de Gestión de Movimientos de Stock de Bulbos
 */

import { apiClient } from '../utils/api-client.js';
import { storage } from '../utils/storage.js';
import { CONFIG } from '../config.js';
import { stockService } from './stock-service.js';

// Semilla de movimientos de prueba
const SEED_MOVIMIENTOS = [
  {
    id: 1,
    numeroComprobante: 'MOV-2025-001',
    fecha: '2025-04-10T11:30:00Z',
    origenId: 'empresa',
    origenNombre: 'Empresa (Depósito Central)',
    destinoId: '1',
    destinoNombre: 'Martín Gómez',
    estado: 'Confirmado',
    observaciones: 'Entrega inicial de bulbos para cultivo.',
    cantidades: [
      { claseBulbo: 0, nombreClase: 'Clase 0', cantidad: 200 },
      { claseBulbo: 1, nombreClase: 'Clase 1', cantidad: 500 },
      { claseBulbo: 2, nombreClase: 'Clase 2', cantidad: 800 },
      { claseBulbo: 3, nombreClase: 'Clase 3', cantidad: 300 },
      { claseBulbo: 4, nombreClase: 'Clase 4', cantidad: 100 },
    ],
    totalBulbos: 1900
  },
  {
    id: 2,
    numeroComprobante: 'MOV-2025-002',
    fecha: '2025-04-15T15:00:00Z',
    origenId: '1',
    origenNombre: 'Martín Gómez',
    destinoId: 'empresa',
    destinoNombre: 'Empresa (Depósito Central)',
    estado: 'Pendiente',
    observaciones: 'Devolución de semillero y bulbos clasificados.',
    cantidades: [
      { claseBulbo: 1, nombreClase: 'Clase 1', cantidad: 150 },
      { claseBulbo: 2, nombreClase: 'Clase 2', cantidad: 350 },
      { claseBulbo: 3, nombreClase: 'Clase 3', cantidad: 200 }
    ],
    totalBulbos: 700
  },
  {
    id: 3,
    numeroComprobante: 'MOV-2025-003',
    fecha: '2025-04-18T09:45:00Z',
    origenId: 'empresa',
    origenNombre: 'Empresa (Depósito Central)',
    destinoId: '2',
    destinoNombre: 'Lucía Fernández',
    estado: 'Confirmado',
    observaciones: 'Asignación de bulbos para nueva temporada.',
    cantidades: [
      { claseBulbo: 1, nombreClase: 'Clase 1', cantidad: 600 },
      { claseBulbo: 2, nombreClase: 'Clase 2', cantidad: 900 }
    ],
    totalBulbos: 1500
  },
  {
    id: 4,
    numeroComprobante: 'MOV-2025-004',
    fecha: '2025-04-20T16:20:00Z',
    origenId: '2',
    origenNombre: 'Lucía Fernández',
    destinoId: '3',
    destinoNombre: 'Carlos Benítez',
    estado: 'Anulado',
    observaciones: 'Transferencia inter-cliente cancelada por error de carga.',
    motivoAnulacion: 'Error en la selección del cliente destino.',
    cantidades: [
      { claseBulbo: 2, nombreClase: 'Clase 2', cantidad: 200 }
    ],
    totalBulbos: 200
  }
];

class MovimientosService {
  constructor() {
    this._initLocalStore();
  }

  _initLocalStore() {
    if (!storage.get(CONFIG.STORAGE_KEYS.MOCK_MOVIMIENTOS)) {
      storage.set(CONFIG.STORAGE_KEYS.MOCK_MOVIMIENTOS, SEED_MOVIMIENTOS);
    }
  }

  _getLocalMovimientos() {
    this._initLocalStore();
    return storage.get(CONFIG.STORAGE_KEYS.MOCK_MOVIMIENTOS, SEED_MOVIMIENTOS);
  }

  _saveLocalMovimientos(list) {
    storage.set(CONFIG.STORAGE_KEYS.MOCK_MOVIMIENTOS, list);
  }

  /**
   * Listar movimientos con filtros
   */
  async listar({ page = 1, pageSize = 10, clienteId = '', estado = 'todos' } = {}) {
    try {
      const params = { page, pageSize, clienteId, estado };
      const response = await apiClient.get('/movimientos', params);
      if (Array.isArray(response)) {
        return { items: response, total: response.length, page, pageSize };
      }
      return response;
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        console.info('[MovimientosService] Usando repositorio local mock.');
        let list = this._getLocalMovimientos();

        if (clienteId && clienteId !== 'todos') {
          list = list.filter(m => String(m.origenId) === String(clienteId) || String(m.destinoId) === String(clienteId));
        }

        if (estado && estado !== 'todos') {
          list = list.filter(m => m.estado.toLowerCase() === estado.toLowerCase());
        }

        // Orden descendente por ID / Fecha
        list.sort((a, b) => Number(b.id) - Number(a.id));

        const total = list.length;
        const startIndex = (page - 1) * pageSize;
        const items = list.slice(startIndex, startIndex + pageSize);

        return { items, total, page: Number(page), pageSize: Number(pageSize) };
      }
      throw err;
    }
  }

  /**
   * Obtener detalle de movimiento
   */
  async obtenerPorId(id) {
    try {
      return await apiClient.get(`/movimientos/${id}`);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        const list = this._getLocalMovimientos();
        const mov = list.find(m => String(m.id) === String(id));
        if (!mov) throw new Error('Movimiento no encontrado.');
        return mov;
      }
      throw err;
    }
  }

  /**
   * Crear nuevo movimiento en estado Pendiente
   * @param {object} data { origenId, destinoId, observaciones, cantidades: [{ claseBulbo, cantidad }] }
   */
  async crear(data) {
    try {
      return await apiClient.post('/movimientos', data);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        const list = this._getLocalMovimientos();
        const nextId = list.length > 0 ? Math.max(...list.map(m => Number(m.id) || 0)) + 1 : 1;

        const totalBulbos = (data.cantidades || []).reduce((acc, c) => acc + (Number(c.cantidad) || 0), 0);

        const nuevo = {
          id: nextId,
          numeroComprobante: `MOV-2025-${String(nextId).padStart(3, '0')}`,
          fecha: new Date().toISOString(),
          origenId: data.origenId,
          origenNombre: data.origenNombre || (data.origenId === 'empresa' ? 'Empresa Central' : `Cliente #${data.origenId}`),
          destinoId: data.destinoId,
          destinoNombre: data.destinoNombre || (data.destinoId === 'empresa' ? 'Empresa Central' : `Cliente #${data.destinoId}`),
          estado: 'Pendiente',
          observaciones: data.observaciones || '',
          cantidades: data.cantidades || [],
          totalBulbos
        };

        list.unshift(nuevo);
        this._saveLocalMovimientos(list);
        return nuevo;
      }
      throw err;
    }
  }

  /**
   * Confirmar movimiento (impacta el stock)
   */
  async confirmar(id) {
    try {
      return await apiClient.post(`/movimientos/${id}/confirmar`, {});
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        const list = this._getLocalMovimientos();
        const mov = list.find(m => String(m.id) === String(id));
        if (!mov) throw new Error('Movimiento no encontrado.');
        if (mov.estado !== 'Pendiente') {
          throw new Error(`El movimiento no está en estado Pendiente (estado actual: ${mov.estado}).`);
        }

        mov.estado = 'Confirmado';
        mov.fechaConfirmacion = new Date().toISOString();
        this._saveLocalMovimientos(list);

        // Actualizar stock local
        stockService.actualizarStockLocalPorMovimiento(mov.origenId, mov.destinoId, mov.cantidades);

        return { success: true, message: 'Movimiento confirmado e impacto de stock registrado.', movimiento: mov };
      }
      throw err;
    }
  }

  /**
   * Anular movimiento
   */
  async anular(id, motivo = '') {
    try {
      return await apiClient.post(`/movimientos/${id}/anular`, { motivo });
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        const list = this._getLocalMovimientos();
        const mov = list.find(m => String(m.id) === String(id));
        if (!mov) throw new Error('Movimiento no encontrado.');

        mov.estado = 'Anulado';
        mov.motivoAnulacion = motivo || 'Anulado por el operador.';
        mov.fechaAnulacion = new Date().toISOString();
        this._saveLocalMovimientos(list);

        return { success: true, message: 'Movimiento anulado correctamente.', movimiento: mov };
      }
      throw err;
    }
  }
}

export const movimientosService = new MovimientosService();
