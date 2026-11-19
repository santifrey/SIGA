/**
 * SIGA - utils/template-loader.js
 * Cargador asíncrono de plantillas HTML estáticas con caché en memoria.
 */

const templateCache = new Map();

/**
 * Carga un archivo HTML desde `frontend/templates/` y lo almacena en caché.
 * @param {string} templateName - Nombre del archivo dentro de `frontend/templates/` (ej. 'movimientos-comerciales.html')
 * @returns {Promise<string>} Contenido HTML de la plantilla
 */
export async function loadTemplate(templateName) {
  if (templateCache.has(templateName)) {
    return templateCache.get(templateName);
  }

  const url = new URL(`../../templates/${templateName}`, import.meta.url);
  const response = await fetch(url);
  if (!response.ok) {
    throw new Error(`No se pudo cargar la plantilla "${templateName}" (${response.status})`);
  }

  const html = await response.text();
  templateCache.set(templateName, html);
  return html;
}
