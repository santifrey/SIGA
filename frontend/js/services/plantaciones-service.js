/**
 * SIGA - services/plantaciones-service.js
 * Servicio de Gestión de Plantaciones, Distribución Física y Lotes de Bulbos
 */

import { apiClient } from '../utils/api-client.js';

class PlantacionesService {
  _unwrap(res) {
    if (res && typeof res === 'object' && 'data' in res && res.data !== null && res.data !== undefined) {
      return res.data;
    }
    return res;
  }

  /**
   * Listar filas por sucursal y temporada
   * GET /api/v1/filas/por-sucursal/{sucursalId}/temporada/{temporadaId}
   */
  async listarFilasPorSucursalYTemporada(sucursalId, temporadaId) {
    const response = await apiClient.get(`/filas/por-sucursal/${sucursalId}/temporada/${temporadaId}`);
    const data = this._unwrap(response);
    return Array.isArray(data) ? data : (data?.items || []);
  }

  /**
   * Obtener plantación asociada a una temporada
   * GET /api/v1/plantaciones/por-temporada/{temporadaId}
   */
  async obtenerPorTemporada(temporadaId) {
    const response = await apiClient.get(`/plantaciones/por-temporada/${temporadaId}`);
    return this._unwrap(response);
  }

  /**
   * Crear nueva plantación para una temporada
   * POST /api/v1/plantaciones (temporadaId, observaciones)
   */
  async crear(data) {
    const response = await apiClient.post('/plantaciones', data);
    return this._unwrap(response);
  }

  /**
   * Registrar distribución física de lote en campo
   * POST /api/v1/plantaciones/{plantacionId}/distribucion
   */
  async registrarDistribucion(plantacionId, data) {
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
  }

  /**
   * Listar lotes registrados para una temporada dada
   * GET /api/v1/lotes/por-temporada/{temporadaId}
   */
  async listarLotes(temporadaId) {
    const response = await apiClient.get(`/lotes/por-temporada/${temporadaId}`);
    const data = this._unwrap(response);
    return Array.isArray(data) ? data : (data?.items || []);
  }

  /**
   * Alias de listarLotes para compatibilidad
   */
  async listarLotesPorTemporada(temporadaId) {
    return this.listarLotes(temporadaId);
  }
}

export const plantacionesService = new PlantacionesService();
