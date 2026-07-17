import { Routes } from '@angular/router';
import { AuthGuard } from '@auth0/auth0-angular';
import { LandingComponent } from './components/landing/landing';
import { DashboardComponent } from './components/dashboard/dashboard';

export const routes: Routes = [
  {
    path: '',
    component: LandingComponent,
    title: 'CareerEngineering.AI - Bem-vindo'
  },
  {
    path: 'analise',
    component: DashboardComponent,
    title: 'CareerEngineering.AI - Painel de Análise',
    canActivate: [AuthGuard] // 🔥 O Auth0 protege a rota e redireciona ao login de forma 100% automática!
  },
  {
    path: '**',
    redirectTo: '' // Fallback para a Landing Page caso digitem uma rota inexistente
  }
];