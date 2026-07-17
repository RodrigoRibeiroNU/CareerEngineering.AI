import { Component, computed, effect, inject, OnInit, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { AuthService } from '@auth0/auth0-angular';
import { FormsModule } from '@angular/forms';
import { SignalRService } from '../../services/signal-r';
import { SystemService } from '../../services/system';
import { AnalysisResultComponent } from '../analysis-result/analysis-result';
import packageJson from '../../../../package.json';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [FormsModule, AnalysisResultComponent],
  templateUrl: './dashboard.html',
})
export class DashboardComponent implements OnInit {
  private readonly signalRService = inject(SignalRService);
  private readonly systemService = inject(SystemService);
  readonly auth = inject(AuthService);

  protected readonly user = toSignal(this.auth.user$);
  protected readonly activeModel = this.systemService.activeModel;
  protected readonly appVersion = `Versão ${packageJson.version}`;
  protected readonly isDropdownOpen = signal(false);
  protected readonly jobDescription = signal('');
  protected readonly resumeText = signal('');
  protected readonly loading = signal(false);
  protected readonly statusText = signal('Consultando o mentor de carreira...');

  protected readonly result = computed(() => this.signalRService.streamMessage() || null);

  protected readonly canSubmit = computed(
    () => this.jobDescription().trim().length > 0 && this.resumeText().trim().length > 0,
  );

  constructor() {
    effect(() => {
      if (this.signalRService.analysisComplete() > 0) {
        this.loading.set(false);
      }
    });
  }

  ngOnInit(): void {
    this.systemService.loadActiveModel();
  }

  protected toggleDropdown(): void {
    this.isDropdownOpen.update((value) => !value);
  }

  protected closeDropdown(): void {
    this.isDropdownOpen.set(false);
  }

  protected async analyze(): Promise<void> {
    if (!this.canSubmit() || this.loading()) return;

    this.loading.set(true);
    this.statusText.set('Conectando ao mentor e analisando...');

    try {
      const currentUser = this.user();
      await this.signalRService.startAnalysis(
        this.jobDescription().trim(),
        this.resumeText().trim(),
        currentUser?.name ?? 'Desenvolvedor',
        currentUser?.email ?? '',
      );
      this.loading.set(false);
    } catch (err) {
      console.error('Falha na análise via SignalR:', err);
      this.loading.set(false);
    }
  }

  protected logout(): void {
    this.auth.logout({
      logoutParams: { returnTo: window.location.origin },
    });
  }
}
