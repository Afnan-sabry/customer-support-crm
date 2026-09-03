import { Routes } from '@angular/router';

export const knowledgeRoutes: Routes = [
  { path: '', loadComponent: () => import('./knowledge-list/knowledge-list').then(m => m.KnowledgeListComponent) },
  { path: 'categories', loadComponent: () => import('./knowledge-categories/knowledge-categories').then(m => m.KnowledgeCategoriesComponent) },
  { path: 'new', loadComponent: () => import('./knowledge-editor/knowledge-editor').then(m => m.KnowledgeEditorComponent) },
  { path: ':id', loadComponent: () => import('./knowledge-article/knowledge-article').then(m => m.KnowledgeArticleComponent) },
  { path: ':id/edit', loadComponent: () => import('./knowledge-editor/knowledge-editor').then(m => m.KnowledgeEditorComponent) },
];
