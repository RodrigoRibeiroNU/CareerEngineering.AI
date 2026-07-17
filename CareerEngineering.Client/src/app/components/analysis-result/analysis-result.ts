import { Component, input, viewChild, ElementRef, effect } from '@angular/core';

@Component({
  selector: 'app-analysis-result',
  standalone: true,
  templateUrl: './analysis-result.html',
})
export class AnalysisResultComponent {
  result = input<string | null>(null);
  loading = input<boolean>(false);
  statusText = input<string>('Consultando o mentor de carreira...');

  private readonly scrollContainer = viewChild<ElementRef<HTMLDivElement>>('scrollContainer');

  constructor() {
    effect(() => {
      if (this.result()) {
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
