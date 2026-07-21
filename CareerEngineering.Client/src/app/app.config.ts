import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { authHttpInterceptorFn, provideAuth0 } from '@auth0/auth0-angular';
import { provideMarkdown } from 'ngx-markdown';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withInterceptors([authHttpInterceptorFn])),
    provideRouter(routes),
    provideMarkdown(),
    provideAuth0({
      domain: 'dev-41nhlxtdpvk10ged.us.auth0.com',
      clientId: 'E6M23rVZb8kKlt3GwHjaDB6bOB3pa0O5',
      authorizationParams: {
        redirect_uri: typeof window !== 'undefined' ? window.location.origin : 'http://localhost:4200',
        audience: 'https://careerengineering-api.com',
        scope: 'openid profile email',
      },
      httpInterceptor: {
        allowedList: ['http://localhost:5019/api/*'],
      },
    }),
  ],
};
