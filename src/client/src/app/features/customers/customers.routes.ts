import { Routes } from '@angular/router';

export const customersRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./customer-list/customer-list').then(m => m.CustomerListComponent)
  },
  {
    path: 'new',
    loadComponent: () => import('./customer-form/customer-form').then(m => m.CustomerFormComponent)
  },
  {
    path: ':id',
    loadComponent: () => import('./customer-detail/customer-detail').then(m => m.CustomerDetailComponent)
  },
  {
    path: ':id/edit',
    loadComponent: () => import('./customer-form/customer-form').then(m => m.CustomerFormComponent)
  }
];
