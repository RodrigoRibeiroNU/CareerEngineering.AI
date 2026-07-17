import { Injectable, signal, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { AuthService } from '@auth0/auth0-angular';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private hubConnection!: signalR.HubConnection;
  private auth = inject(AuthService);
  public streamMessage = signal<string>('');

  // Não conectamos mais no constructor!
  constructor() {}

  public async connect() {
    // Só conecta se estiver autenticado
    const isAuthenticated = await firstValueFrom(this.auth.isAuthenticated$);
    if (!isAuthenticated) return;

    const token = await firstValueFrom(this.auth.getAccessTokenSilently());

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5019/careerChatHub', {
        accessTokenFactory: () => token
      })
      .build();

    this.hubConnection.on('ReceiveToken', (token: string) => {
      this.streamMessage.update(prev => prev + token);
    });

    await this.hubConnection.start();
  }

  public sendAnalysisRequest(job: string, resume: string, userName: string, userEmail: string): void {
    this.streamMessage.set('');
    
    // Mantemos sua excelente verificação de segurança original!
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      this.hubConnection.invoke('StartAnalysis', job, resume, userName, userEmail)
        .catch(err => console.error('Erro ao invocar StartAnalysis:', err));
    } else {
      console.warn('Conexão SignalR não está ativa. Status:', this.hubConnection?.state);
    }
  }
}