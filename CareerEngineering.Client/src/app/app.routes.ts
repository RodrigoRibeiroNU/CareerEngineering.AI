import { Routes } from '@angular/router';
import { AuthGuard } from '@auth0/auth0-angular';
import { LandingComponent } from './components/landing/landing';
import { DashboardComponent } from './components/dashboard/dashboard';

export const routes: Routes = [
  {
    path: '',
    component: LandingComponent,
    title: 'CareerEngineering.AI - Bem-vindo',
  },
  {
    path: 'analise',
    component: DashboardComponent,
    title: 'CareerEngineering.AI - Painel de Análise',
    canActivate: [AuthGuard],
  },
  {
    path: 'analise/:id',
    component: DashboardComponent,
    title: 'CareerEngineering.AI - Análise',
    canActivate: [AuthGuard],
  },
  {
    path: '**',
    redirectTo: '',
  },
];
