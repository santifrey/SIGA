/**
 * SIGA - services/transferencias-service.js
 * Servicio para gestión y consulta de transferencias de lotes completos
 */

import { apiClient } from '../utils/api-client.js';
import { storage } from '../utils/storage.js';
import { CONFIG } from '../config.js';

const SEED_TRANSFERENCIAS = [
  {
    id: 1,
    loteId: 1,
    identificacionFila: 'F-01',
    nombreSucursal: 'Sede Central - Villa General Belgrano',
    titularAnteriorId: 1,
    nombreTitularAnterior: 'Martín Gómez',
    nuevoTitularId: 2,
    nombreNuevoTitular: 'Lucía Fernández',
    fecha: '2025-05-10T14:30:00Z',
    estado: 'Confirmada',
    observaciones: 'Cesión completa de derechos sobre lote plantado.'
  }
];

class TransferenciasService {
  constructor() {
    this._initLocalStore();
  }

  _initLocalStore() {
    if (!storage.get(CONFIG.STORAGE_KEYS.MOCK_TRANSFERENCIAS)) {
      storage.set(CONFIG.STORAGE_KEYS.MOCK_TRANSFERENCIAS, SEED_TRANSFERENCIAS);
    }
  }

  _getLocalList() {
    this._initLocalStore();
    return storage.get(CONFIG.STORAGE_KEYS.MOCK_TRANSFERENCIAS, SEED_TRANSFERENCIAS);
  }

  _saveLocalList(list) {
    storage.set(CONFIG.STORAGE_KEYS.MOCK_TRANSFERENCIAS, list);
  }

  async listar(loteId = null, estado = null) {
    try {
      const params = {};
      if (loteId) params.loteId = loteId;
      if (estado) params.estado = estado;
      const res = await apiClient.get('/transferencias', params);
      return Array.isArray(res) ? res : res.data || [];
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        let list = this._getLocalList();
        if (loteId) list = list.filter(t => t.loteId === Number(loteId));
        if (estado) list = list.filter(t => t.estado.toLowerCase() === estado.toLowerCase());
        return list;
      }
      throw err;
    }
  }

  async obtenerHistorial(loteId) {
    try {
      return await apiClient.get(`/transferencias/lote/${loteId}/historial`);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        const list = this._getLocalList().filter(t => t.loteId === Number(loteId));
        return {
          loteId: Number(loteId),
          identificacionFila: 'F-01',
          nombreSucursal: 'Sede Central',
          titularOriginalId: 1,
          nombreTitularOriginal: 'Martín Gómez',
          titularVigenteId: list.length > 0 ? list[0].nuevoTitularId : 1,
          nombreTitularVigente: list.length > 0 ? list[0].nombreNuevoTitular : 'Martín Gómez',
          cantidadPlantadaTotal: 1500,
          transferencias: list
        };
      }
      throw err;
    }
  }

  async transferirLote(data) {
    try {
      return await apiClient.post('/transferencias', data);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        const list = this._getLocalList();
        const nextId = list.length > 0 ? Math.max(...list.map(t => t.id)) + 1 : 1;
        const nueva = {
          id: nextId,
          loteId: Number(data.loteId),
          identificacionFila: 'Fila Asignada',
          nombreSucursal: 'Sede Central',
          titularAnteriorId: 1,
          nombreTitularAnterior: 'Titular Anterior',
          nuevoTitularId: Number(data.nuevoTitularId),
          nombreNuevoTitular: data.nombreNuevoTitular || `Participante #${data.nuevoTitularId}`,
          fecha: new Date().toISOString(),
          estado: 'Confirmada',
          observaciones: data.observaciones || 'Transferencia directa de lote completo.'
        };
        list.unshift(nueva);
        this._saveLocalList(list);
        return nueva;
      }
      throw err;
    }
  }

  async anular(id, motivo = '') {
    try {
      return await apiClient.post(`/transferencias/${id}/anular`, { motivo });
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        const list = this._getLocalList();
        const item = list.find(t => t.id === Number(id));
        if (item) {
          item.estado = 'Anulada';
          item.observaciones += ` | [Anulado]: ${motivo}`;
          this._saveLocalList(list);
        }
        return { success: true };
      }
      throw err;
    }
  }
}

export const transferenciasService = new TransferenciasService();
