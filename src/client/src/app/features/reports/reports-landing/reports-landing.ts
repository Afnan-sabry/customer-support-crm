import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

interface ReportCard {
  icon: string;
  titleKey: string;
  descKey: string;
  route: string;
}

@Component({
  selector: 'app-reports-landing',
  imports: [RouterLink, TranslateModule, MatCardModule, MatIconModule],
  template: `
    <h1>{{ 'reports.title' | translate }}</h1>
    <div class="report-grid">
      @for (card of reportCards; track card.route) {
        <mat-card class="report-card" [routerLink]="card.route">
          <mat-card-content>
            <mat-icon class="report-icon">{{ card.icon }}</mat-icon>
            <h3>{{ card.titleKey | translate }}</h3>
            <p>{{ card.descKey | translate }}</p>
          </mat-card-content>
        </mat-card>
      }
    </div>
  `,
  styles: [`
    .report-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 16px; }
    .report-card { cursor: pointer; transition: box-shadow 0.2s; }
    .report-card:hover { box-shadow: 0 4px 12px rgba(0,0,0,0.15); }
    .report-card mat-card-content { display: flex; flex-direction: column; align-items: center; text-align: center; padding: 24px 16px; }
    .report-icon { font-size: 48px; width: 48px; height: 48px; color: var(--mat-sys-primary, #1976d2); margin-block-end: 12px; }
    h3 { margin: 0 0 8px; }
    p { color: rgba(0,0,0,0.6); margin: 0; font-size: 14px; }
  `]
})
export class ReportsLandingComponent {
  reportCards: ReportCard[] = [
    { icon: 'bar_chart', titleKey: 'reports.ticketVolume.title', descKey: 'reports.ticketVolume.desc', route: 'ticket-volume' },
    { icon: 'speed', titleKey: 'reports.slaPerformance.title', descKey: 'reports.slaPerformance.desc', route: 'sla-performance' },
    { icon: 'groups', titleKey: 'reports.agentPerformance.title', descKey: 'reports.agentPerformance.desc', route: 'agent-performance' },
    { icon: 'hub', titleKey: 'reports.channelAnalytics.title', descKey: 'reports.channelAnalytics.desc', route: 'channel-analytics' },
    { icon: 'smart_toy', titleKey: 'reports.aiUsage.title', descKey: 'reports.aiUsage.desc', route: 'ai-usage' },
    { icon: 'sentiment_satisfied', titleKey: 'reports.csat.title', descKey: 'reports.csat.desc', route: 'csat' },
  ];
}
