/**
 * SIGA - services/precios-referencia-service.js
 * Servicio de Precios de Referencia Oficiales por Temporada (Feature 33)
 * Soporta las 6 clases comerciales (Clase 0..4 y MIX)
 */

import { apiClient } from '../utils/api-client.js';

export const CLASES_BULBO = [
  { id: 0, codigo: 'Clase0', label: 'Clase 0', calibres: '> 12 cm', descripcion: 'Bulbo calibre premium extra grande' },
  { id: 1, codigo: 'Clase1', label: 'Clase 1', calibres: '10 - 12 cm', descripcion: 'Bulbo comercial estándar de primera línea' },
  { id: 2, codigo: 'Clase2', label: 'Clase 2', calibres: '8 - 10 cm', descripcion: 'Bulbo mediano para producción' },
  { id: 3, codigo: 'Clase3', label: 'Clase 3', calibres: '6 - 8 cm', descripcion: 'Bulbo en desarrollo / multiplicación intermedia' },
  { id: 4, codigo: 'Clase4', label: 'Clase 4', calibres: '< 6 cm', descripcion: 'Bulbo semillero / cría para engorde' },
  { id: 5, codigo: 'MIX',    label: 'MIX Comercial', calibres: '6 - 12 cm', descripcion: 'Mezcla comercial en proporción 1:1:1 (Clases 1, 2 y 3)' }
];

class PreciosReferenciaService {
  _unwrap(res) {
    if (res && typeof res === 'object' && 'data' in res && res.data !== null && res.data !== undefined) {
      return res.data;
    }
    return res;
  }

  _parseClaseIndex(val) {
    if (typeof val === 'number' && !Number.isNaN(val)) return val;
    if (!val) return 0;
    const str = String(val).trim();
    if (str.toUpperCase() === 'MIX') return 5;
    const match = str.match(/(\d+)/);
    return match ? Number(match[1]) : 0;
  }

  _getClaseCodigo(index) {
    const idx = this._parseClaseIndex(index);
    const meta = CLASES_BULBO.find(c => c.id === idx);
    return meta ? meta.codigo : `Clase${idx}`;
  }

  _normalizePrecioItem(raw, claseMeta) {
    const claseId = claseMeta.id;
    const precio = Number(raw?.precioReferencia ?? raw?.precio ?? 0);
    return {
      id: raw?.id ?? 0,
      temporadaId: raw?.temporadaId ?? null,
      claseBulbo: claseId,
      claseCodigo: claseMeta.codigo,
      nombreClase: raw?.nombreClase || claseMeta.label,
      calibres: claseMeta.calibres,
      descripcion: claseMeta.descripcion,
      precioReferencia: Math.max(0, isNaN(precio) ? 0 : precio),
      porcentajeComisionCompra: raw?.porcentajeComisionCompra != null ? Number(raw.porcentajeComisionCompra) : null,
      fechaVigencia: raw?.fechaVigencia || raw?.fechaActualizacion || raw?.fechaCreacion || null
    };
  }

  /**
   * Obtiene los precios de referencia para una temporada determinada (garantiza las 6 clases)
   * GET /api/v1/precios-referencia/temporada/{temporadaId}
   */
  async listarPorTemporada(temporadaId) {
    const tempId = Number(temporadaId);
    if (!tempId) return [];

    const response = await apiClient.get(`/precios-referencia/temporada/${tempId}`);
    const data = this._unwrap(response);
    const list = Array.isArray(data) ? data : (data?.items || []);

    return CLASES_BULBO.map(claseMeta => {
      const found = list.find(it => {
        const itClaseId = this._parseClaseIndex(it.claseBulbo ?? it.clase ?? it.codigo);
        return itClaseId === claseMeta.id;
      });
      return this._normalizePrecioItem(found, claseMeta);
    });
  }

  /**
   * Obtiene el precio vigente de una clase en una temporada
   * GET /api/v1/precios-referencia/vigente/{temporadaId}/{clase}
   */
  async obtenerPrecioVigente(temporadaId, clase) {
    const tempId = Number(temporadaId);
    const claseCodigo = this._getClaseCodigo(clase);

    const response = await apiClient.get(`/precios-referencia/vigente/${tempId}/${claseCodigo}`);
    const data = this._unwrap(response);
    const claseId = this._parseClaseIndex(clase);
    const meta = CLASES_BULBO.find(c => c.id === claseId) || CLASES_BULBO[0];
    return this._normalizePrecioItem(data, meta);
  }

  /**
   * Guarda o actualiza el precio individual de una clase en una temporada
   * POST /api/v1/precios-referencia
   * Soporta tanto (dto) como (temporadaId, clase, precioReferencia)
   */
  async guardarPrecioIndividual(arg1, arg2, arg3) {
    let tempId, clase, precio;
    if (typeof arg1 === 'object' && arg1 !== null) {
      tempId = Number(arg1.temporadaId);
      clase = arg1.claseBulbo ?? arg1.clase;
      precio = Number(arg1.precioReferencia ?? arg1.precio);
    } else {
      tempId = Number(arg1);
      clase = arg2;
      precio = Number(arg3);
    }
    const claseId = this._parseClaseIndex(clase);

    if (isNaN(precio) || precio <= 0) {
      throw new Error('El precio de referencia debe ser un valor numérico mayor a 0.');
    }

    const payload = {
      temporadaId: tempId,
      claseBulbo: claseId,
      precioReferencia: precio
    };

    const response = await apiClient.post('/precios-referencia', payload);
    const data = this._unwrap(response);
    const meta = CLASES_BULBO.find(c => c.id === claseId) || CLASES_BULBO[0];
    return this._normalizePrecioItem(data, meta);
  }

  /**
   * Guarda o actualiza en lote los precios de una temporada y opcionalmente su comisión de compra
   * POST /api/v1/precios-referencia/temporada
   */
  async guardarPreciosTemporada(temporadaId, precios, porcentajeComisionCompra = null) {
    const tempId = Number(temporadaId);
    if (!tempId) throw new Error('ID de temporada inválido.');
    if (!Array.isArray(precios) || precios.length === 0) {
      throw new Error('Debe proporcionar al menos un precio de clase.');
    }

    const itemsDto = precios.map(p => {
      const precio = Number(p.precioReferencia);
      if (isNaN(precio) || precio <= 0) {
        throw new Error(`El precio para la clase ${p.claseBulbo} debe ser mayor a 0.`);
      }
      return {
        claseBulbo: this._parseClaseIndex(p.claseBulbo),
        precioReferencia: precio
      };
    });

    const payload = {
      temporadaId: tempId,
      precios: itemsDto
    };

    if (porcentajeComisionCompra !== null && porcentajeComisionCompra !== undefined) {
      const comisionNum = Number(porcentajeComisionCompra);
      if (!isNaN(comisionNum) && comisionNum >= 0 && comisionNum <= 100) {
        payload.porcentajeComisionCompra = comisionNum;
      }
    }

    const response = await apiClient.post('/precios-referencia/temporada', payload);
    const data = this._unwrap(response);
    return Array.isArray(data) ? data : (data?.items || []);
  }
}

export const preciosReferenciaService = new PreciosReferenciaService();
