import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { authHttpInterceptorFn, provideAuth0 } from '@auth0/auth0-angular';
import { provideMarkdown } from 'ngx-markdown';
import { routes } from './app.routes';
import { environment } from '../environments/environment';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withInterceptors([authHttpInterceptorFn])),
    provideRouter(routes),
    provideMarkdown(),
    provideAuth0({
      domain: environment.auth0.domain,
      clientId: environment.auth0.clientId,
      authorizationParams: {
        redirect_uri: typeof window !== 'undefined' ? window.location.origin : 'http://localhost:4200',
        audience: environment.auth0.audience,
        scope: 'openid profile email',
      },
      httpInterceptor: {
        allowedList: [`${environment.apiUrl}/*`],
      },
    }),
  ],
};
