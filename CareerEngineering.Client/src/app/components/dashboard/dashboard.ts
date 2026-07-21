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
import { DocumentFieldComponent } from '../document-field/document-field';
import { NavbarComponent } from '../navbar/navbar';
import { SidebarComponent } from '../sidebar/sidebar';
import { AnaliseDetail, ChatMessageView } from '../../models/analise.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [FormsModule, NavbarComponent, SidebarComponent, DocumentFieldComponent],
  templateUrl: './dashboard.html',
})
export class DashboardComponent implements OnInit {
  private readonly signalRService = inject(SignalRService);
  private readonly systemService = inject(SystemService);
  private readonly analisesService = inject(AnalisesService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly auth = inject(AuthService);

  private readonly scrollContainer = viewChild<ElementRef<HTMLDivElement>>('chatScroll');

  private lastHandledStartedId: string | null = null;
  private lastHandledUpdatedId: string | null = null;
  private lastHandledComplete = 0;
  private loadingDetailId: string | null = null;
  /** Evita disparar RegenerateAnalysis em paralelo para o mesmo id. */
  private readonly regeneratingIds = new Set<string>();
  /** Uma tentativa de regeneração por análise nesta sessão (evita loop). */
  private readonly regenerateAttemptedIds = new Set<string>();
  /** Evita que a navegação para /analise limpe o formulário durante o modo edição. */
  private suppressFormReset = false;

  private readonly user = toSignal(this.auth.user$, { initialValue: null });
  /** No mobile inicia fechada (drawer); no desktop inicia aberta. */
  protected readonly sidebarCollapsed = signal(
    typeof window !== 'undefined' && window.matchMedia('(max-width: 767px)').matches,
  );

  protected readonly jobDescription = signal('');
  protected readonly resumeText = signal('');
  protected readonly followUpText = signal('');
  protected readonly loading = signal(false);
  protected readonly statusText = signal('Consultando o mentor de carreira...');
  protected readonly loadingDetail = this.analisesService.loadingDetail;

  protected readonly activeAnaliseId = signal<string | null>(null);
  protected readonly activeTitulo = signal<string | null>(null);
  protected readonly messages = signal<ChatMessageView[]>([]);
  /** Id da análise em edição no formulário (null = modo criação). */
  protected readonly editingAnaliseId = signal<string | null>(null);

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

  protected readonly isEditMode = computed(() => !!this.editingAnaliseId());
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
          modeloLLM: this.systemService.activeModel() || 'qwen2.5:14b',
        });
        void this.router.navigate(['/analise', started.id], { replaceUrl: true });
      });
    });

    // Após UpdateAnalysis — só sincroniza título/sidebar (a UI já está no chat).
    effect(() => {
      const updated = this.signalRService.analysisUpdated();
      if (!updated || updated.id === this.lastHandledUpdatedId) return;

      this.lastHandledUpdatedId = updated.id;

      untracked(() => {
        this.activeTitulo.set(updated.titulo);
        this.analisesService.upsertLocal({
          id: updated.id,
          titulo: updated.titulo,
          dataCriacao: new Date().toISOString(),
          modeloLLM: this.systemService.activeModel() || 'qwen2.5:14b',
        });
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
        // Sincroniza com o banco (cobre stream vazio / regeneração / atualização).
        void this.syncActiveDetailAfterComplete();
      });
    });
  }

  ngOnInit(): void {
    void this.analisesService.loadList();

    // Rota → carrega histórico (sem effect, evita loop com navigate).
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      void this.onRouteIdChange(params.get('id'));
    });
  }

  /** Botão "+" da sidebar: limpa o chat e abre o formulário de nova análise. */
  protected startNewAnalysis(): void {
    this.resetToNewAnalysisForm();
    this.closeSidebarOnMobile();
    void this.router.navigateByUrl('/analise');
  }

  /** Menu "Editar": abre o formulário preenchido com vaga/currículo da análise. */
  protected async onEditAnalysis(id: string): Promise<void> {
    this.closeSidebarOnMobile();
    this.suppressFormReset = true;

    const detail = await this.analisesService.getById(id);
    if (!detail) {
      this.suppressFormReset = false;
      return;
    }

    this.loading.set(false);
    this.loadingDetailId = null;
    this.lastHandledStartedId = null;
    this.activeAnaliseId.set(null);
    this.messages.set([]);
    this.followUpText.set('');
    this.signalRService.clearStream();

    this.editingAnaliseId.set(detail.id);
    this.activeTitulo.set(detail.titulo);
    this.jobDescription.set(detail.descricaoVaga ?? '');
    this.resumeText.set(detail.textoCurriculo ?? '');

    await this.router.navigateByUrl('/analise');
    this.suppressFormReset = false;
  }

  private resetToNewAnalysisForm(): void {
    this.loading.set(false);
    this.loadingDetailId = null;
    this.lastHandledStartedId = null;
    this.lastHandledUpdatedId = null;
    this.activeAnaliseId.set(null);
    this.activeTitulo.set(null);
    this.editingAnaliseId.set(null);
    this.messages.set([]);
    this.jobDescription.set('');
    this.resumeText.set('');
    this.followUpText.set('');
    this.signalRService.clearSession();
  }

  private async onRouteIdChange(id: string | null): Promise<void> {
    // Rota /analise (sem id) → formulário de nova análise ou edição.
    if (!id) {
      // Evita limpar no meio do StartAnalysis: AnalysisStarted ainda não navegou para /:id.
      if (this.loading() && !this.activeAnaliseId()) return;
      // Modo edição: mantém textareas populados.
      if (this.suppressFormReset || this.editingAnaliseId()) return;

      this.resetToNewAnalysisForm();
      return;
    }

    // Já estamos nesta análise (streaming ou carregada).
    if (id === this.activeAnaliseId() && (this.loading() || this.messages().length > 0)) {
      return;
    }

    // Evita GET duplicado.
    if (this.loadingDetailId === id) return;
    this.loadingDetailId = id;

    this.editingAnaliseId.set(null);
    this.activeAnaliseId.set(id);
    this.signalRService.clearStream();

    try {
      const detail = await this.analisesService.getById(id);
      if (!detail) {
        this.resetToNewAnalysisForm();
        void this.router.navigateByUrl('/analise');
        return;
      }

      // Não sobrescreve bolha em streaming.
      if (this.loading() && this.messages().some((m) => m.streaming)) {
        this.activeTitulo.set(detail.titulo);
        return;
      }

      this.activeTitulo.set(detail.titulo);
      const chatMsgs = this.mapChatMessages(detail);
      this.messages.set(chatMsgs);
      this.scrollToBottom();

      // Análise órfã: registro ok, histórico vazio → regenera o relatório inicial.
      if (chatMsgs.length === 0) {
        await this.tryRegenerateIfEmpty(detail);
      }
    } finally {
      if (this.loadingDetailId === id) {
        this.loadingDetailId = null;
      }
    }
  }

  private async syncActiveDetailAfterComplete(): Promise<void> {
    const id = this.activeAnaliseId();
    if (!id) return;

    const detail = await this.analisesService.getById(id);
    if (!detail) return;

    this.activeTitulo.set(detail.titulo);
    const chatMsgs = this.mapChatMessages(detail);

    if (chatMsgs.length > 0) {
      this.messages.set(chatMsgs);
      this.scrollToBottom();
      return;
    }

    // Ainda vazio após a geração → tenta regenerar uma vez.
    await this.tryRegenerateIfEmpty(detail);
  }

  private async tryRegenerateIfEmpty(detail: AnaliseDetail): Promise<void> {
    const chatMsgs = this.mapChatMessages(detail);
    if (chatMsgs.length > 0) return;

    if (!detail.descricaoVaga?.trim() || !detail.textoCurriculo?.trim()) return;
    if (this.regeneratingIds.has(detail.id) || this.regenerateAttemptedIds.has(detail.id)) return;

    this.regenerateAttemptedIds.add(detail.id);
    this.regeneratingIds.add(detail.id);
    this.loading.set(true);
    this.statusText.set('Gerando análise ...');
    this.signalRService.clearStream();

    try {
      await this.signalRService.regenerateAnalysis(detail.id);
    } catch (err) {
      console.error('Falha ao regenerar análise órfã:', err);
      this.loading.set(false);
    } finally {
      this.regeneratingIds.delete(detail.id);
    }
  }

  private mapChatMessages(detail: AnaliseDetail): ChatMessageView[] {
    return detail.mensagens
      .filter((m) => m.role === 'user' || m.role === 'assistant' || m.role === 'system')
      .map((m) => ({
        id: m.id,
        role: m.role as 'user' | 'assistant' | 'system',
        conteudo: m.conteudo,
      }));
  }

  protected toggleSidebar(): void {
    this.sidebarCollapsed.update((v) => !v);
  }

  protected closeSidebar(): void {
    this.sidebarCollapsed.set(true);
  }

  /** Fecha o drawer overlay apenas em viewports menores que `md`. */
  protected closeSidebarOnMobile(): void {
    if (typeof window !== 'undefined' && window.matchMedia('(max-width: 767px)').matches) {
      this.sidebarCollapsed.set(true);
    }
  }

  protected async analyze(): Promise<void> {
    if (!this.canSubmit() || this.loading()) return;

    const editId = this.editingAnaliseId();
    if (editId) {
      await this.submitUpdate(editId);
      return;
    }

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

  /**
   * Modo edição: volta ao chat com o histórico intacto e anexa o novo streaming
   * após o aviso de atualização.
   */
  private async submitUpdate(analiseId: string): Promise<void> {
    this.loading.set(true);
    this.statusText.set('Atualizando análise com os novos dados...');
    this.signalRService.clearStream();
    this.lastHandledUpdatedId = null;

    // Carrega histórico existente antes de sair do formulário.
    const detail = await this.analisesService.getById(analiseId);
    const historico = detail ? this.mapChatMessages(detail) : [];

    this.editingAnaliseId.set(null);
    this.activeAnaliseId.set(analiseId);
    this.activeTitulo.set(detail?.titulo ?? this.activeTitulo());
    this.messages.set([
      ...historico,
      {
        id: `local-system-${Date.now()}`,
        role: 'system',
        conteudo:
          '[Sistema: Os dados de Vaga/Currículo foram atualizados pelo usuário nesta etapa da sessão. Considere as novas definições para as próximas respostas]',
      },
    ]);
    this.scrollToBottom();

    void this.router.navigate(['/analise', analiseId], { replaceUrl: true });

    try {
      await this.signalRService.updateAnalysis(
        analiseId,
        this.jobDescription().trim(),
        this.resumeText().trim(),
      );
    } catch (err) {
      console.error('Falha ao atualizar análise via SignalR:', err);
      this.loading.set(false);
    }
  }

  /**
   * Enter envia a mensagem; Ctrl+Enter / Shift+Enter inserem nova linha.
   */
  protected onEnterPressed(event: Event): void {
    const keyboardEvent = event as KeyboardEvent;

    if (keyboardEvent.ctrlKey || keyboardEvent.shiftKey || keyboardEvent.metaKey) {
      return;
    }

    keyboardEvent.preventDefault();

    if (this.canSendFollowUp()) {
      void this.sendFollowUp();
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
    if (this.activeAnaliseId() === id || this.editingAnaliseId() === id) {
      this.resetToNewAnalysisForm();
      void this.router.navigateByUrl('/analise');
    }
  }

  private scrollToBottom(): void {
    queueMicrotask(() => {
      const el = this.scrollContainer()?.nativeElement;
      if (el) el.scrollTop = el.scrollHeight;
    });
  }
}
