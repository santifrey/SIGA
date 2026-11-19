/**
 * SIGA - services/reportes-service.js
 * Servicio de Reportes Ejecutivos, Estado Integral de Cliente y Rendimiento Productivo
 */

import { apiClient } from '../utils/api-client.js';

class ReportesService {
  _unwrap(res) {
    if (res && typeof res === 'object' && 'data' in res && res.data !== null && res.data !== undefined) {
      return res.data;
    }
    return res;
  }

  /**
   * Obtiene la ficha / estado integral 360° de un cliente
   * GET /api/v1/reportes/cliente/{clienteId}/integral
   */
  async obtenerEstadoIntegralCliente(clienteId) {
    const response = await apiClient.get(`/reportes/cliente/${clienteId}/integral`);
    return this._unwrap(response);
  }

  /**
   * Obtiene el reporte de rendimiento y trazabilidad productiva de una temporada
   * GET /api/v1/reportes/produccion/temporada/{temporadaId}
   */
  async obtenerRendimientoTemporada(temporadaId) {
    const response = await apiClient.get(`/reportes/produccion/temporada/${temporadaId}`);
    return this._unwrap(response);
  }

  /**
   * Obtiene el resumen ejecutivo general de la empresa
   * GET /api/v1/reportes/resumen-ejecutivo
   */
  async obtenerResumenEjecutivo() {
    const response = await apiClient.get('/reportes/resumen-ejecutivo');
    return this._unwrap(response);
  }
}

export const reportesService = new ReportesService();
