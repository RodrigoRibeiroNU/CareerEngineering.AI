import { Injectable, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubConnection!: signalR.HubConnection;
  
  // Sinal para armazenar as mensagens recebidas do servidor
  public streamMessage = signal<string>('');

  constructor() {
    this.startConnection();
  }

  private startConnection() {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5019/careerChatHub')
      .build();

    this.hubConnection.start().catch(err => console.error(err));

    // ✅ Ouve o evento "ReceiveToken" e concatena
    this.hubConnection.on('ReceiveToken', (token: string) => {
      const current = this.streamMessage();
      this.streamMessage.set(current + token); // Adiciona ao texto existente
    });
  }

  // Método para disparar o início da análise para o backend
  public sendAnalysisRequest(job: string, resume: string) {
    this.streamMessage.set('');
    
    this.hubConnection.invoke('StartAnalysis', job, resume)
      .catch(err => console.error(err));
  }
}