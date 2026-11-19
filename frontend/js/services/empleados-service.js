/**
 * SIGA - services/empleados-service.js
 * Servicio para gestión CRUD de personal / empleados (/api/v1/empleados) (Feature 30)
 */

import { apiClient } from '../utils/api-client.js';
import { CONFIG } from '../config.js';

class EmpleadosService {
  _unwrap(res) {
    if (res && typeof res === 'object' && 'data' in res && res.data !== null && res.data !== undefined) {
      return res.data;
    }
    return res;
  }

  async listar() {
    const response = await apiClient.get('/empleados');
    const data = this._unwrap(response);
    return Array.isArray(data) ? data : [];
  }

  async obtenerPorId(id) {
    const response = await apiClient.get(`/empleados/${id}`);
    return this._unwrap(response);
  }

  async crear(dto) {
    const response = await apiClient.post('/empleados', dto);
    return this._unwrap(response);
  }

  async actualizar(id, dto) {
    const response = await apiClient.put(`/empleados/${id}`, dto);
    return this._unwrap(response);
  }

  async desactivar(id) {
    const response = await apiClient.delete(`/empleados/${id}`);
    return this._unwrap(response);
  }
}

export const empleadosService = new EmpleadosService();
