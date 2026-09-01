import { Routes } from '@angular/router';

export const ticketsRoutes: Routes = [
  { path: '', loadComponent: () => import('./ticket-list/ticket-list').then(m => m.TicketListComponent) },
  { path: 'new', loadComponent: () => import('./ticket-form/ticket-form').then(m => m.TicketFormComponent) },
  { path: ':id', loadComponent: () => import('./ticket-detail/ticket-detail').then(m => m.TicketDetailComponent) }
];
