import { Routes } from '@angular/router';
import { portalAuthGuard } from '../../core/guards/portal-auth.guard';

export const portalRoutes: Routes = [
  { path: 'login', loadComponent: () => import('./portal-login/portal-login').then(m => m.PortalLoginComponent) },
  { path: 'register', loadComponent: () => import('./portal-register/portal-register').then(m => m.PortalRegisterComponent) },
  {
    path: '',
    canActivate: [portalAuthGuard],
    children: [
      { path: 'home', loadComponent: () => import('./portal-home/portal-home').then(m => m.PortalHomeComponent) },
      { path: 'tickets', loadComponent: () => import('./portal-ticket-list/portal-ticket-list').then(m => m.PortalTicketListComponent) },
      { path: 'tickets/new', loadComponent: () => import('./portal-ticket-form/portal-ticket-form').then(m => m.PortalTicketFormComponent) },
      { path: 'tickets/:id', loadComponent: () => import('./portal-ticket-detail/portal-ticket-detail').then(m => m.PortalTicketDetailComponent) },
      { path: 'knowledge', loadComponent: () => import('./portal-knowledge-list/portal-knowledge-list').then(m => m.PortalKnowledgeListComponent) },
      { path: 'knowledge/:id', loadComponent: () => import('./portal-knowledge-viewer/portal-knowledge-viewer').then(m => m.PortalKnowledgeViewerComponent) },
      { path: 'profile', loadComponent: () => import('./portal-profile/portal-profile').then(m => m.PortalProfileComponent) },
      { path: '', redirectTo: 'home', pathMatch: 'full' },
    ]
  }
];
