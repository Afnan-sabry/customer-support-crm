import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'admin',
    loadComponent: () => import('./layouts/admin-layout/admin-layout').then(m => m.AdminLayoutComponent),
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      // Feature routes will be added in Phase 1+
    ]
  },
  {
    path: 'portal',
    loadComponent: () => import('./layouts/portal-layout/portal-layout').then(m => m.PortalLayoutComponent),
    children: [
      { path: '', redirectTo: 'home', pathMatch: 'full' },
      // Portal routes will be added in Phase 3
    ]
  },
  {
    path: 'login',
    loadComponent: () => import('./layouts/portal-layout/portal-layout').then(m => m.PortalLayoutComponent),
    // Login page will be added in Phase 1
  },
  { path: '', redirectTo: '/admin', pathMatch: 'full' },
  { path: '**', redirectTo: '/admin' }
];
