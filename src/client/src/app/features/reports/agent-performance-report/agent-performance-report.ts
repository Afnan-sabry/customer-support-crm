import { Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { MatTableModule } from '@angular/material/table';
import { NgxChartsModule } from '@swimlane/ngx-charts';
import { ReportsService, AgentPerformanceReportDto } from '../reports.service';
import { ReportDateRangeComponent } from '../shared/report-date-range/report-date-range';
import { ReportExportBarComponent } from '../shared/report-export-bar/report-export-bar';
import { ReportChartCardComponent } from '../shared/report-chart-card/report-chart-card';

@Component({
  selector: 'app-agent-performance-report',
  imports: [
    TranslateModule, MatTableModule, NgxChartsModule,
    ReportDateRangeComponent, ReportExportBarComponent, ReportChartCardComponent
  ],
  template: `
    <h1>{{ 'reports.agentPerformance.title' | translate }}</h1>
    <app-report-date-range (dateRangeChange)="onDateChange($event)" />
    <app-report-export-bar [reportType]="'agent-performance'" [params]="currentParams" />

    @if (report && report.topPerformer) {
      <div class="top-performer">
        {{ 'reports.agentPerformance.topPerformer' | translate }}: <strong>{{ report.topPerformer.agentName }}</strong>
      </div>
    }

    <div class="charts-grid">
      <app-report-chart-card [title]="'reports.agentPerformance.ticketsPerAgent'" [loading]="loading">
        @if (barData.length > 0) {
          <ngx-charts-bar-horizontal
            [results]="barData" [xAxis]="true" [yAxis]="true"
            [view]="[600, 300]">
          </ngx-charts-bar-horizontal>
        }
      </app-report-chart-card>

      <app-report-chart-card [title]="'reports.agentPerformance.timeComparison'" [loading]="loading">
        @if (groupedBarData.length > 0) {
          <ngx-charts-bar-vertical-2d
            [results]="groupedBarData" [xAxis]="true" [yAxis]="true"
            [legend]="true" [view]="[600, 300]">
          </ngx-charts-bar-vertical-2d>
        }
      </app-report-chart-card>
    </div>

    @if (report && report.agents.length > 0) {
      <table mat-table [dataSource]="report.agents" class="full-width">
        <ng-container matColumnDef="agentName">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.agentPerformance.agent' | translate }}</th>
          <td mat-cell *matCellDef="let a">{{ a.agentName }}</td>
        </ng-container>
        <ng-container matColumnDef="ticketsHandled">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.agentPerformance.handled' | translate }}</th>
          <td mat-cell *matCellDef="let a">{{ a.ticketsHandled }}</td>
        </ng-container>
        <ng-container matColumnDef="ticketsResolved">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.agentPerformance.resolved' | translate }}</th>
          <td mat-cell *matCellDef="let a">{{ a.ticketsResolved }}</td>
        </ng-container>
        <ng-container matColumnDef="slaCompliancePercent">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.agentPerformance.slaCompliance' | translate }}</th>
          <td mat-cell *matCellDef="let a">{{ a.slaCompliancePercent }}%</td>
        </ng-container>
        <tr mat-header-row *matHeaderRowDef="agentColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: agentColumns;"></tr>
      </table>
    }
  `,
  styles: [`
    .charts-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(450px, 1fr)); gap: 16px; }
    .full-width { width: 100%; margin-block-start: 16px; }
    .top-performer { margin-block: 12px; font-size: 16px; padding: 12px; background: rgba(76, 175, 80, 0.1); border-radius: 8px; }
  `]
})
export class AgentPerformanceReportComponent {
  private reportsService = inject(ReportsService);

  loading = false;
  report: AgentPerformanceReportDto | null = null;
  currentParams: Record<string, any> = {};
  barData: any[] = [];
  groupedBarData: any[] = [];
  agentColumns = ['agentName', 'ticketsHandled', 'ticketsResolved', 'slaCompliancePercent'];

  onDateChange(range: { startDate: string; endDate: string }): void {
    this.currentParams = { startDate: range.startDate, endDate: range.endDate };
    this.loadReport();
  }

  private loadReport(): void {
    this.loading = true;
    this.reportsService.getAgentPerformance(this.currentParams).subscribe({
      next: (data) => {
        this.report = data;
        this.barData = data.agents.map(a => ({ name: a.agentName, value: a.ticketsHandled }));
        this.groupedBarData = data.agents.map(a => ({
          name: a.agentName,
          series: [
            { name: 'Avg Resolution', value: a.avgResolutionMinutes },
            { name: 'Avg First Response', value: a.avgFirstResponseMinutes }
          ]
        }));
        this.loading = false;
      },
      error: () => { this.loading = false; }
    });
  }
}
