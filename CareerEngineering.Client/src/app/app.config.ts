import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { provideHttpClient } from '@angular/common/http';
import { provideAuth0 } from '@auth0/auth0-angular';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(),
    provideRouter(routes),
    provideAuth0({
      domain: 'dev-41nhlxtdpvk10ged.us.auth0.com',
      clientId: 'E6M23rVZb8kKlt3GwHjaDB6bOB3pa0O5',
      authorizationParams: {
        redirect_uri: typeof window !== 'undefined' ? window.location.origin : 'http://localhost:4200',
        audience: 'https://careerengineering-api.com',
        scope: 'openid profile email'
      }
    })
  ]
};
