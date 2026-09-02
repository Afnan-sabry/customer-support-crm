import { Routes } from '@angular/router';

export const assignmentRoutes: Routes = [
  { path: '', loadComponent: () => import('./assignment-rule-list/assignment-rule-list').then(m => m.AssignmentRuleListComponent) },
  { path: 'new', loadComponent: () => import('./assignment-rule-form/assignment-rule-form').then(m => m.AssignmentRuleFormComponent) },
  { path: ':id/edit', loadComponent: () => import('./assignment-rule-form/assignment-rule-form').then(m => m.AssignmentRuleFormComponent) },
];
