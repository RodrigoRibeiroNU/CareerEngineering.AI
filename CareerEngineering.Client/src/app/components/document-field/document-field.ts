import {
  Component,
  ElementRef,
  inject,
  input,
  model,
  signal,
  viewChild,
} from '@angular/core';
import { NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  DocumentParseError,
  DocumentParserService,
  UnsupportedDocumentError,
} from '../../services/document-parser';
import { ToastService } from '../../services/toast';

@Component({
  selector: 'app-document-field',
  standalone: true,
  imports: [FormsModule, NgClass],
  templateUrl: './document-field.html',
  host: {
    class: 'flex flex-1 flex-col',
  },
})
export class DocumentFieldComponent {
  readonly label = input.required<string>();
  readonly placeholder = input('');

  readonly text = model.required<string>();

  private readonly parser = inject(DocumentParserService);
  private readonly toast = inject(ToastService);
  private readonly fileInput = viewChild<ElementRef<HTMLInputElement>>('fileInput');

  protected readonly fileName = signal<string | null>(null);
  protected readonly parsing = signal(false);
  protected readonly isDragOver = signal(false);
  protected readonly copied = signal(false);

  protected readonly accept = '.pdf,.docx,.txt';

  private copyResetTimer: ReturnType<typeof setTimeout> | null = null;

  protected openFilePicker(): void {
    this.fileInput()?.nativeElement.click();
  }

  protected async copyText(): Promise<void> {
    const value = this.text();
    if (!value.trim() || this.parsing()) return;

    try {
      await navigator.clipboard.writeText(value);
      this.flashCopied();
    } catch (err) {
      console.error('Falha ao copiar para a área de transferência:', err);
      this.toast.error('Não foi possível copiar o texto. Verifique as permissões do navegador.');
    }
  }

  protected async pasteText(): Promise<void> {
    if (this.parsing()) return;

    if (!navigator.clipboard?.readText) {
      this.toast.info('Seu navegador não permite colar automaticamente. Use Ctrl+V no campo.');
      return;
    }

    try {
      const clipboardText = await navigator.clipboard.readText();
      if (!clipboardText.trim()) {
        this.toast.info('A área de transferência está vazia.');
        return;
      }
      this.text.set(clipboardText);
      this.fileName.set(null);
    } catch (err) {
      console.error('Falha ao ler a área de transferência:', err);
      this.toast.info(
        'Não foi possível colar automaticamente. Permita o acesso à área de transferência ou use Ctrl+V.',
      );
    }
  }

  private flashCopied(): void {
    if (this.copyResetTimer) {
      clearTimeout(this.copyResetTimer);
    }
    this.copied.set(true);
    this.copyResetTimer = setTimeout(() => {
      this.copied.set(false);
      this.copyResetTimer = null;
    }, 1800);
  }

  protected onFileSelected(event: Event): void {
    const inputEl = event.target as HTMLInputElement;
    const file = inputEl.files?.[0];
    inputEl.value = '';
    if (file) {
      void this.handleFile(file);
    }
  }

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    if (this.parsing()) return;
    this.isDragOver.set(true);
  }

  protected onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(false);
  }

  protected onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(false);

    if (this.parsing()) return;

    const file = event.dataTransfer?.files?.[0];
    if (!file) return;

    void this.handleFile(file);
  }

  protected clearText(): void {
    this.text.set('');
    this.fileName.set(null);
  }

  protected clearImportedFileIndicator(): void {
    this.fileName.set(null);
  }

  private async handleFile(file: File): Promise<void> {
    if (!this.parser.isSupportedFile(file)) {
      this.toast.error('Formato não suportado. Importe um arquivo PDF, DOCX ou TXT.');
      return;
    }

    this.parsing.set(true);
    this.isDragOver.set(false);

    try {
      const extracted = await this.parser.extractText(file);
      this.text.set(extracted);
      this.fileName.set(file.name);
    } catch (err) {
      const message =
        err instanceof UnsupportedDocumentError || err instanceof DocumentParseError
          ? err.message
          : 'Não foi possível ler o arquivo. Tente outro documento.';
      this.toast.error(message);
    } finally {
      this.parsing.set(false);
    }
  }
}
