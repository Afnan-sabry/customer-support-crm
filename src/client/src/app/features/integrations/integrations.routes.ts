import { Routes } from '@angular/router';

export const integrationsRoutes: Routes = [
  { path: '', loadComponent: () => import('./integrations-page/integrations-page').then(m => m.IntegrationsPageComponent) },
];
