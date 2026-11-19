/**
 * SIGA - services/clientes-service.js
 * Servicio de Gestión de Clientes (CRUD y Baja Lógica)
 */

import { apiClient } from '../utils/api-client.js';
import { CONFIG } from '../config.js';

class ClientesService {
  _unwrap(res) {
    if (res && typeof res === 'object' && 'data' in res && res.data !== null && res.data !== undefined) {
      return res.data;
    }
    return res;
  }

  _normalizeCliente(c) {
    if (!c) return c;
    let nombre = c.nombre || (c.persona && c.persona.nombre) || '';
    let apellido = c.apellido || (c.persona && c.persona.apellido) || '';
    if (!nombre && !apellido && c.nombreCompleto) {
      const parts = String(c.nombreCompleto).trim().split(/\s+/);
      nombre = parts[0] || '';
      apellido = parts.slice(1).join(' ') || '';
    }
    const nombreCompleto = c.nombreCompleto || `${nombre} ${apellido}`.trim();
    const tipoDocumento = c.tipoDocumento || (c.persona && c.persona.tipoDocumento) || 'DNI';
    const documento = c.documento || (c.persona && c.persona.documento) || '';
    const email = c.email || (c.persona && c.persona.email) || '';
    const telefono = c.telefono || (c.persona && c.persona.telefono) || '';
    const codigo = c.codigoCliente || c.codigo || `CLI-${String(c.id).padStart(4, '0')}`;

    let activo = true;
    if (c.activo !== undefined && c.activo !== null) {
      activo = Boolean(c.activo);
    } else if (c.estadoCliente !== undefined && c.estadoCliente !== null) {
      const est = String(c.estadoCliente).toLowerCase();
      activo = est === 'activo' || est === '1';
    }

    return {
      ...c,
      id: c.id,
      personaId: c.personaId || (c.persona && c.persona.id) || null,
      nombre,
      apellido,
      nombreCompleto,
      tipoDocumento,
      documento,
      email,
      telefono,
      codigo,
      codigoCliente: codigo,
      sucursalId: c.sucursalId ?? c.persona?.sucursalId ?? null,
      sucursalNombre: c.sucursalNombre ?? c.persona?.sucursal?.nombre ?? null,
      observaciones: c.observaciones ?? '',
      activo,
      estadoCliente: activo ? 'Activo' : 'Inactivo',
      fechaAlta: c.fechaAlta || c.fechaRegistro || new Date().toISOString(),
      fechaRegistro: c.fechaRegistro || c.fechaAlta || new Date().toISOString(),
    };
  }

  _filterAndPaginate(rawList, { page = 1, pageSize = CONFIG.PAGINATION.DEFAULT_PAGE_SIZE, search = '', estado = 'todos' } = {}) {
    let list = rawList.map(c => this._normalizeCliente(c));

    if (search && search.trim() !== '') {
      const q = search.trim().toLowerCase();
      list = list.filter(c =>
        (c.nombre && c.nombre.toLowerCase().includes(q)) ||
        (c.apellido && c.apellido.toLowerCase().includes(q)) ||
        (c.nombreCompleto && c.nombreCompleto.toLowerCase().includes(q)) ||
        (c.documento && c.documento.toLowerCase().includes(q)) ||
        (c.codigo && c.codigo.toLowerCase().includes(q)) ||
        (c.email && c.email.toLowerCase().includes(q))
      );
    }

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

  /**
   * Listar clientes con paginación, filtros de búsqueda y estado
   */
  async listar({ page = 1, pageSize = CONFIG.PAGINATION.DEFAULT_PAGE_SIZE, search = '', estado = 'todos' } = {}) {
    const params = search && search.trim() !== '' ? { busqueda: search.trim() } : {};
    const response = await apiClient.get('/clientes', params);
    const unwrapped = this._unwrap(response);

    if (Array.isArray(unwrapped)) {
      return this._filterAndPaginate(unwrapped, { page, pageSize, search, estado });
    }
    if (unwrapped && Array.isArray(unwrapped.items)) {
      return {
        ...unwrapped,
        items: unwrapped.items.map(c => this._normalizeCliente(c))
      };
    }
    return { items: [], total: 0, page: Number(page), pageSize: Number(pageSize), totalPages: 1 };
  }

  /**
   * Búsqueda rápida asíncrona para Autocomplete / Typeahead
   * @param {string} busqueda Texto a buscar (DNI, nombre, apellido, código) o vacío para recomendaciones
   * @param {number} limit Límite de resultados devueltos (default: 10)
   */
  async buscarRapido(busqueda = '', limit = 10) {
    try {
      const params = { limit };
      if (busqueda && typeof busqueda === 'string' && busqueda.trim().length > 0) {
        params.busqueda = busqueda.trim();
      }
      const response = await apiClient.get('/clientes', params);
      const unwrapped = this._unwrap(response);
      const items = Array.isArray(unwrapped) ? unwrapped : (unwrapped?.items || []);
      return items.map(c => this._normalizeCliente(c));
    } catch (err) {
      console.warn('[ClientesService] Error en búsqueda rápida:', err);
      return [];
    }
  }

  /**
   * Obtener cliente por ID
   */
  async obtenerPorId(id) {
    const response = await apiClient.get(`/clientes/${id}`);
    return this._normalizeCliente(this._unwrap(response));
  }

  /**
   * Crear nuevo cliente
   */
  async crear(clienteData) {
    const payload = {
      ...clienteData,
      codigoCliente: clienteData.codigoCliente || clienteData.codigo || null
    };
    const response = await apiClient.post('/clientes', payload);
    return this._normalizeCliente(this._unwrap(response));
  }

  /**
   * Actualizar cliente existente
   */
  async actualizar(id, clienteData) {
    const estadoCliente = clienteData.estadoCliente ||
      (clienteData.activo !== undefined ? (clienteData.activo ? 'Activo' : 'Inactivo') : undefined);
    const payload = {
      ...clienteData,
      codigoCliente: clienteData.codigoCliente || clienteData.codigo || undefined,
      estadoCliente
    };
    const response = await apiClient.put(`/clientes/${id}`, payload);
    return this._normalizeCliente(this._unwrap(response));
  }

  /**
   * Baja lógica de cliente (Soft Delete: cambia activo a false / EstadoCliente a Inactivo)
   */
  async eliminar(id) {
    return this.actualizar(id, { activo: false, estadoCliente: 'Inactivo' });
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
