import { Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { MatTableModule } from '@angular/material/table';
import { DatePipe } from '@angular/common';
import { NgxChartsModule } from '@swimlane/ngx-charts';
import { ReportsService, SlaPerformanceReportDto } from '../reports.service';
import { ReportDateRangeComponent } from '../shared/report-date-range/report-date-range';
import { ReportExportBarComponent } from '../shared/report-export-bar/report-export-bar';
import { ReportChartCardComponent } from '../shared/report-chart-card/report-chart-card';

@Component({
  selector: 'app-sla-performance-report',
  imports: [
    TranslateModule, MatTableModule, DatePipe, NgxChartsModule,
    ReportDateRangeComponent, ReportExportBarComponent, ReportChartCardComponent
  ],
  template: `
    <h1>{{ 'reports.slaPerformance.title' | translate }}</h1>
    <app-report-date-range (dateRangeChange)="onDateChange($event)" />
    <app-report-export-bar [reportType]="'sla-performance'" [params]="currentParams" />

    <div class="charts-grid">
      <app-report-chart-card [title]="'reports.slaPerformance.firstResponse'" [loading]="loading">
        @if (report) {
          <ngx-charts-gauge
            [results]="[{ name: 'First Response', value: report.overallFirstResponseCompliance }]"
            [min]="0" [max]="100" [angleSpan]="240" [startAngle]="-120"
            [view]="[400, 250]">
          </ngx-charts-gauge>
        }
      </app-report-chart-card>

      <app-report-chart-card [title]="'reports.slaPerformance.resolution'" [loading]="loading">
        @if (report) {
          <ngx-charts-gauge
            [results]="[{ name: 'Resolution', value: report.overallResolutionCompliance }]"
            [min]="0" [max]="100" [angleSpan]="240" [startAngle]="-120"
            [view]="[400, 250]">
          </ngx-charts-gauge>
        }
      </app-report-chart-card>

      <app-report-chart-card [title]="'reports.slaPerformance.trend'" [loading]="loading">
        @if (trendData.length > 0) {
          <ngx-charts-line-chart
            [results]="trendData" [xAxis]="true" [yAxis]="true"
            [legend]="true" [view]="[700, 300]" [autoScale]="true">
          </ngx-charts-line-chart>
        }
      </app-report-chart-card>
    </div>

    @if (report && report.breachDetails.length > 0) {
      <h3>{{ 'reports.slaPerformance.breaches' | translate }}</h3>
      <table mat-table [dataSource]="report.breachDetails" class="full-width">
        <ng-container matColumnDef="ticketNumber">
          <th mat-header-cell *matHeaderCellDef>{{ 'tickets.ticketNumber' | translate }}</th>
          <td mat-cell *matCellDef="let b">{{ b.ticketNumber }}</td>
        </ng-container>
        <ng-container matColumnDef="breachType">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.slaPerformance.breachType' | translate }}</th>
          <td mat-cell *matCellDef="let b">{{ b.breachType }}</td>
        </ng-container>
        <ng-container matColumnDef="policyName">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.slaPerformance.policy' | translate }}</th>
          <td mat-cell *matCellDef="let b">{{ b.policyName }}</td>
        </ng-container>
        <ng-container matColumnDef="minutesLate">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.slaPerformance.minutesLate' | translate }}</th>
          <td mat-cell *matCellDef="let b">{{ b.minutesLate | number:'1.0-0' }}</td>
        </ng-container>
        <tr mat-header-row *matHeaderRowDef="breachColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: breachColumns;"></tr>
      </table>
    }
  `,
  styles: [`
    .charts-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(400px, 1fr)); gap: 16px; }
    .full-width { width: 100%; }
  `]
})
export class SlaPerformanceReportComponent {
  private reportsService = inject(ReportsService);

  loading = false;
  report: SlaPerformanceReportDto | null = null;
  currentParams: Record<string, any> = {};
  trendData: any[] = [];
  breachColumns = ['ticketNumber', 'breachType', 'policyName', 'minutesLate'];

  onDateChange(range: { startDate: string; endDate: string }): void {
    this.currentParams = { startDate: range.startDate, endDate: range.endDate };
    this.loadReport();
  }

  private loadReport(): void {
    this.loading = true;
    this.reportsService.getSlaPerformance(this.currentParams).subscribe({
      next: (data) => {
        this.report = data;
        this.trendData = [
          { name: 'FR On Time', series: data.timeSeries.map(t => ({ name: t.period, value: t.firstResponseOnTime })) },
          { name: 'FR Breached', series: data.timeSeries.map(t => ({ name: t.period, value: t.firstResponseBreached })) },
          { name: 'Res On Time', series: data.timeSeries.map(t => ({ name: t.period, value: t.resolutionOnTime })) },
          { name: 'Res Breached', series: data.timeSeries.map(t => ({ name: t.period, value: t.resolutionBreached })) }
        ];
        this.loading = false;
      },
      error: () => { this.loading = false; }
    });
  }
}
