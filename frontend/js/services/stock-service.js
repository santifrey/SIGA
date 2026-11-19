/**
 * SIGA - services/stock-service.js
 * Servicio de Consulta de Stock de Bulbos por Cliente (Feature 32)
 */

import { apiClient } from '../utils/api-client.js';

export const EMPRESA_SINGLETON_ID = 1;

export const CLASES_BULBO = {
  0: { id: 0, codigo: 'Clase0', nombre: 'Clase 0 (Sin calificar)', descripcion: 'Sin calificar' },
  1: { id: 1, codigo: 'Clase1', nombre: 'Clase 1 (Calibre menor)', descripcion: 'Calibre menor' },
  2: { id: 2, codigo: 'Clase2', nombre: 'Clase 2 (Calibre estándar)', descripcion: 'Calibre estándar' },
  3: { id: 3, codigo: 'Clase3', nombre: 'Clase 3 (Calibre superior)', descripcion: 'Calibre superior' },
  4: { id: 4, codigo: 'Clase4', nombre: 'Clase 4 (Calibre premium)', descripcion: 'Calibre premium' },
  5: { id: 5, codigo: 'MIX', nombre: 'MIX (Calibres 1, 2 y 3)', descripcion: 'Mix comercial' }
};

class StockService {
  /**
   * Extrae el índice numérico de la clase de bulbo de manera robusta
   * Maneja números (0..5), strings con dígitos ('Clase0', '0') y 'MIX' (5).
   */
  _parseClaseIndex(val) {
    if (typeof val === 'number' && !Number.isNaN(val)) return val;
    if (!val) return 0;
    const str = String(val).trim();
    if (str.toUpperCase() === 'MIX') return 5;
    const match = str.match(/(\d+)/);
    return match ? Number(match[1]) : 0;
  }

  /**
   * Obtiene el stock de bulbos por cliente o para Empresa Central (Clases 0 a 4)
   * Consulta directamente el backend `/api/v1/stock/por-cliente/{id}`
   * Resuelve 'empresa' o 0 a EMPRESA_SINGLETON_ID (1)
   * @param {string|number} clienteId
   */
  async obtenerStockPorCliente(clienteId) {
    const idConsulta = (clienteId === 'empresa' || clienteId === 0 || clienteId === '0' || !clienteId)
      ? EMPRESA_SINGLETON_ID
      : Number(clienteId) || clienteId;

    const response = await apiClient.get(`/stock/por-cliente/${idConsulta}`);

    let rawList = [];
    if (Array.isArray(response)) {
      rawList = response;
    } else if (response && typeof response === 'object') {
      const unwrapped = response.data !== undefined ? response.data : response;
      if (Array.isArray(unwrapped)) {
        rawList = unwrapped;
      } else if (unwrapped && typeof unwrapped === 'object') {
        if (Array.isArray(unwrapped.stockPorClase)) {
          rawList = unwrapped.stockPorClase;
        } else if (Array.isArray(unwrapped.items)) {
          rawList = unwrapped.items;
        } else if (Array.isArray(unwrapped.data)) {
          rawList = unwrapped.data;
        }
      }
    }

    if (!Array.isArray(rawList)) {
      rawList = [];
    }

    return rawList.map(it => {
      const claseId = this._parseClaseIndex(it.clase ?? it.claseBulbo ?? it.nombreClase);
      const meta = CLASES_BULBO[claseId] || { nombre: `Clase ${claseId}` };
      return {
        ...it,
        clase: claseId,
        claseBulbo: claseId,
        nombreClase: it.nombreClase || meta.nombre,
        cantidad: Math.max(0, Number(it.cantidad) || 0)
      };
    });
  }

  /**
   * Verifica disponibilidad de una clase específica para un cliente o Empresa
   */
  async verificarDisponibilidad(clienteId, clase, cantidad) {
    const idConsulta = (clienteId === 'empresa' || clienteId === 0 || clienteId === '0' || !clienteId)
      ? EMPRESA_SINGLETON_ID
      : Number(clienteId) || clienteId;

    return await apiClient.get('/stock/disponibilidad', { clienteId: idConsulta, clase, cantidad });
  }
}

export const stockService = new StockService();
