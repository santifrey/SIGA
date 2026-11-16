/**
 * SIGA - services/movimientos-comerciales-service.js
 * Servicio de Gestión Comercial Unificada (Feature 34):
 * - VentaACliente (Tipo 1)
 * - CompraACliente (Tipo 2)
 * - TraspasoEntreClientes (Tipo 3)
 * - Circuito formal de estados (0..4), MIX decomposition, contratos y facturación.
 */

import { apiClient } from '../utils/api-client.js';
import { storage } from '../utils/storage.js';
import { CONFIG } from '../config.js';

export const TIPOS_MOVIMIENTO = {
  1: { id: 1, codigo: 'VentaACliente', label: 'Venta a Cliente', badgeClass: 'badge-primary' },
  2: { id: 2, codigo: 'CompraACliente', label: 'Compra a Cliente', badgeClass: 'badge-accent' },
  3: { id: 3, codigo: 'TraspasoEntreClientes', label: 'Traspaso entre Clientes', badgeClass: 'badge-info' }
};

export const ESTADOS_MOVIMIENTO = {
  0: { id: 0, codigo: 'PendienteRevision', label: 'Pendiente Revisión', badgeClass: 'badge-warning' },
  1: { id: 1, codigo: 'AprobadoAdmin', label: 'Aprobado Administración', badgeClass: 'badge-info' },
  2: { id: 2, codigo: 'Facturado', label: 'Facturado', badgeClass: 'badge-primary' },
  3: { id: 3, codigo: 'Finalizado', label: 'Finalizado', badgeClass: 'badge-success' },
  4: { id: 4, codigo: 'Rechazado', label: 'Rechazado', badgeClass: 'badge-danger' }
};

export const CLASES_BULBO = [
  { id: 0, codigo: 'Clase0', label: 'Clase 0 (> 12cm)', desc: 'Bulbo calibre premium extra grande' },
  { id: 1, codigo: 'Clase1', label: 'Clase 1 (10 - 12cm)', desc: 'Bulbo comercial estándar' },
  { id: 2, codigo: 'Clase2', label: 'Clase 2 (8 - 10cm)', desc: 'Bulbo mediano' },
  { id: 3, codigo: 'Clase3', label: 'Clase 3 (6 - 8cm)', desc: 'Bulbo desarrollo chico' },
  { id: 4, codigo: 'Clase4', label: 'Clase 4 (< 6cm)', desc: 'Semillero / cría' },
  { id: 5, codigo: 'MIX',    label: 'MIX (1/3 Cal 1, 2, 3)', desc: 'Mezcla comercial (múltiplo de 3)' }
];

/**
 * Extrae el índice numérico (0..5) de la clase de bulbo de manera robusta.
 * Maneja números (0..5), strings ("Clase0".."Clase4", "0".."5") y "MIX" (5).
 * @param {string|number} val
 * @returns {number}
 */
export function _parseClaseIndex(val) {
  if (val === undefined || val === null) return 0;
  if (typeof val === 'number') return isNaN(val) ? 0 : val;
  const s = String(val).trim().toUpperCase();
  if (s === 'MIX' || s === '5') return 5;
  const match = s.match(/\d+/);
  return match ? parseInt(match[0], 10) : 0;
}

export const ACCIONES_APROBACION = {
  0: 'Registrado / Enviado a Revisión',
  1: 'Aprobado Administración',
  2: 'Observado',
  3: 'Facturado',
  4: 'Finalizado e Inventario Impactado',
  5: 'Rechazado',
  6: 'Corregido y Reenviado'
};

const STORAGE_KEY_MOCK_MOVIMIENTOS = 'siga_mock_movimientos_comerciales';

class MovimientosComercialesService {
  _unwrap(res) {
    if (res && typeof res === 'object' && 'data' in res && res.data !== null && res.data !== undefined) {
      return res.data;
    }
    return res;
  }

  /**
   * Listar movimientos comerciales con filtros
   * GET /api/v1/movimientos-comerciales
   */
  async listar(filtro = {}) {
    try {
      const params = {};
      if (filtro.tipo !== undefined && filtro.tipo !== null && filtro.tipo !== 'todos' && filtro.tipo !== '') {
        params.tipo = Number(filtro.tipo);
      }
      if (filtro.estado !== undefined && filtro.estado !== null && filtro.estado !== 'todos' && filtro.estado !== '') {
        params.estado = Number(filtro.estado);
      }
      if (filtro.clienteId && filtro.clienteId !== 'todos') {
        params.clienteId = Number(filtro.clienteId);
      }
      if (filtro.temporadaId && filtro.temporadaId !== 'todos') {
        params.temporadaId = Number(filtro.temporadaId);
      }
      if (filtro.fechaDesde) params.fechaDesde = filtro.fechaDesde;
      if (filtro.fechaHasta) params.fechaHasta = filtro.fechaHasta;

      const response = await apiClient.get('/movimientos-comerciales', params);
      const data = this._unwrap(response);
      return Array.isArray(data) ? data : (data?.items || []);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        console.info('[MovimientosComercialesService] Fallback mock para listar.');
        return this._getLocalMovimientos(filtro);
      }
      throw err;
    }
  }

  /**
   * Obtener detalle completo de un movimiento comercial
   * GET /api/v1/movimientos-comerciales/{id}
   */
  async obtenerPorId(id) {
    try {
      const response = await apiClient.get(`/movimientos-comerciales/${id}`);
      return this._unwrap(response);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        const item = this._getLocalMovimientos().find(m => Number(m.id) === Number(id));
        if (item) return item;
      }
      throw err;
    }
  }

  /**
   * Verificar si un cliente es nuevo y requiere contrato
   * GET /api/v1/movimientos-comerciales/verificar-cliente-nuevo/{clienteId}
   */
  async verificarClienteNuevo(clienteId, excludeMovimientoId = null) {
    const numId = Number(clienteId);
    if (!numId) return { esNuevo: false, requiereContrato: false };

    try {
      const params = excludeMovimientoId ? { excludeMovimientoId: Number(excludeMovimientoId) } : {};
      const response = await apiClient.get(`/movimientos-comerciales/verificar-cliente-nuevo/${numId}`, params);
      return this._unwrap(response);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        return { esNuevo: false, requiereContrato: false };
      }
      throw err;
    }
  }

  /**
   * Crear nuevo movimiento comercial
   * POST /api/v1/movimientos-comerciales
   */
  async crear(dto) {
    const response = await apiClient.post('/movimientos-comerciales', dto);
    return this._unwrap(response);
  }

  /**
   * Modificar y reenviar movimiento rechazado
   * PUT /api/v1/movimientos-comerciales/{id}
   */
  async actualizar(id, dto) {
    const response = await apiClient.put(`/movimientos-comerciales/${id}`, dto);
    return this._unwrap(response);
  }

  /**
   * Aprobación administrativa
   * POST /api/v1/movimientos-comerciales/{id}/aprobar
   */
  async aprobar(id, dto = {}) {
    const response = await apiClient.post(`/movimientos-comerciales/${id}/aprobar`, dto);
    return this._unwrap(response);
  }

  /**
   * Generar contrato previo
   * POST /api/v1/movimientos-comerciales/{id}/contrato
   */
  async generarContrato(id, dto) {
    const response = await apiClient.post(`/movimientos-comerciales/${id}/contrato`, dto);
    return this._unwrap(response);
  }

  async contrato(id, dto) {
    return this.generarContrato(id, dto);
  }

  /**
   * Emitir factura comercial
   * POST /api/v1/movimientos-comerciales/{id}/facturar
   */
  async facturar(id, dto) {
    const response = await apiClient.post(`/movimientos-comerciales/${id}/facturar`, dto);
    return this._unwrap(response);
  }

  /**
   * Finalizar movimiento e impactar stock físico
   * POST /api/v1/movimientos-comerciales/{id}/finalizar
   */
  async finalizar(id, dto = {}) {
    const response = await apiClient.post(`/movimientos-comerciales/${id}/finalizar`, dto);
    return this._unwrap(response);
  }

  /**
   * Rechazar movimiento comercial con motivo obligatorio
   * POST /api/v1/movimientos-comerciales/{id}/rechazar
   */
  async rechazar(id, dtoOrMotivo) {
    const payload = typeof dtoOrMotivo === 'string' ? { motivo: dtoOrMotivo } : dtoOrMotivo;
    if (!payload || !payload.motivo || !payload.motivo.trim()) {
      throw new Error('Debe indicar el motivo del rechazo u observación.');
    }
    const response = await apiClient.post(`/movimientos-comerciales/${id}/rechazar`, payload);
    return this._unwrap(response);
  }

  // Local storage mock helpers
  _getLocalMovimientos(filtro = {}) {
    const raw = storage.get(STORAGE_KEY_MOCK_MOVIMIENTOS, []);
    let list = Array.isArray(raw) ? raw : [];
    if (filtro.tipo && filtro.tipo !== 'todos') {
      list = list.filter(m => Number(m.tipo) === Number(filtro.tipo));
    }
    if (filtro.estado && filtro.estado !== 'todos') {
      list = list.filter(m => Number(m.estado) === Number(filtro.estado));
    }
    if (filtro.clienteId && filtro.clienteId !== 'todos') {
      list = list.filter(m => Number(m.clienteId) === Number(filtro.clienteId) || Number(m.origenParticipanteId) === Number(filtro.clienteId) || Number(m.destinoParticipanteId) === Number(filtro.clienteId));
    }
    if (filtro.temporadaId && filtro.temporadaId !== 'todos') {
      list = list.filter(m => Number(m.temporadaId) === Number(filtro.temporadaId));
    }
    return list;
  }
}

export const movimientosComercialesService = new MovimientosComercialesService();
