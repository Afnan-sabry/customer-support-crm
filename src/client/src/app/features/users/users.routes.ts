import { Routes } from '@angular/router';

export const usersRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./user-list/user-list').then(m => m.UserListComponent)
  },
  {
    path: 'new',
    loadComponent: () => import('./user-form/user-form').then(m => m.UserFormComponent)
  },
  {
    path: ':id/edit',
    loadComponent: () => import('./user-form/user-form').then(m => m.UserFormComponent)
  }
];
