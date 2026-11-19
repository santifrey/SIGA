/**
 * SIGA - views/movimientos-comerciales/movimientos-helpers.js
 * Funciones auxiliares compartidas para resolución de participantes, estados visuales
 * y evaluación unificada de acciones permitidas según estado, tipo y permisos RBAC.
 */

import { authService } from '../../services/auth-service.js';
import {
  ESTADOS_MOVIMIENTO,
  _parseTipoIndex,
  _parseEstadoIndex
} from '../../services/movimientos-comerciales-service.js';
import { getProhibitionIcon } from '../../utils/icons.js';

/**
 * Extrae el índice numérico (0..5) de la clase de bulbo de manera robusta.
 * Maneja números (0..5), strings con dígitos ('Clase0'..'Clase4') y 'MIX' (5).
 */
export function _parseClaseIndex(val) {
  if (val === undefined || val === null) return 0;
  if (typeof val === 'number') return isNaN(val) ? 0 : val;
  const s = String(val).trim().toUpperCase();
  if (s === 'MIX' || s === '5') return 5;
  const match = s.match(/\d+/);
  return match ? parseInt(match[0], 10) : 0;
}

/**
 * Resuelve el nombre legible de un participante (Empresa Central o Cliente).
 */
export function resolverNombreParticipante(participanteId, rawNombre, mov = null, clientes = []) {
  const idNum = Number(participanteId);
  if (idNum === 1) {
    if (rawNombre && !rawNombre.startsWith('Participante #') && !rawNombre.startsWith('Cliente #')) {
      return rawNombre;
    }
    return 'Empresa Central';
  }
  const cli = clientes.find(c => Number(c.id) === idNum || (rawNombre && (c.codigo === rawNombre || c.codigoCliente === rawNombre)));
  if (cli) {
    const fullName = cli.nombreCompleto || `${cli.nombre || ''} ${cli.apellido || ''}`.trim();
    const cod = cli.codigo || cli.codigoCliente;
    if (fullName) {
      return cod ? `${fullName} (${cod})` : fullName;
    }
  }
  if (mov && Number(mov.clienteId) === idNum && mov.nombreCliente && !mov.nombreCliente.startsWith('CLI-') && !mov.nombreCliente.startsWith('Participante #')) {
    return mov.codigoCliente ? `${mov.nombreCliente} (${mov.codigoCliente})` : mov.nombreCliente;
  }
  if (rawNombre && !rawNombre.startsWith('Participante #') && !rawNombre.startsWith('CLI-')) {
    return rawNombre;
  }
  return rawNombre || `Cliente #${participanteId}`;
}

/**
 * Determina si la factura asociada al movimiento fue anulada.
 */
export function esFacturaAnulada(mov) {
  if (!mov || !mov.factura) return false;
  const est = mov.factura.estado;
  return (
    est === 2 ||
    est === 'Anulada' ||
    mov.factura.nombreEstado === 'Anulada' ||
    (typeof est === 'string' && est.toLowerCase().includes('anulad'))
  );
}

/**
 * Resuelve la etiqueta amigable y clase de badge del estado del circuito (5 fases).
 */
export function resolverEstadoVisual(mov, forDetail = false) {
  const tipoNum = _parseTipoIndex(mov.tipo ?? mov.nombreTipo);
  const estadoNum = _parseEstadoIndex(mov.estado ?? mov.nombreEstado);
  const estadoMeta = ESTADOS_MOVIMIENTO[estadoNum] || ESTADOS_MOVIMIENTO[mov.estado] || {
    label: `Estado ${mov.estado}`,
    badgeClass: 'badge-secondary'
  };

  const isFacturaAnulada = esFacturaAnulada(mov);
  let label = estadoMeta.label;
  let badgeClass = estadoMeta.badgeClass;

  if (estadoNum === 0) {
    const sinContrato = !mov.contrato || mov.contrato.isDeleted;
    if (tipoNum === 1 && (forDetail ? !mov.contrato : (mov.requiereContrato && sinContrato))) {
      label = 'Pendiente Contrato';
      badgeClass = 'badge-warning';
    } else {
      label = 'Pendiente Aprobación Contrato';
      badgeClass = 'badge-info';
    }
  } else if (estadoNum === 1) {
    label = (!forDetail && tipoNum === 3) ? 'Pendiente Finalización' : 'Pendiente Factura';
    badgeClass = 'badge-primary';
  } else if (estadoNum === 2) {
    label = 'Pendiente Aprobación Factura';
    badgeClass = 'badge-purple';
  } else if (estadoNum === 3) {
    label = 'Completado';
    badgeClass = 'badge-success';
  } else if (estadoNum === 4) {
    label = isFacturaAnulada ? 'Rechazado (Factura Anulada)' : 'Rechazado';
    badgeClass = 'badge-danger';
  }

  return { tipoNum, estadoNum, isFacturaAnulada, label, badgeClass };
}

/**
 * Renderiza los botones de acción para una fila de la tabla principal.
 */
export function renderRowActionButtons(m) {
  const btns = [];
  const st = _parseEstadoIndex(m.estado ?? m.nombreEstado);
  const tipo = _parseTipoIndex(m.tipo ?? m.nombreTipo);
  const currentUser = authService.getUsuarioActual();
  const isCreator = currentUser && Number(currentUser.id) === Number(m.usuarioRegistroId);

  // 1. Estado 0: Pendiente Contrato / Pendiente Aprobación Contrato
  if (st === 0) {
    if (authService.tienePermiso('MOVCOM_APROBAR')) {
      const reqContrato = tipo === 1 && m.requiereContrato;
      const tieneContrato = Boolean(m.contrato && !m.contrato.isDeleted);

      if (reqContrato && !tieneContrato) {
        btns.push(`
          <button type="button" class="btn btn-warning btn-sm btn-action-contrato" data-id="${m.id}" title="Generar Contrato Comercial PDF">
            <span>Generar Contrato</span>
          </button>
        `);
      } else {
        if (tipo === 1 && tieneContrato) {
          btns.push(`
            <button type="button" class="btn btn-outline btn-sm btn-action-ver-contrato" data-id="${m.id}" title="Descargar / Ver Contrato PDF">
              <span>Ver Contrato</span>
            </button>
          `);
        }
        btns.push(`
          <button type="button" class="btn btn-primary btn-sm btn-action-aprobar" data-id="${m.id}" title="Aprobar contrato / operación y avanzar a Pendiente Factura">
            <span>Aprobar Contrato</span>
          </button>
        `);
      }
    }
    if (authService.tienePermiso('MOVCOM_RECHAZAR')) {
      btns.push(`
        <button type="button" class="btn btn-outline btn-sm btn-action-rechazar" data-id="${m.id}" title="Rechazar u Observar Operación">
          ${getProhibitionIcon({ size: 'sm', title: 'Rechazar' })}
        </button>
      `);
    }
  }

  // 2. Estado 1: Pendiente Factura (o Pendiente Finalización en Traspaso)
  if (st === 1) {
    if (tipo === 1 && m.contrato) {
      btns.push(`
        <button type="button" class="btn btn-outline btn-sm btn-action-ver-contrato" data-id="${m.id}" title="Descargar / Ver Contrato PDF">
          <span>Ver Contrato</span>
        </button>
      `);
    }
    if (tipo !== 3) {
      if (authService.tienePermiso('MOVCOM_FACTURAR')) {
        btns.push(`
          <button type="button" class="btn btn-primary btn-sm btn-action-facturar" data-id="${m.id}" title="Emitir Factura Comercial">
            <span>Facturar</span>
          </button>
        `);
      }
    } else if (authService.tienePermiso('MOVCOM_FINALIZAR')) {
      btns.push(`
        <button type="button" class="btn btn-success btn-sm btn-action-finalizar" data-id="${m.id}" title="Finalizar traspaso e impactar stock">
          <span>Finalizar</span>
        </button>
      `);
    }
    if (authService.tienePermiso('MOVCOM_RECHAZAR')) {
      btns.push(`
        <button type="button" class="btn btn-outline btn-sm btn-action-rechazar" data-id="${m.id}" title="Rechazar u Observar Operación">
          ${getProhibitionIcon({ size: 'sm', title: 'Rechazar' })}
        </button>
      `);
    }
  }

  // 3. Estado 2: Pendiente Aprobación Factura
  if (st === 2 && tipo !== 3) {
    if (tipo === 1 && m.contrato) {
      btns.push(`
        <button type="button" class="btn btn-outline btn-sm btn-action-ver-contrato" data-id="${m.id}" title="Descargar / Ver Contrato PDF">
          <span>Ver Contrato</span>
        </button>
      `);
    }
    if (authService.tienePermiso('MOVCOM_FINALIZAR')) {
      btns.push(`
        <button type="button" class="btn btn-success btn-sm btn-action-finalizar" data-id="${m.id}" title="Aprobar Factura y Finalizar Operación (Impacta stock físico)">
          <span>Aprobar Factura</span>
        </button>
      `);
    }
    if (authService.tienePermiso('MOVCOM_RECHAZAR')) {
      btns.push(`
        <button type="button" class="btn btn-outline-danger btn-sm btn-action-rechazar" data-id="${m.id}" title="Rechazar Factura (Anula la factura actual y permite generar una nueva)">
          <span>Rechazar Factura</span>
        </button>
      `);
    }
  }

  // 4. Estado 4: Rechazado -> Nuevo Contrato, Nueva Factura o Corregir Operación
  if (st === 4) {
    if (tipo === 1 && authService.tienePermiso('MOVCOM_APROBAR')) {
      btns.push(`
        <button type="button" class="btn btn-warning btn-sm btn-action-contrato" data-id="${m.id}" title="Regenerar o editar contrato oficial tras rechazo">
          <span>${m.contrato ? 'Editar Contrato' : 'Generar Contrato'}</span>
        </button>
      `);
    }
    if (authService.tienePermiso('MOVCOM_FACTURAR') && tipo !== 3) {
      btns.push(`
        <button type="button" class="btn btn-primary btn-sm btn-action-facturar" data-id="${m.id}" title="Emitir nueva factura comercial corregida">
          <span>Nueva Factura</span>
        </button>
      `);
    }
    if (isCreator || authService.tienePermiso('MOVCOM_CORREGIR')) {
      btns.push(`
        <button type="button" class="btn btn-secondary btn-sm btn-action-corregir" data-id="${m.id}" title="Corregir datos de la operación y reenviar a revisión">
          <span>Corregir Operación</span>
        </button>
      `);
    }
  }

  // 5. Finalizado (3)
  if (st === 3 && tipo === 1 && m.contrato) {
    btns.push(`
      <button type="button" class="btn btn-outline btn-sm btn-action-ver-contrato" data-id="${m.id}" title="Descargar / Ver Contrato PDF">
        <span>Ver Contrato</span>
      </button>
    `);
  }

  return btns.join('');
}

/**
 * Renderiza los botones de acción para el pie del modal de detalle.
 */
export function renderDetailActionButtons(mov) {
  const btns = [];
  const st = _parseEstadoIndex(mov.estado ?? mov.nombreEstado);
  const tipo = _parseTipoIndex(mov.tipo ?? mov.nombreTipo);
  const currentUser = authService.getUsuarioActual();
  const isCreator = currentUser && Number(currentUser.id) === Number(mov.usuarioRegistroId);

  if (st === 0) {
    if (authService.tienePermiso('MOVCOM_APROBAR')) {
      if (tipo === 1 && !mov.contrato) {
        btns.push(`
          <button type="button" class="btn btn-accent btn-sm btn-detalle-action" id="btn-detalle-contrato" title="Generar Contrato de Compraventa basado en Plantilla">
            <span>Generar Contrato</span>
          </button>
        `);
      }
      if (tipo === 1 && mov.contrato) {
        btns.push(`
          <button type="button" class="btn btn-outline btn-sm btn-detalle-action" id="btn-detalle-ver-contrato" title="Descargar / Ver Contrato PDF">
            <span>Ver Contrato PDF</span>
          </button>
        `);
      }
      btns.push(`
        <button type="button" class="btn btn-primary btn-sm btn-detalle-action" id="btn-detalle-aprobar" title="Aprobar operación">
          <span>Aprobar</span>
        </button>
      `);
    }
    if (authService.tienePermiso('MOVCOM_RECHAZAR')) {
      btns.push(`
        <button type="button" class="btn btn-outline btn-sm btn-detalle-action" id="btn-detalle-rechazar" title="Rechazar u Observar">
          ${getProhibitionIcon({ size: 'sm', title: 'Rechazar' })}
          <span style="color: var(--danger); font-weight: 600;">Rechazar</span>
        </button>
      `);
    }
  }

  if (st === 1) {
    if (tipo !== 3) {
      if (authService.tienePermiso('MOVCOM_FACTURAR')) {
        btns.push(`
          <button type="button" class="btn btn-primary btn-sm btn-detalle-action" id="btn-detalle-facturar" title="Emitir Factura">
            <span>Facturar</span>
          </button>
        `);
      }
    } else if (authService.tienePermiso('MOVCOM_FINALIZAR')) {
      btns.push(`
        <button type="button" class="btn btn-success btn-sm btn-detalle-action" id="btn-detalle-finalizar" title="Finalizar traspaso e impactar stock">
          <span>Finalizar</span>
        </button>
      `);
    }
    if (authService.tienePermiso('MOVCOM_RECHAZAR')) {
      btns.push(`
        <button type="button" class="btn btn-outline btn-sm btn-detalle-action" id="btn-detalle-rechazar" title="Rechazar u Observar">
          ${getProhibitionIcon({ size: 'sm', title: 'Rechazar' })}
          <span style="color: var(--danger); font-weight: 600;">Rechazar</span>
        </button>
      `);
    }
  }

  if (st === 2 && tipo !== 3) {
    if (authService.tienePermiso('MOVCOM_FINALIZAR')) {
      btns.push(`
        <button type="button" class="btn btn-success btn-sm btn-detalle-action" id="btn-detalle-finalizar" title="Aprobar factura y finalizar operación (impacta stock físico)">
          <span>Aprobar Factura</span>
        </button>
      `);
    }
    if (authService.tienePermiso('MOVCOM_RECHAZAR')) {
      btns.push(`
        <button type="button" class="btn btn-outline btn-sm btn-detalle-action" id="btn-detalle-rechazar" title="Rechazar factura (Anula la factura actual y permite generar una nueva)">
          ${getProhibitionIcon({ size: 'sm', title: 'Rechazar' })}
          <span style="color: var(--danger); font-weight: 600;">Rechazar Factura</span>
        </button>
      `);
    }
  }

  if (st === 4) {
    if (tipo === 1 && authService.tienePermiso('MOVCOM_APROBAR')) {
      btns.push(`
        <button type="button" class="btn btn-warning btn-sm btn-detalle-action" id="btn-detalle-contrato" title="Regenerar o editar contrato oficial tras rechazo">
          <span>${mov.contrato ? 'Editar Contrato' : 'Generar Contrato'}</span>
        </button>
      `);
    }
    if (authService.tienePermiso('MOVCOM_FACTURAR') && tipo !== 3) {
      btns.push(`
        <button type="button" class="btn btn-primary btn-sm btn-detalle-action" id="btn-detalle-facturar" title="Emitir nueva factura comercial corregida">
          <span>Nueva Factura</span>
        </button>
      `);
    }
    if (isCreator || authService.tienePermiso('MOVCOM_CORREGIR')) {
      btns.push(`
        <button type="button" class="btn btn-secondary btn-sm btn-detalle-action" id="btn-detalle-corregir" title="Corregir datos de la operación y reenviar a revisión">
          <span>Corregir Operación</span>
        </button>
      `);
    }
  }

  return btns;
}
