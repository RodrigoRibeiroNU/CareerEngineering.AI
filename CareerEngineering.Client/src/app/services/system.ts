import { inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class SystemService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/system`;

  public activeModel = signal<string>('Carregando...');

  public loadActiveModel(): void {
    this.http.get<{ model: string }>(`${this.apiUrl}/active-model`).subscribe({
      next: (res) => this.activeModel.set(res.model),
      error: () => this.activeModel.set('Local LLM')
    });
  }
}