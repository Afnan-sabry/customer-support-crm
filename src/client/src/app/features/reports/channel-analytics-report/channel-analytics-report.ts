import { Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { MatTableModule } from '@angular/material/table';
import { NgxChartsModule } from '@swimlane/ngx-charts';
import { ReportsService, ChannelAnalyticsReportDto } from '../reports.service';
import { ReportDateRangeComponent } from '../shared/report-date-range/report-date-range';
import { ReportExportBarComponent } from '../shared/report-export-bar/report-export-bar';
import { ReportChartCardComponent } from '../shared/report-chart-card/report-chart-card';

@Component({
  selector: 'app-channel-analytics-report',
  imports: [
    TranslateModule, MatTableModule, NgxChartsModule,
    ReportDateRangeComponent, ReportExportBarComponent, ReportChartCardComponent
  ],
  template: `
    <h1>{{ 'reports.channelAnalytics.title' | translate }}</h1>
    <app-report-date-range (dateRangeChange)="onDateChange($event)" />
    <app-report-export-bar [reportType]="'channel-analytics'" [params]="currentParams" />

    <div class="charts-grid">
      <app-report-chart-card [title]="'reports.channelAnalytics.distribution'" [loading]="loading">
        @if (pieData.length > 0) {
          <ngx-charts-pie-chart
            [results]="pieData" [legend]="true"
            [view]="[500, 300]">
          </ngx-charts-pie-chart>
        }
      </app-report-chart-card>

      <app-report-chart-card [title]="'reports.channelAnalytics.volumeOverTime'" [loading]="loading">
        @if (stackedData.length > 0) {
          <ngx-charts-bar-vertical-stacked
            [results]="stackedData" [xAxis]="true" [yAxis]="true"
            [legend]="true" [view]="[600, 300]">
          </ngx-charts-bar-vertical-stacked>
        }
      </app-report-chart-card>
    </div>

    @if (report && report.channelBreakdown.length > 0) {
      <table mat-table [dataSource]="report.channelBreakdown" class="full-width">
        <ng-container matColumnDef="channel">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.channelAnalytics.channel' | translate }}</th>
          <td mat-cell *matCellDef="let c">{{ c.channel }}</td>
        </ng-container>
        <ng-container matColumnDef="conversationCount">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.channelAnalytics.conversations' | translate }}</th>
          <td mat-cell *matCellDef="let c">{{ c.conversationCount }}</td>
        </ng-container>
        <ng-container matColumnDef="messageCount">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.channelAnalytics.messages' | translate }}</th>
          <td mat-cell *matCellDef="let c">{{ c.messageCount }}</td>
        </ng-container>
        <ng-container matColumnDef="avgResponseMinutes">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.channelAnalytics.avgResponse' | translate }}</th>
          <td mat-cell *matCellDef="let c">{{ c.avgResponseMinutes }}</td>
        </ng-container>
        <tr mat-header-row *matHeaderRowDef="channelColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: channelColumns;"></tr>
      </table>
    }
  `,
  styles: [`
    .charts-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(450px, 1fr)); gap: 16px; }
    .full-width { width: 100%; margin-block-start: 16px; }
  `]
})
export class ChannelAnalyticsReportComponent {
  private reportsService = inject(ReportsService);

  loading = false;
  report: ChannelAnalyticsReportDto | null = null;
  currentParams: Record<string, any> = {};
  pieData: any[] = [];
  stackedData: any[] = [];
  channelColumns = ['channel', 'conversationCount', 'messageCount', 'avgResponseMinutes'];

  onDateChange(range: { startDate: string; endDate: string }): void {
    this.currentParams = { startDate: range.startDate, endDate: range.endDate };
    this.loadReport();
  }

  private loadReport(): void {
    this.loading = true;
    this.reportsService.getChannelAnalytics(this.currentParams).subscribe({
      next: (data) => {
        this.report = data;
        this.pieData = data.channelBreakdown.map(c => ({ name: c.channel, value: c.conversationCount }));
        const periods = [...new Set(data.timeSeries.map(t => t.period))].sort();
        const channels = [...new Set(data.timeSeries.map(t => t.channel))];
        this.stackedData = periods.map(period => ({
          name: period,
          series: channels.map(ch => ({
            name: ch,
            value: data.timeSeries.find(t => t.period === period && t.channel === ch)?.conversationCount ?? 0
          }))
        }));
        this.loading = false;
      },
      error: () => { this.loading = false; }
    });
  }
}
