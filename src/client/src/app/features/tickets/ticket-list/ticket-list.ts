import { Component, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { Subject, debounceTime, distinctUntilChanged, forkJoin } from 'rxjs';
import { TicketsService, TicketDto, TicketStatusDto, TicketPriorityDto, TicketCategoryDto } from '../tickets.service';
import { UsersService, UserDetail } from '../../users/users.service';

@Component({
  selector: 'app-ticket-list',
  imports: [
    RouterLink, TranslateModule, DatePipe,
    MatTableModule, MatPaginatorModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatButtonModule, MatIconModule, MatChipsModule
  ],
  template: `
    <div class="ticket-list-header">
      <h1>{{ 'tickets.title' | translate }}</h1>
      <button mat-raised-button color="primary" routerLink="/admin/tickets/new">
        <mat-icon>add</mat-icon>
        {{ 'tickets.createTicket' | translate }}
      </button>
    </div>

    <div class="filters">
      <mat-form-field appearance="outline" class="search-field">
        <mat-label>{{ 'common.search' | translate }}</mat-label>
        <input matInput (keyup)="onSearchInput($event)" [value]="search" />
        <mat-icon matSuffix>search</mat-icon>
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>{{ 'tickets.status' | translate }}</mat-label>
        <mat-select [value]="statusId" (selectionChange)="onFilterChange('statusId', $event.value)">
          <mat-option [value]="null">{{ 'tickets.allStatuses' | translate }}</mat-option>
          @for (status of statuses; track status.id) {
            <mat-option [value]="status.id">{{ status.name }}</mat-option>
          }
        </mat-select>
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>{{ 'tickets.priority' | translate }}</mat-label>
        <mat-select [value]="priorityId" (selectionChange)="onFilterChange('priorityId', $event.value)">
          <mat-option [value]="null">{{ 'tickets.allPriorities' | translate }}</mat-option>
          @for (priority of priorities; track priority.id) {
            <mat-option [value]="priority.id">{{ priority.name }}</mat-option>
          }
        </mat-select>
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>{{ 'tickets.category' | translate }}</mat-label>
        <mat-select [value]="categoryId" (selectionChange)="onFilterChange('categoryId', $event.value)">
          <mat-option [value]="null">{{ 'tickets.allCategories' | translate }}</mat-option>
          @for (category of categories; track category.id) {
            <mat-option [value]="category.id">{{ category.name }}</mat-option>
          }
        </mat-select>
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>{{ 'tickets.assignedTo' | translate }}</mat-label>
        <mat-select [value]="assignedToId" (selectionChange)="onFilterChange('assignedToId', $event.value)">
          <mat-option [value]="null">{{ 'tickets.unassigned' | translate }}</mat-option>
          @for (user of users; track user.id) {
            <mat-option [value]="user.id">{{ user.fullName }}</mat-option>
          }
        </mat-select>
      </mat-form-field>
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

      <ng-container matColumnDef="customerName">
        <th mat-header-cell *matHeaderCellDef>{{ 'tickets.customer' | translate }}</th>
        <td mat-cell *matCellDef="let ticket">{{ ticket.customerName }}</td>
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

      <ng-container matColumnDef="assignedToName">
        <th mat-header-cell *matHeaderCellDef>{{ 'tickets.assignedTo' | translate }}</th>
        <td mat-cell *matCellDef="let ticket">{{ ticket.assignedToName || ('tickets.unassigned' | translate) }}</td>
      </ng-container>

      <ng-container matColumnDef="createdAt">
        <th mat-header-cell *matHeaderCellDef>{{ 'tickets.createdAt' | translate }}</th>
        <td mat-cell *matCellDef="let ticket">{{ ticket.createdAt | date: 'short' }}</td>
      </ng-container>

      <ng-container matColumnDef="actions">
        <th mat-header-cell *matHeaderCellDef>{{ 'common.actions' | translate }}</th>
        <td mat-cell *matCellDef="let ticket">
          <button mat-icon-button [routerLink]="['/admin/tickets', ticket.id]">
            <mat-icon>visibility</mat-icon>
          </button>
        </td>
      </ng-container>

      <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
      <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
    </table>

    <mat-paginator
      [length]="totalCount"
      [pageSize]="pageSize"
      [pageIndex]="page - 1"
      [pageSizeOptions]="[10, 20, 50]"
      (page)="onPageChange($event)">
    </mat-paginator>
  `,
  styles: [`
    .ticket-list-header { display: flex; justify-content: space-between; align-items: center; margin-block-end: 16px; }
    .filters { display: flex; align-items: center; gap: 16px; margin-block-end: 16px; flex-wrap: wrap; }
    .search-field { width: 100%; max-width: 300px; }
    .full-width { width: 100%; }
  `]
})
export class TicketListComponent implements OnInit {
  private ticketsService = inject(TicketsService);
  private usersService = inject(UsersService);

  displayedColumns = ['ticketNumber', 'subject', 'customerName', 'priorityName', 'statusName', 'assignedToName', 'createdAt', 'actions'];
  tickets: TicketDto[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 20;
  search = '';

  statusId: string | null = null;
  priorityId: string | null = null;
  categoryId: string | null = null;
  assignedToId: string | null = null;

  statuses: TicketStatusDto[] = [];
  priorities: TicketPriorityDto[] = [];
  categories: TicketCategoryDto[] = [];
  users: UserDetail[] = [];

  private searchSubject = new Subject<string>();

  ngOnInit(): void {
    this.searchSubject.pipe(debounceTime(300), distinctUntilChanged()).subscribe(value => {
      this.search = value;
      this.page = 1;
      this.loadTickets();
    });

    forkJoin({
      statuses: this.ticketsService.getStatuses(),
      priorities: this.ticketsService.getPriorities(),
      categories: this.ticketsService.getCategories(),
      users: this.usersService.getUsers({ pageSize: 100 })
    }).subscribe(result => {
      this.statuses = result.statuses;
      this.priorities = result.priorities;
      this.categories = result.categories;
      this.users = result.users.items;
    });

    this.loadTickets();
  }

  loadTickets(): void {
    this.ticketsService.getTickets({
      search: this.search,
      statusId: this.statusId ?? undefined,
      priorityId: this.priorityId ?? undefined,
      categoryId: this.categoryId ?? undefined,
      assignedToId: this.assignedToId ?? undefined,
      page: this.page,
      pageSize: this.pageSize
    }).subscribe(result => {
      this.tickets = result.items;
      this.totalCount = result.totalCount;
      this.page = result.page;
      this.pageSize = result.pageSize;
    });
  }

  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchSubject.next(value);
  }

  onFilterChange(field: 'statusId' | 'priorityId' | 'categoryId' | 'assignedToId', value: string | null): void {
    (this as any)[field] = value;
    this.page = 1;
    this.loadTickets();
  }

  onPageChange(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadTickets();
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
