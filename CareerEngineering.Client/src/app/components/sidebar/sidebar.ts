import {
  Component,
  ElementRef,
  inject,
  input,
  output,
  signal,
  viewChildren,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AnalisesService } from '../../services/analises';
import { AnaliseListItem } from '../../models/analise.models';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [FormsModule, RouterLink, RouterLinkActive],
  templateUrl: './sidebar.html',
})
export class SidebarComponent {
  private readonly analisesService = inject(AnalisesService);

  /** Controla se o painel está expandido (desktop/mobile). */
  readonly collapsed = input(false);
  readonly toggle = output<void>();
  readonly deleted = output<string>();

  protected readonly analises = this.analisesService.analises;
  protected readonly loadingList = this.analisesService.loadingList;

  protected readonly openMenuId = signal<string | null>(null);
  protected readonly renamingId = signal<string | null>(null);
  protected readonly renameDraft = signal('');

  private readonly renameInputs = viewChildren<ElementRef<HTMLInputElement>>('renameInput');

  protected startRename(item: AnaliseListItem, event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.openMenuId.set(null);
    this.renamingId.set(item.id);
    this.renameDraft.set(item.titulo);

    queueMicrotask(() => {
      const el = this.renameInputs().find(
        (ref) => ref.nativeElement.dataset['id'] === item.id,
      );
      el?.nativeElement.focus();
      el?.nativeElement.select();
    });
  }

  protected async commitRename(item: AnaliseListItem): Promise<void> {
    if (this.renamingId() !== item.id) return;

    const novo = this.renameDraft().trim();
    this.renamingId.set(null);

    if (!novo || novo === item.titulo) return;
    await this.analisesService.rename(item.id, novo);
  }

  protected cancelRename(): void {
    this.renamingId.set(null);
  }

  protected toggleMenu(id: string, event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.openMenuId.update((current) => (current === id ? null : id));
  }

  protected closeMenu(): void {
    this.openMenuId.set(null);
  }

  protected async deleteItem(item: AnaliseListItem, event: Event): Promise<void> {
    event.preventDefault();
    event.stopPropagation();
    this.openMenuId.set(null);

    const ok = await this.analisesService.delete(item.id);
    if (ok) {
      this.deleted.emit(item.id);
    }
  }

  protected formatDate(iso: string): string {
    try {
      return new Intl.DateTimeFormat('pt-BR', {
        day: '2-digit',
        month: 'short',
        hour: '2-digit',
        minute: '2-digit',
      }).format(new Date(iso));
    } catch {
      return '';
    }
  }
}
