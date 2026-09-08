import { Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { MatTableModule } from '@angular/material/table';
import { DatePipe } from '@angular/common';
import { NgxChartsModule } from '@swimlane/ngx-charts';
import { ReportsService, CsatReportDto } from '../reports.service';
import { ReportDateRangeComponent } from '../shared/report-date-range/report-date-range';
import { ReportExportBarComponent } from '../shared/report-export-bar/report-export-bar';
import { ReportChartCardComponent } from '../shared/report-chart-card/report-chart-card';

@Component({
  selector: 'app-csat-report',
  imports: [
    TranslateModule, MatTableModule, DatePipe, NgxChartsModule,
    ReportDateRangeComponent, ReportExportBarComponent, ReportChartCardComponent
  ],
  template: `
    <h1>{{ 'reports.csat.title' | translate }}</h1>
    <app-report-date-range (dateRangeChange)="onDateChange($event)" />
    <app-report-export-bar [reportType]="'csat'" [params]="currentParams" />

    @if (report) {
      <div class="summary-cards">
        <div class="summary-card">
          <span class="label">{{ 'reports.csat.avgRating' | translate }}</span>
          <span class="value">{{ report.averageRating }} / 5</span>
        </div>
        <div class="summary-card">
          <span class="label">{{ 'reports.csat.totalResponses' | translate }}</span>
          <span class="value">{{ report.totalResponses }}</span>
        </div>
      </div>
    }

    <div class="charts-grid">
      <app-report-chart-card [title]="'reports.csat.avgRating'" [loading]="loading">
        @if (gaugeData.length > 0) {
          <ngx-charts-gauge
            [results]="gaugeData" [min]="0" [max]="5"
            [angleSpan]="240" [startAngle]="-120" [view]="[400, 250]">
          </ngx-charts-gauge>
        }
      </app-report-chart-card>

      <app-report-chart-card [title]="'reports.csat.ratingDistribution'" [loading]="loading">
        @if (distData.length > 0) {
          <ngx-charts-bar-vertical
            [results]="distData" [xAxis]="true" [yAxis]="true"
            [view]="[500, 300]">
          </ngx-charts-bar-vertical>
        }
      </app-report-chart-card>
    </div>

    @if (report && report.recentFeedback.length > 0) {
      <h3>{{ 'reports.csat.recentFeedback' | translate }}</h3>
      <table mat-table [dataSource]="report.recentFeedback" class="full-width">
        <ng-container matColumnDef="ticketNumber">
          <th mat-header-cell *matHeaderCellDef>{{ 'tickets.ticketNumber' | translate }}</th>
          <td mat-cell *matCellDef="let f">{{ f.ticketNumber }}</td>
        </ng-container>
        <ng-container matColumnDef="customerName">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.csat.customer' | translate }}</th>
          <td mat-cell *matCellDef="let f">{{ f.customerName }}</td>
        </ng-container>
        <ng-container matColumnDef="rating">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.csat.rating' | translate }}</th>
          <td mat-cell *matCellDef="let f">{{ '★'.repeat(f.rating) }}{{ '☆'.repeat(5 - f.rating) }}</td>
        </ng-container>
        <ng-container matColumnDef="comment">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.csat.comment' | translate }}</th>
          <td mat-cell *matCellDef="let f">{{ f.comment || '-' }}</td>
        </ng-container>
        <ng-container matColumnDef="submittedAt">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.csat.submittedAt' | translate }}</th>
          <td mat-cell *matCellDef="let f">{{ f.submittedAt | date: 'short' }}</td>
        </ng-container>
        <tr mat-header-row *matHeaderRowDef="feedbackColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: feedbackColumns;"></tr>
      </table>
    }
  `,
  styles: [`
    .charts-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(400px, 1fr)); gap: 16px; }
    .full-width { width: 100%; margin-block-start: 16px; }
    .summary-cards { display: flex; gap: 16px; margin-block: 16px; }
    .summary-card { padding: 16px 24px; background: rgba(76, 175, 80, 0.08); border-radius: 8px; display: flex; flex-direction: column; }
    .summary-card .label { font-size: 12px; color: rgba(0,0,0,0.6); }
    .summary-card .value { font-size: 24px; font-weight: 600; }
  `]
})
export class CsatReportComponent {
  private reportsService = inject(ReportsService);

  loading = false;
  report: CsatReportDto | null = null;
  currentParams: Record<string, any> = {};
  gaugeData: any[] = [];
  distData: any[] = [];
  feedbackColumns = ['ticketNumber', 'customerName', 'rating', 'comment', 'submittedAt'];

  onDateChange(range: { startDate: string; endDate: string }): void {
    this.currentParams = { startDate: range.startDate, endDate: range.endDate };
    this.loadReport();
  }

  private loadReport(): void {
    this.loading = true;
    this.reportsService.getCsat(this.currentParams).subscribe({
      next: (data) => {
        this.report = data;
        this.gaugeData = [{ name: 'Average Rating', value: data.averageRating }];
        this.distData = data.ratingDistribution.map(r => ({ name: `${r.rating} Star`, value: r.count }));
        this.loading = false;
      },
      error: () => { this.loading = false; }
    });
  }
}
