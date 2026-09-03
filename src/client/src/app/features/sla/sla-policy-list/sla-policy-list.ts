import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { SlaService, SlaPolicyDto } from '../sla.service';
import { TicketsService, TicketPriorityDto, TicketCategoryDto } from '../../tickets/tickets.service';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog';

@Component({
  selector: 'app-sla-policy-list',
  imports: [
    RouterLink, TranslateModule,
    MatTableModule, MatPaginatorModule, MatFormFieldModule,
    MatSelectModule, MatButtonModule, MatIconModule, MatChipsModule, MatDialogModule
  ],
  template: `
    <div class="list-header">
      <h1>{{ 'sla.title' | translate }}</h1>
      <button mat-raised-button color="primary" routerLink="/admin/sla/new">
        <mat-icon>add</mat-icon>
        {{ 'sla.createPolicy' | translate }}
      </button>
    </div>

    <div class="filters">
      <mat-form-field appearance="outline">
        <mat-label>{{ 'sla.priority' | translate }}</mat-label>
        <mat-select [value]="priorityId" (selectionChange)="onPriorityChange($event.value)">
          <mat-option [value]="null">{{ 'sla.allPriorities' | translate }}</mat-option>
          @for (priority of priorities; track priority.id) {
            <mat-option [value]="priority.id">{{ priority.name }}</mat-option>
          }
        </mat-select>
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>{{ 'sla.category' | translate }}</mat-label>
        <mat-select [value]="categoryId" (selectionChange)="onCategoryChange($event.value)">
          <mat-option [value]="null">{{ 'sla.allCategories' | translate }}</mat-option>
          @for (category of categories; track category.id) {
            <mat-option [value]="category.id">{{ category.name }}</mat-option>
          }
        </mat-select>
      </mat-form-field>
    </div>

    <table mat-table [dataSource]="filteredPolicies" class="mat-elevation-z2 full-width">
      <ng-container matColumnDef="name">
        <th mat-header-cell *matHeaderCellDef>{{ 'sla.name' | translate }}</th>
        <td mat-cell *matCellDef="let policy">{{ policy.name }}</td>
      </ng-container>

      <ng-container matColumnDef="priorityName">
        <th mat-header-cell *matHeaderCellDef>{{ 'sla.priority' | translate }}</th>
        <td mat-cell *matCellDef="let policy">{{ policy.priorityName || '-' }}</td>
      </ng-container>

      <ng-container matColumnDef="categoryName">
        <th mat-header-cell *matHeaderCellDef>{{ 'sla.category' | translate }}</th>
        <td mat-cell *matCellDef="let policy">{{ policy.categoryName || '-' }}</td>
      </ng-container>

      <ng-container matColumnDef="firstResponseMinutes">
        <th mat-header-cell *matHeaderCellDef>{{ 'sla.firstResponseMinutes' | translate }}</th>
        <td mat-cell *matCellDef="let policy">{{ policy.firstResponseMinutes }}</td>
      </ng-container>

      <ng-container matColumnDef="resolutionMinutes">
        <th mat-header-cell *matHeaderCellDef>{{ 'sla.resolutionMinutes' | translate }}</th>
        <td mat-cell *matCellDef="let policy">{{ policy.resolutionMinutes }}</td>
      </ng-container>

      <ng-container matColumnDef="isActive">
        <th mat-header-cell *matHeaderCellDef>{{ 'sla.active' | translate }}</th>
        <td mat-cell *matCellDef="let policy">
          <mat-chip [color]="policy.isActive ? 'primary' : 'warn'" selected>
            {{ (policy.isActive ? 'sla.active' : 'sla.inactive') | translate }}
          </mat-chip>
        </td>
      </ng-container>

      <ng-container matColumnDef="actions">
        <th mat-header-cell *matHeaderCellDef>{{ 'common.actions' | translate }}</th>
        <td mat-cell *matCellDef="let policy">
          <button mat-icon-button [routerLink]="['/admin/sla', policy.id, 'edit']">
            <mat-icon>edit</mat-icon>
          </button>
          <button mat-icon-button color="warn" (click)="onDelete(policy)">
            <mat-icon>delete</mat-icon>
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
    .list-header { display: flex; justify-content: space-between; align-items: center; margin-block-end: 16px; }
    .filters { display: flex; align-items: center; gap: 16px; margin-block-end: 16px; flex-wrap: wrap; }
    .full-width { width: 100%; }
  `]
})
export class SlaPolicyListComponent implements OnInit {
  private slaService = inject(SlaService);
  private ticketsService = inject(TicketsService);
  private dialog = inject(MatDialog);
  private translate = inject(TranslateService);

  displayedColumns = ['name', 'priorityName', 'categoryName', 'firstResponseMinutes', 'resolutionMinutes', 'isActive', 'actions'];
  policies: SlaPolicyDto[] = [];
  priorities: TicketPriorityDto[] = [];
  categories: TicketCategoryDto[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 20;
  priorityId: string | null = null;
  categoryId: string | null = null;

  get filteredPolicies(): SlaPolicyDto[] {
    return this.policies.filter(p =>
      (!this.priorityId || p.priorityId === this.priorityId) &&
      (!this.categoryId || p.categoryId === this.categoryId));
  }

  ngOnInit(): void {
    this.ticketsService.getPriorities().subscribe(priorities => this.priorities = priorities);
    this.ticketsService.getCategories().subscribe(categories => this.categories = categories);
    this.loadPolicies();
  }

  loadPolicies(): void {
    this.slaService.getSlaPolicies({ page: this.page, pageSize: this.pageSize }).subscribe(result => {
      this.policies = result.items;
      this.totalCount = result.totalCount;
      this.page = result.page;
      this.pageSize = result.pageSize;
    });
  }

  onPriorityChange(priorityId: string | null): void {
    this.priorityId = priorityId;
  }

  onCategoryChange(categoryId: string | null): void {
    this.categoryId = categoryId;
  }

  onPageChange(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadPolicies();
  }

  onDelete(policy: SlaPolicyDto): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: this.translate.instant('common.delete'),
        message: this.translate.instant('sla.deleteConfirm')
      }
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (confirmed) {
        this.slaService.deleteSlaPolicy(policy.id).subscribe(() => this.loadPolicies());
      }
    });
  }
}
