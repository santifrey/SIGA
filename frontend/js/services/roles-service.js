/**
 * SIGA - services/roles-service.js
 * Servicio para gestión CRUD de roles y catálogo de permisos (/api/v1/roles y /api/v1/permisos) (Feature 31)
 */

import { apiClient } from '../utils/api-client.js';

class RolesService {
  _unwrap(res) {
    if (res && typeof res === 'object' && 'data' in res && res.data !== null && res.data !== undefined) {
      return res.data;
    }
    return res;
  }

  async listar() {
    const response = await apiClient.get('/roles');
    const data = this._unwrap(response);
    return Array.isArray(data) ? data : [];
  }

  async obtenerPorId(id) {
    const response = await apiClient.get(`/roles/${id}`);
    return this._unwrap(response);
  }

  async listarPermisos() {
    const response = await apiClient.get('/permisos');
    const data = this._unwrap(response);
    return Array.isArray(data) ? data : [];
  }

  async crear(dto) {
    const response = await apiClient.post('/roles', dto);
    return this._unwrap(response);
  }

  async actualizar(id, dto) {
    const response = await apiClient.put(`/roles/${id}`, dto);
    return this._unwrap(response);
  }

  async eliminar(id) {
    const response = await apiClient.delete(`/roles/${id}`);
    return this._unwrap(response);
  }
}

export const rolesService = new RolesService();
