/**
 * SIGA - components/pagination.js
 * Componente reutilizable de controles de paginación e información de rango.
 * Compartido entre ClientesView y EmpleadosView.
 */

/**
 * Actualiza el texto informativo de paginación (ej. "Mostrando 1 a 10 de 45 clientes")
 * y renderiza los botones de navegación de páginas.
 *
 * @param {Object} options
 * @param {HTMLElement|null} options.infoEl - Elemento donde mostrar el texto "Mostrando X a Y de Z"
 * @param {HTMLElement|null} options.controlsEl - Contenedor donde renderizar los botones de página
 * @param {number} options.currentPage - Página actual (1-based)
 * @param {number} options.pageSize - Cantidad de ítems por página
 * @param {number} options.totalItems - Total de registros
 * @param {number} [options.totalPages] - Total de páginas (calculado automáticamente si se omite)
 * @param {string} [options.entityLabel='registros'] - Etiqueta plural (ej. 'clientes', 'empleados')
 * @param {(newPage: number) => void} options.onPageChange - Callback al cambiar de página
 */
export function renderPagination({
  infoEl,
  controlsEl,
  currentPage,
  pageSize,
  totalItems,
  totalPages,
  entityLabel = 'registros',
  onPageChange
}) {
  const computedTotalPages = totalPages || Math.ceil(totalItems / pageSize) || 1;
  const startItem = totalItems === 0 ? 0 : (currentPage - 1) * pageSize + 1;
  const endItem = Math.min(currentPage * pageSize, totalItems);

  if (infoEl) {
    infoEl.textContent = `Mostrando ${startItem} a ${endItem} de ${totalItems} ${entityLabel}`;
  }

  if (!controlsEl) return;

  if (computedTotalPages <= 1) {
    controlsEl.innerHTML = '';
    return;
  }

  let html = `
    <button type="button" class="btn-page" data-action="prev" ${currentPage === 1 ? 'disabled' : ''}>
      ‹
    </button>
  `;

  for (let p = 1; p <= computedTotalPages; p++) {
    if (p === 1 || p === computedTotalPages || (p >= currentPage - 1 && p <= currentPage + 1)) {
      html += `
        <button type="button" class="btn-page ${p === currentPage ? 'active' : ''}" data-page="${p}">
          ${p}
        </button>
      `;
    } else if (p === currentPage - 2 || p === currentPage + 2) {
      html += `<span style="padding: 0 4px; color: var(--text-light);">...</span>`;
    }
  }

  html += `
    <button type="button" class="btn-page" data-action="next" ${currentPage === computedTotalPages ? 'disabled' : ''}>
      ›
    </button>
  `;

  controlsEl.innerHTML = html;

  controlsEl.querySelector('[data-action="prev"]')?.addEventListener('click', () => {
    if (currentPage > 1) onPageChange(currentPage - 1);
  });

  controlsEl.querySelector('[data-action="next"]')?.addEventListener('click', () => {
    if (currentPage < computedTotalPages) onPageChange(currentPage + 1);
  });

  controlsEl.querySelectorAll('.btn-page[data-page]').forEach(btn => {
    btn.addEventListener('click', () => {
      const targetPage = Number(btn.getAttribute('data-page'));
      if (targetPage && targetPage !== currentPage) {
        onPageChange(targetPage);
      }
    });
  });
}
