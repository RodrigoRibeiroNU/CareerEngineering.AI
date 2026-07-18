import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AnaliseDetail, AnaliseListItem } from '../models/analise.models';

@Injectable({ providedIn: 'root' })
export class AnalisesService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5019/api/analises';

  /** Lista reativa da Sidebar. */
  readonly analises = signal<AnaliseListItem[]>([]);
  readonly loadingList = signal(false);
  readonly loadingDetail = signal(false);

  async loadList(): Promise<void> {
    this.loadingList.set(true);
    try {
      const itens = await firstValueFrom(this.http.get<AnaliseListItem[]>(this.apiUrl));
      this.analises.set(itens ?? []);
    } catch (err) {
      console.error('Falha ao carregar histórico de análises:', err);
    } finally {
      this.loadingList.set(false);
    }
  }

  async getById(id: string): Promise<AnaliseDetail | null> {
    this.loadingDetail.set(true);
    try {
      return await firstValueFrom(this.http.get<AnaliseDetail>(`${this.apiUrl}/${id}`));
    } catch (err) {
      console.error('Falha ao carregar análise:', err);
      return null;
    } finally {
      this.loadingDetail.set(false);
    }
  }

  /** Atualização otimista do título + PATCH. */
  async rename(id: string, titulo: string): Promise<boolean> {
    const previous = this.analises();
    this.analises.update((list) =>
      list.map((a) => (a.id === id ? { ...a, titulo } : a)),
    );

    try {
      await firstValueFrom(
        this.http.patch(`${this.apiUrl}/${id}/title`, { titulo }, { responseType: 'text' }),
      );
      return true;
    } catch (err) {
      console.error('Falha ao renomear análise:', err);
      this.analises.set(previous);
      return false;
    }
  }

  /** Remoção otimista + DELETE. */
  async delete(id: string): Promise<boolean> {
    const previous = this.analises();
    this.analises.update((list) => list.filter((a) => a.id !== id));

    try {
      await firstValueFrom(
        this.http.delete(`${this.apiUrl}/${id}`, { responseType: 'text' }),
      );
      return true;
    } catch (err) {
      console.error('Falha ao excluir análise:', err);
      this.analises.set(previous);
      return false;
    }
  }

  /** Insere/atualiza item na Sidebar após AnalysisStarted. */
  upsertLocal(item: AnaliseListItem): void {
    this.analises.update((list) => {
      const exists = list.some((a) => a.id === item.id);
      if (exists) {
        return list.map((a) => (a.id === item.id ? { ...a, ...item } : a));
      }
      return [item, ...list];
    });
  }
}
