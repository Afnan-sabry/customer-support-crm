import { Component, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { forkJoin } from 'rxjs';
import {
  DashboardService,
  DashboardStatsDto,
  SlaSummaryDto,
  AgentWorkloadDto
} from '../dashboard.service';
import { TicketDto } from '../../tickets/tickets.service';

interface StatCard {
  key: keyof DashboardStatsDto;
  icon: string;
  labelKey: string;
  warn: boolean;
}

@Component({
  selector: 'app-dashboard',
  imports: [
    RouterLink, TranslateModule, DatePipe,
    MatCardModule, MatIconModule, MatButtonModule,
    MatTableModule, MatPaginatorModule, MatProgressBarModule
  ],
  template: `
    <div class="dashboard-header">
      <h1>{{ 'dashboard.title' | translate }}</h1>
      <button mat-icon-button (click)="loadAll()" [disabled]="loading">
        <mat-icon>refresh</mat-icon>
      </button>
    </div>

    <div class="stat-cards">
      @for (card of statCards; track card.key) {
        <mat-card class="stat-card" [class.warn]="card.warn">
          <mat-card-content>
            <mat-icon>{{ card.icon }}</mat-icon>
            <div class="stat-value">{{ stats ? stats[card.key] : 0 }}</div>
            <div class="stat-label">{{ card.labelKey | translate }}</div>
          </mat-card-content>
        </mat-card>
      }
    </div>

    <mat-card class="sla-card">
      <mat-card-header>
        <mat-card-title>{{ 'dashboard.slaCompliance' | translate }}</mat-card-title>
      </mat-card-header>
      <mat-card-content>
        <div class="sla-row">
          <div class="sla-label">
            <span>{{ 'dashboard.firstResponse' | translate }}</span>
            <span>{{ slaSummary?.firstResponseCompliancePercent ?? 0 }}%</span>
          </div>
          <mat-progress-bar
            mode="determinate"
            [value]="slaSummary?.firstResponseCompliancePercent ?? 0"
            [class]="complianceClass(slaSummary?.firstResponseCompliancePercent)">
          </mat-progress-bar>
        </div>
        <div class="sla-row">
          <div class="sla-label">
            <span>{{ 'dashboard.resolution' | translate }}</span>
            <span>{{ slaSummary?.resolutionCompliancePercent ?? 0 }}%</span>
          </div>
          <mat-progress-bar
            mode="determinate"
            [value]="slaSummary?.resolutionCompliancePercent ?? 0"
            [class]="complianceClass(slaSummary?.resolutionCompliancePercent)">
          </mat-progress-bar>
        </div>
      </mat-card-content>
    </mat-card>

    <mat-card class="my-tickets-card">
      <mat-card-header>
        <mat-card-title>{{ 'dashboard.myTickets' | translate }}</mat-card-title>
      </mat-card-header>
      <mat-card-content>
        @if (myTickets.length > 0) {
          <table mat-table [dataSource]="myTickets" class="full-width">
            <ng-container matColumnDef="ticketNumber">
              <th mat-header-cell *matHeaderCellDef>{{ 'tickets.ticketNumber' | translate }}</th>
              <td mat-cell *matCellDef="let ticket">
                <a [routerLink]="['/admin/tickets', ticket.id]">{{ ticket.ticketNumber }}</a>
              </td>
            </ng-container>

            <ng-container matColumnDef="subject">
              <th mat-header-cell *matHeaderCellDef>{{ 'tickets.subject' | translate }}</th>
              <td mat-cell *matCellDef="let ticket">{{ ticket.subject }}</td>
            </ng-container>

            <ng-container matColumnDef="priorityName">
              <th mat-header-cell *matHeaderCellDef>{{ 'tickets.priority' | translate }}</th>
              <td mat-cell *matCellDef="let ticket">{{ ticket.priorityName }}</td>
            </ng-container>

            <ng-container matColumnDef="statusName">
              <th mat-header-cell *matHeaderCellDef>{{ 'tickets.status' | translate }}</th>
              <td mat-cell *matCellDef="let ticket">{{ ticket.statusName }}</td>
            </ng-container>

            <ng-container matColumnDef="createdAt">
              <th mat-header-cell *matHeaderCellDef>{{ 'tickets.createdAt' | translate }}</th>
              <td mat-cell *matCellDef="let ticket">{{ ticket.createdAt | date: 'short' }}</td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="myTicketsColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: myTicketsColumns;"></tr>
          </table>

          <mat-paginator
            [length]="myTicketsTotalCount"
            [pageSize]="myTicketsPageSize"
            [pageIndex]="myTicketsPage - 1"
            [pageSizeOptions]="[10, 20, 50]"
            (page)="onMyTicketsPageChange($event)">
          </mat-paginator>
        } @else {
          <p class="no-data">{{ 'dashboard.noTickets' | translate }}</p>
        }
      </mat-card-content>
    </mat-card>

    <mat-card class="team-workload-card">
      <mat-card-header>
        <mat-card-title>{{ 'dashboard.teamWorkload' | translate }}</mat-card-title>
      </mat-card-header>
      <mat-card-content>
        <table mat-table [dataSource]="teamWorkload" class="full-width">
          <ng-container matColumnDef="agentName">
            <th mat-header-cell *matHeaderCellDef>{{ 'dashboard.agent' | translate }}</th>
            <td mat-cell *matCellDef="let agent">{{ agent.agentName }}</td>
          </ng-container>

          <ng-container matColumnDef="openTickets">
            <th mat-header-cell *matHeaderCellDef>{{ 'dashboard.open' | translate }}</th>
            <td mat-cell *matCellDef="let agent">{{ agent.openTickets }}</td>
          </ng-container>

          <ng-container matColumnDef="overdueTickets">
            <th mat-header-cell *matHeaderCellDef>{{ 'dashboard.overdue' | translate }}</th>
            <td mat-cell *matCellDef="let agent">{{ agent.overdueTickets }}</td>
          </ng-container>

          <tr mat-header-row *matHeaderRowDef="teamWorkloadColumns"></tr>
          <tr mat-row *matRowDef="let row; columns: teamWorkloadColumns;"></tr>
        </table>
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    .dashboard-header { display: flex; justify-content: space-between; align-items: center; margin-block-end: 16px; }
    .stat-cards { display: grid; grid-template-columns: repeat(auto-fit, minmax(160px, 1fr)); gap: 16px; margin-block-end: 16px; }
    .stat-card mat-card-content { display: flex; flex-direction: column; align-items: center; text-align: center; gap: 4px; padding-block: 8px; }
    .stat-card.warn { border-inline-start: 4px solid var(--mat-sys-error, #f44336); }
    .stat-value { font-size: 28px; font-weight: 600; }
    .stat-label { font-size: 13px; color: rgba(0, 0, 0, 0.6); }
    .sla-card, .my-tickets-card, .team-workload-card { margin-block-end: 16px; }
    .sla-row { margin-block-end: 16px; }
    .sla-row:last-child { margin-block-end: 0; }
    .sla-label { display: flex; justify-content: space-between; margin-block-end: 4px; font-size: 14px; }
    .full-width { width: 100%; }
    .no-data { color: rgba(0, 0, 0, 0.6); text-align: center; padding-block: 16px; }
    ::ng-deep .compliance-good .mdc-linear-progress__bar-inner { border-color: #4caf50 !important; }
    ::ng-deep .compliance-warn .mdc-linear-progress__bar-inner { border-color: #ff9800 !important; }
    ::ng-deep .compliance-bad .mdc-linear-progress__bar-inner { border-color: #f44336 !important; }
  `]
})
export class DashboardComponent implements OnInit {
  private dashboardService = inject(DashboardService);

  loading = false;

  stats: DashboardStatsDto | null = null;
  slaSummary: SlaSummaryDto | null = null;
  myTickets: TicketDto[] = [];
  teamWorkload: AgentWorkloadDto[] = [];

  myTicketsColumns = ['ticketNumber', 'subject', 'priorityName', 'statusName', 'createdAt'];
  teamWorkloadColumns = ['agentName', 'openTickets', 'overdueTickets'];

  myTicketsPage = 1;
  myTicketsPageSize = 20;
  myTicketsTotalCount = 0;

  statCards: StatCard[] = [
    { key: 'openTickets', icon: 'confirmation_number', labelKey: 'dashboard.openTickets', warn: false },
    { key: 'overdueTickets', icon: 'warning', labelKey: 'dashboard.overdueTickets', warn: true },
    { key: 'resolvedToday', icon: 'task_alt', labelKey: 'dashboard.resolvedToday', warn: false },
    { key: 'unassignedTickets', icon: 'person_off', labelKey: 'dashboard.unassignedTickets', warn: false },
    { key: 'myOpenTickets', icon: 'assignment_ind', labelKey: 'dashboard.myOpenTickets', warn: false },
    { key: 'myOverdueTickets', icon: 'assignment_late', labelKey: 'dashboard.myOverdueTickets', warn: true },
  ];

  ngOnInit(): void {
    this.loadAll();
  }

  loadAll(): void {
    this.loading = true;
    forkJoin({
      stats: this.dashboardService.getStats(),
      slaSummary: this.dashboardService.getSlaSummary(),
      myTickets: this.dashboardService.getMyTickets(this.myTicketsPage, this.myTicketsPageSize),
      teamWorkload: this.dashboardService.getTeamWorkload()
    }).subscribe({
      next: ({ stats, slaSummary, myTickets, teamWorkload }) => {
        this.stats = stats;
        this.slaSummary = slaSummary;
        this.myTickets = myTickets.items;
        this.myTicketsTotalCount = myTickets.totalCount;
        this.myTicketsPage = myTickets.page;
        this.myTicketsPageSize = myTickets.pageSize;
        this.teamWorkload = [...teamWorkload].sort((a, b) => b.openTickets - a.openTickets);
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  onMyTicketsPageChange(event: PageEvent): void {
    this.myTicketsPage = event.pageIndex + 1;
    this.myTicketsPageSize = event.pageSize;
    this.dashboardService.getMyTickets(this.myTicketsPage, this.myTicketsPageSize).subscribe(result => {
      this.myTickets = result.items;
      this.myTicketsTotalCount = result.totalCount;
      this.myTicketsPage = result.page;
      this.myTicketsPageSize = result.pageSize;
    });
  }

  complianceClass(percent: number | undefined): string {
    const value = percent ?? 0;
    if (value >= 90) return 'compliance-good';
    if (value >= 70) return 'compliance-warn';
    return 'compliance-bad';
  }
}
