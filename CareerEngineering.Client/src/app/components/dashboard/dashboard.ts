import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { AuthService } from '@auth0/auth0-angular';
import { FormsModule } from '@angular/forms';
import { SignalRService } from '../../services/signal-r';
import { AnalysisResultComponent } from '../analysis-result/analysis-result';


@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [ FormsModule, AnalysisResultComponent ],
  templateUrl: './dashboard.html',
})
export class DashboardComponent {
  private readonly signalRService = inject(SignalRService);
  public readonly auth = inject(AuthService);

  protected readonly user = toSignal(this.auth.user$);
  
  protected readonly isDropdownOpen = signal(false);

  protected readonly jobDescription = signal('');
  protected readonly resumeText = signal('');
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly statusText = signal('Consultando o mentor de carreira...');

  // Nosso computed moderno que monitora as mensagens vindas do hub
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

  protected toggleDropdown(): void {
    this.isDropdownOpen.update(value => !value);
  }

  protected closeDropdown(): void {
    this.isDropdownOpen.set(false);
  }

  protected async analyze(): Promise<void> {
    if (!this.canSubmit() || this.loading()) return;
  
    this.loading.set(true);
    this.error.set(null);
    this.statusText.set('Conectando ao mentor e analisando...');
  
    try {
      // 🔥 Aguarda a conexão se consolidar
      await this.signalRService.connect();
      
      const currentUser = this.user();
      const name = currentUser?.name ?? 'Desenvolvedor';
      const email = currentUser?.email ?? '';
  
      this.signalRService.sendAnalysisRequest(
        this.jobDescription().trim(), 
        this.resumeText().trim(),
        name,
        email
      );
    } catch (err) {
      console.error('Falha ao conectar no SignalR:', err);
      this.error.set('Não foi possível estabelecer conexão em tempo real com o servidor.');
      this.loading.set(false);
    }
  }

  protected logout(): void {
    this.auth.logout({ 
      logoutParams: { returnTo: window.location.origin } 
    });
  }
}