import { Component, inject } from '@angular/core';
import { AuthService } from '@auth0/auth0-angular';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [],
  templateUrl: './landing.html',
})
export class LandingComponent {
  private readonly auth = inject(AuthService);

  protected login(): void {
    this.auth.loginWithRedirect({
      appState: { target: '/analise' } // Após logar, o Auth0 redirecionará o usuário direto para a ferramenta
    });
  }
}