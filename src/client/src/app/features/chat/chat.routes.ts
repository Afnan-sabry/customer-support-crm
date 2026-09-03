import { Routes } from '@angular/router';

export const chatRoutes: Routes = [
  { path: '', loadComponent: () => import('./chat-console/chat-console').then(m => m.ChatConsoleComponent) },
];
