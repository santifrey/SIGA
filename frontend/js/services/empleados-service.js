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
    try {
      const response = await apiClient.get('/empleados');
      const data = this._unwrap(response);
      return Array.isArray(data) ? data : [];
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        console.info('[EmpleadosService] Fallback mock para listado de empleados');
        return [
          {
            id: 1,
            personaId: 1,
            username: 'admin',
            nombre: 'Administrador',
            apellido: 'SIGA',
            nombreCompleto: 'Administrador SIGA',
            documento: '00000000',
            telefono: '123456789',
            email: 'admin@azafranmediterraneo.com',
            rolId: 1,
            rol: 'ADMINISTRADOR',
            estado: 'Activo',
            ultimoAcceso: new Date().toISOString(),
            fechaCreacion: '2026-01-01T00:00:00Z'
          },
          {
            id: 2,
            personaId: 2,
            username: 'vendedor1',
            nombre: 'Carlos',
            apellido: 'Ventas',
            nombreCompleto: 'Carlos Ventas',
            documento: '30111222',
            telefono: '+54 351 444-5555',
            email: 'carlos.ventas@siga.com',
            rolId: 2,
            rol: 'VENDEDOR',
            estado: 'Activo',
            ultimoAcceso: new Date().toISOString(),
            fechaCreacion: '2026-02-01T00:00:00Z'
          }
        ];
      }
      throw err;
    }
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
