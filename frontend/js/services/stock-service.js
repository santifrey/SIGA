/**
 * SIGA - services/stock-service.js
 * Servicio de Consulta de Stock de Bulbos por Cliente
 */

import { apiClient } from '../utils/api-client.js';
import { storage } from '../utils/storage.js';
import { CONFIG } from '../config.js';

// Semilla de stock inicial por cliente para desarrollo/mock
const SEED_STOCK = {
  // Empresa Central (id: 'empresa' o 0)
  empresa: [
    { claseBulbo: 0, nombreClase: 'Clase 0 (Extra Grande)', cantidad: 4500 },
    { claseBulbo: 1, nombreClase: 'Clase 1 (Grande)', cantidad: 12800 },
    { claseBulbo: 2, nombreClase: 'Clase 2 (Mediano)', cantidad: 18400 },
    { claseBulbo: 3, nombreClase: 'Clase 3 (Chico)', cantidad: 9200 },
    { claseBulbo: 4, nombreClase: 'Clase 4 (Semillero)', cantidad: 3100 },
  ],
  // Cliente 1: Martín Gómez
  '1': [
    { claseBulbo: 0, nombreClase: 'Clase 0 (Extra Grande)', cantidad: 800 },
    { claseBulbo: 1, nombreClase: 'Clase 1 (Grande)', cantidad: 2500 },
    { claseBulbo: 2, nombreClase: 'Clase 2 (Mediano)', cantidad: 3200 },
    { claseBulbo: 3, nombreClase: 'Clase 3 (Chico)', cantidad: 1400 },
    { claseBulbo: 4, nombreClase: 'Clase 4 (Semillero)', cantidad: 600 },
  ],
  // Cliente 2: Lucía Fernández
  '2': [
    { claseBulbo: 0, nombreClase: 'Clase 0 (Extra Grande)', cantidad: 300 },
    { claseBulbo: 1, nombreClase: 'Clase 1 (Grande)', cantidad: 1100 },
    { claseBulbo: 2, nombreClase: 'Clase 2 (Mediano)', cantidad: 1800 },
    { claseBulbo: 3, nombreClase: 'Clase 3 (Chico)', cantidad: 950 },
    { claseBulbo: 4, nombreClase: 'Clase 4 (Semillero)', cantidad: 400 },
  ],
  // Cliente 3: Carlos Benítez
  '3': [
    { claseBulbo: 0, nombreClase: 'Clase 0 (Extra Grande)', cantidad: 1500 },
    { claseBulbo: 1, nombreClase: 'Clase 1 (Grande)', cantidad: 4200 },
    { claseBulbo: 2, nombreClase: 'Clase 2 (Mediano)', cantidad: 6000 },
    { claseBulbo: 3, nombreClase: 'Clase 3 (Chico)', cantidad: 2800 },
    { claseBulbo: 4, nombreClase: 'Clase 4 (Semillero)', cantidad: 900 },
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
   * Obtiene el stock de bulbos por cliente (Clases 0 a 4)
   * @param {string|number} clienteId
   */
  async obtenerStockPorCliente(clienteId) {
    try {
      const response = await apiClient.get(`/stock/por-cliente/${clienteId}`);
      return response;
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        console.info(`[StockService] Fallback mock para cliente ${clienteId}`);
        const allStock = this._getLocalStock();
        const key = String(clienteId);

        if (allStock[key]) {
          return allStock[key];
        }

        // Si el cliente no tiene stock inicial, devolver 5 clases en 0
        return [
          { claseBulbo: 0, nombreClase: 'Clase 0 (Extra Grande)', cantidad: 0 },
          { claseBulbo: 1, nombreClase: 'Clase 1 (Grande)', cantidad: 0 },
          { claseBulbo: 2, nombreClase: 'Clase 2 (Mediano)', cantidad: 0 },
          { claseBulbo: 3, nombreClase: 'Clase 3 (Chico)', cantidad: 0 },
          { claseBulbo: 4, nombreClase: 'Clase 4 (Semillero)', cantidad: 0 },
        ];
      }
      throw err;
    }
  }

  /**
   * Verifica disponibilidad de una clase específica para un cliente
   */
  async verificarDisponibilidad(clienteId, clase, cantidad) {
    try {
      return await apiClient.get('/stock/disponibilidad', { clienteId, clase, cantidad });
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        const stock = await this.obtenerStockPorCliente(clienteId);
        const item = stock.find(s => Number(s.claseBulbo) === Number(clase));
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
    const origKey = String(origenId);
    const destKey = String(destinoId);

    // Descontar de origen si no es compra externa / ingreso inicial
    if (origKey && origKey !== 'externo') {
      if (!allStock[origKey]) {
        allStock[origKey] = [0,1,2,3,4].map(c => ({ claseBulbo: c, cantidad: 0 }));
      }
      cantidades.forEach(q => {
        const row = allStock[origKey].find(r => Number(r.claseBulbo) === Number(q.claseBulbo));
        if (row) row.cantidad = Math.max(0, row.cantidad - (Number(q.cantidad) || 0));
      });
    }

    // Acreditar a destino si no es egreso externo / merma
    if (destKey && destKey !== 'externo') {
      if (!allStock[destKey]) {
        allStock[destKey] = [0,1,2,3,4].map(c => ({ claseBulbo: c, cantidad: 0 }));
      }
      cantidades.forEach(q => {
        const row = allStock[destKey].find(r => Number(r.claseBulbo) === Number(q.claseBulbo));
        if (row) row.cantidad = (Number(row.cantidad) || 0) + (Number(q.cantidad) || 0);
      });
    }

    this._saveLocalStock(allStock);
  }
}

export const stockService = new StockService();
