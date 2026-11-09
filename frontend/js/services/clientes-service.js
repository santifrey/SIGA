/**
 * SIGA - services/clientes-service.js
 * Servicio de Gestión de Clientes (CRUD y Baja Lógica)
 */

import { apiClient } from '../utils/api-client.js';
import { storage } from '../utils/storage.js';
import { CONFIG } from '../config.js';

// Semilla de datos iniciales para demostración y desarrollo local
const SEED_CLIENTES = [
  {
    id: 1,
    codigo: 'CLI-00101',
    nombre: 'Martín',
    apellido: 'Gómez',
    tipoDocumento: 'DNI',
    documento: '32456789',
    email: 'martin.gomez@gmail.com',
    telefono: '+54 351 456-7890',
    observaciones: 'Cliente corporativo preferencial. Factura A.',
    activo: true,
    fechaAlta: '2025-02-10T10:30:00Z'
  },
  {
    id: 2,
    codigo: 'CLI-00102',
    nombre: 'Lucía',
    apellido: 'Fernández',
    tipoDocumento: 'DNI',
    documento: '35123987',
    email: 'lucia.fernandez@outlook.com',
    telefono: '+54 351 789-1234',
    observaciones: 'Compras recurrentes en Sucursal Centro.',
    activo: true,
    fechaAlta: '2025-02-14T14:15:00Z'
  },
  {
    id: 3,
    codigo: 'CLI-00103',
    nombre: 'Carlos',
    apellido: 'Benítez',
    tipoDocumento: 'CUIT',
    documento: '20-28994512-4',
    email: 'carlos.benitez@empresa.com.ar',
    telefono: '+54 351 612-3456',
    observaciones: 'Cuenta corriente habilitada con límite de crédito.',
    activo: true,
    fechaAlta: '2025-03-01T09:00:00Z'
  },
  {
    id: 4,
    codigo: 'CLI-00104',
    nombre: 'Mariana',
    apellido: 'Sosa',
    tipoDocumento: 'DNI',
    documento: '38901234',
    email: 'mariana.sosa@live.com',
    telefono: '+54 351 555-8821',
    observaciones: 'Atención personalizada vía WhatsApp.',
    activo: true,
    fechaAlta: '2025-03-12T16:20:00Z'
  },
  {
    id: 5,
    codigo: 'CLI-00105',
    nombre: 'Facundo',
    apellido: 'Álvarez',
    tipoDocumento: 'DNI',
    documento: '30456123',
    email: 'facundo.alvarez@gmail.com',
    telefono: '+54 351 999-3322',
    observaciones: 'Registró mora temporal en 2025. Regularizado.',
    activo: false,
    fechaAlta: '2025-01-20T11:45:00Z'
  },
  {
    id: 6,
    codigo: 'CLI-00106',
    nombre: 'Valeria',
    apellido: 'Romero',
    tipoDocumento: 'DNI',
    documento: '36789456',
    email: 'valeria.romero@estudiojuridico.com',
    telefono: '+54 351 444-1188',
    observaciones: 'Asociada al convenio institucional de comercio.',
    activo: true,
    fechaAlta: '2025-03-25T13:00:00Z'
  },
  {
    id: 7,
    codigo: 'CLI-00107',
    nombre: 'Gonzalo',
    apellido: 'Molina',
    tipoDocumento: 'DNI',
    documento: '29876543',
    email: 'gmolina@logistica-norte.com.ar',
    telefono: '+54 351 321-6549',
    observaciones: 'Descuento por volumen mayorista aplicado.',
    activo: true,
    fechaAlta: '2025-04-02T08:15:00Z'
  },
  {
    id: 8,
    codigo: 'CLI-00108',
    nombre: 'Florencia',
    apellido: 'Castro',
    tipoDocumento: 'DNI',
    documento: '41234567',
    email: 'florencia.castro@yahoo.com.ar',
    telefono: '+54 351 888-2211',
    observaciones: 'Adherida al débito automático mensual.',
    activo: true,
    fechaAlta: '2025-04-10T17:40:00Z'
  },
  {
    id: 9,
    codigo: 'CLI-00109',
    nombre: 'Esteban',
    apellido: 'Paredes',
    tipoDocumento: 'CUIT',
    documento: '20-33445566-9',
    email: 'esteban.paredes@metalurgica.com',
    telefono: '+54 351 777-6655',
    observaciones: 'Proveedor y cliente mixto.',
    activo: true,
    fechaAlta: '2025-04-18T10:05:00Z'
  },
  {
    id: 10,
    codigo: 'CLI-00110',
    nombre: 'Agustina',
    apellido: 'Herrera',
    tipoDocumento: 'DNI',
    documento: '37112233',
    email: 'agustina.herrera@hotmail.com',
    telefono: '+54 351 222-9988',
    observaciones: 'Baja solicitada por cambio de radicación.',
    activo: false,
    fechaAlta: '2025-01-15T15:30:00Z'
  }
];

class ClientesService {
  constructor() {
    this._initLocalStore();
  }

  _initLocalStore() {
    if (!storage.get(CONFIG.STORAGE_KEYS.MOCK_CLIENTES)) {
      storage.set(CONFIG.STORAGE_KEYS.MOCK_CLIENTES, SEED_CLIENTES);
    }
  }

  _getLocalClientes() {
    this._initLocalStore();
    return storage.get(CONFIG.STORAGE_KEYS.MOCK_CLIENTES, SEED_CLIENTES);
  }

  _saveLocalClientes(clientes) {
    storage.set(CONFIG.STORAGE_KEYS.MOCK_CLIENTES, clientes);
  }

  /**
   * Listar clientes con paginación, filtros de búsqueda y estado
   */
  async listar({ page = 1, pageSize = CONFIG.PAGINATION.DEFAULT_PAGE_SIZE, search = '', estado = 'todos' } = {}) {
    try {
      const params = { page, pageSize, search, estado };
      const response = await apiClient.get('/clientes', params);
      
      // Adaptación de respuestas del backend si viene como { items, total } o array directo
      if (Array.isArray(response)) {
        return { items: response, total: response.length, page, pageSize };
      }
      return response;
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        console.info('[ClientesService] Usando repositorio local mock.');
        let list = this._getLocalClientes();

        // Filtro por búsqueda (nombre, apellido, documento, código, email)
        if (search && search.trim() !== '') {
          const q = search.trim().toLowerCase();
          list = list.filter(c =>
            (c.nombre && c.nombre.toLowerCase().includes(q)) ||
            (c.apellido && c.apellido.toLowerCase().includes(q)) ||
            (c.documento && c.documento.toLowerCase().includes(q)) ||
            (c.codigo && c.codigo.toLowerCase().includes(q)) ||
            (c.email && c.email.toLowerCase().includes(q))
          );
        }

        // Filtro por estado
        if (estado === 'activos') {
          list = list.filter(c => c.activo === true);
        } else if (estado === 'inactivos') {
          list = list.filter(c => c.activo === false);
        }

        const total = list.length;
        const startIndex = (page - 1) * pageSize;
        const paginatedItems = list.slice(startIndex, startIndex + pageSize);

        return {
          items: paginatedItems,
          total,
          page: Number(page),
          pageSize: Number(pageSize),
          totalPages: Math.ceil(total / pageSize) || 1
        };
      }
      throw err;
    }
  }

  /**
   * Obtener cliente por ID
   */
  async obtenerPorId(id) {
    try {
      return await apiClient.get(`/clientes/${id}`);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        const list = this._getLocalClientes();
        const cliente = list.find(c => String(c.id) === String(id));
        if (!cliente) throw new Error('Cliente no encontrado');
        return cliente;
      }
      throw err;
    }
  }

  /**
   * Crear nuevo cliente
   */
  async crear(clienteData) {
    try {
      return await apiClient.post('/clientes', clienteData);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        const list = this._getLocalClientes();
        const nextId = list.length > 0 ? Math.max(...list.map(c => Number(c.id) || 0)) + 1 : 1;
        const nuevoCliente = {
          ...clienteData,
          id: nextId,
          codigo: clienteData.codigo || `CLI-00${100 + nextId}`,
          activo: clienteData.activo !== undefined ? clienteData.activo : true,
          fechaAlta: new Date().toISOString()
        };
        list.unshift(nuevoCliente);
        this._saveLocalClientes(list);
        return nuevoCliente;
      }
      throw err;
    }
  }

  /**
   * Actualizar cliente existente
   */
  async actualizar(id, clienteData) {
    try {
      return await apiClient.put(`/clientes/${id}`, clienteData);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        const list = this._getLocalClientes();
        const index = list.findIndex(c => String(c.id) === String(id));
        if (index === -1) throw new Error('Cliente a actualizar no encontrado');
        list[index] = { ...list[index], ...clienteData, id: Number(id) };
        this._saveLocalClientes(list);
        return list[index];
      }
      throw err;
    }
  }

  /**
   * Baja lógica de cliente (Soft Delete: cambia activo a false)
   */
  async eliminar(id) {
    try {
      return await apiClient.delete(`/clientes/${id}`);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        const list = this._getLocalClientes();
        const index = list.findIndex(c => String(c.id) === String(id));
        if (index === -1) throw new Error('Cliente no encontrado');
        list[index].activo = false;
        this._saveLocalClientes(list);
        return { success: true, message: 'Baja lógica realizada correctamente' };
      }
      throw err;
    }
  }

  /**
   * Reactivar cliente previamente dado de baja
   */
  async reactivar(id) {
    return this.actualizar(id, { activo: true });
  }

  /**
   * Métricas rápidas para el dashboard
   */
  async obtenerMetricas() {
    try {
      const data = await this.listar({ page: 1, pageSize: 1000, estado: 'todos' });
      const items = data.items || [];
      const total = items.length;
      const activos = items.filter(c => c.activo).length;
      const inactivos = total - activos;
      return { total, activos, inactivos };
    } catch {
      return { total: 0, activos: 0, inactivos: 0 };
    }
  }
}

export const clientesService = new ClientesService();
