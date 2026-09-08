import { Routes } from '@angular/router';

export const reportsRoutes: Routes = [
  { path: '', loadComponent: () => import('./reports-landing/reports-landing').then(m => m.ReportsLandingComponent) },
  { path: 'ticket-volume', loadComponent: () => import('./ticket-volume-report/ticket-volume-report').then(m => m.TicketVolumeReportComponent) },
  { path: 'sla-performance', loadComponent: () => import('./sla-performance-report/sla-performance-report').then(m => m.SlaPerformanceReportComponent) },
  { path: 'agent-performance', loadComponent: () => import('./agent-performance-report/agent-performance-report').then(m => m.AgentPerformanceReportComponent) },
  { path: 'channel-analytics', loadComponent: () => import('./channel-analytics-report/channel-analytics-report').then(m => m.ChannelAnalyticsReportComponent) },
  { path: 'ai-usage', loadComponent: () => import('./ai-usage-report/ai-usage-report').then(m => m.AiUsageReportComponent) },
  { path: 'csat', loadComponent: () => import('./csat-report/csat-report').then(m => m.CsatReportComponent) },
];
