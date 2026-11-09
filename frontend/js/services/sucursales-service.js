/**
 * SIGA - services/sucursales-service.js
 * Servicio de Gestión de Sucursales
 */

import { apiClient } from '../utils/api-client.js';
import { storage } from '../utils/storage.js';
import { CONFIG } from '../config.js';

// Semilla de sucursales para desarrollo y prueba inicial
const SEED_SUCURSALES = [
  {
    id: 1,
    codigo: 'SUC-CBA-01',
    nombre: 'Sucursal Central - Nueva Córdoba',
    direccion: 'Bv. Chacabuco 780',
    ciudad: 'Córdoba Capital',
    provincia: 'Córdoba',
    telefono: '+54 351 468-9000',
    capacidad: 120,
    responsable: 'Lic. Mariano Valdéz',
    activa: true,
    fechaAlta: '2024-01-10T08:00:00Z'
  },
  {
    id: 2,
    codigo: 'SUC-CBA-02',
    nombre: 'Sucursal Cerro de las Rosas',
    direccion: 'Av. Rafael Núñez 4210',
    ciudad: 'Córdoba Capital',
    provincia: 'Córdoba',
    telefono: '+54 351 481-2244',
    capacidad: 85,
    responsable: 'Cra. Gabriela Torres',
    activa: true,
    fechaAlta: '2024-03-15T09:00:00Z'
  },
  {
    id: 3,
    codigo: 'SUC-VCP-01',
    nombre: 'Sucursal Villa Carlos Paz',
    direccion: 'Av. San Martín 250',
    ciudad: 'Villa Carlos Paz',
    provincia: 'Córdoba',
    telefono: '+54 3541 42-1133',
    capacidad: 60,
    responsable: 'Ing. Rodrigo Paz',
    activa: true,
    fechaAlta: '2024-06-01T08:30:00Z'
  },
  {
    id: 4,
    codigo: 'SUC-RCU-01',
    nombre: 'Sucursal Río Cuarto',
    direccion: 'Constitución 680',
    ciudad: 'Río Cuarto',
    provincia: 'Córdoba',
    telefono: '+54 358 464-5500',
    capacidad: 70,
    responsable: 'Sr. Fernando Mansilla',
    activa: true,
    fechaAlta: '2024-09-20T10:00:00Z'
  },
  {
    id: 5,
    codigo: 'SUC-BUE-01',
    nombre: 'Sucursal Buenos Aires - Microcentro',
    direccion: 'Av. Corrientes 1450',
    ciudad: 'CABA',
    provincia: 'Buenos Aires',
    telefono: '+54 11 5234-9800',
    capacidad: 150,
    responsable: 'Lic. Laura Di Marco',
    activa: true,
    fechaAlta: '2024-11-05T09:00:00Z'
  },
  {
    id: 6,
    codigo: 'SUC-ROS-01',
    nombre: 'Sucursal Rosario Centro',
    direccion: 'Peatonal Córdoba 1120',
    ciudad: 'Rosario',
    provincia: 'Santa Fe',
    telefono: '+54 341 425-6677',
    capacidad: 90,
    responsable: 'Dr. Alejandro Busto',
    activa: false,
    fechaAlta: '2025-01-10T08:00:00Z'
  }
];

class SucursalesService {
  constructor() {
    this._initLocalStore();
  }

  _initLocalStore() {
    if (!storage.get(CONFIG.STORAGE_KEYS.MOCK_SUCURSALES)) {
      storage.set(CONFIG.STORAGE_KEYS.MOCK_SUCURSALES, SEED_SUCURSALES);
    }
  }

  _getLocalSucursales() {
    this._initLocalStore();
    return storage.get(CONFIG.STORAGE_KEYS.MOCK_SUCURSALES, SEED_SUCURSALES);
  }

  _saveLocalSucursales(sucursales) {
    storage.set(CONFIG.STORAGE_KEYS.MOCK_SUCURSALES, sucursales);
  }

  /**
   * Alias de listar para compatibilidad
   */
  async getAll() {
    return this.listar();
  }

  /**
   * Listar todas las sucursales con filtros de búsqueda y estado
   */
  async listar({ search = '', estado = 'todas' } = {}) {
    try {
      const params = { search, estado };
      return await apiClient.get('/sucursales', params);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        console.info('[SucursalesService] Usando repositorio local mock.');
        let list = this._getLocalSucursales();

        if (search && search.trim() !== '') {
          const q = search.trim().toLowerCase();
          list = list.filter(s =>
            (s.nombre && s.nombre.toLowerCase().includes(q)) ||
            (s.codigo && s.codigo.toLowerCase().includes(q)) ||
            (s.ciudad && s.ciudad.toLowerCase().includes(q)) ||
            (s.responsable && s.responsable.toLowerCase().includes(q))
          );
        }

        if (estado === 'activas') {
          list = list.filter(s => s.activa === true);
        } else if (estado === 'inactivas') {
          list = list.filter(s => s.activa === false);
        }

        return list;
      }
      throw err;
    }
  }

  /**
   * Obtener sucursal por ID
   */
  async obtenerPorId(id) {
    try {
      return await apiClient.get(`/sucursales/${id}`);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        const list = this._getLocalSucursales();
        const sucursal = list.find(s => String(s.id) === String(id));
        if (!sucursal) throw new Error('Sucursal no encontrada');
        return sucursal;
      }
      throw err;
    }
  }

  /**
   * Crear nueva sucursal
   */
  async crear(data) {
    try {
      return await apiClient.post('/sucursales', data);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        const list = this._getLocalSucursales();
        const nextId = list.length > 0 ? Math.max(...list.map(s => Number(s.id) || 0)) + 1 : 1;
        const nueva = {
          ...data,
          id: nextId,
          codigo: data.codigo || `SUC-${String(nextId).padStart(2, '0')}`,
          activa: data.activa !== undefined ? data.activa : true,
          fechaAlta: new Date().toISOString()
        };
        list.push(nueva);
        this._saveLocalSucursales(list);
        return nueva;
      }
      throw err;
    }
  }

  /**
   * Actualizar sucursal existente
   */
  async actualizar(id, data) {
    try {
      return await apiClient.put(`/sucursales/${id}`, data);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        const list = this._getLocalSucursales();
        const index = list.findIndex(s => String(s.id) === String(id));
        if (index === -1) throw new Error('Sucursal a actualizar no encontrada');
        list[index] = { ...list[index], ...data, id: Number(id) };
        this._saveLocalSucursales(list);
        return list[index];
      }
      throw err;
    }
  }

  /**
   * Desactivar / Eliminar sucursal
   */
  async eliminar(id) {
    try {
      return await apiClient.delete(`/sucursales/${id}`);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        const list = this._getLocalSucursales();
        const index = list.findIndex(s => String(s.id) === String(id));
        if (index === -1) throw new Error('Sucursal no encontrada');
        list[index].activa = false;
        this._saveLocalSucursales(list);
        return { success: true, message: 'Sucursal desactivada correctamente' };
      }
      throw err;
    }
  }

  /**
   * Métricas rápidas para el dashboard
   */
  async obtenerMetricas() {
    try {
      const list = await this.listar();
      const total = list.length;
      const activas = list.filter(s => s.activa).length;
      const capacidadTotal = list.filter(s => s.activa).reduce((acc, curr) => acc + (Number(curr.capacidad) || 0), 0);
      return { total, activas, inactivas: total - activas, capacidadTotal };
    } catch {
      return { total: 0, activas: 0, inactivas: 0, capacidadTotal: 0 };
    }
  }
}

export const sucursalesService = new SucursalesService();
