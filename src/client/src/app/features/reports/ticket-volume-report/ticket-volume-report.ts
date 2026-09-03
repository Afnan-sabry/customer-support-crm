import { Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { MatTableModule } from '@angular/material/table';
import { NgxChartsModule } from '@swimlane/ngx-charts';
import { ReportsService, TicketVolumeReportDto } from '../reports.service';
import { ReportDateRangeComponent } from '../shared/report-date-range/report-date-range';
import { ReportExportBarComponent } from '../shared/report-export-bar/report-export-bar';
import { ReportChartCardComponent } from '../shared/report-chart-card/report-chart-card';

@Component({
  selector: 'app-ticket-volume-report',
  imports: [
    TranslateModule, MatTableModule, NgxChartsModule,
    ReportDateRangeComponent, ReportExportBarComponent, ReportChartCardComponent
  ],
  template: `
    <h1>{{ 'reports.ticketVolume.title' | translate }}</h1>
    <app-report-date-range (dateRangeChange)="onDateChange($event)" />
    <app-report-export-bar [reportType]="'ticket-volume'" [params]="currentParams" />

    <div class="charts-grid">
      <app-report-chart-card [title]="'reports.ticketVolume.trend'" [loading]="loading">
        @if (lineData.length > 0) {
          <ngx-charts-line-chart
            [results]="lineData" [xAxis]="true" [yAxis]="true"
            [legend]="true" [showXAxisLabel]="false" [showYAxisLabel]="false"
            [view]="[700, 300]" [autoScale]="true">
          </ngx-charts-line-chart>
        }
      </app-report-chart-card>

      <app-report-chart-card [title]="'reports.ticketVolume.byCategory'" [loading]="loading">
        @if (categoryData.length > 0) {
          <ngx-charts-bar-vertical
            [results]="categoryData" [xAxis]="true" [yAxis]="true"
            [view]="[500, 300]">
          </ngx-charts-bar-vertical>
        }
      </app-report-chart-card>

      <app-report-chart-card [title]="'reports.ticketVolume.byPriority'" [loading]="loading">
        @if (priorityData.length > 0) {
          <ngx-charts-pie-chart
            [results]="priorityData" [legend]="true"
            [view]="[500, 300]">
          </ngx-charts-pie-chart>
        }
      </app-report-chart-card>
    </div>

    @if (report) {
      <div class="summary">
        <strong>{{ 'reports.ticketVolume.created' | translate }}: {{ report.totalCreated }}</strong> |
        <strong>{{ 'reports.ticketVolume.resolved' | translate }}: {{ report.totalResolved }}</strong>
      </div>
    }
  `,
  styles: [`
    .charts-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(450px, 1fr)); gap: 16px; }
    .summary { margin-block: 16px; font-size: 16px; }
  `]
})
export class TicketVolumeReportComponent {
  private reportsService = inject(ReportsService);

  loading = false;
  report: TicketVolumeReportDto | null = null;
  currentParams: Record<string, any> = {};
  lineData: any[] = [];
  categoryData: any[] = [];
  priorityData: any[] = [];

  onDateChange(range: { startDate: string; endDate: string }): void {
    this.currentParams = { startDate: range.startDate, endDate: range.endDate, groupBy: 'Day' };
    this.loadReport();
  }

  private loadReport(): void {
    this.loading = true;
    this.reportsService.getTicketVolume(this.currentParams).subscribe({
      next: (data) => {
        this.report = data;
        this.lineData = [
          { name: 'Created', series: data.timeSeries.map(t => ({ name: t.period, value: t.createdCount })) },
          { name: 'Resolved', series: data.timeSeries.map(t => ({ name: t.period, value: t.resolvedCount })) }
        ];
        this.categoryData = data.categoryBreakdown.map(c => ({ name: c.categoryName, value: c.count }));
        this.priorityData = data.priorityBreakdown.map(p => ({ name: p.priorityName, value: p.count }));
        this.loading = false;
      },
      error: () => { this.loading = false; }
    });
  }
}
