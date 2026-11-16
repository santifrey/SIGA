/**
 * SIGA - utils/icons.js
 * Catálogo y utilidades de renderizado de iconos SVG estandarizados (Feature 28)
 */

/**
 * Genera el símbolo SVG estandarizado de prohibición (círculo con barra diagonal \)
 * para acciones de "dar de baja", deshabilitación o eliminación lógica.
 * 
 * Geometría exacta:
 * - ViewBox: 0 0 24 24
 * - Círculo: cx=12, cy=12, r=10 (diámetro 20)
 * - Barra diagonal: x1=4.93, y1=4.93 a x2=19.07, y2=19.07 (intersección exacta a 45° con el perímetro)
 * - Color: var(--danger)
 * - Accesibilidad: role="img", aria-label, title
 *
 * @param {Object} [options]
 * @param {'sm'|'md'|'lg'} [options.size='sm'] - Tamaño: sm (16px), md (20px), lg (28px)
 * @param {string} [options.title='Dar de baja'] - Texto descriptivo accesible y tooltip
 * @param {string} [options.className=''] - Clases CSS adicionales
 * @returns {string} Markup SVG inline listo para interpolar en template literals
 */
export function getProhibitionIcon({ size = 'sm', title = 'Dar de baja', className = '' } = {}) {
  const sizeClass = size === 'lg' ? 'icon-svg lg' : (size === 'md' ? 'icon-svg' : 'icon-svg sm');
  const fullClass = [sizeClass, className].filter(Boolean).join(' ');

  return `<svg class="${fullClass}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="color: var(--danger);" role="img" aria-label="${title}" title="${title}"><circle cx="12" cy="12" r="10"></circle><line x1="4.93" y1="4.93" x2="19.07" y2="19.07"></line></svg>`;
}
