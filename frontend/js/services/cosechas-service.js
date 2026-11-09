/**
 * SIGA - services/cosechas-service.js
 * Servicio de Gestión de Cosechas por Sucursal y Acreditación de Bulbos
 */

import { apiClient } from '../utils/api-client.js';
import { storage } from '../utils/storage.js';
import { CONFIG } from '../config.js';

// Semilla inicial de cosechas para desarrollo / fallback mock
const SEED_COSECHAS = [
  {
    id: 1,
    temporadaId: 2,
    nombreTemporada: 'Temporada 2025-2026',
    sucursalId: 1,
    nombreSucursal: 'Sucursal Central - Nueva Córdoba',
    fechaInicio: '2025-10-15T08:00:00Z',
    fechaFin: null,
    estado: 0,
    nombreEstado: 'EnProceso',
    usuarioCreadorId: 1,
    observaciones: 'Cosecha general iniciada en Sucursal Central',
    totalCosechado: 6400,
    detalles: [
      {
        id: 1,
        cosechaId: 1,
        loteId: 1,
        identificacionFila: 'F-01',
        titularOriginalId: 1,
        nombreTitularOriginal: 'Martín Gómez',
        titularVigenteId: 2,
        nombreTitularVigente: 'Lucía Fernández',
        claseBulbo: 0,
        nombreClase: 'Clase0',
        cantidad: 800
      },
      {
        id: 2,
        cosechaId: 1,
        loteId: 1,
        identificacionFila: 'F-01',
        titularOriginalId: 1,
        nombreTitularOriginal: 'Martín Gómez',
        titularVigenteId: 2,
        nombreTitularVigente: 'Lucía Fernández',
        claseBulbo: 1,
        nombreClase: 'Clase1',
        cantidad: 1800
      },
      {
        id: 3,
        cosechaId: 1,
        loteId: 1,
        identificacionFila: 'F-01',
        titularOriginalId: 1,
        nombreTitularOriginal: 'Martín Gómez',
        titularVigenteId: 2,
        nombreTitularVigente: 'Lucía Fernández',
        claseBulbo: 2,
        nombreClase: 'Clase2',
        cantidad: 2200
      },
      {
        id: 4,
        cosechaId: 1,
        loteId: 1,
        identificacionFila: 'F-01',
        titularOriginalId: 1,
        nombreTitularOriginal: 'Martín Gómez',
        titularVigenteId: 2,
        nombreTitularVigente: 'Lucía Fernández',
        claseBulbo: 3,
        nombreClase: 'Clase3',
        cantidad: 1200
      },
      {
        id: 5,
        cosechaId: 1,
        loteId: 1,
        identificacionFila: 'F-01',
        titularOriginalId: 1,
        nombreTitularOriginal: 'Martín Gómez',
        titularVigenteId: 2,
        nombreTitularVigente: 'Lucía Fernández',
        claseBulbo: 4,
        nombreClase: 'Clase4',
        cantidad: 400
      }
    ],
    fechaCreacion: '2025-10-15T08:00:00Z'
  }
];

class CosechasService {
  constructor() {
    this._initLocalStore();
  }

  _initLocalStore() {
    if (!storage.get(CONFIG.STORAGE_KEYS.MOCK_COSECHAS)) {
      storage.set(CONFIG.STORAGE_KEYS.MOCK_COSECHAS, SEED_COSECHAS);
    }
  }

  _getLocalCosechas() {
    this._initLocalStore();
    return storage.get(CONFIG.STORAGE_KEYS.MOCK_COSECHAS, SEED_COSECHAS);
  }

  _saveLocalCosechas(list) {
    storage.set(CONFIG.STORAGE_KEYS.MOCK_COSECHAS, list);
  }

  _unwrap(res) {
    if (res && typeof res === 'object' && 'data' in res && res.data !== null && res.data !== undefined) {
      return res.data;
    }
    return res;
  }

  /**
   * Listar cosechas registradas para una temporada
   * GET /api/v1/cosechas/por-temporada/{temporadaId}
   */
  async listarPorTemporada(temporadaId) {
    try {
      const response = await apiClient.get(`/cosechas/por-temporada/${temporadaId}`);
      const data = this._unwrap(response);
      return Array.isArray(data) ? data : (data.items || []);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        console.info('[CosechasService] Usando fallback local mock para listar por temporada.');
        const list = this._getLocalCosechas();
        if (!temporadaId) return list;
        return list.filter(c => String(c.temporadaId) === String(temporadaId));
      }
      throw err;
    }
  }

  /**
   * Obtener cosecha por ID
   * GET /api/v1/cosechas/{id}
   */
  async obtenerPorId(id) {
    try {
      const response = await apiClient.get(`/cosechas/${id}`);
      return this._unwrap(response);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        console.info('[CosechasService] Usando fallback local mock para obtener por id.');
        const list = this._getLocalCosechas();
        const found = list.find(c => String(c.id) === String(id));
        if (!found) throw new Error(`Cosecha #${id} no encontrada`);
        return found;
      }
      throw err;
    }
  }

  /**
   * Iniciar una nueva cosecha para una sucursal en una temporada
   * POST /api/v1/cosechas
   * Body: { temporadaId, sucursalId, observaciones }
   */
  async crear(dto) {
    try {
      const response = await apiClient.post('/cosechas', dto);
      return this._unwrap(response);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        console.info('[CosechasService] Usando fallback local mock para crear cosecha.');
        const list = this._getLocalCosechas();

        // Regla: No duplicar cosecha por (temporadaId, sucursalId)
        const existente = list.find(c =>
          String(c.temporadaId) === String(dto.temporadaId) &&
          String(c.sucursalId) === String(dto.sucursalId)
        );
        if (existente) {
          throw new Error('Ya existe una cosecha registrada para esta sucursal en la temporada seleccionada.');
        }

        // Obtener nombres de temporada y sucursal si están disponibles en mocks
        let nombreTemporada = `Temporada #${dto.temporadaId}`;
        const temporadasMock = storage.get(CONFIG.STORAGE_KEYS.MOCK_TEMPORADAS, []);
        const tempObj = temporadasMock.find(t => String(t.id) === String(dto.temporadaId));
        if (tempObj) nombreTemporada = tempObj.nombre;

        let nombreSucursal = `Sucursal #${dto.sucursalId}`;
        const sucursalesMock = storage.get(CONFIG.STORAGE_KEYS.MOCK_SUCURSALES, []);
        const sucObj = sucursalesMock.find(s => String(s.id) === String(dto.sucursalId));
        if (sucObj) nombreSucursal = sucObj.nombre;

        const nextId = list.length > 0 ? Math.max(...list.map(c => Number(c.id) || 0)) + 1 : 1;
        const nueva = {
          id: nextId,
          temporadaId: Number(dto.temporadaId),
          nombreTemporada,
          sucursalId: Number(dto.sucursalId),
          nombreSucursal,
          fechaInicio: new Date().toISOString(),
          fechaFin: null,
          estado: 0,
          nombreEstado: 'EnProceso',
          usuarioCreadorId: 1,
          observaciones: dto.observaciones || '',
          totalCosechado: 0,
          detalles: [],
          fechaCreacion: new Date().toISOString()
        };

        list.push(nueva);
        this._saveLocalCosechas(list);
        return nueva;
      }
      throw err;
    }
  }

  /**
   * Registrar pesaje / cantidades cosechadas de un lote
   * POST /api/v1/cosechas/{cosechaId}/detalle
   * Body: { loteId, cantidades: [{ claseBulbo, cantidad }] }
   */
  async registrarDetalle(cosechaId, dto) {
    try {
      const response = await apiClient.post(`/cosechas/${cosechaId}/detalle`, dto);
      return this._unwrap(response);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        console.info('[CosechasService] Usando fallback local mock para registrar detalle.');
        const list = this._getLocalCosechas();
        const index = list.findIndex(c => String(c.id) === String(cosechaId));
        if (index === -1) throw new Error('Cosecha no encontrada');

        const cosecha = list[index];
        if (cosecha.estado === 2 || cosecha.nombreEstado === 'Cerrada') {
          throw new Error('No se pueden registrar ni modificar cantidades en una cosecha cerrada.');
        }

        // Buscar información del lote en mock
        const lotes = storage.get(CONFIG.STORAGE_KEYS.MOCK_LOTES, []);
        const lote = lotes.find(l => String(l.id) === String(dto.loteId));

        const clientes = storage.get(CONFIG.STORAGE_KEYS.MOCK_CLIENTES, []);
        const titularOriginalId = lote ? Number(lote.clienteId) : 1;
        const cliOrig = clientes.find(c => Number(c.id) === titularOriginalId);
        const nombreOriginal = cliOrig ? (cliOrig.razonSocial || `${cliOrig.nombre || ''} ${cliOrig.apellido || ''}`.trim()) : `Cliente #${titularOriginalId}`;

        // Titular vigente considerando transferencias
        const transferencias = storage.get(CONFIG.STORAGE_KEYS.MOCK_TRANSFERENCIAS, []);
        const transfLote = transferencias.filter(t => Number(t.loteId) === Number(dto.loteId));
        let titularVigenteId = titularOriginalId;
        let nombreVigente = nombreOriginal;

        if (transfLote.length > 0) {
          const ultima = transfLote[transfLote.length - 1];
          titularVigenteId = Number(ultima.nuevoTitularId);
          nombreVigente = ultima.nombreNuevoTitular || `Cliente #${titularVigenteId}`;
        }

        const identificacionFila = lote?.filaIdentificacion || 'Fila';

        if (!cosecha.detalles) cosecha.detalles = [];

        // Registrar o actualizar los ítems por clase
        for (const item of (dto.cantidades || [])) {
          const claseBulbo = Number(item.claseBulbo);
          const cantidad = Number(item.cantidad) || 0;
          const nombreClase = `Clase${claseBulbo}`;

          const existingDetalle = cosecha.detalles.find(d =>
            Number(d.loteId) === Number(dto.loteId) && Number(d.claseBulbo) === claseBulbo
          );

          if (existingDetalle) {
            existingDetalle.cantidad = cantidad;
          } else {
            const nextDetalleId = cosecha.detalles.length > 0
              ? Math.max(...cosecha.detalles.map(d => Number(d.id) || 0)) + 1
              : 1;

            cosecha.detalles.push({
              id: nextDetalleId,
              cosechaId: Number(cosechaId),
              loteId: Number(dto.loteId),
              identificacionFila,
              titularOriginalId,
              nombreTitularOriginal: nombreOriginal,
              titularVigenteId,
              nombreTitularVigente: nombreVigente,
              claseBulbo,
              nombreClase,
              cantidad
            });
          }
        }

        // Recalcular total cosechado
        cosecha.totalCosechado = cosecha.detalles.reduce((sum, d) => sum + (Number(d.cantidad) || 0), 0);
        list[index] = cosecha;
        this._saveLocalCosechas(list);
        return cosecha;
      }
      throw err;
    }
  }

  /**
   * Cerrar la cosecha de la sucursal y acreditar stock a titulares vigentes
   * POST /api/v1/cosechas/{cosechaId}/cerrar
   */
  async cerrar(cosechaId) {
    try {
      const response = await apiClient.post(`/cosechas/${cosechaId}/cerrar`);
      return this._unwrap(response);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        console.info('[CosechasService] Usando fallback local mock para cerrar cosecha.');
        const list = this._getLocalCosechas();
        const index = list.findIndex(c => String(c.id) === String(cosechaId));
        if (index === -1) throw new Error('Cosecha no encontrada');

        const cosecha = list[index];
        if (cosecha.estado === 2 || cosecha.nombreEstado === 'Cerrada') {
          throw new Error('La cosecha ya se encuentra cerrada.');
        }

        cosecha.estado = 2;
        cosecha.nombreEstado = 'Cerrada';
        cosecha.fechaFin = new Date().toISOString();

        list[index] = cosecha;
        this._saveLocalCosechas(list);
        return cosecha;
      }
      throw err;
    }
  }
}

export const cosechasService = new CosechasService();
