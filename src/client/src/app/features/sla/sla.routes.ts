import { Routes } from '@angular/router';

export const slaRoutes: Routes = [
  { path: '', loadComponent: () => import('./sla-policy-list/sla-policy-list').then(m => m.SlaPolicyListComponent) },
  { path: 'new', loadComponent: () => import('./sla-policy-form/sla-policy-form').then(m => m.SlaPolicyFormComponent) },
  { path: ':id/edit', loadComponent: () => import('./sla-policy-form/sla-policy-form').then(m => m.SlaPolicyFormComponent) },
];
