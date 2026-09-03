import { Component, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { PortalTicketService, PortalTicketDto } from '../portal-ticket.service';

@Component({
  selector: 'app-portal-ticket-list',
  imports: [
    RouterLink, TranslateModule, DatePipe,
    MatTableModule, MatPaginatorModule, MatButtonModule, MatIconModule, MatChipsModule
  ],
  template: `
    <div class="list-header">
      <h1>{{ 'portal.myTickets' | translate }}</h1>
      <button mat-raised-button color="primary" routerLink="/portal/tickets/new">
        <mat-icon>add</mat-icon>
        {{ 'portal.submitTicket' | translate }}
      </button>
    </div>

    <table mat-table [dataSource]="tickets" class="mat-elevation-z2 full-width">
      <ng-container matColumnDef="ticketNumber">
        <th mat-header-cell *matHeaderCellDef>{{ 'tickets.ticketNumber' | translate }}</th>
        <td mat-cell *matCellDef="let ticket">{{ ticket.ticketNumber }}</td>
      </ng-container>

      <ng-container matColumnDef="subject">
        <th mat-header-cell *matHeaderCellDef>{{ 'tickets.subject' | translate }}</th>
        <td mat-cell *matCellDef="let ticket">{{ ticket.subject }}</td>
      </ng-container>

      <ng-container matColumnDef="categoryName">
        <th mat-header-cell *matHeaderCellDef>{{ 'tickets.category' | translate }}</th>
        <td mat-cell *matCellDef="let ticket">{{ ticket.categoryName }}</td>
      </ng-container>

      <ng-container matColumnDef="priorityName">
        <th mat-header-cell *matHeaderCellDef>{{ 'tickets.priority' | translate }}</th>
        <td mat-cell *matCellDef="let ticket">
          <mat-chip [color]="priorityColor(ticket.priorityName)" selected>{{ ticket.priorityName }}</mat-chip>
        </td>
      </ng-container>

      <ng-container matColumnDef="statusName">
        <th mat-header-cell *matHeaderCellDef>{{ 'tickets.status' | translate }}</th>
        <td mat-cell *matCellDef="let ticket">
          <mat-chip color="primary" selected>{{ ticket.statusName }}</mat-chip>
        </td>
      </ng-container>

      <ng-container matColumnDef="createdAt">
        <th mat-header-cell *matHeaderCellDef>{{ 'tickets.createdAt' | translate }}</th>
        <td mat-cell *matCellDef="let ticket">{{ ticket.createdAt | date: 'short' }}</td>
      </ng-container>

      <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
      <tr mat-row *matRowDef="let row; columns: displayedColumns;"
          (click)="openTicket(row)" class="clickable-row"></tr>
    </table>

    @if (tickets.length === 0) {
      <p class="no-results">{{ 'portal.noTickets' | translate }}</p>
    }

    <mat-paginator
      [length]="totalCount"
      [pageSize]="pageSize"
      [pageIndex]="page - 1"
      [pageSizeOptions]="[10, 20, 50]"
      (page)="onPageChange($event)">
    </mat-paginator>
  `,
  styles: [`
    .list-header { display: flex; justify-content: space-between; align-items: center; margin-block-end: 16px; }
    .full-width { width: 100%; }
    .clickable-row { cursor: pointer; }
    .clickable-row:hover { background: rgba(0,0,0,0.04); }
    .no-results { color: rgba(0,0,0,0.6); padding: 16px 0; }
  `]
})
export class PortalTicketListComponent implements OnInit {
  private ticketService = inject(PortalTicketService);
  private router = inject(Router);

  displayedColumns = ['ticketNumber', 'subject', 'categoryName', 'priorityName', 'statusName', 'createdAt'];
  tickets: PortalTicketDto[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 20;

  ngOnInit(): void {
    this.loadTickets();
  }

  loadTickets(): void {
    this.ticketService.getTickets(this.page, this.pageSize).subscribe(result => {
      this.tickets = result.items;
      this.totalCount = result.totalCount;
      this.page = result.page;
      this.pageSize = result.pageSize;
    });
  }

  onPageChange(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadTickets();
  }

  openTicket(ticket: PortalTicketDto): void {
    this.router.navigate(['/portal/tickets', ticket.id]);
  }

  priorityColor(priorityName: string): 'primary' | 'accent' | 'warn' {
    const lower = (priorityName || '').toLowerCase();
    if (lower.includes('high') || lower.includes('urgent') || lower.includes('عالي') || lower.includes('عاجل')) {
      return 'warn';
    }
    if (lower.includes('medium') || lower.includes('متوسط')) {
      return 'accent';
    }
    return 'primary';
  }
}
