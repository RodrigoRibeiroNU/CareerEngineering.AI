import { Component, computed, inject, signal } from '@angular/core';
import { CareerMentorService } from './services/career-mentor';

@Component({
  selector: 'app-root',
  imports: [],
  templateUrl: './app.html',
})
export class App {
  private readonly careerMentor = inject(CareerMentorService);

  protected readonly jobDescription = signal('');
  protected readonly resumeText = signal('');
  protected readonly result = signal<string | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  
  // 🔥 Novo sinal para o texto de status dinâmico
  protected readonly statusText = signal('Consultando o mentor de carreira...');

  protected readonly canSubmit = computed(
    () => this.jobDescription().trim().length > 0 && this.resumeText().trim().length > 0,
  );

  protected analyze(): void {
    if (!this.canSubmit() || this.loading()) {
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.result.set(null);

    this.careerMentor
      .evaluateGap(this.jobDescription().trim(), this.resumeText().trim())
      .subscribe({
        next: (response) => {
          this.result.set(response.missingTechnologies ?? 'Nenhuma lacuna identificada.');
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Não foi possível analisar.');
          this.loading.set(false);
        },
      });
  }
}