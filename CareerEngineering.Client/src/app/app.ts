import { AuthService } from '@auth0/auth0-angular';
import { Component, computed, ElementRef, inject, signal } from '@angular/core';
import { CareerMentorService } from './services/career-mentor';
import { SignalRService } from './services/signal-r';
import { effect } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { AnalysisResultComponent } from './components/analysis-result/analysis-result';
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [ FormsModule, AnalysisResultComponent ],
  templateUrl: './app.html',
})
export class App {
  private readonly careerMentor = inject(CareerMentorService);
  private readonly signalRService = inject(SignalRService);
  public auth = inject(AuthService);
  
  protected readonly jobDescription = signal('');
  protected readonly resumeText = signal('');
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  
  protected readonly statusText = signal('Consultando o mentor de carreira...');

  protected readonly result = computed(() => {
    const stream = this.signalRService.streamMessage();
    
    if (stream && this.loading()) {
      setTimeout(() => this.loading.set(false), 0);
    }
    
    return stream || null;
  });

  protected readonly canSubmit = computed(
    () => this.jobDescription().trim().length > 0 && this.resumeText().trim().length > 0,
  );

  constructor() {}
  
  protected async analyze(): Promise<void> {
    if (!this.canSubmit() || this.loading()) return;

    const isAuthenticated = await firstValueFrom(this.auth.isAuthenticated$);
    if (!isAuthenticated) {
      this.auth.loginWithRedirect(); 
      return;
    }

    await this.signalRService.connect();

    this.loading.set(true);
    this.error.set(null);
    this.statusText.set('Conectando ao mentor e analisando...');

    this.signalRService.streamMessage.set('');

    this.signalRService.sendAnalysisRequest(
      this.jobDescription().trim(), 
      this.resumeText().trim()
    );
  }
}