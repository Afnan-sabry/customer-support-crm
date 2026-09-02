import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'admin',
    loadComponent: () => import('./layouts/admin-layout/admin-layout').then(m => m.AdminLayoutComponent),
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'users',
        loadChildren: () => import('./features/users/users.routes').then(m => m.usersRoutes)
      },
      {
        path: 'roles',
        loadChildren: () => import('./features/roles/roles.routes').then(m => m.rolesRoutes)
      },
      {
        path: 'customers',
        loadChildren: () => import('./features/customers/customers.routes').then(m => m.customersRoutes)
      },
      {
        path: 'tickets',
        loadChildren: () => import('./features/tickets/tickets.routes').then(m => m.ticketsRoutes)
      },
      {
        path: 'knowledge',
        loadChildren: () => import('./features/knowledge/knowledge.routes').then(m => m.knowledgeRoutes)
      },
      {
        path: 'sla',
        loadChildren: () => import('./features/sla/sla.routes').then(m => m.slaRoutes)
      },
      {
        path: 'escalation',
        loadChildren: () => import('./features/escalation/escalation.routes').then(m => m.escalationRoutes)
      },
      {
        path: 'assignment',
        loadChildren: () => import('./features/assignment/assignment.routes').then(m => m.assignmentRoutes)
      },
    ]
  },
  {
    path: 'portal',
    loadComponent: () => import('./layouts/portal-layout/portal-layout').then(m => m.PortalLayoutComponent),
    children: [
      { path: '', redirectTo: 'home', pathMatch: 'full' },
    ]
  },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login').then(m => m.LoginComponent)
  },
  { path: '', redirectTo: '/admin', pathMatch: 'full' },
  { path: '**', redirectTo: '/admin' }
];
