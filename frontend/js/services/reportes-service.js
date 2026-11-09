/**
 * SIGA - services/reportes-service.js
 * Servicio de Reportes Ejecutivos, Estado Integral de Cliente y Rendimiento Productivo
 */

import { apiClient } from '../utils/api-client.js';
import { storage } from '../utils/storage.js';
import { CONFIG } from '../config.js';

class ReportesService {
  _unwrap(res) {
    if (res && typeof res === 'object' && 'data' in res && res.data !== null && res.data !== undefined) {
      return res.data;
    }
    return res;
  }

  /**
   * Obtiene la ficha / estado integral 360° de un cliente
   * GET /api/v1/reportes/cliente/{clienteId}/integral
   */
  async obtenerEstadoIntegralCliente(clienteId) {
    try {
      const response = await apiClient.get(`/reportes/cliente/${clienteId}/integral`);
      return this._unwrap(response);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        console.info('[ReportesService] Fallback mock para estado integral de cliente.');
        return this._calcularEstadoIntegralClienteMock(Number(clienteId));
      }
      throw err;
    }
  }

  /**
   * Obtiene el reporte de rendimiento y trazabilidad productiva de una temporada
   * GET /api/v1/reportes/produccion/temporada/{temporadaId}
   */
  async obtenerRendimientoTemporada(temporadaId) {
    try {
      const response = await apiClient.get(`/reportes/produccion/temporada/${temporadaId}`);
      return this._unwrap(response);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        console.info('[ReportesService] Fallback mock para rendimiento de temporada.');
        return this._calcularRendimientoTemporadaMock(Number(temporadaId));
      }
      throw err;
    }
  }

  /**
   * Obtiene el resumen ejecutivo general de la empresa
   * GET /api/v1/reportes/resumen-ejecutivo
   */
  async obtenerResumenEjecutivo() {
    try {
      const response = await apiClient.get('/reportes/resumen-ejecutivo');
      return this._unwrap(response);
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        console.info('[ReportesService] Fallback mock para resumen ejecutivo.');
        return this._calcularResumenEjecutivoMock();
      }
      throw err;
    }
  }

  /* =========================================================================
     CÁLCULOS EN VIVO PARA MODO MOCK / FALLBACK LOCAL
     ========================================================================= */

  _calcularResumenEjecutivoMock() {
    const clientes = storage.get(CONFIG.STORAGE_KEYS.MOCK_CLIENTES, []);
    const sucursales = storage.get(CONFIG.STORAGE_KEYS.MOCK_SUCURSALES, []);
    const temporadas = storage.get(CONFIG.STORAGE_KEYS.MOCK_TEMPORADAS, []);
    const lotes = storage.get(CONFIG.STORAGE_KEYS.MOCK_LOTES, []);
    const cosechas = storage.get(CONFIG.STORAGE_KEYS.MOCK_COSECHAS, []);
    const ventas = storage.get(CONFIG.STORAGE_KEYS.MOCK_VENTAS, []);

    const activa = temporadas.find(t => {
      const st = String(t.nombreEstado || t.estado || '').toLowerCase();
      return !st.includes('cerrada');
    }) || temporadas[0];

    let totalLotesActiva = 0;
    let totalPlantadoActiva = 0;
    let totalCosechadoActiva = 0;

    if (activa) {
      const lotesActiva = lotes.filter(l => String(l.temporadaId) === String(activa.id));
      totalLotesActiva = lotesActiva.length;
      totalPlantadoActiva = lotesActiva.reduce((sum, l) => sum + (Number(l.totalBulbos || l.cantidadPlantadaTotal) || 0), 0);

      const cosechasActiva = cosechas.filter(c => String(c.temporadaId) === String(activa.id));
      totalCosechadoActiva = cosechasActiva.reduce((sum, c) => sum + (Number(c.totalCosechado) || 0), 0);
    }

    const ventasCompletadas = ventas.filter(v => Number(v.estado) === 4);
    const montoCompletadas = ventasCompletadas.reduce((sum, v) => sum + (Number(v.montoTotal) || 0), 0);

    const ventasEnCircuito = ventas.filter(v => Number(v.estado) >= 0 && Number(v.estado) < 4);
    const montoEnCircuito = ventasEnCircuito.reduce((sum, v) => sum + (Number(v.montoTotal) || 0), 0);

    return {
      totalClientesActivos: clientes.filter(c => c.activo !== false).length,
      totalSucursalesActivas: sucursales.filter(s => s.activa !== false).length,
      temporadaActivaId: activa ? activa.id : null,
      temporadaActivaNombre: activa ? activa.nombre : 'Sin temporada activa',
      temporadaActivaEstado: activa ? (activa.nombreEstado || activa.estado) : 'N/A',
      totalLotesTemporadaActiva: totalLotesActiva,
      totalBulbosPlantadosTemporadaActiva: totalPlantadoActiva,
      totalBulbosCosechadosTemporadaActiva: totalCosechadoActiva,
      totalVentasRegistradas: ventas.length,
      totalVentasCompletadas: ventasCompletadas.length,
      montoTotalVentasCompletadas: montoCompletadas,
      montoTotalVentasEnCircuito: montoEnCircuito
    };
  }

  _calcularEstadoIntegralClienteMock(clienteId) {
    const clientes = storage.get(CONFIG.STORAGE_KEYS.MOCK_CLIENTES, []);
    const cli = clientes.find(c => Number(c.id) === clienteId) || {
      id: clienteId,
      nombre: 'Cliente',
      apellido: `#${clienteId}`,
      codigoCliente: `CLI-${clienteId}`,
      documento: '20-12345678-9',
      email: 'cliente@siga.com',
      activo: true
    };

    const temporadas = storage.get(CONFIG.STORAGE_KEYS.MOCK_TEMPORADAS, []);
    const lotes = storage.get(CONFIG.STORAGE_KEYS.MOCK_LOTES, []);
    const cosechas = storage.get(CONFIG.STORAGE_KEYS.MOCK_COSECHAS, []);
    const transferencias = storage.get(CONFIG.STORAGE_KEYS.MOCK_TRANSFERENCIAS, []);
    const sucursales = storage.get(CONFIG.STORAGE_KEYS.MOCK_SUCURSALES, []);
    const ventas = storage.get(CONFIG.STORAGE_KEYS.MOCK_VENTAS, []);

    // 1. Stock por clase
    const stockMock = storage.get(CONFIG.STORAGE_KEYS.MOCK_STOCK, {});
    const clienteStock = stockMock[clienteId] || {
      0: 800,
      1: 1800,
      2: 2200,
      3: 1200,
      4: 400
    };

    const stockPorClase = [0, 1, 2, 3, 4].map(c => ({
      claseBulbo: c,
      nombreClase: `Clase${c}`,
      cantidadDisponible: Number(clienteStock[c]) || 0
    }));
    const totalBulbosEnStock = stockPorClase.reduce((sum, s) => sum + s.cantidadDisponible, 0);

    // 2. Derechos vigentes sobre lotes plantados
    const derechosLotes = [];
    lotes.forEach(l => {
      // Determinar titular vigente considerando transferencias
      const transfs = transferencias.filter(t => Number(t.loteId) === Number(l.id));
      let titularVigenteId = Number(l.clienteId);
      let adquiridoPorTransferencia = false;

      if (transfs.length > 0) {
        const ultima = transfs[transfs.length - 1];
        titularVigenteId = Number(ultima.nuevoTitularId);
        if (titularVigenteId === clienteId && Number(l.clienteId) !== clienteId) {
          adquiridoPorTransferencia = true;
        }
      }

      if (titularVigenteId === clienteId) {
        const temp = temporadas.find(t => Number(t.id) === Number(l.temporadaId));
        const suc = sucursales.find(s => Number(s.id) === Number(l.sucursalId));
        derechosLotes.push({
          loteId: Number(l.id),
          temporadaId: Number(l.temporadaId),
          nombreTemporada: temp ? temp.nombre : `Temporada #${l.temporadaId}`,
          sucursalId: Number(l.sucursalId),
          nombreSucursal: suc ? suc.nombre : `Sucursal #${l.sucursalId}`,
          identificacionFila: l.filaIdentificacion || l.identificacionFila || 'Fila',
          orden: 1,
          estadoLote: l.estado || 1,
          nombreEstadoLote: l.nombreEstado || 'Plantado',
          cantidadPlantadaTotal: Number(l.totalBulbos || l.cantidadPlantadaTotal) || 0,
          adquiridoPorTransferencia,
          titularOriginalId: Number(l.clienteId),
          nombreTitularOriginal: `Titular #${l.clienteId}`
        });
      }
    });

    // 3. Historial productivo por temporada
    const historialProductivo = temporadas.map(t => {
      // Lotes donde el cliente era titular
      const lotesCliente = derechosLotes.filter(d => Number(d.temporadaId) === Number(t.id));
      const bulbosPlantados = lotesCliente.reduce((sum, d) => sum + d.cantidadPlantadaTotal, 0);

      // Cosechas de la temporada para los lotes de este cliente
      const cosechasTemp = cosechas.filter(c => Number(c.temporadaId) === Number(t.id));
      let bulbosCosechados = 0;
      cosechasTemp.forEach(c => {
        (c.detalles || []).forEach(det => {
          if (Number(det.titularVigenteId) === clienteId) {
            bulbosCosechados += Number(det.cantidad) || 0;
          }
        });
      });

      const dif = bulbosCosechados - bulbosPlantados;
      const pct = bulbosPlantados > 0 ? Math.round((bulbosCosechados / bulbosPlantados) * 10000) / 100 : 0;

      return {
        temporadaId: Number(t.id),
        nombreTemporada: t.nombre,
        estadoTemporada: t.estado,
        nombreEstadoTemporada: t.nombreEstado || t.estado,
        cantidadLotesPlantados: lotesCliente.length,
        bulbosPlantados,
        cantidadLotesCosechados: bulbosCosechados > 0 ? lotesCliente.length : 0,
        bulbosCosechados,
        diferenciaNeta: dif,
        porcentajeRendimiento: pct
      };
    });

    const ventasCliente = ventas.filter(v => Number(v.clienteId) === clienteId);

    return {
      clienteId: cli.id,
      codigoCliente: cli.codigoCliente || `CLI-${cli.id}`,
      nombreCompleto: cli.persona ? `${cli.persona.nombre} ${cli.persona.apellido}` : (cli.razonSocial || `${cli.nombre || ''} ${cli.apellido || ''}`.trim() || 'Cliente'),
      documento: cli.documento || '20-00000000-0',
      email: cli.email || '',
      estadoCliente: cli.activo ? 'Activo' : 'Inactivo',
      nombreEstadoCliente: cli.activo ? 'Activo' : 'Inactivo',
      totalBulbosEnStock,
      stockDisponiblePorClase: stockPorClase,
      totalLotesVigentes: derechosLotes.length,
      totalBulbosPlantadosEnLotesVigentes: derechosLotes.reduce((sum, d) => sum + d.cantidadPlantadaTotal, 0),
      derechosSobreLotesPlantados: derechosLotes,
      historialProductivoPorTemporada: historialProductivo,
      historialComprasVentas: ventasCliente,
      historialTransferencias: transferencias.filter(tr => Number(tr.nuevoTitularId) === clienteId || Number(tr.titularAnteriorId) === clienteId)
    };
  }

  _calcularRendimientoTemporadaMock(temporadaId) {
    const temporadas = storage.get(CONFIG.STORAGE_KEYS.MOCK_TEMPORADAS, []);
    const temp = temporadas.find(t => Number(t.id) === temporadaId) || {
      id: temporadaId,
      nombre: `Temporada #${temporadaId}`,
      estado: 'Cosecha',
      fechaInicio: '2025-03-01T08:00:00Z',
      fechaFin: null
    };

    const lotes = storage.get(CONFIG.STORAGE_KEYS.MOCK_LOTES, []).filter(l => Number(l.temporadaId) === temporadaId);
    const cosechas = storage.get(CONFIG.STORAGE_KEYS.MOCK_COSECHAS, []).filter(c => Number(c.temporadaId) === temporadaId);
    const sucursales = storage.get(CONFIG.STORAGE_KEYS.MOCK_SUCURSALES, []);
    const transferencias = storage.get(CONFIG.STORAGE_KEYS.MOCK_TRANSFERENCIAS, []);

    // 1. Comparativa global por clase (0..4)
    const comparativaPorClase = [0, 1, 2, 3, 4].map(c => {
      let plantado = 0;
      let cosechado = 0;

      lotes.forEach(l => {
        const item = (l.cantidades || []).find(x => Number(x.claseBulbo) === c);
        if (item) plantado += Number(item.cantidad) || 0;
      });

      cosechas.forEach(cos => {
        (cos.detalles || []).forEach(d => {
          if (Number(d.claseBulbo) === c) cosechado += Number(d.cantidad) || 0;
        });
      });

      const dif = cosechado - plantado;
      const pct = plantado > 0 ? Math.round((cosechado / plantado) * 10000) / 100 : 0;

      return {
        claseBulbo: c,
        nombreClase: `Clase${c}`,
        cantidadPlantada: plantado,
        cantidadCosechada: cosechado,
        diferenciaNeta: dif,
        porcentajeRendimiento: pct
      };
    });

    const totalPlantadoTemporada = comparativaPorClase.reduce((s, c) => s + c.cantidadPlantada, 0);
    const totalCosechadoTemporada = comparativaPorClase.reduce((s, c) => s + c.cantidadCosechada, 0);
    const difGlobal = totalCosechadoTemporada - totalPlantadoTemporada;
    const pctGlobal = totalPlantadoTemporada > 0 ? Math.round((totalCosechadoTemporada / totalPlantadoTemporada) * 10000) / 100 : 0;

    // 2. Desglose por sucursal
    const desglosePorSucursal = sucursales.map(s => {
      const lotesSuc = lotes.filter(l => Number(l.sucursalId) === Number(s.id));
      const cosechaSuc = cosechas.find(c => Number(c.sucursalId) === Number(s.id));

      const plantado = lotesSuc.reduce((sum, l) => sum + (Number(l.totalBulbos || l.cantidadPlantadaTotal) || 0), 0);
      const cosechado = cosechaSuc ? Number(cosechaSuc.totalCosechado) || 0 : 0;
      const dif = cosechado - plantado;
      const pct = plantado > 0 ? Math.round((cosechado / plantado) * 10000) / 100 : 0;

      return {
        sucursalId: s.id,
        nombreSucursal: s.nombre,
        estadoCosecha: cosechaSuc ? (cosechaSuc.nombreEstado || 'EnProceso') : 'Sin Iniciar',
        cantidadLotes: lotesSuc.length,
        totalPlantado: plantado,
        totalCosechado: cosechado,
        diferenciaNeta: dif,
        porcentajeRendimiento: pct
      };
    }).filter(s => s.cantidadLotes > 0 || s.totalCosechado > 0);

    // 3. Trazabilidad por lote
    const trazabilidadPorLote = lotes.map(l => {
      const suc = sucursales.find(s => Number(s.id) === Number(l.sucursalId));
      const transfs = transferencias.filter(t => Number(t.loteId) === Number(l.id));
      const titularOriginalId = Number(l.clienteId);
      let titularVigenteId = titularOriginalId;
      let nombreTitularVigente = l.nombreCliente || `Cliente #${titularOriginalId}`;

      if (transfs.length > 0) {
        const u = transfs[transfs.length - 1];
        titularVigenteId = Number(u.nuevoTitularId);
        nombreTitularVigente = u.nombreNuevoTitular || `Cliente #${titularVigenteId}`;
      }

      let cosechadoLote = 0;
      cosechas.forEach(c => {
        (c.detalles || []).forEach(d => {
          if (Number(d.loteId) === Number(l.id)) {
            cosechadoLote += Number(d.cantidad) || 0;
          }
        });
      });

      const plantadoLote = Number(l.totalBulbos || l.cantidadPlantadaTotal) || 0;

      return {
        loteId: Number(l.id),
        sucursalId: Number(l.sucursalId),
        nombreSucursal: suc ? suc.nombre : `Sucursal #${l.sucursalId}`,
        identificacionFila: l.filaIdentificacion || l.identificacionFila || 'Fila',
        orden: 1,
        titularOriginalId,
        nombreTitularOriginal: l.nombreCliente || `Cliente #${titularOriginalId}`,
        titularVigenteId,
        nombreTitularVigente,
        cantidadPlantada: plantadoLote,
        cantidadCosechada: cosechadoLote,
        diferenciaNeta: cosechadoLote - plantadoLote,
        estadoLote: l.estado || 1,
        nombreEstadoLote: l.nombreEstado || 'Plantado'
      };
    });

    return {
      temporadaId: temp.id,
      nombreTemporada: temp.nombre,
      estadoTemporada: temp.estado,
      nombreEstadoTemporada: temp.nombreEstado || temp.estado,
      fechaInicio: temp.fechaInicio,
      fechaFin: temp.fechaFin,
      totalPlantadoTemporada,
      totalCosechadoTemporada,
      diferenciaNetaTemporada: difGlobal,
      porcentajeRendimientoGlobal: pctGlobal,
      comparativaPorClase,
      desglosePorSucursal,
      trazabilidadPorLote
    };
  }
}

export const reportesService = new ReportesService();
