/**
 * SIGA - services/sucursales-service.js
 * Servicio de Gestión de Sucursales
 */

import { apiClient } from '../utils/api-client.js';

class SucursalesService {
  _unwrap(res) {
    if (res && typeof res === 'object' && 'data' in res && res.data !== null && res.data !== undefined) {
      return res.data;
    }
    return res;
  }

  _normalizeSucursal(s) {
    if (!s) return s;
    const capacidad = Number(s.capacidadTotal ?? s.capacidad ?? 0);
    const activa = s.activo !== undefined
      ? Boolean(s.activo)
      : (s.activa !== undefined
          ? Boolean(s.activa)
          : (s.estado !== undefined && s.estado !== null
              ? (typeof s.estado === 'number'
                  ? s.estado === 1
                  : ['activo', 'activa', '1', 'true'].includes(String(s.estado).toLowerCase().trim()))
              : true));

    let direccion = s.direccion || '';
    let ciudad = s.ciudad || '';
    let provincia = s.provincia || '';

    if (!direccion && s.ubicacion) {
      const parts = String(s.ubicacion).split(',').map(p => p.trim());
      direccion = parts[0] || '';
      ciudad = parts[1] || '';
      provincia = parts[2] || '';
    }

    return {
      ...s,
      id: s.id,
      nombre: s.nombre || '',
      codigo: s.codigo || `SUC-${String(s.id).padStart(2, '0')}`,
      direccion,
      ciudad,
      provincia,
      ubicacion: s.ubicacion || [direccion, ciudad, provincia].filter(Boolean).join(', '),
      telefono: s.telefono || '',
      capacidad,
      capacidadTotal: capacidad,
      participanteComercialId: s.participanteComercialId || null,
      participanteRazonSocial: s.participanteRazonSocial || null,
      responsable: s.responsable || s.nombreResponsable || '',
      activa,
      estado: activa ? 'Activa' : 'Inactiva',
      fechaAlta: s.fechaAlta || s.fechaCreacion || new Date().toISOString()
    };
  }

  async getAll() {
    return this.listar();
  }

  /**
   * Listar todas las sucursales con filtros de búsqueda y estado
   */
  async listar({ search = '', estado = 'todas', incluirTecnicas = false } = {}) {
    const response = await apiClient.get('/sucursales');
    const unwrapped = this._unwrap(response);
    let list = (Array.isArray(unwrapped) ? unwrapped : (unwrapped?.items || [])).map(s => this._normalizeSucursal(s));

    // Excluir entidades técnicas de contraparte interna salvo que se soliciten explícitamente
    if (!incluirTecnicas) {
      list = list.filter(s => s.nombre?.toLowerCase() !== 'ajustes de inventario');
    }

    if (search && search.trim() !== '') {
      const q = search.trim().toLowerCase();
      list = list.filter(s =>
        (s.nombre && s.nombre.toLowerCase().includes(q)) ||
        (s.codigo && s.codigo.toLowerCase().includes(q)) ||
        (s.ciudad && s.ciudad.toLowerCase().includes(q)) ||
        (s.direccion && s.direccion.toLowerCase().includes(q)) ||
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

  /**
   * Obtener sucursal por ID
   */
  async obtenerPorId(id) {
    const response = await apiClient.get(`/sucursales/${id}`);
    return this._normalizeSucursal(this._unwrap(response));
  }

  /**
   * Crear nueva sucursal
   */
  async crear(data) {
    const isActivo = data.activo !== undefined ? Boolean(data.activo) : (data.activa !== undefined ? Boolean(data.activa) : true);
    const payload = {
      nombre: data.nombre,
      ubicacion: data.ubicacion || [data.direccion, data.ciudad, data.provincia].filter(Boolean).join(', '),
      capacidadTotal: Number(data.capacidadTotal ?? data.capacidad ?? 0),
      activo: isActivo
    };
    const response = await apiClient.post('/sucursales', payload);
    return this._normalizeSucursal(this._unwrap(response));
  }

  /**
   * Actualizar sucursal existente
   */
  async actualizar(id, data) {
    const isActivo = data.activo !== undefined
      ? Boolean(data.activo)
      : (data.activa !== undefined ? Boolean(data.activa) : (data.estado !== 0 && data.estado !== 'Inactivo' && data.estado !== 'Inactiva'));
    const payload = {
      nombre: data.nombre,
      ubicacion: data.ubicacion || [data.direccion, data.ciudad, data.provincia].filter(Boolean).join(', '),
      capacidadTotal: Number(data.capacidadTotal ?? data.capacidad ?? 0),
      activo: isActivo
    };
    const response = await apiClient.put(`/sucursales/${id}`, payload);
    return this._normalizeSucursal(this._unwrap(response));
  }

  /**
   * Cambiar estado activo/inactivo sin eliminar físicamente
   */
  async cambiarEstado(id, activa) {
    const s = await this.obtenerPorId(id);
    return await this.actualizar(id, {
      ...s,
      activo: Boolean(activa),
      activa: Boolean(activa)
    });
  }

  /**
   * Desactivar / Eliminar sucursal (soft delete)
   */
  async eliminar(id) {
    return await apiClient.delete(`/sucursales/${id}`);
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
