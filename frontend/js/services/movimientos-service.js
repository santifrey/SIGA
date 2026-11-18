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

  _unwrap(res) {
    if (res && typeof res === 'object' && 'data' in res && res.data !== null && res.data !== undefined) {
      return res.data;
    }
    return res;
  }

  _parseClaseIndex(val) {
    if (typeof val === 'number' && !Number.isNaN(val)) return val;
    const match = String(val ?? '').match(/(\d+)/);
    return match ? Number(match[1]) : 0;
  }

  _normalizeMovimiento(m) {
    if (!m) return m;
    const cantidades = (m.cantidades || []).map(c => {
      const idx = this._parseClaseIndex(c.claseBulbo ?? c.clase);
      return {
        ...c,
        claseBulbo: idx,
        clase: idx,
        nombreClase: c.nombreClase || `Clase ${idx}`,
        cantidad: Number(c.cantidad) || 0
      };
    });
    const totalBulbos = Number(m.totalBulbos) || cantidades.reduce((acc, c) => acc + c.cantidad, 0);
    return {
      ...m,
      numeroComprobante: m.numeroComprobante || `MOV-${String(m.id).padStart(4, '0')}`,
      estado: String(m.estado || 'Pendiente'),
      cantidades,
      totalBulbos
    };
  }

  /**
   * Listar movimientos con filtros
   */
  async listar({ page = 1, pageSize = 10, clienteId = '', estado = 'todos' } = {}) {
    try {
      const params = {};
      if (clienteId && clienteId !== 'todos' && clienteId !== 'empresa') {
        params.clienteId = Number(clienteId);
      }
      if (estado && estado !== 'todos') {
        params.estado = estado;
      }
      const response = await apiClient.get('/movimientos', params);
      const unwrapped = this._unwrap(response);
      let list = (Array.isArray(unwrapped) ? unwrapped : (unwrapped?.items || [])).map(m => this._normalizeMovimiento(m));
      list.sort((a, b) => Number(b.id) - Number(a.id));
      const total = list.length;
      const startIndex = (page - 1) * pageSize;
      const items = list.slice(startIndex, startIndex + pageSize);
      return { items, total, page: Number(page), pageSize: Number(pageSize) };
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
      const response = await apiClient.get(`/movimientos/${id}`);
      return this._normalizeMovimiento(this._unwrap(response));
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
   * Crear nuevo movimiento o ajuste en estado Pendiente
   * @param {object} data { origenId, destinoId, observaciones, cantidades: [{ claseBulbo, cantidad }] }
   */
  async crear(data) {
    try {
      const origenId = data.origenId === 'empresa' ? 1 : (data.origenId !== undefined && data.origenId !== null ? Number(data.origenId) : 0);
      const destinoId = data.destinoId === 'empresa' ? 1 : (data.destinoId !== undefined && data.destinoId !== null ? Number(data.destinoId) : 0);
      const cantidades = (data.cantidades || []).map(c => ({
        clase: this._parseClaseIndex(c.claseBulbo ?? c.clase),
        cantidad: Number(c.cantidad) || 0
      }));
      const payload = {
        origenId,
        destinoId,
        observaciones: data.observaciones || null,
        cantidades
      };
      const response = await apiClient.post('/movimientos', payload);
      return this._normalizeMovimiento(this._unwrap(response));
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
          origenNombre: data.origenNombre || (data.origenId === 'empresa' || data.origenId === 1 ? 'Empresa Central' : (data.origenId === 0 ? 'Ajustes de Inventario' : `Cliente #${data.origenId}`)),
          destinoId: data.destinoId,
          destinoNombre: data.destinoNombre || (data.destinoId === 'empresa' || data.destinoId === 1 ? 'Empresa Central' : (data.destinoId === 0 ? 'Ajustes de Inventario' : `Cliente #${data.destinoId}`)),
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
   * Registrar un Ajuste de Stock (quitar o añadir bulbos a un cliente o a la empresa con motivo)
   * @param {object} param0 { entidad: 'empresa'|'cliente', clienteId, accion: 'sumar'|'restar', cantidades, motivo, autoConfirmar }
   */
  async registrarAjuste({ entidad, clienteId, accion, cantidades, motivo, autoConfirmar = false }) {
    if (!motivo || !motivo.trim()) {
      throw new Error('Debe ingresar el motivo u observación del ajuste de stock.');
    }

    let origenId = 0;
    let destinoId = 1;
    let etiqueta = '';

    if (entidad === 'empresa') {
      if (accion === 'sumar') {
        origenId = 0;
        destinoId = 1;
        etiqueta = `[Ingreso a Empresa] ${motivo.trim()}`;
      } else {
        origenId = 1;
        destinoId = 0;
        etiqueta = `[Egreso de Empresa] ${motivo.trim()}`;
      }
    } else {
      const cId = Number(clienteId);
      if (!cId || cId <= 0) {
        throw new Error('Debe seleccionar el cliente para registrar el ajuste.');
      }
      if (accion === 'sumar') {
        origenId = 1;
        destinoId = cId;
        etiqueta = `[Ingreso a Cliente] ${motivo.trim()}`;
      } else {
        origenId = cId;
        destinoId = 1;
        etiqueta = `[Egreso de Cliente] ${motivo.trim()}`;
      }
    }

    const nuevo = await this.crear({
      origenId,
      destinoId,
      observaciones: etiqueta,
      cantidades
    });

    if (autoConfirmar && nuevo && nuevo.id) {
      await this.confirmar(nuevo.id);
      nuevo.estado = 'Confirmado';
    }

    return nuevo;
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
