import { Injectable } from '@angular/core';

export type SupportedDocumentExtension = '.pdf' | '.docx' | '.txt';

export class UnsupportedDocumentError extends Error {
  constructor(message = 'Formato de arquivo não suportado. Use PDF, DOCX ou TXT.') {
    super(message);
    this.name = 'UnsupportedDocumentError';
  }
}

export class DocumentParseError extends Error {
  constructor(message = 'Não foi possível ler o arquivo. Verifique se ele não está corrompido.') {
    super(message);
    this.name = 'DocumentParseError';
  }
}

@Injectable({ providedIn: 'root' })
export class DocumentParserService {
  private readonly supportedExtensions: readonly SupportedDocumentExtension[] = [
    '.pdf',
    '.docx',
    '.txt',
  ];

  private workerConfigured = false;

  async extractText(file: File): Promise<string> {
    const extension = this.getExtension(file.name);

    if (!this.isSupportedExtension(extension)) {
      throw new UnsupportedDocumentError();
    }

    try {
      switch (extension) {
        case '.pdf':
          return this.cleanText(await this.extractFromPdf(file));
        case '.docx':
          return this.cleanText(await this.extractFromDocx(file));
        case '.txt':
          return this.cleanText(await this.extractFromTxt(file));
      }
    } catch (err) {
      if (err instanceof UnsupportedDocumentError || err instanceof DocumentParseError) {
        throw err;
      }
      console.error('Falha ao extrair texto do documento:', err);
      throw new DocumentParseError();
    }
  }

  isSupportedFile(file: File): boolean {
    return this.isSupportedExtension(this.getExtension(file.name));
  }

  private isSupportedExtension(ext: string): ext is SupportedDocumentExtension {
    return (this.supportedExtensions as readonly string[]).includes(ext);
  }

  private getExtension(fileName: string): string {
    const idx = fileName.lastIndexOf('.');
    return idx >= 0 ? fileName.slice(idx).toLowerCase() : '';
  }

  private async extractFromPdf(file: File): Promise<string> {
    const { getDocument, GlobalWorkerOptions } = await import('pdfjs-dist');

    if (!this.workerConfigured) {
      // Arquivo em public/assets/pdf.worker.min.mjs (copiado do pdfjs-dist).
      const baseHref = document.querySelector('base')?.href ?? document.baseURI;
      GlobalWorkerOptions.workerSrc = new URL('assets/pdf.worker.min.mjs', baseHref).toString();
      this.workerConfigured = true;
    }

    try {
      const data = new Uint8Array(await file.arrayBuffer());
      const pdf = await getDocument({ data, useSystemFonts: true }).promise;
      const pages: string[] = [];

      for (let pageNum = 1; pageNum <= pdf.numPages; pageNum++) {
        const page = await pdf.getPage(pageNum);
        const content = await page.getTextContent();
        const pageText = content.items
          .map((item) => ('str' in item ? item.str : ''))
          .join(' ');
        pages.push(pageText);
      }

      const text = pages.join('\n\n').trim();
      if (!text) {
        throw new DocumentParseError(
          'Nenhum texto foi encontrado neste PDF. Ele pode ser uma imagem escaneada.',
        );
      }

      return text;
    } catch (err) {
      if (err instanceof DocumentParseError) throw err;
      console.error('Falha ao processar PDF com pdf.js:', err);
      throw new DocumentParseError(
        'Não foi possível ler o PDF. Confirme que o arquivo não está corrompido ou protegido por senha.',
      );
    }
  }

  private async extractFromDocx(file: File): Promise<string> {
    const mammoth = await import('mammoth');
    const arrayBuffer = await file.arrayBuffer();
    const result = await mammoth.extractRawText({ arrayBuffer });
    const text = result.value?.trim() ?? '';

    if (!text) {
      throw new DocumentParseError('Nenhum texto foi encontrado neste DOCX.');
    }

    return text;
  }

  private async extractFromTxt(file: File): Promise<string> {
    const text = (await file.text()).trim();
    if (!text) {
      throw new DocumentParseError('O arquivo TXT está vazio.');
    }
    return text;
  }

  private cleanText(text: string): string {
    return text
      .replace(/\r\n/g, '\n')
      .replace(/[ \t]+\n/g, '\n')
      .replace(/\n{3,}/g, '\n\n')
      .replace(/[ \t]{2,}/g, ' ')
      .trim();
  }
}
