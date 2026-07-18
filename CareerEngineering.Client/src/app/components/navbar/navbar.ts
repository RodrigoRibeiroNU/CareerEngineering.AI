import { Component, inject, input, OnInit, output, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { AuthService } from '@auth0/auth0-angular';
import { SystemService } from '../../services/system';
import packageJson from '../../../../package.json';

@Component({
  selector: 'app-navbar',
  standalone: true,
  templateUrl: './navbar.html',
})
export class NavbarComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly systemService = inject(SystemService);

  /** Subtítulo da sessão/vaga ativa (opcional). */
  readonly activeTitulo = input<string | null>(null);
  /** Alterna a visibilidade da sidebar de histórico. */
  readonly toggleSidebar = output<void>();

  protected readonly user = toSignal(this.auth.user$, { initialValue: null });
  protected readonly activeModel = this.systemService.activeModel;
  protected readonly appVersion = `Versão ${packageJson.version}`;
  protected readonly isDropdownOpen = signal(false);

  ngOnInit(): void {
    this.systemService.loadActiveModel();
  }

  protected onToggleSidebar(): void {
    this.toggleSidebar.emit();
  }

  protected toggleDropdown(): void {
    this.isDropdownOpen.update((v) => !v);
  }

  protected closeDropdown(): void {
    this.isDropdownOpen.set(false);
  }

  protected logout(): void {
    this.closeDropdown();
    this.auth.logout({
      logoutParams: { returnTo: window.location.origin },
    });
  }
}
