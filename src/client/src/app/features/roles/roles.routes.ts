import { Routes } from '@angular/router';

export const rolesRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./role-list/role-list').then(m => m.RoleListComponent)
  },
  {
    path: 'new',
    loadComponent: () => import('./role-form/role-form').then(m => m.RoleFormComponent)
  },
  {
    path: ':id/edit',
    loadComponent: () => import('./role-form/role-form').then(m => m.RoleFormComponent)
  },
  {
    path: ':id/permissions',
    loadComponent: () => import('./role-form/role-form').then(m => m.RoleFormComponent)
  }
];
