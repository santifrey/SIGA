/**
 * SIGA - services/cosechas-service.js
 * Servicio de Gestión de Cosechas por Sucursal y Acreditación de Bulbos
 */

import { apiClient } from '../utils/api-client.js';

class CosechasService {
  _unwrap(res) {
    if (res && typeof res === 'object' && 'data' in res && res.data !== null && res.data !== undefined) {
      return res.data;
    }
    return res;
  }

  /**
   * Listar cosechas registradas para una temporada
   * GET /api/v1/cosechas/por-temporada/{temporadaId}
   */
  async listarPorTemporada(temporadaId) {
    const response = await apiClient.get(`/cosechas/por-temporada/${temporadaId}`);
    const data = this._unwrap(response);
    return Array.isArray(data) ? data : (data?.items || []);
  }

  /**
   * Obtener cosecha por ID
   * GET /api/v1/cosechas/{id}
   */
  async obtenerPorId(id) {
    const response = await apiClient.get(`/cosechas/${id}`);
    return this._unwrap(response);
  }

  /**
   * Iniciar una nueva cosecha para una sucursal en una temporada
   * POST /api/v1/cosechas
   * Body: { temporadaId, sucursalId, observaciones }
   */
  async crear(dto) {
    const response = await apiClient.post('/cosechas', dto);
    return this._unwrap(response);
  }

  /**
   * Registrar pesaje / cantidades cosechadas de un lote
   * POST /api/v1/cosechas/{cosechaId}/detalle
   * Body: { loteId, cantidades: [{ claseBulbo, cantidad }] }
   */
  async registrarDetalle(cosechaId, dto) {
    const response = await apiClient.post(`/cosechas/${cosechaId}/detalle`, dto);
    return this._unwrap(response);
  }

  /**
   * Cerrar la cosecha de la sucursal y acreditar stock a titulares vigentes
   * POST /api/v1/cosechas/{cosechaId}/cerrar
   */
  async cerrar(cosechaId) {
    const response = await apiClient.post(`/cosechas/${cosechaId}/cerrar`);
    return this._unwrap(response);
  }
}

export const cosechasService = new CosechasService();
