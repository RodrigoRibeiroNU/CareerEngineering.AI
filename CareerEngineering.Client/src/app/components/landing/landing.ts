import { Component, inject } from '@angular/core';
import { AuthService } from '@auth0/auth0-angular';

@Component({
  selector: 'app-landing',
  standalone: true,
  templateUrl: './landing.html',
})
export class LandingComponent {
  private readonly auth = inject(AuthService);

  protected login(): void {
    this.auth.loginWithRedirect({
      appState: { target: '/analise' },
    });
  }
}
