/**
 * SIGA - services/ventas-service.js
 * Servicio de Gestión Comercial de Ventas de Bulbos, Precios de Referencia,
 * Generación de Contratos, Emisión de Facturas y Circuito Administrativo.
 */

import { apiClient } from '../utils/api-client.js';
import { storage } from '../utils/storage.js';
import { CONFIG } from '../config.js';

// Semilla de precios de referencia por clase (DP-4)
const SEED_PRECIOS_CLASE = [
  { id: 1, temporadaId: 2, nombreTemporada: 'Temporada 2025-2026', claseBulbo: 0, nombreClase: 'Clase0', precioReferencia: 250.00, fechaVigencia: '2025-03-01T08:00:00Z' },
  { id: 2, temporadaId: 2, nombreTemporada: 'Temporada 2025-2026', claseBulbo: 1, nombreClase: 'Clase1', precioReferencia: 210.00, fechaVigencia: '2025-03-01T08:00:00Z' },
  { id: 3, temporadaId: 2, nombreTemporada: 'Temporada 2025-2026', claseBulbo: 2, nombreClase: 'Clase2', precioReferencia: 180.00, fechaVigencia: '2025-03-01T08:00:00Z' },
  { id: 4, temporadaId: 2, nombreTemporada: 'Temporada 2025-2026', claseBulbo: 3, nombreClase: 'Clase3', precioReferencia: 140.00, fechaVigencia: '2025-03-01T08:00:00Z' },
  { id: 5, temporadaId: 2, nombreTemporada: 'Temporada 2025-2026', claseBulbo: 4, nombreClase: 'Clase4', precioReferencia: 95.00, fechaVigencia: '2025-03-01T08:00:00Z' }
];

// Semilla de ventas registradas para modo mock
const SEED_VENTAS = [
  {
    id: 1,
    clienteId: 1,
    codigoCliente: 'CLI-001',
    nombreCliente: 'Martín Gómez',
    temporadaId: 2,
    nombreTemporada: 'Temporada 2025-2026',
    vendedorId: 1,
    nombreVendedor: 'Administrador',
    origenParticipanteId: 1,
    fecha: '2025-04-10T11:00:00Z',
    fechaCreacion: '2025-04-10T11:00:00Z',
    montoTotal: 475000.00,
    totalBulbos: 2500,
    estado: 1,
    nombreEstado: 'EnRevisionAdministracion',
    observaciones: 'Venta inicial temporada bulbos seleccionados',
    detalles: [
      { id: 1, ventaId: 1, claseBulbo: 0, nombreClase: 'Clase0', cantidad: 500, precioUnitarioPactado: 250.00, subtotal: 125000.00 },
      { id: 2, ventaId: 1, claseBulbo: 1, nombreClase: 'Clase1', cantidad: 1000, precioUnitarioPactado: 210.00, subtotal: 210000.00 },
      { id: 3, ventaId: 1, claseBulbo: 2, nombreClase: 'Clase2', cantidad: 1000, precioUnitarioPactado: 140.00, subtotal: 140000.00 }
    ],
    contrato: null,
    factura: null,
    historialAprobaciones: [
      { id: 1, ventaId: 1, usuarioAprobadorId: 1, nombreUsuarioAprobador: 'Admin', fecha: '2025-04-10T11:30:00Z', accion: 0, nombreAccion: 'EnviadoARevision', observaciones: 'Enviado para revisión y elaboración de contrato' }
    ]
  },
  {
    id: 2,
    clienteId: 2,
    codigoCliente: 'CLI-002',
    nombreCliente: 'Lucía Fernández',
    temporadaId: 2,
    nombreTemporada: 'Temporada 2025-2026',
    vendedorId: 1,
    nombreVendedor: 'Administrador',
    origenParticipanteId: 1,
    fecha: '2025-04-05T09:00:00Z',
    fechaCreacion: '2025-04-05T09:00:00Z',
    montoTotal: 352500.00,
    totalBulbos: 3000,
    estado: 3,
    nombreEstado: 'Facturada',
    observaciones: 'Venta de bulbos de multiplicación semillero',
    detalles: [
      { id: 4, ventaId: 2, claseBulbo: 3, nombreClase: 'Clase3', cantidad: 1500, precioUnitarioPactado: 140.00, subtotal: 210000.00 },
      { id: 5, ventaId: 2, claseBulbo: 4, nombreClase: 'Clase4', cantidad: 1500, precioUnitarioPactado: 95.00, subtotal: 142500.00 }
    ],
    contrato: {
      id: 1,
      ventaId: 2,
      numeroContrato: 'CTR-2025-001',
      fechaGeneracion: '2025-04-06T10:00:00Z',
      archivoUrl: 'https://ejemplo.com/contrato-001.pdf',
      estado: 0,
      nombreEstado: 'Generado',
      usuarioGeneradorId: 1
    },
    factura: {
      id: 1,
      ventaId: 2,
      numeroFactura: 'FAC-A-0001-00001234',
      fechaEmision: '2025-04-07T14:00:00Z',
      montoTotal: 352500.00,
      archivoUrl: 'https://ejemplo.com/factura-1234.pdf',
      estado: 0,
      nombreEstado: 'Emitida',
      usuarioEmisionId: 1
    },
    historialAprobaciones: [
      { id: 2, ventaId: 2, usuarioAprobadorId: 1, nombreUsuarioAprobador: 'Admin', fecha: '2025-04-05T09:30:00Z', accion: 0, nombreAccion: 'EnviadoARevision', observaciones: 'Revisión formal' },
      { id: 3, ventaId: 2, usuarioAprobadorId: 1, nombreUsuarioAprobador: 'Admin', fecha: '2025-04-06T11:00:00Z', accion: 1, nombreAccion: 'AprobadoAdministracion', observaciones: 'Contrato verificado y firmado' },
      { id: 4, ventaId: 2, usuarioAprobadorId: 1, nombreUsuarioAprobador: 'Admin', fecha: '2025-04-07T14:30:00Z', accion: 3, nombreAccion: 'Facturado', observaciones: 'Factura A emitida formalmente' }
    ]
  }
];

class VentasService {
  constructor() {
    this._initLocalStore();
  }

  _initLocalStore() {
    if (!storage.get(CONFIG.STORAGE_KEYS.MOCK_PRECIOS_CLASE)) {
      storage.set(CONFIG.STORAGE_KEYS.MOCK_PRECIOS_CLASE, SEED_PRECIOS_CLASE);
    }
    if (!storage.get(CONFIG.STORAGE_KEYS.MOCK_VENTAS)) {
      storage.set(CONFIG.STORAGE_KEYS.MOCK_VENTAS, SEED_VENTAS);
    }
  }

  _getLocalPrecios() {
    this._initLocalStore();
    return storage.get(CONFIG.STORAGE_KEYS.MOCK_PRECIOS_CLASE, SEED_PRECIOS_CLASE);
  }

  _saveLocalPrecios(list) {
    storage.set(CONFIG.STORAGE_KEYS.MOCK_PRECIOS_CLASE, list);
  }

  _getLocalVentas() {
    this._initLocalStore();
    return storage.get(CONFIG.STORAGE_KEYS.MOCK_VENTAS, SEED_VENTAS);
  }

  _saveLocalVentas(list) {
    storage.set(CONFIG.STORAGE_KEYS.MOCK_VENTAS, list);
  }

  _unwrap(res) {
    if (res && typeof res === 'object' && 'data' in res && res.data !== null && res.data !== undefined) {
      return res.data;
    }
    return res;
  }

  _normalizeClaseBulbo(val) {
    if (typeof val === 'number' && !Number.isNaN(val)) return val;
    const str = String(val ?? '').trim();
    const match = str.match(/(\d+)/);
    return match ? Number(match[1]) : 0;
  }

  _normalizeEstadoVenta(val) {
    if (typeof val === 'number' && !Number.isNaN(val)) return val;
    const map = {
      registrada: 0,
      enrevisionadministracion: 1,
      aprobadaadministracion: 2,
      facturada: 3,
      completada: 4,
      rechazada: 5,
      anulada: 6
    };
    const key = String(val ?? '').trim().toLowerCase();
    if (key in map) return map[key];
    const num = Number(val);
    return Number.isNaN(num) ? 0 : num;
  }

  _normalizePrecioClase(p) {
    if (!p) return p;
    const claseNum = this._normalizeClaseBulbo(p.claseBulbo ?? p.nombreClase);
    return {
      ...p,
      claseBulbo: claseNum,
      nombreClase: p.nombreClase || `Clase${claseNum}`,
      precioReferencia: Number(p.precioReferencia) || 0
    };
  }

  _normalizeVenta(v) {
    if (!v) return v;
    const estadoNum = this._normalizeEstadoVenta(v.estado ?? v.nombreEstado);
    const detalles = Array.isArray(v.detalles)
      ? v.detalles.map(d => {
          const claseNum = this._normalizeClaseBulbo(d.claseBulbo ?? d.nombreClase);
          return {
            ...d,
            claseBulbo: claseNum,
            nombreClase: d.nombreClase || `Clase${claseNum}`
          };
        })
      : [];
    return {
      ...v,
      estado: estadoNum,
      nombreEstado: v.nombreEstado || (typeof v.estado === 'string' ? v.estado : 'Registrada'),
      detalles
    };
  }

  /**
   * Listar precios de referencia de bulbos por clase para una temporada
   * GET /api/v1/ventas/precios/temporada/{temporadaId}
   */
  async listarPreciosPorTemporada(temporadaId) {
    try {
      const response = await apiClient.get(`/ventas/precios/temporada/${temporadaId}`);
      const data = this._unwrap(response);
      const list = Array.isArray(data) ? data : (data?.items || []);
      return list.map(p => this._normalizePrecioClase(p));
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        console.info('[VentasService] Fallback mock para listar precios por temporada.');
        const list = this._getLocalPrecios();
        if (!temporadaId) return list.map(p => this._normalizePrecioClase(p));
        return list
          .filter(p => String(p.temporadaId) === String(temporadaId))
          .map(p => this._normalizePrecioClase(p));
      }
      throw err;
    }
  }

  /**
   * Configurar o actualizar el precio de referencia de una clase de bulbo
   * POST /api/v1/ventas/precios
   * Body: { temporadaId, claseBulbo, precioReferencia }
   */
  async configurarPrecioClase(dto) {
    try {
      const response = await apiClient.post('/ventas/precios', dto);
      return this._normalizePrecioClase(this._unwrap(response));
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        console.info('[VentasService] Fallback mock para configurar precio clase.');
        const list = this._getLocalPrecios();
        const index = list.findIndex(p =>
          String(p.temporadaId) === String(dto.temporadaId) &&
          Number(p.claseBulbo) === Number(dto.claseBulbo)
        );

        if (index >= 0) {
          list[index].precioReferencia = Number(dto.precioReferencia);
          list[index].fechaVigencia = new Date().toISOString();
          this._saveLocalPrecios(list);
          return this._normalizePrecioClase(list[index]);
        }

        const nextId = list.length > 0 ? Math.max(...list.map(p => Number(p.id) || 0)) + 1 : 1;
        const nuevo = {
          id: nextId,
          temporadaId: Number(dto.temporadaId),
          nombreTemporada: `Temporada #${dto.temporadaId}`,
          claseBulbo: Number(dto.claseBulbo),
          nombreClase: `Clase${dto.claseBulbo}`,
          precioReferencia: Number(dto.precioReferencia),
          fechaVigencia: new Date().toISOString()
        };
        list.push(nuevo);
        this._saveLocalPrecios(list);
        return this._normalizePrecioClase(nuevo);
      }
      throw err;
    }
  }

  /**
   * Listar todas las ventas registradas
   * GET /api/v1/ventas
   */
  async listarTodas() {
    try {
      const response = await apiClient.get('/ventas');
      const data = this._unwrap(response);
      const list = Array.isArray(data) ? data : (data?.items || []);
      return list.map(v => this._normalizeVenta(v));
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        console.info('[VentasService] Fallback mock para listar todas las ventas.');
        const list = this._getLocalVentas();
        return [...list]
          .sort((a, b) => Number(b.id) - Number(a.id))
          .map(v => this._normalizeVenta(v));
      }
      throw err;
    }
  }

  /**
   * Obtener venta por ID
   * GET /api/v1/ventas/{id}
   */
  async obtenerPorId(id) {
    try {
      const response = await apiClient.get(`/ventas/${id}`);
      return this._normalizeVenta(this._unwrap(response));
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        console.info('[VentasService] Fallback mock para obtener venta por ID.');
        const list = this._getLocalVentas();
        const found = list.find(v => String(v.id) === String(id));
        if (!found) throw new Error(`Venta #${id} no encontrada`);
        return this._normalizeVenta(found);
      }
      throw err;
    }
  }

  /**
   * Registrar una nueva venta
   * POST /api/v1/ventas
   * Body: { clienteId, temporadaId, origenParticipanteId, observaciones, detalles: [{ claseBulbo, cantidad, precioUnitarioPactado }] }
   */
  async crear(dto) {
    try {
      const response = await apiClient.post('/ventas', dto);
      return this._normalizeVenta(this._unwrap(response));
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        console.info('[VentasService] Fallback mock para crear venta.');
        const list = this._getLocalVentas();
        const clientes = storage.get(CONFIG.STORAGE_KEYS.MOCK_CLIENTES, []);
        const cli = clientes.find(c => String(c.id) === String(dto.clienteId));
        const nombreCliente = cli ? (cli.razonSocial || `${cli.nombre || ''} ${cli.apellido || ''}`.trim()) : `Cliente #${dto.clienteId}`;
        const codigoCliente = cli ? cli.codigoCliente : `CLI-${dto.clienteId}`;

        let nombreTemporada = null;
        if (dto.temporadaId) {
          const temps = storage.get(CONFIG.STORAGE_KEYS.MOCK_TEMPORADAS, []);
          const t = temps.find(x => String(x.id) === String(dto.temporadaId));
          if (t) nombreTemporada = t.nombre;
        }

        const nextId = list.length > 0 ? Math.max(...list.map(v => Number(v.id) || 0)) + 1 : 1;

        let totalBulbos = 0;
        let montoTotal = 0;

        const detalles = (dto.detalles || []).map((d, idx) => {
          const cant = Number(d.cantidad) || 0;
          const precio = Number(d.precioUnitarioPactado) || 0;
          const sub = cant * precio;
          totalBulbos += cant;
          montoTotal += sub;
          return {
            id: idx + 1,
            ventaId: nextId,
            claseBulbo: Number(d.claseBulbo),
            nombreClase: `Clase${d.claseBulbo}`,
            cantidad: cant,
            precioUnitarioPactado: precio,
            subtotal: sub
          };
        });

        const nueva = {
          id: nextId,
          clienteId: Number(dto.clienteId),
          codigoCliente,
          nombreCliente,
          temporadaId: dto.temporadaId ? Number(dto.temporadaId) : null,
          nombreTemporada,
          vendedorId: 1,
          nombreVendedor: 'Administrador',
          origenParticipanteId: dto.origenParticipanteId ? Number(dto.origenParticipanteId) : 1,
          fecha: new Date().toISOString(),
          fechaCreacion: new Date().toISOString(),
          montoTotal,
          totalBulbos,
          estado: 0,
          nombreEstado: 'Registrada',
          observaciones: dto.observaciones || '',
          detalles,
          contrato: null,
          factura: null,
          historialAprobaciones: []
        };

        list.unshift(nueva);
        this._saveLocalVentas(list);
        return nueva;
      }
      throw err;
    }
  }

  /**
   * Enviar venta a revisión de Secretaría / Administración
   * POST /api/v1/ventas/{id}/enviar-revision
   */
  async enviarARevision(id, observaciones = '') {
    try {
      const payload = { observaciones: typeof observaciones === 'string' ? observaciones : (observaciones?.observaciones || '') };
      const response = await apiClient.post(`/ventas/${id}/enviar-revision`, payload);
      return this._normalizeVenta(this._unwrap(response));
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        console.info('[VentasService] Fallback mock para enviar a revisión.');
        const list = this._getLocalVentas();
        const v = list.find(x => String(x.id) === String(id));
        if (!v) throw new Error('Venta no encontrada');

        v.estado = 1;
        v.nombreEstado = 'EnRevisionAdministracion';
        if (!v.historialAprobaciones) v.historialAprobaciones = [];
        v.historialAprobaciones.push({
          id: v.historialAprobaciones.length + 1,
          ventaId: v.id,
          usuarioAprobadorId: 1,
          nombreUsuarioAprobador: 'Admin',
          fecha: new Date().toISOString(),
          accion: 0,
          nombreAccion: 'EnviadoARevision',
          observaciones: typeof observaciones === 'string' ? observaciones : (observaciones?.observaciones || 'Enviado a revisión')
        });
        this._saveLocalVentas(list);
        return v;
      }
      throw err;
    }
  }

  /**
   * Generar contrato formal de compraventa
   * POST /api/v1/ventas/{id}/contrato
   * Body: { numeroContrato, archivoUrl }
   */
  async generarContrato(id, dto) {
    try {
      const response = await apiClient.post(`/ventas/${id}/contrato`, dto);
      return this._normalizeVenta(this._unwrap(response));
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        console.info('[VentasService] Fallback mock para generar contrato.');
        const list = this._getLocalVentas();
        const v = list.find(x => String(x.id) === String(id));
        if (!v) throw new Error('Venta no encontrada');

        v.contrato = {
          id: (v.contrato?.id || 1),
          ventaId: v.id,
          numeroContrato: dto.numeroContrato,
          archivoUrl: dto.archivoUrl || null,
          fechaGeneracion: new Date().toISOString(),
          estado: 0,
          nombreEstado: 'Generado',
          usuarioGeneradorId: 1
        };
        this._saveLocalVentas(list);
        return v;
      }
      throw err;
    }
  }

  /**
   * Aprobar venta por Secretaría / Administración
   * POST /api/v1/ventas/{id}/aprobar-administracion
   */
  async aprobarAdministracion(id, observaciones = '') {
    try {
      const payload = { observaciones: typeof observaciones === 'string' ? observaciones : (observaciones?.observaciones || '') };
      const response = await apiClient.post(`/ventas/${id}/aprobar-administracion`, payload);
      return this._normalizeVenta(this._unwrap(response));
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        console.info('[VentasService] Fallback mock para aprobar administración.');
        const list = this._getLocalVentas();
        const v = list.find(x => String(x.id) === String(id));
        if (!v) throw new Error('Venta no encontrada');

        v.estado = 2;
        v.nombreEstado = 'AprobadaAdministracion';
        if (!v.historialAprobaciones) v.historialAprobaciones = [];
        v.historialAprobaciones.push({
          id: v.historialAprobaciones.length + 1,
          ventaId: v.id,
          usuarioAprobadorId: 1,
          nombreUsuarioAprobador: 'Admin',
          fecha: new Date().toISOString(),
          accion: 1,
          nombreAccion: 'AprobadoAdministracion',
          observaciones: typeof observaciones === 'string' ? observaciones : (observaciones?.observaciones || 'Aprobado por Administración')
        });
        this._saveLocalVentas(list);
        return v;
      }
      throw err;
    }
  }

  /**
   * Emitir factura formal de la venta por Contabilidad
   * POST /api/v1/ventas/{id}/factura
   * Body: { numeroFactura, archivoUrl, observaciones }
   */
  async emitirFactura(id, dto) {
    try {
      const response = await apiClient.post(`/ventas/${id}/factura`, dto);
      return this._normalizeVenta(this._unwrap(response));
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        console.info('[VentasService] Fallback mock para emitir factura.');
        const list = this._getLocalVentas();
        const v = list.find(x => String(x.id) === String(id));
        if (!v) throw new Error('Venta no encontrada');

        v.estado = 3;
        v.nombreEstado = 'Facturada';
        v.factura = {
          id: (v.factura?.id || 1),
          ventaId: v.id,
          numeroFactura: dto.numeroFactura,
          archivoUrl: dto.archivoUrl || null,
          montoTotal: v.montoTotal,
          fechaEmision: new Date().toISOString(),
          estado: 0,
          nombreEstado: 'Emitida',
          usuarioEmisionId: 1
        };

        if (!v.historialAprobaciones) v.historialAprobaciones = [];
        v.historialAprobaciones.push({
          id: v.historialAprobaciones.length + 1,
          ventaId: v.id,
          usuarioAprobadorId: 1,
          nombreUsuarioAprobador: 'Admin',
          fecha: new Date().toISOString(),
          accion: 3,
          nombreAccion: 'Facturado',
          observaciones: dto.observaciones || `Factura ${dto.numeroFactura} emitida`
        });

        this._saveLocalVentas(list);
        return v;
      }
      throw err;
    }
  }

  /**
   * Aprobación final y acreditación de stock de bulbos al comprador
   * POST /api/v1/ventas/{id}/aprobar-final
   */
  async aprobarFinal(id, observaciones = '') {
    try {
      const payload = { observaciones: typeof observaciones === 'string' ? observaciones : (observaciones?.observaciones || '') };
      const response = await apiClient.post(`/ventas/${id}/aprobar-final`, payload);
      return this._normalizeVenta(this._unwrap(response));
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        console.info('[VentasService] Fallback mock para aprobación final.');
        const list = this._getLocalVentas();
        const v = list.find(x => String(x.id) === String(id));
        if (!v) throw new Error('Venta no encontrada');

        v.estado = 4;
        v.nombreEstado = 'Completada';
        if (!v.historialAprobaciones) v.historialAprobaciones = [];
        v.historialAprobaciones.push({
          id: v.historialAprobaciones.length + 1,
          ventaId: v.id,
          usuarioAprobadorId: 1,
          nombreUsuarioAprobador: 'Admin',
          fecha: new Date().toISOString(),
          accion: 4,
          nombreAccion: 'AprobacionFinal',
          observaciones: typeof observaciones === 'string' ? observaciones : (observaciones?.observaciones || 'Aprobación final completada')
        });

        this._saveLocalVentas(list);
        return v;
      }
      throw err;
    }
  }

  /**
   * Rechazar u observar venta en el circuito
   * POST /api/v1/ventas/{id}/rechazar
   */
  async rechazar(id, observaciones) {
    try {
      const payload = { observaciones: typeof observaciones === 'string' ? observaciones : (observaciones?.observaciones || '') };
      const response = await apiClient.post(`/ventas/${id}/rechazar`, payload);
      return this._normalizeVenta(this._unwrap(response));
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        console.info('[VentasService] Fallback mock para rechazar venta.');
        const list = this._getLocalVentas();
        const v = list.find(x => String(x.id) === String(id));
        if (!v) throw new Error('Venta no encontrada');

        v.estado = 5;
        v.nombreEstado = 'Rechazada';
        if (!v.historialAprobaciones) v.historialAprobaciones = [];
        v.historialAprobaciones.push({
          id: v.historialAprobaciones.length + 1,
          ventaId: v.id,
          usuarioAprobadorId: 1,
          nombreUsuarioAprobador: 'Admin',
          fecha: new Date().toISOString(),
          accion: 5,
          nombreAccion: 'Rechazado',
          observaciones: typeof observaciones === 'string' ? observaciones : (observaciones?.observaciones || 'Venta rechazada')
        });

        this._saveLocalVentas(list);
        return v;
      }
      throw err;
    }
  }
}

export const ventasService = new VentasService();
