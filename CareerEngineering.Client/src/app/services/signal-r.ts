import { Injectable, inject, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { AuthService } from '@auth0/auth0-angular';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private readonly auth = inject(AuthService);
  private hubConnection: signalR.HubConnection | null = null;
  private connectPromise: Promise<void> | null = null;
  private listenersRegistered = false;

  readonly streamMessage = signal('');
  readonly analysisComplete = signal(0);
  readonly analysisStarted = signal<{ id: string; titulo: string } | null>(null);
  readonly analysisUpdated = signal<{ id: string; titulo: string } | null>(null);

  async ensureConnected(): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      return;
    }

    if (this.connectPromise) {
      return this.connectPromise;
    }

    this.connectPromise = this.startConnection().finally(() => {
      this.connectPromise = null;
    });

    return this.connectPromise;
  }

  async startAnalysis(
    job: string,
    resume: string,
    userName: string,
    userEmail: string,
  ): Promise<void> {
    this.streamMessage.set('');
    this.analysisStarted.set(null);
    await this.ensureConnected();

    if (this.hubConnection?.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Conexão SignalR não está ativa.');
    }

    await this.hubConnection.invoke('StartAnalysis', job, resume, userName, userEmail);
  }

  async sendChatMessage(analiseId: string, texto: string): Promise<void> {
    this.streamMessage.set('');
    await this.ensureConnected();

    if (this.hubConnection?.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Conexão SignalR não está ativa.');
    }

    await this.hubConnection.invoke('SendChatMessage', analiseId, texto);
  }

  async regenerateAnalysis(analiseId: string): Promise<void> {
    this.streamMessage.set('');
    await this.ensureConnected();

    if (this.hubConnection?.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Conexão SignalR não está ativa.');
    }

    await this.hubConnection.invoke('RegenerateAnalysis', analiseId);
  }

  /**
   * Atualiza vaga/currículo de uma análise existente e gera novo relatório
   * preservando o histórico anterior no chat.
   */
  async updateAnalysis(analiseId: string, job: string, resume: string): Promise<void> {
    this.streamMessage.set('');
    this.analysisUpdated.set(null);
    await this.ensureConnected();

    if (this.hubConnection?.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Conexão SignalR não está ativa.');
    }

    await this.hubConnection.invoke('UpdateAnalysis', analiseId, job, resume);
  }

  clearStream(): void {
    this.streamMessage.set('');
  }

  clearSession(): void {
    this.streamMessage.set('');
    this.analysisStarted.set(null);
    this.analysisUpdated.set(null);
  }

  private async startConnection(): Promise<void> {
    const isAuthenticated = await firstValueFrom(this.auth.isAuthenticated$);
    if (!isAuthenticated) {
      throw new Error('Usuário não autenticado.');
    }

    if (!this.hubConnection) {
      this.hubConnection = new signalR.HubConnectionBuilder()
        .withUrl(environment.hubUrl, {
          accessTokenFactory: () => firstValueFrom(this.auth.getAccessTokenSilently()),
        })
        .withAutomaticReconnect()
        .build();

      this.registerListeners();
    }

    if (this.hubConnection.state === signalR.HubConnectionState.Disconnected) {
      await this.hubConnection.start();
    }
  }

  private registerListeners(): void {
    if (!this.hubConnection || this.listenersRegistered) {
      return;
    }

    this.hubConnection.on('ReceiveToken', (chunk: string) => {
      this.streamMessage.update((prev) => prev + chunk);
    });

    this.hubConnection.on('AnalysisStarted', (id: string, titulo: string) => {
      this.analysisStarted.set({ id, titulo });
    });

    this.hubConnection.on('AnalysisUpdated', (id: string, titulo: string) => {
      this.analysisUpdated.set({ id, titulo });
    });

    this.hubConnection.on('AnalysisCompleted', (_analiseId?: string | null) => {
      this.analysisComplete.update((n) => n + 1);
    });

    this.listenersRegistered = true;
  }
}
