import { Injectable, signal } from '@angular/core';

export type ToastKind = 'error' | 'success' | 'info';

export interface ToastMessage {
  id: number;
  kind: ToastKind;
  text: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 1;
  private dismissTimer: ReturnType<typeof setTimeout> | null = null;

  readonly current = signal<ToastMessage | null>(null);

  error(text: string, durationMs = 4500): void {
    this.show('error', text, durationMs);
  }

  success(text: string, durationMs = 3500): void {
    this.show('success', text, durationMs);
  }

  info(text: string, durationMs = 3500): void {
    this.show('info', text, durationMs);
  }

  dismiss(): void {
    if (this.dismissTimer) {
      clearTimeout(this.dismissTimer);
      this.dismissTimer = null;
    }
    this.current.set(null);
  }

  private show(kind: ToastKind, text: string, durationMs: number): void {
    this.dismiss();
    const id = this.nextId++;
    this.current.set({ id, kind, text });
    this.dismissTimer = setTimeout(() => {
      if (this.current()?.id === id) {
        this.current.set(null);
      }
      this.dismissTimer = null;
    }, durationMs);
  }
}
