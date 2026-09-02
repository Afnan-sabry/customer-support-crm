import { Routes } from '@angular/router';

export const escalationRoutes: Routes = [
  { path: '', loadComponent: () => import('./escalation-rule-list/escalation-rule-list').then(m => m.EscalationRuleListComponent) },
  { path: 'new', loadComponent: () => import('./escalation-rule-form/escalation-rule-form').then(m => m.EscalationRuleFormComponent) },
  { path: ':id/edit', loadComponent: () => import('./escalation-rule-form/escalation-rule-form').then(m => m.EscalationRuleFormComponent) },
];
