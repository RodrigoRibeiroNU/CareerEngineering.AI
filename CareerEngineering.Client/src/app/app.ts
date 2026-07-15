import { Component, computed, ElementRef, inject, signal, viewChild } from '@angular/core';
import { CareerMentorService } from './services/career-mentor';
import { SignalRService } from './services/signal-r';
import { effect } from '@angular/core';
import { FormsModule } from '@angular/forms';
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [ FormsModule ],
  templateUrl: './app.html',
})
export class App {
  private readonly careerMentor = inject(CareerMentorService);
  private readonly signalRService = inject(SignalRService);

  private readonly scrollContainer = viewChild<ElementRef<HTMLDivElement>>('scrollContainer');
  
  protected readonly jobDescription = signal('');
  protected readonly resumeText = signal('');
  protected readonly result = signal<string | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  
  protected readonly statusText = signal('Consultando o mentor de carreira...');

  protected readonly canSubmit = computed(
    () => this.jobDescription().trim().length > 0 && this.resumeText().trim().length > 0,
  );

  constructor() {
    effect(() => {
      const msg = this.signalRService.streamMessage();
      if (msg) {
        this.result.set(msg);
        this.loading.set(false);
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

  protected analyze(): void {
    if (!this.canSubmit() || this.loading()) {
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.result.set(null);
    this.statusText.set('Conectando ao mentor e analisando...');

    this.signalRService.streamMessage.set('');

    this.signalRService.sendAnalysisRequest(
      this.jobDescription().trim(), 
      this.resumeText().trim()
    );
  }
}