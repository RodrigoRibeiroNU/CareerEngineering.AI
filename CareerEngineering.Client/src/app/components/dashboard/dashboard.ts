import {
  Component,
  computed,
  DestroyRef,
  effect,
  ElementRef,
  inject,
  OnInit,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '@auth0/auth0-angular';
import { FormsModule } from '@angular/forms';
import { AnalisesService } from '../../services/analises';
import { SignalRService } from '../../services/signal-r';
import { SystemService } from '../../services/system';
import { SidebarComponent } from '../sidebar/sidebar';
import { ChatMessageView } from '../../models/analise.models';
import packageJson from '../../../../package.json';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [FormsModule, SidebarComponent],
  templateUrl: './dashboard.html',
})
export class DashboardComponent implements OnInit {
  private readonly signalRService = inject(SignalRService);
  private readonly systemService = inject(SystemService);
  private readonly analisesService = inject(AnalisesService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  readonly auth = inject(AuthService);

  private readonly scrollContainer = viewChild<ElementRef<HTMLDivElement>>('chatScroll');

  private lastHandledStartedId: string | null = null;
  private lastHandledComplete = 0;
  private loadingDetailId: string | null = null;

  protected readonly user = toSignal(this.auth.user$, { initialValue: null });
  protected readonly activeModel = this.systemService.activeModel;
  protected readonly appVersion = `Versão ${packageJson.version}`;
  protected readonly isDropdownOpen = signal(false);
  protected readonly sidebarCollapsed = signal(false);

  protected readonly jobDescription = signal('');
  protected readonly resumeText = signal('');
  protected readonly followUpText = signal('');
  protected readonly loading = signal(false);
  protected readonly statusText = signal('Consultando o mentor de carreira...');
  protected readonly loadingDetail = this.analisesService.loadingDetail;

  protected readonly activeAnaliseId = signal<string | null>(null);
  protected readonly activeTitulo = signal<string | null>(null);
  protected readonly messages = signal<ChatMessageView[]>([]);

  protected readonly streamPreview = computed(() => this.signalRService.streamMessage());

  protected readonly canSubmit = computed(
    () => this.jobDescription().trim().length > 0 && this.resumeText().trim().length > 0,
  );

  protected readonly canSendFollowUp = computed(
    () =>
      !!this.activeAnaliseId() &&
      this.followUpText().trim().length > 0 &&
      !this.loading(),
  );

  protected readonly isChatMode = computed(() => !!this.activeAnaliseId());

  constructor() {
    // Após StartAnalysis, o Hub emite AnalysisStarted → atualiza sidebar e rota.
    effect(() => {
      const started = this.signalRService.analysisStarted();
      if (!started || started.id === this.lastHandledStartedId) return;

      this.lastHandledStartedId = started.id;

      untracked(() => {
        this.activeAnaliseId.set(started.id);
        this.activeTitulo.set(started.titulo);
        this.analisesService.upsertLocal({
          id: started.id,
          titulo: started.titulo,
          dataCriacao: new Date().toISOString(),
          modeloLLM: this.activeModel() || 'qwen2.5:14b',
        });
        void this.router.navigate(['/analise', started.id], { replaceUrl: true });
      });
    });

    // Streaming → bubble do assistente.
    effect(() => {
      const chunk = this.streamPreview();
      const id = this.activeAnaliseId();
      const isLoading = this.loading();
      if (!id || !isLoading || !chunk) return;

      untracked(() => {
        this.messages.update((msgs) => {
          const last = msgs[msgs.length - 1];
          if (last?.streaming && last.role === 'assistant') {
            return [...msgs.slice(0, -1), { ...last, conteudo: chunk }];
          }
          return [
            ...msgs,
            { id: `stream-${Date.now()}`, role: 'assistant', conteudo: chunk, streaming: true },
          ];
        });
        this.scrollToBottom();
      });
    });

    effect(() => {
      const complete = this.signalRService.analysisComplete();
      if (complete <= 0 || complete === this.lastHandledComplete) return;
      this.lastHandledComplete = complete;

      untracked(() => {
        this.loading.set(false);
        this.messages.update((msgs) =>
          msgs.map((m) => (m.streaming ? { ...m, streaming: false } : m)),
        );
        void this.analisesService.loadList();
      });
    });
  }

  ngOnInit(): void {
    this.systemService.loadActiveModel();
    void this.analisesService.loadList();

    // Rota → carrega histórico (sem effect, evita loop com navigate).
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      void this.onRouteIdChange(params.get('id'));
    });
  }

  private async onRouteIdChange(id: string | null): Promise<void> {
    // Nova análise (sem id na rota).
    if (!id) {
      // Não limpa UI se estamos no meio do streaming de uma análise recém-criada.
      if (this.loading() && this.lastHandledStartedId) return;

      this.activeAnaliseId.set(null);
      this.activeTitulo.set(null);
      this.messages.set([]);
      return;
    }

    // Já estamos nesta análise (streaming ou carregada).
    if (id === this.activeAnaliseId() && (this.loading() || this.messages().length > 0)) {
      return;
    }

    // Evita GET duplicado.
    if (this.loadingDetailId === id) return;
    this.loadingDetailId = id;

    this.activeAnaliseId.set(id);
    this.signalRService.clearStream();

    try {
      const detail = await this.analisesService.getById(id);
      if (!detail) {
        void this.router.navigateByUrl('/analise');
        return;
      }

      // Não sobrescreve bolha em streaming.
      if (this.loading() && this.messages().some((m) => m.streaming)) {
        this.activeTitulo.set(detail.titulo);
        return;
      }

      this.activeTitulo.set(detail.titulo);
      this.messages.set(
        detail.mensagens
          .filter((m) => m.role === 'user' || m.role === 'assistant')
          .map((m) => ({
            id: m.id,
            role: m.role as 'user' | 'assistant',
            conteudo: m.conteudo,
          })),
      );
      this.scrollToBottom();
    } finally {
      if (this.loadingDetailId === id) {
        this.loadingDetailId = null;
      }
    }
  }

  protected toggleSidebar(): void {
    this.sidebarCollapsed.update((v) => !v);
  }

  protected toggleDropdown(): void {
    this.isDropdownOpen.update((v) => !v);
  }

  protected closeDropdown(): void {
    this.isDropdownOpen.set(false);
  }

  protected async analyze(): Promise<void> {
    if (!this.canSubmit() || this.loading()) return;

    this.loading.set(true);
    this.statusText.set('Conectando ao mentor e analisando...');
    this.messages.set([]);
    this.lastHandledStartedId = null;
    this.signalRService.clearStream();

    try {
      const currentUser = this.user();
      await this.signalRService.startAnalysis(
        this.jobDescription().trim(),
        this.resumeText().trim(),
        currentUser?.name ?? 'Desenvolvedor',
        currentUser?.email ?? '',
      );
    } catch (err) {
      console.error('Falha na análise via SignalR:', err);
      this.loading.set(false);
    }
  }

  protected async sendFollowUp(): Promise<void> {
    const id = this.activeAnaliseId();
    const texto = this.followUpText().trim();
    if (!id || !texto || this.loading()) return;

    this.messages.update((msgs) => [
      ...msgs,
      { id: `local-user-${Date.now()}`, role: 'user', conteudo: texto },
    ]);
    this.followUpText.set('');
    this.loading.set(true);
    this.statusText.set('Gerando resposta...');
    this.signalRService.clearStream();
    this.scrollToBottom();

    try {
      await this.signalRService.sendChatMessage(id, texto);
    } catch (err) {
      console.error('Falha no follow-up via SignalR:', err);
      this.loading.set(false);
    }
  }

  protected onDeleted(id: string): void {
    if (this.activeAnaliseId() === id) {
      void this.router.navigateByUrl('/analise');
    }
  }

  protected logout(): void {
    this.auth.logout({
      logoutParams: { returnTo: window.location.origin },
    });
  }

  private scrollToBottom(): void {
    queueMicrotask(() => {
      const el = this.scrollContainer()?.nativeElement;
      if (el) el.scrollTop = el.scrollHeight;
    });
  }
}
