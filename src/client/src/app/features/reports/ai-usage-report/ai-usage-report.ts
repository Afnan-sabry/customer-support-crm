import { Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { MatTableModule } from '@angular/material/table';
import { NgxChartsModule } from '@swimlane/ngx-charts';
import { ReportsService, AiUsageReportDto } from '../reports.service';
import { ReportDateRangeComponent } from '../shared/report-date-range/report-date-range';
import { ReportExportBarComponent } from '../shared/report-export-bar/report-export-bar';
import { ReportChartCardComponent } from '../shared/report-chart-card/report-chart-card';

@Component({
  selector: 'app-ai-usage-report',
  imports: [
    TranslateModule, MatTableModule, NgxChartsModule,
    ReportDateRangeComponent, ReportExportBarComponent, ReportChartCardComponent
  ],
  template: `
    <h1>{{ 'reports.aiUsage.title' | translate }}</h1>
    <app-report-date-range (dateRangeChange)="onDateChange($event)" />
    <app-report-export-bar [reportType]="'ai-usage'" [params]="currentParams" />

    @if (report) {
      <div class="summary-cards">
        <div class="summary-card">
          <span class="label">{{ 'reports.aiUsage.avgConfidence' | translate }}</span>
          <span class="value">{{ report.avgConfidence }}%</span>
        </div>
        <div class="summary-card">
          <span class="label">{{ 'reports.aiUsage.totalTokens' | translate }}</span>
          <span class="value">{{ report.totalTokensUsed | number }}</span>
        </div>
      </div>
    }

    <div class="charts-grid">
      <app-report-chart-card [title]="'reports.aiUsage.byType'" [loading]="loading">
        @if (barData.length > 0) {
          <ngx-charts-bar-vertical
            [results]="barData" [xAxis]="true" [yAxis]="true"
            [view]="[500, 300]">
          </ngx-charts-bar-vertical>
        }
      </app-report-chart-card>

      <app-report-chart-card [title]="'reports.aiUsage.acceptanceRate'" [loading]="loading">
        @if (gaugeData.length > 0) {
          <ngx-charts-gauge
            [results]="gaugeData" [min]="0" [max]="100"
            [angleSpan]="240" [startAngle]="-120" [view]="[400, 250]">
          </ngx-charts-gauge>
        }
      </app-report-chart-card>

      <app-report-chart-card [title]="'reports.aiUsage.tokenTrend'" [loading]="loading">
        @if (trendData.length > 0) {
          <ngx-charts-line-chart
            [results]="trendData" [xAxis]="true" [yAxis]="true"
            [view]="[700, 300]" [autoScale]="true">
          </ngx-charts-line-chart>
        }
      </app-report-chart-card>
    </div>

    @if (report && report.suggestionsByType.length > 0) {
      <table mat-table [dataSource]="report.suggestionsByType" class="full-width">
        <ng-container matColumnDef="type">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.aiUsage.type' | translate }}</th>
          <td mat-cell *matCellDef="let s">{{ s.type }}</td>
        </ng-container>
        <ng-container matColumnDef="totalCount">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.aiUsage.total' | translate }}</th>
          <td mat-cell *matCellDef="let s">{{ s.totalCount }}</td>
        </ng-container>
        <ng-container matColumnDef="acceptedCount">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.aiUsage.accepted' | translate }}</th>
          <td mat-cell *matCellDef="let s">{{ s.acceptedCount }}</td>
        </ng-container>
        <ng-container matColumnDef="acceptanceRate">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.aiUsage.acceptanceRate' | translate }}</th>
          <td mat-cell *matCellDef="let s">{{ s.acceptanceRate }}%</td>
        </ng-container>
        <tr mat-header-row *matHeaderRowDef="typeColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: typeColumns;"></tr>
      </table>
    }
  `,
  styles: [`
    .charts-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(400px, 1fr)); gap: 16px; }
    .full-width { width: 100%; margin-block-start: 16px; }
    .summary-cards { display: flex; gap: 16px; margin-block: 16px; }
    .summary-card { padding: 16px 24px; background: rgba(63, 81, 181, 0.08); border-radius: 8px; display: flex; flex-direction: column; }
    .summary-card .label { font-size: 12px; color: rgba(0,0,0,0.6); }
    .summary-card .value { font-size: 24px; font-weight: 600; }
  `]
})
export class AiUsageReportComponent {
  private reportsService = inject(ReportsService);

  loading = false;
  report: AiUsageReportDto | null = null;
  currentParams: Record<string, any> = {};
  barData: any[] = [];
  gaugeData: any[] = [];
  trendData: any[] = [];
  typeColumns = ['type', 'totalCount', 'acceptedCount', 'acceptanceRate'];

  onDateChange(range: { startDate: string; endDate: string }): void {
    this.currentParams = { startDate: range.startDate, endDate: range.endDate };
    this.loadReport();
  }

  private loadReport(): void {
    this.loading = true;
    this.reportsService.getAiUsage(this.currentParams).subscribe({
      next: (data) => {
        this.report = data;
        this.barData = data.suggestionsByType.map(s => ({ name: s.type, value: s.totalCount }));
        const overallAcceptance = data.suggestionsByType.length > 0
          ? data.suggestionsByType.reduce((sum, s) => sum + s.acceptedCount, 0) * 100 /
            Math.max(data.suggestionsByType.reduce((sum, s) => sum + s.totalCount, 0), 1)
          : 0;
        this.gaugeData = [{ name: 'Acceptance Rate', value: Math.round(overallAcceptance * 10) / 10 }];
        this.trendData = [
          { name: 'Suggestions', series: data.timeSeries.map(t => ({ name: t.period, value: t.suggestionCount })) }
        ];
        this.loading = false;
      },
      error: () => { this.loading = false; }
    });
  }
}
