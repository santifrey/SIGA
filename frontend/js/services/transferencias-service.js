/**
 * SIGA - services/transferencias-service.js
 * Servicio para gestión y consulta de transferencias de lotes completos
 */

import { apiClient } from '../utils/api-client.js';

class TransferenciasService {
  _unwrap(res) {
    if (res && typeof res === 'object' && 'data' in res && res.data !== null && res.data !== undefined) {
      return res.data;
    }
    return res;
  }

  async listar(loteId = null, estado = null) {
    const params = {};
    if (loteId) params.loteId = loteId;
    if (estado) params.estado = estado;
    const res = await apiClient.get('/transferencias', params);
    const data = this._unwrap(res);
    return Array.isArray(data) ? data : (data?.items || []);
  }

  async obtenerHistorial(loteId) {
    const res = await apiClient.get(`/transferencias/lote/${loteId}/historial`);
    return this._unwrap(res);
  }

  async transferirLote(data) {
    const res = await apiClient.post('/transferencias', data);
    return this._unwrap(res);
  }

  async anular(id, motivo = '') {
    const res = await apiClient.post(`/transferencias/${id}/anular`, { motivo });
    return this._unwrap(res);
  }
}

export const transferenciasService = new TransferenciasService();
