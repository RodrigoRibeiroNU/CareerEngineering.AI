import { inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class SystemService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5019/api/system';

  public activeModel = signal<string>('Carregando...');

  public loadActiveModel(): void {
    this.http.get<{ model: string }>(`${this.apiUrl}/active-model`).subscribe({
      next: (res) => this.activeModel.set(res.model),
      error: () => this.activeModel.set('Local LLM')
    });
  }
}