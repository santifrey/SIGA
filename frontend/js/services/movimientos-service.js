/**
 * SIGA - services/movimientos-service.js
 * Servicio de Gestión de Movimientos de Stock de Bulbos
 */

import { apiClient } from '../utils/api-client.js';

class MovimientosService {
  _unwrap(res) {
    if (res && typeof res === 'object' && 'data' in res && res.data !== null && res.data !== undefined) {
      return res.data;
    }
    return res;
  }

  _parseClaseIndex(val) {
    if (typeof val === 'number' && !Number.isNaN(val)) return val;
    const match = String(val ?? '').match(/(\d+)/);
    return match ? Number(match[1]) : 0;
  }

  _normalizeMovimiento(m) {
    if (!m) return m;
    const cantidades = (m.cantidades || []).map(c => {
      const idx = this._parseClaseIndex(c.claseBulbo ?? c.clase);
      return {
        ...c,
        claseBulbo: idx,
        clase: idx,
        nombreClase: c.nombreClase || `Clase ${idx}`,
        cantidad: Number(c.cantidad) || 0
      };
    });
    const totalBulbos = Number(m.totalBulbos) || cantidades.reduce((acc, c) => acc + c.cantidad, 0);
    return {
      ...m,
      numeroComprobante: m.numeroComprobante || `MOV-${String(m.id).padStart(4, '0')}`,
      estado: String(m.estado || 'Pendiente'),
      cantidades,
      totalBulbos
    };
  }

  /**
   * Listar movimientos con filtros
   */
  async listar({ page = 1, pageSize = 500, clienteId = '', estado = 'todos' } = {}) {
    const params = {};
    if (clienteId && clienteId !== 'todos' && clienteId !== 'empresa') {
      params.clienteId = Number(clienteId);
    }
    if (estado && estado !== 'todos') {
      params.estado = estado;
    }
    const response = await apiClient.get('/movimientos', params);
    const unwrapped = this._unwrap(response);
    let list = (Array.isArray(unwrapped) ? unwrapped : (unwrapped?.items || [])).map(m => this._normalizeMovimiento(m));
    list.sort((a, b) => Number(b.id) - Number(a.id));
    const total = list.length;
    const startIndex = (page - 1) * pageSize;
    const items = list.slice(startIndex, startIndex + pageSize);
    return { items, total, page: Number(page), pageSize: Number(pageSize) };
  }

  /**
   * Obtener detalle de movimiento
   */
  async obtenerPorId(id) {
    const response = await apiClient.get(`/movimientos/${id}`);
    return this._normalizeMovimiento(this._unwrap(response));
  }

  /**
   * Crear nuevo movimiento o ajuste en estado Pendiente
   * @param {object} data { origenId, destinoId, observaciones, cantidades: [{ claseBulbo, cantidad }] }
   */
  async crear(data) {
    const origenId = data.origenId === 'empresa' ? 1 : (data.origenId !== undefined && data.origenId !== null ? Number(data.origenId) : 0);
    const destinoId = data.destinoId === 'empresa' ? 1 : (data.destinoId !== undefined && data.destinoId !== null ? Number(data.destinoId) : 0);
    const cantidades = (data.cantidades || []).map(c => ({
      clase: this._parseClaseIndex(c.claseBulbo ?? c.clase),
      cantidad: Number(c.cantidad) || 0
    }));
    const payload = {
      tipoOperacion: data.tipoOperacion || 'AjusteStock',
      origenId,
      destinoId,
      observaciones: data.observaciones || null,
      cantidades
    };
    const response = await apiClient.post('/movimientos', payload);
    return this._normalizeMovimiento(this._unwrap(response));
  }

  /**
   * Registrar un Ajuste de Stock (quitar o añadir bulbos a una sucursal o a un cliente con motivo)
   * @param {object} param0 { entidad: 'sucursal'|'empresa'|'cliente', sucursalId, sucursalNombre, clienteId, accion: 'sumar'|'restar', cantidades, motivo, autoConfirmar }
   */
  async registrarAjuste({ entidad, sucursalId, sucursalNombre, clienteId, accion, cantidades, motivo, autoConfirmar = false }) {
    if (!motivo || !motivo.trim()) {
      throw new Error('Debe ingresar el motivo u observación del ajuste de stock.');
    }

    let origenId = 0;
    let destinoId = 1;
    let etiqueta = '';

    if (entidad === 'sucursal' || entidad === 'empresa') {
      const targetId = Number(sucursalId) || 1;
      const sNombre = sucursalNombre ? `Sucursal: ${sucursalNombre}` : 'Sucursal';
      if (accion === 'sumar') {
        origenId = 0;
        destinoId = targetId;
        etiqueta = `[Ingreso a ${sNombre}] ${motivo.trim()}`;
      } else {
        origenId = targetId;
        destinoId = 0;
        etiqueta = `[Egreso de ${sNombre}] ${motivo.trim()}`;
      }
    } else {
      const cId = Number(clienteId);
      if (!cId || cId <= 0) {
        throw new Error('Debe seleccionar el cliente para registrar el ajuste.');
      }
      if (accion === 'sumar') {
        origenId = 0;
        destinoId = cId;
        etiqueta = `[Ingreso a Cliente] ${motivo.trim()}`;
      } else {
        origenId = cId;
        destinoId = 0;
        etiqueta = `[Egreso de Cliente] ${motivo.trim()}`;
      }
    }

    const nuevo = await this.crear({
      origenId,
      destinoId,
      observaciones: etiqueta,
      cantidades
    });

    if (autoConfirmar && nuevo && nuevo.id) {
      await this.confirmar(nuevo.id);
      nuevo.estado = 'Confirmado';
    }

    return nuevo;
  }

  /**
   * Confirmar movimiento (impacta el stock)
   */
  async confirmar(id) {
    return await apiClient.post(`/movimientos/${id}/confirmar`, {});
  }

  /**
   * Anular movimiento
   */
  async anular(id, motivo = '') {
    return await apiClient.post(`/movimientos/${id}/anular`, { motivo });
  }
}

export const movimientosService = new MovimientosService();
