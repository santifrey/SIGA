/**
 * SIGA - services/temporadas-service.js
 * Servicio de Gestión de Temporadas Agrícolas y Ciclos Productivos
 */

import { apiClient } from '../utils/api-client.js';

// Estados del ciclo productivo del azafrán (Etiquetas de interfaz)
export const ESTADOS_TEMPORADA = [
  'Planificada',
  'Plantación',
  'En Crecimiento',
  'Cosecha',
  'Comercialización',
  'Cerrada'
];

// Mapeo canónico hacia los valores del enum C# en el backend (EstadoTemporada)
const MAPA_API_ESTADOS = {
  'planificada': 'Planificada',
  'plantacion': 'Plantacion',
  'plantación': 'Plantacion',
  'en crecimiento': 'EnCrecimiento',
  'encrecimiento': 'EnCrecimiento',
  'cosecha': 'Cosecha',
  'comercializacion': 'Comercializacion',
  'comercialización': 'Comercializacion',
  'cerrada': 'Cerrada'
};

class TemporadasService {
  _unwrap(res) {
    if (res && typeof res === 'object' && 'data' in res && res.data !== null && res.data !== undefined) {
      return res.data;
    }
    return res;
  }

  /**
   * Listar todas las temporadas
   * GET /api/v1/temporadas
   */
  async listar() {
    const response = await apiClient.get('/temporadas');
    const data = this._unwrap(response);
    return Array.isArray(data) ? data : (data?.items || []);
  }

  /**
   * Alias de listar para compatibilidad
   */
  async listarTodas() {
    return this.listar();
  }

  /**
   * Obtener la temporada activa actual
   * GET /api/v1/temporadas/activa
   */
  async obtenerActiva() {
    const response = await apiClient.get('/temporadas/activa');
    return this._unwrap(response);
  }

  /**
   * Obtener temporada por ID
   * GET /api/v1/temporadas/{id}
   */
  async obtenerPorId(id) {
    const response = await apiClient.get(`/temporadas/${id}`);
    return this._unwrap(response);
  }

  /**
   * Crear nueva temporada
   * POST /api/v1/temporadas (nombre, fechaInicio, fechaFin)
   */
  async crear(data) {
    const response = await apiClient.post('/temporadas', data);
    return this._unwrap(response);
  }

  /**
   * Cambiar de estado a una temporada
   * POST /api/v1/temporadas/{id}/cambiar-estado
   */
  async cambiarEstado(id, nuevoEstado) {
    const rawVal = typeof nuevoEstado === 'object'
      ? (nuevoEstado.nuevoEstado || nuevoEstado.estado || '')
      : (nuevoEstado || '');

    const key = rawVal.toString().trim().toLowerCase();
    const estadoCanonico = MAPA_API_ESTADOS[key] || rawVal;

    const payload = {
      nuevoEstado: estadoCanonico
    };
    const response = await apiClient.post(`/temporadas/${id}/cambiar-estado`, payload);
    return this._unwrap(response);
  }

  /**
   * Actualizar el porcentaje de comisión de compra retenida a nivel temporada
   * PUT /api/v1/temporadas/{id}/comision-compra
   */
  async actualizarComisionCompra(id, porcentaje) {
    const comisionNum = Number(porcentaje);
    if (isNaN(comisionNum) || comisionNum < 0 || comisionNum > 100) {
      throw new Error('El porcentaje de comisión debe ser un valor numérico entre 0 y 100.');
    }
    const payload = {
      porcentajeComisionCompra: comisionNum
    };
    const response = await apiClient.put(`/temporadas/${id}/comision-compra`, payload);
    return this._unwrap(response);
  }

  /**
   * Retorna el siguiente estado en el flujo productivo o null si ya está cerrada
   */
  obtenerSiguienteEstado(estadoActual) {
    if (!estadoActual) return null;
    const normalizado = estadoActual.toString().toLowerCase()
      .normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/\s+/g, '');

    const idx = ESTADOS_TEMPORADA.findIndex(e => {
      const eNorm = e.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/\s+/g, '');
      return eNorm === normalizado;
    });

    if (idx >= 0 && idx < ESTADOS_TEMPORADA.length - 1) {
      return ESTADOS_TEMPORADA[idx + 1];
    }
    return null;
  }
}

export const temporadasService = new TemporadasService();
