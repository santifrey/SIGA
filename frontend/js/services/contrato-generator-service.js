/**
 * SIGA - services/contrato-generator-service.js
 * Servicio para generación dinámica de contratos de clientes en PDF
 * basado en la plantilla oficial "MODELO DE CONTRATO PARA CLIENTES.pdf".
 */

export class ContratoGeneratorService {
  constructor() {
    this._templateCache = null;
    this._cacheContratos = {};
    this.templateUrl = 'assets/MODELO_DE_CONTRATO_PARA_CLIENTES.pdf';
  }

  /**
   * Obtiene los bytes de la plantilla base en PDF
   */
  async getTemplateBytes() {
    if (this._templateCache) {
      return this._templateCache;
    }
    const response = await fetch(this.templateUrl);
    if (!response.ok) {
      throw new Error(`No se pudo cargar la plantilla de contrato desde ${this.templateUrl} (HTTP ${response.status})`);
    }
    const buffer = await response.arrayBuffer();
    this._templateCache = new Uint8Array(buffer);
    return this._templateCache;
  }

  /**
   * Genera el PDF del contrato completado con los datos correspondientes.
   * @param {Object} data
   * @param {string} data.numeroContrato - ej. CTR-2026-0042
   * @param {string|number} data.anioInicio - Año de inicio (ej. 2026)
   * @param {string} data.sucursal - Nombre de la sucursal o predio
   * @param {string} data.referente - Referente del productor / sucursal
   * @param {Object} data.cliente - Datos del cliente comitente
   * @param {Object} data.cormos - Cantidades por clase / calibre
   * @param {boolean} data.incluirAnexoCopropietarios - Si se incluye la página 4
   * @param {Array} [data.copropietarios] - Lista opcional de copropietarios
   * @returns {Promise<{ pdfBytes: Uint8Array, blob: Blob, dataUrl: string, filename: string, pageCount: number }>}
   */
  async generarContrato(data) {
    if (typeof window.PDFLib === 'undefined') {
      throw new Error('La biblioteca PDFLib no está cargada en el navegador.');
    }

    const { PDFDocument, rgb, StandardFonts } = window.PDFLib;
    const templateBytes = await this.getTemplateBytes();

    // Crear una copia independiente del documento plantilla
    const doc = await PDFDocument.load(templateBytes);
    const font = await doc.embedFont(StandardFonts.Helvetica);
    const fontBold = await doc.embedFont(StandardFonts.HelveticaBold);

    const pages = doc.getPages();
    if (pages.length === 0) {
      throw new Error('La plantilla de contrato está vacía o dañada.');
    }

    const page1 = pages[0];

    // 1. Año de inicio (superior derecha)
    const anio = String(data.anioInicio || new Date().getFullYear());
    page1.drawRectangle({
      x: 450,
      y: 746,
      width: 85,
      height: 14,
      color: rgb(1, 1, 1),
    });
    page1.drawText(anio, {
      x: 462,
      y: 750,
      size: 9,
      font: fontBold,
      color: rgb(0.1, 0.45, 0.1),
    });

    // 2. Encabezado Sucursal y Referente
    const sucursalStr = String(data.sucursal || 'Central - Villa General Belgrano');
    const referenteStr = String(data.referente || 'Federico Carlos Paak');
    page1.drawText(sucursalStr, { x: 105, y: 728, size: 8.5, font });
    page1.drawText(referenteStr, { x: 130, y: 716, size: 8.5, font });

    // 3. Preliminar - Datos del Cliente (COMITENTE)
    const cli = data.cliente || {};
    const nombreCompleto = String(cli.nombreCompleto || `${cli.nombre || ''} ${cli.apellido || ''}`).trim() || '____________________';
    const tel = String(cli.telefono || 'Sin registrar');
    const direccion = String(cli.direccion || 'Sin registrar');
    const email = String(cli.email || cli.domicilioElectronico || 'Sin registrar');
    const docIdentidad = String(cli.documento || cli.dni || cli.cuit || 'Sin registrar');
    const fechaNac = String(cli.nacidoElDia || cli.fechaNacimiento || '-');
    const profesion = String(cli.profesion || 'Productor / Inversor');
    const estadoCivil = String(cli.estadoCivil || 'Soltero/a');

    page1.drawText(nombreCompleto, { x: 145, y: 638, size: 8.5, font: fontBold });
    page1.drawText(tel, { x: 100, y: 627, size: 8.5, font });
    page1.drawText(direccion, { x: 125, y: 615, size: 8.5, font });
    page1.drawText(email, { x: 165, y: 603, size: 8.5, font });
    page1.drawText(docIdentidad, { x: 180, y: 592, size: 8.5, font: fontBold });
    page1.drawText(fechaNac, { x: 135, y: 580, size: 8.5, font });
    page1.drawText(profesion, { x: 135, y: 569, size: 8.5, font });
    page1.drawText(estadoCivil, { x: 140, y: 557, size: 8.5, font });

    // 4. Cláusula PRIMERA - Cantidades de Cormos
    const cormos = data.cormos || {};
    const cantCormillos = String(cormos.cormillos ?? '0');
    const cantN1 = String(cormos.n1 ?? cormos.calibre1 ?? '0');
    const cantN2 = String(cormos.n2 ?? cormos.calibre2 ?? '0');
    const cantN3 = String(cormos.n3 ?? cormos.calibre3 ?? '0');
    const cantN4 = String(cormos.n4 ?? cormos.calibre4 ?? '0');

    // a) Cormillos: cubre exacto <<cormillos>>
    page1.drawRectangle({ x: 341, y: 414, width: 68, height: 11, color: rgb(1, 1, 1) });
    page1.drawText(cantCormillos, { x: 360, y: 417, size: 8.5, font: fontBold });

    // b) Calibre 1: cubre exacto <<N1>>
    page1.drawRectangle({ x: 90, y: 403, width: 30, height: 11, color: rgb(1, 1, 1) });
    page1.drawText(cantN1, { x: 95, y: 406, size: 8.5, font: fontBold });

    // c) Calibre 2: cubre exacto <<N2>>
    page1.drawRectangle({ x: 260, y: 403, width: 30, height: 11, color: rgb(1, 1, 1) });
    page1.drawText(cantN2, { x: 265, y: 406, size: 8.5, font: fontBold });

    // d) Calibre 3: cubre exacto <<N3>>
    page1.drawRectangle({ x: 443, y: 403, width: 30, height: 11, color: rgb(1, 1, 1) });
    page1.drawText(cantN3, { x: 448, y: 406, size: 8.5, font: fontBold });

    // e) Calibre 4: cubre exacto <<N4>>
    page1.drawRectangle({ x: 186, y: 392, width: 30, height: 11, color: rgb(1, 1, 1) });
    page1.drawText(cantN4, { x: 191, y: 395, size: 8.5, font: fontBold });

    // 5. Gestión del Anexo de Copropietarios (Página 4 / índice 3)
    const incluirAnexo = Boolean(data.incluirAnexoCopropietarios);
    if (!incluirAnexo) {
      // Si no se requiere el anexo, se descarta la última página (página 4)
      if (doc.getPageCount() >= 4) {
        doc.removePage(3);
      }
    } else if (doc.getPageCount() >= 4) {
      // Se incluye la última página con los copropietarios
      const page4 = doc.getPage(3);
      const cops = Array.isArray(data.copropietarios) ? data.copropietarios : [];
      const cop1 = cops[0] || {};
      const cop2 = cops[1] || {};
      const cop3 = cops[2] || {};

      // Copropietario 1
      page4.drawRectangle({ x: 115, y: 420, width: 230, height: 12, color: rgb(1, 1, 1) });
      if (cop1.nombre) {
        page4.drawText(String(cop1.nombre), { x: 118, y: 423, size: 8.5, font: fontBold });
      }
      page4.drawRectangle({ x: 115, y: 386, width: 150, height: 12, color: rgb(1, 1, 1) });
      if (cop1.dni) {
        page4.drawText(String(cop1.dni), { x: 118, y: 389, size: 8.5, font: fontBold });
      }

      // Copropietario 2
      page4.drawRectangle({ x: 115, y: 309, width: 230, height: 12, color: rgb(1, 1, 1) });
      if (cop2.nombre) {
        page4.drawText(String(cop2.nombre), { x: 118, y: 312, size: 8.5, font: fontBold });
      }
      page4.drawRectangle({ x: 115, y: 275, width: 150, height: 12, color: rgb(1, 1, 1) });
      if (cop2.dni) {
        page4.drawText(String(cop2.dni), { x: 118, y: 278, size: 8.5, font: fontBold });
      }

      // Copropietario 3
      page4.drawRectangle({ x: 115, y: 197, width: 230, height: 12, color: rgb(1, 1, 1) });
      if (cop3.nombre) {
        page4.drawText(String(cop3.nombre), { x: 118, y: 200, size: 8.5, font: fontBold });
      }
      page4.drawRectangle({ x: 115, y: 163, width: 150, height: 12, color: rgb(1, 1, 1) });
      if (cop3.dni) {
        page4.drawText(String(cop3.dni), { x: 118, y: 166, size: 8.5, font: fontBold });
      }
    }

    const pdfBytes = await doc.save();
    const blob = new Blob([pdfBytes], { type: 'application/pdf' });
    const dataUrl = await this._blobToDataUrl(blob, pdfBytes);
    const cleanNum = (data.numeroContrato || 'CTR').replace(/[^a-zA-Z0-9_-]/g, '_');
    const cleanCli = nombreCompleto.replace(/[^a-zA-Z0-9_-]/g, '_').substring(0, 25);
    const filename = `Contrato_${cleanNum}_${cleanCli}.pdf`;

    return {
      pdfBytes,
      blob,
      dataUrl,
      filename,
      pageCount: doc.getPageCount(),
    };
  }

  /**
   * Guarda en caché local el contrato generado para consulta inmediata
   */
  guardarContratoCache(movId, dataUrl, filename) {
    if (!this._cacheContratos) this._cacheContratos = {};
    this._cacheContratos[movId] = { dataUrl, filename, fecha: new Date().toISOString() };
    try {
      sessionStorage.setItem(`siga_contrato_${movId}`, JSON.stringify({ dataUrl, filename, fecha: new Date().toISOString() }));
    } catch (e) {
      // ignore storage quota error
    }
  }

  /**
   * Obtiene el contrato en caché local si ya fue generado
   */
  obtenerContratoCache(movId) {
    if (this._cacheContratos && this._cacheContratos[movId]) {
      return this._cacheContratos[movId];
    }
    try {
      const stored = sessionStorage.getItem(`siga_contrato_${movId}`);
      if (stored) return JSON.parse(stored);
    } catch (e) {
      // ignore storage error
    }
    return null;
  }

  /**
   * Dispara la descarga del PDF generado en el navegador del cliente
   */
  descargarPdf(blobOrBytes, filename = 'Contrato_Cliente.pdf') {
    const blob = blobOrBytes instanceof Blob
      ? blobOrBytes
      : new Blob([blobOrBytes], { type: 'application/pdf' });

    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    setTimeout(() => {
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    }, 200);
  }

  async _blobToDataUrl(blob, pdfBytes) {
    if (typeof FileReader !== 'undefined') {
      return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onloadend = () => resolve(reader.result);
        reader.onerror = reject;
        reader.readAsDataURL(blob);
      });
    }
    const bytes = pdfBytes || new Uint8Array(await blob.arrayBuffer());
    let binary = '';
    const len = bytes.byteLength;
    for (let i = 0; i < len; i++) {
      binary += String.fromCharCode(bytes[i]);
    }
    const b64 = (typeof btoa !== 'undefined') ? btoa(binary) : Buffer.from(binary, 'binary').toString('base64');
    return `data:application/pdf;base64,${b64}`;
  }
}

export const contratoGeneratorService = new ContratoGeneratorService();
