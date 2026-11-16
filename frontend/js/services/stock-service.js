/**
 * SIGA - services/stock-service.js
 * Servicio de Consulta de Stock de Bulbos por Cliente (Feature 32)
 */

import { apiClient } from '../utils/api-client.js';
import { storage } from '../utils/storage.js';
import { CONFIG } from '../config.js';

export const EMPRESA_SINGLETON_ID = 1;

export const CLASES_BULBO = {
  0: { id: 0, codigo: 'Clase0', nombre: 'Clase 0 (Sin calificar)', descripcion: 'Sin calificar' },
  1: { id: 1, codigo: 'Clase1', nombre: 'Clase 1 (Calibre menor)', descripcion: 'Calibre menor' },
  2: { id: 2, codigo: 'Clase2', nombre: 'Clase 2 (Calibre estándar)', descripcion: 'Calibre estándar' },
  3: { id: 3, codigo: 'Clase3', nombre: 'Clase 3 (Calibre superior)', descripcion: 'Calibre superior' },
  4: { id: 4, codigo: 'Clase4', nombre: 'Clase 4 (Calibre premium)', descripcion: 'Calibre premium' },
  5: { id: 5, codigo: 'MIX',    nombre: 'MIX (Calibres 1, 2 y 3)',  descripcion: 'Mix comercial' }
};

// Semilla de stock inicial por cliente para desarrollo/mock con nombres oficiales de dominio
const SEED_STOCK = {
  // Empresa Central (id: 1)
  '1': [
    { clase: 0, claseBulbo: 0, nombreClase: 'Clase 0 (Sin calificar)', cantidad: 4500 },
    { clase: 1, claseBulbo: 1, nombreClase: 'Clase 1 (Calibre menor)', cantidad: 12800 },
    { clase: 2, claseBulbo: 2, nombreClase: 'Clase 2 (Calibre estándar)', cantidad: 18400 },
    { clase: 3, claseBulbo: 3, nombreClase: 'Clase 3 (Calibre superior)', cantidad: 9200 },
    { clase: 4, claseBulbo: 4, nombreClase: 'Clase 4 (Calibre premium)', cantidad: 3100 },
  ],
  empresa: [
    { clase: 0, claseBulbo: 0, nombreClase: 'Clase 0 (Sin calificar)', cantidad: 4500 },
    { clase: 1, claseBulbo: 1, nombreClase: 'Clase 1 (Calibre menor)', cantidad: 12800 },
    { clase: 2, claseBulbo: 2, nombreClase: 'Clase 2 (Calibre estándar)', cantidad: 18400 },
    { clase: 3, claseBulbo: 3, nombreClase: 'Clase 3 (Calibre superior)', cantidad: 9200 },
    { clase: 4, claseBulbo: 4, nombreClase: 'Clase 4 (Calibre premium)', cantidad: 3100 },
  ],
  // Cliente 2: Lucía Fernández
  '2': [
    { clase: 0, claseBulbo: 0, nombreClase: 'Clase 0 (Sin calificar)', cantidad: 300 },
    { clase: 1, claseBulbo: 1, nombreClase: 'Clase 1 (Calibre menor)', cantidad: 1100 },
    { clase: 2, claseBulbo: 2, nombreClase: 'Clase 2 (Calibre estándar)', cantidad: 1800 },
    { clase: 3, claseBulbo: 3, nombreClase: 'Clase 3 (Calibre superior)', cantidad: 950 },
    { clase: 4, claseBulbo: 4, nombreClase: 'Clase 4 (Calibre premium)', cantidad: 400 },
  ],
  // Cliente 3: Carlos Benítez
  '3': [
    { clase: 0, claseBulbo: 0, nombreClase: 'Clase 0 (Sin calificar)', cantidad: 1500 },
    { clase: 1, claseBulbo: 1, nombreClase: 'Clase 1 (Calibre menor)', cantidad: 4200 },
    { clase: 2, claseBulbo: 2, nombreClase: 'Clase 2 (Calibre estándar)', cantidad: 6000 },
    { clase: 3, claseBulbo: 3, nombreClase: 'Clase 3 (Calibre superior)', cantidad: 2800 },
    { clase: 4, claseBulbo: 4, nombreClase: 'Clase 4 (Calibre premium)', cantidad: 900 },
  ]
};

class StockService {
  constructor() {
    this._initLocalStore();
  }

  _initLocalStore() {
    if (!storage.get(CONFIG.STORAGE_KEYS.MOCK_STOCK)) {
      storage.set(CONFIG.STORAGE_KEYS.MOCK_STOCK, SEED_STOCK);
    }
  }

  _getLocalStock() {
    this._initLocalStore();
    return storage.get(CONFIG.STORAGE_KEYS.MOCK_STOCK, SEED_STOCK);
  }

  _saveLocalStock(stockData) {
    storage.set(CONFIG.STORAGE_KEYS.MOCK_STOCK, stockData);
  }

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

    try {
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
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        console.info(`[StockService] Fallback mock para cliente/empresa ${idConsulta}`);
        const allStock = this._getLocalStock();
        const key = String(idConsulta);

        if (allStock[key]) {
          return allStock[key];
        }

        if (idConsulta === EMPRESA_SINGLETON_ID && allStock.empresa) {
          return allStock.empresa;
        }

        // Si el cliente no tiene stock inicial, devolver 5 clases en 0 con nombres oficiales
        return [0, 1, 2, 3, 4].map(c => ({
          clase: c,
          claseBulbo: c,
          nombreClase: CLASES_BULBO[c].nombre,
          cantidad: 0
        }));
      }
      throw err;
    }
  }

  /**
   * Verifica disponibilidad de una clase específica para un cliente o Empresa
   */
  async verificarDisponibilidad(clienteId, clase, cantidad) {
    const idConsulta = (clienteId === 'empresa' || clienteId === 0 || clienteId === '0' || !clienteId)
      ? EMPRESA_SINGLETON_ID
      : Number(clienteId) || clienteId;

    try {
      return await apiClient.get('/stock/disponibilidad', { clienteId: idConsulta, clase, cantidad });
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        const stock = await this.obtenerStockPorCliente(idConsulta);
        const item = stock.find(s => Number(s.claseBulbo ?? s.clase) === Number(clase));
        const disponible = item ? Number(item.cantidad) >= Number(cantidad) : false;
        return {
          disponible,
          stockActual: item ? item.cantidad : 0,
          solicitado: cantidad
        };
      }
      throw err;
    }
  }

  /**
   * Actualiza el stock localmente tras confirmar un movimiento (para mock)
   */
  actualizarStockLocalPorMovimiento(origenId, destinoId, cantidades) {
    const allStock = this._getLocalStock();
    const origKey = String(origenId === 'empresa' ? EMPRESA_SINGLETON_ID : origenId);
    const destKey = String(destinoId === 'empresa' ? EMPRESA_SINGLETON_ID : destinoId);

    // Descontar de origen si no es compra externa / ingreso inicial
    if (origKey && origKey !== 'externo') {
      if (!allStock[origKey]) {
        allStock[origKey] = [0, 1, 2, 3, 4].map(c => ({
          clase: c,
          claseBulbo: c,
          nombreClase: CLASES_BULBO[c].nombre,
          cantidad: 0
        }));
      }
      cantidades.forEach(q => {
        const targetClase = Number(q.clase ?? q.claseBulbo);
        const row = allStock[origKey].find(r => Number(r.clase ?? r.claseBulbo) === targetClase);
        if (row) row.cantidad = Math.max(0, row.cantidad - (Number(q.cantidad) || 0));
      });
    }

    // Acreditar a destino si no es egreso externo / merma
    if (destKey && destKey !== 'externo') {
      if (!allStock[destKey]) {
        allStock[destKey] = [0, 1, 2, 3, 4].map(c => ({
          clase: c,
          claseBulbo: c,
          nombreClase: CLASES_BULBO[c].nombre,
          cantidad: 0
        }));
      }
      cantidades.forEach(q => {
        const targetClase = Number(q.clase ?? q.claseBulbo);
        const row = allStock[destKey].find(r => Number(r.clase ?? r.claseBulbo) === targetClase);
        if (row) row.cantidad = (Number(row.cantidad) || 0) + (Number(q.cantidad) || 0);
      });
    }

    this._saveLocalStock(allStock);
  }
}

export const stockService = new StockService();
