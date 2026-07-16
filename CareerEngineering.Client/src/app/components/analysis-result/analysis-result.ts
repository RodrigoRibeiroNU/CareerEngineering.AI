import { Component, input, viewChild, ElementRef, effect } from '@angular/core';

@Component({
  selector: 'app-analysis-result',
  standalone: true,
  imports: [], // Mantemos limpo por enquanto (sem NgIf/NgFor pois usamos a nova sintaxe @if)
  templateUrl: './analysis-result.html',
})
export class AnalysisResultComponent {
  // 🔥 Versão Moderna: Usando a nova input() API do Angular (Signals nativos)
  result = input<string | null>(null);
  loading = input<boolean>(false);
  statusText = input<string>('Consultando o mentor de carreira...');

  // 🔥 Versão Moderna: Substituindo o antigo @ViewChild pelo signal-based viewChild
  private readonly scrollContainer = viewChild<ElementRef<HTMLDivElement>>('scrollContainer');

  constructor() {
    // Efeito reativo: Sempre que o sinal 'result' mudar, tenta fazer o scroll automático
    effect(() => {
      const currentText = this.result();
      if (currentText) {
        this.scrollToBottom();
      }
    });
  }

  private scrollToBottom(): void {
    setTimeout(() => {
      const container = this.scrollContainer()?.nativeElement;
      if (container) {
        container.scrollTop = container.scrollHeight;
      }
    }, 10);
  }
}