/**
 * SIGA - services/roles-service.js
 * Servicio para gestión CRUD de roles y catálogo de permisos (/api/v1/roles y /api/v1/permisos) (Feature 31)
 */

import { apiClient } from '../utils/api-client.js';
import { CONFIG } from '../config.js';

class RolesService {
  _unwrap(res) {
    if (res && typeof res === 'object' && 'data' in res && res.data !== null && res.data !== undefined) {
      return res.data;
    }
    return res;
  }

  async listar() {
    try {
      const response = await apiClient.get('/roles');
      const data = this._unwrap(response);
      return Array.isArray(data) ? data : [];
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        console.info('[RolesService] Fallback mock para listado de roles');
        return [
          {
            id: 1,
            nombre: 'ADMINISTRADOR',
            descripcion: 'Control total de la plataforma y administración del sistema',
            esSistema: true,
            totalUsuarios: 1,
            permisos: [
              'ROLES_VER', 'ROLES_GESTIONAR', 'PERMISOS_VER',
              'EMPLEADOS_VER', 'EMPLEADOS_GESTIONAR',
              'CLIENTES_VER', 'CLIENTES_GESTIONAR',
              'SUCURSALES_VER', 'SUCURSALES_GESTIONAR',
              'STOCK_VER', 'STOCK_AJUSTAR',
              'MOVCOM_VER', 'MOVCOM_CREAR', 'MOVCOM_APROBAR', 'MOVCOM_FACTURAR', 'MOVCOM_FINALIZAR', 'MOVCOM_RECHAZAR', 'MOVCOM_CORREGIR',
              'PRECIOS_REF_VER', 'PRECIOS_REF_GESTIONAR',
              'TEMPORADAS_VER', 'TEMPORADAS_GESTIONAR',
              'PLANTACIONES_VER', 'PLANTACIONES_GESTIONAR',
              'TRANSFERENCIAS_VER', 'TRANSFERENCIAS_GESTIONAR',
              'COSECHAS_VER', 'COSECHAS_GESTIONAR',
              'REPORTES_VER',
              'PERFIL_VER', 'PERFIL_MODIFICAR'
            ]
          },
          {
            id: 2,
            nombre: 'VENDEDOR',
            descripcion: 'Gestión comercial, cartera de clientes y consulta de stock',
            esSistema: true,
            totalUsuarios: 1,
            permisos: [
              'CLIENTES_VER', 'CLIENTES_GESTIONAR',
              'STOCK_VER',
              'MOVCOM_VER', 'MOVCOM_CREAR', 'MOVCOM_CORREGIR',
              'PRECIOS_REF_VER',
              'TEMPORADAS_VER', 'PLANTACIONES_VER', 'TRANSFERENCIAS_VER', 'COSECHAS_VER',
              'REPORTES_VER',
              'PERFIL_VER', 'PERFIL_MODIFICAR'
            ]
          },
          {
            id: 3,
            nombre: 'SECRETARIA',
            descripcion: 'Administración operativa, sucursales y operaciones comerciales',
            esSistema: true,
            totalUsuarios: 0,
            permisos: [
              'CLIENTES_VER', 'CLIENTES_GESTIONAR',
              'SUCURSALES_VER',
              'STOCK_VER', 'STOCK_AJUSTAR',
              'MOVCOM_VER', 'MOVCOM_CREAR', 'MOVCOM_APROBAR', 'MOVCOM_FINALIZAR', 'MOVCOM_RECHAZAR', 'MOVCOM_CORREGIR',
              'PRECIOS_REF_VER',
              'TEMPORADAS_VER', 'TEMPORADAS_GESTIONAR',
              'PLANTACIONES_VER', 'PLANTACIONES_GESTIONAR',
              'TRANSFERENCIAS_VER', 'TRANSFERENCIAS_GESTIONAR',
              'COSECHAS_VER', 'COSECHAS_GESTIONAR',
              'REPORTES_VER',
              'PERFIL_VER', 'PERFIL_MODIFICAR'
            ]
          },
          {
            id: 4,
            nombre: 'CONTABILIDAD',
            descripcion: 'Facturación comercial, precios de referencia y control de operaciones',
            esSistema: true,
            totalUsuarios: 0,
            permisos: [
              'CLIENTES_VER',
              'STOCK_VER',
              'MOVCOM_VER', 'MOVCOM_FACTURAR', 'MOVCOM_RECHAZAR',
              'PRECIOS_REF_VER', 'PRECIOS_REF_GESTIONAR',
              'TEMPORADAS_VER',
              'REPORTES_VER',
              'PERFIL_VER', 'PERFIL_MODIFICAR'
            ]
          }
        ];
      }
      throw err;
    }
  }

  async obtenerPorId(id) {
    const response = await apiClient.get(`/roles/${id}`);
    return this._unwrap(response);
  }

  async listarPermisos() {
    try {
      const response = await apiClient.get('/permisos');
      const data = this._unwrap(response);
      return Array.isArray(data) ? data : [];
    } catch (err) {
      if (err.isNetworkError && CONFIG.ENABLE_MOCK_FALLBACK) {
        console.info('[RolesService] Fallback mock para catálogo de permisos');
        return [
          { id: 1, codigo: 'ROLES_VER', nombre: 'Ver Roles', descripcion: 'Visualizar roles configurados', modulo: 'Seguridad' },
          { id: 2, codigo: 'ROLES_GESTIONAR', nombre: 'Gestionar Roles', descripcion: 'Crear, modificar y asignar permisos a roles', modulo: 'Seguridad' },
          { id: 3, codigo: 'PERMISOS_VER', nombre: 'Ver Permisos', descripcion: 'Consultar el catálogo maestro de permisos', modulo: 'Seguridad' },
          { id: 4, codigo: 'EMPLEADOS_VER', nombre: 'Ver Empleados', descripcion: 'Visualizar nómina de empleados internos', modulo: 'Personal' },
          { id: 5, codigo: 'EMPLEADOS_GESTIONAR', nombre: 'Gestionar Empleados', descripcion: 'Crear, editar y dar de baja personal', modulo: 'Personal' },
          { id: 6, codigo: 'CLIENTES_VER', nombre: 'Ver Clientes', descripcion: 'Consultar información de clientes', modulo: 'Clientes' },
          { id: 7, codigo: 'CLIENTES_GESTIONAR', nombre: 'Gestionar Clientes', descripcion: 'Crear, actualizar y dar de baja clientes', modulo: 'Clientes' },
          { id: 8, codigo: 'SUCURSALES_VER', nombre: 'Ver Sucursales', descripcion: 'Consultar sucursales de la empresa', modulo: 'Sucursales' },
          { id: 9, codigo: 'SUCURSALES_GESTIONAR', nombre: 'Gestionar Sucursales', descripcion: 'Crear y actualizar sucursales', modulo: 'Sucursales' },
          { id: 10, codigo: 'STOCK_VER', nombre: 'Ver Stock', descripcion: 'Consultar inventario de bulbos de empresa y clientes', modulo: 'Stock' },
          { id: 11, codigo: 'STOCK_AJUSTAR', nombre: 'Ajustar Stock', descripcion: 'Realizar ajustes de inventario de bulbos', modulo: 'Stock' },
          { id: 12, codigo: 'MOVCOM_VER', nombre: 'Ver Operaciones', descripcion: 'Visualizar movimientos comerciales', modulo: 'Comercial' },
          { id: 13, codigo: 'MOVCOM_CREAR', nombre: 'Crear Operación', descripcion: 'Registrar ventas, compras y traspasos comerciales', modulo: 'Comercial' },
          { id: 14, codigo: 'MOVCOM_APROBAR', nombre: 'Aprobar Operación', descripcion: 'Aprobar administrativamente operaciones comerciales', modulo: 'Comercial' },
          { id: 15, codigo: 'MOVCOM_FACTURAR', nombre: 'Facturar Operación', descripcion: 'Emitir comprobante y avanzar a facturado', modulo: 'Comercial' },
          { id: 16, codigo: 'MOVCOM_FINALIZAR', nombre: 'Finalizar Operación', descripcion: 'Finalizar movimiento e impactar stock físico', modulo: 'Comercial' },
          { id: 17, codigo: 'MOVCOM_RECHAZAR', nombre: 'Rechazar Operación', descripcion: 'Rechazar operación comercial especificando motivo', modulo: 'Comercial' },
          { id: 18, codigo: 'MOVCOM_CORREGIR', nombre: 'Corregir Operación', descripcion: 'Editar y reenviar operaciones en estado Rechazado', modulo: 'Comercial' },
          { id: 19, codigo: 'PRECIOS_REF_VER', nombre: 'Ver Precios Ref.', descripcion: 'Consultar tabla de precios de referencia', modulo: 'Comercial' },
          { id: 20, codigo: 'PRECIOS_REF_GESTIONAR', nombre: 'Gestionar Precios Ref.', descripcion: 'Actualizar precios de referencia por temporada', modulo: 'Comercial' },
          { id: 21, codigo: 'TEMPORADAS_VER', nombre: 'Ver Temporadas', descripcion: 'Consultar temporadas agrícolas', modulo: 'Producción' },
          { id: 22, codigo: 'TEMPORADAS_GESTIONAR', nombre: 'Gestionar Temporadas', descripcion: 'Crear y abrir/cerrar temporadas agrícolas', modulo: 'Producción' },
          { id: 23, codigo: 'PLANTACIONES_VER', nombre: 'Ver Plantaciones', descripcion: 'Consultar plantaciones, filas y lotes', modulo: 'Producción' },
          { id: 24, codigo: 'PLANTACIONES_GESTIONAR', nombre: 'Gestionar Plantaciones', descripcion: 'Registrar distribución y siembra de lotes', modulo: 'Producción' },
          { id: 25, codigo: 'TRANSFERENCIAS_VER', nombre: 'Ver Transferencias', descripcion: 'Consultar transferencias de lotes entre sucursales', modulo: 'Producción' },
          { id: 26, codigo: 'TRANSFERENCIAS_GESTIONAR', nombre: 'Gestionar Transferencias', descripcion: 'Registrar transferencias físicas de lotes', modulo: 'Producción' },
          { id: 27, codigo: 'COSECHAS_VER', nombre: 'Ver Cosechas', descripcion: 'Consultar pesajes y registros de cosecha', modulo: 'Producción' },
          { id: 28, codigo: 'COSECHAS_GESTIONAR', nombre: 'Gestionar Cosechas', descripcion: 'Registrar cosechas de bulbos y flores', modulo: 'Producción' },
          { id: 29, codigo: 'REPORTES_VER', nombre: 'Ver Reportes', descripcion: 'Consultar reportes ejecutivos e indicadores', modulo: 'Reportes' },
          { id: 30, codigo: 'PERFIL_VER', nombre: 'Ver Perfil', descripcion: 'Visualizar información del propio perfil de usuario', modulo: 'Mi Cuenta' },
          { id: 31, codigo: 'PERFIL_MODIFICAR', nombre: 'Modificar Perfil', descripcion: 'Actualizar datos de contacto personales', modulo: 'Mi Cuenta' }
        ];
      }
      throw err;
    }
  }

  async crear(dto) {
    const response = await apiClient.post('/roles', dto);
    return this._unwrap(response);
  }

  async actualizar(id, dto) {
    const response = await apiClient.put(`/roles/${id}`, dto);
    return this._unwrap(response);
  }

  async eliminar(id) {
    const response = await apiClient.delete(`/roles/${id}`);
    return this._unwrap(response);
  }
}

export const rolesService = new RolesService();
